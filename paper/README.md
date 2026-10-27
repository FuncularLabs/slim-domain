# Slim Domain — the paper

**Slim Domain: Technical Debt Has Nowhere to Grow**
*A .NET domain-structuring pattern that cuts cognitive load and makes the correct path the easy one.*
Paul Smith, Funcular Labs · Version 1.0, Release Candidate 6 · October 2026

| File | Use it for |
|---|---|
| [`Slim_Domain_v1.0-rc6.pdf`](Slim_Domain_v1.0-rc6.pdf) | Reading. 78 pages, 22 listings, 7 tables, two appendices. |
| [`Slim_Domain_v1.0-rc6.docx`](Slim_Domain_v1.0-rc6.docx) | Reviewing with tracked changes or comments. |

The companion digest for coding agents is in [`../guidance/`](../guidance/). The companion sample solution is in
[`../samples/Switchback/`](../samples/Switchback/).

## Abstract

Slim Domain is a domain-structuring pattern for .NET systems — not an application architecture and not a
library. Relative to a disciplined service layer over Dapper or Entity Framework Core, it changes five things:
reads are shaped to the use case rather than to the object graph; the canonical entity is the table and
nothing more; business objects acquire the entity shape by inheriting or wrapping it inside a trust
boundary, so in-process DTO mapping disappears except where a defined boundary warrants it; services alone
persist, and nouns never save themselves; and recurring column groups become generator-proposed,
human-approved interfaces that centralize cross-cutting rules such as audit stamping. Read through the lens of
technical debt, the five changes have one effect: the common forms of inadvertent debt — over-fetching,
parallel shapes, ambient persistence, scattered cross-cutting rules — become structurally unavailable rather
than merely discouraged, and the interest paid on cross-cutting change is bounded by construction. It is
intended for teams of mixed experience levels, including those common in regulated, high-security
environments.

The pattern rests on four principles: slim entities as pure data contracts; enriched business objects, via
inheritance or composition, that acquire the persistence shape and add behavior; service-gated persistence,
in which the services layer is the sole consumer of the data-access library; and use-case-shaped reads, in
which every query returns a shape carrying only what the operation uses. Where a library maps only the
intersection of columns and properties, business objects need no per-member mapping attributes even on
writes — a convenience the pattern welcomes and does not require.

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

The paper went public as rc1 on 2026-10-02; rc6 (2026-10-27) is the current revision. It has been through
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

rc2 and rc3 were internal revisions; rc4, rc5 and rc6 followed within two days. If you read rc1, this is what moved:

- **Title and subtitle (rc6).** The paper is now *Slim Domain: Technical Debt Has Nowhere to Grow*, with the
  subtitle *A .NET domain-structuring pattern that cuts cognitive load and makes the correct path the easy one.*
  The cover carries the title and subtitle only; the body is unchanged from rc4. (rc4's subtitle, *A
  Domain-Structuring Pattern for .NET That Leaves Inadvertent Technical Debt Nowhere to Accrue*, and rc5's title,
  *Debt-Proof by Design*, each lasted one revision; "proof" promised more than §3.4 and §11.1 claim.)
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
- **Length:** 75 → 78 pages; listings (22) and figures (6) unchanged.

## Versions

| Version | Date | Notes |
|---|---|---|
| 1.0-rc6 | 2026-10-27 | Title *Technical Debt Has Nowhere to Grow* and a new subtitle; body identical to rc4. |
| 1.0-rc5 | 2026-10-26 | Title *Debt-Proof by Design*; the cover drops the tagline; body identical to rc4. Superseded the same week. |
| 1.0-rc4 | 2026-10-26 | Technical-debt lens; `[Table]` on every canonical entity; read-only markers stated precisely; FunkyORM references at 3.12.1. The rc1 files are in the repository history (commit `8141453`). |
| 1.0-rc1 | 2026-10-02 | First public release candidate. |

## Citing

See [`../CITATION.cff`](../CITATION.cff). In prose: *Smith, P. (2026). Slim Domain: Technical Debt Has Nowhere
to Grow (v1.0-rc6). Funcular Labs.*

## License

The paper is licensed under [CC BY 4.0](../LICENSE-DOCS.md).
