namespace Tgs.CalculateCommitter;

public static class CommitterConsts
{
	public const string ExchangeEventName = "CalculateExchanged";
	public const string UpdateEventName = "CalculateUpdate";
	public static readonly TimeSpan ReceptionExpiry = new(0, 10, 10);
	public static readonly TimeSpan BidExpiry = new(0, 10, 0);
}
