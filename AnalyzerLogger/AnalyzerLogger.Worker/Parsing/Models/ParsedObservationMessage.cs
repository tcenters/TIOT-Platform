namespace AnalyzerLogger.Worker.Parsing.Models;

public sealed class ParsedObservationMessage
{
	public string MessageControlId { get; init; } = string.Empty;
	public string? PatientId { get; init; }
	public string? OrderNumber { get; init; }
	public DateTime MessageDateTimeUtc { get; init; }
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
}
