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

Development source: **0.3.2 / protocol38**, Ghost Samurai cleanup, target ownership, late wave/dash tracking and foot-anchored fields. [Evidence](evidence/2026-09-14-ghost-samurai-lifecycle.json) identifies the isolated build; normal-profile installation is not performed. The separately published/test-channel baseline remains [0.3.1 / protocol37](releases/0.3.1.md). Latest actual gameplay evidence remains0.2.78.

- **Requiem of the Hollow Doll — initial prototype complete**, as designated by the owner on 2026-09-14. Boss: **Lacrimosa — The Bound Heart**. Connected-party preparation, Ready, P1/P2/P3/Final, revival and reward loop are implemented. “Complete prototype” is not final balance or compatibility certification.
- **Ghost Samurai — in development.** Cleanup and target ownership repaired with automated checks; rewards/balance and actual multiplayer/re-entry validation remain incomplete.
- Solo admission remains enabled for this public-test channel by the owner's explicit request; multiplayer testing is preferred. No solo redesign, invulnerability or NPC Raid participant is added.

Use the owning specs for details: [combat and public names](encounters/first-severance/ENCOUNTER_SPEC.md), [visuals](encounters/first-severance/VISUAL_SPEC.md), [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md), [weapons](encounters/first-severance/WEAPONS.md), [audio](AUDIO_CUE_SHEET.md), [recovery](encounters/first-severance/REVIVE_SPEC.md), [Ghost Samurai](encounters/ghost-samurai/ENCOUNTER_SPEC.md).

## Verification state

- Latest [0.2.78 owner playtest](evidence/2026-09-14-playtest-0278.json): solo Host & Play, one victory in 240.65s, Stack19/19 and Spread22/22, no Down/revive. Twenty-four weapon cues / 48 samples report actual playback. This is neither multiplayer recovery evidence nor measured listening quality.
- Unchanged gameplay baseline: [0.2.78 build/implementation evidence](evidence/2026-09-14-doll-video-feedback.json), 206 domain cases, 324 codec round-trips / 50 malformed rejections, 37 tooling guards; native build with 0 errors / 4 existing CS8632 warnings. The release changes version, presentation text and documentation, not combat, assets or wire layout.
- **0.3.2 automated verification:** 211 domain cases (32 focused), protocol38 324 codec round-trips / 50 malformed rejections, native build0 errors /4 existing warnings,18 locales and shader exports pass. Packaged Global/summon registration and exact-Fight/repeated teardown with uninitialized Player/ModContent pass. The separate native tile-stream/SubworldLibrary shutdown errors are not claimed fixed. Actual wipe/victory/re-summon, ratios, field placement and timing remain user-owned / not_run.
- **User-owned / not_run for 0.3.1:** reload/load and repeated entry/exit, 2–4-player matching-peer Ready/Stack/revive, latency/rejoin and latest scene/audio/accessibility/performance checks. Build success does not satisfy these checks. Use backed-up test saves.

## Next change

For Ghost Samurai0.3.2, review/integrate the feature, build from canonical main into the normal profile, then Reload Mods. The separate0.3.1 public-test distribution is not updated by this isolated build. [Contributing](../CONTRIBUTING.md#shared-development) owns integration and build destinations.

Preserve accepted mechanics and art unless explicitly revised. Log meaningful owner feedback in the optional [ledger](history/PLAYTEST_FEEDBACK.md), update only the affected fact owner, and select checks using the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md). Do not replay historical checklists.

## History

[Verification through 0.2.78](history/2026-09-14-status-through-0278.md) preserves the complete preceding STATUS and evidence links. [Through 0.2.46](history/2026-09-11-status-through-0246.md) and [pre-consolidation](history/2026-09-07-pre-consolidation.md) remain intact. [Player feedback](history/PLAYTEST_FEEDBACK.md) summarizes acceptance/rejections separately from automated observations. None is mandatory startup reading.
