using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Calculate;
using Tgs.Calculate.Clients;
using Tgs.Calculate.Protos;
using Valhalla.MessageQueue;
using Xunit.Abstractions;

namespace Tgs.CalculateService.IntegrationTests;

public class ExchangeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task 整合測試_建立計算值()
    {
        // Arrange
        var fakeMessageSender = Substitute.For<IMessageSender>();
        await using var web = CalculateServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging
                    .AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeMessageSender)));

        var httpClient = web.CreateClient();
        var sut = new CalculateClient(httpClient);

        var calculateId = Guid.NewGuid();

        var expectedResponse = new ExchangeResult(Guid.NewGuid(), 0M);

        _ = fakeMessageSender.RequestAsync<CreateRequest, ExchangeResponse>(
            Arg.Is(Subjects.CreateSubject),
            Arg.Any<CreateRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeResponse
            {
                SnapshotId = expectedResponse.Id,
                Balance = expectedResponse.PostBalance
            });

        // Act
        var actual = await sut.CreateCalculateAsync(calculateId);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedResponse, actual);
    }

    [Fact]
    public async Task 整合測試_取得餘額()
    {
        // Arrange
        var fakeMessageSender = Substitute.For<IMessageSender>();
        await using var web = CalculateServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging
                    .AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeMessageSender)));

        var httpClient = web.CreateClient();
        var sut = new CalculateClient(httpClient);

        var calculateId = Guid.NewGuid();

        var expectedResponse = new ExchangeResult(Guid.NewGuid(), 100M);

        _ = fakeMessageSender.RequestAsync<GetBalanceRequest, ExchangeResponse>(
            Arg.Is(Subjects.GetBalanceSubject),
            Arg.Any<GetBalanceRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeResponse
            {
                SnapshotId = expectedResponse.Id,
                Balance = expectedResponse.PostBalance
            });

        // Act
        var actual = await sut.GetBalanceAsync(calculateId);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedResponse, actual);
    }

    [Fact]
    public async Task 整合測試_計算()
    {
        // Arrange
        var fakeMessageSender = Substitute.For<IMessageSender>();
        await using var web = CalculateServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging
                    .AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeMessageSender)));

        var httpClient = web.CreateClient();
        var sut = new CalculateClient(httpClient);

        var calculateId = Guid.NewGuid();

        var expectedResponse = new ExchangeResult(Guid.NewGuid(), 100M);

        _ = fakeMessageSender.RequestAsync<ExchangeRequest, ExchangeResponse>(
            Arg.Is(Subjects.ExchangeSubject),
            Arg.Is<ExchangeRequest>(request => request.Change.ToDecimal() == 100M),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeResponse
            {
                SnapshotId = expectedResponse.Id,
                Balance = expectedResponse.PostBalance
            });

        // Act
        var actual = await sut.ExchangeAsync(calculateId, 100M);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedResponse, actual);
    }

    [Fact]
    public async Task 整合測試_刪除計算()
    {
        // Arrange
        var fakeMessageSender = Substitute.For<IMessageSender>();
        await using var web = CalculateServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging
                    .AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeMessageSender)));

        var httpClient = web.CreateClient();
        var sut = new CalculateClient(httpClient);

        var calculateId = Guid.NewGuid();

        var expectedResponse = new ExchangeResult(Guid.NewGuid(), 100M);

        _ = fakeMessageSender.RequestAsync<DeleteRequest, ExchangeResponse>(
            Arg.Is(Subjects.DeleteSubject),
            Arg.Any<DeleteRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(new ExchangeResponse
            {
                SnapshotId = expectedResponse.Id,
                Balance = expectedResponse.PostBalance
            });

        // Act
        var actual = await sut.DeleteCalculateAsync(calculateId);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedResponse, actual);
    }
}
