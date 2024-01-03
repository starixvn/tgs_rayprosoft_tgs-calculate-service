using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.MessageQueue;

internal class ExchangeCalculateProcessor(IExchanger exchanger)
	: IMessageProcessor<ExchangeRequest, ExchangeResponse>
{
	public async ValueTask<ExchangeResponse> HandleAsync(
		string subject,
		ExchangeRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var result = await exchanger.RequestAsync(
			data.CalculateId,
			new ExchangeCalculate(data.Change),
			cancellationToken).ConfigureAwait(false);

		return new ExchangeResponse
		{
			Balance = result.Balance,
			SnapshotId = result.SnapshotId
		};
	}
}
