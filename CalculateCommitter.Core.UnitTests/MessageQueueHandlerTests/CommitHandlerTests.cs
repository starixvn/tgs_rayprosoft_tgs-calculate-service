using Tgs.Calculate.Protos;
using Tgs.CalculateCommitter;
using Tgs.CalculateCommitter.MessageQueue;
using Valhalla.Messages;

namespace CalculateCommitter.Core.UnitTests.MessageQueueHandlerTests;

public class CommitHandlerTests
{
    [Fact]
    public async Task CommitHandler_處理保存要求()
    {
        // Arrange
        var fakeEventBus = Substitute.For<IEventBus>();
        var fakeEventPublisher = Substitute.For<IEventPublisher<ExchangeEvent>>();

        _ = fakeEventBus.GetEventPublisher<ExchangeEvent>(Arg.Is(CommitterConsts.ExchangeEventName))
            .Returns(fakeEventPublisher);

        var sut = new CommitHandler(fakeEventBus);

        var calculateId = Guid.NewGuid();
        var request = new CommitRequest
        {
            CalculateId = calculateId
        };

        // Act
        await sut.HandleAsync("commit", request, null);

        // Assert
        _ = fakeEventPublisher.Received(1)
            .PublishAsync(Arg.Is(new ExchangeEvent(calculateId)), Arg.Any<CancellationToken>());
    }
}
