using AnalyzerLogger.Worker;
using AnalyzerLogger.Worker.Options;
using AnalyzerLogger.Worker.Services;
using Microsoft.Extensions.Options;
using Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
	.AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration)
	.CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
builder.Services.Configure<ListenerOptions>(builder.Configuration.GetSection("Listener"));
builder.Services.Configure<SqlOptions>(builder.Configuration.GetSection("Sql"));
builder.Services.AddSingleton<IValidateOptions<ListenerOptions>, ListenerOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<SqlOptions>, SqlOptionsValidator>();

builder.Services.AddAnalyzerLogger();

builder.Services.AddWindowsService(options =>
{
	options.ServiceName = builder.Configuration["WindowsService:ServiceName"] ?? "AnalyzerLogger";
});

// Optional: quick DB connectivity test
if (args.Contains("--testdb", StringComparer.OrdinalIgnoreCase))
{
	var sqlOptions = builder.Configuration.GetSection("Sql").Get<SqlOptions>() ?? new SqlOptions();
	try
	{
		using var conn = new SqlConnection(sqlOptions.ConnectionString);
		conn.Open();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT TOP (1) 1";
		var _ = cmd.ExecuteScalar();
		Console.WriteLine("Database connection OK: {0}", conn.DataSource);
		return;
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine("Database connection FAILED: {0}", ex.Message);
		Environment.ExitCode = 1;
		return;
	}
}

var host = builder.Build();
host.Run();
