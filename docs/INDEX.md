---
doc_id: docs.search-index
document_type: index
status: accepted
owners:
  - project
last_reviewed: 2026-09-06
source_of_truth_for:
  - documentation.search_index
aliases:
  - document index
  - search map
related_code:
  - tools/docs_catalog.py
related_docs:
  - docs.entrypoint
  - docs.system
---

# Documentation Search Index

Search by `doc_id`, topic, alias, or source path. Machine-readable metadata is generated at [`catalog/documents.yml`](catalog/documents.yml); schema and maintenance rules are in [Documentation System](DOCUMENTATION_SYSTEM.md).

## Thirty-second lookup

```bash
# Locate current-state sections without searching historical slices
rg -n "^## (Current build|Verification state|Next change)" docs/STATUS.md

# Stable ID, alias, mechanic, or code owner
rg -ni "encounter\.first-severance|第一断絶|頭割り|Common/Raids/Revive" docs

# Confirm that the generated catalog matches every indexed/excluded Markdown file
python3 tools/docs_catalog.py --check
```

Start with this curated map, then use [`catalog/documents.yml`](catalog/documents.yml) for deterministic tooling. Search hits in `research/`, `backlog`, `historical`, or `superseded` material are context—not current authority; follow `source_of_truth_for` back to the active owner.

## Current high-signal documents

| Document ID | Type / status | Owns | Useful search terms |
|---|---|---|---|
| `project.status` | status / accepted | implementation inventory | implemented, missing, inert, verification, playable |
| `handoff.windows` | handoff / accepted | historical 2026-09-04 Windows transfer checkpoint | transfer history, ModSources, Mac audit |
| `encounter.first-severance.overview` | overview / accepted | feature reading map | First Severance, 第一断絶, legacy ThirdSeverance |
| `encounter.first-severance.spec` | spec / accepted | active encounter loop and outcomes | Pylon, DPS check, Stack, 頭割り, Spread, 散開, Core exposure |
| `encounter.first-severance.plan` | plan / accepted | implementation order and gates | slice, rename, adapter, executor, Definition of Done |
| `encounter.first-severance.visual` | spec / provisional | prototype boss appearance within the accepted one-body/readability boundary | central Core, broken ring, side arms, Shielded, Exposed |
| `encounter.first-severance.revive` | spec / accepted | Downed/Revive player rules | Resuscitation Kit, 蘇生キット, instant revival, recipient lockout, PreKill |
| `encounter.first-severance.backlog` | backlog / accepted | deferred ideas | Part Break, Effigy, Last Stand, Split Reality |
| `development.windows` | runbook / accepted | repeatable workstation setup | tModLoader, Calamity, .NET 8, Build + Reload, Dedicated Server |
| `research.wotg-raid-benchmark` | research / provisional | reference evidence, not gameplay authority | [WotG](research/WOTG_RAID_BENCHMARK.md), Avatar, Nameless, composite, telegraph, audio, video chapters |
| `development.single-operator-testing` | runbook / provisional | proposed local testing method, not runtime evidence | [一人二窓](runbooks/SINGLE_OPERATOR_TESTING.md), localhost, debug Down, God Mode, NPC limitations |
| `verification.evidence` | evidence / accepted | build/test record format | commit SHA, versions, checksums, logs |
| `docs.system` | governance / accepted | documentation data model | front matter, catalog, SQLite, semantic search, doc_id |

## Architecture and policy map

| Topic | Read first | Related paths |
|---|---|---|
| Authority and replica | [Architecture](ARCHITECTURE.md), [ADR-0002](adr/0002-server-authoritative-encounters.md) | `Common/Encounters`, `Common/Networking/Replication` |
| Packet validation | [Network Architecture](NETWORK_ARCHITECTURE.md) | `Common/Networking` |
| Arena and Barrier | [Arena Infrastructure](ARENA_INFRASTRUCTURE.md), [ADR-0003](adr/0003-in-world-logical-arena.md) | current `Content/Encounters/FirstSeverance/*Arena*` |
| Downed/Revive authority | [Revive Spec](encounters/first-severance/REVIVE_SPEC.md), [ADR-0011](adr/0011-instant-revival-and-recipient-lockout.md), [ADR-0005 foundation](adr/0005-server-authoritative-downed-revive.md) | `Common/Raids/Revive`, feature `Revive/` boundary |
| Calamity isolation/removal | [ADR-0004](adr/0004-calamity-compatibility-boundary.md), [ADR-0006](adr/0006-staged-calamity-independence.md) | `Common/Compatibility/Calamity`, `build.txt` |
| Feature ownership | [Repository Layout](REPOSITORY_LAYOUT.md), [Content Authoring](CONTENT_AUTHORING.md) | `Content/Encounters/<Feature>`, `Client/Encounters/<Feature>` |
| Testing | [Test Plan](TEST_PLAN.md), [Evidence](evidence/README.md) | `Tests/Convergence.DomainTests`, `.github/workflows` |
| Asset rights | [Asset Pipeline](ASSET_PIPELINE.md), [IP Provenance](IP_PROVENANCE.md) | `Assets/ATTRIBUTION.md` |
| Repository Skills | [Codex Skills survey](research/CODEX_SKILLS_SURVEY.md), [`AGENTS.md`](../AGENTS.md) | `.agents/skills/develop-convergence-raids`, `.agents/skills/research-tmodloader-sources` |

## Name and migration searches

- Target feature name: `First Severance`, `FirstSeverance`, `first_severance`, `第一断絶`.
- Historical implementation names: `Third Severance`, `ThirdSeverance`, `third_severance`.
- Working boss name: `The Null Cantor`, `無響の唱導者`; it is provisional, not accepted branding.
- Working revive item: `Resuscitation Kit`, `蘇生キット`; it is provisional.
- Removed-from-MVP concepts: `Part Break`, `Targeted Line`, `Personal Effigy`, `Split Reality`, `Last Stand`, `Crown`, `Wings`, `Heart Casing`.

Do not globally replace legacy strings in historical ADRs, research, changelog entries, or completed-rename instructions. Current code and tests use the `FirstSeverance` identity; follow the remaining sequence in the implementation plan.
