using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Switchback.Data.Model;

namespace Switchback.Data;

/// <summary>
/// The EF Core context. It owns the schema (migrations) and models the four tables with navigation
/// properties, the way most EF applications do. Lazy loading is off, which is the EF default.
/// </summary>
public sealed class SwitchbackDbContext(DbContextOptions<SwitchbackDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(e =>
        {
            e.ToTable("Countries");
            e.Property(c => c.IsoCode).HasColumnType("char(2)");
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.CurrencyCode).HasColumnType("char(3)");
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.Property(c => c.Name).HasMaxLength(150);
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.Phone).HasMaxLength(40);
            e.Property(c => c.LoyaltyTier).HasMaxLength(20);
            e.Property(c => c.CreditLimit).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Address>(e =>
        {
            e.ToTable("Addresses");
            e.Property(a => a.Line1).HasMaxLength(200);
            e.Property(a => a.Line2).HasMaxLength(200);
            e.Property(a => a.City).HasMaxLength(100);
            e.Property(a => a.Region).HasMaxLength(100);
            e.Property(a => a.PostalCode).HasMaxLength(20);
            e.Property(a => a.Latitude).HasPrecision(9, 6);
            e.Property(a => a.Longitude).HasPrecision(9, 6);
            e.HasOne(a => a.Customer).WithMany(c => c.Addresses).HasForeignKey(a => a.CustomerId);
            e.HasOne(a => a.Country).WithMany().HasForeignKey(a => a.CountryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.Property(o => o.OrderNumber).HasMaxLength(20);
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.Status).HasMaxLength(20);
            e.Property(o => o.Subtotal).HasPrecision(18, 2);
            e.Property(o => o.Tax).HasPrecision(18, 2);
            e.Property(o => o.ShippingFee).HasPrecision(18, 2);
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId);
            // Restrict: SQL Server rejects a second cascade path to Orders (Customers → Addresses → Orders).
            e.HasOne(o => o.ShipToAddress).WithMany().HasForeignKey(o => o.ShipToAddressId).OnDelete(DeleteBehavior.Restrict);
            // The Recent orders page sorts newest first; this index backs the paging.
            e.HasIndex(o => new { o.OrderedUtc, o.Id }).IsDescending().HasDatabaseName("IX_Orders_OrderedUtc_Id");
        });
    }

    /// <summary>Options for a context outside DI (the runner, the seed, the tests), with optional interceptors.</summary>
    public static DbContextOptions<SwitchbackDbContext> CreateOptions(string connectionString, params IInterceptor[] interceptors) =>
        new DbContextOptionsBuilder<SwitchbackDbContext>()
            .UseSqlServer(connectionString)
            .AddInterceptors(interceptors)
            .Options;
}
