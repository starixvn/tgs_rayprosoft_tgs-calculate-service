namespace Tgs.Calculate.Clients;

public interface ICalculateClient
{
	ValueTask<ExchangeResult> CreateCalculateAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask<ExchangeResult> DeleteCalculateAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask<ExchangeResult> ExchangeAsync(Guid calculateId, decimal change, CancellationToken cancellationToken = default);

	ValueTask<ExchangeResult> GetBalanceAsync(Guid calculateId, CancellationToken cancellationToken = default);
}
