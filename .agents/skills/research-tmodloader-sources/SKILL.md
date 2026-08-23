---
name: research-tmodloader-sources
description: Research current Terraria/tModLoader APIs and prior implementations in official documentation and public Mod source repositories. Use when investigating multiplayer authority, packets, player hooks, death/respawn, bosses, arenas, barriers, UI, rendering, compatibility, performance, or another Mod's implementation before designing or changing Convergence. Produces source-linked evidence and license-aware design findings; it does not authorize copying third-party code or assets.
---

# Research tModLoader Sources

Produce reproducible, license-aware prior-art research before changing Convergence.

## Establish the question

1. Convert the request into a small set of mechanisms, such as lethal-damage interception, revive channel authority, arena ejection, part ownership, or DPS-window timing.
2. Record the target Terraria, tModLoader, Calamity, .NET, and C# versions from the repository's `docs/VERSION_MATRIX.md`, `build.txt`, project/props files, and dependency manifests. Record conflicts or missing declarations instead of guessing.
3. Define what evidence would change the design. Avoid browsing unrelated showcase content.
4. Choose implementation or audit-only mode. In audit-only mode, do not edit durable documents or create build/bytecode output; return proposed evidence entries and clearly say which files were not updated.

## Use primary sources in order

1. Search the pinned tModLoader documentation and official ExampleMod source.
2. Search the named dependency's official repository, documented API, changelog, and license.
3. Search official public repositories maintained by the Mod's authors or organization. Treat a personal repository as authoritative only when the project page, release channel, Mod listing, organization, or maintainer statement links it to the shipped Mod; record that evidence.
4. Use release tags or commit SHAs that match the target version when available.
5. Use secondary discussion only to locate primary code or to document an unresolved behavior; label it secondary.

For every finding, capture repository URL, exact file path or documentation page, tag/commit/branch, access date, relevant type/member, observation, inference, applicability, and license. Use the template in [evidence-template.md](references/evidence-template.md).

Record the exact repositories, paths, symbols, and search terms examined for a negative result. Write “not found in this scoped survey,” never “no prior art exists” or a legal novelty claim. Verify every cited URL before finalizing the report. If a primary URL is unavailable, blocked, or rate-limited, label the status and access date, try a release/tag page or another official mirror, and keep the claim unverified unless a pinned primary source can support it.

Determine each surveyed commit's target versions from its `build.txt`, project files, dependency manifests, release notes, or tags. If none declare the target, record `unknown`; do not infer compatibility from recency or branch name.

## Inspect behavior, not just names

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
- Record ideas influenced by another project in Convergence's research and IP provenance documents.

## Synthesize for Convergence

Separate the report into:

1. Confirmed API facts.
2. Observed prior-art patterns.
3. Inferences requiring a prototype.
4. Patterns to adopt.
5. Patterns to reject and why.
6. Version or license uncertainties.
7. Concrete tests and architecture consequences.

Do not declare multiplayer correctness from source inspection alone. Recommend a minimal Dedicated Server prototype for hooks, packet timing, death suppression, movement correction, and inter-Mod conflicts.

## Update durable knowledge

In implementation mode, place task-specific research under `docs/research/`. Update `docs/SOURCES.md` for durable external sources and `docs/IP_PROVENANCE.md` when a design is materially influenced by another project. Keep links direct and include version/commit evidence. In audit-only mode, return the same structured material without writing it.
