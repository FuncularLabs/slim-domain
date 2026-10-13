using Switchback.Data;

namespace Switchback.Tests;

/// <summary>Creates and seeds the database once per test run (a no-op when it already exists).</summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; } = SwitchbackConnection.Resolve();

    public async ValueTask InitializeAsync() =>
        await SwitchbackDatabase.EnsureReadyAsync(ConnectionString, ct: TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
