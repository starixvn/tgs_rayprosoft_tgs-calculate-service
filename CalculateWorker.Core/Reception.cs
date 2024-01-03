using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Tgs.CalculateWorker.ExchangeCommands;
using Valhalla.Awaitables;

namespace Tgs.CalculateWorker;

internal class Reception(
	Guid calculateId,
	TimeSpan expiry,
	TimeProvider timeProvider,
	ILogger<Reception> logger)
	: IDisposable
	, IAsyncDisposable
{
	private readonly Channel<IExchangeCommand> _exchangeChannel = Channel.CreateUnbounded<IExchangeCommand>(
		new UnboundedChannelOptions
		{
			SingleReader = true
		});

	private decimal _balance;
	private Guid _snapshotId = Guid.Empty;
	private bool _isDeleted = false;
	private CancellationTokenSource? _backgroundTask;
	private bool _disposedValue;

	public Guid CalculateId => calculateId;

	public bool IsStartReceived => _backgroundTask != null;

	public DateTime Expired { get; private set; } = timeProvider.GetUtcNow().UtcDateTime.Add(expiry);

	public async ValueTask StartReceiveAsync(InitialCommand initCommand, CancellationToken cancellationToken = default)
	{
		if (_backgroundTask is not null)
			return;

		if (!cancellationToken.IsCancellationRequested)
			(_balance, _snapshotId, _isDeleted) = await initCommand.ExchangeAsync(
				calculateId,
				_balance,
				_snapshotId,
				cancellationToken).ConfigureAwait(false);

		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_ = ProcessExchangeLoopAsync(cts.Token);
		_backgroundTask = cts;
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

	public async ValueTask EnqueueAsync(IExchangeCommand command, CancellationToken cancellationToken = default)
	{
		if (_isDeleted)
		{
			command.Fail(new ReceptionDeletedException());
			return;
		}

		if (_snapshotId == Guid.Empty && !(command is CreateCommand or ReadCommand))
		{
			command.Fail(new ReceptionNotCreatedException());
			return;
		}

		await _exchangeChannel.Writer.WriteAsync(
			command,
			cancellationToken).ConfigureAwait(false);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
			}

			if (_backgroundTask is not null && !_backgroundTask.IsCancellationRequested)
				_backgroundTask.Cancel();

			_backgroundTask?.Dispose();
			_backgroundTask = null;

			_disposedValue = true;
		}
	}

	protected virtual async ValueTask DisposeCoreAsync()
	{
		_exchangeChannel.Writer.TryComplete();

		if (_backgroundTask is not null && !_backgroundTask.IsCancellationRequested)
			_backgroundTask.Cancel();

		var allCommands = _exchangeChannel.Reader.ReadAllAsync();

		await foreach (var command in allCommands)
			command.Cancel();
	}

	private async Task ProcessExchangeLoopAsync(CancellationToken cancellationToken)
	{
		await TaskHelper.NewThread();

		try
		{
			var reader = _exchangeChannel.Reader;
			while (await reader.WaitToReadAsync(
				 cancellationToken).ConfigureAwait(false))
			{
				if (reader.TryRead(out var command))
					await HandleExchangeAsync(
						command,
						cancellationToken).ConfigureAwait(false);
			}
		}
		catch (TaskCanceledException)
		{
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Stop process exchange loop, because occur error.");
		}

		await cancellationToken.ConfigureAwait(false);

		await DisposeAsync().ConfigureAwait(false);
	}

	private async Task HandleExchangeAsync(IExchangeCommand command, CancellationToken cancellationToken)
	{
		try
		{
			if (cancellationToken.IsCancellationRequested)
			{
				command.Cancel();
				return;
			}

			Expired = timeProvider.GetUtcNow().UtcDateTime.Add(expiry);

			if (_snapshotId == Guid.Empty && !(command is CreateCommand or ReadCommand))
			{
				command.Fail(new ReceptionNotCreatedException());
				return;
			}

			if (_isDeleted)
			{
				command.Fail(new ReceptionDeletedException());
				return;
			}

			(_balance, _snapshotId, _isDeleted) = await command.ExchangeAsync(
				calculateId,
				_balance,
				_snapshotId,
				cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			command.Fail(ex);
			logger.LogError(ex, "Process exchange occur error.");
		}
	}
}
