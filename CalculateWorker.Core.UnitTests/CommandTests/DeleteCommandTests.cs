using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.ExchangeCommands;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;

namespace CalculateWorker.Core.UnitTests.CommandTests;

public class DeleteCommandTests
{
    [Fact]
    public async Task DeleteCommand_刪除計數()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new DeleteCommand(
            fakeFirstLevelRepository,
            fakeSerialNumberGenerator,
            fakeMessageSender);

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

    [Fact]
    public async Task DeleteCommand_刪除計數完成後會發出保存到DB的訊息()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new DeleteCommand(
            fakeFirstLevelRepository,
            fakeSerialNumberGenerator,
            fakeMessageSender);

        var calculateId = Guid.NewGuid();
        var balance = 100M;
        var snapshotId = Guid.NewGuid();

        var changeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync()
            .Returns(changeId);

        // Act
        _ = await sut.ExchangeAsync(calculateId, balance, snapshotId);

        // Assert
        _ = fakeMessageSender.Received(1)
            .PublishAsync(
                Arg.Is(Subjects.CommitSubject),
                Arg.Is<CommitRequest>(request => request.CalculateId.ToGuid() == calculateId),
                Arg.Any<CancellationToken>());
    }
}
