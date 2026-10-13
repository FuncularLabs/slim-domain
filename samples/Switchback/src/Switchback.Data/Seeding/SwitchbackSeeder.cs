using Bogus;
using Microsoft.EntityFrameworkCore;
using Switchback.Data.Model;

namespace Switchback.Data.Seeding;

/// <summary>
/// Deterministic seed: a fixed random seed and a fixed reference date produce the same rows on every
/// machine, so the scoreboard numbers and the row hash in the labs and videos reproduce exactly.
/// </summary>
public static class SwitchbackSeeder
{
    public const int RandomSeed = 8675309;
    public const int CountryCount = 20;
    public const int CustomerCount = 5_000;
    public const int AddressCount = 10_037;   // what the seed produces: 1–3 per customer
    public const int OrderCount = 100_000;
    public static readonly DateTime ReferenceUtc = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private const int CustomerBatch = 1_000;
    private const int OrderBatch = 10_000;

    /// <summary>
    /// Clears the four tables (a no-op when they are empty), restarts their identities, and seeds them, all in
    /// one transaction. Nothing else in the database is touched.
    /// </summary>
    public static async Task SeedAsync(SwitchbackDbContext db, Action<string>? progress = null, CancellationToken ct = default)
    {
        Randomizer.Seed = new Random(RandomSeed);
        var random = new Randomizer();

        // One transaction: the seed is all or nothing, so an interrupted run leaves everything as it was and
        // the next run starts over. Batches are saved and then dropped from the change tracker.
        db.ChangeTracker.AutoDetectChangesEnabled = false;   // bulk insert: nothing to detect
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await ClearAsync(db, progress, ct);

        progress?.Invoke($"Seeding {CountryCount} countries…");
        var countries = Countries
            .Select(c => new Country { IsoCode = c.IsoCode, Name = c.Name, CurrencyCode = c.CurrencyCode })
            .ToList();
        db.Countries.AddRange(countries);
        await db.SaveChangesAsync(ct);
        var countryIds = countries.Select(c => c.Id).ToArray();
        db.ChangeTracker.Clear();

        progress?.Invoke($"Seeding {CustomerCount:N0} customers with 1–3 addresses each…");
        var customerFaker = CustomerFaker();
        var addressFaker = AddressFaker(countryIds);
        for (var done = 0; done < CustomerCount; done += CustomerBatch)
        {
            var customers = customerFaker.Generate(Math.Min(CustomerBatch, CustomerCount - done));
            foreach (var customer in customers)
                customer.Addresses = addressFaker.Generate(random.Int(1, 3));
            db.Customers.AddRange(customers);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }

        var addressIdsByCustomer = (await db.Addresses.Select(a => new { a.CustomerId, a.Id }).ToListAsync(ct))
            .GroupBy(a => a.CustomerId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.Id).Order().ToArray());
        var customerIds = addressIdsByCustomer.Keys.Order().ToArray();

        progress?.Invoke($"Seeding {OrderCount:N0} orders over 24 months (about 3% without a ship-to address)…");
        var orderFaker = OrderFaker(customerIds, addressIdsByCustomer);
        for (var done = 0; done < OrderCount; done += OrderBatch)
        {
            var orders = orderFaker.Generate(Math.Min(OrderBatch, OrderCount - done));
            for (var i = 0; i < orders.Count; i++)
                orders[i].OrderNumber = $"SW-{done + i + 1:D8}";
            db.Orders.AddRange(orders);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            progress?.Invoke($"  {done + orders.Count:N0} orders");
        }

