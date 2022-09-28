using System.Text.Json;

using Cronos.Scheduling.Models;

using Hangfire;
using Hangfire.Storage;

namespace Cronos.Scheduling.Services;
public class FileWatcherService : BackgroundService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileWatcherService> _logger;

    public FileWatcherService(IWebHostEnvironment environment, ILogger<FileWatcherService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    private IStorageConnection JobStorageConnections => JobStorage.Current.GetConnection();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await DoWork(stoppingToken);


    private async Task DoWork(CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(_environment.ContentRootPath, "file.json");

        var fileName = Path.GetFileName(filePath);

        using var fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(filePath)!);

        fileSystemWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.Security;

        fileSystemWatcher.Filter = fileName;

        fileSystemWatcher.Changed += OnFileChanged;
        fileSystemWatcher.EnableRaisingEvents = true;

        _logger.LogInformation("Starting background process...");

        using var jobStorage = JobStorageConnections;

        if (jobStorage.GetRecurringJobs().Count == 0)
        {
            _logger.LogInformation("No recurring jobs were found, processing file for the first time!");
            RunBackgroundJobs();
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(int.MaxValue, cancellationToken);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (eventArgs.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        _logger.LogInformation("File was update, jobs are being recreated!");

        using var jobStorage = JobStorageConnections;

        foreach (var recurringJob in jobStorage.GetRecurringJobs())
        {
            RecurringJob.RemoveIfExists(recurringJob.Id);
        }

        RunBackgroundJobs();
    }

    private void RunBackgroundJobs()
    {
        using StreamReader streamReader = new(Path.Combine(_environment.ContentRootPath, "file.json"));

        var jobsModel = JsonSerializer.Deserialize<JobsModel>(streamReader.BaseStream);

        if (jobsModel is not null)
        {
            foreach (var job in jobsModel.Jobs)
            {
                RecurringJob.AddOrUpdate<CommandRunner>(job.Name, runner => runner.RunCommand(job.Name, jobsModel.CommandParser, job.Command), job.Expression);
            }
        }
    }
}
