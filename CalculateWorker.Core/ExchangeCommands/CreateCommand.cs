using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.ExchangeCommands;

internal class CreateCommand(
	IFirstLevelRepository firstLevelRepository,
	ISerialNumberGenerator serialNumberGenerator,
	IMessageSender messageSender)
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

		await messageSender.PublishAsync(
			Subjects.CommitSubject,
			new CommitRequest
			{
				CalculateId = calculateId
			},
			cancellationToken).ConfigureAwait(false);

		return (changeLog.Balance, changeLog.Id, false);
	}
}
