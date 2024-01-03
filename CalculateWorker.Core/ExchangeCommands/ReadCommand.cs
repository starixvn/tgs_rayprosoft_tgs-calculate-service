namespace Tgs.CalculateWorker.ExchangeCommands;

internal class ReadCommand : BaseCommand
{
	public override ValueTask<(decimal PostBalance, Guid SnapshotId, bool IsDelete)> ExchangeAsync(
		Guid calculateId,
		decimal balance,
		Guid snapshotId,
		CancellationToken cancellationToken = default)
	{
		SetResult(new ExchangeResult(balance, snapshotId));
		return ValueTask.FromResult((balance, snapshotId, false));
	}
}
