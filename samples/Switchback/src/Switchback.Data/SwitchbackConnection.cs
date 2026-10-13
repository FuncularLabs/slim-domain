using Microsoft.Data.SqlClient;

namespace Switchback.Data;

/// <summary>
/// Where the database lives. The default is SQL Server LocalDB, which Visual Studio installs, with the
/// database files under <c>%LOCALAPPDATA%\Switchback</c>: a fresh clone runs with no setup and leaves
/// nothing behind but that folder. Set <c>SWITCHBACK_CONNECTION</c> to use any other SQL Server.
/// </summary>
public static class SwitchbackConnection
{
    public const string EnvironmentVariable = "SWITCHBACK_CONNECTION";
    public const string DatabaseName = "Switchback";
    public const string LocalDbInstance = @"(localdb)\MSSQLLocalDB";

    /// <summary>The environment variable first, then the configured value, then LocalDB.</summary>
    public static string Resolve(string? configured = null)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        return LocalDb();
    }

    /// <summary>LocalDB, with the .mdf and .ldf files in a folder of their own rather than the home directory.</summary>
    public static string LocalDb()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseName);
        Directory.CreateDirectory(folder);

        return new SqlConnectionStringBuilder
        {
            DataSource = LocalDbInstance,
            InitialCatalog = DatabaseName,
            AttachDBFilename = Path.Combine(folder, $"{DatabaseName}.mdf"),
            IntegratedSecurity = true,
            TrustServerCertificate = true,
        }.ConnectionString;
    }

    /// <summary>The server, the database, and the file if one is attached. Safe to print.</summary>
    public static string Describe(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var text = $"{builder.DataSource} / {builder.InitialCatalog}";
        return string.IsNullOrEmpty(builder.AttachDBFilename) ? text : $"{text} ({builder.AttachDBFilename})";
    }
}
