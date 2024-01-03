using Tgs.Calculate;

namespace Tgs.CalculateCommitter;

public interface ITgsRepository
{
	ValueTask InsertExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default);

	ValueTask MigrateExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default);

	ValueTask UpdateSnapshotAsync(Guid calculateId, CancellationToken cancellationToken = default);
}
