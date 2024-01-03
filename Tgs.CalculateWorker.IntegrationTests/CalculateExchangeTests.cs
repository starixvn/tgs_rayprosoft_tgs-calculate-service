using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.CalculateWorker.MessageQueue;
using Valhalla.MessageQueue;

namespace Tgs.CalculateWorker.IntegrationTests;

public class CalculateExchangeTests
{
    private static readonly JsonSerializerOptions _SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    [Fact]
    public async void 整合測試_建立一個新的計數()
    {
        // Arrange
        var subject = $"{Subjects.CreateSubject}.test";

        await using var web = CalculateWorkerTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(ProcessorTypes.CreateCalculateProcessorType, subject))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")));

        var sut = web.Services.GetRequiredService<IMessageSender>();

        var fakeRedisDatabase = web.Services.GetRequiredKeyedService<IDatabase>("Redis");

        var calculateId = Guid.NewGuid();
        var request = new CreateRequest
        {
            CalculateId = calculateId
        };

        // Act
        var actual = await sut.RequestAsync<CreateRequest, ExchangeResponse>(subject, request);

        // Assert
        Assert.NotNull(actual);
        _ = fakeRedisDatabase.Received(1)
            .ListRightPushAsync(
                Arg.Is<RedisKey>($"Tgs:Calculate:{calculateId}"),
                Arg.Is<RedisValue>(value => CheckLog(value, actual.SnapshotId)));
    }

    [Fact]
    public async Task 整合測試_對計數做計算要求()
    {
        // Arrange
        var subject = $"{Subjects.ExchangeSubject}.test";

        await using var web = CalculateWorkerTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(ProcessorTypes.ExchangeCalculateProcessorType, subject))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")));

        var sut = web.Services.GetRequiredService<IMessageSender>();

        var fakeRedisDatabase = web.Services.GetRequiredKeyedService<IDatabase>("Redis");

        var calculateId = Guid.NewGuid();

        _ = fakeRedisDatabase.ListGetByIndexAsync(Arg.Is<RedisKey>($"Tgs:Calculate:{calculateId}"), Arg.Is(-1L))
            .Returns(JsonSerializer.Serialize(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                100M,
                DateTime.UtcNow), _SerializerOptions));

        var request = new ExchangeRequest
        {
            CalculateId = calculateId,
            Change = 100M
        };

        // Act
        var actual = await sut.RequestAsync<ExchangeRequest, ExchangeResponse>(subject, request);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(200M, actual.Balance.ToDecimal());
        _ = fakeRedisDatabase.Received(1)
            .ListRightPushAsync(
                Arg.Is<RedisKey>($"Tgs:Calculate:{calculateId}"),
                Arg.Is<RedisValue>(value => CheckLog(value, actual.SnapshotId)));
    }

    [Fact]
    public async Task 整合測試_取得計數目前的數值()
    {
        // Arrange
        var subject = $"{Subjects.GetBalanceSubject}.test";

        await using var web = CalculateWorkerTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(ProcessorTypes.GetBalanceProcessorType, subject))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")));

        var sut = web.Services.GetRequiredService<IMessageSender>();

        var fakeRedisDatabase = web.Services.GetRequiredKeyedService<IDatabase>("Redis");

        var calculateId = Guid.NewGuid();

        _ = fakeRedisDatabase.ListGetByIndexAsync(Arg.Is<RedisKey>($"Tgs:Calculate:{calculateId}"), Arg.Is(-1L))
            .Returns(JsonSerializer.Serialize(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                100M,
                DateTime.UtcNow), _SerializerOptions));

        var request = new GetBalanceRequest
        {
            CalculateId = calculateId
        };

        // Act
        var actual = await sut.RequestAsync<GetBalanceRequest, ExchangeResponse>(subject, request);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(100M, actual.Balance.ToDecimal());
    }

    [Fact]
    public async Task 整合測試_刪除計數()
    {
        // Arrange
        var subject = $"{Subjects.DeleteSubject}.test";

        await using var web = CalculateWorkerTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(ProcessorTypes.DeleteCalculateProcessorType, subject))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")));

        var sut = web.Services.GetRequiredService<IMessageSender>();

        var fakeRedisDatabase = web.Services.GetRequiredKeyedService<IDatabase>("Redis");

        var calculateId = Guid.NewGuid();

        _ = fakeRedisDatabase.ListGetByIndexAsync(Arg.Is<RedisKey>($"Tgs:Calculate:{calculateId}"), Arg.Is(-1L))
            .Returns(JsonSerializer.Serialize(new ExchangeLog(
                Guid.NewGuid(),
                calculateId,
                0M,
                100M,
                DateTime.UtcNow), _SerializerOptions));

        var request = new DeleteRequest
        {
            CalculateId = calculateId
        };

        // Act
        var actual = await sut.RequestAsync<DeleteRequest, ExchangeResponse>(subject, request);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(100M, actual.Balance.ToDecimal());
        _ = fakeRedisDatabase.Received(1)
            .ListRightPushAsync(
                Arg.Is<RedisKey>($"Tgs:Calculate:{calculateId}"),
                Arg.Is<RedisValue>(value => CheckDeleteLog(value, actual.SnapshotId)));
    }

    private static bool CheckDeleteLog(RedisValue redisValue, Guid changeId)
    {
        var log = DeserializeExchangeLog(redisValue);

        return log is not null && log.Id == changeId && log.Deleted == true;
    }

    private static bool CheckLog(RedisValue redisValue, Guid changeId)
    {
        var log = DeserializeExchangeLog(redisValue);

        return log is not null && log.Id == changeId && log.Deleted == false;
    }

    private static ExchangeLog? DeserializeExchangeLog(RedisValue value)
        => JsonSerializer.Deserialize<ExchangeLog>(value.ToString(), _SerializerOptions);
}
