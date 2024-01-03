using Tgs.Calculate;
using Tgs.Calculate.Repositories;

namespace Tgs.CalculateCommitter.Repositories;

internal class TgsRepository(CommonTgsRepository commonTgsRepository)
	: ITgsRepository
{
	public ValueTask InsertExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default)
		=> commonTgsRepository.InsertExchangeLogsAsync(records, cancellationToken);

	public ValueTask MigrateExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default)
		=> commonTgsRepository.MigrateExchangeLogsAsync(records, cancellationToken);

	public ValueTask UpdateSnapshotAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> commonTgsRepository.UpdateSnapshotAsync(calculateId, cancellationToken);
}
