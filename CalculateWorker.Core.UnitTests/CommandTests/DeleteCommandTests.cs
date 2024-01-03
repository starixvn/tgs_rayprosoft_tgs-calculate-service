using Tgs.Calculate;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.ExchangeCommands;
using Tgs.SerialNumberGenerator;

namespace CalculateWorker.Core.UnitTests.CommandTests;

public class DeleteCommandTests
{
    [Fact]
    public async Task DeleteCommand_刪除計數()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var sut = new DeleteCommand(
            fakeFirstLevelRepository,
            fakeSerialNumberGenerator);

        var calculateId = Guid.NewGuid();
        var balance = 100M;
        var snapshotId = Guid.NewGuid();

        var changeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(changeId);

        // Act
        var (actualBalance, actualSnapshotId, _) = await sut.ExchangeAsync(calculateId, balance, snapshotId);

        // Assert
        Assert.Equal(balance, actualBalance);
        Assert.Equal(changeId, actualSnapshotId);
        _ = fakeFirstLevelRepository.Received(1)
            .WriteExchangeLogAsync(Arg.Is<ExchangeLog>(
                log => log.Id == changeId
                    && log.CalculateId == calculateId
                    && log.Deleted == true));
    }
}
