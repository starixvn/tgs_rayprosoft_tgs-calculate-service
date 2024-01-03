using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.Calculate.Repositories.Entities;

namespace Tgs.CalculateCommitter.IntegrationTests;

internal static class CalculateCommitterTestHost
{
    public static WebApplicationFactory<Program> Instance => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(hostBuilder => hostBuilder
            .ConfigureServices(services => services
                .AddFakeNatsMessageQueue()
                .AddKeyedSingleton("Redis", Substitute.For<IDatabase>())
                .AddSingleton(sp => new TgsContext(new DbContextOptionsBuilder<TgsContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options))));
}
