# Switchback

A small online shop on EF Core and SQL Server, with 100,000 orders, that converts one feature slice — the
**Recent orders** list — to FunkyORM the Slim Domain way, and measures what changed. It is the companion
solution for the Slim Domain paper, the [labs](../../labs/) and the video series.

## Run it

**Prerequisites:** the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), and SQL Server
Express LocalDB (Visual Studio installs it with its .NET workloads; `sqllocaldb info` lists `MSSQLLocalDB`
if you have it). Any other SQL Server works too; see *Using another SQL Server* below.

```bash
dotnet run --project src/Switchback.Demo
```

Or open `Switchback.slnx` in Visual Studio and press **F5** with `Switchback.Demo` as the startup project.

The first run creates the database in LocalDB and seeds it (20 countries, 5,000 customers, about 10,000
addresses, 100,000 orders; 15–30 seconds, in one transaction). Every later run goes straight to the scoreboard:

```
Recent orders, page 0 (50 rows)                 fields loaded   fields used   objects tracked   median ms*
--------------------------------------------------------------------------------------------------------------
1  EF Core — Include chain                                 36             5               166          2.2
2  EF Core — projection                                     5             5                 0          1.0
3  FunkyORM — read model                                    8             5                 0          0.8

Rows identical across variants: YES (sha256 e970…1100)
* this machine, warm cache, median of 20 runs after 3 warm-ups. Illustrative only.
```

The three rows are three implementations of the same page, run against the same data and checked for
identical results. The columns:

- **fields loaded** — columns in the outer `SELECT` list of the SQL that was actually sent.
- **fields used** — what the screen shows: five.
- **objects tracked** — what the EF change tracker is holding after the query.
- **median ms** — the query plus the mapping to the response type, fresh context or provider each run.

### Commands

```bash
dotnet run --project src/Switchback.Demo -- scoreboard [--page N] [--country NAME]
dotnet run --project src/Switchback.Demo -- sql include|projection|funky [--page N] [--country NAME]
dotnet run --project src/Switchback.Demo -- seed      # create the schema and the data if missing
dotnet run --project src/Switchback.Demo -- reset     # clear the four tables and seed them again
dotnet test                                           # parity, SQL shape, architecture and guard tests
dotnet run --project src/Switchback.Web               # minimal API on http://localhost:5080
```

`sql` prints the statement one variant sends, formatted for the screen, and counts its columns.
`--country Canada` filters on the ship-to country, a value two joins away from the order.

The web app serves the same page three ways: `/orders/recent` (FunkyORM), `/orders/recent/ef-include` and
`/orders/recent/ef-projection`. `src/Switchback.Web/Switchback.Web.http` has ready-made requests.

## What is where

```
Switchback.slnx
src/
  Switchback.Demo/                                  the scoreboard (F5 here). Measurement tooling, not application code.
  Switchback.Web/                                   minimal API: three endpoints, one per implementation
  Switchback.Application.Services/                  the service gate: the only application code that talks to an ORM
    EfIncludeChainRecentOrdersService.cs            1. the mistake: Include / ThenInclude, 36 columns
    EfProjectionRecentOrdersService.cs              2. the EF fix: Select into the contract, 5 columns
    RecentOrdersService.cs                          3. the FunkyORM read model, 8 columns
    ServiceBase.cs                                  the persistence gate: private provider, IReadModel write refusal
  Switchback.Data/                                  EF Core: the DbContext, the model with navigation properties, migrations, seed
  Switchback.Infrastructure.Persistence.Entities/   FunkyORM: one slim class per table, IReadModel, RecentOrderRow
  Switchback.Shared.Contracts/                      RecentOrderResponse: the published shape, an allow-list
tests/
  Switchback.Tests/                                 parity across variants, column counts, architecture gate, guard
```

Dependency direction: `Web` and `Demo` → `Application.Services` → `Entities`, `Data`, `Contracts`. The two
hosts also call `Switchback.Data` directly for the bootstrap (resolve the connection string, migrate, seed).
No assembly other than the entities and the services references `Funcular.Data.Orm`, and none other than
`Switchback.Data` and the services references `Microsoft.EntityFrameworkCore`; the architecture tests fail
if that changes.

The schema is owned by EF Core migrations in every version of the story. That is the realistic case: an
existing EF application converts one slice, and EF keeps the migrations and everything else.

## The slice, in three files

The read model declares the screen's shape once (`RecentOrderRow.cs`):

