namespace Switchback.Data.Model;

public class Country
{
    public int Id { get; set; }
    public string IsoCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
}
