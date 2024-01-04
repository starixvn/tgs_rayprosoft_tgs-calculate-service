using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.IntegrationTests;

internal class StubMessageHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
    public ValueTask HandleAsync(
        string subject,
        ReadOnlyMemory<byte> data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
