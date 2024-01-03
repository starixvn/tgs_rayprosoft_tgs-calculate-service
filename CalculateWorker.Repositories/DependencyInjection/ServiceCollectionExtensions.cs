using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tgs.Calculate.Repositories;
using Tgs.Calculate.Repositories.Entities;
using Tgs.CalculateWorker;
using Tgs.CalculateWorker.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCalculateWorkerRepositories(
		this IServiceCollection services,
		Action<DbContextOptionsBuilder> configureDbContext,
		Func<IServiceProvider, IDatabase> resolveRedisDatabase)
		=> services
			.AddSingleton(sp => ActivatorUtilities.CreateInstance<CommonRedisRepository>(
				sp,
				resolveRedisDatabase(sp)))
			.AddSingleton<RedisRepository>()
			.AddSingleton<IDistributedResourceManager, RedisRepository>(sp => sp.GetRequiredService<RedisRepository>())
			.AddSingleton<IFirstLevelRepository>(sp => sp.GetRequiredService<RedisRepository>())
			.AddDbContext<TgsContext>(configureDbContext)
			.AddScoped<CommonTgsRepository>()
			.AddScoped<ITgsRepository, TgsRepository>();
}
