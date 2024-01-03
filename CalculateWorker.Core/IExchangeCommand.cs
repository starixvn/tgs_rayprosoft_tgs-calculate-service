using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tgs.CalculateWorker;

internal interface IExchangeCommand
{
	void Cancel();

	void Fail(Exception ex);

	[EditorBrowsable(EditorBrowsableState.Never)]
	TaskAwaiter<ExchangeResult> GetAwaiter();

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "AMNF0001:Awaitable (Asynchronous) methods should be suffixed with 'Async'", Justification = "<暫止>")]
	ConfiguredTaskAwaitable<ExchangeResult> ConfigureAwait(bool continueOnCapturedContext);

	ValueTask<(decimal PostBalance, Guid SnapshotId, bool IsDelete)> ExchangeAsync(
		Guid calculateId,
		decimal balance,
		Guid snapshotId,
		CancellationToken cancellationToken = default);
}
