using NATS.Client.Core;
using Polly;
using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateCommitter.MessageQueue;

public class CommitRequestDispatchHandler(
	IDistributedResourceManager distributedResourceManager,
	IMessageSender messageSender,
	ILogger<CommitRequestDispatchHandler> logger)
	: IAcknowledgeMessageHandler<ReadOnlyMemory<byte>>
{
	public async ValueTask HandleAsync(IAcknowledgeMessage<ReadOnlyMemory<byte>> msg, CancellationToken cancellationToken = default)
	{
		var request = BidRequest.Parser.ParseFrom(msg.Data.Span);

		await Policy
			.Handle<NatsNoRespondersException>()
			.RetryAsync(
				1,
				async (ex, count) =>
				{
					await distributedResourceManager.DiscardBidAsync(
						request.CalculateId,
						cancellationToken).ConfigureAwait(false);

					logger.LogWarning(ex, "NoReponders error retry {count}", count);
				})
			.ExecuteAsync(
				async ct =>
				{
					var resource = await GetResourceAsync(
						request,
						cancellationToken).ConfigureAwait(false);

					await messageSender.SendAsync(
						$"{msg.Subject}.{resource}",
						msg.Data,
						cancellationToken).ConfigureAwait(false);

					await distributedResourceManager.ExtendBidAsync(
						request.CalculateId,
						CommitterConsts.BidExpiry,
						cancellationToken).ConfigureAwait(false);
				},
				cancellationToken)
			.ConfigureAwait(false);

		await msg.AckAsync(cancellationToken).ConfigureAwait(false);
	}

	private async Task<string> GetResourceAsync(BidRequest request, CancellationToken cancellationToken)
	{
		var resource = await distributedResourceManager
			.GetBidResourceAsync(request.CalculateId, cancellationToken)
			.ConfigureAwait(false);

		if (string.IsNullOrEmpty(resource))
		{
			var bidResponse = await messageSender.RequestAsync<BidRequest, BidResponse>(
				Subjects.CommitterBidSubject,
				request,
				cancellationToken)
				.ConfigureAwait(false);

			resource = bidResponse.Resource;
		}

		return resource;
	}
}
