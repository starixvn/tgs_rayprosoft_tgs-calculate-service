using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Calculate;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.ExchangeCommands;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;
using Xunit;

namespace CalculateWorker.Core.UnitTests;

public class ReceptionTests
{
    [Fact]
    public async Task Reception_初始化後能夠取得錢包最新的餘額()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            NullLogger<Reception>.Instance);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                snapshotId,
                calculateId,
                0,
                1234M,
                DateTime.UtcNow));

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        // Act
        await sut.StartReceiveAsync(initCommand);
        var command = new ReadCommand();

        await sut.EnqueueAsync(command);

        var actual = await command;

        // Assert
        Assert.Equal(1234M, actual.Balance);
        Assert.Equal(snapshotId, actual.SnapshotId);
    }

    [Fact]
    public async Task Reception_初始化的計數如果是已經刪除的_不能執行命令()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            NullLogger<Reception>.Instance);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                snapshotId,
                calculateId,
                0,
                1234M,
                DateTime.UtcNow,
                true));

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        // Act
        await sut.StartReceiveAsync(initCommand);
        var command = new ReadCommand();

        await sut.EnqueueAsync(command);
        _ = await Assert.ThrowsAsync<ReceptionDeletedException>(async () => await command);
    }

    [Fact]
    public void Reception_建立物件後會設定Expired為建立時間加上expiry時間()
    {
        // Arrange
        var calculateId = Guid.NewGuid();

        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var currentTime = DateTimeOffset.UtcNow;
        _ = fakeTimeProvider.GetUtcNow()
            .Returns(currentTime);

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            fakeTimeProvider,
            NullLogger<Reception>.Instance);

        var expected = currentTime.AddMinutes(1D);

        // Assert
        Assert.Equal(expected, sut.Expired);
    }

    [Fact]
    public async Task Reception_執行命令會更新Expired()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var currentTime = DateTimeOffset.UtcNow;
        _ = fakeTimeProvider.GetUtcNow()
            .Returns(
                DateTimeOffset.MinValue,
                currentTime);

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            fakeTimeProvider,
            NullLogger<Reception>.Instance);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                snapshotId,
                calculateId,
                0,
                1234M,
                DateTime.UtcNow));

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        var expected = currentTime.AddMinutes(1D);

        // Act
        await sut.StartReceiveAsync(initCommand);
        var command = new ReadCommand();

        await sut.EnqueueAsync(command);

        _ = await command;

        // Assert
        Assert.Equal(expected, sut.Expired);
    }

    [Fact]
    public async Task Reception_當物件解構後_無法再執行Command()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var currentTime = DateTimeOffset.UtcNow;
        _ = fakeTimeProvider.GetUtcNow()
            .Returns(
                DateTimeOffset.MinValue,
                currentTime);

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            fakeTimeProvider,
            NullLogger<Reception>.Instance);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                snapshotId,
                calculateId,
                0,
                1234M,
                DateTime.UtcNow));

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        // Act
        await sut.StartReceiveAsync(initCommand);

        await sut.DisposeAsync();

        var command = new ReadCommand();

        _ = await Assert.ThrowsAsync<ChannelClosedException>(async () => await sut.EnqueueAsync(command));
    }

    [Fact]
    public async Task Reception_解構時在Channel中的Command會回應已取消()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var currentTime = DateTimeOffset.UtcNow;
        _ = fakeTimeProvider.GetUtcNow()
            .Returns(
                DateTimeOffset.MinValue,
                currentTime);

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            fakeTimeProvider,
            NullLogger<Reception>.Instance);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                snapshotId,
                calculateId,
                0,
                1234M,
                DateTime.UtcNow));

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        using var cts = new CancellationTokenSource();

        // Act
        var command = new ReadCommand();
        await sut.EnqueueAsync(command);

        await sut.DisposeAsync();
        await sut.StartReceiveAsync(initCommand, cts.Token);

        _ = await Assert.ThrowsAsync<TaskCanceledException>(async () => await command);
    }

    [Fact(Timeout = 1000)]
    public async Task Reception_接到取消後未執行的Command也會收到取消()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var currentTime = DateTimeOffset.UtcNow;
        _ = fakeTimeProvider.GetUtcNow()
            .Returns(
                DateTimeOffset.MinValue,
                currentTime);

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            fakeTimeProvider,
            NullLogger<Reception>.Instance);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                snapshotId,
                calculateId,
                0,
                1234M,
                DateTime.UtcNow));

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        using var cts = new CancellationTokenSource();

        // Act

        var command = new ReadCommand();
        await sut.EnqueueAsync(command);

        cts.Cancel();
        await sut.StartReceiveAsync(initCommand, cts.Token);
        //await sut.DisposeAsync();

        _ = await Assert.ThrowsAsync<TaskCanceledException>(async () => await command);
    }

    [Fact]
    public async Task Reception_如果計數未建立無法執行計數命令()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            NullLogger<Reception>.Instance);

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        // Act
        await sut.StartReceiveAsync(initCommand);
        var command = new ExchangeCommand(
            new ExchangeCalculate(100M),
            fakeFirstLevelRepository,
            Substitute.For<ISerialNumberGenerator>(),
            Substitute.For<IMessageSender>());

        await sut.EnqueueAsync(command);

        _ = await Assert.ThrowsAsync<ReceptionNotCreatedException>(async () => await command);
    }

    [Fact]
    public async Task Reception_如果計數已刪除無法執行計數命令()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            NullLogger<Reception>.Instance);

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(Arg.Is(calculateId), Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                0M,
                DateTime.UtcNow,
                true));

        // Act
        await sut.StartReceiveAsync(initCommand);
        var command = new ReadCommand();

        await sut.EnqueueAsync(command);

        _ = await Assert.ThrowsAsync<ReceptionDeletedException>(async () => await command);
    }

    [Fact]
    public async Task Reception_如果計數未建立也可以執行讀取命令()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new Reception(
            calculateId,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            NullLogger<Reception>.Instance);

        var initCommand = new InitialCommand(
            fakeFirstLevelRepository,
            fakeTgsRepository);

        // Act
        await sut.StartReceiveAsync(initCommand);
        var command = new ReadCommand();

        await sut.EnqueueAsync(command);

        var actual = await command;

        // Assert
        Assert.Equal(0M, actual.Balance);
        Assert.Equal(Guid.Empty, actual.SnapshotId);
    }
}
