namespace Tgs.CalculateWorker.ExchangeCommands;

internal class InitialCommand(
	IFirstLevelRepository firstLevelRepository,
	ITgsRepository tgsRepository)
	: BaseCommand
{
	public override async ValueTask<(decimal PostBalance, Guid SnapshotId, bool IsDelete)> ExchangeAsync(
		Guid calculateId,
		decimal balance,
		Guid snapshotId,
		CancellationToken cancellationToken = default)
	{
		var newestLog = await firstLevelRepository.GetNewestExchangeLogAsync(
			calculateId,
			cancellationToken).ConfigureAwait(false);

		if (newestLog is not null)
		{
			SetResult(new ExchangeResult(newestLog.Balance, newestLog.Id, newestLog.Deleted));
			return (newestLog.Balance, newestLog.Id, newestLog.Deleted);
		}

		var dbResult = await tgsRepository.GetBalanceAsync(
			calculateId,
			cancellationToken).ConfigureAwait(false);

		if (dbResult is not null)
		{
			SetResult(dbResult);
			return (dbResult.Balance, dbResult.SnapshotId, dbResult.IsDeleted);
		}

		return (0M, Guid.Empty, false);
	}
}
