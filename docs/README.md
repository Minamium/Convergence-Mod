---
doc_id: docs.entrypoint
document_type: index
status: accepted
owners:
  - project
last_reviewed: 2026-09-04
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

## Fast read for a new workstation

Read these in order:

1. [Repository README](../README.md) — product and repository overview.
2. [Windows handoff](handoff/WINDOWS.md) — current checkpoint and exact next actions.
3. [Status](STATUS.md) — what exists, what is disconnected, and what is unverified.
4. [First Severance overview](encounters/first-severance/README.md) — feature document map.
5. [Version matrix](VERSION_MATRIX.md) and [Windows development runbook](runbooks/WINDOWS_DEVELOPMENT.md).
6. [Architecture](ARCHITECTURE.md), [Network architecture](NETWORK_ARCHITECTURE.md), and the linked ADRs before editing authority code.

Use [Search Index](INDEX.md) when looking for a topic, stable document ID, alias, or related source path. Use [Glossary](GLOSSARY.md) whenever `FirstSeverance` and the legacy `ThirdSeverance` names appear together.

## Sources of truth

| Question | Authoritative document |
|---|---|
| What is implemented now? | [Status](STATUS.md) |
| What is the current first Raid? | [First Severance encounter specification](encounters/first-severance/ENCOUNTER_SPEC.md) |
| What gets implemented, and in what order? | [First Severance implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) |
| How does Downed/Revive feel to players? | [Revive specification](encounters/first-severance/REVIVE_SPEC.md) |
| What authority invariants are mandatory? | [ADR-0002](adr/0002-server-authoritative-encounters.md) and [ADR-0005](adr/0005-server-authoritative-downed-revive.md) |
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

Update [Status](STATUS.md) whenever implementation state changes. Update the feature plan when work order changes, the feature spec when player-visible behavior changes, and an ADR when a structural invariant changes. Then run:

```bash
python3 tools/docs_catalog.py --write
python3 tools/docs_catalog.py --check
python3 tools/repository_checks.py
python3 tools/validate_yaml.py
```

The generated YAML catalog is an index, not a second source of truth.
