using Switchback.Infrastructure.Persistence.Entities;
using Switchback.Shared.Contracts;

namespace Switchback.Application.Services;

public static class RecentOrderRowExtensions
{
    /// <summary>
    /// The one mapping to the published contract, called at the edge. It is warranted by confidentiality:
    /// the row carries internal ids (<c>Id</c>, <c>CustomerId</c>, <c>ShipToAddressId</c>) that must not be published.
    /// </summary>
    public static RecentOrderResponse ToResponse(this RecentOrderRow row) =>
        new(row.OrderNumber, row.OrderedUtc, row.CustomerName, row.ShipToCountryName, row.Total);
}
