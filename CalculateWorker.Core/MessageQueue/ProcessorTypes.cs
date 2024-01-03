namespace Tgs.CalculateWorker.MessageQueue;

public static class ProcessorTypes
{
	public static readonly Type BidProcessorType = typeof(BidProcessor);
	public static readonly Type CreateCalculateProcessorType = typeof(CreateCalculateProcessor);
	public static readonly Type DeleteCalculateProcessorType = typeof(DeleteCalculateProcessor);
	public static readonly Type ExchangeCalculateProcessorType = typeof(ExchangeCalculateProcessor);
	public static readonly Type GetBalanceProcessorType = typeof(GetBalanceProcessor);
}
