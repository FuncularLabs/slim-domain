using System.ComponentModel.DataAnnotations.Schema;
using Funcular.Data.Orm.Attributes;

namespace Switchback.Infrastructure.Persistence.Entities;

/// <summary>
/// One row of the Recent orders list: exactly what the screen shows, plus the keys needed to reach it.
/// Sealed and <see cref="IReadModel"/>: a projection is never a legal write.
/// <para>
/// It does not inherit <see cref="OrderEntity"/>. Inheriting would load all 13 order columns; this row
/// needs four of them plus two keys. The Slim Domain rule is "carry only what the operation uses".
/// </para>
/// </summary>
[Table("Orders")]
public sealed class RecentOrderRow : IReadModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public DateTime OrderedUtc { get; set; }
    public decimal Total { get; set; }

    // Join keys. FunkyORM walks these to reach the remote values; the mapping at the edge leaves them out.
    [RemoteLink(typeof(CustomerEntity))]
    public int CustomerId { get; set; }

    [RemoteLink(typeof(AddressEntity))]
    public int? ShipToAddressId { get; set; }

    // One hop: Orders.CustomerId → Customers.Name
    [RemoteProperty(typeof(CustomerEntity),
        nameof(CustomerId), nameof(CustomerEntity.Name))]
    public string CustomerName { get; set; } = "";

    // Two hops: Orders.ShipToAddressId → Addresses.CountryId → Countries.Name. LEFT JOINs, so a
    // missing ship-to address yields null rather than dropping the row.
    [RemoteProperty(typeof(CountryEntity),
        nameof(ShipToAddressId), nameof(AddressEntity.CountryId), nameof(CountryEntity.Name))]
    public string? ShipToCountryName { get; set; }
}
