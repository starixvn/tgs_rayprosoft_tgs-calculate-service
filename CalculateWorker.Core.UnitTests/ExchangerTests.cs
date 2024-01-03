using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Calculate;
using Tgs.CalculateWorker;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;

namespace CalculateWorker.Core.UnitTests;

public class ExchangerTests
{
    [Fact]
    public async Task Exchanger_處理計算命令()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .AddSingleton(fakeMessageSender)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();
        var request = new ExchangeCalculate(100M);

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(Arg.Is(calculateId), Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                0M,
                DateTime.UtcNow));

        // Act
        var actual = await sut.RequestAsync(calculateId, request);

        // Assert
        Assert.Equal(100M, actual.Balance);
    }

    [Fact]
    public async Task Exchanger_處理讀取命令()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                100M,
                DateTime.UtcNow));

        // Act
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Equal(100M, actual.Balance);
    }

    [Fact]
    public async Task Exchanger_解構指定的Reception()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(
                new ExchangeLog(
                    Guid.NewGuid(),
                    calculateId,
                    0M,
                    100M,
                    DateTime.UtcNow),
                new ExchangeLog(
                    Guid.NewGuid(),
                    calculateId,
                    0M,
                    200M,
                    DateTime.UtcNow));

        // Act
        _ = await sut.GetBalanceAsync(calculateId);
        await sut.DiscardReceptionAsync(calculateId);
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Equal(200M, actual.Balance);
    }

    [Fact]
    public async Task Exchanger_清除過時的Receptions()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeTimeProvier = Substitute.For<TimeProvider>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(fakeTimeProvier)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(
                new ExchangeLog(
                    Guid.NewGuid(),
                    calculateId,
                    0M,
                    100M,
                    DateTime.UtcNow),
                new ExchangeLog(
                    Guid.NewGuid(),
                    calculateId,
                    0M,
                    200M,
                    DateTime.UtcNow));

        _ = fakeTimeProvier.GetUtcNow()
            .Returns(
                TimeProvider.System.GetUtcNow(),
                TimeProvider.System.GetUtcNow().AddDays(-1D));

        // Act
        _ = await sut.GetBalanceAsync(calculateId);
        await sut.CleanExpiredReceptionsAsync();
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.Equal(200M, actual.Balance);
    }

    [Fact]
    public async Task Exchanger_解構能正確完成()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                100M,
                DateTime.UtcNow));

        // Act
        _ = await sut.GetBalanceAsync(calculateId);
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task Exchanger_解構後執行計算命令會執行錯誤()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();
        var request = new ExchangeCalculate(100M);

        // Act
        await sut.DisposeAsync();

        _ = await Assert.ThrowsAnyAsync<ObjectDisposedException>(async () => await sut.RequestAsync(calculateId, request));
    }

    [Fact]
    public async Task Exchanger_取消後計算命令會回應取消()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .AddSingleton(fakeMessageSender)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        using var cts = new CancellationTokenSource();
        _ = fakeHostApplicationLifetime.ApplicationStopping
            .Returns(cts.Token);

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();
        var request = new ExchangeCalculate(100M);

        // Act
        cts.Cancel();

        _ = await Assert.ThrowsAnyAsync<TaskCanceledException>(async () => await sut.RequestAsync(calculateId, request));
    }

    [Fact]
    public async Task Exchanger_解構後執行讀取命令會執行錯誤()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        // Act
        await sut.DisposeAsync();

        _ = await Assert.ThrowsAnyAsync<ObjectDisposedException>(async () => await sut.GetBalanceAsync(calculateId));
    }

    [Fact]
    public async Task Exchanger_取消後讀取命令會回應取消()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        using var cts = new CancellationTokenSource();
        _ = fakeHostApplicationLifetime.ApplicationStopping
            .Returns(cts.Token);

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        // Act
        cts.Cancel();

        _ = await Assert.ThrowsAnyAsync<TaskCanceledException>(async () => await sut.GetBalanceAsync(calculateId));
    }

    [Fact]
    public async Task Exchaner_建立計數器()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .AddSingleton(fakeMessageSender)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        var changeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
            .Returns(changeId);

        // Act
        var actual = await sut.CreateAsync(calculateId);

        // Assert
        Assert.Equal(changeId, actual.SnapshotId);
    }

    [Fact]
    public async Task Exchanger_刪除計數器()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ILogger<Reception>>(NullLogger<Reception>.Instance)
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton(fakeSerialNumberGenerator)
            .AddSingleton(fakeMessageSender)
            .BuildServiceProvider();
        var fakeHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        var sut = new Exchanger(
            serviceProvider,
            TimeProvider.System,
            fakeHostApplicationLifetime);

        var calculateId = Guid.NewGuid();

        _ = fakeFirstLevelRepository.GetNewestExchangeLogAsync(
            Arg.Is(calculateId),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                100M,
                DateTime.UtcNow));

        var changeId = Guid.NewGuid();
        _ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
            .Returns(changeId);

        // Act
        var actual = await sut.DeleteAsync(calculateId);

        // Assert
        Assert.Equal(changeId, actual.SnapshotId);
        Assert.True(actual.IsDeleted);

        _ = fakeFirstLevelRepository.Received(1)
            .WriteExchangeLogAsync(
                Arg.Is<ExchangeLog>(log => log.Id == changeId
                    && log.CalculateId == calculateId
                    && log.Deleted == true),
                Arg.Any<CancellationToken>());
    }
}
