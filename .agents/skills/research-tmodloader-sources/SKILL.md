---
name: research-tmodloader-sources
description: Investigate unresolved Terraria/tModLoader API behavior or a named public Mod implementation for Convergence. Use for source-backed compatibility and design research; skip changes already supported by existing code, specifications, and version-matched evidence.
---

# Research tModLoader Sources

Resolve the API or prior-art question with reproducible evidence and a clear consequence for Convergence.

## Establish the question

Start with existing scoped research and source/version evidence. Reuse findings when the target version and relevant source still match; check freshness for mutable or compatibility-critical claims. Search only unresolved mechanisms rather than repeating a broad prior-art survey.

1. Convert the request into a small set of mechanisms, such as lethal-damage interception, revive channel authority, arena ejection, part ownership, or DPS-window timing.
2. Record the runtime/dependency versions relevant to the question, reusing `docs/VERSION_MATRIX.md` and matching evidence. Inspect project/manifest declarations when version resolution is uncertain; a narrow API lookup does not require an unrelated full toolchain inventory.
3. Define what evidence would change the design. Avoid browsing unrelated showcase content.
4. Choose implementation or audit-only mode. In audit-only mode, do not edit durable documents or create build/bytecode output; return proposed evidence entries and clearly say which files were not updated.

## Use primary sources in order

1. Search the pinned tModLoader documentation and official ExampleMod source.
2. Search the named dependency's official repository, documented API, changelog, and license.
3. Search official public repositories maintained by the Mod's authors or organization. Treat a personal repository as authoritative only when the project page, release channel, Mod listing, organization, or maintainer statement links it to the shipped Mod; record that evidence.
4. Use release tags or commit SHAs that match the target version when available.
5. Use secondary discussion only to locate primary code or to document an unresolved behavior; label it secondary.

For a narrow official-API lookup, record the pinned version/source URL, type/member, verified behavior, applicability, and any uncertainty in a short note; include the access date for remote verification. Use the full [evidence-template.md](references/evidence-template.md) for cross-Mod comparisons, design-changing findings, or copying/provenance questions. Shared repository/version/license details can be recorded once per survey and referenced by its findings.

Record the exact repositories, paths, symbols, and search terms examined for a negative result. Write “not found in this scoped survey,” never “no prior art exists” or a legal novelty claim. Verify every cited URL before finalizing the report. If a primary URL is unavailable, blocked, or rate-limited, label the status and access date, try a release/tag page or another official mirror, and keep the claim unverified unless a pinned primary source can support it.

Determine each surveyed commit's target versions from its `build.txt`, project files, dependency manifests, release notes, or tags. If none declare the target, record `unknown`; do not infer compatibility from recency or branch name.

## Inspect behavior, not just names

Select the paths below that can affect the question. A drawing API lookup does not require an unrelated death/rejoin audit.

- Trace authority from hook or packet entry to state mutation and replication.
- Identify whether code executes on client, server, or both.
- Follow identity, timing, random selection, spawn ownership, cleanup, disconnect, and rejoin paths.
- Look for packet bounds and sender validation.
- Distinguish gameplay entities from client-only drawing.
- Check failure paths, World unload, Mod unload, and Dedicated Server guards.
- Compare implementation with the pinned API source; do not assume a newer branch is compatible.
- Pair moving documentation such as `docs/stable` with a pinned tag/commit source for compatibility-critical claims. Label the moving page as current guidance, not reproducible historical evidence.

## Keep provenance boundaries

- Do not vendor repositories, binaries, extracted assets, decompiled output, recordings, or large source excerpts.
- Do not paste third-party implementation code into Convergence merely because it is public.
- Treat missing or ambiguous license terms as no permission to copy.
- Prefer independently written contracts and algorithms based on documented behavior.
- Quote only the minimum needed to identify an API or prove a finding.
- Record material design influence from another project in Convergence's research and IP provenance documents; a routine official API signature lookup does not require a new provenance entry.

## Synthesize for Convergence

For design-changing research, distinguish these categories where applicable; omit empty sections. A narrow API answer needs only the supported fact, source/version, and consequence for the task:

1. Confirmed API facts.
2. Observed prior-art patterns.
3. Inferences requiring a prototype.
4. Patterns to adopt.
5. Patterns to reject and why.
6. Version or license uncertainties.
7. Concrete tests and architecture consequences.

Do not declare multiplayer correctness from source inspection alone. Recommend a minimal Dedicated Server prototype when the unresolved claim depends on hooks, packet timing, death suppression, movement correction, or inter-Mod conflicts.

## Update durable knowledge

In implementation mode, extend an existing relevant research note when the finding will be reused; create a new `docs/research/` report only for a distinct durable question. Routine lookups can remain in the task result or the owning document. Update `docs/SOURCES.md` for new durable external sources and `docs/IP_PROVENANCE.md` for material design influence, using links rather than repeating the full finding. In audit-only mode, return findings without writing them.
