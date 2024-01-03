using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.MessageQueue;

internal class DiscardCalculateHandler(IExchanger exchanger)
	: IMessageHandler<BidRequest>
{
	public ValueTask HandleAsync(
		string subject,
		BidRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
		=> exchanger.DiscardReceptionAsync(data.CalculateId);
}
