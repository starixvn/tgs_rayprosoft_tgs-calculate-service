using Tgs.Calculate;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.ExchangeCommands;

namespace CalculateWorker.Core.UnitTests.CommandTests;

public class InitialCommandTests
{
    [Fact]
    public async Task InitialCommand_初始化時會先從FirstLevelRepository取得餘額()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        var calculateId = Guid.NewGuid();

        var exchangeLog = new ExchangeLog(
            Guid.NewGuid(),
            calculateId,
            0M,
            100M,
            DateTime.Now);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(Arg.Is(calculateId))
            .Returns(exchangeLog);

        // Act
        var (actualBalance, actualSnapshotId, _) = await sut.ExchangeAsync(calculateId, 0M, Guid.NewGuid());

        // Assert
        Assert.Equal(exchangeLog.Balance, actualBalance);
        Assert.Equal(exchangeLog.Id, actualSnapshotId);
        _ = fakeTgsRepository.DidNotReceive()
            .GetBalanceAsync(Arg.Is(calculateId));
    }

    [Fact]
    public async Task InitialCommand_初始化時從FirstLevelRepository取得最新餘額如果標記已刪除_回應會標記已刪除()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        var calculateId = Guid.NewGuid();

        var exchangeLog = new ExchangeLog(
            Guid.NewGuid(),
            calculateId,
            0M,
            100M,
            DateTime.Now,
            true);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(Arg.Is(calculateId))
            .Returns(exchangeLog);

        // Act
        var (_, _, actualIsDeleted) = await sut.ExchangeAsync(calculateId, 0M, Guid.NewGuid());

        // Assert
        Assert.True(actualIsDeleted);
    }

    [Fact]
    public async Task InitialCommand_初始化時從FirstLevelRepository取不到餘額的話會從TgsRepository取得餘額()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        var calculateId = Guid.NewGuid();

        var result = new ExchangeResult(100M, Guid.NewGuid());
        _ = fakeTgsRepository.GetBalanceAsync(Arg.Is(calculateId))
            .Returns(result);

        // Act
        var (actualBalance, actualSnapshotId, _) = await sut.ExchangeAsync(calculateId, 0M, Guid.Empty);

        // Assert
        Assert.Equal(result.Balance, actualBalance);
        Assert.Equal(result.SnapshotId, actualSnapshotId);
    }

    [Fact]
    public async Task InitialCommand_從TgsRepository取得餘額如果標記已刪除_回應會標記已刪除()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        var calculateId = Guid.NewGuid();

        var result = new ExchangeResult(100M, Guid.NewGuid(), true);
        _ = fakeTgsRepository.GetBalanceAsync(Arg.Is(calculateId))
            .Returns(result);

        // Act
        var (_, _, actualIsDeleted) = await sut.ExchangeAsync(calculateId, 0M, Guid.Empty);

        // Assert
        Assert.True(actualIsDeleted);
    }

    [Fact]
    public async Task InitialCommand_如果都取不到餘額會回應未初始化的餘額及SnapshotId()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        var calculateId = Guid.NewGuid();

        // Act
        var (actualBalance, actualSnapshotId, _) = await sut.ExchangeAsync(calculateId, 0M, Guid.NewGuid());

        // Assert
        Assert.Equal(0M, actualBalance);
        Assert.Equal(Guid.Empty, actualSnapshotId);
    }
}
