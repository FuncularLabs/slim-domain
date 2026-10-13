using Microsoft.EntityFrameworkCore;
using Switchback.Data;
using Switchback.Demo;
using Switchback.Shared.Contracts;

namespace Switchback.Tests;

/// <summary>Every variant must return exactly the same page. The scoreboard is only fair if that holds.</summary>
[Collection(DatabaseCollection.Name)]
public class RecentOrdersParityTests(DatabaseFixture database)
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999)]
    public async Task Every_variant_returns_the_same_page(int page)
    {
        var (include, projection, funky) = await RunAllAsync(page, country: null);

        Assert.Equal(50, include.Count);
        Assert.Equal(include, projection);
        Assert.Equal(include, funky);
    }

    [Fact]
    public async Task Filtering_by_ship_to_country_matches_across_variants()
    {
        var (include, projection, funky) = await RunAllAsync(page: 0, country: "Canada");

        Assert.Equal(50, include.Count);
        Assert.All(include, row => Assert.Equal("Canada", row.ShipToCountry));
        Assert.Equal(include, projection);
        Assert.Equal(include, funky);
    }

    [Fact]
    public async Task The_first_page_with_a_missing_ship_to_address_matches_across_variants()
    {
        var page = await FirstPageWithNoShipToAddressAsync();
        var (include, projection, funky) = await RunAllAsync(page, country: null);

        Assert.Contains(include, row => row.ShipToCountry is null);
        Assert.Equal(include, projection);
        Assert.Equal(include, funky);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]   // page * pageSize would overflow into a negative offset
    public async Task An_impossible_page_is_rejected_by_every_variant(int page)
    {
        // Without this, EF fails in SQL (negative OFFSET) while FunkyORM clamps to page 0; both must refuse.
        foreach (var variant in Variants.All)
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => variant.RunAsync(database.ConnectionString, page, country: null));
    }

    private async Task<(IReadOnlyList<RecentOrderResponse> Include, IReadOnlyList<RecentOrderResponse> Projection, IReadOnlyList<RecentOrderResponse> Funky)>
        RunAllAsync(int page, string? country)
    {
        var include = await Variants.EfInclude.RunAsync(database.ConnectionString, page, country);
        var projection = await Variants.EfProjection.RunAsync(database.ConnectionString, page, country);
        var funky = await Variants.Funky.RunAsync(database.ConnectionString, page, country);
        return (include.Rows, projection.Rows, funky.Rows);
    }

    /// <summary>Found by query, not hard-coded: the seed decides where the first order without a ship-to address lands.</summary>
    private async Task<int> FirstPageWithNoShipToAddressAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = new SwitchbackDbContext(SwitchbackDbContext.CreateOptions(database.ConnectionString));

        var first = await db.Orders
            .Where(o => o.ShipToAddressId == null)
            .OrderByDescending(o => o.OrderedUtc)
            .ThenByDescending(o => o.Id)
            .Select(o => new { o.OrderedUtc, o.Id })
            .FirstAsync(ct);

        var rowsBefore = await db.Orders.CountAsync(
            o => o.OrderedUtc > first.OrderedUtc || (o.OrderedUtc == first.OrderedUtc && o.Id > first.Id), ct);

        return rowsBefore / 50;
    }
}
