using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tgs.Calculate.Repositories.Entities;

namespace Tgs.Calculate.Repositories;

internal class CommonTgsRepository(TgsContext dbContext, ILogger<CommonTgsRepository> logger)
{
	public async ValueTask<(decimal Balance, Guid SnapshotId)?> GetBalanceAsync(Guid calculateId, CancellationToken cancellationToken = default)
	{
		var snapshotRow = await dbContext.Calculates.FindAsync(
			[calculateId],
			cancellationToken: cancellationToken).ConfigureAwait(false);

		var query = dbContext.CalculateChangeLogs
			.Where(log => log.CalculateId == calculateId);

		if (snapshotRow is not null)
			query = query.Where(log => log.Id > snapshotRow.SnapshotId);

		query = query.OrderByDescending(log => log.Id);

		var newestLog = await query.FirstOrDefaultAsync(
			cancellationToken).ConfigureAwait(false);

		return newestLog.Transform(log => (log.Balance, log.Id))
			?? snapshotRow.Transform(snapshot => (snapshot.Balance, snapshot.SnapshotId));
	}

	public async ValueTask InsertExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default)
	{
		await dbContext.CalculateChangeLogs.AddRangeAsync(
			records.Select(record => new CalculateChangeLog
			{
				Id = record.Id,
				CalculateId = record.CalculateId,
				Change = record.Change,
				Balance = record.Balance,
				RecordedOn = record.RecordedOn.ToUniversalTime(),
			}),
			cancellationToken).ConfigureAwait(false);

		_ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask MigrateExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default)
	{
		dbContext.ChangeTracker.Clear();

		var recordIds = records.Select(record => record.Id).ToList();
		var alreadyInsertedIds = await dbContext.CalculateChangeLogs
			.Where(row => recordIds.Contains(row.Id))
			.Select(row => row.Id)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var insertingRecords = records.Where(
			record => !alreadyInsertedIds.Contains(record.Id)).ToArray();

		if (insertingRecords is null || insertingRecords.Length == 0)
			return;

		await InsertExchangeLogsAsync(
			insertingRecords,
			cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask UpdateSnapshotAsync(Guid calculateId, CancellationToken cancellationToken = default)
	{
		var calculateRow = await dbContext.Calculates.FindAsync(
			[calculateId],
			cancellationToken).ConfigureAwait(false);

		logger.LogDebug("Find walletRow is {walletRow}", calculateRow);

		var changeLogsQuery = dbContext.CalculateChangeLogs
			.Where(row => row.CalculateId == calculateId);

		if (calculateRow is not null)
			changeLogsQuery = changeLogsQuery.Where(row => row.Id > calculateRow.SnapshotId);

		var newestRecord = changeLogsQuery
			.OrderByDescending(row => row.Id)
			.FirstOrDefault();

		logger.LogDebug("Newest record is {newestRecord}", newestRecord);

		if (newestRecord is null)
			return;

		if (calculateRow is null)
			_ = await dbContext.Calculates.AddAsync(
				new Entities.Calculate
				{
					Id = calculateId,
					Balance = newestRecord.Balance,
					SnapshotId = newestRecord.Id
				},
				cancellationToken).ConfigureAwait(false);
		else
		{
			calculateRow.Balance = newestRecord.Balance;
			calculateRow.SnapshotId = newestRecord.Id;
		}

		_ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}

static file class Extensions
{
	public static TTransform? Transform<TInput, TTransform>(this TInput? input, Func<TInput, TTransform> transform)
		where TTransform : struct
		=> input is null
			? null
			: transform(input);
}
