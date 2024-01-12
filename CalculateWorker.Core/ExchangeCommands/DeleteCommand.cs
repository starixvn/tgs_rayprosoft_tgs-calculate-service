using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.ExchangeCommands;

internal class DeleteCommand(
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

		await messageSender.PublishAsync(
			Subjects.CommitSubject,
			new CommitRequest
			{
				CalculateId = calculateId
			},
			cancellationToken).ConfigureAwait(false);

		SetResult(new ExchangeResult(balance, changeLog.Id, true));

		return (changeLog.Balance, changeLog.Id, true);
	}
}
