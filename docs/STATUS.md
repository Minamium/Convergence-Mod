---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-17
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

Development source: **0.3.14 / protocol46**. Repairs the violet Ghost Samurai body's cold texture load: the cutout cache waits for real pixels instead of storing a transparent pending-asset placeholder, and body materialization retains a visible minimum opacity before initial synchronization. Boss AI, hit geometry, attack clocks, wire layout, Oboro performance and accepted assets are unchanged from0.3.13. Crimson's GPU hotfix and integrated Doll/Crimson work remain intact. Use matching0.3.14/protocol46 peers when checking visibility. The separately published baseline remains [0.3.1 / protocol37](releases/0.3.1.md); no new release is implied.

- **Requiem of the Hollow Doll — initial prototype complete**, as designated by the owner on 2026-09-14. Boss: **Lacrimosa — The Bound Heart**. Connected-party preparation, Ready, P1/P2/P3/Final, revival and reward loop are implemented. “Complete prototype” is not final balance or compatibility certification.
- **Ghost Samurai — in development.** Oboro is a guaranteed single ground drop; further rewards and balance remain provisional. Cleanup and target ownership have automated coverage; actual multiplayer/re-entry and the new weapon/art need owner playtesting.
- **Crimson Invocation — prototype under playtest.** [Owning spec](encounters/crimson-foundry/ENCOUNTER_SPEC.md) covers1–8-player Ready, shared-pedestal bounded combat, independent apparitions, small performer/companion, musical corridors and native damage. The owner-approved BGM/loop remains included unchanged; intro playback now fades in. Bespoke rewards, extended character motions and calibrated balance remain incomplete.
- Solo admission is normal gameplay in public and development builds, not a compile-time exception. Multiplayer is recommended, not required. Companion party substitution is planned, not implemented; no fake player, invulnerability or solo rebalance is added.

Use the owning specs for details: [combat and public names](encounters/first-severance/ENCOUNTER_SPEC.md), [visuals](encounters/first-severance/VISUAL_SPEC.md), [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md), [weapons](encounters/first-severance/WEAPONS.md), [audio](AUDIO_CUE_SHEET.md), [recovery](encounters/first-severance/REVIVE_SPEC.md), [Ghost Samurai](encounters/ghost-samurai/ENCOUNTER_SPEC.md).

## Verification state

- **Ghost Samurai invisible body:** [visibility evidence](evidence/2026-09-17-ghost-samurai-visibility.json) distinguishes the owner's0.3.13 observation from the pending-asset reproduction and0.3.14 fix. A linked-production CPU cache fixture reproduces the old failure and passes after the fix; all twelve unchanged atlas cells retain visible pixels. The body uses valid frames/scales and nonzero opacity for idle, movement, attacks, phase transitions and first/late snapshots. Actual tModLoader drawing and multiple-client playtesting remain user-owned / not_run; the fixture is not a network session.0.3.14 is a feature build awaiting integration.

- **Oboro and violet Ghost Samurai:** [evidence](evidence/2026-09-16-oboro-violet.json) records the isolated0.3.13/protocol46 native build, exact-package loader/codec/regen checks, domain verification and offline light/dark visual inspection. The administrator approved PR #37; integrated main was built into the normal playtest profile and its installed weapon/summon registration, codec, regen and cleanup checks passed. Server/SP owns hits, marks, buff benefits and detonation; client state and visual events are bounded. Runtime-generated cutout textures are created on draw and disposed through the main-thread queue. Equipped Earth comparison, actual drop/save/reload, combat, latency/rejoin and in-game visual checks remain user-owned / not_run.

- **Ghost Samurai horizontal cleave:** [evidence](evidence/2026-09-15-ghost-samurai-horizontal.json) distinguishes the isolated0.3.7/protocol41 feature checks from subsequent integration with main0.3.9 as0.3.10/protocol44. The original feature passed229 domain cases (45 focused), native compilation, loader/teardown and330/54 codec cases. The administrator approved PR #30 integration and normal-profile installation. Actual arrival/pose/terrain/readability and matching-peer playtesting remain user-owned / not_run.

- **Crimson Invocation:** [evidence and0.3.12 hotfix](evidence/2026-09-16-crimson-invocation.json) distinguish0.3.11's passing type/codec checks from the owner's actual failed content load. The correction passes native compilation and a linked-production FNA worker-load/render/queued-reload lifetime check; actual tModLoader reload remains user-owned / not_run. Four-target progression, corridor fairness, remote companion, mount/landing, field/Ready/re-summon, UI107%/zoom, mix/listening and performance remain unverified. [0.3.9 machine evidence](evidence/2026-09-15-crimson-stage.json) and [initial0.3.8](evidence/2026-09-15-crimson-foundry.json) remain historical, not approval of this redesign.

- **Ghost Samurai new attacks:** [new-state evidence](evidence/2026-09-15-ghost-samurai-combos.json) records the original isolated0.3.5/protocol40 feature build and the subsequent0.3.6 integration with current main. The feature passed225 domain cases (41 Ghost Samurai),330 compiled round-trips /54 malformed rejections, package registration and both encounters' teardown. The administrator approved PR #28 integration and normal-profile installation. Actual SP/MP playtesting remains user-owned / not_run.

