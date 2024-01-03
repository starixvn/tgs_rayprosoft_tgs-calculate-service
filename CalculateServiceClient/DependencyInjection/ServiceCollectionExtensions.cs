using Tgs.Calculate.Clients;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCalculateClient(
		this IServiceCollection services,
		Action<IHttpClientBuilder> configureHttpClient)
	{
		configureHttpClient(services.AddHttpClient<ICalculateClient, CalculateClient>());

		return services;
	}
}
