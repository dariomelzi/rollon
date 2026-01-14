namespace FasThinkQueueWorkerService.Options;

public sealed class WorkerOptions
{
    public int WorkerMaxSeconds { get; set; } = 270;
    public int LoopSleepSeconds { get; set; } = 30;
    public int SleepOnEmptySeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
    public int OpDelaySeconds { get; set; } = 0;
    public int OpHoldSeconds { get; set; } = 0;
    public int InterItemDelaySeconds { get; set; } = 0;
    public int AlignPollMs { get; set; } = 200;
    public int AlignMaxWaitSeconds { get; set; } = 30;
    public bool AlignRequireMismatch { get; set; } = false;
    public int MaxCleanupRetries { get; set; } = 3;
}
