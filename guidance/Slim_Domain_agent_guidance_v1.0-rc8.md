# Slim Domain — agent guidance for structuring domains in .NET solutions

**Source:** *Slim Domain: Hostile to Technical Debt by Design — A .NET domain-structuring pattern that lowers cognitive load and puts gravity beneath design discipline*, v1.0-rc8 (Funcular Labs, October 2026). This file is a compressed, agent-oriented digest of that paper. When this file and the paper disagree, the paper wins.

**How to use:** paste into your repository's agent instructions (`CLAUDE.md`, `.github/copilot-instructions.md`, `AGENTS.md`, or equivalent), fill in §0, and delete any section that does not apply. Everything below is written as instructions to a coding agent working in a .NET solution.

---

## 0. Team settings (fill these in before use)

```
DATA_ACCESS_ROAD:        attribute | view | projection
                         attribute  = micro-ORM with intersection mapping and attribute-defined projections (e.g. FunkyORM)
                         view       = Entity Framework Core, constrained as described in §8
                         projection = Dapper / LinqToDB / RepoDB with explicit SQL or LINQ projections
PERSISTENCE_MODE:        bypass | repository
                         bypass     = services call the data-access library directly (default)
                         repository = services call thin, stateless repository interfaces only
DEFAULT_VARIANT:         composition | inheritance
                         composition = default for modules with invariants that carry compliance weight
                         inheritance = acceptable for CRUD modules whose fields are individually editable by design
APPROVED_PERSISTENCE_NAMESPACES:  [ "<Solution>.Infrastructure.Persistence", "<Solution>.Application.Services" ]   # remove the services entry in repository mode
ENTITY_GENERATOR:        present | absent      # if absent, hand-write slim entities and follow §2.1 anyway
CANONICAL_ENTITY_PROJECT:   <Solution>.Infrastructure.Persistence.Entities
BUSINESS_OBJECT_PROJECT:    <Solution>.Domain
SERVICES_PROJECT:           <Solution>.Application.Services
CONTRACTS_PROJECT:          <Solution>.Shared.Contracts
```

---

## 1. What Slim Domain is — and is not

**Purpose.** Cut data access down to what is actually used or displayed, and make that the path of least resistance rather than an act of discipline. In technical-debt terms: make the common forms of *inadvertent* debt — over-fetching, parallel shapes, ambient persistence, scattered cross-cutting rules — structurally unavailable, so the interest paid on cross-cutting change stays bounded as the codebase grows.

**Five tenets** (quote these in design reviews):
1. **Flatten to the use case.** Eschew large — ideally any — object graphs; shape each read to the immediate need.
2. **Keep canonical entities minimal.** The persistence contract is the table and nothing more.
3. **Inherit where boundaries do not require separation.** Business objects acquire the entity shape directly (by inheritance or by wrapping); DTOs are reserved for genuine boundaries.
4. **Keep nouns and verbs apart.** Objects carry state and behavior; services perform persistence. An object never saves itself.
5. **Structure the domain; do not dictate the architecture.** Coexists with layered, vertical-slice, onion/clean, and microservice hosts.

**Three roles and one dependency direction:**
- **Canonical entity** — generated, one per table, no behavior, no navigation properties.
- **Business object** — the entity shape plus behavior, validation, computed members.
- **Service** — the only code that references the data-access library; owns transactions, audit, consent, the version predicate.
- Dependencies: Presentation → Services → Business objects → Entities. Entities know nothing above them.

**It is NOT:** an ORM or library; an application architecture; a ban on relationships (navigation is just not the organizing principle of retrieval); a ban on DTOs (they belong at boundaries); an anemic domain model (behavior lives on business objects); a repository mandate or ban; a replacement for strategic DDD.

