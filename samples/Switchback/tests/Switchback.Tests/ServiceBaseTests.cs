using Funcular.Data.Orm;
using Switchback.Application.Services;
using Switchback.Infrastructure.Persistence.Entities;

namespace Switchback.Tests;

/// <summary>The IReadModel write refusal, without a database: the guard throws before any SQL is built.</summary>
public class ServiceBaseTests
{
    private sealed class ProbeService(IOrmDataProvider provider) : ServiceBase(provider)
    {
        public Task<int> InsertRow(RecentOrderRow row) => InsertAsync<RecentOrderRow, int>(row);
        public Task<RecentOrderRow> UpdateRow(RecentOrderRow row) => UpdateAsync(row);
    }

    [Fact]
    public async Task Inserting_a_read_model_is_refused()
    {
        var service = new ProbeService(ProviderThatMustNotRun());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InsertRow(new RecentOrderRow()));

        Assert.Contains(nameof(RecentOrderRow), ex.Message);
    }

    [Fact]
    public async Task Updating_a_read_model_is_refused()
    {
        var service = new ProbeService(ProviderThatMustNotRun());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateRow(new RecentOrderRow()));
    }

    private static IOrmDataProvider ProviderThatMustNotRun() =>
        FunkyOrmRegistration.CreateProvider("Server=(none);Database=none;", sql => Assert.Fail($"No SQL should run, but got: {sql}"));
}
