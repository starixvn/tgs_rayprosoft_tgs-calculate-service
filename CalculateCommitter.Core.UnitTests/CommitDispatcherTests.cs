using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tgs.CalculateCommitter;
using Valhalla.Messages;

namespace CalculateCommitter.Core.UnitTests;

public class CommitDispatcherTests
{
    [Fact(Timeout = 3000)]
    public async Task CommitDispatcher_接收到一個保存事件()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var serviceProvider = new ServiceCollection()
            .AddEventBus(config => config
                .RegisterEventSource<ExchangeEvent>(CommitterConsts.ExchangeEventName)
                .RegisterEventSource<ExchangeEvent>(CommitterConsts.UpdateEventName))
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton<ILogger<Recorder>>(NullLogger<Recorder>.Instance)
            .BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        using var sut = new CommitDispatcher(
            eventBus,
            serviceProvider,
            NullLogger<CommitDispatcher>.Instance);

        var publisher = eventBus.GetEventPublisher<ExchangeEvent>(CommitterConsts.ExchangeEventName)!;

        var exchangeEvent = new ExchangeEvent(Guid.NewGuid());

        var tcs = new TaskCompletionSource();
        fakeFirstLevelRepository.When(fake => fake.GetWaitingPersistentLogsAsync(
            Arg.Is(exchangeEvent.CalculateId),
            Arg.Any<CancellationToken>()))
            .Do(_ => tcs.SetResult());

        // Act
        await sut.StartAsync(CancellationToken.None);
        await publisher.PublishAsync(exchangeEvent);

        // Assert
        await tcs.Task;
    }

    [Fact]
    public async Task CommitDispatcher_接收到一個更新快照事件()
    {
        // Arrange
        var fakeFirstLevelRepository = Substitute.For<IFirstLevelRepository>();
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var serviceProvider = new ServiceCollection()
            .AddEventBus(config => config
                .RegisterEventSource<ExchangeEvent>(CommitterConsts.ExchangeEventName)
                .RegisterEventSource<ExchangeEvent>(CommitterConsts.UpdateEventName))
            .AddSingleton(fakeFirstLevelRepository)
            .AddSingleton(fakeTgsRepository)
            .AddSingleton<ILogger<Recorder>>(NullLogger<Recorder>.Instance)
            .BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        using var sut = new CommitDispatcher(
            eventBus,
            serviceProvider,
            NullLogger<CommitDispatcher>.Instance);

        var publisher = eventBus.GetEventPublisher<ExchangeEvent>(CommitterConsts.UpdateEventName)!;

        var exchangeEvent = new ExchangeEvent(Guid.NewGuid());

        var tcs = new TaskCompletionSource();
        fakeTgsRepository.When(fake => fake.UpdateSnapshotAsync(
            Arg.Is(exchangeEvent.CalculateId),
            Arg.Any<CancellationToken>()))
            .Do(_ => tcs.SetResult());

        // Act
        await sut.StartAsync(CancellationToken.None);
        await publisher.PublishAsync(exchangeEvent);

        // Assert
        await tcs.Task;
    }
}
