using System.ComponentModel.DataAnnotations.Schema;

namespace Switchback.Infrastructure.Persistence.Entities;

/// <summary>The Orders table and nothing more. Loading one of these loads 13 columns, by design: it is the row.</summary>
[Table("Orders")]
public class OrderEntity
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public int? ShipToAddressId { get; set; }
    public DateTime OrderedUtc { get; set; }
    public string Status { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
}
