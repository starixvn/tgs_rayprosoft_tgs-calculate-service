using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Query;

namespace CalculateRepositories.UnitTests;

internal class TestQueryCompilationContext(QueryCompilationContextDependencies dependencies, bool async)
    : QueryCompilationContext(dependencies, async)
{
    public override Func<QueryContext, TResult> CreateQueryExecutor<TResult>(Expression query)
    {
        if (query is MethodCallExpression methodCall && methodCall.Method.Name == "ExecuteDelete")
        {
            var entityExecutor = typeof(QueryCompilationContext).GetMethod(nameof(CreateQueryExecutor))!
                .MakeGenericMethod(typeof(IEnumerable<>).MakeGenericType(methodCall.Arguments[0].Type.GenericTypeArguments[0]))
                .Invoke(this, [methodCall.Arguments[0]]);

            var deleteMethod = IsAsync
                ? typeof(TestQueryCompilationContext).GetMethod(
                    nameof(AsyncWrapQueryExecutorForDelete),
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(methodCall.Arguments[0].Type.GenericTypeArguments[0])
                : typeof(TestQueryCompilationContext).GetMethod(
                    nameof(WrapQueryExecutorForDelete),
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(methodCall.Arguments[0].Type.GenericTypeArguments[0]);

            var executor = deleteMethod
                .Invoke(this, [entityExecutor]);

            return Unsafe.As<object, Func<QueryContext, TResult>>(ref executor!);
        }

        return base.CreateQueryExecutor<TResult>(query);
    }

    private Func<QueryContext, Task<int>> AsyncWrapQueryExecutorForDelete<TEntity>(Func<QueryContext, IEnumerable<TEntity>> executor)
        => ctx => Task.FromResult(WrapQueryExecutorForDelete(executor)(ctx));

    private Func<QueryContext, int> WrapQueryExecutorForDelete<TEntity>(Func<QueryContext, IEnumerable<TEntity>> executor)
        => ctx =>
        {
            var deleteRows = executor(ctx).ToArray();

            foreach (var row in deleteRows)
                Dependencies.Context.Remove(row!);
            Dependencies.Context.SaveChanges();

            return deleteRows.Length;
        };
}
