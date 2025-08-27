using AnalyzerLogger.Worker;
using AnalyzerLogger.Worker.Options;
using AnalyzerLogger.Worker.Services;
using Microsoft.Extensions.Options;
using Serilog;
using Microsoft.Extensions.Logging;

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

var host = builder.Build();
host.Run();
