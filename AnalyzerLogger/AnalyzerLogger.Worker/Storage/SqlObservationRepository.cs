using AnalyzerLogger.Worker.Options;
using AnalyzerLogger.Worker.Parsing.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AnalyzerLogger.Worker.Storage;

public sealed class SqlObservationRepository : IObservationRepository
{
	private readonly string _connectionString;
	private readonly string _schema;
	private readonly ILogger<SqlObservationRepository> _logger;

	public SqlObservationRepository(IOptions<SqlOptions> options, ILogger<SqlObservationRepository> logger)
	{
		_connectionString = options.Value.ConnectionString;
		_schema = options.Value.Schema;
		_logger = logger;
	}

	public async Task SaveAsync(ParsedObservationMessage message, CancellationToken ct)
	{
		await using var connection = new SqlConnection(_connectionString);
		await connection.OpenAsync(ct);

		await EnsureSchemaAsync(connection, ct);

		const string insertSql = @"INSERT INTO [{schema}].Observations
(
	MessageControlId, PatientId, OrderNumber, ObservationId, ObservationText, Value, Units, ReferenceRange, AbnormalFlags, ObservationDateTimeUtc, MessageDateTimeUtc, CreatedAtUtc
)
VALUES
(
	@MessageControlId, @PatientId, @OrderNumber, @ObservationId, @ObservationText, @Value, @Units, @ReferenceRange, @AbnormalFlags, @ObservationDateTimeUtc, @MessageDateTimeUtc, SYSUTCDATETIME()
);";

		foreach (var obs in message.Observations)
		{
			await connection.ExecuteAsync(
				insertSql.Replace("{schema}", _schema),
				new
				{
					message.MessageControlId,
					message.PatientId,
					message.OrderNumber,
					ObservationId = obs.ObservationId,
					ObservationText = obs.ObservationText,
					Value = obs.Value,
					Units = obs.Units,
					ReferenceRange = obs.ReferenceRange,
					AbnormalFlags = obs.AbnormalFlags,
					ObservationDateTimeUtc = obs.ObservationDateTimeUtc,
					MessageDateTimeUtc = message.MessageDateTimeUtc
				},
				commandTimeout: 30
			);
		}
	}

	private async Task EnsureSchemaAsync(SqlConnection connection, CancellationToken ct)
	{
		const string createSql = @"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @schema)
BEGIN
    EXEC('CREATE SCHEMA [' + @schema + ']')
END;

IF OBJECT_ID(QUOTENAME(@schema) + '.Observations', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE [' + @schema + '].Observations (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        MessageControlId NVARCHAR(64) NOT NULL,
        PatientId NVARCHAR(64) NULL,
        OrderNumber NVARCHAR(64) NULL,
        ObservationId NVARCHAR(64) NOT NULL,
        ObservationText NVARCHAR(256) NULL,
        Value NVARCHAR(256) NULL,
        Units NVARCHAR(64) NULL,
        ReferenceRange NVARCHAR(64) NULL,
        AbnormalFlags NVARCHAR(16) NULL,
        ObservationDateTimeUtc DATETIME2 NULL,
        MessageDateTimeUtc DATETIME2 NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    )')
END;";

		await connection.ExecuteAsync(createSql, new { schema = _schema });
		_logger.LogInformation("Ensured schema {schema}", _schema);
	}
}