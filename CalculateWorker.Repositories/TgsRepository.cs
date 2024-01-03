using Tgs.Calculate.Repositories;

namespace Tgs.CalculateWorker.Repositories;

internal class TgsRepository(CommonTgsRepository commonTgsRepository) : ITgsRepository
{
	public async ValueTask<ExchangeResult?> GetBalanceAsync(Guid calculateId, CancellationToken cancellationToken = default)
	{
		var result = await commonTgsRepository.GetBalanceAsync(
			calculateId,
			cancellationToken).ConfigureAwait(false);

		return result.Transform(o => new ExchangeResult(o.Balance, o.SnapshotId));
	}
}

static file class Extensions
{
	public static TTransform? Transform<TInput, TTransform>(this TInput? input, Func<TInput, TTransform> transform)
		where TInput : struct
		where TTransform : class
		=> input is null
			? null
			: transform(input.Value);
}
