using System.ComponentModel.DataAnnotations.Schema;

namespace Switchback.Infrastructure.Persistence.Entities;

/// <summary>The Customers table and nothing more: no addresses, no orders, no behavior.</summary>
[Table("Customers")]
public class CustomerEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string LoyaltyTier { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
}
