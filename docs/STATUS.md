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

Development **0.2.17 / protocol16** removes active Eliminated/Down expiry, targets all standing players simultaneously in the opening Prism, replaces Phase-III horizontal lanes with smoothly deploying/accelerating flood bands and moving safe strips, makes twin blades rotate twice faster, removes heavy beam/sword border rails, and makes Final patterns shift and progressively accelerate. Existing Stack synchronization/shares, Spread separation/damage, frozen HP, recipient60-second lockout, reusable instant item, all-Down Defeat and solo admission are preserved. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#new-attack-modules), [recovery spec](encounters/first-severance/REVIVE_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md#continuous-borderless-danger-materials--0217) and [audio cues](AUDIO_CUE_SHEET.md#rebalanced-mix-and-continuous-flood-articulation--0217) own design/tuning.

The Raid development Skill kept server/SP outcomes and read-only fractional drawing on shared geometry, bounded Prism snapshots and exact-Fight cleanup. [ADR-0021](adr/0021-untimed-recovery-and-simultaneous-prism.md) supersedes only the old timed-Down policy and single-focus Prism bound. Generic legacy recovery enums/tests remain, but active First Severance has no Eliminated transition or label. No new actors, graphics/audio on Dedicated Server, global audio settings, external sources, saves, Mod-list or public debug changes. Every peer needs the matching protocol16 package.

Focused checks passed: **93 deterministic domain cases;308 compiled preparation/combat round trips and48 malformed/truncated cases; actual ModSources build/package with zero warnings/errors,31,939,686bytes**. Checks cover indefinite Down through lockout/exact rescue, all-Down failure,1–4 simultaneous Prism rays, shifting safe pockets and shared flood width, two-turn identity, mandatory phase scores, travel estimates and lack of permanent stationary Final lanes. Two old test expectations (expired-Down event and comb edge count) were updated before the passing run. Four revised music assets and three action effects decode as finite48kHz samples; four-times oversampled peak stays below0.902. BGM gain is about+1.94dB; feature SFX playback is0.80×. External PCM24 auditions are available. [The check record](evidence/2026-09-07-recovery-tempo-checks.json) owns static/deployment results.

**0.2.17 game load, live audio/visual readability, dodgeability and multiplayer contact/cleanup remain not_run / user-owned.** No game, server or GUI was launched. Automated checks do not establish human audiovisual acceptance. Current packages replace only Convergence, with previous packages retained externally.

## Preceding build 0.2.16

Development **0.2.16 / protocol v15** makes warning audio conspicuous, replaces the single slow blade with two rapid-unsheathed accelerating blades, unifies the full-volume anticipation of broad Phase-I/III beams, adds a lethal central hand crush to the Phase-III score, and increases Final bead/comb density and effects. The user confirms the preceding Stack synchronization problem is resolved; its movement heartbeat, authority coordinates, valid radius, HP, Spread and revival rules are preserved. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#new-attack-modules), [visual spec](encounters/first-severance/VISUAL_SPEC.md#rigid-blades-legible-volumes-and-terminal-density--0216) and [audio cues](AUDIO_CUE_SHEET.md#forewarnings-and-bladecrush-articulation--0216) own tuning and art intent.

The Raid development Skill kept outcomes and lethal-to-Down in the existing exact-Fight runtime, with shared deterministic geometry and no new world entities. Imagegen Skill supplied an original2172×724 RGBA blade, preserved without raster edits and consumed client-only as a rigid, field-clipped material. New/revised SFX are independently synthesized; music, dependencies, Mod lists, saves and public controls are unchanged. Protocol15 appends RemoteCrush without renumbering earlier substates/packets or changing the bounded DTO layout; all peers must update together.

Focused checks passed: **90 deterministic domain cases; 308 compiled preparation/combat round trips and46 malformed/truncated cases; actual ModSources build/package with zero warnings/errors,32,221,196bytes**. Coverage includes the full1–4-player score, crush warning/contact/recovery and safe pockets, accelerated blade boundaries, denser but open Final lanes, and retained Stack/recovery/terminal invariants. An initial test argument order and one client compile identifier were corrected before passing. One original texture was visually/alpha inspected;16 new/revised mono effects decode as finite samples with peaks below0.911. External PCM24 auditions are available. [The check record](evidence/2026-09-06-attack-intensity-checks.json) owns final static/deployment results.

**0.2.16 game load, live audio/readability/dodgeability, multiplayer contact alignment and cancel/end cleanup remain not_run / user-owned.** Neither GUI nor game/server processes were launched. A package and waveform/geometry checks are not a claim of finished WotG-level presentation. The latest user observation accepts Stack sync, not the new audiovisual pass or solo actual activation.

## Preceding build 0.2.15

Development **0.2.15 / protocol v14** fixes the Ready-to-combat scan's forgotten solo opt-in: both actual CoreResolver admission paths now use one compiled, non-optional start-validation policy. It adds an owner-only six-tick vanilla movement heartbeat after Player updates, removes duplicate server-side fallback lift/predictive braking, and retains authoritative containment and Stack decisions. Bounded client Stack coordinate/velocity samples and authority attendance/correction logs support cross-view diagnosis. The observed activation receive-underflow is fixed by parsing each fixed activation/Ready/cancel payload before header/rate rejection. Stack radius112, shares, Spread640 separation, HP, damage, phase score, recovery, dependencies, saves and Mod list are unchanged.

The Raid development Skill kept these changes feature-local with exact-Fight capability cleanup. The source-research Skill checked pinned hook execution and movement-message contracts; [the source record](research/RAID_MOVEMENT_SYNC_APIS.md) distinguishes confirmed defects from the remaining movement-drift inference. **The reported peer displacement is not conclusively explained by the old logs:** they lacked client coordinates and the friend's log is unavailable. This build corrects the identified asymmetric movement path and adds evidence, not a claim of proven live synchronization.

Focused checks passed: **90 deterministic domain cases with solo enabled and disabled**, **28 compiled fixed-request checks** (valid/invalid/truncated/shared-stream sentinel cases), and actual ModSources compile/package with zero warnings/errors,31,030,224bytes. The installed assembly's solo flag is enabled. Protocol/layout and combat DTO are unchanged; the preceding v14 full combat-codec results remain applicable. [The check record](evidence/2026-09-06-solo-stack-sync-checks.json) owns final static/deployment evidence. **0.2.15 game load, solo actual start, two-client Stack alignment, field/flight/recovery cleanup and dedicated-server latency checks remain not_run / user-owned.** No game, server or GUI was launched.

## Inspected 0.2.14 Host & Play session

