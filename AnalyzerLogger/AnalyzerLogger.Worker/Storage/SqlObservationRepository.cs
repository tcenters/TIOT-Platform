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

		const string insertSql = @"INSERT INTO {table}
(
	TIMESTAMP, DATE_TIME_STAMP, DATA_SOURCE_NAME, DEVICE_ID,
	PATIENT_SEQ_NUM, PATIENT_ID, PATIENT_NAME, PATIENT_NAME_LAST, PATIENT_NAME_MIDDLE, PATIENT_NAME_FIRST, PATIENT_NAME_SEX,
	PATIENT_ADDRESS, PATIENT_PHONE, PATIENT_RACE,
	RESULT_SEQ_NUM, RESULT_TEST_ID, RESULT_TEST_ID1, RESULT_TEST_ID2, RESULT_TEST_ID3, RESULT_TEST_ID4,
	RESULT_VALUE, RESULT_UNIT, RESULT_ABNORMAL, RESULT_STATUS, RESULT_OPERATOR_ID
)
VALUES
(
	SYSUTCDATETIME(), @MessageDateTimeUtc, @DataSourceName, @DeviceId,
	@PatientSequenceNumber, @PatientId, @PatientName, @PatientNameLast, @PatientNameMiddle, @PatientNameFirst, @PatientSex,
	@PatientAddress, @PatientPhone, @PatientRace,
	@ResultSequenceNumber, @ObservationId, @ObservationId1, @ObservationId2, @ObservationId3, @ObservationId4,
	@Value, @Units, @AbnormalFlags, @ResultStatus, @ResultOperatorId
);";

		foreach (var obs in message.Observations)
		{
			await connection.ExecuteAsync(
				insertSql.Replace("{table}", _schema.Contains('.') ? _schema : _schema + ".TEST_DATA").Replace("{schema}", _schema),
				new
				{
					message.MessageDateTimeUtc,
					message.DataSourceName,
					message.DeviceId,
					message.PatientSequenceNumber,
					message.PatientId,
					message.PatientName,
					message.PatientNameLast,
					message.PatientNameMiddle,
					message.PatientNameFirst,
					message.PatientSex,
					message.PatientAddress,
					message.PatientPhone,
					message.PatientRace,
					ResultSequenceNumber = obs.ResultSequenceNumber,
					ObservationId = obs.ObservationId,
					ObservationId1 = obs.ObservationId1,
					ObservationId2 = obs.ObservationId2,
					ObservationId3 = obs.ObservationId3,
					ObservationId4 = obs.ObservationId4,
					obs.Value,
					Units = obs.Units,
					AbnormalFlags = obs.AbnormalFlags,
					ResultStatus = obs.ResultStatus,
					ResultOperatorId = obs.ResultOperatorId
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