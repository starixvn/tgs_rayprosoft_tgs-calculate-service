using NATS.Client.Core;
using Polly;
using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.MessageQueue;

public class ExchangeRequestDispatchProcessor(
	IDistributedResourceManager distributedResourceManager,
	IMessageSender messageSender,
	ILogger<ExchangeRequestDispatchProcessor> logger)
	: IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = BidRequest.Parser.ParseFrom(data.Span);

		return await Policy
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

					var response = await messageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
						$"{subject}.{resource}",
						data,
						cancellationToken).ConfigureAwait(false);

					await distributedResourceManager.ExtendBidAsync(
						request.CalculateId,
						CalculateConsts.BidExpiry,
						cancellationToken).ConfigureAwait(false);

					return response;
				},
				cancellationToken)
			.ConfigureAwait(false);
	}

	private async Task<string> GetResourceAsync(BidRequest request, CancellationToken cancellationToken)
	{
		var resource = await distributedResourceManager
			.GetBidResourceAsync(request.CalculateId, cancellationToken)
			.ConfigureAwait(false);

		if (string.IsNullOrEmpty(resource))
		{
			var bidResponse = await messageSender.RequestAsync<BidRequest, BidResponse>(
				Subjects.WorkerBidSubject,
				request,
				cancellationToken)
				.ConfigureAwait(false);

			resource = bidResponse.Resource;
		}

		return resource;
	}
}
