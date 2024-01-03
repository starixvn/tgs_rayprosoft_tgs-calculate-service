using Tgs.Calculate;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.ExchangeCommands;
using Tgs.SerialNumberGenerator;

namespace CalculateWorker.Core.UnitTests.CommandTests;

public class CreateCommandTests
{
    [Fact]
    public async Task CreateCommand_建立計數器()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var sut = new CreateCommand(fakeFirstLevelRepository, fakeSerialNumberGenerator);

        var calculateId = Guid.NewGuid();

        var changeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(changeId);

        // Act
        var (_, actualSnapshotId, _) = await sut.ExchangeAsync(calculateId, 0M, Guid.Empty);

        // Assert
        Assert.Equal(changeId, actualSnapshotId);
        _ = fakeFirstLevelRepository.Received(1)
            .WriteExchangeLogAsync(Arg.Is<ExchangeLog>(
                log => log.Id == changeId && log.CalculateId == calculateId));
    }

    [Fact]
    public async Task CreateCommand_建立計數器時Reception需要處在未初始化狀態()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var sut = new CreateCommand(fakeFirstLevelRepository, fakeSerialNumberGenerator);

        var calculateId = Guid.NewGuid();

        var changeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(changeId);

        // Act
        _ = await Assert.ThrowsAsync<ReceptionAlreadyCreatedException>(
            async () => await sut.ExchangeAsync(calculateId, 0M, Guid.NewGuid()));
    }
}
