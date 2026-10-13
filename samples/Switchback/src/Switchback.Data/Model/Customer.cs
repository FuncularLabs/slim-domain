namespace Switchback.Data.Model;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string LoyaltyTier { get; set; } = LoyaltyTiers.Standard;
    public decimal CreditLimit { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }

    // Navigation properties: the "before" world. Each one is an invitation to Include.
    public List<Address> Addresses { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
}

public static class LoyaltyTiers
{
    public const string Standard = "Standard", Silver = "Silver", Gold = "Gold";
    public static readonly string[] All = [Standard, Silver, Gold];
}
