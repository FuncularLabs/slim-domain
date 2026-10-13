namespace Switchback.Data.Model;

public class Address
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string City { get; set; } = "";
    public string? Region { get; set; }
    public string PostalCode { get; set; } = "";
    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
