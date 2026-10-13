# Lab 00 — Setup and tour

**Goal:** run the Switchback sample, know where everything is, and know how to put the database back the
way you found it. About 20 minutes.

## 1. Run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and SQL Server Express
LocalDB (Visual Studio installs it; `sqllocaldb info` should list `MSSQLLocalDB`). Then:

```bash
cd samples/Switchback
dotnet run --project src/Switchback.Demo
```

Or open `Switchback.slnx` in Visual Studio and press F5. The first run creates and seeds the database (15–30
seconds), then prints the scoreboard. Run it again: this time it goes straight to the scoreboard.

If you would rather use another SQL Server, set `SWITCHBACK_CONNECTION` first; the
[sample README](../samples/Switchback/README.md#using-another-sql-server) has a Docker one-liner.

**Check:** the scoreboard shows three rows, ends with `Rows identical across variants: YES`, and the hash
is `e970…1100`. The hash is the same on every machine, because the seed is deterministic.

## 2. Read the scoreboard

```
Recent orders, page 0 (50 rows)                 fields loaded   fields used   objects tracked   median ms*
--------------------------------------------------------------------------------------------------------------
1  EF Core — Include chain                                 36             5               166          2.2
2  EF Core — projection                                     5             5                 0          1.0
3  FunkyORM — read model                                    8             5                 0          0.8
```

Three implementations of the same page, run against the same data. The milliseconds are illustrative and
will differ on your machine; the other three columns will not. **Fields loaded against fields used** is the
number every lab comes back to.

## 3. See the SQL

```bash
dotnet run --project src/Switchback.Demo -- sql include
dotnet run --project src/Switchback.Demo -- sql projection
dotnet run --project src/Switchback.Demo -- sql funky
```

Each prints the statement that variant actually sent, and counts the columns in its outer `SELECT` list.
That counter is what the scoreboard uses; it is unit-tested in `SqlColumnCounterTests`.

## 4. Find your way around

```
src/Switchback.Application.Services/     the three implementations, and ServiceBase
src/Switchback.Data/                      EF Core: model with navigation properties, DbContext, migrations, seed
src/Switchback.Infrastructure.Persistence.Entities/   FunkyORM: slim entities and the read model
src/Switchback.Shared.Contracts/          RecentOrderResponse
src/Switchback.Web/                       the same page as three HTTP endpoints
src/Switchback.Demo/                      the scoreboard
tests/Switchback.Tests/                   parity, shape, architecture, guard
```

Open `RecentOrdersService.cs` and `RecentOrderRow.cs` first; they are the whole FunkyORM slice. Then
`EfIncludeChainRecentOrdersService.cs`, which is what the slice replaced.

## 5. Run the tests

```bash
dotnet test
```

**Check:** everything passes. The parity tests prove the three implementations agree; the architecture
tests prove no assembly outside the approved ones touches an ORM.

## 6. The web app (optional)

```bash
dotnet run --project src/Switchback.Web
```

Then open http://localhost:5080/orders/recent, or use the requests in `src/Switchback.Web/Switchback.Web.http`.
Watch the console: both EF and FunkyORM log the SQL they send.

## 7. Reset

```bash
dotnet run --project src/Switchback.Demo -- reset
```

Clears the four tables and seeds them again, in one transaction; the database itself stays. Do this
whenever a lab leaves the data in a state you do not want. If the data is not the seed, the sample refuses
to run until you do. The LocalDB files live in `%LOCALAPPDATA%\Switchback`.

## Where the labs go from here

[Lab 01](LAB-01-36-columns-to-show-5.md) takes the scoreboard apart: why the first row says 36, how the
second row gets to 5, and what the third row changes about where the discipline lives.
