namespace FasThinkQueueWorkerService.Options;

public sealed class WatchOptions
{
    public int IntervalSeconds { get; set; } = 60;
    public string LogFolder { get; set; } = "C:\\FasThinkConnect\\Service\\logs";
    public string ServiceName { get; set; } = "FasThinkConnect";
    public string StateFilePath { get; set; } = "C:\\Scripts\\FasThinkConnectRestart.state";
    public int CooldownMinutes { get; set; } = 10;
}