Two two-player fights ended AllParticipantsDowned after43.95s/43.45s. Initial Stack was1/2 in both, with participant0 at40.3/22.8px and participant1 at669.4/710.3px from the112px marker. The first relay Stack was0/2 in both and caused the final Down events. All four Stack resolutions failed; both Spread resolutions passed. Six Down events and two successful instant revives were recorded; each revive set the recipient deadline exactly3,600ticks later. Host-client normal Defeat death was logged for both fights. No battle exception occurred during either fight.

Each fight cleared both300,000-HP Pylons in7.45s/6.95s (party effective DPS80,537/86,331). The18-second Core exposures dealt692,773/798,232 (38,487/44,346DPS), leaving86.14%/84.04% of5,000,000HP. Neither reached PhaseII. Do not retune HP from these movement-disputed/down-heavy attempts.

Thirteen one-member starts reached the second gate and were rejected with `first_severance.roster_too_small`: the initial scan used the debug flag but combat revalidation did not. Subsequent repeated activations also produced seven rate-limit warnings and one logged tML `Read underflow31of43bytes` warning from the unread12-byte request payload. The latter occurred after both fights and is separate from the Stack complaint. Raw logs/names/world paths remain outside the repository; [sanitized evidence](evidence/2026-09-06-solo-stack-sync-checks.json) retains the measurements.

## Preceding build 0.2.14

