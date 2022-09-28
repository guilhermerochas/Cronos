using Cronos.Scheduling.Services;

using Hangfire;
using Hangfire.SQLite;

using Microsoft.Data.Sqlite;

const string connectionStringKey = "DefaultConnection";

var builder = WebApplication.CreateBuilder(args);

using (SqliteConnection connection = new SqliteConnection(builder.Configuration.GetConnectionString(connectionStringKey)))
{
    await connection.OpenAsync();
}

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(builder.Configuration.GetConnectionString(connectionStringKey))
);

builder.Services.AddHostedService<FileWatcherService>();
builder.Services.AddHangfireServer(options => options.WorkerCount = 2);

var app = builder.Build();

app.UseHangfireDashboard("/hangfire");

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHangfireDashboard();
});

await app.RunAsync();
