using Tgs.CalculateWorker.ExchangeCommands;

namespace CalculateWorker.Core.UnitTests.CommandTests;

public class ReadCommandTests
{
    [Fact]
    public async Task ReadCommand_取得目前餘額()
    {
        // Arrange
        var sut = new ReadCommand();

        var calculateId = Guid.NewGuid();
        var balance = 100M;
        var snapshotId = Guid.NewGuid();

        // Act
        var (actualBalance, actualSnapshotId, _) = await sut.ExchangeAsync(calculateId, balance, snapshotId);

        // Assert
        Assert.Equal(balance, actualBalance);
        Assert.Equal(snapshotId, actualSnapshotId);
    }
}
