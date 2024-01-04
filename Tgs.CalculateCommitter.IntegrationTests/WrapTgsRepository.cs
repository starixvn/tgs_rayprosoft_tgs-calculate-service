using Tgs.Calculate;

namespace Tgs.CalculateCommitter.IntegrationTests;

internal class WrapTgsRepository(ITgsRepository tgsRepository) : ITgsRepository
{
    private static readonly TaskCompletionSource _TaskCompletionSource = new();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "AMNF0001:Awaitable (Asynchronous) methods should be suffixed with 'Async'", Justification = "<暫止>")]
    public static Task AfterSnapshot => _TaskCompletionSource.Task;

    public ValueTask InsertExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default)
        => tgsRepository.InsertExchangeLogsAsync(records, cancellationToken);

    public ValueTask MigrateExchangeLogsAsync(IEnumerable<ExchangeLog> records, CancellationToken cancellationToken = default)
        => tgsRepository.MigrateExchangeLogsAsync(records, cancellationToken);

    public async ValueTask UpdateSnapshotAsync(Guid calculateId, CancellationToken cancellationToken = default)
    {
        await tgsRepository.UpdateSnapshotAsync(calculateId, cancellationToken);
        _TaskCompletionSource.SetResult();
    }
}
