namespace AnalyzerLogger.Worker.Options;

public sealed class SqlOptions
{
	public string ConnectionString { get; init; } = string.Empty;
	public string Schema { get; init; } = "dbo";
	public string TableName { get; init; } = "dbo.TEST_DATA";
}

public sealed class SqlOptionsValidator : Microsoft.Extensions.Options.IValidateOptions<SqlOptions>
{
	public Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, SqlOptions options)
	{
		if (string.IsNullOrWhiteSpace(options.ConnectionString))
		{
			return Microsoft.Extensions.Options.ValidateOptionsResult.Fail("Sql:ConnectionString is required");
		}
		if (string.IsNullOrWhiteSpace(options.TableName))
		{
			return Microsoft.Extensions.Options.ValidateOptionsResult.Fail("Sql:TableName is required");
		}
		return Microsoft.Extensions.Options.ValidateOptionsResult.Success;
	}
}
