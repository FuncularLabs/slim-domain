# Slim Domain — the paper

**Slim Domain: Hostile to Technical Debt by Design**
*A .NET domain-structuring pattern that lowers cognitive load and puts gravity beneath design discipline.*
Paul Smith, Funcular Labs · Version 1.0, Release Candidate 8 · October 2026

| File | Use it for |
|---|---|
| [`Slim_Domain_v1.0-rc8.pdf`](Slim_Domain_v1.0-rc8.pdf) | Reading. 79 pages, 22 listings, 7 tables, two appendices. |
| [`Slim_Domain_v1.0-rc8.docx`](Slim_Domain_v1.0-rc8.docx) | Reviewing with tracked changes or comments. |

The companion digest for coding agents is in [`../guidance/`](../guidance/). The companion sample solution is in
[`../samples/Switchback/`](../samples/Switchback/).

## Abstract

Slim Domain is a domain-structuring pattern for .NET systems — not an application architecture and not a
library. Relative to a disciplined service layer over Dapper or Entity Framework Core, it changes five things:

**Five things that change on Monday**

|  | A disciplined service layer over EF Core | Slim Domain |
|---|---|---|
| **Reads** | shaped to the object graph, however well the Includes are managed | shaped to the use case: a detail entity, a view, or a projection |
| **Canonical entity** | the one shape every consumer adds its members to | the table, and nothing more; richer shapes live elsewhere |
| **Business objects** | mapped to a DTO per layer, boundary or not | inherit or wrap the entity shape inside a trust boundary; DTOs only where a boundary warrants them |
| **Persistence** | confined to handlers by discipline, yet ambient through the tracker: any mutation persists on the next SaveChanges | services only, and only what the service writes; a noun never saves itself |
| **Cross-cutting rules** | re-stated in each service, or matched by type in an interceptor | generator-proposed, human-approved interfaces, applied in one place |

Read through the lens of technical debt, the five changes have one effect: the common forms of inadvertent
debt — over-fetching, parallel shapes, ambient persistence, scattered cross-cutting rules — become
structurally unavailable rather than merely discouraged, and the interest paid on cross-cutting change is
bounded by construction. It is intended for teams of mixed experience levels, including those common in
regulated, high-security environments.

The pattern rests on four principles:

**The four principles**

| **Slim entities**<br>pure data contracts, typically generated from the schema — the table and nothing more | **Enriched business objects**<br>the entity shape plus behavior, acquired by inheritance or composition |
|---|---|
| **Service-gated persistence**<br>the services layer is the sole consumer of the data-access library | **Use-case-shaped reads**<br>every query returns a shape carrying only what the operation uses |

Where a library maps only the intersection of columns and properties, business objects need no per-member
mapping attributes even on writes — a convenience the pattern welcomes and does not require.

This paper defines the pattern and illustrates it with C# examples drawn from a fictitious reference
solution, the Aegis Platform. It states what the pattern is and is not, which application architectures it
fits inside and where it meets friction, and how teams on any data-access library can adopt its central
habit; develops technical debt as the lens that unifies its tenets (Section 3.4); provides a measured feature
comparison of leading micro-ORMs; demonstrates practical techniques for object-graph loading, transactions,
non-canonical projections, and mapping exceptions; states the pattern's consequences and the discipline each
cost requires, with particular attention to healthcare and financial services; compares the discipline the
pattern requires against classic strict n-tier layering; and shows how teams can present a Slim Domain
solution in conventional n-tier terms — including optional build-time enforcement — without conversion or
rework.

The argument rests on mechanism — how each rule constrains the defect classes a regulated team fears — and
on a structural measurement of two production systems (Appendix B) that describes what the pattern's rules
oblige developers to write, compared with a legacy Entity Framework and active-record codebase from the same
lineage. That measurement is not a comparison with a modern service layer and is not offered as proof of
superiority; Section 11 states the evidentiary limits and proposes the measurements that should follow. The
central claim is narrower: once the deliberate engineering discipline required by any layered architecture
is accounted for, Slim Domain changes the default path so that the discipline is easier to apply, easier to
audit, and harder to violate accidentally.

## Status: release candidate, open for review

The paper went public as rc1 on 2026-10-02; rc8 (2026-10-27) is the current revision. It has been through
several rounds of adversarial review by AI reviewers (the method and the disclosure are in Appendix A), and
it has not yet been reviewed by people outside Funcular Labs. That is what this release is for.

**What would help most**, roughly in order:

1. **Counter-examples.** A situation where a rule in §2 or §4 produces a worse outcome than the
   conventional approach, described concretely enough to reproduce.
2. **Prior art we missed.** §10 (Related Work) names what we know of. If the pattern, or a tenet of it,
   has an earlier name, we want to cite it.
3. **Holes in the evidence.** §11 and Appendix B state what was measured and what was not. Tell us where
   the stated limits are still too generous.
4. **Places the text is unclear**, with the section number and what you read it to mean.
5. Anything in the listings that would not compile, or would not behave as the surrounding text claims.

**How to send it:**

