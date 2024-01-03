namespace Tgs.CalculateWorker;

public interface IDistributedResourceManager
{
	ValueTask<string> BidAsync(Guid calculateId, string resource, TimeSpan expiry, CancellationToken cancellationToken = default);

	ValueTask<string?> GetBidResourceAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask DiscardBidAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask ExtendBidAsync(Guid calculateId, TimeSpan expiry, CancellationToken cancellationToken = default);
}
