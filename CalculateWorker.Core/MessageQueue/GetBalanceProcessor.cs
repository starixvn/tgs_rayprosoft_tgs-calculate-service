using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.MessageQueue;

internal class GetBalanceProcessor(IExchanger exchanger)
	: IMessageProcessor<GetBalanceRequest, ExchangeResponse>
{
	public async ValueTask<ExchangeResponse> HandleAsync(
		string subject,
		GetBalanceRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var result = await exchanger.GetBalanceAsync(
			data.CalculateId,
			cancellationToken).ConfigureAwait(false);

		return new ExchangeResponse
		{
			Balance = result.Balance,
			SnapshotId = result.SnapshotId
		};
	}
}
