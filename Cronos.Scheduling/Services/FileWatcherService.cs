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

        using var jobStorageConnection = JobStorage.Current.GetConnection();

        foreach (var recurringJob in jobStorageConnection.GetRecurringJobs())
        {
            RecurringJob.RemoveIfExists(recurringJob.Id);
        }

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
