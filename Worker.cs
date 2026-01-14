using System.Diagnostics;
using FasThinkQueueWorkerService.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FasThinkQueueWorkerService;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly SqlQueueClient _queueClient;
    private readonly WorkerOptions _options;

    public Worker(ILogger<Worker> logger, SqlQueueClient queueClient, IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _queueClient = queueClient;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue worker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var cycleStopwatch = Stopwatch.StartNew();
            var cycleDuration = TimeSpan.FromSeconds(_options.WorkerMaxSeconds);

            while (cycleStopwatch.Elapsed < cycleDuration && !stoppingToken.IsCancellationRequested)
            {
                SqlQueueResult result;

                try
                {
                    result = await _queueClient.ProcessPendingQueueAsync(stoppingToken);
                    _logger.LogInformation("Stored procedure result: {Esito} - {Messaggio}", result.Esito, result.Messaggio);
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "SQL exception while processing queue.");
                    await DelayAsync(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception while processing queue.");
                    await DelayAsync(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                await HandleResultAsync(result, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            _logger.LogInformation("Cycle complete. Sleeping for {LoopSleepSeconds} seconds.", _options.LoopSleepSeconds);
            await DelayAsync(TimeSpan.FromSeconds(_options.LoopSleepSeconds), stoppingToken);
        }

        _logger.LogInformation("Queue worker stopping.");
    }

    private async Task HandleResultAsync(SqlQueueResult result, CancellationToken cancellationToken)
    {
        var esito = result.Esito?.Trim() ?? string.Empty;

        if (esito.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Queue empty. Sleeping for {SleepOnEmptySeconds} seconds.", _options.SleepOnEmptySeconds);
            await DelayAsync(TimeSpan.FromSeconds(_options.SleepOnEmptySeconds), cancellationToken);
            return;
        }

        if (esito.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (esito.Equals("LOCK_TIMEOUT", StringComparison.OrdinalIgnoreCase) ||
            esito.Equals("DEADLOCK", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Transient lock issue: {Esito}. Backing off for 2 seconds.", esito);
            await DelayAsync(TimeSpan.FromSeconds(2), cancellationToken);
            return;
        }

        _logger.LogError("Stored procedure returned error or unknown status: {Esito}.", esito);
        await DelayAsync(TimeSpan.FromSeconds(5), cancellationToken);
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay, cancellationToken);
    }
}