- Latest [0.3.5 multiplayer playtest / 0.3.7 changes](evidence/2026-09-15-doll-final-check.json): two players, Victory236.48s, Stack19/19, Spread19/22, two Down/two revives. 229 domain tests,334 exact-package codec round-trips/54 malformed rejections, native Release compilation and scoped installed-API/type/teardown checks pass. Packaged admission remains1–4, solo enabled. New remote visibility, check difficulty, material readability and SFX listening remain user-owned / not_run. [Earlier solo damage evidence](evidence/2026-09-15-doll-damage-tuning.json) remains available.
- Unchanged gameplay baseline: [0.2.78 build/implementation evidence](evidence/2026-09-14-doll-video-feedback.json), 206 domain cases, 324 codec round-trips / 50 malformed rejections, 37 tooling guards; native build with 0 errors / 4 existing CS8632 warnings. The release changes version, presentation text and documentation, not combat, assets or wire layout.
- **Ghost Samurai installation:** [PR #25 integration and normal-profile evidence](evidence/2026-09-14-ghost-samurai-lifecycle.json) records the initial0.3.2 install and a follow-up build from current main0.3.4/protocol39. Installed summon/Global registration, both encounters' teardown and compiled codec checks pass. Reload Mods; no repeat Build + Reload is required. Actual gameplay remains user-owned / not_run.
- **0.3.2 automated verification:** 211 domain cases (32 focused), protocol38 324 codec round-trips / 50 malformed rejections, native build0 errors /4 existing warnings,18 locales and shader exports pass. Packaged Global/summon registration and exact-Fight/repeated teardown with uninitialized Player/ModContent pass. The separate native tile-stream/SubworldLibrary shutdown errors are not claimed fixed. Actual wipe/victory/re-summon, ratios, field placement and timing remain user-owned / not_run.
- **User-owned / not_run for 0.3.1:** reload/load and repeated entry/exit, 2–4-player matching-peer Ready/Stack/revive, latency/rejoin and latest scene/audio/accessibility/performance checks. Build success does not satisfy these checks. Use backed-up test saves.
- **0.3.3:** automated results are recorded in [native-Hurt evidence](evidence/2026-09-14-native-raid-hurt.json). Actual equipped damage, Calamity shields/dodge/Adrenaline, lethal→Down→rescue and last-hit all-Down remain user-owned / not_run. Native Hurt is covered; DoT/direct KillMe/foreign HP writes and reconnect remain limited. No combat text is restored.
- **0.3.4 admission:** [regression/build evidence](evidence/2026-09-14-solo-admission.json) distinguishes the GitHub and Workshop0.3.1 packages. Integrated main was built without a solo opt-in symbol; the successful solo session above closes the start-only owner check, not multiplayer/recovery compatibility.
- **0.3.5:** native Release package passes compilation (0 errors/4 existing warnings), solo1–4 admission, installed HurtModifiers calibration and exact-Fight loader/teardown checks. The domain suite's affected timing expectations are updated; compiled protocol39 passes330 round-trips/54 malformed cases. [Evidence](evidence/2026-09-15-doll-damage-tuning.json) records hashes and remaining owner checks. New diagnostics distinguish immediate native damage from a Chalice buffer; no equipment mechanic is disabled. Post-change gameplay remains user-owned / not_run.

## Next change

The normal profile currently has0.3.13; after approved0.3.14 integration/installation, Reload Mods without another Build + Reload. First summon with a cold texture cache and check the body during idle/movement, every attack, both phase transitions and after re-summon; repeat on a second matching client and a late join. Verify the violet body remains visible while ghost transparency and danger markers remain readable. Oboro drop/three-cut/Zanshin/Wraith Fire, equipped Earth comparison and inherited horizontal-cleave arrival/terrain checks remain separate owner checks, not certified by this visual fix.

For Crimson, use Crimson Grimoire on Foundation Core → Ready → defeat three independent apparitions → confirm the performer becomes damageable without growing → victory/wipe/re-summon. Try Scarlet Covenant with ten minion slots, including remote visibility and flying/landing. Earlier Doll companion/final-check/audio checks remain outstanding. Workshop/GitHub releases remain separate from ordinary main integration. [Contributing](../CONTRIBUTING.md#shared-development) owns integration/build destinations.

Preserve accepted mechanics and art unless explicitly revised. Log meaningful owner feedback in the optional [ledger](history/PLAYTEST_FEEDBACK.md), update only the affected fact owner, and select checks using the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md). Do not replay historical checklists.

## History

[Verification through 0.2.78](history/2026-09-14-status-through-0278.md) preserves the complete preceding STATUS and evidence links. [Through 0.2.46](history/2026-09-11-status-through-0246.md) and [pre-consolidation](history/2026-09-07-pre-consolidation.md) remain intact. [Player feedback](history/PLAYTEST_FEEDBACK.md) summarizes acceptance/rejections separately from automated observations. None is mandatory startup reading.
