using Switchback.Application.Services;
using Switchback.Data;
using Switchback.Shared.Contracts;

namespace Switchback.Demo;

/// <summary>What one run of a variant produced: the rows, the SQL that was sent, and what EF tracked.</summary>
public sealed record VariantRun(IReadOnlyList<RecentOrderResponse> Rows, string Sql, int TrackedObjects);

public sealed class Variant(string key, string name, Func<string, int, string?, Task<VariantRun>> run)
{
    public string Key { get; } = key;
    public string Name { get; } = name;

    /// <summary>A fresh context or provider every time, so each run pays the full per-request cost.</summary>
    public Task<VariantRun> RunAsync(string connectionString, int page, string? country) => run(connectionString, page, country);
}

/// <summary>The three implementations of the same page, side by side.</summary>
public static class Variants
{
    public static readonly Variant EfInclude = new("include", "EF Core — Include chain", RunEfIncludeAsync);
    public static readonly Variant EfProjection = new("projection", "EF Core — projection", RunEfProjectionAsync);
    public static readonly Variant Funky = new("funky", "FunkyORM — read model", RunFunkyAsync);

    public static readonly IReadOnlyList<Variant> All = [EfInclude, EfProjection, Funky];

    public static Variant? Find(string? key) =>
        All.FirstOrDefault(v => v.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    private static async Task<VariantRun> RunEfIncludeAsync(string connectionString, int page, string? country)
    {
        var capture = new EfSqlCapture();
        await using var db = new SwitchbackDbContext(SwitchbackDbContext.CreateOptions(connectionString, capture));
        var rows = await new EfIncludeChainRecentOrdersService(db).GetPageAsync(page, shipToCountry: country);
        return new VariantRun(rows, capture.LastSelect, db.ChangeTracker.Entries().Count());
    }

    private static async Task<VariantRun> RunEfProjectionAsync(string connectionString, int page, string? country)
    {
        var capture = new EfSqlCapture();
        await using var db = new SwitchbackDbContext(SwitchbackDbContext.CreateOptions(connectionString, capture));
        var rows = await new EfProjectionRecentOrdersService(db).GetPageAsync(page, shipToCountry: country);
        return new VariantRun(rows, capture.LastSelect, db.ChangeTracker.Entries().Count());
    }

    private static Task<VariantRun> RunFunkyAsync(string connectionString, int page, string? country)
    {
        var capture = new FunkyOrmSqlCapture();
        var provider = FunkyOrmRegistration.CreateProvider(connectionString, capture.Log);
        try
        {
            var rows = new RecentOrdersService(provider)
                .GetPage(page, shipToCountry: country)
                .Select(r => r.ToResponse())   // the mapping at the edge
                .ToList();
            return Task.FromResult(new VariantRun(rows, capture.LastSelect, TrackedObjects: 0));
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }
}
