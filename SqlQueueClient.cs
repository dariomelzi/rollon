using FasThinkQueueWorkerService.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FasThinkQueueWorkerService;

public sealed class SqlQueueClient
{
    private readonly SqlOptions _sqlOptions;
    private readonly WorkerOptions _workerOptions;

    public SqlQueueClient(IOptions<SqlOptions> sqlOptions, IOptions<WorkerOptions> workerOptions)
    {
        _sqlOptions = sqlOptions.Value;
        _workerOptions = workerOptions.Value;
    }

    public async Task<SqlQueueResult> ProcessPendingQueueAsync(CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = _sqlOptions.Server,
            InitialCatalog = _sqlOptions.Database,
            UserID = _sqlOptions.User,
            Password = _sqlOptions.Password,
            TrustServerCertificate = true,
            Encrypt = true,
            ConnectTimeout = _sqlOptions.CommandTimeoutSeconds
        };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(_sqlOptions.ProcedureName, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = _sqlOptions.CommandTimeoutSeconds
        };

        command.Parameters.Add(new SqlParameter("BatchSize", System.Data.SqlDbType.Int) { Value = _workerOptions.BatchSize });
        command.Parameters.Add(new SqlParameter("OpDelaySeconds", System.Data.SqlDbType.Int) { Value = _workerOptions.OpDelaySeconds });
        command.Parameters.Add(new SqlParameter("OpHoldSeconds", System.Data.SqlDbType.Int) { Value = _workerOptions.OpHoldSeconds });
        command.Parameters.Add(new SqlParameter("InterItemDelaySeconds", System.Data.SqlDbType.Int) { Value = _workerOptions.InterItemDelaySeconds });
        command.Parameters.Add(new SqlParameter("AlignPollMs", System.Data.SqlDbType.Int) { Value = _workerOptions.AlignPollMs });
        command.Parameters.Add(new SqlParameter("AlignMaxWaitSeconds", System.Data.SqlDbType.Int) { Value = _workerOptions.AlignMaxWaitSeconds });
        command.Parameters.Add(new SqlParameter("AlignRequireMismatch", System.Data.SqlDbType.Bit) { Value = _workerOptions.AlignRequireMismatch });
        command.Parameters.Add(new SqlParameter("MaxCleanupRetries", System.Data.SqlDbType.Int) { Value = _workerOptions.MaxCleanupRetries });

        var esitoParam = new SqlParameter("Esito", System.Data.SqlDbType.NVarChar, 30)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        var messaggioParam = new SqlParameter("Messaggio", System.Data.SqlDbType.NVarChar, 4000)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        command.Parameters.Add(esitoParam);
        command.Parameters.Add(messaggioParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var esito = esitoParam.Value?.ToString() ?? string.Empty;
        var messaggio = messaggioParam.Value?.ToString() ?? string.Empty;

        return new SqlQueueResult(esito, messaggio);
    }
}

public sealed record SqlQueueResult(string Esito, string Messaggio);
