using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;
using Valhalla.Messages;

namespace Tgs.CalculateCommitter.MessageQueue;

internal class CommitHandler(IEventBus eventBus)
	: IMessageHandler<CommitRequest>
{
	public async ValueTask HandleAsync(
		string subject,
		CommitRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var eventPublisher = eventBus.GetEventPublisher<ExchangeEvent>(CommitterConsts.ExchangeEventName)!;

		var exchangeEvent = new ExchangeEvent(data.CalculateId);

		await eventPublisher.PublishAsync(
			exchangeEvent,
			cancellationToken).ConfigureAwait(false);
	}
}
