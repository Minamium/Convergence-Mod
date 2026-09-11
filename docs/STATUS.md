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

Development **0.2.51 / protocol 28**. Version comes from [build.txt](../build.txt); wire compatibility from [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs). First Severance is a playable development Raid, not a production-completeness claim.

- Server-wide roster → field deployment → manual Ready → separate combat introduction is implemented. [Arena infrastructure](ARENA_INFRASTRUCTURE.md) owns admission, movement and cancellation.
- Phase I / II / III / Final survival, frozen-roster HP scaling, fixed-position Stack, Spread, Raid-owned Down/instant revival and terminal effects/rewards are implemented. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](encounters/first-severance/REVIVE_SPEC.md) own current behavior.
- Latest presentation: [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) restores the old Phase-III remote arms, adding eight authored claw poses and eight torso-restraint poses alongside continuous rig motion. Main-body porcelain arms/small face and the restored shell remain. NPC stays intact during preparation/Ready; capture now happens in the extended all-Ready combat intro with camera/sound/light beats. Attack wrists, collision, battle cadence and music are unchanged.
- Audio files remain the prior EigHt section-loop/mix revision. [Audio sheet](AUDIO_CUE_SHEET.md) owns those files, phase handoff and preparation silence. Only the new intro adds scheduling of existing cues; battle-action timing and music/SFX assets are unchanged.
- One-member admission defaults on only as a build-gated development aid. It adds no companion, invulnerability or solo-specific fight. Normal testing is multiplayer unless the user explicitly chooses solo.

## Verification state

The **0.2.51** native package is built and installed: **0 errors, 4 existing CS8632 warnings**; source unchanged during compilation/packaging. Eight selected domain tests (four doll presentation, two plan validation, combo cadence, full-loop continuity), authored-frame/shared-pose previews and static checks pass. [Frame/intro evidence](evidence/2026-09-12-remote-hands-intro.json) records package identity. In-game frame animation, capture/camera, multiplayer/zoom and frame time remain `not_run` / user-owned. No game session/playable server was launched. [0.2.50 evidence](evidence/2026-09-12-shell-motion.json) retains shell/mesh checks; [0.2.49 evidence](evidence/2026-09-11-doll-capture-refinement.json) retains capture/refinement; [0.2.48 evidence](evidence/2026-09-11-doll-theater.json) retains initial NPC/art; [0.2.47 evidence](evidence/2026-09-11-fukou-section-loops-mix.json) retains audio measurements.

The working tree includes pre-existing English edits in `Localization/DollTheater/en-US.hjson`, `Localization/Preparation/en-US.hjson`, `Localization/RitualArmaments/en-US.hjson` and `Localization/en-US.hjson`. They are preserved and kept outside this change's commits. A fresh clone does not contain those local edits; consult the build manifest before claiming byte-identical reproduction.

Recent observed gameplay: the [0.2.43 solo run](evidence/2026-09-10-grounded-posts-audio-tails.json) ended in all-Down Defeat after a failed Stack; earlier successful solo and multiplayer runs and detailed diagnostics remain in the [frozen checkpoint history](history/2026-09-11-status-through-0246.md#verification-state). Those results are version/topology-specific, not certification of the current package.

Concrete unverified surfaces to select **when relevant**, not a mandatory retest queue:

- Latest audio: silent preparation → P1, old BGM held through each transformation, new high-energy section starting with the first attack, section-loop seam, and BGM/SFX balance audition.
- Preparation/UI: a distant three-player roster, last Ready/unready/cancel, and a peer at 107% UI scale seeing aligned black exterior/letterboxes.
- Doll Theater: the short [acceptance list](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md#検証と残課題) covers NPC/Core lifecycle, capture/reveal, suspension and actual combat-scale readability.
- Production compatibility: ordinary lethal hits, reconnect/observer identity, outsider rules and release matrix remain separate gates.

## Constraints and deferred work

The user accepted the Raid combat body as the current development baseline; do not reopen every attack or invent Phase IV/V merely from old requests. Remaining polish is scoped by the next user request. Normal Terraria/Calamity lethal events are not converted to Raid Down; robust rejoin and adversarial movement/weapon validation are not implemented guarantees. General outsider admission/ejection remains broader than the development containment.

Calamity is still a hard dependency, including progression and Rogue/true-melee integration; targeting its endgame DPS does not eliminate that dependency. See [staged independence](adr/0006-staged-calamity-independence.md).

Development loot is implemented, but balance, public solo/companions and final production acceptance are not settled. Source/asset license selection, exact-package release approval and current Workshop visibility must not be inferred from a build or an earlier “approval pending” report. [Release process](RELEASE_PROCESS.md) owns the gate and explicit development-publication exception process.

## Next change

The new package is installed; restart/reload tModLoader (no unchanged rebuild needed). Inspect NPC remaining intact through Ready, then capture only after all-Ready with the expanded intro, unobstructed camera/title and no duplicate NPC. Check the old remote arms with actual claw-frame changes, torso material animation, unchanged attack wrist alignment and ReducedEffects. Keep the accepted music and attack balance. Eight drawings per part are not a full-body 60-frame redraw; offline 60Hz samples are not game FPS evidence.

Keep current behavior and accepted art unless explicitly revising them. Choose checks from the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md); do not replay historical checklists.

## History

The [checkpoint through 0.2.46](history/2026-09-11-status-through-0246.md) preserves all preceding build/playtest evidence and superseded follow-up lists. [Pre-consolidation history](history/2026-09-07-pre-consolidation.md) preserves the earlier slices. Neither is the current work queue.
