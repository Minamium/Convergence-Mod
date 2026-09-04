---
doc_id: policy.release-process
document_type: policy
status: accepted
owners:
  - project
  - quality
last_reviewed: 2026-09-04
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

There is no release artifact yet. This policy prevents a green repository check from being mistaken for a playable build.

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

## CI tiers

| Tier | Purpose | Current status |
|---|---|---|
| L0 | Repository structure, links, provenance, boundaries | Automated |
| L1 | Terraria-independent domain tests | Automated in GitHub Actions |
| L2 | Pinned tML + Calamity build/load | Manual until legal and reproducible dependency provisioning exists |
| L3 | Dedicated Server multiplayer smoke/soak | Manual; later protected runner/nightly |

Never run untrusted fork code on a self-hosted runner containing Steam credentials, game files, private Mods, or Workshop tokens.

## Branch policy

Use short-lived branches and squash merges. Keep compatibility updates, pure refactors, encounter tuning, and bulk assets separate. After the first successful Actions run, protect `main` with the stable repository-check job, linear history, no force push, and no deletion. Additional review requirements begin when more maintainers join.
