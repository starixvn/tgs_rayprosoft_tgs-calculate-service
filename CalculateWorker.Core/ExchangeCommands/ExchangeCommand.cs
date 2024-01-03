using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.SerialNumberGenerator;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.ExchangeCommands;

internal class ExchangeCommand(
	ExchangeCalculate request,
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
		var exchangeId = await serialNumberGenerator.GenerateAsync(
			cancellationToken).ConfigureAwait(false);

		var changeLog = new ExchangeLog(
			exchangeId,
			calculateId,
			request.Change,
			balance + request.Change,
			DateTime.UtcNow);

		await firstLevelRepository.WriteExchangeLogAsync(
			changeLog,
			cancellationToken).ConfigureAwait(false);

		SetResult(new ExchangeResult(changeLog.Balance, exchangeId));

		await messageSender.PublishAsync(
			Subjects.CommitSubject,
			new CommitRequest { CalculateId = calculateId },
			cancellationToken).ConfigureAwait(false);

		return (changeLog.Balance, exchangeId, false);
	}
}
