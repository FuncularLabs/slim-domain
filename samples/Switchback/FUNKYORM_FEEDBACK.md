# FunkyORM feedback from building Switchback

What the sample ran into while using FunkyORM, with the version, the provider, the construct, what was
expected, and what happened. The FunkyORM maintainers read this file; it feeds the backlog.

Format: one entry per finding, newest first.

---

## 2026-10-12 — `Generated ORDER BY clause: …` printed to stdout

- **Version / provider:** Funcular.Data.Orm 3.10.0, SQL Server.
- **Construct:** any ordered LINQ query, for example
  `provider.Query<RecentOrderRow>().OrderByDescending(r => r.OrderedUtc).ThenByDescending(r => r.Id).Skip(0).Take(50).ToList()`.
- **Expected:** nothing on stdout. SQL goes to `provider.Log` only.
- **Actual:** one line per ORDER BY key is written to the console:
  ```
  Generated ORDER BY clause: ORDER BY Orders.OrderedUtc DESC
  Generated ORDER BY clause: ORDER BY Orders.OrderedUtc DESC, Orders.Id DESC
  ```
- **Effect:** noise in any console host. The scoreboard would print 46 of these before its table.
- **Workaround in this sample:** `Switchback.Demo/ConsoleNoiseFilter.cs` drops lines with that prefix from
  `Console.Out`. The web host prints them among its logs.
- **Status:** known to the maintainers as a leftover debug line; removal planned.

## 2026-10-12 — `Skip` with a negative count is clamped to zero, silently

- **Version / provider:** Funcular.Data.Orm 3.10.0, SQL Server.
- **Construct:** `Query<RecentOrderRow>().OrderByDescending(r => r.OrderedUtc).Skip(-50).Take(50)`.
- **Expected:** either the LINQ-to-Objects behavior (a negative count means skip nothing, which is what
  happened) or an `ArgumentOutOfRangeException`; the point is that it should be documented.
- **Actual:** page 0 comes back; the statement sent `OFFSET 0 ROWS`. EF Core sends a negative OFFSET and
  SQL Server rejects it. The two libraries disagree, and nothing says so.
- **Effect:** a paging bug in a caller is invisible on the FunkyORM road. The sample validates `page` in its
  services so both roads refuse.
- **Status:** reported here; a documentation note would settle it.

## 2026-10-12 — `IOrmDataProvider` does not extend `IDisposable`

- **Version:** Funcular.Data.Orm 3.10.0.
- **Construct:** holding a provider as `IOrmDataProvider` (the interface the services depend on) and disposing it.
- **Expected:** `using var provider = …` compiles against the interface.
- **Actual:** `OrmDataProvider` implements `IDisposable`, but the interface does not, so callers holding
  the interface must write `(provider as IDisposable)?.Dispose()`. Dependency injection disposes it correctly
  because it checks the concrete object.
- **Effect:** minor API friction. See `Switchback.Demo/Variants.cs`.
- **Status:** reported here; not blocking.

## Constructs the sample uses that worked as documented on 3.10.0

- `[Table]` on classes whose names differ from the tables; column matching by convention.
- `[RemoteLink]` on nullable and non-nullable keys; `[RemoteProperty]` one hop and two hops through a nullable key.
- `Where` on a remote property with a captured variable (`r.ShipToCountryName == shipToCountry`).
- `OrderByDescending` / `ThenByDescending` on own columns of an entity with remote joins, then `Skip` / `Take`.
