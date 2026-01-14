using FasThinkQueueWorkerService.Options;
using Microsoft.Extensions.Options;
using System.ServiceProcess;

namespace FasThinkQueueWorkerService;

public sealed class ServiceRestarter
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(30);

    private readonly ILogger<ServiceRestarter> _logger;
    private readonly WatchOptions _options;

    public ServiceRestarter(ILogger<ServiceRestarter> logger, IOptions<WatchOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task<bool> TryRestartAsync(CancellationToken cancellationToken)
    {
        if (!CanRestart(out var lastRestartUtc))
        {
            _logger.LogInformation("Restart suppressed. Last restart was {LastRestartUtc}.", lastRestartUtc);
            return Task.FromResult(false);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_options.StateFilePath) ?? "C:\\Scripts");
        var now = DateTimeOffset.UtcNow;
        WriteState(now);

        try
        {
            using var controller = new ServiceController(_options.ServiceName);

            if (controller.Status != ServiceControllerStatus.Stopped &&
                controller.Status != ServiceControllerStatus.StopPending)
            {
                _logger.LogWarning("Stopping service {ServiceName}.", _options.ServiceName);
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, StopTimeout);
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogWarning("Starting service {ServiceName}.", _options.ServiceName);
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, StartTimeout);

            _logger.LogInformation("Service {ServiceName} restarted successfully.", _options.ServiceName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart service {ServiceName}.", _options.ServiceName);
            return Task.FromResult(false);
        }
    }

    private bool CanRestart(out DateTimeOffset? lastRestartUtc)
    {
        lastRestartUtc = null;

        if (!File.Exists(_options.StateFilePath))
        {
            return true;
        }

        var content = File.ReadAllText(_options.StateFilePath).Trim();
        if (!DateTimeOffset.TryParse(content, out var parsed))
        {
            return true;
        }

        lastRestartUtc = parsed;
        var cooldown = TimeSpan.FromMinutes(_options.CooldownMinutes);
        return DateTimeOffset.UtcNow - parsed >= cooldown;
    }

    private void WriteState(DateTimeOffset timestamp)
    {
        try
        {
            File.WriteAllText(_options.StateFilePath, timestamp.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write restart state file {StateFilePath}.", _options.StateFilePath);
        }
    }
}
