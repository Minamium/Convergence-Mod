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

Development **0.2.18**, protocol **17**. This is an implementation-structure build with unchanged combat tuning. Current version declarations live in [build.txt](../build.txt) and [the packet header](../Common/Networking/Protocol/EncounterPacketHeader.cs); this page records implementation/evidence, not tuning.

- First Severance is playable: Core placement/validation, Ready, server-owned combat, participant containment/infinite flight, fixed-site Stack and player-centered Spread.
- Ordered phases: sealed opening and clockwise relay; lattice/twin blades; distant arms/floods/crush; HP-zero Final survival. Required first scores gate phase transitions and Victory.
- Instant reusable ally revival, no item/token cost, untimed Down (no active Eliminated), recipient lockout and immediate all-Down Defeat. Ordinary Terraria lethal-hook integration remains separate.
- Client-only original Boss/materials, continuous beam forecasts, phase music, SFX and start/transition/result cinematics. Presentation and balance remain developmental.
- Build-gated one-member start is available for explicitly requested solo debugging; ordinary workflow remains user GUI plus a friend.

The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md), [recovery spec](encounters/first-severance/REVIVE_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md) and [audio cues](AUDIO_CUE_SHEET.md) own behavior. The [version matrix](VERSION_MATRIX.md) owns the runtime baseline.

## Verification state

[0.2.17 check record](evidence/2026-09-07-recovery-tempo-checks.json): 93 domain cases, 308 compiled codec round trips / 48 malformed cases, static checks and actual Mod package build passed. These unchanged-input results were reused when preserving the source and unpublished Git history.

**Not run / user-owned:** matching 0.2.18 load/Ready/common snapshot repair and 0.2.17 gameplay changes and Host & Play observation of untimed recovery, simultaneous Prism, two blade turns, changing flood safe strips, accelerating Final and audio balance. Prior Stack synchronization was confirmed by the user; do not reopen it without new evidence. Compilation is not live audiovisual, latency or multiplayer proof.

## Constraints and deferred work

- General Terraria/Calamity lethal-hit interception, robust rejoin/observer identity, outsider admission/ejection and adversarial movement handling are not production-complete.
- Current Raid-owned damage/recovery and participant containment are enabled experiments, not blocked by the deferred production adapters.
- Rewards, final balance/art/audio, release packaging and standalone progression replacing the hard Calamity dependency remain deferred.
- One-member start must be compiled off before public release. Full release/compatibility gates are separate from normal development checks.

## Next change

Preservation/GitHub, current docs/skills and source-identified local builds are complete. Definition-routed packet dispatch is implemented with focused regression coverage; combat responsibility extraction and repeatable test cleanup follow. The [implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) owns that queue. No new gameplay, GUI launch or broad runtime matrix is requested.

## History

[Preserved pre-consolidation record](history/2026-09-07-pre-consolidation.md) contains the preceding build narrative, original slice plan, old Windows handoff and superseded encounter text, with their original evidence links. Historical “inert” and activation-denied instructions are not current gates.
