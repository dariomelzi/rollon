using Serilog.Core;
using Serilog.Events;

namespace FasThinkQueueWorkerService;

public sealed class UtcTimestampEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff'Z'");
        var property = propertyFactory.CreateProperty("UtcTimestamp", timestamp);
        logEvent.AddPropertyIfAbsent(property);
    }
}
