namespace Tgs.CalculateCommitter.MessageQueue;

public static class ProcessorTypes
{
	public static readonly Type BidProcessorType = typeof(BidProcessor);
	public static readonly Type CommitHandlerType = typeof(CommitHandler);
}
