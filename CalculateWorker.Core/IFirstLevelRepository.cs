using Tgs.Calculate;

namespace Tgs.CalculateWorker;

public interface IFirstLevelRepository
{
	ValueTask<ExchangeLog?> GetNewestExchangeLogAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask WriteExchangeLogAsync(ExchangeLog exchangeLog, CancellationToken cancellationToken = default);
}
