using NATS.Client.Core;

namespace Tgs.CalculateWorker.MessageQueue;

public class ProtobufSerializerRegistration : INatsSerializerRegistry
{
	public static readonly INatsSerializerRegistry Default = new ProtobufSerializerRegistration();

	public INatsSerialize<T> GetSerializer<T>()
		=> ProtobufSerializer<T>.Default;

	public INatsDeserialize<T> GetDeserializer<T>()
		=> ProtobufSerializer<T>.Default;
}
