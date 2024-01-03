using System.Runtime.CompilerServices;

namespace Tgs.CalculateWorker.ExchangeCommands;

internal abstract class BaseCommand : IExchangeCommand
{
	private readonly TaskCompletionSource<ExchangeResult> _promise = new();

	public void Cancel()
		=> _promise.TrySetCanceled();

	public void Fail(Exception ex)
		=> _promise.TrySetException(ex);

	public TaskAwaiter<ExchangeResult> GetAwaiter()
		=> _promise.Task.GetAwaiter();

	public ConfiguredTaskAwaitable<ExchangeResult> ConfigureAwait(bool continueOnCapturedContext)
		=> _promise.Task.ConfigureAwait(continueOnCapturedContext);

	public abstract ValueTask<(decimal PostBalance, Guid SnapshotId, bool IsDelete)> ExchangeAsync(
		Guid calculateId,
		decimal balance,
		Guid snapshotId,
		CancellationToken cancellationToken = default);

	protected void SetResult(ExchangeResult result)
		=> _promise.SetResult(result);
}
