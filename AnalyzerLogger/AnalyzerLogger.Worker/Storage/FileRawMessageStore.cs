using System.Text;

namespace AnalyzerLogger.Worker.Storage;

public sealed class FileRawMessageStore : IRawMessageStore
{
	private readonly string _directoryPath;
	private readonly ILogger<FileRawMessageStore> _logger;

	public FileRawMessageStore(ILogger<FileRawMessageStore> logger)
	{
		_logger = logger;
		_directoryPath = Path.Combine(AppContext.BaseDirectory, "raw");
		Directory.CreateDirectory(_directoryPath);
	}

	public async Task StoreAsync(string rawMessage, CancellationToken ct)
	{
		var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fffffff}.hl7";
		var fullPath = Path.Combine(_directoryPath, fileName);
		await File.WriteAllTextAsync(fullPath, rawMessage, Encoding.ASCII, ct);
		_logger.LogInformation("Stored raw message {file}", fullPath);
	}
}