Development **0.2.14 / protocol v14** introduced a default-enabled build-time solo-debug admission flag and participant-only Victory/Defeat HUD cinematics. Its intended solo start was incomplete because of the second-scan bug documented above. It reuses the two-player HP/Pylon workload and existing phase score; one Down takes ordinary immediate AllParticipantsDowned/Defeat. There is no NPC, invulnerability, fake second participant, public toggle, saved setting or GUI automation. [ADR-0020](adr/0020-development-solo-admission-and-terminal-hud.md), the [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#one-member-development-start-and-results--0214), [visual spec](encounters/first-severance/VISUAL_SPEC.md#success-and-failure-designations--0214) and [one-client runbook](runbooks/SINGLE_OPERATOR_TESTING.md#0-最短の一窓デバッグ) own that contract and procedure.

Focused results: **88 domain cases passed with the build flag enabled and disabled; 304 compiled codec round trips and46 malformed/truncated cases passed**, now including one-member preparation/Ready and all scores. Reflection confirmed the installed Mod assembly has solo enabled and the separate disabled harness has it off. Actual ModSources build/package passed with zero warnings/errors,31,028,948 bytes. A new test initially referenced the wrong runtime-update property, corrected before these passing runs. No distributable assets or dependencies changed. [The check record](evidence/2026-09-06-solo-debug-results-checks.json) records final static/deployment results.

Those automated checks missed the live second-scan call site. Runtime was not_run at the original handoff; subsequent game load, failed solo activation and two-player outcomes are recorded above. Terminal camera/HUD appearance remains unverified. Public-release packaging must disable the flag and verify one-player rejection; see [Release Process](RELEASE_PROCESS.md).

## Preceding build 0.2.13

Development **0.2.13 / protocol v13** adds wider Spread, clockwise fixed-site Stack relays, first-cycle HP gates, the Phase-II rotating blade, remote-hand Phase III and an explicit HP-zero Final survival score. The shell is smaller and uses new original high-detail art; new phase effects, Phase-III/accelerating-Final music and a point-collapse Victory ending accompany it. The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#ordered-phase-scores-and-terminal-survival--0213), [visual spec](encounters/first-severance/VISUAL_SPEC.md), [audio cues](AUDIO_CUE_SHEET.md) and [ADR-0019](adr/0019-phase-scores-and-terminal-survival.md) own behavior and tuning. Frozen roster HP, defense, recovery, field dimensions, dependencies and Mod selections are unchanged.

The Raid development Skill kept all phase/HP/assignment/damage/cleanup decisions in the exact-Fight authority and drove focused movement/sequence/codec checks. Imagegen Skill supplied an original1254x1254 RGBA shell, copied unedited with alpha preserved and consumed through client-only masks. No new game actors, client health/outcome requests, saved fields, public debug grant or persistent input/UI flags were introduced. All peers need the matching protocol13 package.

Focused results: **84 deterministic domain cases passed; 222 compiled codec round trips and40 malformed/truncated cases passed**. Actual ModSources compilation/package passed with **zero warnings/errors**,31,026,406 bytes. One initial compile found a UI local-name collision, corrected before the successful build; one old enum-inventory assertion was updated for the new actions before the passing domain run. Two new music files and12 new/replaced effects decode as finite, unclipped samples with external PCM24 auditions. The encoder's initial large-buffer failure was corrected using bounded Vorbis writes. The final static/deployment state is recorded in [the check record](evidence/2026-09-06-phase-scores-checks.json).

**0.2.13 game load, all-phase multiplayer play, actual movement/latency margins, remote-hand/depth quality, shell/camera/readability, live mix, cancel/failure cleanup and remote NPC HP-zero behavior are not_run / user-owned.** No game/server/GUI was launched. A compile, synthetic movement estimate or waveform check is not audiovisual or multiplayer acceptance.

## Confirmed preceding 0.2.12 two-player pull

The latest inspected preceding pull won in88.92s, with Core5,000,000 and Pylons300,000 each. Core effective DPS was110,660 over45.18 damageable seconds; Phase-I/II rates116,732/105,189. Pylon checks cleared in4.67s and6.07s. Both Stack resolutions passed2/2 inside112px with no Stack damage; no Spread overlap damage was logged. The half-HP transition lasted6s, and twelve grids fired, with Core salvos from the third and2→1 beams after one member became Downed. Two Down events and one successful instant revive occurred; the second Down was inside the recorded recipient60-second lockout. No battle exception was observed. These are authority outcomes, not proof that both screens matched or that the user approved the prior visuals. Raw logs/names remain external.

## Preceding build 0.2.12

Development **0.2.12 / protocol v12** adds forgiving frozen-roster HP, a single valid Stack circle with exterior arrows, connected limb-first eclosion, Core-origin lasers from Phase-II grid three, stronger original pressure/plasma audio and a short client-only Victory ending. The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#forgiving-roster-hp-and-core-salvos--0212), [visual spec](encounters/first-severance/VISUAL_SPEC.md), [audio cues](AUDIO_CUE_SHEET.md) and [ADR-0018](adr/0018-roster-health-and-core-salvos.md) own exact details. Music, defense, recovery, field dimensions and Mod selections are unchanged.

The Raid development Skill kept targeting, HP thresholds and shared damage ledgers on authority, and eclosion/terminal effects on clients; the source-research Skill narrowly checked pinned NPC extra-AI semantics. The bounded NPC-health record and v12 direction payload require matching packages across peers. No saved field, client health request, persistent input flag or server graphics dependency was added.

Focused checks passed: **81 deterministic domain cases**, **84 compiled codec round trips and 24 malformed/truncated cases**, and actual ModSources compile/package with **zero warnings/errors**. One initial rig-continuity check exposed an excessively fast elbow sweep; the folded wrist path was improved and the affected domain command passed. A final draw-only fold/tail adjustment was then rebuilt; protocol/domain logic did not change. Five original audio exports are finite and below full scale, with external individual/stem/composite auditions. Final package hashes, deployment and scoped static results are in [the check record](evidence/2026-09-06-eclosion-salvos-checks.json).

**0.2.12 game/client load, multiplayer HP-bar timing, eclosion/ending quality, camera/mask alignment and live audio are not_run / user-owned.** No game/server/GUI action was taken. Compilation and curve tests are not visual acceptance.

## Confirmed preceding 0.2.11 three-player pulls

Three authority-recorded pulls ended after **90.32s Defeat**, **72.80s Defeat**, and **44.77s Victory**. The clear consumed the fixed 4,000,000 Core HP over 20.05 damageable seconds (**199,501 DPS**), and the 750,000-HP Pylon pool in 3.22 seconds (**233,161 DPS**). Phase-I/II effective Core rates were **188,976 / 211,268 DPS**. The final phase lasted only 9.47 seconds: four grid fires occurred, and the fifth warning was cancelled by Victory. No Down/revive occurred in that clear. This supports more HP for three players; it does not prove a universal per-extra-player multiplier.

Across the three pulls, all five completed Pylon checks cleared; the final partial check of the first wipe was interrupted. All five Stack checks were incomplete: **1/3, 2/3; 2/3, 1/3; 2/3**. Authority distances agree with their 112px acceptance checks; one excluded survivor was only 3.9px outside. Logs do not establish peer-side marker alignment. Spread recorded no overlap damage. The two wipes had 10 Down events, four accepted instantaneous revives in their request ticks, and two eliminations in total. One revived recipient was Downed again before their 60-second lockout expired, then reached the 30-second Down timeout; that is a current-rule consequence, not evidence of failed item transport.

No Convergence battle exception was present in the inspected intervals. Startup warnings from other Mods and a before-listen socket error are not attributed to Raid HP/synchronization. [Sanitized telemetry](evidence/2026-09-06-eclosion-salvos-checks.json) retains per-pull windows/attendance; raw logs, player names, world paths and a detailed local report stay outside the repository. The user rejected the prior shell/readability/audio treatment, so these authority outcomes are not art/audio approval.

## Preceding build 0.2.11

Development **0.2.11 / protocol v11** adds a sealed Phase-I Boss shell, a protected half-HP cinematic rupture and an Unbound Phase-II lattice attack module. Stack now uses a fixed world-space gathering point above the Boss; Spread/recovery and current HP/defense remain unchanged. The field halves in both dimensions with an opaque black exterior; thicker colored warnings, stronger original SFX and a faster original Phase-II score accompany the new phases. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#boss-stages-and-fixed-assembly--0211), [visual spec](encounters/first-severance/VISUAL_SPEC.md), [audio cues](AUDIO_CUE_SHEET.md) and [ADR-0017](adr/0017-boss-stages-fixed-stack-and-lattice.md) own those facts.

The Raid development Skill guided authority/DTO/cleanup separation and focused checks; the source-research Skill was used only for the pinned NPC death and sound-limit hooks. The exact-Fight server/SP runtime chooses the stage, fixed Stack attendance and grid hits. Clients render bounded snapshots and conditional camera/HUD layers without persistent control flags. Packet IDs/requests and saved data are unchanged, but all peers must update together for protocol v11.

Focused checks passed: **77 deterministic domain cases**, **60 compiled codec round trips for 2/3/4 participants and 19 malformed/truncated cases**, and the actual ModSources build/package with **zero warnings/errors (23,016,178 bytes)**. The normal profile and four existing local test profiles have matching new packages; the old package is retained externally and other Mods are untouched. Seventeen new/remixed audio exports decode as finite, unclipped samples; PCM loop-boundary checks are not listening approval. The final scoped static wrapper result is recorded in the [check record](evidence/2026-09-06-boss-stages-checks.json).

GUI/game/server launch, multiplayer phase-NPC behavior, camera/zoom/mask alignment, animation quality and live mix are **not_run / user-owned** for 0.2.11. No in-game verification is claimed from a successful build or mathematical curves.

## Confirmed preceding 0.2.10 pull

The 2026-09-06 two-player authority log records a **109.43-second Victory / BossLifeZero**, all three Pylon checks cleared, and 4,000,000 effective Core damage. Core damageable time was 52.43s (party effective DPS 76,287.3); Pylons took 1,500,000 damage over 18.50s (81,081.1 DPS). Stack attendance was **1/2, 2/2, 1/2**: one success and two failures. No Spread overlap hit occurred. One charge caused Down; one accepted instantaneous revive followed 7.7s later with the recipient's 60-second lockout. This validates that pull's authority logs/HP calibration and recovery, not universal Stack synchronization or final balance.

No Convergence battle exception was present. Two caught `ObjectDisposedException` entries occurred later during player disconnect in Terraria's `NetMessage.CompressTileBlock` / `SendSection` MemoryStream path; this does not identify Convergence as their cause. A preceding client connection attempt also occurred before the server was listening. Raw logs and the local report remain outside the repository; sanitized aggregates are in the new check record.

## Preceding HP calibration — 0.2.10

Development **0.2.10 / protocol v10** recalibrates Boss/Core and Pylon HP using the observed two-player effective DPS, and adds passive authority damage-window telemetry. Defense, attack damage, timing, count/scaling policy, recovery and visuals remain unchanged. The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#provisional-hp-calibration-and-authority-diagnostics--0210) owns exact tuning and diagnostic field meanings. Telemetry is owned/cleared by the existing exact-Fight combat runtime; no client requests, replica fields, saved state or gameplay outcomes are added.

Focused domain checks passed: 72 cases, including shield-time exclusion, deadline/overkill bounds, 2/3/4-Pylon pool rates, recent versus whole-window DPS and persistent Core HP between exposures. The actual ModSources build/package passed with zero warnings/errors (21,011,111 bytes). The [check record](evidence/2026-09-06-hp-telemetry-checks.json) records the final static wrapper and deployment results. Runtime HP/readability and terminal log checks are `not_run`, user-owned; no game/server was launched.

Calibration evidence from the preceding 0.2.9 two-player Host & Play log: one pull survived without Downed events, completed both Stack checks, but failed three Pylon checks and ended at Overload 3. The Core lost 2,113,652 HP over two 12-second penalized exposures. This supports lowering the excessive HP, not a general balance or multiplayer synchronization certification.

## Preceding fluorescent-beam pass — 0.2.9

Development **0.2.9 / protocol v10** brightens fixed beams to fluorescent plasma with a white inner spine, replaces dashed warning lines with continuous contrast rails and converging lock glyphs, and strengthens smooth anticipation at emitters, Boss, Stack/Spread markers and remaining Pylons. Fixed beam damage is now 120 HP; charges, Raid-share penalties, fast intra-combo cadence, category rests and recovery are unchanged. The dark Boss/background remain. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) owns damage; [visual spec](encounters/first-severance/VISUAL_SPEC.md) owns appearance. No protocol/authority/persistence change or new image/audio asset is introduced.

