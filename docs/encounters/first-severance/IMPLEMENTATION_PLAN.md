---
doc_id: encounter.first-severance.plan
document_type: plan
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-07
source_of_truth_for:
  - first_severance.implementation_sequence
aliases:
  - First Severance implementation plan
  - Windows implementation queue
related_code:
  - Content/Encounters/FirstSeverance
  - Common/Encounters
  - Common/Raids/Revive
  - Common/Networking
related_docs:
  - encounter.first-severance.spec
  - project.status
  - handoff.windows
---

# First Severance Implementation Plan

Current implementation/evidence is in [Status](../../STATUS.md). This is the forward queue, not a requirement to replay completed slices.

## Completed consolidation

1. **Preservation complete:** checkpoint latest 0.2.17 and prior history on GitHub; establish one canonical checkout and retain all former copies.
2. **Complete:** consolidate current docs and the already-integrated targeted-reading/verification workflow. Archive old instructions without losing evidence; generate the catalog once after the edit batch.
3. **Complete:** make local toolchain discovery, configuration and source/package identification reproducible, with private paths and binaries excluded.
4. **Complete:** common packet registration and bounded definition-specific dispatch, with matching-session guards. No second Raid or global feature switch.
5. **Complete:** fight-owned recovery, actor, attack and passive telemetry collaborators; one authoritative tick/termination/cleanup orchestrator.
6. **Complete:** auto-discovered feature tests, compiled codec and tooling checks; lightweight CI separated from an optional pinned-environment Mod-build job. Provisioning that runner and live integration remain unrun, not implied successes.

These stages were committed/pushed independently. Evidence and remaining user-owned smoke are linked from [Status](../../STATUS.md). Continue from current main; no copied checkout or completed migration step is required.

## Invariants

- Server or Single Player local authority owns lifecycle, roster, ticks, assignments, entity spawn, feature damage gates, committed actor life, Downed/Revive, victory, and cleanup.
- Clients send bounded intent and render read-only snapshots/events. No custom packet reports DPS, position-check success, revive completion, life, or player identity as truth.
- `Common` does not depend on `Content` or `Client`; feature behavior stays in `Content/Encounters/FirstSeverance`.
- Calamity access stays in `Common/Compatibility/Calamity` behind project-owned contracts.
- Every transient actor is registered to exact `FightId` ownership immediately and cleanup is idempotent.
- Terminal snapshot/outcome is committed before the active runtime and player projections are released.
- Existing numeric packet IDs are never renumbered during the feature rename.

The first slice assumes cooperative multiplayer with unmodified clients. Terraria/tModLoader supplies ordinary movement and combat facts observed by the server; custom-message authority is not a claim of anti-cheat against a modified client. The exact normal-hit and lethal-hook seams must be measured on the pinned runtime before their respective adapters are enabled.

## Follow-on work, not current gates

Production lethal-hit collection/Calamity coexistence, durable rejoin identity, outsider policy, final assets/rewards and release acceptance remain separate scoped tasks. Use [the verification selector](../../../.agents/skills/develop-convergence-raids/references/verification-matrix.md) for daily work; consult only the affected sections of [Test Plan](../../TEST_PLAN.md) for wider acceptance.

GUI checks belong to the user unless explicitly delegated. Report specific unrun checks; do not treat a build as multiplayer success.

## Historical plan

The completed Windows baseline/rename and original Slices 0–7 are preserved in the [historical record](../../history/2026-09-07-pre-consolidation.md). Their old activation-denied conditions do not disable the current development experiment.
