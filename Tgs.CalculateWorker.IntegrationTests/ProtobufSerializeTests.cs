using System.Buffers;
using Tgs.Calculate.MessageSerializers;
using Tgs.Calculate.Protos;

namespace Tgs.CalculateWorker.IntegrationTests;

public class ProtobufSerializeTests
{
    [Fact]
    public void 序列化測試_BidRequest()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new BidRequest
        {
            CalculateId = Guid.NewGuid()
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<BidRequest>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<BidRequest>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.CalculateId.ToGuid(), actual.CalculateId.ToGuid());
    }

    [Fact]
    public void 序列化測試_BidResponse()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new BidResponse
        {
            Resource = "test"
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<BidResponse>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<BidResponse>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.Resource, actual.Resource);
    }

    [Fact]
    public void 序列化測試_ExchangeRequest()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new ExchangeRequest
        {
            CalculateId = Guid.NewGuid(),
            Change = 100M
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<ExchangeRequest>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<ExchangeRequest>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.CalculateId.ToGuid(), actual.CalculateId.ToGuid());
        Assert.Equal(data.Change.ToDecimal(), actual.Change.ToDecimal());
    }

    [Fact]
    public void 序列化測試_ExchangeResponse()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new ExchangeResponse
        {
            Balance = 100M,
            SnapshotId = Guid.NewGuid()
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<ExchangeResponse>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<ExchangeResponse>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.Balance.ToDecimal(), actual.Balance.ToDecimal());
        Assert.Equal(data.SnapshotId.ToGuid(), actual.SnapshotId.ToGuid());
    }

    [Fact]
    public void 序列化測試_GetBalanceRequest()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new GetBalanceRequest
        {
            CalculateId = Guid.NewGuid()
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<GetBalanceRequest>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<GetBalanceRequest>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.CalculateId.ToGuid(), actual.CalculateId.ToGuid());
    }

    [Fact]
    public void 序列化測試_DeleteRequest()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new DeleteRequest
        {
            CalculateId = Guid.NewGuid()
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<DeleteRequest>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<DeleteRequest>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.CalculateId.ToGuid(), actual.CalculateId.ToGuid());
    }

    [Fact]
    public void 序列化測試_CreateRequest()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new CreateRequest
        {
            CalculateId = Guid.NewGuid()
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<CreateRequest>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<CreateRequest>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.CalculateId.ToGuid(), actual.CalculateId.ToGuid());
    }

    [Fact]
    public void 序列化測試_CommitRequest()
    {
        // Arrange
        var sut = ProtobufSerializerRegistration.Default;

        var data = new CommitRequest
        {
            CalculateId = Guid.NewGuid()
        };
        var buffer = new ArrayBufferWriter<byte>();

        // Act
        sut.GetSerializer<CommitRequest>().Serialize(buffer, data);
        var actual = sut.GetDeserializer<CommitRequest>().Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(data.CalculateId.ToGuid(), actual.CalculateId.ToGuid());
    }
}