Focused checks: 70 deterministic domain cases passed, including HP-independent beam damage and bounded continuous anticipation. The actual ModSources build/package passed with zero warnings/errors (21,005,951 bytes); the normal and four local profiles have matching packages. The scoped static wrapper passed once (51 indexed documents, 260 files, 10 YAML files). See [check record](evidence/2026-09-06-fluorescent-beams-checks.json).

GUI/reload and the two-player smoke are user-owned and `not_run`; no game/server was launched. At the user's request the normal world's 20-Mod selection, including Ore Excavator 0.8.9 and CalamityHunt 1.2.3, is now the local testing selection too; all user-added Mods are preserved. The prior normal-profile client content load confirms those Mods were present, not compatibility with the new Convergence build. This is not a new compatibility certification.

## Preceding continuous-emission pass — 0.2.8

Development **0.2.8 / protocol v10** connects continuous attack emitters, original high-resolution emission/rig/architecture atlases, a ground-anchored 12x4 foundation, a physically confining participant-only field and Raid-only infinite flight. The authority keeps the validated 320x140 layout and rechecks it at all-Ready. Old 2x2 Cores retain their saved identity; mining/replacing uses the larger foundation. Boss/Pylons now occupy the airborne field center. The safe designation/deployment intro is six seconds; fast intra-combo cadence, other major-phase rests, stats and recovery rules are retained. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md) and [ADR-0016](adr/0016-ground-containment-and-continuous-emission.md) own behavior and architecture.

The previous four-tick tracking presentation and abrupt pose/beam switches are replaced by harmless-windup interpolation, a fractional visual clock, connected parent/child bones and shared opening/gathering/emission/cooling envelopes. Up to four casts retain bounded harmless tails; actual warning/live rails remain tied to authority geometry. This is an implemented presentation pass, **not** evidence that WotG-equivalent visual quality or dual-client performance is achieved. [F11 research](research/WOTG_RAID_BENCHMARK.md) records the pinned source influence without importing external code/assets.

Focused checks: 68 domain cases passed, including continuity/field-body bounds and strict airspace validation. Compiled v10 codec passed 42 round trips and 11 malformed/truncated cases. Actual ModSources compiled/packaged with zero warnings/errors (21,002,047-byte package). Dedicated Server loaded the copied world and started listening; both real clients loaded the matching package and reached their menu. Simple Whip Addon 1.15.12 is present in all three profiles. The requested launch handoff does not join characters, arm an auxiliary permit, auto-Ready or start a Raid. Latest operator player data is copied into the isolated profile with its previous copy backed up; originals remain unchanged. See [focused check record](evidence/2026-09-06-continuous-emission-checks.json).

Runtime field/flight/legacy-placement, live animation readability and equipment/latency/cleanup interactions remain **not_run / user-owned**. Startup retains the prior missing optional icon_small warning and third-party asset-load timing warnings; inspected startup logs contain no error-level exception. A load-to-menu is not a combat or rendering-quality pass.

## Preceding fast-combo pass — 0.2.7

Development **0.2.7 / protocol v9** restores the earlier fast cadence inside each attack combo: Pursuit Prism starts every 42 ticks instead of 96; Dash–Stillness steps start every 72 instead of 156. The intro, Pylon opening cue, four-second Stack/Spread windows, exposure opening rest, extra full-combo rest and two-second Reset are unchanged. The shorter charge windup retains the 24-tick harmless locked hold; damage never starts before FireTick. Hit geometry, travel speed, stats, recovery and auxiliary permissions are unchanged. Current numbers live in the [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md).

Focused checks: 66 domain cases passed, including actual-volley/scheduler deadline agreement and unchanged major-phase timing. Compiled v9 codec passed 42 round trips and 11 malformed/truncated cases. The actual ModSources package built with zero warnings/errors. The static wrapper result and explicit remaining runtime checks are recorded in the [check record](evidence/2026-09-06-fast-combo-checks.json). GUI/game launch and multiplayer smoke are user-owned and `not_run` for 0.2.7; all peers need the new timing-compatible package.

Animation diagnosis (not repaired in this tuning pass): authority samples the harmless tracking orbit every four ticks, and `ChargeHeadAt`/`DrawEnergyBody` draw the last received windup position without interpolation, so the body can visibly step at approximately 15 Hz despite a higher render rate. Separately, `FirstSeveranceBossVisuals.Draw` switches casting pose from fully raised to zero on FireTick while recoil jumps from zero to full; its three rigid texture slices have discontinuous transforms rather than a continuous cast/fire/recovery animation. These are concrete presentation gaps, not a conclusion that developing content must be jerky. Existing logs have no frame-time evidence to establish or exclude additional FPS/dual-client load. The intentional stationary prelaunch hold is distinct from these issues. Future smoothing must remain client-only and preserve exact lock/fire/collision cues.

## Preceding auxiliary-client build and staging — 0.2.6

Development **0.2.6 / protocol v8** adds opt-in, Dedicated Server console-only, one-Fight protection for one real auxiliary client. Normal play has no grant command or persisted character permission. Manual Core activation/Ready and the 2–4-player Raid rules remain unchanged. [ADR-0015](adr/0015-console-only-single-pull-assist.md) owns authority/cleanup; the [single-operator runbook](runbooks/SINGLE_OPERATOR_TESTING.md) owns usage. User GUI plus a friend remains the default; the user explicitly authorized Computer Use and one-person staging for this session only.

The isolated local server and two real clients loaded the matching package and joined successfully. Separate A/B/server save profiles use copies, leaving original player/world files untouched. Both are at the existing Core. Console confirmed the exact auxiliary binding was claimed by preparation; the auxiliary's GUI confirmed Ready **1/2**. Its hotbar slot 8 holds a Resuscitation Kit. Both external Cheat Sheet God Mode tooltips were confirmed Disabled after staging. The operator is left unready for the final Core click; combat has not been started by the agent. This is not a damage-protection, revive or combat-clear result.

