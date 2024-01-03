using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using NSubstitute.ExceptionExtensions;
using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.CalculateCommitter.MessageQueue;
using Valhalla.MessageQueue;

namespace Tgs.CalculateCommitter.IntegrationTests;

public class CommitRequestDispatchProcessorTests
{
    [Fact]
    public async Task CommitRequestDispatchProcessor_處理要求轉發_會先取得目標通道資料()
    {
        // Arrange
        var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new CommitRequestDispatchHandler(
            fakeDistributeResourceManager,
            fakeMessageSender,
            NullLogger<CommitRequestDispatchHandler>.Instance);

        var calculateId = Guid.NewGuid();
        var request = new CommitRequest
        {
            CalculateId = calculateId
        };
        var subject = "exchange";
        var resource = "resource";

        _ = fakeDistributeResourceManager.GetBidResourceAsync(Arg.Is(calculateId))
            .Returns(resource);

        var requestData = request.ToByteArray();
        var msg = Substitute.For<IAcknowledgeMessage<ReadOnlyMemory<byte>>>();
        _ = msg.Subject.Returns(subject);
        _ = msg.Data.Returns(requestData);

        // Act
        await sut.HandleAsync(msg);

        // Assert
        _ = fakeMessageSender.Received(1)
            .SendAsync(
                Arg.Is($"{subject}.{resource}"),
                Arg.Is((ReadOnlyMemory<byte>)requestData),
                Arg.Any<IEnumerable<MessageHeaderValue>>());
    }

    [Fact]
    public async Task CommitRequestDispatchProcessor_處理要求轉發_如果還沒有Bid會進行Bid程序()
    {
        // Arrange
        var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new CommitRequestDispatchHandler(
            fakeDistributeResourceManager,
            fakeMessageSender,
            NullLogger<CommitRequestDispatchHandler>.Instance);

        var calculateId = Guid.NewGuid();
        var request = new ExchangeRequest
        {
            CalculateId = calculateId,
            Change = 100M
        };
        var subject = "exchange";
        var resource = "resource";

        _ = fakeMessageSender.RequestAsync<BidRequest, BidResponse>(
            Arg.Is(Subjects.CommitterBidSubject),
            Arg.Is<BidRequest>(req => req.CalculateId.ToGuid() == calculateId))
            .Returns(new BidResponse
            {
                Resource = resource
            });

        var requestData = request.ToByteArray();
        var msg = Substitute.For<IAcknowledgeMessage<ReadOnlyMemory<byte>>>();
        _ = msg.Subject.Returns(subject);
        _ = msg.Data.Returns(requestData);

        // Act
        await sut.HandleAsync(msg);

        // Assert
        _ = fakeMessageSender.Received(1)
            .SendAsync(
                Arg.Is($"{subject}.{resource}"),
                Arg.Is((ReadOnlyMemory<byte>)requestData),
                Arg.Any<IEnumerable<MessageHeaderValue>>());
    }

    [Fact]
    public async Task CommitRequestDispatchProcessor_處理要求轉發_如果遇到遠端失聯會清除Bid重試一次()
    {
        // Arrange
        var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new CommitRequestDispatchHandler(
            fakeDistributeResourceManager,
            fakeMessageSender,
            NullLogger<CommitRequestDispatchHandler>.Instance);

        var calculateId = Guid.NewGuid();
        var request = new ExchangeRequest
        {
            CalculateId = calculateId,
            Change = 100M
        };
        var subject = "exchange";
        var resource = "resource";
        var resource2 = "resource2";

        _ = fakeDistributeResourceManager.GetBidResourceAsync(Arg.Is(calculateId))
            .Returns(resource, default(string));

        var requestData = request.ToByteArray();
        var msg = Substitute.For<IAcknowledgeMessage<ReadOnlyMemory<byte>>>();
        _ = msg.Subject.Returns(subject);
        _ = msg.Data.Returns(requestData);

        _ = fakeMessageSender.SendAsync(
            Arg.Is($"{subject}.{resource}"),
            Arg.Is((ReadOnlyMemory<byte>)requestData),
            Arg.Any<IEnumerable<MessageHeaderValue>>(),
            Arg.Any<CancellationToken>())
            .Throws(new NatsNoRespondersException());

        _ = fakeMessageSender.RequestAsync<BidRequest, BidResponse>(
            Arg.Is(Subjects.CommitterBidSubject),
            Arg.Is<BidRequest>(req => req.CalculateId.ToGuid() == calculateId))
            .Returns(new BidResponse
            {
                Resource = resource2
            });

        // Act
        await sut.HandleAsync(msg);

        // Assert
        _ = fakeDistributeResourceManager.Received(1)
            .DiscardBidAsync(Arg.Is(calculateId), Arg.Any<CancellationToken>());

        _ = fakeMessageSender.Received(1)
            .SendAsync(
                Arg.Is($"{subject}.{resource2}"),
                Arg.Is((ReadOnlyMemory<byte>)requestData),
                Arg.Any<IEnumerable<MessageHeaderValue>>());
    }

    [Fact]
    public async Task CommitRequestDispatchProcessor_處理要求轉發_會延長Bid時間()
    {
        // Arrange
        var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new CommitRequestDispatchHandler(
            fakeDistributeResourceManager,
            fakeMessageSender,
            NullLogger<CommitRequestDispatchHandler>.Instance);

        var calculateId = Guid.NewGuid();
        var request = new ExchangeRequest
        {
            CalculateId = calculateId,
            Change = 100M
        };
        var subject = "exchange";
        var resource = "resource";

        _ = fakeDistributeResourceManager.GetBidResourceAsync(Arg.Is(calculateId))
            .Returns(resource);

        var requestData = request.ToByteArray();
        var msg = Substitute.For<IAcknowledgeMessage<ReadOnlyMemory<byte>>>();
        _ = msg.Subject.Returns(subject);
        _ = msg.Data.Returns(requestData);

        // Act
        await sut.HandleAsync(msg);

        // Assert
        _ = fakeDistributeResourceManager.Received(1)
            .ExtendBidAsync(Arg.Is(calculateId), Arg.Is(CommitterConsts.BidExpiry));
    }
}