**The technical-debt lens** (the paper's §3.4). Each tenet closes a debt channel; use this when justifying a rule to a stakeholder:

| Tenet / rule | Debt channel closed | Interest avoided |
|---|---|---|
| Flatten to the use case | Over-fetching that accretes screen by screen | Latency and payload paid on every request |
| Minimal canonical entities | Entity accretion (screen members, `[NotMapped]` piles) | Every schema change negotiated with every consumer |
| Inherit within a boundary; DTO warrant test | Parallel shapes kept in sync by hand | One domain change touches N copies plus mappers |
| Nouns and verbs apart | Ambient persistence | A cross-cutting persistence change touches hundreds of files |
| Generator contract; fitness functions | Architecture erosion, silent schema drift | Drift found late, at runtime, by someone else |

Two limits the agent must respect when making this argument: the pattern prevents *inadvertent* debt (it cannot stop a team borrowing deliberately under deadline pressure), and schema churn is **parity, not deficiency** — every persistence strategy pays it somewhere; Slim Domain pays it at compile time in the consuming layer, which is the cheapest place.

---

## 2. Rules (MUST / SHOULD / NEVER)

### 2.1 Canonical entities
- MUST be one class per table, containing only that table's columns.
- MUST have no behavior and no navigation properties.
- MUST carry `[Table("<table>")]`: the `Entity` suffix means the class name never matches the table name, so never rely on the library's name inference for a canonical class (or drop the suffix — but the naming convention and the inference rule must agree). Otherwise no mapping attributes unless a column name deviates from convention (then standard `System.ComponentModel.DataAnnotations`: `[Column]`, `[Key]`, `[NotMapped]`).
- MUST NOT be hand-edited when generated; hand-written members go in a sibling partial file. Treat a regeneration diff as a contract change.
- SHOULD carry a `Version` column where writes can contend, and soft-delete / audit columns as the schema dictates (see §2.6).
- NEVER add a screen's or report's needs to a canonical entity. Make a detail entity instead (§2.4).

### 2.2 Business objects
- MUST acquire the entity shape by **composition** (wrap a private entity; expose getters and intention-revealing methods) when `DEFAULT_VARIANT = composition`, or by **inheritance** when the module is CRUD-shaped and fields are individually editable by design.
- MUST route every state change through a method that enforces the rule (`ApplyDebit`, `Suspend`), never a bare setter from outside the object.
- MUST take time as a parameter (`DateTime nowUtc`) — never call `DateTime.UtcNow` inside domain code.
- MUST compare status-like strings against named constants (`AccountStatus.Active`), never literals.
- NEVER carry persistence logic; NEVER expose a `Save()`; NEVER be serialized across an API boundary (see §2.5).
- Inheritance variant: emit **protected setters** where the generator and library support it; public setters are a known bypass and are acceptable only in CRUD modules.

### 2.3 Services and persistence
- MUST be the only code referencing the data-access library (`APPROVED_PERSISTENCE_NAMESPACES`); enforce with the architecture test in §4.6.
- MUST open, own, and commit the transaction for any multi-write operation. Helpers receive the transaction (or the scoped provider) and NEVER commit.
- MUST use a **version predicate** (compare-and-swap) on writes that can contend: load version → apply behavior → `UPDATE … WHERE id = @id AND version = @expected`; zero rows ⇒ throw a concurrency conflict. On EF Core, increment the token or use a store-generated `rowversion`.
- MUST write audit records inside the same transaction as the change they describe.
- MUST hold no hidden state: no caching, no identity map, no pending-change buffer. The provider/connection is **scoped** (per request / per circuit / per unit of work), never a singleton.
- SHOULD name columns explicitly in SQL; NEVER `SELECT *`.
- SHOULD filter soft-deleted rows explicitly (`AND deleted_utc IS NULL`) or through a base-class `QueryActive<T>()`; a raw query of a soft-deletable type is a defect unless the caller states "including deleted".

### 2.4 Reads
- MUST return a shape carrying only what the operation uses: the canonical entity for a single-row operation; otherwise a **detail entity**, a view, or a projection.
- Detail entities MUST be `sealed`, MUST carry an `IReadModel` marker, and NEVER reach a write path. Where the data-access library offers a read-only entity marker of its own (EF Core: a keyless entity type, `HasNoKey()`), it goes on every detail type too and supersedes the service base's runtime guard (paper §6.3). FunkyORM offers none.
- Related data is loaded **explicitly** (one narrow query per shape, composed in the service) or via attribute/view projections — never through ambient navigation properties or lazy loading.
- A column that must never leave the service is protected by **absence**: the read type has no member for it.

### 2.5 DTOs and boundaries
A **boundary** is a line across which trust, ownership, serialization format, or lifecycle changes (HTTP API, message queue, file export, another bounded context). Inside a boundary, use the business object. Across one, a DTO is warranted when **any** of these applies:
1. **Confidentiality** — the object carries members not to be published (internal ids, cost basis, risk tier, audit stamps, soft-delete markers, PHI/PII beyond need). The DTO is an allow-list.
2. **Behavior containment** — verbs and invariants must not be visible or invocable outside the API.
3. **Contract stability** — the published shape must outlive the schema and object.
4. **Wire-format fit** — naming, nullability, enums, dates, cycles, payload size.
5. **Ownership / anti-corruption** — the receiver must not depend on the sender's internal shape.
6. **Inbound intent / over-posting** — requests bind to a request DTO that states intent; NEVER bind client input to a business object.

**Not a warrant:** "every layer should have its own model"; coupling fear inside one process; a mapper already installed; a hypothetical future API.

Rules: map **once, explicitly, at the edge**. Where a read-side contract needs no behavior, **project straight into the contract** (detail entity / view / narrow `Select`) — no business object in the middle. When a mapping appears in a PR, ask which boundary it serves; if the answer is a layer diagram, delete it.

### 2.6 Cross-cutting concerns (SOLID in practice)
- Recurring column groups (audit stamps, soft delete, version) become **interfaces** — `IAuditable`, `ISoftDeletable`, `IVersioned` — proposed by the generator from the schema and **approved by a human** (column-name coincidence is not semantic identity). Emit an abstract base only for a dominant or strictly nested group.
- One **service base** centralizes stamping (`CreatedUtc/By`, `ModifiedUtc/By`), the insert/update split, soft-delete query intent, and the `IReadModel` write refusal. The raw provider is private to the base; `User`, `Clock`, `Audit` are protected.
- Prefer this over per-service repetition, reflection-by-name, database triggers (which see the connection principal, not the user — unless session context is primed), or ORM `SaveChanges` interceptors.
- For row-level security, prime the end-user identity into per-connection **session context** (`sp_set_session_context` / `set_config`) per request.

### 2.7 Repositories (only in `PERSISTENCE_MODE = repository`, or when auditors require them)
- Thin, stateless, one- or two-line delegates to the library.
- **Drift signs to reject in review:** an identity map or entity cache; a pending-change buffer with a save-everything method; connection/transaction state in fields beyond an explicit scope; per-entity `FindByX` zoos. Any of these is a hand-rolled `DbContext` — the thing the pattern removed.

### 2.8 Entity generator contract (if `ENTITY_GENERATOR = present`)
Input = live schema or committed snapshot. Output = generator-owned partial per table; hand code in sibling partials. Renames = remove + add (compiler reports every use). Column-group suggestions = a report, applied only after a human updates the approval file. CI regenerates to scratch and fails on diff. Escape hatch: exclude a table or pin a hand-written class.

---

## 3. Decision procedures

**Variant:** invariants carry compliance weight → composition. CRUD module, fields individually editable → inheritance (protected setters if possible).

**Loading related data:** need a behavioral graph → explicit composition (one narrow query per shape, in the service). Need a read-side shape → detail entity / view / projection. Database team owns the logic → stored procedure through the same mapping pipeline. Never: lazy loading, `Include` graphs, navigation-driven retrieval.

**A value is about to leave a business object:** crosses a boundary? No → use the business object. Yes, inbound → request DTO. Yes, outbound and read-only → project straight into the contract. Yes, outbound from a first-order object → allow-list DTO, mapped once at the edge.

**Concurrency:** contended write → version predicate (projection road: in the SQL; view road: increment the token or store-generated rowversion; attribute road: stored procedure until the library offers a conditional update). Multi-row consistency → one transaction, key-ordered loads, spanning rules on a domain type, helpers that cannot commit.

---

## 4. Reference shapes (compact)

### 4.1 Canonical entity (generated) and companion constants (hand-written)
```csharp
[Table("account")]                             // always: the Entity suffix ≠ the table name
public class AccountEntity                      // one table, no behavior, no navigation
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public int HolderId { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = AccountStatus.Pending;
    public int Version { get; set; }            // concurrency token
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public DateTime? DeletedUtc { get; set; }   // soft delete
    public string? DeletedBy { get; set; }
}
public static class AccountStatus { public const string Pending = "Pending", Active = "Active", Suspended = "Suspended"; }
```

### 4.2 Business object — composition variant (default where invariants matter)
```csharp
public sealed class Account
{
    private readonly AccountEntity _data;
    public Account(AccountEntity data) => _data = data;
    public int Id => _data.Id;  public string AccountNumber => _data.AccountNumber;
    public decimal Balance => _data.Balance;  public string Status => _data.Status;

    public void ApplyDebit(decimal amount, DateTime nowUtc)
    {
        if (amount <= 0m) throw new DomainRuleException("Debit amount must be positive.");
        if (_data.Status != AccountStatus.Active) throw new DomainRuleException("Debits require an active account.");
        _data.Balance -= amount;  _data.ModifiedUtc = nowUtc;
    }
    public AccountEntity ToEntity() => _data;   // for the service's write only
}
```
Inheritance variant: `public class Account : AccountEntity { … same methods, computed members ignored by intersection mapping … }`.

### 4.3 Flagship service write (projection road, Dapper) — the shape every write follows
```csharp
public async Task<Account> DebitAsync(int id, decimal amount, Guid correlationId)
{
    using var tx = _db.BeginTransaction();                       // disposed => rolled back
    var entity = await _db.QuerySingleOrDefaultAsync<AccountEntity>(SelectSql, new { id }, tx)
                 ?? throw new NotFoundException(nameof(Account), id);   // SelectSql: named columns, aliased, AND deleted_utc IS NULL
    var expected = entity.Version;

    var account = new Account(entity);
    account.ApplyDebit(amount, _clock.UtcNow);                   // the only way Balance changes

    var row = account.ToEntity();  row.Version = expected + 1;
    var rows = await _db.ExecuteAsync(UpdateSql,                 // UPDATE … WHERE id=@Id AND version=@expected AND deleted_utc IS NULL
        new { row.Balance, row.Status, row.ModifiedUtc, row.Version, row.Id, expectedVersion = expected }, tx);
    if (rows == 0) throw new ConcurrencyConflictException(nameof(Account), id);

    await _audit.WriteAsync("Account.Debit", id, correlationId, tx);   // same transaction
    tx.Commit();
    return account;
}
```
**View road (EF Core) delta:** load the tracked `AccountEntity` with no `Include`; wrap; `entity.Version++` (token must change to conflict) or configure a store-generated rowversion; add the audit row to the same context; one `SaveChangesAsync`; map `DbUpdateConcurrencyException` to your conflict exception.
**Attribute road (intersection mapping) delta:** `UpdateAsync(account)` persists the enriched object with no attributes — but carries no predicate; for contended writes use the library's conditional update if it has one, else a stored procedure.

### 4.4 Transfer — a consistency boundary
Open one transaction; load both rows **in key order** (deadlock avoidance); `TransferRules.Check(source, target, amount)` on a static domain type; `ApplyDebit` / `ApplyCredit`; guarded writes via helpers that take `tx` and cannot commit; ledger + audit in the same transaction; commit once.

### 4.5 Service base (attribute road; adapt for others)
```csharp
public abstract class ServiceBase
{
    private readonly IOrmDataProvider _provider;             // private: services never touch it directly
    protected ICurrentUser User { get; }  protected IClock Clock { get; }  protected IAuditWriter Audit { get; }
    protected Task<TKey> InsertAsync<T,TKey>(T e) where T:class,new() { Guard(e); Stamp(e, isNew:true);  return _provider.InsertAsync<T,TKey>(e); }
    protected Task<T>    UpdateAsync<T>(T e)      where T:class,new() { Guard(e); Stamp(e, isNew:false); return _provider.UpdateAsync(e); }
    protected IQueryable<T> Query<T>() where T:class,new()
    { if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T))) throw new InvalidOperationException("use QueryActive or QueryIncludingDeleted"); return _provider.Query<T>(); }
    protected IQueryable<T> QueryActive<T>() where T:class,ISoftDeletable,new() => _provider.Query<T>().Where(e => e.DeletedUtc == null);
    private void Stamp(object e, bool isNew) { if (e is not IAuditable a) return; var now = Clock.UtcNow; if (isNew) { a.CreatedUtc = now; a.CreatedBy = User.Name; } a.ModifiedUtc = now; a.ModifiedBy = User.Name; }
    private static void Guard(object e) { if (e is IReadModel) throw new InvalidOperationException("Read models are never written."); }
}
```

### 4.6 Architecture test (the persistence gate as a failing build)
```csharp
static readonly string[] Approved = { "<Solution>.Infrastructure.Persistence", "<Solution>.Application.Services" /* bypass mode only */ };
[Fact] public void Only_approved_namespaces_reference_a_data_access_library()
{
    var r = Types.InAssembly(typeof(AccountService).Assembly)
        .That().DoNotResideInNamespaceMatching(string.Join("|", Approved.Select(Regex.Escape)))
        .ShouldNot().HaveDependencyOnAny("Funcular.Data.Orm", "Dapper", "Microsoft.EntityFrameworkCore").GetResult();
    Assert.True(r.IsSuccessful, string.Join(", ", r.FailingTypeNames ?? Array.Empty<string>()));
}
```
Further fitness functions worth adding as analyzer rules: service methods above an orchestration-size threshold (fat-service drift); setter assignment to a business object outside its own methods (invariant bypass); insert/update with an `IReadModel` type argument; generated entities differing from a schema snapshot (generator drift).

---

## 5. Pull-request review checklist (ask these, in order)

1. Does any type outside `APPROVED_PERSISTENCE_NAMESPACES` reference the data-access library? → reject.
2. Does any entity or business object have a `Save`/`Update`/`Delete` method? → reject (nouns don't save themselves).
3. Does any read load more than the operation uses (`SELECT *`, `Include` chains, navigation traversal)? → ask for a detail entity / projection.
4. Does any write that can contend lack a version predicate? → add one.
5. Is any audit record written outside the transaction of the change it describes? → move it in.
6. For every new DTO or mapper: which boundary does it serve? No boundary → delete it. Read-only across a boundary → project directly instead.
7. Is client input bound to a business object anywhere (over-posting)? → request DTO.
8. Is a detail/read-model type reachable from a write path? → seal it, mark `IReadModel`.
9. Does a query of a soft-deletable type state its intent (active / including deleted)? → make it explicit.
10. Did a generated file get hand-edited? → move the edit to the partial.
11. Does a repository hold state (cache, identity map, pending changes, connection fields)? → it is a DbContext; strip it.
12. Does domain code read the clock or compare bare status strings? → inject time; use constants.

---

## 6. Anti-patterns to flag on sight

| Pattern | Why it's wrong here | Replace with |
|---|---|---|
| `virtual ICollection<T>` on an entity | ambient graph edge; invites lazy loads | explicit composition or a detail entity |
| `.Include(...).ThenInclude(...)` | loads a graph the use case may not need | narrow query per shape, or a view/projection |
| `entity.Save()` / `ActiveRecord` base | noun carries the verb | service method |
| `AutoMapper.Map<Dto>(bo)` between layers in one process | ceremony without a boundary | use the business object; DTO only at the edge |
| `SaveChanges()` scattered across handlers/controllers | ambient persistence | service-owned transaction |
| `DateTime.UtcNow` inside domain methods | untestable, un-injectable time | `nowUtc` parameter / `IClock` |
| `if (status == "Active")` | magic string invariant | named constants |
| Repository with `Dictionary<int, T> _cache` | reinvented identity map | stateless delegate |
| Singleton provider/connection with transactions | cross-request transaction bug | scoped lifetime |
| `[NotMapped]` on every computed member | ceremony the mapping should absorb | intersection mapping (attribute road) or a detail type |

---

## 7. Fit and friction (where to apply, where to be careful)

**Natural fit:** layered/n-tier monolith (services are already the gate); vertical slice (each handler is a gate, entities shared); onion/clean (inward dependencies hold, with one declared exception: business objects depend on the generated entity shape — use composition to restore the inversion); microservices/modular monolith (minimal entities per service; contracts at the boundary); CQRS/event sourcing (the service seam is where events and read models attach).

**Friction (not incompatibility):** graph-navigating APIs (GraphQL resolvers, OData `$expand`); aggregate-centric tactical DDD; Active Record frameworks; change-tracker-dependent code (lazy loading, detached-entity flows); mapper-everywhere DTO cultures. Each can host the pattern once its graph-centric assumption is replaced by use-case shapes — at the cost of the host style no longer doing what it was designed to make easy.

---

## 8. Adopting inside an existing codebase

- **Scope a trial:** one subdomain, one feature slice, or one new service. Keep the host architecture and naming; rename projects later if auditors prefer conventional names (it is naming, not conversion).
- **EF Core (view road), one view at a time:** map read-side shapes to SQL views as keyless entity types read with `AsNoTracking()`; strip navigation properties from canonical entities (or isolate them to detail types); disable lazy loading; confine `SaveChanges` to services; configure `Version` as a concurrency token and change it on write; add the architecture test. The change tracker remains a bypass, so hold the guarantee with fitness functions.
- **Dapper vertical slice:** most of the pattern is already present; add the DTO warrant test, the canonical/detail split, behavior on the noun, and schema-derived interfaces.
- **Pick one persistence mode per solution** (bypass or repository) and write it down; the architecture test's approved-namespace list is the switch.

---

## 9. What to measure

- **Ratio of fields loaded to fields used** per read path — the pattern's adoption metric.
- Onboarding time: days from first commit to first unassisted production-merged feature.
- Defect-class incidence tagged in incidents and reviews: N+1 / lazy-load, stale-state / tracking, mapping drift, invariant bypass. The pattern predicts near-elimination of the first two.
- Audit findings per cycle attributable to data-access controls.
- Structural counts per 100k lines: mapping attributes, navigation collections, change-tracking operations, files participating in persistence.

---

*Digest of Slim Domain v1.0-rc8. The paper carries the full argument, the evidence and its limits, twenty-two listings, and the review methodology; consult it before deviating from any rule above.*
