using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.Calculate.Repositories.Entities;
using Tgs.CalculateCommitter.MessageQueue;
using Valhalla.MessageQueue;
using Valhalla.Messages;

namespace Tgs.CalculateCommitter.IntegrationTests;

public class CommitCalculateTests
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

    [Fact(Timeout = 3000)]
    public async Task 整合測試_接收到CommitRequest()
    {
        // Arrange
        var subject = $"{Subjects.CommitSubject}.test";

        await using var web = CalculateCommitterTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .Decorate<ITgsRepository, WrapTgsRepository>()
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddHandler(ProcessorTypes.CommitHandlerType, subject))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")));

        var sut = web.Services.GetRequiredService<IMessageSender>();

        var fakeRedisDatabase = web.Services.GetRequiredKeyedService<IDatabase>("Redis");
        var dbContext = web.Services.GetRequiredService<TgsContext>();

        var calculateId = Guid.NewGuid();
        var request = new CommitRequest
        {
            CalculateId = calculateId
        };

        var data = new ExchangeLog(
            Guid.NewGuid(),
            calculateId,
            100M,
            100M,
            DateTime.UtcNow);

        _ = fakeRedisDatabase.ListRangeAsync(
            Arg.Is(new RedisKey($"Tgs:Calculate:{calculateId}")),
            Arg.Is(0L),
            Arg.Is(10L))
            .Returns(
                [new RedisValue(JsonSerializer.Serialize(data, _SerializerOptions))],
                Array.Empty<RedisValue>());

        // Act
        await sut.SendAsync(subject, request);
        await WrapTgsRepository.AfterSnapshot;
        var actual = await dbContext.CalculateChangeLogs.ToArrayAsync();

        // Assert
        Assert.Single(actual);
        Assert.Equal(data.Id, actual.First().Id);
    }
}
