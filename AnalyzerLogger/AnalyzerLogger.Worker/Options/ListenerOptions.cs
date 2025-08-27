namespace AnalyzerLogger.Worker.Options;

public sealed class ListenerOptions
{
	public string IpAddress { get; init; } = "0.0.0.0";
	public int Port { get; init; } = 2575;
	public int Backlog { get; init; } = 100;
	public int ReceiveTimeoutSeconds { get; init; } = 120;
}

public sealed class ListenerOptionsValidator : Microsoft.Extensions.Options.IValidateOptions<ListenerOptions>
{
	public Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, ListenerOptions options)
	{
		if (options.Port <= 0 || options.Port > 65535)
		{
			return Microsoft.Extensions.Options.ValidateOptionsResult.Fail("Listener:Port must be 1-65535");
		}
		if (options.Backlog < 1)
		{
			return Microsoft.Extensions.Options.ValidateOptionsResult.Fail("Listener:Backlog must be >= 1");
		}
		if (options.ReceiveTimeoutSeconds < 1)
		{
			return Microsoft.Extensions.Options.ValidateOptionsResult.Fail("Listener:ReceiveTimeoutSeconds must be >= 1");
		}
		return Microsoft.Extensions.Options.ValidateOptionsResult.Success;
	}
}
