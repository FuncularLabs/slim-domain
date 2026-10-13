using System.ComponentModel.DataAnnotations.Schema;

namespace Switchback.Infrastructure.Persistence.Entities;

/// <summary>The Countries table and nothing more.</summary>
[Table("Countries")]
public class CountryEntity
{
    public int Id { get; set; }
    public string IsoCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
}
