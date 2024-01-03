namespace Tgs.Calculate;
public record ExchangeLog(
	Guid Id,
	Guid CalculateId,
	decimal Change,
	decimal Balance,
	DateTime RecordedOn,
	bool Deleted = false);
