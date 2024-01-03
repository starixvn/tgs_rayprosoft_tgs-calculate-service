using System.Buffers;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using NATS.Client.Core;

namespace Tgs.CalculateService.Models;

public class ProtobufSerializer<T> : INatsSerializer<T>
{
	public static readonly ProtobufSerializer<T> Default = new();

	private static readonly INatsSerializer<T> _Next = NatsDefaultSerializer<T>.Default;

	public void Serialize(IBufferWriter<byte> bufferWriter, T value)
	{
		if (value is IMessage msg)
		{
			msg.WriteTo(bufferWriter);
			return;
		}

		_Next.Serialize(bufferWriter, value);
	}

	public T? Deserialize(in ReadOnlySequence<byte> buffer)
	{
		if (typeof(T).IsAssignableFrom(typeof(IMessage)))
		{
			var instance = Activator.CreateInstance(typeof(T)) as IMessage;

			if (instance is not null)
			{
				instance.MergeFrom(buffer);

				return Unsafe.As<IMessage, T>(ref instance);
			}
		}

		return _Next.Deserialize(buffer);
	}
}
