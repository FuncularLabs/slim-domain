using Switchback.Application.Services;
using Switchback.Demo;
using Switchback.Infrastructure.Persistence.Entities;

namespace Switchback.Tests;

/// <summary>The scoreboard numbers, pinned: the headline of episode 1 depends on these.</summary>
[Collection(DatabaseCollection.Name)]
public class SqlShapeTests(DatabaseFixture database)
{
    [Theory]
    [InlineData("include", 36)]
    [InlineData("projection", 5)]
    [InlineData("funky", 8)]
    public async Task Each_variant_loads_the_expected_number_of_columns(string key, int expectedColumns)
    {
        var run = await Variants.Find(key)!.RunAsync(database.ConnectionString, page: 0, country: null);

        Assert.Equal(expectedColumns, SqlColumnCounter.Count(run.Sql));
    }

    [Fact]
    public async Task The_Include_chain_tracks_every_object_it_loaded()
    {
        var run = await Variants.EfInclude.RunAsync(database.ConnectionString, page: 0, country: null);

        // 50 orders plus their customers, addresses and countries.
        Assert.True(run.TrackedObjects > 50, $"Expected more than 50 tracked objects, found {run.TrackedObjects}.");
    }

    [Fact]
    public async Task The_EF_projection_tracks_nothing()
    {
        // FunkyORM has no tracker, so there is nothing to assert for it; EF's projection must not use its own.
        var run = await Variants.EfProjection.RunAsync(database.ConnectionString, page: 0, country: null);

        Assert.Equal(0, run.TrackedObjects);
    }

    [Fact]
    public async Task FunkyORM_reaches_the_country_through_two_LEFT_JOINs()
    {
        var run = await Variants.Funky.RunAsync(database.ConnectionString, page: 0, country: null);

        Assert.Equal(3, CountOccurrences(run.Sql, "LEFT JOIN"));
        Assert.Contains("Customers", run.Sql);
        Assert.Contains("Addresses", run.Sql);
        Assert.Contains("Countries", run.Sql);
    }

    [Fact]
    public async Task A_filter_on_a_joined_column_lands_in_the_WHERE_clause()
    {
        var run = await Variants.Funky.RunAsync(database.ConnectionString, page: 0, country: "Canada");

        Assert.Contains("WHERE", run.Sql);
        Assert.DoesNotContain("'Canada'", run.Sql);   // parameterized, never inlined
    }

    [Fact]
    public void A_canonical_entity_loads_exactly_the_columns_it_declares()
    {
        // Lab 01, step 5: OrderEntity has 13 properties, so a query of it selects 13 columns.
        var capture = new FunkyOrmSqlCapture();
        var provider = FunkyOrmRegistration.CreateProvider(database.ConnectionString, capture.Log);
        try
        {
            var rows = provider.Query<OrderEntity>().OrderBy(o => o.Id).Take(1).ToList();

            Assert.Single(rows);
            Assert.Equal(13, SqlColumnCounter.Count(capture.LastSelect));
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.OrdinalIgnoreCase); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.OrdinalIgnoreCase))
            count++;
        return count;
    }
}
