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

Development **0.2.23**, protocol **20**. Every Spread now shares the white muzzle cross, high firing ping and ray strength; success bursts into a larger split plume before the player instead of impacting. Stack's accepted shell fragments and friction arcs are enlarged, with original electrical contact audio. The accepted beam materials, BGM mix and gameplay remain unchanged. Version declarations live in [build.txt](../build.txt) and [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs); exact mechanics/tuning belong to the linked specifications/code.

- First Severance is playable: Core placement/validation, Ready, server-owned combat, participant containment/infinite flight, fixed-site Stack and player-centered Spread.
- Ordered phases: sealed opening and clockwise relay; lattice/twin blades; distant arms/floods/crush; HP-zero Final survival. Required first scores gate phase transitions and Victory.
- Instant reusable ally revival, no item/token cost, untimed Down (no active Eliminated), recipient lockout and immediate all-Down Defeat. Ordinary Terraria lethal-hook integration remains separate.
- Client-only original Boss/materials, continuous beam forecasts, phase music, SFX and start/transition/result cinematics. Presentation and balance remain developmental.
- Build-gated one-member start is available for explicitly requested solo debugging; ordinary workflow remains user GUI plus a friend.

The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md), [recovery spec](encounters/first-severance/REVIVE_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md) and [audio cues](AUDIO_CUE_SHEET.md) own behavior. The [version matrix](VERSION_MATRIX.md) owns the runtime baseline.

## Verification state

[Current check record](evidence/2026-09-07-shared-spread-shell-checks.json) owns scoped automated results and pending visual/audio acceptance. Logs and artifact hash confirmed both client/server loaded0.2.22; its solo Spread succeeded, so the then-failure-only flash did not appear. This restriction is now removed. [0.2.22 checks](evidence/2026-09-07-mechanic-intensity-checks.json), [0.2.21 checks](evidence/2026-09-07-mechanic-presentation-checks.json) and [consolidation](evidence/2026-09-07-development-consolidation.json) retain preceding evidence/history.

**Not run / user-owned:** matching0.2.23 load, enlarged shell/lightning readability, electrical audio mix and reduced-effects behavior; successful Spread cross/ping and split plume. Three-player partial failures remain unobserved; solo results do not substitute. No GUI, game or server was launched for this change. Compilation is not audiovisual, performance, latency or multiplayer proof. Previous checks not explicitly observed remain pending in their evidence record; no blanket retest is required. The manual self-hosted Mod-build CI runner remains unprovisioned.

## Constraints and deferred work

- General Terraria/Calamity lethal-hit interception, robust rejoin/observer identity, outsider admission/ejection and adversarial movement handling are not production-complete.
- Current Raid-owned damage/recovery and participant containment are enabled experiments, not blocked by the deferred production adapters.
- A final production loot table, balance/art/audio acceptance, release packaging and standalone progression replacing the hard Calamity dependency remain deferred. Null Refrain is a development reward, not final progression.
- One-member start must be compiled off before public release. Full release/compatibility gates are separate from normal development checks.

## Next change

User-owned0.2.23 Host & Play: solo can now show the common Spread firing flash/ping plus the successful plume, and enlarged electrified Stack shells. Failure/mixed endpoints still need multiplayer. Discuss HP separately from distinct build/roster playtests; no implicit retuning. The [implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) owns production backlog.

## History

[Preserved pre-consolidation record](history/2026-09-07-pre-consolidation.md) contains the preceding build narrative, original slice plan, old Windows handoff and superseded encounter text, with their original evidence links. Historical “inert” and activation-denied instructions are not current gates.
