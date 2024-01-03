using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.MessageQueue;

internal class CreateCalculateProcessor(IExchanger exchanger)
	: IMessageProcessor<CreateRequest, ExchangeResponse>
{
	public async ValueTask<ExchangeResponse> HandleAsync(
		string subject,
		CreateRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var result = await exchanger.CreateAsync(
			data.CalculateId,
			cancellationToken).ConfigureAwait(false);

		return new ExchangeResponse
		{
			Balance = result.Balance,
			SnapshotId = result.SnapshotId
		};
	}
}
