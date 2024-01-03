using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;
using Tgs.Calculate;
using Tgs.CalculateCommitter;
using Valhalla.Messages;

namespace CalculateCommitter.Core.UnitTests;

public class RecorderTests
{
    [Fact(Timeout = 1000)]
    public async Task Recorder_批次寫回在FirstLevelRepository的資料()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeUpdateEventPublisher = Substitute.For<IEventPublisher<ExchangeEvent>>();

        var sut = new Recorder(
            calculateId,
            fakeUpdateEventPublisher,
            fakeFirstLevelRepository,
            fakeTgsRepository,
            NullLogger<Recorder>.Instance);

        _ = fakeFirstLevelRepository.GetWaitingPersistentLogsAsync(Arg.Is(calculateId))
            .Returns(
                AsyncEnumerableEx.Return(
                    new ExchangeLog(
                        Guid.NewGuid(),
                        calculateId,
                        10M,
                        10M,
                        DateTime.UtcNow)),
                AsyncEnumerable.Empty<ExchangeLog>());

        // Act
        await sut.PersistentDataAsync();

        // Assert
        _ = fakeTgsRepository.Received(1)
            .InsertExchangeLogsAsync(Arg.Is<IEnumerable<ExchangeLog>>(logs => logs.Count() == 1));
        _ = fakeUpdateEventPublisher.Received(1)
            .PublishAsync(Arg.Is(new ExchangeEvent(calculateId)));
    }

    [Fact]
    public async Task Recorder_批次寫回時_如果發生錯誤會用Migrate再試一次()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeUpdateEventPublisher = Substitute.For<IEventPublisher<ExchangeEvent>>();

        var sut = new Recorder(
            calculateId,
            fakeUpdateEventPublisher,
            fakeFirstLevelRepository,
            fakeTgsRepository,
            NullLogger<Recorder>.Instance);

        _ = fakeFirstLevelRepository.GetWaitingPersistentLogsAsync(Arg.Is(calculateId))
            .Returns(
                AsyncEnumerableEx.Return(
                    new ExchangeLog(
                        Guid.NewGuid(),
                        calculateId,
                        10M,
                        10M,
                        DateTime.UtcNow)),
                AsyncEnumerable.Empty<ExchangeLog>());

        _ = fakeTgsRepository.InsertExchangeLogsAsync(Arg.Any<IEnumerable<ExchangeLog>>())
            .Throws<Exception>();

        // Act
        await sut.PersistentDataAsync();

        // Assert
        _ = fakeTgsRepository.Received(1)
            .MigrateExchangeLogsAsync(Arg.Is<IEnumerable<ExchangeLog>>(logs => logs.Count() == 1));
        _ = fakeUpdateEventPublisher.Received(1)
            .PublishAsync(Arg.Is(new ExchangeEvent(calculateId)));
    }

    [Fact]
    public async Task Recorder_執行更新資料庫快照資料()
    {
        // Arrange
        var calculateId = Guid.NewGuid();
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeUpdateEventPublisher = Substitute.For<IEventPublisher<ExchangeEvent>>();

        var sut = new Recorder(
            calculateId,
            fakeUpdateEventPublisher,
            fakeFirstLevelRepository,
            fakeTgsRepository,
            NullLogger<Recorder>.Instance);

        // Act
        await sut.UpdateSnapshotAsync();

        // Assert
        _ = fakeTgsRepository.Received(1)
            .UpdateSnapshotAsync(Arg.Is(calculateId));
    }
}
