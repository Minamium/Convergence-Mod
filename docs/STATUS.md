---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-11
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

Development **0.2.47 / protocol 28**. Version comes from [build.txt](../build.txt); wire compatibility from [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs). First Severance is a playable development Raid, not a production-completeness claim.

- Server-wide roster → field deployment → manual Ready → separate combat introduction is implemented. [Arena infrastructure](ARENA_INFRASTRUCTURE.md) owns admission, movement and cancellation.
- Phase I / II / III / Final survival, frozen-roster HP scaling, fixed-position Stack, Spread, Raid-owned Down/instant revival and terminal effects/rewards are implemented. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](encounters/first-severance/REVIVE_SPEC.md) own current behavior.
- Current presentation includes articulated shell/eclosion/remote arms, flowing beams, fragment verdicts, staged weapon rituals and oblique-rift Victory. [Visual spec](encounters/first-severance/VISUAL_SPEC.md) and [five weapons](encounters/first-severance/WEAPONS.md) own the accepted direction.
- Latest change: phase transitions keep the previous BGM through the full transformation, then start P2/P3/Final at section-only high-energy loops; P1/P2 mastering is raised so BGM stays audible against raid SFX. [Audio sheet](AUDIO_CUE_SHEET.md) owns the active cuts/mix; gameplay timing, SFX selection and silent preparation are unchanged.
- One-member admission defaults on only as a build-gated development aid. It adds no companion, invulnerability or solo-specific fight. Normal testing is multiplayer unless the user explicitly chooses solo.

## Verification state

Latest native package remains the 0.2.46 package from source commit **9232caa** (0 errors, 4 existing CS8632 warnings). For **0.2.47**, the one-shot integration verifies all four new OGGs as finite 48 kHz stereo with bounded true peak and the intended rising LUFS-I curve, then runs repository tests, catalog refresh and `git diff --check` before push. The 0.2.47 native tModLoader package and in-game phase handoff/loop/mix audition remain `not_run` / user-owned. [0.2.47 evidence](evidence/2026-09-11-fukou-section-loops-mix.json) owns exact hashes and measurements.

The package also included existing, uncommitted English edits in `Localization/Preparation/en-US.hjson`, `Localization/RitualArmaments/en-US.hjson` and `Localization/en-US.hjson`. They are preserved, not included in this documentation commit. A fresh clone does not contain those local edits; consult the build manifest before claiming byte-identical reproduction.

Recent observed gameplay: the [0.2.43 solo run](evidence/2026-09-10-grounded-posts-audio-tails.json) ended in all-Down Defeat after a failed Stack; earlier successful solo and multiplayer runs and detailed diagnostics remain in the [frozen checkpoint history](history/2026-09-11-status-through-0246.md#verification-state). Those results are version/topology-specific, not certification of the current package.

Concrete unverified surfaces to select **when relevant**, not a mandatory retest queue:

- Latest audio: silent preparation → P1, old BGM held through each transformation, new high-energy section starting with the first attack, section-loop seam, and BGM/SFX balance audition.
- Preparation/UI: a distant three-player roster, last Ready/unready/cancel, and a peer at 107% UI scale seeing aligned black exterior/letterboxes.
- Latest four-column/plinth art: ground contact and suspension continuity in the actual world.
- Production compatibility: ordinary lethal hits, reconnect/observer identity, outsider rules and release matrix remain separate gates.

## Constraints and deferred work

The user accepted the Raid combat body as the current development baseline; do not reopen every attack or invent Phase IV/V merely from old requests. Remaining polish is scoped by the next user request. Normal Terraria/Calamity lethal events are not converted to Raid Down; robust rejoin and adversarial movement/weapon validation are not implemented guarantees. General outsider admission/ejection remains broader than the development containment.

Calamity is still a hard dependency, including progression and Rogue/true-melee integration; targeting its endgame DPS does not eliminate that dependency. See [staged independence](adr/0006-staged-calamity-independence.md).

Development loot is implemented, but balance, public solo/companions and final production acceptance are not settled. Source/asset license selection, exact-package release approval and current Workshop visibility must not be inferred from a build or an earlier “approval pending” report. [Release process](RELEASE_PROCESS.md) owns the gate and explicit development-publication exception process.

## Next change

After obtaining a matching 0.2.47 client build, reload/restart and audition only the affected audio surfaces: confirm each transformation retains the old track until completion, the next phase enters directly on its selected high-energy section, loops never reintroduce the removed opening, and BGM remains continuously perceptible beneath attack SFX at the unchanged user sliders.

Keep current behavior and accepted art unless explicitly revising them. Choose checks from the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md); do not replay historical checklists.

## History

The [checkpoint through 0.2.46](history/2026-09-11-status-through-0246.md) preserves all preceding build/playtest evidence and superseded follow-up lists. [Pre-consolidation history](history/2026-09-07-pre-consolidation.md) preserves the earlier slices. Neither is the current work queue.
