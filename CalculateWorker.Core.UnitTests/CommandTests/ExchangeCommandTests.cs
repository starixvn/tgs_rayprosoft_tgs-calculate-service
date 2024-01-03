using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.ExchangeCommands;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;
using Xunit;

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
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new ExchangeCommand(
            request,
            fakeFirstLevelRepository,
            fakeSerialNumberGenerator,
            fakeMessageSender);

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

    [Fact]
    public async Task ExchangeCommand_計算餘額後會發出保存到DB的訊息()
    {
        // Arrange
        var request = new ExchangeCalculate(100M);
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new ExchangeCommand(
            request,
            fakeFirstLevelRepository,
            fakeSerialNumberGenerator,
            fakeMessageSender);

        var calculateId = Guid.NewGuid();

        var exchangeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(exchangeId);

        // Act
        _ = await sut.ExchangeAsync(calculateId, 0M, Guid.NewGuid());

        // Assert
        _ = fakeMessageSender.Received(1)
            .PublishAsync(
                Arg.Is(Subjects.CommitSubject),
                Arg.Is<CommitRequest>(request => request.CalculateId.ToGuid() == calculateId),
                Arg.Any<CancellationToken>());
    }
}
