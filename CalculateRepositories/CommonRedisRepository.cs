using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace Tgs.Calculate.Repositories;

internal class CommonRedisRepository(IDatabase redisDatabase)
{
	private static readonly RedisKey _RootKey = new("Tgs:Calculate");

	private static readonly JsonSerializerOptions _SerializerOptions = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		WriteIndented = false,
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};

	public async ValueTask<ExchangeLog?> GetNewestExchangeLogAsync(Guid calculateId)
	{
		var key = _RootKey.Append($":{calculateId}");

		var result = await redisDatabase.ListGetByIndexAsync(
			key,
			-1).ConfigureAwait(false);

		return result.HasValue
			? JsonSerializer.Deserialize<ExchangeLog>((string)result!, _SerializerOptions)
			: null;
	}

	public async ValueTask WriteExchangeLogAsync(ExchangeLog exchangeLog)
	{
		var key = _RootKey.Append($":{exchangeLog.CalculateId}");

		_ = await redisDatabase.ListRightPushAsync(
			key,
			JsonSerializer.Serialize(exchangeLog, _SerializerOptions)).ConfigureAwait(false);
	}

	public IAsyncEnumerable<ExchangeLog> GetWaitingPersistentExchangeLogsAsync(Guid calculateId, int limit, CancellationToken cancellationToken = default)
		=> redisDatabase.ListRangeAsync(_RootKey.Append($":{calculateId}"), 0, limit)
			.ToAsyncEnumerable()
			.SelectMany(values => values.ToAsyncEnumerable())
			.Select(value => JsonSerializer.Deserialize<ExchangeLog>((string)value!, _SerializerOptions))
			.Where(record => record is not null)
			.Select(record => record!)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());

	public async ValueTask RemoveExchangeLogsAsync(Guid calculateId, int length)
		=> await redisDatabase.ListTrimAsync(
			_RootKey.Append($":{calculateId}"),
			length,
			-1).ConfigureAwait(false);

	public async ValueTask<string> BidAsync(
		Guid calculateId,
		string role,
		string resource,
		TimeSpan expiry)
	{
		var key = _RootKey.Append($":{role}:{calculateId}");

		var result = await redisDatabase.StringSetAndGetAsync(
			key,
			resource,
			expiry: expiry,
			when: When.NotExists).ConfigureAwait(false);

		return (string)result!;
	}

	public async ValueTask<string?> GetBidResourceAsync(Guid calculateId, string role)
	{
		var key = _RootKey.Append($":{role}:{calculateId}");

		var resource = await redisDatabase.StringGetAsync(
			key).ConfigureAwait(false);

		return resource;
	}

	public async ValueTask DiscardBidAsync(Guid calculateId, string role)
	{
		var key = _RootKey.Append($":{role}:{calculateId}");

		_ = await redisDatabase.KeyDeleteAsync(
			key).ConfigureAwait(false);
	}

	public async ValueTask ExtendBidAsync(Guid calculateId, string role, TimeSpan expiry)
	{
		var key = _RootKey.Append($":{role}:{calculateId}");

		_ = await redisDatabase.KeyExpireAsync(
			key,
			expiry,
			when: ExpireWhen.GreaterThanCurrentExpiry).ConfigureAwait(false);
	}
}
