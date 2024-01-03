using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateCommitter.MessageQueue;

internal class BidProcessor(IDistributedResourceManager distributedResourceManager)
	: IMessageProcessor<BidRequest, BidResponse>
{
	public async ValueTask<BidResponse> HandleAsync(
		string subject,
		BidRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var resource = await distributedResourceManager.BidAsync(
			data.CalculateId,
			Environment.MachineName,
			CommitterConsts.BidExpiry,
			cancellationToken).ConfigureAwait(false);

		return new BidResponse
		{
			Resource = resource
		};
	}
}
