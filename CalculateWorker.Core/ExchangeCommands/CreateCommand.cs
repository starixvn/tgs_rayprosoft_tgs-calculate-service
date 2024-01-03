using Tgs.Calculate;
using Tgs.SerialNumberGenerator;

namespace Tgs.CalculateWorker.ExchangeCommands;

internal class CreateCommand(
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
		if (snapshotId != Guid.Empty)
			throw new ReceptionAlreadyCreatedException();

		var changeId = await serialNumberGenerator.GenerateAsync(
			cancellationToken).ConfigureAwait(false);

		var changeLog = new ExchangeLog(
			changeId,
			calculateId,
			0M,
			0M,
			DateTime.UtcNow);

		await firstLevelRepository.WriteExchangeLogAsync(
			changeLog,
			cancellationToken).ConfigureAwait(false);

		SetResult(new ExchangeResult(changeLog.Balance, changeLog.Id));
		return (changeLog.Balance, changeLog.Id, false);
	}
}
