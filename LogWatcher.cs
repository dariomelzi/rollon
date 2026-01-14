using FasThinkQueueWorkerService.Options;
using Microsoft.Extensions.Options;

namespace FasThinkQueueWorkerService;

public sealed class LogWatcher : BackgroundService
{
    private static readonly string[] ErrorSignatures =
    {
        "WorkerTickAsync exception: A task was canceled.",
        "RollonWorker worker tick exception: A task was canceled."
    };

    private readonly ILogger<LogWatcher> _logger;
    private readonly WatchOptions _options;
    private readonly ServiceRestarter _restarter;

    public LogWatcher(
        ILogger<LogWatcher> logger,
        IOptions<WatchOptions> options,
        ServiceRestarter restarter)
    {
        _logger = logger;
        _options = options.Value;
        _restarter = restarter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log watcher starting for {LogFolder}.", _options.LogFolder);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckLogsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log watcher error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
        }
    }

    private async Task CheckLogsAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_options.LogFolder))
        {
            _logger.LogWarning("Log folder does not exist: {LogFolder}", _options.LogFolder);
            return;
        }

        var files = Directory.GetFiles(_options.LogFolder, "FConnectService_log*");
        if (files.Length == 0)
        {
            _logger.LogInformation("No FasThinkConnect log files found.");
            return;
        }

        var latestFile = files
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .First();

        var lastLine = await ReadLastNonEmptyLineAsync(latestFile.FullName, cancellationToken);
        if (string.IsNullOrWhiteSpace(lastLine))
        {
            _logger.LogInformation("Latest log file {File} has no content.", latestFile.Name);
            return;
        }

        var message = StripPrefix(lastLine);
        if (!HasSignature(message))
        {
            return;
        }

        _logger.LogWarning("Detected FasThinkConnect cancellation signature: {Message}", message);
        await _restarter.TryRestartAsync(cancellationToken);
    }

    private static string StripPrefix(string line)
    {
        var pipeIndex = line.IndexOf('|');
        if (pipeIndex < 0)
        {
            return line.Trim();
        }

        return line[(pipeIndex + 1)..].Trim();
    }

    private static bool HasSignature(string message)
    {
        return ErrorSignatures.Any(signature =>
            message.Contains(signature, StringComparison.OrdinalIgnoreCase));
    }

    private static Task<string?> ReadLastNonEmptyLineAsync(string path, CancellationToken cancellationToken)
    {
        string? lastLine = null;

        foreach (var line in File.ReadLines(path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(line))
            {
                lastLine = line.Trim();
            }
        }

        return Task.FromResult(lastLine);
    }
}
