using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.MessageQueue;

internal class BidProcessor(
	IDistributedResourceManager distributedResourceManager,
	IExchanger exchanger)
	: IMessageProcessor<BidRequest, BidResponse>
{
	public async ValueTask<BidResponse> HandleAsync(
		string subject,
		BidRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		await exchanger.DiscardReceptionAsync(
			data.CalculateId).ConfigureAwait(false);

		var resource = await distributedResourceManager.BidAsync(
			data.CalculateId,
			Environment.MachineName,
			CalculateConsts.BidExpiry,
			cancellationToken).ConfigureAwait(false);

		return new BidResponse
		{
			Resource = resource
		};
	}
}
