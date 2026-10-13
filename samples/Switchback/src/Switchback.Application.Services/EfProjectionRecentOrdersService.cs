using Microsoft.EntityFrameworkCore;
using Switchback.Data;
using Switchback.Data.Model;
using Switchback.Shared.Contracts;

namespace Switchback.Application.Services;

/// <summary>
/// The correct EF fix: project with <c>Select</c> into the contract. EF writes the same joins, selects only
/// the five fields, and tracks nothing (a projection that returns no entity type is never tracked, so no
/// <c>AsNoTracking()</c> is needed). The catch: it has to be remembered at every call site.
/// </summary>
public sealed class EfProjectionRecentOrdersService(SwitchbackDbContext db)
{
    public async Task<IReadOnlyList<RecentOrderResponse>> GetPageAsync(
        int page, int pageSize = Paging.DefaultPageSize, string? shipToCountry = null, CancellationToken ct = default)
    {
        Paging.Validate(page, pageSize);

        IQueryable<Order> query = db.Orders;

        if (shipToCountry is not null)
            query = query.Where(o => o.ShipToAddress!.Country.Name == shipToCountry);

        return await query
            .OrderByDescending(o => o.OrderedUtc)
            .ThenByDescending(o => o.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(o => new RecentOrderResponse(
                o.OrderNumber,
                o.OrderedUtc,
                o.Customer.Name,
                o.ShipToAddress!.Country.Name,   // EF propagates null through the LEFT JOINs
                o.Total))
            .ToListAsync(ct);
    }
}
