---
doc_id: policy.release-process
document_type: policy
status: accepted
owners:
  - project
  - quality
last_reviewed: 2026-09-14
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

For the owner's requested 0.3.1 playtest distribution, retain the existing rights boundary: publish the completed Mod for players, but do not introduce MIT/another open-source license or grant standalone reuse of code/art/music. Creator-specific terms still govern licensed assets. The optional request for a future license choice is not treated as answered by silence. If a different license is chosen, review its contribution/asset scope before applying it; GitHub visibility and this build do not grant that license. Keep `includeSource=false`. This is a narrow release permission, not a change of ownership or universal redistribution permission.

The [existing Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3798073077) was verified on 2026-09-14 through Steam public metadata: item3798073077, app1281930, title `Convergence (Development Build)`, public visibility0 and banned0. This does not mean 0.3.1 has been uploaded. The owner performs the new tModLoader Publish step; GitHub release work changes no Steam agreement or upload setting. Earlier approval/private-repository observations are historical.

The [official Workshop guide](https://github.com/tModLoader/tModLoader/wiki/Workshop#update-mod-icon), checked2026-09-08, specifies80×80 `icon.png`, up to512×512 `icon_workshop.png`, and Workshop BBCode in `description_workshop.txt`. The generated emblem is promotional artwork, not a gameplay screenshot. Suggested actual screenshots: preparation/arena, one readable Stack/Spread, Phase-II lattice and Phase-III swords. Hide names/chat where practical; captions describe pictured current content, not future features. The owner approves screenshots/publication; GUI assistance requires explicit scope for that run, not general build permission.

## 0.3.x GitHub public-test releases

On 2026-09-14 the owner explicitly selected **0.3.1**, authorized its GitHub release and retained the tModLoader Publish step. The earlier unselected 0.3.0 proposal is superseded. Publish this as a **GitHub prerelease / public playtest**, not a production-certified build. [0.3.1 notes](releases/0.3.1.md) record its checks, known issues and owner-approved channel exceptions.

Prepare the candidate from integrated main after the relevant fixes from both encounter branches are included. Release notes should cover Doll raid entry/Ready/recovery/rewards, Ghost Samurai, exact dependencies/protocol, known limitations, and the solo-allowed/multiplayer-recommended policy. README is an English content/setup overview, not a substitute for those notes or a marketing slogan.

Before creation, close or explicitly record the [pre-release gate](#pre-release-gate), particularly source/asset terms, exact music permissions, solo admission and load/re-summon evidence. For this explicitly requested development channel, one-player admission remains enabled and the full production compatibility matrix is deferred and labelled `not_run`, not passed. The owner accepts the existing playable prototype for broader testing, not final balance. The [Ghost Samurai unload failure](encounters/ghost-samurai/ENCOUNTER_SPEC.md#lifecycle-handoff--2026-09-14) remains a disclosed known issue requiring work in its owning feature; documentation does not fix it. Never silently use these exceptions for a later production release.

Once the owner approves a specific candidate, tag `v<build.txt version>` at its source commit and create the prerelease with the matching `.tmod`, checksum, compatibility notes and test evidence. GitHub's automatically generated source archive needs the same rights review as the package: `includeSource = false` does not exclude tracked sources or assets from that archive. Keep the prior artifact/tag available for rollback; never retarget a published version to different bytes. Workshop publication is a separate, explicitly authorized operation.

## Version sources

- User-facing Mod version: `build.txt`.
- Network protocol version: `EncounterProtocol.CurrentVersion`.
- Future save schema versions: owned by each persistent schema.
- Git tag: `v<build.txt version>`.

These versions change independently. A protocol or save break must be called out explicitly in the changelog.

## Pre-release gate

This is the full production/compatibility gate. A specifically authorized public-test prerelease may disclose incomplete runtime checks and its exact development exceptions in its release notes; it must not claim this full gate passed. Rights, provenance, source identity and package exclusion checks are not waived by a test label.

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
15. Preserve solo admission: **multiplayer recommended is not multiplayer required**, including public releases. Run `pwsh -NoProfile -File tools/check-package-admission.ps1 -PackagePath <exact Convergence.tmod to publish>` and verify1–4-player admission. Recheck if GUI Build + Reload replaces the package before Publish. Observe one-player Ready/start and matching-peer multiplayer; do not claim these runtime checks from a compiler pass. [ADR-0025](adr/0025-public-solo-admission.md) retires the old release opt-out. A new solo/companion balance design is not a prerequisite for admission.

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
