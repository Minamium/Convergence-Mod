---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-12
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

Development **0.2.53 / protocol 28**. Version comes from [build.txt](../build.txt); wire compatibility from [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs). Public names are now **不幸な人形劇 / The Unfortunate Doll Play** and **ラクリモーサ — 縛られた心 / Lacrimosa — The Bound Heart**; internal FirstSeverance/NullCantor identifiers remain stable. This is a playable development Raid, not a production-completeness claim.

- Server-wide roster → field deployment → manual Ready → separate combat introduction is implemented. [Arena infrastructure](ARENA_INFRASTRUCTURE.md) owns admission, movement and cancellation.
- Phase I / II / III / Final survival, frozen-roster HP scaling, fixed-position Stack, Spread, Raid-owned Down/instant revival and terminal effects/rewards are implemented. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](encounters/first-severance/REVIVE_SPEC.md) own current behavior.
- Latest presentation: Boss-centered Stack/Spread reticles are removed; player/gather circles, casting posture and verdict flashes remain. [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) retains the approved shell/body/remote-hand frames and all-Ready capture. Final's four-color read-ahead pursuit-beam score is unchanged.
- [Weapons](encounters/first-severance/WEAPONS.md#doll-companion--the-unbroken-promise) adds the 10-slot Doll companion, obtainable by exchanging Choir of the Unmade at a Work Bench. Its 36 cels combine NPC idle with new ground/air/casting motion; it is not a Raid participant. Weapon audio is separated into 27 original dedicated masters; [Audio](AUDIO_CUE_SHEET.md#weapon-only-foley) owns selection/cancellation and audition policy.
- Approved EigHt section-loop audio files remain unchanged. Phase handoff now occurs during transformation and caps the outgoing track fade before combat; [Audio sheet](AUDIO_CUE_SHEET.md) owns this policy and preparation silence.
- One-member admission defaults on only as a build-gated development aid. It adds no companion, invulnerability or solo-specific fight. Normal testing is multiplayer unless the user explicitly chooses solo.

## Verification state

The **0.2.53** native package is built and installed with **0 errors / 4 existing CS8632 warnings**. Two focused companion contract tests, 36-cel/alpha/pixel previews, 27 PCM checks and selected static checks pass; [build evidence](evidence/2026-09-12-doll-companion-foley.json) records identity and scope. Game load, physical listening, movement on real terrain and peer replication remain **not_run**, user-owned. [Previous build evidence](evidence/2026-09-12-four-color-prism-frames.json) preserves the prior Final beam/music/frame checks; no unrelated domain suite is repeated.

The working tree includes pre-existing English edits in `Localization/DollTheater/en-US.hjson`, `Localization/Preparation/en-US.hjson`, `Localization/RitualArmaments/en-US.hjson` and `Localization/en-US.hjson`. They are preserved and kept outside this change's commits. A fresh clone does not contain those local edits; consult the build manifest before claiming byte-identical reproduction.

Recent observed gameplay: [0.2.51 archived logs](evidence/2026-09-12-playtest-0251.json) contain one solo victory and six three-player all-Down defeats. Ten revives succeeded. The final multiplayer attempt reached Final and ended on a one-of-three Stack with two Downed members; its Final slicer had two nonlethal hits. Logs do not certify the new package or every peer's visuals/audio. Earlier runs remain in the [checkpoint history](history/2026-09-11-status-through-0246.md#verification-state).

Concrete unverified surfaces to select **when relevant**, not a mandatory retest queue:

- Latest audio: silent preparation → P1, new high-energy section during transformation with no old-track bleed into combat, section-loop seam, and BGM/SFX balance audition.
- Preparation/UI: a distant three-player roster, last Ready/unready/cancel, and a peer at 107% UI scale seeing aligned black exterior/letterboxes.
- Doll Theater: the short [acceptance list](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md#検証と残課題) covers NPC/Core lifecycle, capture/reveal, suspension and actual combat-scale readability.
- Production compatibility: ordinary lethal hits, reconnect/observer identity, outsider rules and release matrix remain separate gates.

## Constraints and deferred work

The user accepted the Raid combat body as the current development baseline; do not reopen every attack or invent Phase IV/V merely from old requests. Remaining polish is scoped by the next user request. Normal Terraria/Calamity lethal events are not converted to Raid Down; robust rejoin and adversarial movement/weapon validation are not implemented guarantees. General outsider admission/ejection remains broader than the development containment.

Calamity is still a hard dependency, including progression and Rogue/true-melee integration; targeting its endgame DPS does not eliminate that dependency. See [staged independence](adr/0006-staged-calamity-independence.md).

Development loot includes a companion summon weapon, but balance, public solo/NPC-party substitutes and final production acceptance are not settled. Source/asset license selection, exact-package release approval and current Workshop visibility must not be inferred from a build or an earlier “approval pending” report. [Release process](RELEASE_PROCESS.md) owns the gate and explicit development-publication exception process.

## Next change

Reload/restart the installed 0.2.53 package; no unchanged rebuild needed. Check the player-only Stack/Spread rings and renamed intro/bar; summon The Unbroken Promise with 10 slots and try ground/flight/retarget/Down/dismissal with a peer. Audition weapon families at unchanged sliders and confirm charge/sustain cancellation. The stage NPC remains intact until all-Ready intro; minions do not replace missing Raid participants. Native rendering, listening and performance checks are user-owned; authored frames/CPU previews are not proof of game FPS.

Keep current behavior and accepted art unless explicitly revising them. Choose checks from the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md); do not replay historical checklists.

## History

The [checkpoint through 0.2.46](history/2026-09-11-status-through-0246.md) preserves all preceding build/playtest evidence and superseded follow-up lists. [Pre-consolidation history](history/2026-09-07-pre-consolidation.md) preserves the earlier slices. Neither is the current work queue.
