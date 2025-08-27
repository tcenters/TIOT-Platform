namespace AnalyzerLogger.Worker.Options;

public sealed class SqlOptions
{
	public string ConnectionString { get; init; } = string.Empty;
	public string Schema { get; init; } = "dbo";
}

public sealed class SqlOptionsValidator : Microsoft.Extensions.Options.IValidateOptions<SqlOptions>
{
	public Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, SqlOptions options)
	{
		if (string.IsNullOrWhiteSpace(options.ConnectionString))
		{
			return Microsoft.Extensions.Options.ValidateOptionsResult.Fail("Sql:ConnectionString is required");
		}
		return Microsoft.Extensions.Options.ValidateOptionsResult.Success;
	}
}