Focused checks: 65 domain cases passed (including three new permit/identity/lease groups); compiled v8 codec passed 42 round trips over 2/3/4-member records and 11 malformed/bounds cases; actual ModSources build/package passed with zero warnings/errors. The scoped static wrapper passed (50 indexed documents, 247 repository files, 10 YAML files). Runtime damage suppression, owner-client TTL/Defeat cleanup, remote latency and friend-side behavior remain `not_run` for this build.

Staging observations: simultaneous client startup produced one shared `steam_appid.txt` file-in-use failure; sequential startup succeeded. Existing eclipse enemies caused ordinary deaths in copied saves; temporary external God Mode was used during positioning, then disabled on both clients. Two caught Calamity `SepulcherMinion` null-reference exceptions (charging/darts) appeared, without established Convergence causality. The oversized Core visual is offset from its actual 2x2 ground interaction tile in the observed rendering; that existing visual alignment issue is not fixed by this build. No raw logs, saves or personal paths are committed.

Subsequent user-run log observation (2026-09-06, 12:36:56–12:37:31): two real clients entered a new Fight after an earlier `roster_too_small` rejection. The previous one-use auxiliary permit was bound to the expired preparation, and no new `DebugAssistBound` appeared; the auxiliary took Pylon/charge damage and was not protected in this pull. Full 2/2 Stack cost zero HP; no Spread damage was recorded. The operator Downed to Stillness, then the auxiliary Downed to SweepLeft, causing `AllParticipantsDowned`. Both client logs reported `DefeatDeathApplied dead=True`. No revive request/success was logged. This confirms ordinary two-client Defeat execution in that unprotected pull, not assisted invulnerability or revival. A caught third-party gold-critter/Lighting exception preceded combat; no uncaught Convergence runtime exception was observed in the inspected combat interval.

## Preceding audiovisual build — 0.2.5

Development `0.2.5` replaces the rejected Ninth/chiptune with the original **Obsidian Liturgy** orchestral-textural sketch and 18 newly rendered action cues. The user can audition external BGM/individual WAVs/a timestamped SFX reel without launching the game. New original high-resolution basalt/ivory Boss, cathedral sky and void-lance textures accompany a restrained bone/bronze palette, fine rupture filaments and a much larger casting pose. [Visual spec](encounters/first-severance/VISUAL_SPEC.md) and [Audio cues](AUDIO_CUE_SHEET.md) own appearance and sound.

Longer warnings and between-action pauses are in the [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md). A charge locks its position/heading for 24 harmless ticks before firing; this removes the pre-lock hits seen in the preceding session. Fast live travel, HP/damage, free successful Stack/Spread, reusable revival and exact-Fight cleanup are retained. The unchanged attack DTO has protocol **v7** because both peers derive collision deadlines from shared tuning; v6 peers must update. [ADR-0014](adr/0014-deliberate-anticipation-and-solemn-presentation.md) supersedes the old live-steering/presentation choice.

The [0.2.5 check record](evidence/2026-09-06-solemn-pass-checks.json) owns automated results and explicit remaining checks. No GUI/game launch is performed: the user owns reload/Host & Play and audiovisual acceptance. Source/asset licenses remain undecided for release.

The preceding 0.2.4 two-player session loaded the hotfix, entered the world and ran six pulls; the old spawn exception was absent. All six ended AllParticipantsDowned. Two full 2/2 Stacks had no damage; no Spread damage or successful revive was logged. Charge hits preceded direction lock by 2–4 ticks in the comparable casts. One caught Calamity Sepulcher AI exception occurred. These observations do not establish 0.2.5 behavior or friend-side rendering.

## Energy and audiovisual pass — 0.2.3

- The exact-Fight authority swings one body behind/overhead, briefly steers its fast launch, then freezes the heading. Compact swept collision prevents tunnelling; the long wake is cosmetic. No per-player projectile multiplication. [Encounter Spec](encounters/first-severance/ENCOUNTER_SPEC.md) owns tuning; [ADR-0013](adr/0013-energy-charge-and-client-feedback.md) owns motion replication/cleanup.
- Stack/Spread gain distinct gathering/dispersing sounds, final countdown accents and committed success/failure bursts. Participant cues no longer fade with distance from the giant Boss. Down/revive, warning/fire, Pylon break, exposure, intro and terminal outcomes have dedicated synthesized audio. [Audio Cue Sheet](AUDIO_CUE_SHEET.md) owns the mapping and [Attribution](../Assets/ATTRIBUTION.md) separates all music rights/source layers.
- The original black occultation, broken corona and far pillars render as a client sky, without changing world weather/time or overlaying terrain. Reduced Effects and independent shake settings remain supported. Audio voices, temporary feedback and sky are reset on World/Mod unload.
- The preceding 0.2.2 two-player logs showed four AllParticipantsDowned defeats; complete 2/2 Stack passed with no damage, separated Spread advanced without damage, and one instant revive carried the exact +3,600-tick deadline. Host normal Defeat death was logged four times. Friend-side death execution and audiovisual quality were not proven by those logs. Two caught Calamity Sepulcher projectile exceptions also appeared; no Convergence runtime exception was observed in that session.

## Historical illustrated attacks and Raid consequences — 0.2.2

- Pylon windows use predicted angled beams in the requested red/blue/green/yellow return order. Exposure uses right-dash/stop/left-dash/stop with sweeping blades and overhead curtains around an empty column. Origins are independent of the Boss; its casting structures animate and recoil. [Encounter Spec](encounters/first-severance/ENCOUNTER_SPEC.md) owns the precise geometry/timing.
- Full frozen-roster Stack attendance and non-overlapping Spread cost zero HP. Incomplete Stack deals each standing participant the missing-roster fraction of their own maximum HP, including those outside the circle; two players with one missing is 50%, four with one missing is 25%. Required attendance is now 2/3/4, not 2/2/3.
- Accepted exact-Fight Defeat orders ordinary death for all connected participants. Outsiders and other endings are excluded. Normal character-difficulty penalties apply, including Hardcore character loss. Owner execution and external death-cancelling hooks are logged; actual multiplayer death/respawn is not yet verified.
- The 1.5-second intro temporarily skips normal HUD drawing and displays the Raid/Boss names, dark monochrome framing, an ominous vanilla cue and optional camera shake. It does not change persistent UI settings or stop gameplay. No new raster/audio asset or extra world actor was added.
- Protocol v5 adds bounded pattern/step/target/ray dimensions. Both peers must update together. The reusable instant kit, recipient-only lockout and excessive Boss/Pylon stats remain unchanged. [ADR-0012](adr/0012-pattern-sequences-and-defeat-death.md) records the authority/compatibility changes.
- 61 domain cases passed, including five targeted groups for this change. Compiled v5 codec passed 42 absent/pattern cases over 2/3/4-player records and seven malformed/truncation cases. Actual build/package and client reload-by-restart passed after the initial live-file lock was released. Static completion results are recorded in [the focused check record](evidence/2026-09-06-pattern-sequences-checks.json).
- The user's updated Skills/document instructions were recovered from their edited working copy and retained. Verification is scoped to changed behavior; the only agent-run game launch was the user's subsequently requested load-to-menu handoff. No agent-run world/combat session or release matrix was added.

