using Tgs.Calculate;
using Tgs.SerialNumberGenerator;

namespace Tgs.CalculateWorker.ExchangeCommands;

internal class DeleteCommand(
	IFirstLevelRepository firstLevelRepository,
	ISerialNumberGenerator serialNumberGenerator)
	: BaseCommand
{
	public override async ValueTask<(decimal PostBalance, Guid SnapshotId, bool IsDelete)> ExchangeAsync(
		Guid calculateId,
		decimal balance,
		Guid snapshotId,
		CancellationToken cancellationToken = default)
	{
		var changeId = await serialNumberGenerator.GenerateAsync(
			cancellationToken).ConfigureAwait(false);

		var changeLog = new ExchangeLog(
			changeId,
			calculateId,
			0M,
			balance,
			DateTime.UtcNow,
			true);

		await firstLevelRepository.WriteExchangeLogAsync(
			changeLog,
			cancellationToken).ConfigureAwait(false);

		SetResult(new ExchangeResult(balance, changeLog.Id, true));
		return (changeLog.Balance, changeLog.Id, true);
	}
}
