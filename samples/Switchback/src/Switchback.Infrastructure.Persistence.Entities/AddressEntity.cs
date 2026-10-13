using System.ComponentModel.DataAnnotations.Schema;

namespace Switchback.Infrastructure.Persistence.Entities;

/// <summary>The Addresses table and nothing more. Foreign keys are plain ints; there is nothing to walk.</summary>
[Table("Addresses")]
public class AddressEntity
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string City { get; set; } = "";
    public string? Region { get; set; }
    public string PostalCode { get; set; } = "";
    public int CountryId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
