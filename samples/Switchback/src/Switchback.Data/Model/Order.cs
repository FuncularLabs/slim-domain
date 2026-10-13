namespace Switchback.Data.Model;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? ShipToAddressId { get; set; }
    public Address? ShipToAddress { get; set; }
    public DateTime OrderedUtc { get; set; }
    public string Status { get; set; } = OrderStatus.Placed;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
}

public static class OrderStatus
{
    public const string Placed = "Placed", Shipped = "Shipped", Delivered = "Delivered", Cancelled = "Cancelled";
    public static readonly string[] All = [Placed, Shipped, Delivered, Cancelled];
}
