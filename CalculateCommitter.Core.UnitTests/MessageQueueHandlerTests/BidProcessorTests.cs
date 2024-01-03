using Tgs.Calculate.Protos;
using Tgs.CalculateCommitter;
using Tgs.CalculateCommitter.MessageQueue;

namespace CalculateCommitter.Core.UnitTests.MessageQueueHandlerTests;

public class BidProcessorTests
{
    [Fact]
    public async Task BidProcessor_處理要求競標()
    {
        // Arrange
        var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();

        var sut = new BidProcessor(fakeDistributeResourceManager);

        var calculateId = Guid.NewGuid();
        var request = new BidRequest
        {
            CalculateId = calculateId
        };
        var expectedResource = "test";

        _ = fakeDistributeResourceManager.BidAsync(
            Arg.Is(calculateId),
            Arg.Any<string>(),
            Arg.Is(CommitterConsts.BidExpiry))
            .Returns(expectedResource);

        // Act
        var actual = await sut.HandleAsync("bid", request, null);

        // Assert
        Assert.Equal(expectedResource, actual.Resource);
    }
}
