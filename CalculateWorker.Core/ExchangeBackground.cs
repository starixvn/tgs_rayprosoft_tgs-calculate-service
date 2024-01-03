using Microsoft.Extensions.Hosting;

namespace Tgs.CalculateWorker;

internal class ExchangeBackground(IExchanger exchanger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await exchanger.CleanExpiredReceptionsAsync(
				stoppingToken).ConfigureAwait(false);

			await Task.Delay(
				CalculateConsts.CleanInterval,
				stoppingToken).ConfigureAwait(false);
		}
	}
}
