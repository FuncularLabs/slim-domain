# Switchback — agent instructions

Switchback is the companion sample for the Slim Domain paper. It is an EF Core application that converts
one feature slice (the Recent orders list) to FunkyORM the Slim Domain way, and measures the difference.
Follow [`../../guidance/Slim_Domain_agent_guidance_v1.0-rc9.md`](../../guidance/Slim_Domain_agent_guidance_v1.0-rc9.md)
with the settings below.

## Slim Domain §0 settings for this solution

```
DATA_ACCESS_ROAD:        attribute (FunkyORM) for converted slices; view (EF Core, constrained) for the rest
PERSISTENCE_MODE:        bypass
DEFAULT_VARIANT:         inheritance (no business objects yet; episode 1 is a read-only slice)
APPROVED_PERSISTENCE_NAMESPACES:  [ "Switchback.Infrastructure.Persistence", "Switchback.Application.Services", "Switchback.Data" ]
ENTITY_GENERATOR:        absent (hand-written slim entities; follow §2.1 anyway)
CANONICAL_ENTITY_PROJECT:   Switchback.Infrastructure.Persistence.Entities
BUSINESS_OBJECT_PROJECT:    Switchback.Domain (created in a later lab)
SERVICES_PROJECT:           Switchback.Application.Services
CONTRACTS_PROJECT:          Switchback.Shared.Contracts
```

## House rules

- **The EF model is the "before" world. Leave it that way.** `Switchback.Data.Model.*` keeps its navigation
  properties and `EfIncludeChainRecentOrdersService` keeps its `Include` chain on purpose: they are what the
  sample measures. The §8 "constrained EF" advice in the guidance does not apply to them.
- **EF Core owns the schema.** Change tables through a migration in `Switchback.Data`
  (`dotnet ef migrations add <Name> --project src/Switchback.Data`), then `dotnet run --project src/Switchback.Demo -- reset`.
  The four tables have exactly 36 columns; the scoreboard, the tests and the labs depend on that number.
- **The seed is deterministic** (`SwitchbackSeeder`: fixed seed, fixed reference date). Changing it changes
  every row hash quoted in the labs and videos. Do not change it without saying so.
- **FunkyORM operator order:** `Where` → `OrderBy`/`ThenBy` → `Skip` → `Take` → materialize. No filtering,
  ordering or aggregating after `Skip`/`Take`; no second `OrderBy`; no `Where` after a subset `Select`.
- **When FunkyORM surprises you,** do not work around it silently. Reproduce it in a test, mark the test
  `Skip = "FunkyORM: …"`, add an entry to `FUNKYORM_FEEDBACK.md`, and use a construct that works.
- **The architecture tests are the gate.** Only `Switchback.Infrastructure.Persistence.*` and
  `Switchback.Application.Services` may reference `Funcular.Data.Orm`; only `Switchback.Data` and
  `Switchback.Application.Services` may reference `Microsoft.EntityFrameworkCore`. Do not widen the lists; move the code.
- Builds must be warning-free (`TreatWarningsAsErrors` is on). Keep files short enough to read on screen.
