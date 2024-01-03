using Tgs.CalculateWorker;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCalculateWorkerCore(this IServiceCollection services)
		=> services
			.AddSingleton<IExchanger, Exchanger>()
			.AddSerialNumberGenerator()
			.AddHostedService<ExchangeBackground>();
}
