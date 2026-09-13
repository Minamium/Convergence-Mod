---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-14
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

Source version: **0.3.1 / protocol37**, selected for the owner-requested public playtest. [Release notes](releases/0.3.1.md) own its distribution scope, compatibility and exceptions; the [v0.3.1 release](https://github.com/Minamium/Convergence-Mod/releases/tag/v0.3.1) carries the exact built package, checksum and execution record when published. The latest gameplay evidence is from **0.2.78**, not a newly claimed 0.3.1 playtest.

- **Requiem of the Hollow Doll — initial prototype complete**, as designated by the owner on 2026-09-14. Boss: **Lacrimosa — The Bound Heart**. Connected-party preparation, Ready, P1/P2/P3/Final, revival and reward loop are implemented. “Complete prototype” is not final balance or compatibility certification.
- **Ghost Samurai — in development.** Independent summoned boss, normal death, unfinished rewards/balance/lifecycle; not another completed Raid. Its known unload exception can occur even without summoning it.
- Solo admission remains enabled for this public-test channel by the owner's explicit request; multiplayer testing is preferred. No solo redesign, invulnerability or NPC Raid participant is added.

Use the owning specs for details: [combat and public names](encounters/first-severance/ENCOUNTER_SPEC.md), [visuals](encounters/first-severance/VISUAL_SPEC.md), [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md), [weapons](encounters/first-severance/WEAPONS.md), [audio](AUDIO_CUE_SHEET.md), [recovery](encounters/first-severance/REVIVE_SPEC.md), [Ghost Samurai](encounters/ghost-samurai/ENCOUNTER_SPEC.md).

## Verification state

- Latest [0.2.78 owner playtest](evidence/2026-09-14-playtest-0278.json): solo Host & Play, one victory in 240.65s, Stack19/19 and Spread22/22, no Down/revive. Twenty-four weapon cues / 48 samples report actual playback. This is neither multiplayer recovery evidence nor measured listening quality.
- Unchanged gameplay baseline: [0.2.78 build/implementation evidence](evidence/2026-09-14-doll-video-feedback.json), 206 domain cases, 324 codec round-trips / 50 malformed rejections, 37 tooling guards; native build with 0 errors / 4 existing CS8632 warnings. The release changes version, presentation text and documentation, not combat, assets or wire layout.
- **Known failure:** Ghost Samurai's `OnWorldUnload` throws `IndexOutOfRangeException`; latest shutdown also records native tile-stream `ObjectDisposedException` and SubworldLibrary EOF. See [lifecycle handoff](encounters/ghost-samurai/ENCOUNTER_SPEC.md#lifecycle-handoff--2026-09-14). No fix is claimed by relabelling the content.
- **User-owned / not_run for 0.3.1:** reload/load and repeated entry/exit, 2–4-player matching-peer Ready/Stack/revive, latency/rejoin and latest scene/audio/accessibility/performance checks. Build success does not satisfy these checks. Use backed-up test saves.

## Next change

Hand the recorded 0.3.1 package to the owner for the separate tModLoader Publish step and broader playtesting. Do not operate GUI/Steam Publish under GitHub release authorization. For subsequent work, branch from integrated main; [Contributing](../CONTRIBUTING.md#shared-development) owns integration and shared build destinations.

Preserve accepted mechanics and art unless explicitly revised. Log meaningful owner feedback in the optional [ledger](history/PLAYTEST_FEEDBACK.md), update only the affected fact owner, and select checks using the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md). Do not replay historical checklists.

## History

[Verification through 0.2.78](history/2026-09-14-status-through-0278.md) preserves the complete preceding STATUS and evidence links. [Through 0.2.46](history/2026-09-11-status-through-0246.md) and [pre-consolidation](history/2026-09-07-pre-consolidation.md) remain intact. [Player feedback](history/PLAYTEST_FEEDBACK.md) summarizes acceptance/rejections separately from automated observations. None is mandatory startup reading.
