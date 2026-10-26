# FunkyORM feedback from building Switchback

What the sample ran into while using FunkyORM, with the version, the provider, the construct, what was
expected, and what happened. The FunkyORM maintainers read this file; it feeds the backlog.

Format: one entry per finding, newest first, with its current status.

---

## 2026-10-26 — Moved to 3.12.1: verified

- **Version / provider:** Funcular.Data.Orm 3.12.1 (NuGet, 2026-10-26), SQL Server (LocalDB and SQL Server 2019).
- **Result:** the scoreboard is unchanged (36 / 5 / 8 columns, 166 tracked objects, rows `e970…1100`); all 33
  tests pass; `dotnet list package --vulnerable --include-transitive` reports nothing (the Pkcs pin in
  `Directory.Packages.props` stays for EF Core's SqlClient path, which 3.12.1 doesn't cover).
- **Also checked on the sample's data** (a probe outside the repo), each against LINQ to objects over the same rows
  except where noted:
  `Count(o => o.ShipToAddressId != id)` includes the 2,960 NULL rows (99,976; 3.11.0 gave 97,016);
  `o.ShipToAddressId == nullVariable` selects them (2,960; 3.11.0 gave 0); `All` over a nullable column answers
  as C# does; `Average` over `decimal` and over `int`; `.Year` in `Where`; `Contains("%")` asserted to match no row.
  Nothing is written to stdout during a query.

## 2026-10-12 — `Generated ORDER BY clause: …` printed to stdout

- **Status: fixed in 3.11.0.** Verified on the published 3.11.0, 3.12.0 and 3.12.1 packages (nothing written during a
  query; 2026-10-26). The sample's `ConsoleNoiseFilter` was removed on 2026-10-26.
- **Version / provider:** Funcular.Data.Orm 3.10.0, SQL Server.
- **Construct:** any ordered LINQ query, for example
  `provider.Query<RecentOrderRow>().OrderByDescending(r => r.OrderedUtc).ThenByDescending(r => r.Id).Skip(0).Take(50).ToList()`.
- **Expected:** nothing on stdout. SQL goes to `provider.Log` only.
- **Actual:** one line per ORDER BY key was written to the console:
  ```
  Generated ORDER BY clause: ORDER BY Orders.OrderedUtc DESC
  Generated ORDER BY clause: ORDER BY Orders.OrderedUtc DESC, Orders.Id DESC
  ```

## 2026-10-12 — `Skip` with a negative count is clamped to zero, silently

- **Status: documented, by design.** `Advanced.md` §5 says `Skip(n < 0)` acts as `Skip(0)` (since 3.10.0), and
  the maintainers list it as a deliberate difference from EF Core (which sends a negative `OFFSET` that SQL Server
  rejects). The sample validates `page` in `Paging.Validate`, so both roads refuse the same inputs.
- **Version / provider:** Funcular.Data.Orm 3.10.0, SQL Server.
- **Construct:** `Query<RecentOrderRow>().OrderByDescending(r => r.OrderedUtc).Skip(-50).Take(50)`.
- **Actual:** page 0 comes back; the statement sent `OFFSET 0 ROWS`.

## 2026-10-12 — `IOrmDataProvider` does not extend `IDisposable`

- **Status: open.** Recorded by the maintainers as a 4.0 candidate: adding a base interface is a breaking change
  for implementers. Dependency injection disposes the concrete provider correctly.
- **Version:** Funcular.Data.Orm 3.10.0; still true in 3.12.1.
- **Construct:** holding a provider as `IOrmDataProvider` (the interface the services depend on) and disposing it.
- **Expected:** `using var provider = …` compiles against the interface.
- **Actual:** `OrmDataProvider` implements `IDisposable`, but the interface does not, so callers holding the
  interface write `(provider as IDisposable)?.Dispose()`. See `Switchback.Demo/Variants.cs`.

## Constructs the sample uses that work as documented on 3.12.1

- `[Table]` on classes whose names differ from the tables; column matching by convention.
- `[RemoteLink]` on nullable and non-nullable keys; `[RemoteProperty]` one hop and two hops through a nullable key.
- `Where` on a remote property with a captured variable (`r.ShipToCountryName == shipToCountry`).
- `OrderByDescending` / `ThenByDescending` on own columns of an entity with remote joins, then `Skip` / `Take`.
