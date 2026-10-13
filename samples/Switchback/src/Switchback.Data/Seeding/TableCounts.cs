using Microsoft.EntityFrameworkCore;

namespace Switchback.Data.Seeding;

/// <summary>Row counts of the four tables: the readiness check compares them with what the seed produces.</summary>
public readonly record struct TableCounts(int Countries, int Customers, int Addresses, int Orders)
{
    public static readonly TableCounts Empty = new(0, 0, 0, 0);

    public static readonly TableCounts Seeded = new(
        SwitchbackSeeder.CountryCount, SwitchbackSeeder.CustomerCount, SwitchbackSeeder.AddressCount, SwitchbackSeeder.OrderCount);

    public bool IsEmpty => this == Empty;

    public static async Task<TableCounts> ReadAsync(SwitchbackDbContext db, CancellationToken ct) => new(
        await db.Countries.CountAsync(ct),
        await db.Customers.CountAsync(ct),
        await db.Addresses.CountAsync(ct),
        await db.Orders.CountAsync(ct));

    public override string ToString() =>
        $"{Countries:N0} countries, {Customers:N0} customers, {Addresses:N0} addresses, {Orders:N0} orders";
}
