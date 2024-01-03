namespace Tgs.CalculateWorker;

public static class CalculateConsts
{
	public static readonly TimeSpan ReceptionExpiry = new(0, 10, 10);
	public static readonly TimeSpan BidExpiry = new(0, 10, 0);
	public static readonly int CleanInterval = 30000;
}
