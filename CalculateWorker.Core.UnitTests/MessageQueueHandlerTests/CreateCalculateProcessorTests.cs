using Tgs.Calculate.Protos;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.MessageQueue;

namespace CalculateWorker.Core.UnitTests.MessageQueueHandlerTests;

public class CreateCalculateProcessorTests
{
    [Fact]
    public async Task CreateCalculateProcessor_處理建立計算器()
    {
        // Arrange
        var fakeExchanger = Substitute.For<IExchanger>();

        var sut = new CreateCalculateProcessor(fakeExchanger);

        var calculateId = Guid.NewGuid();
        var request = new CreateRequest
        {
            CalculateId = calculateId
        };

        _ = fakeExchanger.CreateAsync(Arg.Is(calculateId))
            .Returns(new ExchangeResult(100M, Guid.NewGuid()));

        // Act
        _ = await sut.HandleAsync("create", request, null);

        // Assert
        _ = fakeExchanger.Received(1)
            .CreateAsync(Arg.Is(calculateId));
    }
}
