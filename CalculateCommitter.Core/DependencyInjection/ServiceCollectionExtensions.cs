using Tgs.CalculateCommitter;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCalculateCommitter(this IServiceCollection services)
		=> services
			.AddEventBus(config => config
				.RegisterEventSource<ExchangeEvent>(CommitterConsts.ExchangeEventName)
				.RegisterEventSource<ExchangeEvent>(CommitterConsts.UpdateEventName))
			.AddHostedService<CommitDispatcher>();
}
