using Tgs.Calculate;
using Tgs.Calculate.Repositories;

namespace Tgs.CalculateCommitter.Repositories;

internal class RedisRepository(CommonRedisRepository commonRedisRepository)
	: IFirstLevelRepository
	, IDistributedResourceManager
{
	public IAsyncEnumerable<ExchangeLog> GetWaitingPersistentLogsAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> commonRedisRepository.GetWaitingPersistentExchangeLogsAsync(calculateId, 10, cancellationToken);

	public ValueTask RemoveLogsAsync(Guid calculateId, int length, CancellationToken cancellationToken = default)
		=> commonRedisRepository.RemoveExchangeLogsAsync(calculateId, length);

	public ValueTask<string> BidAsync(Guid calculateId, string resource, TimeSpan expiry, CancellationToken cancellationToken = default)
		=> commonRedisRepository.BidAsync(calculateId, "Committer", resource, expiry);

	public ValueTask<string?> GetBidResourceAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> commonRedisRepository.GetBidResourceAsync(calculateId, "Committer");

	public ValueTask DiscardBidAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> commonRedisRepository.DiscardBidAsync(calculateId, "Committer");

	public ValueTask ExtendBidAsync(Guid calculateId, TimeSpan expiry, CancellationToken cancellationToken = default)
		=> commonRedisRepository.ExtendBidAsync(calculateId, "Committer", expiry);
}
