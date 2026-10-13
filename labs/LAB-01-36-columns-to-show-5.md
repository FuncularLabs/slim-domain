# Lab 01 — 36 columns to show 5

**Goal:** see an ordinary EF query load 36 columns from four tables to display five fields; fix it the
proper EF way; then build the Slim Domain version with FunkyORM and understand what it changes. About an hour.

**Before you start:** [Lab 00](LAB-00-setup-and-tour.md). The narrative version of this lab, with all
the SQL, is [docs/before-after.md](../docs/before-after.md); keep it open.

The solution already contains the finished code for every step. The exercises ask you to write your own
version first and then compare. Work in a branch; `git checkout .` puts everything back.

## Step 1 — The mistake

Open `src/Switchback.Application.Services/EfIncludeChainRecentOrdersService.cs`. Read `GetPageAsync`.

Now look at the model it queries: `src/Switchback.Data/Model/Order.cs`, `Customer.cs`, `Address.cs`.
Every navigation property is an edge, and the query walks three of them.

```bash
dotnet run --project src/Switchback.Demo -- sql include
```

**Count** the columns in the outer `SELECT`. Find `[c].[Email]`, `[c].[CreditLimit]` and `[c].[Notes]`:
the screen shows none of them. Find `[o0].[InternalNotes]`.

**Question:** this code is readable, returns the right rows, and would pass most code reviews. What would a
reviewer have to know to object to it?

## Step 2 — Measure

The scoreboard's second-to-last column says 166 objects tracked. Where do they come from?

**Exercise:** in `EfIncludeChainRecentOrdersService.cs`, add `.AsNoTracking()` after `db.Orders`, run the
scoreboard, and note the tracked count and the columns. Then revert.

**Check:** tracked drops to 0; fields loaded stays at 36. Tracking is a cost, but it is not *the* cost.
The columns are.

## Step 3 — The proper EF fix

**Exercise:** without looking at `EfProjectionRecentOrdersService.cs`, write a version of `GetPageAsync`
that projects with `Select` straight into `RecentOrderResponse`. Keep the ordering and the paging. Mind
the nullable ship-to address.

Then compare with the solution's version and run:

```bash
dotnet run --project src/Switchback.Demo -- sql projection
dotnet test --filter "RecentOrdersParityTests"
```

**Check:** 5 columns; 0 tracked; parity tests green (your version must return exactly the same rows,
including the null country on the page that has one).

**Notes:**
- EF propagates null through `o.ShipToAddress!.Country.Name`; the `!` only silences the compiler.
- Do not add `AsNoTracking()`. A projection that returns no entity type is never tracked; adding it
  tells the next reader it was needed.

This is the right EF answer. Give it full credit.

## Step 4 — The catch

**Exercise:** a product owner asks for the customer's email on the list. Add it the way the two EF services
would each be changed, and run `sql` for each.

**Check:** the projection version adds one column (6). The Include version adds nothing: the email was
already loaded, and so was everything else. Notice which change is *easier* to make. The navigation
properties are still there, and nothing stops the next developer from reaching for `Include` again. The
discipline lives in people.

Revert both changes before continuing.

## Step 5 — The table, and nothing more

Open `src/Switchback.Infrastructure.Persistence.Entities/`. Four classes, one per table, each with that
table's columns and no navigation properties, no behavior, and one `[Table]` attribute because the class
names differ from the table names.

**Exercise:** `OrderEntity` has 13 properties. Query it on its own: in a scratch test, call
`provider.Query<OrderEntity>().OrderBy(o => o.Id).Take(1).ToList()` with `provider.Log = Console.WriteLine`.
How many columns? What would happen to the count if you deleted `InternalNotes` from the class?

**Check:** 13, then 12. FunkyORM loads the columns the class declares. The class *is* the projection.

## Step 6 — The read model

Open `RecentOrderRow.cs`. Read it slowly; it is the star of the episode.

- It is `[Table("Orders")]`, because its rows are orders.
- It is `sealed` and `IReadModel`: a projection, never a legal write (`ServiceBase` refuses it; see
  `ServiceBaseTests`).
- Four of its members are order columns, two are join keys, and two are values from other tables reached
  along a declared path of foreign keys.

**Exercise:** write your own read model for a different screen: "Customer directory": `Id`, `Name`, `Email`,
and the name of the country of the customer's *first* address. Stop when you notice the problem.

**Check:** you cannot express "first address" as a `[RemoteProperty]`: the path from a customer to an
address runs the wrong way (one customer, many addresses). A remote property is one value along a path of
single-valued keys. That is a limit, and a deliberate one; the collection shapes get their own lab (04).

```bash
dotnet run --project src/Switchback.Demo -- sql funky
```

**Check:** 8 columns. Say which three are not on the screen, and why each is there.

## Step 7 — The service, and the edge

Open `RecentOrdersService.cs` and `ServiceBase.cs`.

- The provider is private to the base. A service never touches the library directly.
- The operator order is `Where` → `OrderBy`/`ThenBy` → `Skip` → `Take` → `ToList()`. FunkyORM 3.10
  rejects anything after `Skip`/`Take`, by design.
- The service returns `RecentOrderRow`, not the contract. The mapping to `RecentOrderResponse` happens once,
  at the edge (`RecentOrderRowExtensions.ToResponse`, called from the web endpoint and the scoreboard),
  because the row carries internal ids that must not be published.

**Exercise:** run `dotnet run --project src/Switchback.Demo -- sql funky --country Canada`. Find the
`WHERE` clause.

**Check:** the filter lands on `[Countries_0].Name`, two joins away from the order, parameterized. The
service did not write a join; it wrote `Where(r => r.ShipToCountryName == shipToCountry)`.

## Step 8 — The guardrails

Open `tests/Switchback.Tests/ArchitectureTests.cs`.

**Exercise:** break the rule on purpose. In `src/Switchback.Web/Program.cs`, add
`using Funcular.Data.Orm.SqlServer;` and construct a `new SqlServerOrmDataProvider(connectionString)`
inline instead of calling `AddSwitchbackServices`. Run `dotnet test --filter "ArchitectureTests"`.

**Check:** `Only_the_approved_assemblies_reference_a_data_access_library` fails for `Switchback.Web` and
names `Program`. Revert.

**Exercise:** add a navigation property to `OrderEntity` (`public CustomerEntity Customer { get; set; }`).
Run the architecture tests.

**Check:** `Entities_carry_columns_and_nothing_else` fails and names the property. Revert.

## Checkpoint

Answer without looking:

1. Why does the Include version load 36 columns when the screen shows 5?
2. What does `AsNoTracking()` fix, and what does it not fix?
3. Why is the EF projection the right EF answer, and what is its one weakness?
4. What is the difference between a navigation property and a `[RemoteProperty]`?
5. Why does the read model load 8 columns rather than 5?
6. Where, in each of the three versions, does the discipline live?

## The one idea

Count the fields you load against the fields you use. In EF, project. In Slim Domain, declare the read
model once, and every query of it stays that lean.