        await transaction.CommitAsync(ct);
        progress?.Invoke("Seed complete.");
    }

    // Deletes the sample's rows only, children first, and restarts each identity, so a reset equals a fresh
    // database. Other tables in the database are never touched. Table names are literals: EF parameterizes
    // these interpolations, and a table name cannot be a parameter.
    private static async Task ClearAsync(SwitchbackDbContext db, Action<string>? progress, CancellationToken ct)
    {
        var deleted = 0;

        deleted += await ClearTableAsync(db, ct,
            $"DELETE FROM [Orders]",
            $"SELECT CASE WHEN last_value IS NULL THEN 0 ELSE 1 END AS [Value] FROM sys.identity_columns WHERE object_id = OBJECT_ID('Orders')",
            $"DBCC CHECKIDENT ('Orders', RESEED, 0)");

        deleted += await ClearTableAsync(db, ct,
            $"DELETE FROM [Addresses]",
            $"SELECT CASE WHEN last_value IS NULL THEN 0 ELSE 1 END AS [Value] FROM sys.identity_columns WHERE object_id = OBJECT_ID('Addresses')",
            $"DBCC CHECKIDENT ('Addresses', RESEED, 0)");

        deleted += await ClearTableAsync(db, ct,
            $"DELETE FROM [Customers]",
            $"SELECT CASE WHEN last_value IS NULL THEN 0 ELSE 1 END AS [Value] FROM sys.identity_columns WHERE object_id = OBJECT_ID('Customers')",
            $"DBCC CHECKIDENT ('Customers', RESEED, 0)");

        deleted += await ClearTableAsync(db, ct,
            $"DELETE FROM [Countries]",
            $"SELECT CASE WHEN last_value IS NULL THEN 0 ELSE 1 END AS [Value] FROM sys.identity_columns WHERE object_id = OBJECT_ID('Countries')",
            $"DBCC CHECKIDENT ('Countries', RESEED, 0)");

        if (deleted > 0)
            progress?.Invoke($"Cleared {deleted:N0} rows from Orders, Addresses, Customers and Countries.");
    }

    // RESEED 0 makes the next identity 1 on a table that has ever had a row, but 0 on one that never has.
    // sys.identity_columns.last_value is NULL exactly until the first insert, so the identity is restarted
    // only for a table with a history. Returns the number of rows deleted.
    private static async Task<int> ClearTableAsync(SwitchbackDbContext db, CancellationToken ct,
        FormattableString delete, FormattableString hadRows, FormattableString reseed)
    {
        var deleted = await db.Database.ExecuteSqlAsync(delete, ct);

        if (await db.Database.SqlQuery<int>(hadRows).SingleAsync(ct) == 1)
            await db.Database.ExecuteSqlAsync(reseed, ct);

        return deleted;
    }

    private static Faker<Customer> CustomerFaker() => new Faker<Customer>()
        .RuleFor(c => c.Name, f => f.Name.FullName())
        .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Name.Split(' ')[0], c.Name.Split(' ')[^1]).ToLowerInvariant())
        .RuleFor(c => c.Phone, f => f.Random.Bool(0.8f) ? f.Phone.PhoneNumber("###-###-####") : null)
        .RuleFor(c => c.LoyaltyTier, f => f.Random.WeightedRandom(LoyaltyTiers.All, [0.7f, 0.2f, 0.1f]))
        .RuleFor(c => c.CreditLimit, f => Math.Round(f.Random.Decimal(500m, 25_000m), 2))
        .RuleFor(c => c.Notes, f => f.Random.Bool(0.3f) ? f.Lorem.Sentences(f.Random.Int(2, 3)) : null)
        .RuleFor(c => c.CreatedUtc, f => Utc(f.Date.Between(ReferenceUtc.AddYears(-3), ReferenceUtc.AddMonths(-24))))
        .RuleFor(c => c.ModifiedUtc, (f, c) => Utc(f.Date.Between(c.CreatedUtc, ReferenceUtc)));

    private static Faker<Address> AddressFaker(int[] countryIds) => new Faker<Address>()
        .RuleFor(a => a.Line1, f => f.Address.StreetAddress())
        .RuleFor(a => a.Line2, f => f.Random.Bool(0.25f) ? f.Address.SecondaryAddress() : null)
        .RuleFor(a => a.City, f => f.Address.City())
        .RuleFor(a => a.Region, f => f.Random.Bool(0.6f) ? f.Address.State() : null)
        .RuleFor(a => a.PostalCode, f => f.Address.ZipCode())
        .RuleFor(a => a.CountryId, f => f.PickRandom(countryIds))
        .RuleFor(a => a.Latitude, f => f.Random.Bool(0.5f) ? Math.Round((decimal)f.Address.Latitude(), 6) : null)
        .RuleFor(a => a.Longitude, (f, a) => a.Latitude is null ? null : Math.Round((decimal)f.Address.Longitude(), 6));

    private static Faker<Order> OrderFaker(int[] customerIds, Dictionary<int, int[]> addressIdsByCustomer) => new Faker<Order>()
        .RuleFor(o => o.CustomerId, f => f.PickRandom(customerIds))
        .RuleFor(o => o.ShipToAddressId, (f, o) => f.Random.Bool(0.97f) ? f.PickRandom(addressIdsByCustomer[o.CustomerId]) : null)
        .RuleFor(o => o.OrderedUtc, f => Utc(f.Date.Between(ReferenceUtc.AddMonths(-24), ReferenceUtc)))
        .RuleFor(o => o.Status, f => f.Random.WeightedRandom(OrderStatus.All, [0.10f, 0.15f, 0.70f, 0.05f]))
        .RuleFor(o => o.Subtotal, f => Math.Round(f.Random.Decimal(10m, 2_000m), 2))
        .RuleFor(o => o.Tax, (f, o) => Math.Round(o.Subtotal * f.Random.Decimal(0m, 0.10m), 2))
        .RuleFor(o => o.ShippingFee, f => Math.Round(f.Random.Decimal(0m, 50m), 2))
        .RuleFor(o => o.Total, (_, o) => o.Subtotal + o.Tax + o.ShippingFee)
        .RuleFor(o => o.InternalNotes, f => f.Random.Bool(0.2f) ? f.Lorem.Sentence() : null)
        .RuleFor(o => o.CreatedUtc, (_, o) => o.OrderedUtc)
        .RuleFor(o => o.ModifiedUtc, (f, o) => o.OrderedUtc.AddHours(f.Random.Int(0, 72)));

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static readonly (string IsoCode, string Name, string CurrencyCode)[] Countries =
    [
        ("US", "United States", "USD"), ("CA", "Canada", "CAD"), ("MX", "Mexico", "MXN"),
        ("GB", "United Kingdom", "GBP"), ("IE", "Ireland", "EUR"), ("FR", "France", "EUR"),
        ("DE", "Germany", "EUR"), ("ES", "Spain", "EUR"), ("IT", "Italy", "EUR"),
        ("NL", "Netherlands", "EUR"), ("SE", "Sweden", "SEK"), ("NO", "Norway", "NOK"),
        ("CH", "Switzerland", "CHF"), ("PL", "Poland", "PLN"), ("JP", "Japan", "JPY"),
        ("AU", "Australia", "AUD"), ("NZ", "New Zealand", "NZD"), ("BR", "Brazil", "BRL"),
        ("ZA", "South Africa", "ZAR"), ("IN", "India", "INR"),
    ];
}
