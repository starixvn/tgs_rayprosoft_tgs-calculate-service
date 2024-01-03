using Tgs.Calculate;

namespace Tgs.CalculateCommitter;

public interface IFirstLevelRepository
{
	IAsyncEnumerable<ExchangeLog> GetWaitingPersistentLogsAsync(Guid calculateId, CancellationToken cancellationToken = default);

	ValueTask RemoveLogsAsync(Guid calculateId, int length, CancellationToken cancellationToken = default);
}
