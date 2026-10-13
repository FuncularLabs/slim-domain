using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Switchback.Data.Seeding;

namespace Switchback.Data;

/// <summary>Creates the schema and seeds the data on first run; a no-op afterwards.</summary>
public static class SwitchbackDatabase
{
    public static async Task EnsureReadyAsync(string connectionString, Action<string>? progress = null, CancellationToken ct = default)
    {
        EnsureAttachFolderExists(connectionString);

        await using var db = new SwitchbackDbContext(SwitchbackDbContext.CreateOptions(connectionString));

        progress?.Invoke("Applying migrations…");
        await db.Database.MigrateAsync(ct);

        var counts = await TableCounts.ReadAsync(db, ct);
        if (counts == TableCounts.Seeded)
        {
            progress?.Invoke("Database already seeded.");
            return;
        }

        if (!counts.IsEmpty)
        {
            // Not the data set the labs and videos quote, and not ours to overwrite without being asked.
            throw new InvalidOperationException(
                $"The database has {counts}; the seed produces {TableCounts.Seeded}. " +
                "Run `Switchback.Demo reset` to clear those four tables and seed them again, " +
                "or point SWITCHBACK_CONNECTION at another database.");
        }

        // Empty tables, but their identities may have moved (a lab deleted the rows); the seed restarts them.
        await SwitchbackSeeder.SeedAsync(db, progress, ct);
    }

    /// <summary>
    /// Clears the four tables and seeds them again, in one transaction. The database itself, and anything
    /// else in it, is left alone. Use it after changing the seed, or after a lab that changed the data.
    /// </summary>
    public static async Task ResetAsync(string connectionString, Action<string>? progress = null, CancellationToken ct = default)
    {
        EnsureAttachFolderExists(connectionString);

        await using var db = new SwitchbackDbContext(SwitchbackDbContext.CreateOptions(connectionString));

        progress?.Invoke("Applying migrations…");
        await db.Database.MigrateAsync(ct);

        await SwitchbackSeeder.SeedAsync(db, progress, ct);
    }

    // SQL Server creates the .mdf but not the folder it goes in.
    private static void EnsureAttachFolderExists(string connectionString)
    {
        var file = new SqlConnectionStringBuilder(connectionString).AttachDBFilename;
        if (!string.IsNullOrEmpty(file) && Path.GetDirectoryName(file) is { Length: > 0 } folder)
            Directory.CreateDirectory(folder);
    }
}
