namespace Tgs.CalculateWorker;

internal interface IExchanger
{
	Task CleanExpiredReceptionsAsync(CancellationToken cancellationToken = default);

	ValueTask<ExchangeResult> CreateAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask<ExchangeResult> DeleteAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask DiscardReceptionAsync(Guid calculateId);

	ValueTask<ExchangeResult> GetBalanceAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask<ExchangeResult> RequestAsync(Guid calculateId, ExchangeCalculate request, CancellationToken cancellationToken = default);
}
