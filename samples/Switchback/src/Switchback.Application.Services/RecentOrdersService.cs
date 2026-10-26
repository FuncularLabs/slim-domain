using Funcular.Data.Orm;
using Switchback.Infrastructure.Persistence.Entities;

namespace Switchback.Application.Services;

/// <summary>
/// The Recent orders list on the FunkyORM road. The read model declares the shape once; every query of it
/// stays that lean, with nothing to remember at the call site.
/// </summary>
public sealed class RecentOrdersService(IOrmDataProvider provider) : ServiceBase(provider)
{
    /// <summary>One page, newest first. With <paramref name="shipToCountry"/>, only orders shipping to that country.</summary>
    public IReadOnlyList<RecentOrderRow> GetPage(int page, int pageSize = Paging.DefaultPageSize, string? shipToCountry = null)
    {
        Paging.Validate(page, pageSize);

        var query = Query<RecentOrderRow>();

        if (shipToCountry is not null)
            query = query.Where(r => r.ShipToCountryName == shipToCountry);   // a filter on a joined column

        // FunkyORM operator order: Where → OrderBy/ThenBy → Skip → Take → materialize (nothing after the page).
        return query
            .OrderByDescending(r => r.OrderedUtc)
            .ThenByDescending(r => r.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
