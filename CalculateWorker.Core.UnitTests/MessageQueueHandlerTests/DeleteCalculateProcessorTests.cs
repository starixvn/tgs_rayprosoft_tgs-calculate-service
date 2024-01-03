using Tgs.Calculate.Protos;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.MessageQueue;

namespace CalculateWorker.Core.UnitTests.MessageQueueHandlerTests;

public class DeleteCalculateProcessorTests
{
    [Fact]
    public async Task DeleteCalculateProcessor_處理刪除計算器()
    {
        // Arrange
        var fakeExchanger = Substitute.For<IExchanger>();

        var sut = new DeleteCalculateProcessor(fakeExchanger);

        var calculateId = Guid.NewGuid();
        var request = new DeleteRequest
        {
            CalculateId = calculateId
        };

        _ = fakeExchanger.DeleteAsync(Arg.Is(calculateId))
            .Returns(new ExchangeResult(100M, Guid.NewGuid(), true));

        // Act
        _ = await sut.HandleAsync("delete", request, null);

        // Assert
        _ = fakeExchanger.Received(1)
            .DeleteAsync(Arg.Is(calculateId));
    }
}
