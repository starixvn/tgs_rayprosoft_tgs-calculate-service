using Tgs.Calculate;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.ExchangeCommands;
using Tgs.SerialNumberGenerator;

namespace CalculateWorker.Core.UnitTests.CommandTests;

public class ExchangeCommandTests
{
    [Fact]
    public async Task ExchangeCommand_依據要求計算餘額()
    {
        // Arrange
        var request = new ExchangeCalculate(100M);
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var sut = new ExchangeCommand(request, fakeFirstLevelRepository, fakeSerialNumberGenerator);

        var calculateId = Guid.NewGuid();

        var exchangeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(exchangeId);

        // Act
        var (actualBalance, actualSnapshotId, _) = await sut.ExchangeAsync(calculateId, 0M, Guid.NewGuid());

        // Assert
        Assert.Equal(100M, actualBalance);
        Assert.Equal(exchangeId, actualSnapshotId);

        _ = fakeFirstLevelRepository.Received(1)
            .WriteExchangeLogAsync(Arg.Is<ExchangeLog>(
                log => log.Id == exchangeId
                    && log.CalculateId == calculateId
                    && log.Change == request.Change
                    && log.Balance == 100M));
    }
}
