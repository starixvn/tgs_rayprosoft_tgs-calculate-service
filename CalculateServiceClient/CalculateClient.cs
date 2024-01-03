using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Calculate.Clients;

internal class CalculateClient(HttpClient httpClient) : ICalculateClient
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

	public ValueTask<ExchangeResult> CreateCalculateAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> httpClient.PostAsync($"/exchange/{calculateId}", null, cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<ExchangeResult>(_SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Take(1)
			.Select(result => result!)
			.FirstAsync(cancellationToken);

	public ValueTask<ExchangeResult> GetBalanceAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> httpClient.GetAsync($"/exchange/{calculateId}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<ExchangeResult>(_SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Take(1)
			.Select(result => result!)
			.FirstAsync(cancellationToken);

	public ValueTask<ExchangeResult> ExchangeAsync(Guid calculateId, decimal change, CancellationToken cancellationToken = default)
		=> httpClient.PutAsJsonAsync(
			$"/exchange/{calculateId}",
			new
			{
				Change = change
			},
			cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<ExchangeResult>(_SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Take(1)
			.Select(result => result!)
			.FirstAsync(cancellationToken);

	public ValueTask<ExchangeResult> DeleteCalculateAsync(Guid calculateId, CancellationToken cancellationToken = default)
		=> httpClient.DeleteAsync($"/exchange/{calculateId}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<ExchangeResult>(_SerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Take(1)
			.Select(result => result!)
			.FirstAsync(cancellationToken);
}
