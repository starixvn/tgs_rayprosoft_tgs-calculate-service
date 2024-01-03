using Tgs.Calculate.Protos;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.MessageQueue;

namespace CalculateWorker.Core.UnitTests.MessageQueueHandlerTests;

public class ExchangeCalculateProcessorTests
{
    [Fact]
    public async Task ExchangeCalculateProcessor_處理計算要求()
    {
        // Arrange
        var fakeExchanger = Substitute.For<IExchanger>();

        var sut = new ExchangeCalculateProcessor(fakeExchanger);

        var calculateId = Guid.NewGuid();
        var reqeust = new ExchangeRequest
        {
            CalculateId = calculateId,
            Change = 100M
        };

        var expectedRequest = new ExchangeCalculate(100M);
        _ = fakeExchanger.RequestAsync(Arg.Is(calculateId), Arg.Is(expectedRequest))
            .Returns(new ExchangeResult(100M, Guid.NewGuid()));

        // Act
        await sut.HandleAsync("exchange", reqeust, null);

        // Assert
        _ = fakeExchanger.Received(1)
            .RequestAsync(Arg.Is(calculateId), Arg.Is(expectedRequest));
    }
}
