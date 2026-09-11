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

Development **0.2.52 / protocol 28**. Version comes from [build.txt](../build.txt); wire compatibility from [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs). First Severance is a playable development Raid, not a production-completeness claim.

- Server-wide roster → field deployment → manual Ready → separate combat introduction is implemented. [Arena infrastructure](ARENA_INFRASTRUCTURE.md) owns admission, movement and cancellation.
- Phase I / II / III / Final survival, frozen-roster HP scaling, fixed-position Stack, Spread, Raid-owned Down/instant revival and terminal effects/rewards are implemented. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](encounters/first-severance/REVIVE_SPEC.md) own current behavior.
- Latest presentation: [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) adds sixteen authored claw and sixteen torso poses, plus twelve NPC expressions/gestures. The restored remote-arm identity, main-body doll arms/small face, shell and all-Ready capture remain. Final now uses a four-color read-ahead pursuit-beam score; [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#four-color-final-prism-score) owns its shared geometry and timing.
- Approved EigHt section-loop audio files remain unchanged. Phase handoff now occurs during transformation and caps the outgoing track fade before combat; [Audio sheet](AUDIO_CUE_SHEET.md) owns this policy and preparation silence.
- One-member admission defaults on only as a build-gated development aid. It adds no companion, invulnerability or solo-specific fight. Normal testing is multiplayer unless the user explicitly chooses solo.

## Verification state

The **0.2.52** native package is built and installed: **0 errors, 4 existing CS8632 warnings**; source unchanged during packaging. Twelve selected domain tests, atlas/palette/pose previews and static checks pass. [Build evidence](evidence/2026-09-12-four-color-prism-frames.json) records exact identity and remaining in-game checks. No game session/server was launched. [Previous frame/intro evidence](evidence/2026-09-12-remote-hands-intro.json) preserves prior build checks and links to earlier art/audio records.

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

Development loot is implemented, but balance, public solo/companions and final production acceptance are not settled. Source/asset license selection, exact-package release approval and current Workshop visibility must not be inferred from a build or an earlier “approval pending” report. [Release process](RELEASE_PROCESS.md) owns the gate and explicit development-publication exception process.

## Next change

Reload/restart the installed 0.2.52 package; no unchanged rebuild needed. Inspect the red/blue/green/gold forecasts and same-order prism strikes, safe-band movement and peer hit alignment; then check music handoff and the new NPC/hand/torso frames. Ready still leaves NPC intact until accepted Raid intro. Native rendering, listening and performance checks are user-owned; authored frames/CPU previews are not proof of game FPS.

Keep current behavior and accepted art unless explicitly revising them. Choose checks from the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md); do not replay historical checklists.

## History

The [checkpoint through 0.2.46](history/2026-09-11-status-through-0246.md) preserves all preceding build/playtest evidence and superseded follow-up lists. [Pre-consolidation history](history/2026-09-07-pre-consolidation.md) preserves the earlier slices. Neither is the current work queue.