## Confirmed preceding 0.2.1 Host & Play log

The inspected 2026-09-06 server/client logs loaded `0.2.1` and contain four two-player attempts. Four instant revivals succeeded, each publishing a recipient deadline exactly 3,600 ticks later; the two players also revived each other, confirming a recipient's lockout does not block rescuing someone else. All four attempts ended in `Defeat / AllParticipantsDowned`; the final observed survivor took 786 Stack damage at 124 HP. This informed the requested Stack redesign. Exact lockout-expiry eligibility, rejected repeat revival, and the new `0.2.2` behavior are not runtime-confirmed. Caught Simple Whip Addon and Calamity projectile exceptions were present; causality with Convergence was not established. No raw logs or player identities are committed.

## Historical instant revival and high-intensity pass — 0.2.1

- An Alive participant uses the reusable Resuscitation Kit within 8 tiles of the nearest eligible Downed ally. Authority revalidates after this tick's damage and resolves the stable request batch instantly: no held channel, movement interruption, shared token or item consumption. HP returns to 35% with 3 seconds of protection.
- The recipient cannot receive another revival for 3,600 ticks / 60 seconds. Down does not clear the deadline; the visible non-cancellable debuff reflects server-owned state. The recipient may still revive someone else. Fight cleanup clears it. Down still expires after 30 seconds, so a second Down while locked may become unrecoverable.
- Kit requests send ordinary inventory/control synchronization first; wrong-item/alive/range/lockout failures now have distinct localized messages. This is not yet proof that every multiplayer item-switch race is fixed. Protocol v4 appends a bounded recipient deadline to each participant projection; both players need this build. See [ADR-0011](adr/0011-instant-revival-and-recipient-lockout.md).
- Foundation Core has a new original 176-world-pixel-tall visual canvas and placement preview. The existing 2x2 Tile/TE, saved coordinates and right-click base remain unchanged; no replacement/migration is required. Dedicated Server never loads the image.
- Boss HP/defense: **60,000,000 / 240**. Each Pylon: **1,000,000 / 120**. Stack pool: 140% of average pull maximum HP; failed Spread: 70% maximum HP; failed Pylon pulse: 35%, clamped nonlethal; lance: 60%. The enormous decorative body still has no contact damage.
- Intro 1.5 s, Pylon cue 0.5 s plus 8 s active, Stack/Spread 2.25 s each, exposure 10 s or penalized 5 s, Reset 0.5 s. Lances warn for 42 ticks / 0.7 s, fire for 14 ticks, and repeat every 66 ticks / 1.1 s. The two-ray cap and exact locked 88-pixel damage corridor are unchanged. The existing eight-exposure/three-Overload caps may make this intentional over-tuning impractical to clear.
- Faster casing opening/orbits, recoil, exposure shock rings, brighter charge/fire, speed streaks, layered vanilla impacts and short stronger camera kicks replace the softer motion. Camera Shake OFF and Reduced Effects remain available without hiding danger information. There is no full-screen white flash or gameplay hit-stop.
- At delivery: 56 domain tests passed, including five focused instant-revival/lockout/race/cleanup cases; actual ModSources `0.2.1` packaged with 0 warnings/errors. The later user-run Host & Play log observation is recorded above; the agent did not operate that game session.

## Historical giant Boss and observation lances — 0.2.0

The following records the preceding build; `0.2.1` values and recovery rules above supersede its timings, damage, protocol and channel behavior.

- Original generated black-ice/ceramic containment body, approximately 820 pixels tall at full reveal, with a roughly 1,100-pixel orbital apparatus. Side structures open on exposure; broken seals, faint aurora, drifting shards, core brackets, charge/fire effects and terminal dissipation are client-only. The body source is RGBA with a transparent exterior, not extracted artwork.
- One stationary authority-owned Boss NPC remains. Its 144x144 central aperture is the only Boss hitbox, 360 pixels above the Foundation Core. Decorative mass has no contact damage. Boss/Pylon HP and the six-state loop are unchanged. Pylons use larger original code-drawn containment cages and wider placement.
- Stack still shares its experimental pool within 7 tiles. Spread now shows 14-tile-radius circles and requires 28-tile center separation. Circle boundaries and server checks share the same tuning constants.
- Pylon/Exposure phases aim at the current Alive roster, lock the observed positions at cast start, show the complete 88-pixel-wide danger corridor for 72 ticks, then fire for 18 ticks. Single/double shots alternate every 102 ticks (1.7 s), capped at two rays regardless of party size. No beams run during intro, Stack, Spread or Reset; phase exit cancels the current volley.
- Each volley hits each participant at most once, for experimental 35% maximum HP. Downed players are excluded and post-revive immunity is honored. Lethal lance damage enters the existing Raid Down/revive path, not ordinary Terraria death hooks. Hits interrupt revival through the existing HP observation. No terrain is destroyed and outsiders are not damaged by this custom attack.
- Protocol v3 appends an optional bounded volley to full combat snapshots: serial, start tick and at most two finite unit rays. The server publishes assignments immediately; clients do not choose aim, timing or hits. Packet IDs and all client request shapes remain unchanged. Both players must update to `0.2.0`; v2 clients are intentionally incompatible. [ADR-0010](adr/0010-giant-boss-observation-lances.md) records this development expansion.
- Client settings offer reduced decoration and camera-shake disablement without removing danger indicators. Vanilla Boss 3 remains the BGM; vanilla sound IDs supply temporary charge/fire cues. No music/SFX recording is bundled.
- Source/asset review and focused geometry/timing/immutability tests plus the actual tModLoader command build are this iteration's checks. The new in-game size, hit readability, 2-client aim synchronization and frame cost are user-run checks, not inferred from compilation.
- Completed checks: all 51 deterministic domain tests passed; the actual compiled combat codec round-tripped absent/single/double volleys from a shared buffer, preserving locked rays and consuming the exact payload only; actual ModSources `0.2.0` packaged with 0 warnings/errors. No game GUI or live server was operated for this pass.

## Confirmed 0.1.2 targeted playtest

