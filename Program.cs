using FasThinkQueueWorkerService;
using FasThinkQueueWorkerService.Options;
using Serilog;
using Serilog.Events;

Directory.CreateDirectory("C:\\Scripts");

var runId = Guid.NewGuid();
var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithProperty("RunId", runId)
    .Enrich.With(new UtcTimestampEnricher())
    .WriteTo.File(
        "C:\\Scripts\\FasThinkQueueWorker.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{UtcTimestamp} [{Level:u3}] ({RunId}) {Message:lj}{NewLine}{Exception}");

try
{
    loggerConfiguration = loggerConfiguration.WriteTo.EventLog(
        "FasThinkQueueWorkerService",
        manageEventSource: true,
        restrictedToMinimumLevel: LogEventLevel.Information);
}
catch
{
}

Log.Logger = loggerConfiguration.CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.Configure<SqlOptions>(builder.Configuration.GetSection("Sql"));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<WatchOptions>(builder.Configuration.GetSection("Watch"));

builder.Services.AddSingleton<SqlQueueClient>();
builder.Services.AddSingleton<ServiceRestarter>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<LogWatcher>();

builder.Host.UseSerilog();
builder.Host.UseWindowsService();

await builder.Build().RunAsync();
