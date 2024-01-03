namespace Tgs.CalculateWorker;
public record ExchangeResult(
	decimal Balance,
	Guid SnapshotId,
	bool IsDeleted = false);
