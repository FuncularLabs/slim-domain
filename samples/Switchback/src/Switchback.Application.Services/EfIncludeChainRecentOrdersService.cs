using Microsoft.EntityFrameworkCore;
using Switchback.Data;
using Switchback.Data.Model;
using Switchback.Shared.Contracts;

namespace Switchback.Application.Services;

/// <summary>
/// The Recent orders list as most EF teams first write it. The navigation properties exist, so the code
/// walks them. It is readable, it returns the right rows, and it passes review.
/// </summary>
public sealed class EfIncludeChainRecentOrdersService(SwitchbackDbContext db)
{
    public async Task<IReadOnlyList<RecentOrderResponse>> GetPageAsync(
        int page, int pageSize = Paging.DefaultPageSize, string? shipToCountry = null, CancellationToken ct = default)
    {
        Paging.Validate(page, pageSize);

        IQueryable<Order> query = db.Orders;

        if (shipToCountry is not null)
            query = query.Where(o => o.ShipToAddress!.Country.Name == shipToCountry);

        var orders = await query
            .Include(o => o.Customer)
            .Include(o => o.ShipToAddress)
                .ThenInclude(a => a!.Country)
            .OrderByDescending(o => o.OrderedUtc)
            .ThenByDescending(o => o.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // The screen needs five values. We loaded every column of four tables to get them.
        return orders
            .Select(o => new RecentOrderResponse(
                o.OrderNumber,
                o.OrderedUtc,
                o.Customer.Name,
                o.ShipToAddress?.Country.Name,
                o.Total))
            .ToList();
    }
}
