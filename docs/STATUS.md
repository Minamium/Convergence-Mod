---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-15
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

Development source: **0.3.5 / protocol40**, Ghost Samurai adds independent triple-vertical and frontal-cleave/ground-shockwave attacks. Ordinary solo admission in every build ([ADR-0025](adr/0025-public-solo-admission.md)) and native receiving-player Hurt are retained with authoritative Doll Down/revival ([ADR-0024](adr/0024-native-raid-hurt-and-downed.md)) and integrated Ghost Samurai0.3.2 fixes. Defensive-equipment behavior changed and matching peers are required. The separately published baseline remains [0.3.1 / protocol37](releases/0.3.1.md), with a confirmed Workshop solo-admission regression; no new release is implied by a local build.

- **Requiem of the Hollow Doll — initial prototype complete**, as designated by the owner on 2026-09-14. Boss: **Lacrimosa — The Bound Heart**. Connected-party preparation, Ready, P1/P2/P3/Final, revival and reward loop are implemented. “Complete prototype” is not final balance or compatibility certification.
- **Ghost Samurai — in development.** Cleanup and target ownership repaired with automated checks; rewards/balance and actual multiplayer/re-entry validation remain incomplete.
- Solo admission is normal gameplay in public and development builds, not a compile-time exception. Multiplayer is recommended, not required. Companion party substitution is planned, not implemented; no fake player, invulnerability or solo rebalance is added.

Use the owning specs for details: [combat and public names](encounters/first-severance/ENCOUNTER_SPEC.md), [visuals](encounters/first-severance/VISUAL_SPEC.md), [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md), [weapons](encounters/first-severance/WEAPONS.md), [audio](AUDIO_CUE_SHEET.md), [recovery](encounters/first-severance/REVIVE_SPEC.md), [Ghost Samurai](encounters/ghost-samurai/ENCOUNTER_SPEC.md).

## Verification state

- **0.3.5 Ghost Samurai:** [new-state evidence](evidence/2026-09-15-ghost-samurai-combos.json): 225 domain cases (41 Ghost Samurai), protocol40 330 compiled round-trips /54 malformed rejections; isolated native build0 errors /4 existing warnings,18 locales and shader exports pass. Exact-package summon/Global registration, native API and both encounters' teardown pass. Normal-profile0.3.4 is retained until integration approval. SP/MP playtesting of the new attacks is not_run.

- Latest [0.2.78 owner playtest](evidence/2026-09-14-playtest-0278.json): solo Host & Play, one victory in 240.65s, Stack19/19 and Spread22/22, no Down/revive. Twenty-four weapon cues / 48 samples report actual playback. This is neither multiplayer recovery evidence nor measured listening quality.
- Unchanged gameplay baseline: [0.2.78 build/implementation evidence](evidence/2026-09-14-doll-video-feedback.json), 206 domain cases, 324 codec round-trips / 50 malformed rejections, 37 tooling guards; native build with 0 errors / 4 existing CS8632 warnings. The release changes version, presentation text and documentation, not combat, assets or wire layout.
- **Ghost Samurai installation:** [PR #25 integration and normal-profile evidence](evidence/2026-09-14-ghost-samurai-lifecycle.json) records the initial0.3.2 install and a follow-up build from current main0.3.4/protocol39. Installed summon/Global registration, both encounters' teardown and compiled codec checks pass. Reload Mods; no repeat Build + Reload is required. Actual gameplay remains user-owned / not_run.
- **0.3.2 automated verification:** 211 domain cases (32 focused), protocol38 324 codec round-trips / 50 malformed rejections, native build0 errors /4 existing warnings,18 locales and shader exports pass. Packaged Global/summon registration and exact-Fight/repeated teardown with uninitialized Player/ModContent pass. The separate native tile-stream/SubworldLibrary shutdown errors are not claimed fixed. Actual wipe/victory/re-summon, ratios, field placement and timing remain user-owned / not_run.
- **User-owned / not_run for 0.3.1:** reload/load and repeated entry/exit, 2–4-player matching-peer Ready/Stack/revive, latency/rejoin and latest scene/audio/accessibility/performance checks. Build success does not satisfy these checks. Use backed-up test saves.
- **0.3.3:** automated results are recorded in [native-Hurt evidence](evidence/2026-09-14-native-raid-hurt.json). Actual equipped damage, Calamity shields/dodge/Adrenaline, lethal→Down→rescue and last-hit all-Down remain user-owned / not_run. Native Hurt is covered; DoT/direct KillMe/foreign HP writes and reconnect remain limited. No combat text is restored.
- **0.3.4 admission:** [regression/build evidence](evidence/2026-09-14-solo-admission.json) distinguishes the GitHub and Workshop0.3.1 packages. Integrated main was built without a solo opt-in symbol, verified and installed into normal Mods with the old package backed up. The installed package permits1–4 players; the owner has not yet confirmed a new successful solo start.

## Next change

Ghost Samurai0.3.5 is built in the separate feature profile; review/integrate it before replacing the shared package. Then Reload Mods and verify the triple dash dodge, rear dodge then jump, retarget/cleanup and matching-peer behavior. Also verify one-player Ready/start and the focused matching-peer recovery checks above. Do not mix protocol40 with earlier peers. Compare source damage against native damage in logs before retuning HP/damage budgets. Workshop and GitHub releases remain separate owner-authorized publication steps. [Contributing](../CONTRIBUTING.md#shared-development) owns integration/build destinations; the [review disposition](research/2026-09-14-implementation-review.md) records deferred latency/rejoin/reward/performance work.

Preserve accepted mechanics and art unless explicitly revised. Log meaningful owner feedback in the optional [ledger](history/PLAYTEST_FEEDBACK.md), update only the affected fact owner, and select checks using the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md). Do not replay historical checklists.

## History

[Verification through 0.2.78](history/2026-09-14-status-through-0278.md) preserves the complete preceding STATUS and evidence links. [Through 0.2.46](history/2026-09-11-status-through-0246.md) and [pre-consolidation](history/2026-09-07-pre-consolidation.md) remain intact. [Player feedback](history/PLAYTEST_FEEDBACK.md) summarizes acceptance/rejections separately from automated observations. None is mandatory startup reading.
