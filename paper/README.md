# Slim Domain — the paper

**Slim Domain: A Domain-Structuring Pattern for .NET — Persistence Contracts Separated from Business Behavior**
Paul Smith, Funcular Labs · Version 1.0, Release Candidate 1 · October 2026

| File | Use it for |
|---|---|
| [`Slim_Domain_v1.0-rc1.pdf`](Slim_Domain_v1.0-rc1.pdf) | Reading. 75 pages, 22 listings, two appendices. |
| [`Slim_Domain_v1.0-rc1.docx`](Slim_Domain_v1.0-rc1.docx) | Reviewing with tracked changes or comments. |

The companion digest for coding agents is in [`../guidance/`](../guidance/). The companion sample solution is in
[`../samples/Switchback/`](../samples/Switchback/).

## Abstract

Slim Domain is a domain-structuring pattern for .NET systems — not an application architecture and not a
library. Relative to a disciplined service layer over Dapper or Entity Framework Core, it changes five things:
reads are shaped to the use case rather than to the object graph; the canonical entity is the table and
nothing more; business objects acquire the entity shape by inheriting or wrapping it inside a trust
boundary, so in-process DTO mapping disappears except where a defined boundary warrants it; services alone
persist, and nouns never save themselves; and recurring column groups become generator-proposed,
human-approved interfaces that centralize cross-cutting rules such as audit stamping. It is intended for
teams of mixed experience levels, including those common in regulated, high-security environments.

The pattern rests on four principles: slim entities as pure data contracts; enriched business objects, via
inheritance or composition, that acquire the persistence shape and add behavior; service-gated persistence,
in which the services layer is the sole consumer of the data-access library; and use-case-shaped reads, in
which every query returns a shape carrying only what the operation uses. Where a library maps only the
intersection of columns and properties, business objects need no mapping attributes even on writes — a
convenience the pattern welcomes and does not require.

This paper defines the pattern and illustrates it with C# examples drawn from a fictitious reference
solution, the Aegis Platform. It states what the pattern is and is not, which application architectures it
fits inside and where it meets friction, and how teams on any data-access library can adopt its central
habit; provides a measured feature comparison of leading micro-ORMs; demonstrates practical techniques for
object-graph loading, transactions, non-canonical projections, and mapping exceptions; states the pattern's
consequences and the discipline each cost requires, with particular attention to healthcare and financial
services; compares the discipline the pattern requires against classic strict n-tier layering; and shows how
teams can present a Slim Domain solution in conventional n-tier terms — including optional build-time
enforcement — without conversion or rework.

The argument rests on mechanism — how each rule constrains the defect classes a regulated team fears — and
on a structural measurement of two production systems (Appendix B) that describes what the pattern's rules
oblige developers to write, compared with a legacy Entity Framework and active-record codebase from the same
lineage. That measurement is not a comparison with a modern service layer and is not offered as proof of
superiority; Section 11 states the evidentiary limits and proposes the measurements that should follow. The
central claim is narrower: once the deliberate engineering discipline required by any layered architecture
is accounted for, Slim Domain changes the default path so that the discipline is easier to apply, easier to
audit, and harder to violate accidentally.

## Status: release candidate, open for review

This is the first public version. It has been through several rounds of adversarial review by AI reviewers
(the method and the disclosure are in Appendix A), and it has not yet been reviewed by people outside
Funcular Labs. That is what this release is for.

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

## Versions

| Version | Date | Notes |
|---|---|---|
| 1.0-rc1 | 2026-10-02 | First public release candidate. |

## Citing

See [`../CITATION.cff`](../CITATION.cff). In prose: *Smith, P. (2026). Slim Domain: A Domain-Structuring
Pattern for .NET — Persistence Contracts Separated from Business Behavior (v1.0-rc1). Funcular Labs.*

## License

The paper is licensed under [CC BY 4.0](../LICENSE-DOCS.md).
