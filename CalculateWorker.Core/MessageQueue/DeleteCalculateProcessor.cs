using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.MessageQueue;

internal class DeleteCalculateProcessor(IExchanger exchanger)
	: IMessageProcessor<DeleteRequest, ExchangeResponse>
{
	public async ValueTask<ExchangeResponse> HandleAsync(
		string subject,
		DeleteRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var result = await exchanger.DeleteAsync(
			data.CalculateId,
			cancellationToken).ConfigureAwait(false);

		return new ExchangeResponse
		{
			Balance = result.Balance,
			SnapshotId = result.SnapshotId
		};
	}
}
