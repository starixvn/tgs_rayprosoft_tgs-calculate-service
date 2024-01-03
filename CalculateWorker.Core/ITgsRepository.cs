namespace Tgs.CalculateWorker;

public interface ITgsRepository
{
	ValueTask<ExchangeResult?> GetBalanceAsync(Guid calculateId, CancellationToken cancellationToken = default);
}
