---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-07
source_of_truth_for:
  - project.implementation_status
aliases:
  - current state
  - implementation inventory
related_code:
  - Common
  - Content/Encounters/FirstSeverance
  - Tests/Convergence.DomainTests
related_docs:
  - handoff.windows
  - encounter.first-severance.plan
---

# Project Status

## Current build

Development **0.2.19**, protocol **18**. Sanctuary Stack/Spread, longer Final warnings, natural-spawn suppression, flowing beam/crush materials and the Null Refrain victory weapon are implemented; HP is unchanged. Version declarations live in [build.txt](../build.txt) and [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs); exact mechanics/tuning belong to the linked specifications/code.

- First Severance is playable: Core placement/validation, Ready, server-owned combat, participant containment/infinite flight, fixed-site Stack and player-centered Spread.
- Ordered phases: sealed opening and clockwise relay; lattice/twin blades; distant arms/floods/crush; HP-zero Final survival. Required first scores gate phase transitions and Victory.
- Instant reusable ally revival, no item/token cost, untimed Down (no active Eliminated), recipient lockout and immediate all-Down Defeat. Ordinary Terraria lethal-hook integration remains separate.
- Client-only original Boss/materials, continuous beam forecasts, phase music, SFX and start/transition/result cinematics. Presentation and balance remain developmental.
- Build-gated one-member start is available for explicitly requested solo debugging; ordinary workflow remains user GUI plus a friend.

The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md), [recovery spec](encounters/first-severance/REVIVE_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md) and [audio cues](AUDIO_CUE_SHEET.md) own behavior. The [version matrix](VERSION_MATRIX.md) owns the runtime baseline.

## Verification state

[Current check record](evidence/2026-09-07-sanctuaries-reward-checks.json): domain contracts, actual Mod codec and successful packaging/source identity. [Pre-change playtests](evidence/2026-09-07-pre-0219-playtests.json) establish that solo0.2.18 reached Final and preserve distinct two/three-player observations. [Consolidation](evidence/2026-09-07-development-consolidation.json) and [0.2.17](evidence/2026-09-07-recovery-tempo-checks.json) evidence remain historical, not fresh0.2.19 playtests.

**Not run / user-owned:** matching0.2.19 load, multiplayer sanctuary timing/positions, normal-spawn suppression/recovery, live beam/crush readability, extended Final audio timing, weapon drop/use/save behavior and end-to-new-Raid cleanup. No GUI, game or server was launched for this change. Prior Stack synchronization was user-confirmed; this does not prove the new overlapping mechanics. Compilation is not audiovisual, performance, latency or multiplayer proof. The manual self-hosted Mod-build CI runner remains unprovisioned.

## Constraints and deferred work

- General Terraria/Calamity lethal-hit interception, robust rejoin/observer identity, outsider admission/ejection and adversarial movement handling are not production-complete.
- Current Raid-owned damage/recovery and participant containment are enabled experiments, not blocked by the deferred production adapters.
- A final production loot table, balance/art/audio acceptance, release packaging and standalone progression replacing the hard Calamity dependency remain deferred. Null Refrain is a development reward, not final progression.
- One-member start must be compiled off before public release. Full release/compatibility gates are separate from normal development checks.

## Next change

User-owned0.2.19 Host & Play: check safe-window grouping/separation, warning-to-hit alignment, natural spawns, weapon drops/combo and ending-to-next-Raid cleanup. Discuss HP using [pre-change observations](evidence/2026-09-07-pre-0219-playtests.json); do not silently raise it. Consolidation is complete; the [implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) separates completed work from production backlog.

## History

[Preserved pre-consolidation record](history/2026-09-07-pre-consolidation.md) contains the preceding build narrative, original slice plan, old Windows handoff and superseded encounter text, with their original evidence links. Historical “inert” and activation-denied instructions are not current gates.
