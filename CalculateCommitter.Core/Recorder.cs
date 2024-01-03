using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Tgs.Calculate;
using Valhalla.Messages;

namespace Tgs.CalculateCommitter;

internal class Recorder(
	Guid calculateId,
	IEventPublisher<ExchangeEvent> updateEventPublisher,
	IFirstLevelRepository firstLevelRepository,
	ITgsRepository tgsRepository,
	ILogger<Recorder> logger)
{
	public async ValueTask PersistentDataAsync(CancellationToken cancellationToken = default)
	{
		await foreach (var records in GetWaitingProcessBatchAsync(
			cancellationToken).ConfigureAwait(false))
		{
			var processCount = records.Length;

			if (processCount == 0)
				break;

			try
			{
				await tgsRepository.InsertExchangeLogsAsync(
				records,
					cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "{transcationLogs}", records);

				await tgsRepository.MigrateExchangeLogsAsync(
				records,
					cancellationToken).ConfigureAwait(false);
			}

			await firstLevelRepository.RemoveLogsAsync(
				calculateId,
				processCount,
				cancellationToken).ConfigureAwait(false);
		}

		_ = await updateEventPublisher.PublishAsync(
			new ExchangeEvent(calculateId),
			cancellationToken).ConfigureAwait(false);
	}

	public ValueTask UpdateSnapshotAsync(CancellationToken cancellationToken = default)
		=> tgsRepository.UpdateSnapshotAsync(calculateId, cancellationToken);

	private async IAsyncEnumerable<ExchangeLog[]> GetWaitingProcessBatchAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		while (true)
		{
			yield return await firstLevelRepository
				.GetWaitingPersistentLogsAsync(calculateId, cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
