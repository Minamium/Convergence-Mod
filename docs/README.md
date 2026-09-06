---
doc_id: docs.entrypoint
document_type: index
status: accepted
owners:
  - project
last_reviewed: 2026-09-07
source_of_truth_for:
  - documentation.read_order
aliases:
  - documentation home
  - start here
related_code: []
related_docs:
  - docs.search-index
  - project.status
  - handoff.windows
---

# Documentation Home

This page is the starting point for humans and coding agents. The repository includes an experimental Raid playtest build; do not infer production completeness from a design document. Verify current implementation and evidence in [Status](STATUS.md).

## Read by task

For code/behavior work, start with [Current build](STATUS.md#current-build), [Verification state](STATUS.md#verification-state), and [Next change](STATUS.md#next-change), then inspect the affected code. Text-only fixes need only the affected passage and its conventions. This page is a routing map, not a mandatory reading queue.

| Task | Additional context, only as relevant |
|---|---|
| Markdown, wording, repository housekeeping | Target file; [documentation schema](DOCUMENTATION_SYSTEM.md#front-matter-record) only if metadata/structure changes |
| VFX, UI, assets | Target code and affected [visual spec](encounters/first-severance/VISUAL_SPEC.md) section; [asset pipeline](ASSET_PIPELINE.md) and attribution rules for distributable assets |
| HP, timing, radius, damage tuning | Target constants, callers, and affected [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) section |
| Combat, recovery, arena | Affected feature spec and code; [revive spec](encounters/first-severance/REVIVE_SPEC.md) or [arena rules](ARENA_INFRASTRUCTURE.md) for that subsystem; follow linked active ADRs only where a decision matters |
| Authority, protocol, lifecycle, module boundaries | Relevant [architecture](ARCHITECTURE.md) / [network](NETWORK_ARCHITECTURE.md) sections and the ADRs governing the changed contract |
| Work order or new feature scope | Current section of the [implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md); backlog only when explicitly promoting an idea |
| API research, real build, new workstation | [Version matrix](VERSION_MATRIX.md), existing scoped research, and relevant [Windows runbook](runbooks/WINDOWS_DEVELOPMENT.md) procedure |
| WotG-level presentation without losing multiplayer Raid mechanics | [WotG benchmark](research/WOTG_RAID_BENCHMARK.md): video metadata/access limits, pinned public source, independent design proposals |
| Playtesting when only one person is available | [Single-operator testing](runbooks/SINGLE_OPERATOR_TESTING.md): build-gated one-member start or separately authorized two-client assistance |

Use `rg` for headings, symbols, and topic names before opening a long document. Reuse sections already read until relevant files or scope change. [Search Index](INDEX.md) and [Glossary](GLOSSARY.md) help only when locating an unfamiliar topic/name. The [Windows handoff](handoff/WINDOWS.md) is a brief resume entry; completed slices are [historical context](history/2026-09-07-pre-consolidation.md), not prerequisites.

Select checks from the shared [Verification Matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md). Detailed test cases are references, not an instruction to run every case.

## Sources of truth

| Question | Authoritative document |
|---|---|
| What is implemented now? | [Status](STATUS.md) |
| What is the current first Raid? | [First Severance encounter specification](encounters/first-severance/ENCOUNTER_SPEC.md) |
| What gets implemented, and in what order? | [First Severance implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) |
| How does Downed/Revive feel to players? | [Revive specification](encounters/first-severance/REVIVE_SPEC.md) |
| What authority invariants are mandatory? | [ADR-0002](adr/0002-server-authoritative-encounters.md), [ADR-0005 foundation](adr/0005-server-authoritative-downed-revive.md), and [ADR-0011 recovery policy](adr/0011-instant-revival-and-recipient-lockout.md) |
| What is deliberately postponed? | [First Severance backlog](encounters/first-severance/BACKLOG.md) |
| How should Windows be prepared? | [Windows development runbook](runbooks/WINDOWS_DEVELOPMENT.md) |
| How are documents indexed? | [Documentation system](DOCUMENTATION_SYSTEM.md) |
| Which dependency versions are allowed? | [Version matrix](VERSION_MATRIX.md) |

If two active documents disagree, the document declaring the relevant `source_of_truth_for` topic wins. ADRs override ordinary design documents for architecture, authority, protocol, persistence, dependency, release-safety, and rights decisions.

## Document classes

- `docs/encounters/<feature>/`: current feature experience, implementation sequence, visual constraints, and backlog.
- `docs/runbooks/`: repeatable procedures independent of one handoff date.
- `docs/handoff/`: point-in-time transfer state and next-work queue.
- `docs/evidence/`: sanitized verification record formats; local logs and personal paths stay untracked.
- `docs/adr/`: immutable records for expensive structural decisions.
- `docs/research/`: evidence and prior-art notes; research does not override accepted specifications.
- existing root-level documents: stable project-wide paths retained to avoid breaking repository instructions and Skills.

## Maintenance rule

Update only the owner of a changed fact: [Status](STATUS.md) for implementation/verification state, feature spec for behavior, plan for work order, ADR for a structural decision. A wording fix or tuning adjustment does not require updating all of them or writing a new report. Other documents link to the owner instead of copying its state.

After indexed-document edits are settled, regenerate and check once:

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py --write-catalog .
```

The generated YAML catalog is an index, not a second source of truth.
