---
doc_id: policy.release-process
document_type: policy
status: accepted
owners:
  - project
  - quality
last_reviewed: 2026-09-12
source_of_truth_for:
  - policy.release_process
aliases:
  - release process
  - release gate
related_code:
  - build.txt
  - .github/workflows/repository-checks.yml
related_docs:
  - policy.ip-provenance
  - verification.test-plan
  - project.status
---

# Release Process

Playable development packages and Workshop materials exist. This policy separates a build from approval of a particular public/production artifact. [Status](STATUS.md) owns the latest built package and outstanding evidence.

## Development-publication preparation

The user requested materials for sharing the current playable development build, not completion of production content. `description.txt`, `description_workshop.txt`, `icon.png` and `icon_workshop.png` are the publication assets. This preparation is not a Publish action or a declaration that the gate below passed. Current version comes from `build.txt`, not the outdated0.2.26 planning excerpt. Keep `Convergence (Development Build)` as the public-facing name.

Before publishing a candidate: choose source/asset distribution terms, explicitly approve/document any solo-debug exception for the intended development-test channel (the production rule below remains in force), confirm the exact package's load/setup/reward smoke, inspect package exclusions, and capture current gameplay screenshots. Do not invent license permissions or mark a compiled package release-approved.

The owner previously reported completing the publication interaction and possibly waiting for approval. The current Workshop URL/visibility/approval and latest uploaded version are **not verified by this documentation checkpoint**; check them only when publication is in scope. The2026-09-08 private-repository observation and comments-based report wording are historical, not proof of current GitHub/Workshop visibility. This documentation work changes no visibility, Steam agreement or upload setting.

The [official Workshop guide](https://github.com/tModLoader/tModLoader/wiki/Workshop#update-mod-icon), checked2026-09-08, specifies80×80 `icon.png`, up to512×512 `icon_workshop.png`, and Workshop BBCode in `description_workshop.txt`. The generated emblem is promotional artwork, not a gameplay screenshot. Suggested actual screenshots: preparation/arena, one readable Stack/Spread, Phase-II lattice and Phase-III swords. Hide names/chat where practical; captions describe pictured current content, not future features. The owner approves screenshots/publication; GUI assistance requires explicit scope for that run, not general build permission.

## Version sources

- User-facing Mod version: `build.txt`.
- Network protocol version: `EncounterProtocol.CurrentVersion`.
- Future save schema versions: owned by each persistent schema.
- Git tag: `v<build.txt version>`.

These versions change independently. A protocol or save break must be called out explicitly in the changelog.

## Pre-release gate

1. Repository checks pass.
2. Pinned tModLoader Build + Reload passes.
3. Dedicated Server load passes.
4. Required 2/3/4-player and failure matrix passes.
5. Calamity compatibility and dependency provenance are recorded.
6. Asset attribution and redistribution audit passes.
7. Source and asset licenses are selected, documented, and compatible with every contribution.
8. `includeSource` is reviewed against the selected source license; it remains `false` while no license is granted.
9. A working private security reporting channel has been tested.
10. GitHub secret scanning and push protection are enabled where the repository plan supports them.
11. `.tmod` contents contain no docs, tools, concept/raw assets, dependency binaries, saves, logs, or secrets.
12. Changelog and release notes describe compatibility and known limitations.
13. Artifact checksum is recorded.
14. Workshop upload remains manual until credential and rollback policy are reviewed.
15. Disable development solo admission with `-p:ConvergenceDevelopmentSolo=false` on the public-candidate build. Verify the compiled flag is false and one-player Core activation returns `first_severance.roster_too_small`, while normal two-player preparation still works. Do not publish the default development package. [ADR-0020](adr/0020-development-solo-admission-and-terminal-hud.md) owns this temporary development exception; a balanced public solo mode requires a separate decision.

## CI tiers

| Tier | Purpose | Current status |
|---|---|---|
| L0 | Repository structure, links, provenance, boundaries | Automated |
| L1 | Terraria-independent domain tests | Automated in GitHub Actions |
| L2 | Pinned tML + Calamity build/load | Manual until legal and reproducible dependency provisioning exists |
| L3 | Dedicated Server multiplayer smoke/soak | Manual; later protected runner/nightly |

Never run untrusted fork code on a self-hosted runner containing Steam credentials, game files, private Mods, or Workshop tokens.

## Branch policy

Use short-lived branches and squash merges under [shared development](../CONTRIBUTING.md#shared-development). Keep compatibility updates, pure refactors, encounter tuning, and bulk assets separate. After the first successful Actions run, protect `main` with the stable repository-check job, linear history, no force push, and no deletion. Additional review requirements begin when more maintainers join.
