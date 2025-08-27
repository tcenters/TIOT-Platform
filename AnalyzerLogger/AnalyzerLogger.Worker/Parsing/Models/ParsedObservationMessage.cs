namespace AnalyzerLogger.Worker.Parsing.Models;

public sealed class ParsedObservationMessage
{
	public string MessageControlId { get; init; } = string.Empty;
	public string? PatientId { get; init; }
	public string? OrderNumber { get; init; }
	public DateTime MessageDateTimeUtc { get; init; }
	public string? DataSourceName { get; init; }
	public string? DeviceId { get; init; }
	public string? PatientSequenceNumber { get; init; }
	public string? PatientName { get; init; }
	public string? PatientNameLast { get; init; }
	public string? PatientNameMiddle { get; init; }
	public string? PatientNameFirst { get; init; }
	public string? PatientSex { get; init; }
	public string? PatientAddress { get; init; }
	public string? PatientPhone { get; init; }
	public string? PatientRace { get; init; }
	public IReadOnlyList<ObservationRecord> Observations { get; init; } = Array.Empty<ObservationRecord>();
}

public sealed class ObservationRecord
{
	public string ObservationId { get; init; } = string.Empty;
	public string? ObservationText { get; init; }
	public string? Value { get; init; }
	public string? Units { get; init; }
	public string? ReferenceRange { get; init; }
	public string? AbnormalFlags { get; init; }
	public DateTime? ObservationDateTimeUtc { get; init; }
	public string? ResultSequenceNumber { get; init; }
	public string? ResultStatus { get; init; }
	public string? ResultOperatorId { get; init; }
	public string? ObservationId1 { get; init; }
	public string? ObservationId2 { get; init; }
	public string? ObservationId3 { get; init; }
	public string? ObservationId4 { get; init; }
}