- Open a [discussion](https://github.com/FuncularLabs/slim-domain/discussions) for questions, disagreements
  and alternatives (one topic per thread, please).
- Open an [issue](https://github.com/FuncularLabs/slim-domain/issues) labelled `paper` for a specific defect:
  a wrong claim, a broken listing, a missing citation. Quote the section number.
- For line-level comments, mark up the `.docx` and attach it to an issue, or email it to the author.

Comments are read by the author. Substantive contributions are acknowledged in the next revision's
Appendix A unless you ask otherwise.

## What changed since rc1

rc2 and rc3 were internal revisions; rc4 through rc8 followed within two days. If you read rc1, this is what moved:

- **The abstract's lists become pattern cards (rc8).** The two list-sentences of the abstract (the five things
  that change, the four principles) are now two unnumbered cards on the abstract page, in the style of a design-
  pattern catalog: *Five things that change on Monday*, a three-column table that sets each concern beside what a
  disciplined service layer does and what Slim Domain does, and *The four principles*, a two-by-two grid. The
  abstract above mirrors them as tables. They carry no caption, so Tables 1–7, Listings 1–22 and Figures 1–6 keep
  their numbers. Apart from the glyph substitutions below, the body is that of rc7.
- **No fallback fonts (rc8).** CMU Serif lacks three glyphs the prose used (≈, →, ×), which the PDF had been
  rendering from Cambria, Cambria Math and Times New Roman. They are now spelled out ("about", "from … through …
  to", "x"), so no visible glyph is drawn from any font but CMU Serif and Consolas. (Word's table-of-contents and
  list furniture still embeds Aptos and Arial subsets for whitespace, as in every revision.) rc7's re-cut had
  already replaced synthesized bold with the real CMU Serif Bold face, which is why the cover title and headings
  now copy and search cleanly.
- **Title, subtitle and the gravity metaphor (rc7).** The paper is now *Slim Domain: Hostile to Technical Debt by
  Design*, with the subtitle *A .NET domain-structuring pattern that lowers cognitive load and puts gravity beneath
  design discipline.* The cover carries the title and subtitle only. Three passages carry the cover's image into the
  body: a new paragraph in §1.2 (discipline is effort applied against a slope; the pattern turns the slope so the
  maintainable shape is the one a developer falls into by default), a sentence closing §3.3, and a sentence closing
  §3.4 that says what "hostile to technical debt by design" means: an environment in which the inadvertent kinds of
  debt have no easy path, not a guarantee that none accrues. The body is otherwise that of rc4. (The rc4, rc5 and
  rc6 title pages each lasted one revision: rc4's subtitle, then *Debt-Proof by Design* and *Technical Debt Has
  Nowhere to Grow*, each with a subtitle of its own.)
- **Framing (rc4).** The abstract's opening and closing paragraphs state the pattern's effect in technical-debt
  terms.
- **Technical debt as the unifying lens** (new §3.4, new Table 1). Each tenet is mapped to the debt channel it
  closes, the interest it avoids, and the Appendix B proxy that measures it, with two stated limits: the
  pattern addresses inadvertent debt, not deliberate borrowing, and prevention is claimed by construction, not
  from accrual data. Because Table 1 is new, every later table is renumbered by one: the micro-ORM matrix is
  now Table 2, and Appendix B's tables are 5–7.
- **Schema churn restated as parity.** §3.4 and §11.1 now say that every persistence strategy pays schema churn
  somewhere; Slim Domain's choice is to pay it earliest, at compile time in the consuming layer. §11.2 adds a
  debt accrual-rate metric (files touched per cross-cutting change, attribute growth per KLOC per year,
  hotspot overlap), and §10 adds the technical-debt literature (references [25]–[30]).
- **Canonical entities always carry `[Table]`.** Because the `Entity` suffix means a class name never matches
  its table, every canonical entity in the listings now declares `[Table("…")]` (Listings 2, 19, 21), and
  §4.1, §4.6 (the generator contract emits it) and §6.4 say so; a team that prefers name inference may drop
  the suffix instead.
- **Read-only markers, precisely.** §6.3 now states that Entity Framework Core's keyless entity types
  (`HasNoKey`) serve as a library-level read-only marker and that FunkyORM offers none at the time of writing,
  so on the attribute road the service base's runtime guard and the architecture test carry the rule. The
  guidance digest carries the same wording.
- **FunkyORM references updated to 3.12.1** (October 2026) in the §1.4 disclosure, Table 2 and its footnote,
  §6.7 and Listing 18 ("3.12.1 has no predicate-update primitive" still holds; the stored-procedure
  compare-and-swap stays), §6.8 ("3.8 and later" for session-context priming), and §11.1. No Table 2 row
  values changed.
- **Syntax-highlighted listings.** All 22 listings are colored, with no change to font, size, indentation or
  pagination; the palette is print-muted so grayscale copies stay readable.
- **Length:** 75 → 79 pages; listings (22) and figures (6) unchanged.

## Versions

| Version | Date | Notes |
|---|---|---|
| 1.0-rc8 | 2026-10-27 | The abstract's two lists become pattern cards; fallback glyphs removed so every visible glyph comes from CMU Serif or Consolas; body otherwise as rc7; 79 pages. |
| 1.0-rc7 | 2026-10-27 | Title *Hostile to Technical Debt by Design* and a new subtitle; the cover's gravity metaphor carried into §1.2, §3.3 and §3.4; 79 pages. Superseded the same day. |
| 1.0-rc6 | 2026-10-27 | Title *Technical Debt Has Nowhere to Grow* and a new subtitle; body identical to rc4. Superseded the same day. |
| 1.0-rc5 | 2026-10-26 | Title *Debt-Proof by Design*; the cover drops the tagline; body identical to rc4. Superseded the same week. |
| 1.0-rc4 | 2026-10-26 | Technical-debt lens; `[Table]` on every canonical entity; read-only markers stated precisely; FunkyORM references at 3.12.1. The rc1 files are in the repository history (commit `8141453`). |
| 1.0-rc1 | 2026-10-02 | First public release candidate. |

## Citing

See [`../CITATION.cff`](../CITATION.cff). In prose: *Smith, P. (2026). Slim Domain: Hostile to Technical Debt
by Design (v1.0-rc8). Funcular Labs.*

## License

The paper is licensed under [CC BY 4.0](../LICENSE-DOCS.md).
