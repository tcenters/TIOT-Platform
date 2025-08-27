using AnalyzerLogger.Worker.Parsing;
using AnalyzerLogger.Worker.Services;
using AnalyzerLogger.Worker.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AnalyzerLogger.Worker;

public static class DependencyInjection
{
	public static IServiceCollection AddAnalyzerLogger(this IServiceCollection services)
	{
		services.AddSingleton<IRawMessageStore, FileRawMessageStore>();
		services.AddSingleton<IObservationRepository, SqlObservationRepository>();
		services.AddSingleton<IHl7Parser, NhapiHl7Parser>();
		services.AddHostedService<MllpTcpServerService>();
		return services;
	}
}
