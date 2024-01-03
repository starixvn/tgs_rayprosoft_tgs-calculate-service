using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.Awaitables;
using Valhalla.Messages;

namespace Tgs.CalculateCommitter;

internal class CommitDispatcher(
	IEventBus eventBus,
	IServiceProvider serviceProvider,
	ILogger<CommitDispatcher> logger)
	: BackgroundService
{
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var commitTask = Observable.Using(
			() => Disposable.Empty,
			res =>
			{
				var calculates = new ConcurrentDictionary<Guid, Guid>();

				return eventBus.GetEventObserable<ExchangeEvent>(CommitterConsts.ExchangeEventName)!
					.GroupByUntil(
						evt => evt.CalculateId,
						g =>
						{
							var waitSeconds = 300;
							var trigger = new TaskCompletionSource();

							return Observable.Using(
								() => g.Subscribe(_ => Interlocked.Exchange(ref waitSeconds, 300)),
								d => Observable.Using(
									() => new Timer(
										_ =>
										{
											_ = Interlocked.Decrement(ref waitSeconds);
											if (waitSeconds <= 0)
												trigger.SetResult();
										},
										null,
										0,
										1000),
									tm => Observable.FromAsync(() => trigger.Task)));
						})
					.Select(g => g
						.Select(evt => (Event: evt, Nonce: Guid.NewGuid()))
						.Where(tup => calculates.GetOrAdd(tup.Event.CalculateId, key => tup.Nonce) == tup.Nonce)
						.Select(tup => tup.Event)
						.Select(evt => Observable.FromAsync(async () =>
						{
							_ = calculates.TryRemove(evt.CalculateId, out _);

							await TaskHelper.NewThread();

							try
							{
								var scope = serviceProvider.CreateAsyncScope();
								await using var r = scope.ConfigureAwait(false);

								var recorder = ActivatorUtilities.CreateInstance<Recorder>(
									scope.ServiceProvider,
									evt.CalculateId,
									eventBus.GetEventPublisher<ExchangeEvent>(CommitterConsts.UpdateEventName)!)
									?? throw new NullReferenceException();

								await recorder.PersistentDataAsync(
									stoppingToken).ConfigureAwait(false);
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Commit {calculateId} event occur error!", evt.CalculateId);
							}
						}))
						.Concat())
					.Merge();
			})
			.ToTask(stoppingToken);

		var updateTask = Observable.Using(
			() => Disposable.Empty,
			res =>
			{
				var calculates = new ConcurrentDictionary<Guid, Guid>();

				return eventBus.GetEventObserable<ExchangeEvent>(CommitterConsts.UpdateEventName)!
					.GroupByUntil(
						evt => evt.CalculateId,
						g =>
						{
							var waitSeconds = 300;
							var trigger = new TaskCompletionSource();

							return Observable.Using(
								() => g.Subscribe(_ => Interlocked.Exchange(ref waitSeconds, 300)),
								d => Observable.Using(
									() => new Timer(
										_ =>
										{
											_ = Interlocked.Decrement(ref waitSeconds);
											if (waitSeconds <= 0)
												trigger.SetResult();
										},
										null,
										0,
										1000),
									tm => Observable.FromAsync(() => trigger.Task)));
						})
					.Select(g => g
						.Select(evt => (Event: evt, Nonce: Guid.NewGuid()))
						.Where(tup => calculates.GetOrAdd(tup.Event.CalculateId, key => tup.Nonce) == tup.Nonce)
						.Select(tup => tup.Event)
						.Select(evt => Observable.FromAsync(async () =>
						{
							_ = calculates.TryRemove(evt.CalculateId, out _);

							await TaskHelper.NewThread();

							try
							{
								var scope = serviceProvider.CreateAsyncScope();
								await using var r = scope.ConfigureAwait(false);

								var recorder = ActivatorUtilities.CreateInstance<Recorder>(
									scope.ServiceProvider,
									evt.CalculateId,
									eventBus.GetEventPublisher<ExchangeEvent>(CommitterConsts.UpdateEventName)!);

								await recorder.UpdateSnapshotAsync(
									stoppingToken).ConfigureAwait(false);
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Snapshot {calculateId} event occur error!", evt.CalculateId);
							}
						}))
						.Concat())
					.Merge();
			})
			.ToTask(stoppingToken);

		return Task.WhenAll(commitTask, updateTask);
	}
}