```csharp
[Table("Orders")]
public sealed class RecentOrderRow : IReadModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public DateTime OrderedUtc { get; set; }
    public decimal Total { get; set; }

    [RemoteLink(typeof(CustomerEntity))] public int CustomerId { get; set; }
    [RemoteLink(typeof(AddressEntity))]  public int? ShipToAddressId { get; set; }

    [RemoteProperty(typeof(CustomerEntity), nameof(CustomerId), nameof(CustomerEntity.Name))]
    public string CustomerName { get; set; } = "";

    [RemoteProperty(typeof(CountryEntity),
        nameof(ShipToAddressId), nameof(AddressEntity.CountryId), nameof(CountryEntity.Name))]
    public string? ShipToCountryName { get; set; }
}
```

The service queries it (`RecentOrdersService.cs`):

```csharp
return Query<RecentOrderRow>()
    .OrderByDescending(r => r.OrderedUtc)
    .ThenByDescending(r => r.Id)
    .Skip(page * pageSize)
    .Take(pageSize)
    .ToList();
```

And FunkyORM sends this (the `SELECT` list is wrapped here for width; `sql funky` prints it on one line):

```sql
SELECT Orders.Id, Orders.OrderNumber, Orders.OrderedUtc, Orders.Total, Orders.CustomerId, Orders.ShipToAddressId,
       [Customers_0].Name AS [CustomerName], [Countries_0].Name AS [ShipToCountryName]
FROM Orders
  LEFT JOIN Customers [Customers_0] ON Orders.CustomerId = [Customers_0].Id
  LEFT JOIN Addresses [Addresses_0] ON Orders.ShipToAddressId = [Addresses_0].Id
  LEFT JOIN Countries [Countries_0] ON [Addresses_0].CountryId = [Countries_0].Id
ORDER BY Orders.OrderedUtc DESC, Orders.Id DESC
OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY
```

The full before-and-after, with the EF SQL, is in [`../../docs/before-after.md`](../../docs/before-after.md).

## Using another SQL Server

Set `SWITCHBACK_CONNECTION` to any SQL Server connection string. The database is created and seeded on first
use, so an empty server is fine:

```bash
# SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Switchback!2026" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
export SWITCHBACK_CONNECTION="Server=localhost,1433;Database=Switchback;User Id=sa;Password=Switchback!2026;TrustServerCertificate=True;"
dotnet run --project src/Switchback.Demo
```

The web app also reads `ConnectionStrings:Switchback` from `appsettings.json`. The environment variable
wins over both.

## Where the LocalDB database lives

`%LOCALAPPDATA%\Switchback\Switchback.mdf` (and its `.ldf`), attached to the `MSSQLLocalDB` instance under
the name `Switchback`. `dotnet run --project src/Switchback.Demo -- reset` clears the four tables and seeds
them again, in one transaction; it never drops the database or touches other tables, so it is safe to point
`SWITCHBACK_CONNECTION` at a shared server. To remove the LocalDB database for good:
`sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE Switchback"` (this deletes the files too).

If the four tables hold anything other than the seed (a lab changed the data, or the database was someone
else's), the sample refuses to run and says so; `reset` is the way back.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `A network-related or instance-specific error…` on `(localdb)\MSSQLLocalDB` | LocalDB is not installed, or the instance is missing. Run `sqllocaldb info`; create one with `sqllocaldb create MSSQLLocalDB`. Or point `SWITCHBACK_CONNECTION` at another SQL Server. |
| `Logon failed for login … due to trigger execution` (error 17892) | Your LocalDB instance has a server logon trigger that rejects connections. That is machine-specific; use another instance (`sqllocaldb create Switchback -s`, then set `SWITCHBACK_CONNECTION` to `Server=(localdb)\Switchback;…`) or another SQL Server. |
| `Database 'Switchback' already exists` after deleting the `.mdf` by hand | LocalDB still has the name registered. `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE Switchback"`, then run again. |
| `Generated ORDER BY clause: …` lines in the web console | A leftover debug line in FunkyORM 3.10.0 ([`FUNKYORM_FEEDBACK.md`](FUNKYORM_FEEDBACK.md)). Harmless; the scoreboard filters it. |
| Seeding is slow | It inserts 115,000 rows through EF in batches; 15–30 seconds depending on the machine. It runs once. |

## Versions

.NET 10 · EF Core 10.0.12 · FunkyORM (`Funcular.Data.Orm`) 3.10.0 · Bogus 35.6.5 · xUnit v3 · NetArchTest.
Package versions are pinned in `Directory.Packages.props`.

FunkyORM is young and moving fast; the [FUNKYORM_FEEDBACK.md](FUNKYORM_FEEDBACK.md) file records what the
sample ran into, for the maintainers.
