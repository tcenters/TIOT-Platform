using AnalyzerLogger.Worker.Parsing.Models;

namespace AnalyzerLogger.Worker.Storage;

public interface IObservationRepository
{
	Task SaveAsync(ParsedObservationMessage message, CancellationToken ct);
}
