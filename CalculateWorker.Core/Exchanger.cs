using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tgs.CalculateWorker.ExchangeCommands;

namespace Tgs.CalculateWorker;

internal class Exchanger(
	IServiceProvider serviceProvider,
	TimeProvider timeProvider,
	IHostApplicationLifetime hostApplicationLifetime)
	: IDisposable
	, IAsyncDisposable
	, IExchanger
{
	private readonly ConcurrentDictionary<Guid, Reception> _exchangeReceptions = new();

	private readonly CancellationTokenSource _cancellationTokenSource =
		CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping);

	private bool _disposedValue;

	public async ValueTask<ExchangeResult> CreateAsync(Guid calculateId, CancellationToken cancellationToken = default)
	{
		var scope = serviceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

			var reception = await GetReceptionAsync(
				calculateId).ConfigureAwait(false);

			var command = ActivatorUtilities.CreateInstance<CreateCommand>(scope.ServiceProvider);
			await using var tokenRegistration = cts.Token.Register(
				command.Cancel).ConfigureAwait(false);
			await reception.EnqueueAsync(
				command,
				cts.Token).ConfigureAwait(false);

			return await command.ConfigureAwait(false);
		}
	}

	public async ValueTask<ExchangeResult> RequestAsync(Guid calculateId, ExchangeCalculate request, CancellationToken cancellationToken = default)
	{
		var scope = serviceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

			var reception = await GetReceptionAsync(
				calculateId).ConfigureAwait(false);

			var command = ActivatorUtilities.CreateInstance<ExchangeCommand>(scope.ServiceProvider, request);
			await using var tokenRegistration = cts.Token.Register(
				command.Cancel).ConfigureAwait(false);
			await reception.EnqueueAsync(
				command,
				cts.Token).ConfigureAwait(false);

			return await command.ConfigureAwait(false);
		}
	}

	public async ValueTask<ExchangeResult> DeleteAsync(Guid calculateId, CancellationToken cancellationToken = default)
	{
		var scope = serviceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

			var reception = await GetReceptionAsync(
				calculateId).ConfigureAwait(false);

			var command = ActivatorUtilities.CreateInstance<DeleteCommand>(scope.ServiceProvider);
			await using var tokenRegistration = cts.Token.Register(
				command.Cancel).ConfigureAwait(false);
			await reception.EnqueueAsync(
				command,
				cts.Token).ConfigureAwait(false);

			return await command.ConfigureAwait(false);
		}
	}

	public async ValueTask<ExchangeResult> GetBalanceAsync(Guid calculateId, CancellationToken cancellationToken = default)
	{
		var scope = serviceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

			var reception = await GetReceptionAsync(
				calculateId).ConfigureAwait(false);

			var command = ActivatorUtilities.CreateInstance<ReadCommand>(scope.ServiceProvider);
			await using var tokenRegistration = cts.Token.Register(
				command.Cancel).ConfigureAwait(false);
			await reception.EnqueueAsync(
				command,
				cts.Token).ConfigureAwait(false);

			return await command.ConfigureAwait(false);
		}
	}

	public async ValueTask DiscardReceptionAsync(Guid calculateId)
	{
		if (_exchangeReceptions.TryRemove(calculateId, out var reception))
			await reception.DisposeAsync().ConfigureAwait(false);
	}

	public async Task CleanExpiredReceptionsAsync(CancellationToken cancellationToken = default)
	{
		var currentTime = timeProvider.GetUtcNow().UtcDateTime;

		var expiredReceptions = _exchangeReceptions.Values
			.Where(reception => reception.Expired < currentTime)
			.ToArray();

		foreach (var reception in expiredReceptions)
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			if (_exchangeReceptions.TryRemove(reception.CalculateId, out var o))
			{
				await reception.DisposeAsync().ConfigureAwait(false);
				await o.DisposeAsync().ConfigureAwait(false);
			}
		}
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeCoreAsync().ConfigureAwait(false);

		Dispose(false);

		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			_cancellationTokenSource.Cancel();
			_cancellationTokenSource.Dispose();
			_exchangeReceptions.Clear();

			if (disposing)
			{
			}

			_disposedValue = true;
		}
	}

	protected async ValueTask DisposeCoreAsync()
	{
		var allValues = _exchangeReceptions.Values;
		_exchangeReceptions.Clear();

		foreach (var reception in allValues)
			await reception.DisposeAsync().ConfigureAwait(false);
	}

	private async ValueTask<Reception> GetReceptionAsync(Guid calculateId)
	{
		var reception = _exchangeReceptions.GetOrAdd(
			calculateId,
			_ => ActivatorUtilities.CreateInstance<Reception>(
				serviceProvider,
				calculateId,
				CalculateConsts.ReceptionExpiry));

		if (!reception.IsStartReceived)
		{
			var scope = serviceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				var initCommand = ActivatorUtilities.CreateInstance<InitialCommand>(scope.ServiceProvider);

				await reception.StartReceiveAsync(
					initCommand,
					_cancellationTokenSource.Token).ConfigureAwait(false);
			}
		}

		return reception;
	}
}
