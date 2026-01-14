namespace FasThinkQueueWorkerService.Options;

public sealed class SqlOptions
{
    public string Server { get; set; } = "HQ-ETIP01\\DIGLABLE";
    public string Database { get; set; } = "FasThinkConnect";
    public string User { get; set; } = "fasthink";
    public string Password { get; set; } = "FasThink@2024";
    public string ProcedureName { get; set; } = "dbo.sp_ProcessPendingQueue";
    public int CommandTimeoutSeconds { get; set; } = 60;
}