The user reported readable shrinking circles. The two-player Host & Play authority log confirms a Down, an interrupted first attempt, and then a successful revive from tick 4745 to 4865 (exactly 120 ticks). A later second Down expired after 1,800 ticks with the shared token consumed, correctly ending as `Defeat / RecoveryImpossible`. No recovery payload-size rejection, read underflow or Convergence runtime exception appeared in that inspected run. This is not a `0.1.2` clear claim; the earlier `0.1.1` clear remains user-reported.

## Playtest hotfix 0.1.2

- `0.1.1` server logs confirmed repeated `first_severance.revive_payload_size` rejection with `Read underflow 31 of 35 bytes`. Down and revive readers incorrectly treated the shared receive stream's remaining length as the packet payload size. Both now read exactly one uint nonce; header/sender/rate checks follow decoding. IDs, payloads and protocol version 2 are unchanged.
- The screenshot shows screen-length orange spokes during **Spread**, not a Down attack. Circle segments were stretching the full MagicPixel texture; drawing now samples an explicit 1x1 source rectangle. Rings and Down particles are presentation only. Spread still deals damage once to players closer than 16 tiles at resolution; damage tuning is unchanged.
- Authority logs now record phase transitions, damage source/life, Down/revive/cancellation/timeout events and exact terminal cause. No player names, addresses, positions or per-frame records are added. Diagnostics cannot interrupt cleanup or domain transitions.
- The client displays the localized ending cause in chat and for 10 seconds on the HUD, with more prominent Down instructions and a separate eliminated state. All-Downed still ends immediately; it does not wait for the 30-second personal deadline.
- Focused compiled-code checks reproduced both old shared-buffer failures and passed the fixed standalone/shared-buffer, zero-nonce and truncated-input cases. The subsequent user render confirmation and logged held-use revival are recorded above.
- Separate `0.1.1` logs also showed caught exceptions in Simple Whip `GoldRush_Shot` and Calamity `SepulcherMinion`. Their relationship to this fight is unproven; no third-party code or enabled-Mod settings were changed.

## Historical initial development combat experiment — 0.1.1

These are the initial implementation values, not the current balance or recovery contract. See the `0.2.1` section and current encounter/revival specs for active rules.

