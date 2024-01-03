using Tgs.Calculate;
using Tgs.Calculate.Repositories;

namespace Tgs.CalculateWorker.Repositories;

internal class RedisRepository(CommonRedisRepository commonRedisRepository)
	: IFirstLevelRepository
	, IDistributedResourceManager
{
	public ValueTask<ExchangeLog?> GetNewestExchangeLogAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> commonRedisRepository.GetNewestExchangeLogAsync(calculateId);

	public ValueTask WriteExchangeLogAsync(ExchangeLog exchangeLog, CancellationToken cancellationToken = default)
		=> commonRedisRepository.WriteExchangeLogAsync(exchangeLog);

	public ValueTask<string> BidAsync(
		Guid calculateId,
		string resource,
		TimeSpan expiry,
		CancellationToken cancellationToken = default)
		=> commonRedisRepository.BidAsync(calculateId, "Workers", resource, expiry);

	public ValueTask<string?> GetBidResourceAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> commonRedisRepository.GetBidResourceAsync(calculateId, "Workers");

	public ValueTask DiscardBidAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> commonRedisRepository.DiscardBidAsync(calculateId, "Workers");

	public ValueTask ExtendBidAsync(Guid calculateId, TimeSpan expiry, CancellationToken cancellationToken = default)
		=> commonRedisRepository.ExtendBidAsync(calculateId, "Workers", expiry);
}
