# Slim Domain — agent guidance

[`Slim_Domain_agent_guidance_v1.0-rc7.md`](Slim_Domain_agent_guidance_v1.0-rc7.md) is the paper compressed
into instructions for a coding agent: the rules as MUST / SHOULD / NEVER, the decision procedures, the
reference shapes, a pull-request checklist, and the anti-patterns to flag on sight.

## How to use it

1. Copy the file into your repository's agent instructions: `CLAUDE.md`, `AGENTS.md`,
   `.github/copilot-instructions.md`, or whatever your tools read. A one-line `@` import works too.
2. Fill in **§0, Team settings** — which data-access road you are on, which namespaces may touch the
   data-access library, where entities, business objects, services and contracts live.
3. Delete the sections that do not apply (for example §2.7 if you are not using repositories, or §2.8 if
   you have no entity generator).
4. Add the architecture test from §4.6 to your test project, so the main rule is a failing build rather
   than a review comment.

The [Switchback sample](../samples/Switchback/) has a filled-in example: see its
[`AGENTS.md`](../samples/Switchback/AGENTS.md).

When this file and the paper disagree, the paper wins. The guidance is versioned with the paper; this copy
matches v1.0-rc7 (rc4 added the technical-debt lens table and the `[Table]` rule for canonical entities; rc5 and rc6
changed only the paper's title page; rc7 retitled it again and carried the cover's gravity metaphor into three
passages of the body, none of which alters a rule).

## License

[CC BY 4.0](../LICENSE-DOCS.md).
