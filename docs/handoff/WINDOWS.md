---
doc_id: handoff.windows
document_type: handoff
status: accepted
owners:
  - project
last_reviewed: 2026-09-07
source_of_truth_for:
  - handoff.windows.2026-09-04
aliases:
  - Windows handoff
  - desktop migration
related_code:
  - Content/Encounters/FirstSeverance
  - Common/Raids/Revive
  - Tests/Convergence.DomainTests
related_docs:
  - project.status
  - development.windows
  - encounter.first-severance.plan
---

# Windows Development Handoff

## Resume current work

1. Read only [Status](../STATUS.md)'s current build, constraints and next change.
2. Select the task's owning spec and checks via [docs routing](../README.md#read-by-task); consult [the implementation plan](../encounters/first-severance/IMPLEMENTATION_PLAN.md) for structural work.
3. Use the [Windows runbook](../runbooks/WINDOWS_DEVELOPMENT.md) for local setup/build. Do not recreate per-edit source copies.

The current First Severance development Raid is playable after Core validation and Ready. Its baseline/rename are complete. [The encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md) owns phase behavior and [the recovery spec](../encounters/first-severance/REVIVE_SPEC.md) owns Raid Down/revival.

## Source and evidence

Use one canonical checkout named Convergence. The tModLoader ModSources entry must reference that same checkout; confirm the actual source and local targets before building. Never replace it with an older editing snapshot. Keep personal absolute paths in ignored local configuration, not this shared handoff.

Before editing, inspect branch/remote/dirty state. Preserve other work. Commit coherent completed batches and normally push the GitHub branch; no history rewriting or routine copy creation.

A successful build is not a game check. Hand the user the specific changed load/playtest items, marked not_run until observed. Normal operation is user GUI plus a friend; [solo procedures](../runbooks/SINGLE_OPERATOR_TESTING.md) are opt-in.

## Remaining safety boundaries

Ordinary Terraria/Calamity lethal-hook interception and robust rejoin identity remain deferred production seams. This does not block the authorized Raid-owned damage/recovery experiment. Preserve server authority, one-Fight ownership and idempotent cleanup.

## Historical handoff

The original Windows checkpoint, source revision and completed slice instructions are retained in [the historical record](../history/2026-09-07-pre-consolidation.md); they are not current startup instructions.
