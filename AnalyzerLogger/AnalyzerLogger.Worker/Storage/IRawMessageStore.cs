namespace AnalyzerLogger.Worker.Storage;

public interface IRawMessageStore
{
	Task StoreAsync(string rawMessage, CancellationToken ct);
}