- Authority composes the existing loop and revive domains after Ready; Boss/Pylons are tracked by NPC slot/type and a per-Fight token. Observed `OnKill`, not arbitrary disappearance, supplies actor deaths. Normal NPC hit gating is provisional for cooperative clients, not a verified anti-cheat boundary.
- `SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset` runs with the existing timers. Boss HP is 1,200,000 and each Pylon has 25,000 HP for this experiment. Only exposed Boss/current Pylons accept player hits.
- Stack follows a round-robin Alive target and shares a fixed experimental HP-damage pool (90% of the pull roster's average maximum HP). Spread hits each overlapping Alive participant once for 40% maximum HP. Failed Pylons pulse 25%, clamped nonlethal. These direct Raid-owned HP changes intentionally do not claim the unmeasured Terraria mitigation/death pipeline. Stack target reissue and a live Barrier remain deferred.
- `/convergence-down` requests the sender's experimental Downed state. Raid-owned damage can also Down a participant without sending lethal HP through ordinary death hooks. The shared service owns the 30-second Down deadline, 2-second channel, 1/2/3 tokens, 35% restored HP, and post-revive timers.
- Kit starts validate the current sender binding, Alive state, held item, range and available token. The server chooses the nearest unreserved Downed ally; channels check held use, movement, damage, hooks/mounts and range. Health corrections have a per-participant revision and are applied on the owning client as well as authority. Weakness reduces generic damage by 20% for 10 seconds.
- The initiator may use `/convergence-cancel` during combat. Victory, Defeat, cancel, Core/actor loss, unload and exceptions clean owned NPCs and player projections. Ordinary Terraria death or a roster disconnect aborts this experiment; reconnect/re-entry is not enabled. Cleanup restores incapacitated players to at least 35% HP.
- Protocol v2 introduced bounded Down/revive requests and a combat section; v3 extends it with the observation-lance volley above. No audio file is extracted or packaged.
- Foundation-smoke validation remains explicit for this development experiment because it does not edit World terrain, build a Barrier, or grant progression/rewards. Production arena and lethal-hook gates are not declared complete. See [ADR-0009](adr/0009-development-combat-experiment.md).

## Implemented and connected

- tModLoader Mod source skeleton, feature registration, project metadata, Calamity hard reference for Stage A, and a Windows-confirmed compatibility-version policy.
- Generic encounter identifiers, definitions, catalog, lifecycle transitions, one-session authority coordinator, runtime/factory boundary, cleanup scope, retry backlog, and terminal snapshot outbox.
- Feature-neutral immutable termination descriptors carrying generic reason plus bounded schema/version/cause data; constructor-validated definition-owned mappings for World unload, internal failure, and fatal protocol failure; external preemption and replica/tombstone validation.
- Versioned packet envelope parsing, explicit packet IDs, direction guard, bounded rejection behavior, typed First Severance activate/Ready/cancel/snapshot handling, and initial/join snapshot delivery.
- Read-only encounter replica ordering by Encounter Sequence, revision, and authority tick, including terminal tombstones.
- Pure server/SP `Common/Raids/Revive` state machine: stable participants and connection epochs, Downed deadlines, batched revive arbitration, channel leases/nonces, token reservation/consumption, reconnect grace, same-tick wipe commit, bounded snapshots, projections, and exact-Fight cleanup.
- Dependency-free domain harness covering the revive domain, immutable arena objects and scan results, roster/Ready/Core lease boundaries, six-state loop policy/deadline behavior, Calamity gate-result invariants, terminal mapping, coordinator external endings, creation failure, and retained tombstones.
- Repository policy, YAML validation, CI, ADRs, provenance rules, and repository-local Raid/source-research Skills.

## Implemented First Severance foundation

- `Content/Encounters/FirstSeverance` contains an immutable 160x70 Core-anchored arena blueprint, logical outsider policy, preparation/combat runtimes, and a boundary around the revive service.
- Slice 3 adds a development Foundation Core ModItem/2x2 ModTile/ModTileEntity with dedicated prototype pixel art, active-only mine/explosion protection projection, and an authority-owned exact-Fight lease. The item has no recipe; right-click submits a server/SP activation request and reports validation or Ready-count state in chat.
- An authority-only resolver derives the exact server TE from any Core coordinate, checks the prospective arena without mutation, records deterministic fatal issues/warnings/metrics, and rejects requester distance, World conflict, foundation/protected/container/foreign-TE/Core conflicts, incomplete scans, and ambiguous participant counts.
- Current server-slot connection epochs feed a deterministic frozen 2–4 roster. The pure preparation state supports Ready/unready, per-participant nonce and exact binding checks, a provisional 60-second timeout, initiator cancel, Core loss, participant loss, a permanently closed combat gate, and exact-Fight idempotent cleanup.
- The coordinator now resolves Core/Arena/roster before acceptance, enters `Validating -> Preparing`, applies Ready/cancel intents on authority ticks, publishes bounded preparation snapshots, and releases the exact Core lease on cancel, timeout, Core loss, participant loss, unload, or failure.
- The Development Build keeps Core identity, bounds, requester, World conflict, 2–4 roster and duplicate Core fatal; foundation/content checks are warnings only in the explicitly scoped experiment above. Strict validation remains the default API.
- The Calamity boundary queries only public `GetBossDowned`/`GetDifficultyActive` calls for Exo Mechs, Supreme Calamitas, and Boss Rush, validates boolean returns, and fails closed on missing/changed behavior. Public `2.2.2` source is reference-only for the installed `2.2.4` binary, so runtime call verification remains required.
- The feature owns a validated `SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset` plan and pure high-level loop state machine. It encodes 2/3/4-player Pylons and full-roster Stack attendance, persistent Boss life, normal/penalized exposure deadlines, three-Overload Defeat, eight-exposure `Defeat + LoopCapExceeded`, and no damage quota or enrage phase.
- `FirstSeveranceTerminalCause` has append-only byte values `0..13` and exact generic mappings. Feature-owned runtime End, World unload, runtime exception, and queued fatal-protocol termination preserve the descriptor through terminal snapshot, cleanup context, outbox, and retained replica tombstone.
- `FirstSeveranceAvailabilityPolicy` permits the development path; the production inert world adapter remains unused. The preparation domain itself remains inert, and its composing runtime starts the experimental adapter only after all Ready.

## Not implemented

- Attack replay/preview controls, auto-rescue and NPC participant adapters. The real-client, console-authorized auxiliary is separate; see Current build and the [single-operator runbook](runbooks/SINGLE_OPERATOR_TESTING.md).
- General outsider warning/ejection/admission, adversarial teleport handling and progression-call runtime instrumentation. Participant-only development containment is implemented above.
- Production normal-hit collector/mitigation, general projectile attacks, robust observer/rejoin support, and expanded multiplayer/latency verification. Player-carried Stack target reissue is no longer applicable to the current fixed-site design.
- General gameplay lethal-hit interception into Raid Down and Calamity self-revive coexistence. The developer-only protection uses a scoped `PreKill` cancellation, not the production lethal-to-Down adapter.
- Rewards, final hand-cleaned sprites/audio/VFX, final tuning, and release packaging. The new original giant-body/VFX pass remains provisional. Experimental UI is localized in English/Japanese.
- Optional local SQLite/FTS/embedding cache generator; the committed Markdown/front-matter catalog exists, but no binary-search database is built or required.
- Standalone progression/content that would replace the current hard Calamity dependency.

## Verification state

| Gate | State |
|---|---|
| Documentation/catalog, repository/Skills and YAML | 0.2.17 scoped result: [check record](evidence/2026-09-07-recovery-tempo-checks.json) |
| Dependency-free domain tests | 0.2.17:93 passed, including untimed recovery, simultaneous Prism, new shared geometry and retained solo/Stack rules |
| Compiled preparation/combat codec | v16:308 round trips/48 malformed cases passed for1–4members, including untimed Down and larger Prism bound |
| `dotnet build ConvergenceMod.csproj` | Actual0.2.17 ModSources build/package passed, zero warnings/errors |
| Client load / Build + Reload equivalent | Preceding0.2.16 played by user;0.2.17 restart/load not_run |
| Single Player / one-client Host & Play | Repaired0.2.15 path retained; actual one-member combat-start evidence still unavailable |
| Steam-friend preparation | `0.1.1` logs confirm local Host & Play server and two joined players |
| Combat/BGM/Down/revive in game | User confirms preceding Stack issue resolved; new0.2.17 recovery/attacks/mix remain user-owned not_run |
| Dedicated Server load/2-client smoke | 0.2.8 server/world load passed; two-client joining/combat not_run |
| Calamity lethal-hook instrumentation | Not run; blocks the live Downed adapter |

The confirmed runtime is Terraria `1.4.4.9`, tModLoader stable `v2026.07.3.0`, Calamity `2.2.4`, and Calamity Music `2.1`. See the [sanitized Windows baseline](evidence/2026-09-05-windows-baseline.json). The latest local development pack also loads Simple Whip Addon `1.15.12`, WingSlot Extra `1.4.5`, Cheat Sheet `0.7.8.1`, Magic Storage `0.7.0.11`, and its Serous Common Library `1.0.6.2` dependency. The subsequent `0.1.1` Host & Play observation above is separate from that earlier Dedicated Server baseline; it does not establish the full compatibility matrix.

## Current accepted direction

- Primary implementation/verification workstation: Windows desktop.
- MacBook: documentation, review, Git, lightweight domain edits; no runtime claim without the pinned game toolchain.
- First playable Raid: `First Severance` for 2–4 players after Exo Mechs and Supreme Calamitas; temporary build-gated one-player development checks are available, not a public solo rebalance.
- Active stages: I opening plus clockwise Stack relay; II lattice/Spread/twin rotating blades; III remote arms/half-field beams/Stack/Spread/central crush; explicit Final at HP0 with eight clockwise Stack/Spread/dodge stations. Each phase must finish its first full score before advancing; current thresholds50%/25%/0.
- Boss accepted boundary: one logical NPC/life pool, with user-requested giant original presentation. The one-body boundary does not limit visual size or decorative complexity.
- Recovery: untimed Raid-only Downed, no Eliminated; ally-used nonconsumed instant item, server-owned recipient-only60-second revival lockout, no shared tokens. All-Down remains immediate Defeat.
- Long term: ship/validate the Calamity addon first, then migrate through the accepted staged path toward a standalone Content Mod.

## Next change

User-owned Host & Play after restarting on matching0.2.17 packages: confirm Down can wait out the recipient lockout without Eliminated; observe simultaneous Prism targets, borderless forecasts, two faster sword revolutions, changing Phase-III safe strips and progressively faster/shifting Final. Listen to the less dominant effects versus raised music. Keep server/client logs for contact disagreement; observe ordinary end/cancel cleanup and Reduced Effects as convenient. Do not reopen accepted Stack synchronization without fresh evidence or demand a full release matrix for this short development check. Solo is opt-in; no extra client/NPC is needed. No game/server launch or other Mod/save change belongs to this pass.

Ordinary workflow remains user GUI plus a friend unless solo is explicitly requested. Art/audio acceptance remains human review; the research record does not claim unobserved video frames/audio were watched, nor that another broad test suite is required before this short observation.
