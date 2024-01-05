using Microsoft.EntityFrameworkCore.Query;

namespace CalculateRepositories.UnitTests;

internal class TestQueryCompilationContextFactory(QueryCompilationContextDependencies dependencies) : IQueryCompilationContextFactory
{
    public QueryCompilationContext Create(bool async)
        => new TestQueryCompilationContext(dependencies, async);
}
