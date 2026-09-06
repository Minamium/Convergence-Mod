---
doc_id: history.pre-consolidation
document_type: evidence
status: historical
owners:
  - engineering
last_reviewed: 2026-09-07
source_of_truth_for: []
aliases:
  - development history through 0.2.17
related_code: []
related_docs:
  - project.status
  - encounter.first-severance.plan
---

# Pre-consolidation development record

Historical snapshot at commit e52f6ba. Preserved statements include superseded constraints, plans and provisional tuning: **none is a current instruction or activation gate**. Use [Status](../STATUS.md) and the [current encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md) for present behavior. Evidence links are retained; accepted ADR bodies are unchanged.

## Snapshot: docs/STATUS.md

## Project Status

### Current build

Development **0.2.17 / protocol16** removes active Eliminated/Down expiry, targets all standing players simultaneously in the opening Prism, replaces Phase-III horizontal lanes with smoothly deploying/accelerating flood bands and moving safe strips, makes twin blades rotate twice faster, removes heavy beam/sword border rails, and makes Final patterns shift and progressively accelerate. Existing Stack synchronization/shares, Spread separation/damage, frozen HP, recipient60-second lockout, reusable instant item, all-Down Defeat and solo admission are preserved. [Encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md#new-attack-modules), [recovery spec](../encounters/first-severance/REVIVE_SPEC.md), [visual spec](../encounters/first-severance/VISUAL_SPEC.md#continuous-borderless-danger-materials--0217) and [audio cues](../AUDIO_CUE_SHEET.md#rebalanced-mix-and-continuous-flood-articulation--0217) own design/tuning.

The Raid development Skill kept server/SP outcomes and read-only fractional drawing on shared geometry, bounded Prism snapshots and exact-Fight cleanup. [ADR-0021](../adr/0021-untimed-recovery-and-simultaneous-prism.md) supersedes only the old timed-Down policy and single-focus Prism bound. Generic legacy recovery enums/tests remain, but active First Severance has no Eliminated transition or label. No new actors, graphics/audio on Dedicated Server, global audio settings, external sources, saves, Mod-list or public debug changes. Every peer needs the matching protocol16 package.

Focused checks passed: **93 deterministic domain cases;308 compiled preparation/combat round trips and48 malformed/truncated cases; actual ModSources build/package with zero warnings/errors,31,939,686bytes**. Checks cover indefinite Down through lockout/exact rescue, all-Down failure,1–4 simultaneous Prism rays, shifting safe pockets and shared flood width, two-turn identity, mandatory phase scores, travel estimates and lack of permanent stationary Final lanes. Two old test expectations (expired-Down event and comb edge count) were updated before the passing run. Four revised music assets and three action effects decode as finite48kHz samples; four-times oversampled peak stays below0.902. BGM gain is about+1.94dB; feature SFX playback is0.80×. External PCM24 auditions are available. [The check record](../evidence/2026-09-07-recovery-tempo-checks.json) owns static/deployment results.

**0.2.17 game load, live audio/visual readability, dodgeability and multiplayer contact/cleanup remain not_run / user-owned.** No game, server or GUI was launched. Automated checks do not establish human audiovisual acceptance. Current packages replace only Convergence, with previous packages retained externally.

### Preceding build 0.2.16

Development **0.2.16 / protocol v15** makes warning audio conspicuous, replaces the single slow blade with two rapid-unsheathed accelerating blades, unifies the full-volume anticipation of broad Phase-I/III beams, adds a lethal central hand crush to the Phase-III score, and increases Final bead/comb density and effects. The user confirms the preceding Stack synchronization problem is resolved; its movement heartbeat, authority coordinates, valid radius, HP, Spread and revival rules are preserved. [Encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md#new-attack-modules), [visual spec](../encounters/first-severance/VISUAL_SPEC.md#rigid-blades-legible-volumes-and-terminal-density--0216) and [audio cues](../AUDIO_CUE_SHEET.md#forewarnings-and-bladecrush-articulation--0216) own tuning and art intent.

The Raid development Skill kept outcomes and lethal-to-Down in the existing exact-Fight runtime, with shared deterministic geometry and no new world entities. Imagegen Skill supplied an original2172×724 RGBA blade, preserved without raster edits and consumed client-only as a rigid, field-clipped material. New/revised SFX are independently synthesized; music, dependencies, Mod lists, saves and public controls are unchanged. Protocol15 appends RemoteCrush without renumbering earlier substates/packets or changing the bounded DTO layout; all peers must update together.

Focused checks passed: **90 deterministic domain cases; 308 compiled preparation/combat round trips and46 malformed/truncated cases; actual ModSources build/package with zero warnings/errors,32,221,196bytes**. Coverage includes the full1–4-player score, crush warning/contact/recovery and safe pockets, accelerated blade boundaries, denser but open Final lanes, and retained Stack/recovery/terminal invariants. An initial test argument order and one client compile identifier were corrected before passing. One original texture was visually/alpha inspected;16 new/revised mono effects decode as finite samples with peaks below0.911. External PCM24 auditions are available. [The check record](../evidence/2026-09-06-attack-intensity-checks.json) owns final static/deployment results.

**0.2.16 game load, live audio/readability/dodgeability, multiplayer contact alignment and cancel/end cleanup remain not_run / user-owned.** Neither GUI nor game/server processes were launched. A package and waveform/geometry checks are not a claim of finished WotG-level presentation. The latest user observation accepts Stack sync, not the new audiovisual pass or solo actual activation.

### Preceding build 0.2.15

Development **0.2.15 / protocol v14** fixes the Ready-to-combat scan's forgotten solo opt-in: both actual CoreResolver admission paths now use one compiled, non-optional start-validation policy. It adds an owner-only six-tick vanilla movement heartbeat after Player updates, removes duplicate server-side fallback lift/predictive braking, and retains authoritative containment and Stack decisions. Bounded client Stack coordinate/velocity samples and authority attendance/correction logs support cross-view diagnosis. The observed activation receive-underflow is fixed by parsing each fixed activation/Ready/cancel payload before header/rate rejection. Stack radius112, shares, Spread640 separation, HP, damage, phase score, recovery, dependencies, saves and Mod list are unchanged.

The Raid development Skill kept these changes feature-local with exact-Fight capability cleanup. The source-research Skill checked pinned hook execution and movement-message contracts; [the source record](../research/RAID_MOVEMENT_SYNC_APIS.md) distinguishes confirmed defects from the remaining movement-drift inference. **The reported peer displacement is not conclusively explained by the old logs:** they lacked client coordinates and the friend's log is unavailable. This build corrects the identified asymmetric movement path and adds evidence, not a claim of proven live synchronization.

Focused checks passed: **90 deterministic domain cases with solo enabled and disabled**, **28 compiled fixed-request checks** (valid/invalid/truncated/shared-stream sentinel cases), and actual ModSources compile/package with zero warnings/errors,31,030,224bytes. The installed assembly's solo flag is enabled. Protocol/layout and combat DTO are unchanged; the preceding v14 full combat-codec results remain applicable. [The check record](../evidence/2026-09-06-solo-stack-sync-checks.json) owns final static/deployment evidence. **0.2.15 game load, solo actual start, two-client Stack alignment, field/flight/recovery cleanup and dedicated-server latency checks remain not_run / user-owned.** No game, server or GUI was launched.

### Inspected 0.2.14 Host & Play session

Two two-player fights ended AllParticipantsDowned after43.95s/43.45s. Initial Stack was1/2 in both, with participant0 at40.3/22.8px and participant1 at669.4/710.3px from the112px marker. The first relay Stack was0/2 in both and caused the final Down events. All four Stack resolutions failed; both Spread resolutions passed. Six Down events and two successful instant revives were recorded; each revive set the recipient deadline exactly3,600ticks later. Host-client normal Defeat death was logged for both fights. No battle exception occurred during either fight.

Each fight cleared both300,000-HP Pylons in7.45s/6.95s (party effective DPS80,537/86,331). The18-second Core exposures dealt692,773/798,232 (38,487/44,346DPS), leaving86.14%/84.04% of5,000,000HP. Neither reached PhaseII. Do not retune HP from these movement-disputed/down-heavy attempts.

Thirteen one-member starts reached the second gate and were rejected with `first_severance.roster_too_small`: the initial scan used the debug flag but combat revalidation did not. Subsequent repeated activations also produced seven rate-limit warnings and one logged tML `Read underflow31of43bytes` warning from the unread12-byte request payload. The latter occurred after both fights and is separate from the Stack complaint. Raw logs/names/world paths remain outside the repository; [sanitized evidence](../evidence/2026-09-06-solo-stack-sync-checks.json) retains the measurements.

### Preceding build 0.2.14

Development **0.2.14 / protocol v14** introduced a default-enabled build-time solo-debug admission flag and participant-only Victory/Defeat HUD cinematics. Its intended solo start was incomplete because of the second-scan bug documented above. It reuses the two-player HP/Pylon workload and existing phase score; one Down takes ordinary immediate AllParticipantsDowned/Defeat. There is no NPC, invulnerability, fake second participant, public toggle, saved setting or GUI automation. [ADR-0020](../adr/0020-development-solo-admission-and-terminal-hud.md), the [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md#one-member-development-start-and-results--0214), [visual spec](../encounters/first-severance/VISUAL_SPEC.md#success-and-failure-designations--0214) and [one-client runbook](../runbooks/SINGLE_OPERATOR_TESTING.md#0-最短の一窓デバッグ) own that contract and procedure.

Focused results: **88 domain cases passed with the build flag enabled and disabled; 304 compiled codec round trips and46 malformed/truncated cases passed**, now including one-member preparation/Ready and all scores. Reflection confirmed the installed Mod assembly has solo enabled and the separate disabled harness has it off. Actual ModSources build/package passed with zero warnings/errors,31,028,948 bytes. A new test initially referenced the wrong runtime-update property, corrected before these passing runs. No distributable assets or dependencies changed. [The check record](../evidence/2026-09-06-solo-debug-results-checks.json) records final static/deployment results.

Those automated checks missed the live second-scan call site. Runtime was not_run at the original handoff; subsequent game load, failed solo activation and two-player outcomes are recorded above. Terminal camera/HUD appearance remains unverified. Public-release packaging must disable the flag and verify one-player rejection; see [Release Process](../RELEASE_PROCESS.md).

### Preceding build 0.2.13

Development **0.2.13 / protocol v13** adds wider Spread, clockwise fixed-site Stack relays, first-cycle HP gates, the Phase-II rotating blade, remote-hand Phase III and an explicit HP-zero Final survival score. The shell is smaller and uses new original high-detail art; new phase effects, Phase-III/accelerating-Final music and a point-collapse Victory ending accompany it. The [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md#ordered-phase-scores-and-terminal-survival--0213), [visual spec](../encounters/first-severance/VISUAL_SPEC.md), [audio cues](../AUDIO_CUE_SHEET.md) and [ADR-0019](../adr/0019-phase-scores-and-terminal-survival.md) own behavior and tuning. Frozen roster HP, defense, recovery, field dimensions, dependencies and Mod selections are unchanged.

The Raid development Skill kept all phase/HP/assignment/damage/cleanup decisions in the exact-Fight authority and drove focused movement/sequence/codec checks. Imagegen Skill supplied an original1254x1254 RGBA shell, copied unedited with alpha preserved and consumed through client-only masks. No new game actors, client health/outcome requests, saved fields, public debug grant or persistent input/UI flags were introduced. All peers need the matching protocol13 package.

Focused results: **84 deterministic domain cases passed; 222 compiled codec round trips and40 malformed/truncated cases passed**. Actual ModSources compilation/package passed with **zero warnings/errors**,31,026,406 bytes. One initial compile found a UI local-name collision, corrected before the successful build; one old enum-inventory assertion was updated for the new actions before the passing domain run. Two new music files and12 new/replaced effects decode as finite, unclipped samples with external PCM24 auditions. The encoder's initial large-buffer failure was corrected using bounded Vorbis writes. The final static/deployment state is recorded in [the check record](../evidence/2026-09-06-phase-scores-checks.json).

**0.2.13 game load, all-phase multiplayer play, actual movement/latency margins, remote-hand/depth quality, shell/camera/readability, live mix, cancel/failure cleanup and remote NPC HP-zero behavior are not_run / user-owned.** No game/server/GUI was launched. A compile, synthetic movement estimate or waveform check is not audiovisual or multiplayer acceptance.

### Confirmed preceding 0.2.12 two-player pull

The latest inspected preceding pull won in88.92s, with Core5,000,000 and Pylons300,000 each. Core effective DPS was110,660 over45.18 damageable seconds; Phase-I/II rates116,732/105,189. Pylon checks cleared in4.67s and6.07s. Both Stack resolutions passed2/2 inside112px with no Stack damage; no Spread overlap damage was logged. The half-HP transition lasted6s, and twelve grids fired, with Core salvos from the third and2→1 beams after one member became Downed. Two Down events and one successful instant revive occurred; the second Down was inside the recorded recipient60-second lockout. No battle exception was observed. These are authority outcomes, not proof that both screens matched or that the user approved the prior visuals. Raw logs/names remain external.

### Preceding build 0.2.12

Development **0.2.12 / protocol v12** adds forgiving frozen-roster HP, a single valid Stack circle with exterior arrows, connected limb-first eclosion, Core-origin lasers from Phase-II grid three, stronger original pressure/plasma audio and a short client-only Victory ending. The [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md#forgiving-roster-hp-and-core-salvos--0212), [visual spec](../encounters/first-severance/VISUAL_SPEC.md), [audio cues](../AUDIO_CUE_SHEET.md) and [ADR-0018](../adr/0018-roster-health-and-core-salvos.md) own exact details. Music, defense, recovery, field dimensions and Mod selections are unchanged.

The Raid development Skill kept targeting, HP thresholds and shared damage ledgers on authority, and eclosion/terminal effects on clients; the source-research Skill narrowly checked pinned NPC extra-AI semantics. The bounded NPC-health record and v12 direction payload require matching packages across peers. No saved field, client health request, persistent input flag or server graphics dependency was added.

Focused checks passed: **81 deterministic domain cases**, **84 compiled codec round trips and 24 malformed/truncated cases**, and actual ModSources compile/package with **zero warnings/errors**. One initial rig-continuity check exposed an excessively fast elbow sweep; the folded wrist path was improved and the affected domain command passed. A final draw-only fold/tail adjustment was then rebuilt; protocol/domain logic did not change. Five original audio exports are finite and below full scale, with external individual/stem/composite auditions. Final package hashes, deployment and scoped static results are in [the check record](../evidence/2026-09-06-eclosion-salvos-checks.json).

**0.2.12 game/client load, multiplayer HP-bar timing, eclosion/ending quality, camera/mask alignment and live audio are not_run / user-owned.** No game/server/GUI action was taken. Compilation and curve tests are not visual acceptance.

### Confirmed preceding 0.2.11 three-player pulls

Three authority-recorded pulls ended after **90.32s Defeat**, **72.80s Defeat**, and **44.77s Victory**. The clear consumed the fixed 4,000,000 Core HP over 20.05 damageable seconds (**199,501 DPS**), and the 750,000-HP Pylon pool in 3.22 seconds (**233,161 DPS**). Phase-I/II effective Core rates were **188,976 / 211,268 DPS**. The final phase lasted only 9.47 seconds: four grid fires occurred, and the fifth warning was cancelled by Victory. No Down/revive occurred in that clear. This supports more HP for three players; it does not prove a universal per-extra-player multiplier.

Across the three pulls, all five completed Pylon checks cleared; the final partial check of the first wipe was interrupted. All five Stack checks were incomplete: **1/3, 2/3; 2/3, 1/3; 2/3**. Authority distances agree with their 112px acceptance checks; one excluded survivor was only 3.9px outside. Logs do not establish peer-side marker alignment. Spread recorded no overlap damage. The two wipes had 10 Down events, four accepted instantaneous revives in their request ticks, and two eliminations in total. One revived recipient was Downed again before their 60-second lockout expired, then reached the 30-second Down timeout; that is a current-rule consequence, not evidence of failed item transport.

No Convergence battle exception was present in the inspected intervals. Startup warnings from other Mods and a before-listen socket error are not attributed to Raid HP/synchronization. [Sanitized telemetry](../evidence/2026-09-06-eclosion-salvos-checks.json) retains per-pull windows/attendance; raw logs, player names, world paths and a detailed local report stay outside the repository. The user rejected the prior shell/readability/audio treatment, so these authority outcomes are not art/audio approval.

### Preceding build 0.2.11

Development **0.2.11 / protocol v11** adds a sealed Phase-I Boss shell, a protected half-HP cinematic rupture and an Unbound Phase-II lattice attack module. Stack now uses a fixed world-space gathering point above the Boss; Spread/recovery and current HP/defense remain unchanged. The field halves in both dimensions with an opaque black exterior; thicker colored warnings, stronger original SFX and a faster original Phase-II score accompany the new phases. [Encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md#boss-stages-and-fixed-assembly--0211), [visual spec](../encounters/first-severance/VISUAL_SPEC.md), [audio cues](../AUDIO_CUE_SHEET.md) and [ADR-0017](../adr/0017-boss-stages-fixed-stack-and-lattice.md) own those facts.

The Raid development Skill guided authority/DTO/cleanup separation and focused checks; the source-research Skill was used only for the pinned NPC death and sound-limit hooks. The exact-Fight server/SP runtime chooses the stage, fixed Stack attendance and grid hits. Clients render bounded snapshots and conditional camera/HUD layers without persistent control flags. Packet IDs/requests and saved data are unchanged, but all peers must update together for protocol v11.

Focused checks passed: **77 deterministic domain cases**, **60 compiled codec round trips for 2/3/4 participants and 19 malformed/truncated cases**, and the actual ModSources build/package with **zero warnings/errors (23,016,178 bytes)**. The normal profile and four existing local test profiles have matching new packages; the old package is retained externally and other Mods are untouched. Seventeen new/remixed audio exports decode as finite, unclipped samples; PCM loop-boundary checks are not listening approval. The final scoped static wrapper result is recorded in the [check record](../evidence/2026-09-06-boss-stages-checks.json).

GUI/game/server launch, multiplayer phase-NPC behavior, camera/zoom/mask alignment, animation quality and live mix are **not_run / user-owned** for 0.2.11. No in-game verification is claimed from a successful build or mathematical curves.

### Confirmed preceding 0.2.10 pull

The 2026-09-06 two-player authority log records a **109.43-second Victory / BossLifeZero**, all three Pylon checks cleared, and 4,000,000 effective Core damage. Core damageable time was 52.43s (party effective DPS 76,287.3); Pylons took 1,500,000 damage over 18.50s (81,081.1 DPS). Stack attendance was **1/2, 2/2, 1/2**: one success and two failures. No Spread overlap hit occurred. One charge caused Down; one accepted instantaneous revive followed 7.7s later with the recipient's 60-second lockout. This validates that pull's authority logs/HP calibration and recovery, not universal Stack synchronization or final balance.

No Convergence battle exception was present. Two caught `ObjectDisposedException` entries occurred later during player disconnect in Terraria's `NetMessage.CompressTileBlock` / `SendSection` MemoryStream path; this does not identify Convergence as their cause. A preceding client connection attempt also occurred before the server was listening. Raw logs and the local report remain outside the repository; sanitized aggregates are in the new check record.

### Preceding HP calibration — 0.2.10

Development **0.2.10 / protocol v10** recalibrates Boss/Core and Pylon HP using the observed two-player effective DPS, and adds passive authority damage-window telemetry. Defense, attack damage, timing, count/scaling policy, recovery and visuals remain unchanged. The [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md#provisional-hp-calibration-and-authority-diagnostics--0210) owns exact tuning and diagnostic field meanings. Telemetry is owned/cleared by the existing exact-Fight combat runtime; no client requests, replica fields, saved state or gameplay outcomes are added.

Focused domain checks passed: 72 cases, including shield-time exclusion, deadline/overkill bounds, 2/3/4-Pylon pool rates, recent versus whole-window DPS and persistent Core HP between exposures. The actual ModSources build/package passed with zero warnings/errors (21,011,111 bytes). The [check record](../evidence/2026-09-06-hp-telemetry-checks.json) records the final static wrapper and deployment results. Runtime HP/readability and terminal log checks are `not_run`, user-owned; no game/server was launched.

Calibration evidence from the preceding 0.2.9 two-player Host & Play log: one pull survived without Downed events, completed both Stack checks, but failed three Pylon checks and ended at Overload 3. The Core lost 2,113,652 HP over two 12-second penalized exposures. This supports lowering the excessive HP, not a general balance or multiplayer synchronization certification.

### Preceding fluorescent-beam pass — 0.2.9

Development **0.2.9 / protocol v10** brightens fixed beams to fluorescent plasma with a white inner spine, replaces dashed warning lines with continuous contrast rails and converging lock glyphs, and strengthens smooth anticipation at emitters, Boss, Stack/Spread markers and remaining Pylons. Fixed beam damage is now 120 HP; charges, Raid-share penalties, fast intra-combo cadence, category rests and recovery are unchanged. The dark Boss/background remain. [Encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md) owns damage; [visual spec](../encounters/first-severance/VISUAL_SPEC.md) owns appearance. No protocol/authority/persistence change or new image/audio asset is introduced.

Focused checks: 70 deterministic domain cases passed, including HP-independent beam damage and bounded continuous anticipation. The actual ModSources build/package passed with zero warnings/errors (21,005,951 bytes); the normal and four local profiles have matching packages. The scoped static wrapper passed once (51 indexed documents, 260 files, 10 YAML files). See [check record](../evidence/2026-09-06-fluorescent-beams-checks.json).

GUI/reload and the two-player smoke are user-owned and `not_run`; no game/server was launched. At the user's request the normal world's 20-Mod selection, including Ore Excavator 0.8.9 and CalamityHunt 1.2.3, is now the local testing selection too; all user-added Mods are preserved. The prior normal-profile client content load confirms those Mods were present, not compatibility with the new Convergence build. This is not a new compatibility certification.

### Preceding continuous-emission pass — 0.2.8

Development **0.2.8 / protocol v10** connects continuous attack emitters, original high-resolution emission/rig/architecture atlases, a ground-anchored 12x4 foundation, a physically confining participant-only field and Raid-only infinite flight. The authority keeps the validated 320x140 layout and rechecks it at all-Ready. Old 2x2 Cores retain their saved identity; mining/replacing uses the larger foundation. Boss/Pylons now occupy the airborne field center. The safe designation/deployment intro is six seconds; fast intra-combo cadence, other major-phase rests, stats and recovery rules are retained. [Encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md), [visual spec](../encounters/first-severance/VISUAL_SPEC.md) and [ADR-0016](../adr/0016-ground-containment-and-continuous-emission.md) own behavior and architecture.

The previous four-tick tracking presentation and abrupt pose/beam switches are replaced by harmless-windup interpolation, a fractional visual clock, connected parent/child bones and shared opening/gathering/emission/cooling envelopes. Up to four casts retain bounded harmless tails; actual warning/live rails remain tied to authority geometry. This is an implemented presentation pass, **not** evidence that WotG-equivalent visual quality or dual-client performance is achieved. [F11 research](../research/WOTG_RAID_BENCHMARK.md) records the pinned source influence without importing external code/assets.

Focused checks: 68 domain cases passed, including continuity/field-body bounds and strict airspace validation. Compiled v10 codec passed 42 round trips and 11 malformed/truncated cases. Actual ModSources compiled/packaged with zero warnings/errors (21,002,047-byte package). Dedicated Server loaded the copied world and started listening; both real clients loaded the matching package and reached their menu. Simple Whip Addon 1.15.12 is present in all three profiles. The requested launch handoff does not join characters, arm an auxiliary permit, auto-Ready or start a Raid. Latest operator player data is copied into the isolated profile with its previous copy backed up; originals remain unchanged. See [focused check record](../evidence/2026-09-06-continuous-emission-checks.json).

Runtime field/flight/legacy-placement, live animation readability and equipment/latency/cleanup interactions remain **not_run / user-owned**. Startup retains the prior missing optional icon_small warning and third-party asset-load timing warnings; inspected startup logs contain no error-level exception. A load-to-menu is not a combat or rendering-quality pass.

### Preceding fast-combo pass — 0.2.7

Development **0.2.7 / protocol v9** restores the earlier fast cadence inside each attack combo: Pursuit Prism starts every 42 ticks instead of 96; Dash–Stillness steps start every 72 instead of 156. The intro, Pylon opening cue, four-second Stack/Spread windows, exposure opening rest, extra full-combo rest and two-second Reset are unchanged. The shorter charge windup retains the 24-tick harmless locked hold; damage never starts before FireTick. Hit geometry, travel speed, stats, recovery and auxiliary permissions are unchanged. Current numbers live in the [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md).

Focused checks: 66 domain cases passed, including actual-volley/scheduler deadline agreement and unchanged major-phase timing. Compiled v9 codec passed 42 round trips and 11 malformed/truncated cases. The actual ModSources package built with zero warnings/errors. The static wrapper result and explicit remaining runtime checks are recorded in the [check record](../evidence/2026-09-06-fast-combo-checks.json). GUI/game launch and multiplayer smoke are user-owned and `not_run` for 0.2.7; all peers need the new timing-compatible package.

Animation diagnosis (not repaired in this tuning pass): authority samples the harmless tracking orbit every four ticks, and `ChargeHeadAt`/`DrawEnergyBody` draw the last received windup position without interpolation, so the body can visibly step at approximately 15 Hz despite a higher render rate. Separately, `FirstSeveranceBossVisuals.Draw` switches casting pose from fully raised to zero on FireTick while recoil jumps from zero to full; its three rigid texture slices have discontinuous transforms rather than a continuous cast/fire/recovery animation. These are concrete presentation gaps, not a conclusion that developing content must be jerky. Existing logs have no frame-time evidence to establish or exclude additional FPS/dual-client load. The intentional stationary prelaunch hold is distinct from these issues. Future smoothing must remain client-only and preserve exact lock/fire/collision cues.

### Preceding auxiliary-client build and staging — 0.2.6

Development **0.2.6 / protocol v8** adds opt-in, Dedicated Server console-only, one-Fight protection for one real auxiliary client. Normal play has no grant command or persisted character permission. Manual Core activation/Ready and the 2–4-player Raid rules remain unchanged. [ADR-0015](../adr/0015-console-only-single-pull-assist.md) owns authority/cleanup; the [single-operator runbook](../runbooks/SINGLE_OPERATOR_TESTING.md) owns usage. User GUI plus a friend remains the default; the user explicitly authorized Computer Use and one-person staging for this session only.

The isolated local server and two real clients loaded the matching package and joined successfully. Separate A/B/server save profiles use copies, leaving original player/world files untouched. Both are at the existing Core. Console confirmed the exact auxiliary binding was claimed by preparation; the auxiliary's GUI confirmed Ready **1/2**. Its hotbar slot 8 holds a Resuscitation Kit. Both external Cheat Sheet God Mode tooltips were confirmed Disabled after staging. The operator is left unready for the final Core click; combat has not been started by the agent. This is not a damage-protection, revive or combat-clear result.

Focused checks: 65 domain cases passed (including three new permit/identity/lease groups); compiled v8 codec passed 42 round trips over 2/3/4-member records and 11 malformed/bounds cases; actual ModSources build/package passed with zero warnings/errors. The scoped static wrapper passed (50 indexed documents, 247 repository files, 10 YAML files). Runtime damage suppression, owner-client TTL/Defeat cleanup, remote latency and friend-side behavior remain `not_run` for this build.

Staging observations: simultaneous client startup produced one shared `steam_appid.txt` file-in-use failure; sequential startup succeeded. Existing eclipse enemies caused ordinary deaths in copied saves; temporary external God Mode was used during positioning, then disabled on both clients. Two caught Calamity `SepulcherMinion` null-reference exceptions (charging/darts) appeared, without established Convergence causality. The oversized Core visual is offset from its actual 2x2 ground interaction tile in the observed rendering; that existing visual alignment issue is not fixed by this build. No raw logs, saves or personal paths are committed.

Subsequent user-run log observation (2026-09-06, 12:36:56–12:37:31): two real clients entered a new Fight after an earlier `roster_too_small` rejection. The previous one-use auxiliary permit was bound to the expired preparation, and no new `DebugAssistBound` appeared; the auxiliary took Pylon/charge damage and was not protected in this pull. Full 2/2 Stack cost zero HP; no Spread damage was recorded. The operator Downed to Stillness, then the auxiliary Downed to SweepLeft, causing `AllParticipantsDowned`. Both client logs reported `DefeatDeathApplied dead=True`. No revive request/success was logged. This confirms ordinary two-client Defeat execution in that unprotected pull, not assisted invulnerability or revival. A caught third-party gold-critter/Lighting exception preceded combat; no uncaught Convergence runtime exception was observed in the inspected combat interval.

### Preceding audiovisual build — 0.2.5

Development `0.2.5` replaces the rejected Ninth/chiptune with the original **Obsidian Liturgy** orchestral-textural sketch and 18 newly rendered action cues. The user can audition external BGM/individual WAVs/a timestamped SFX reel without launching the game. New original high-resolution basalt/ivory Boss, cathedral sky and void-lance textures accompany a restrained bone/bronze palette, fine rupture filaments and a much larger casting pose. [Visual spec](../encounters/first-severance/VISUAL_SPEC.md) and [Audio cues](../AUDIO_CUE_SHEET.md) own appearance and sound.

Longer warnings and between-action pauses are in the [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md). A charge locks its position/heading for 24 harmless ticks before firing; this removes the pre-lock hits seen in the preceding session. Fast live travel, HP/damage, free successful Stack/Spread, reusable revival and exact-Fight cleanup are retained. The unchanged attack DTO has protocol **v7** because both peers derive collision deadlines from shared tuning; v6 peers must update. [ADR-0014](../adr/0014-deliberate-anticipation-and-solemn-presentation.md) supersedes the old live-steering/presentation choice.

The [0.2.5 check record](../evidence/2026-09-06-solemn-pass-checks.json) owns automated results and explicit remaining checks. No GUI/game launch is performed: the user owns reload/Host & Play and audiovisual acceptance. Source/asset licenses remain undecided for release.

The preceding 0.2.4 two-player session loaded the hotfix, entered the world and ran six pulls; the old spawn exception was absent. All six ended AllParticipantsDowned. Two full 2/2 Stacks had no damage; no Spread damage or successful revive was logged. Charge hits preceded direction lock by 2–4 ticks in the comparable casts. One caught Calamity Sepulcher AI exception occurred. These observations do not establish 0.2.5 behavior or friend-side rendering.

### Energy and audiovisual pass — 0.2.3

- The exact-Fight authority swings one body behind/overhead, briefly steers its fast launch, then freezes the heading. Compact swept collision prevents tunnelling; the long wake is cosmetic. No per-player projectile multiplication. [Encounter Spec](../encounters/first-severance/ENCOUNTER_SPEC.md) owns tuning; [ADR-0013](../adr/0013-energy-charge-and-client-feedback.md) owns motion replication/cleanup.
- Stack/Spread gain distinct gathering/dispersing sounds, final countdown accents and committed success/failure bursts. Participant cues no longer fade with distance from the giant Boss. Down/revive, warning/fire, Pylon break, exposure, intro and terminal outcomes have dedicated synthesized audio. [Audio Cue Sheet](../AUDIO_CUE_SHEET.md) owns the mapping and [Attribution](../../Assets/ATTRIBUTION.md) separates all music rights/source layers.
- The original black occultation, broken corona and far pillars render as a client sky, without changing world weather/time or overlaying terrain. Reduced Effects and independent shake settings remain supported. Audio voices, temporary feedback and sky are reset on World/Mod unload.
- The preceding 0.2.2 two-player logs showed four AllParticipantsDowned defeats; complete 2/2 Stack passed with no damage, separated Spread advanced without damage, and one instant revive carried the exact +3,600-tick deadline. Host normal Defeat death was logged four times. Friend-side death execution and audiovisual quality were not proven by those logs. Two caught Calamity Sepulcher projectile exceptions also appeared; no Convergence runtime exception was observed in that session.

### Historical illustrated attacks and Raid consequences — 0.2.2

- Pylon windows use predicted angled beams in the requested red/blue/green/yellow return order. Exposure uses right-dash/stop/left-dash/stop with sweeping blades and overhead curtains around an empty column. Origins are independent of the Boss; its casting structures animate and recoil. [Encounter Spec](../encounters/first-severance/ENCOUNTER_SPEC.md) owns the precise geometry/timing.
- Full frozen-roster Stack attendance and non-overlapping Spread cost zero HP. Incomplete Stack deals each standing participant the missing-roster fraction of their own maximum HP, including those outside the circle; two players with one missing is 50%, four with one missing is 25%. Required attendance is now 2/3/4, not 2/2/3.
- Accepted exact-Fight Defeat orders ordinary death for all connected participants. Outsiders and other endings are excluded. Normal character-difficulty penalties apply, including Hardcore character loss. Owner execution and external death-cancelling hooks are logged; actual multiplayer death/respawn is not yet verified.
- The 1.5-second intro temporarily skips normal HUD drawing and displays the Raid/Boss names, dark monochrome framing, an ominous vanilla cue and optional camera shake. It does not change persistent UI settings or stop gameplay. No new raster/audio asset or extra world actor was added.
- Protocol v5 adds bounded pattern/step/target/ray dimensions. Both peers must update together. The reusable instant kit, recipient-only lockout and excessive Boss/Pylon stats remain unchanged. [ADR-0012](../adr/0012-pattern-sequences-and-defeat-death.md) records the authority/compatibility changes.
- 61 domain cases passed, including five targeted groups for this change. Compiled v5 codec passed 42 absent/pattern cases over 2/3/4-player records and seven malformed/truncation cases. Actual build/package and client reload-by-restart passed after the initial live-file lock was released. Static completion results are recorded in [the focused check record](../evidence/2026-09-06-pattern-sequences-checks.json).
- The user's updated Skills/document instructions were recovered from their edited working copy and retained. Verification is scoped to changed behavior; the only agent-run game launch was the user's subsequently requested load-to-menu handoff. No agent-run world/combat session or release matrix was added.

### Confirmed preceding 0.2.1 Host & Play log

The inspected 2026-09-06 server/client logs loaded `0.2.1` and contain four two-player attempts. Four instant revivals succeeded, each publishing a recipient deadline exactly 3,600 ticks later; the two players also revived each other, confirming a recipient's lockout does not block rescuing someone else. All four attempts ended in `Defeat / AllParticipantsDowned`; the final observed survivor took 786 Stack damage at 124 HP. This informed the requested Stack redesign. Exact lockout-expiry eligibility, rejected repeat revival, and the new `0.2.2` behavior are not runtime-confirmed. Caught Simple Whip Addon and Calamity projectile exceptions were present; causality with Convergence was not established. No raw logs or player identities are committed.

### Historical instant revival and high-intensity pass — 0.2.1

- An Alive participant uses the reusable Resuscitation Kit within 8 tiles of the nearest eligible Downed ally. Authority revalidates after this tick's damage and resolves the stable request batch instantly: no held channel, movement interruption, shared token or item consumption. HP returns to 35% with 3 seconds of protection.
- The recipient cannot receive another revival for 3,600 ticks / 60 seconds. Down does not clear the deadline; the visible non-cancellable debuff reflects server-owned state. The recipient may still revive someone else. Fight cleanup clears it. Down still expires after 30 seconds, so a second Down while locked may become unrecoverable.
- Kit requests send ordinary inventory/control synchronization first; wrong-item/alive/range/lockout failures now have distinct localized messages. This is not yet proof that every multiplayer item-switch race is fixed. Protocol v4 appends a bounded recipient deadline to each participant projection; both players need this build. See [ADR-0011](../adr/0011-instant-revival-and-recipient-lockout.md).
- Foundation Core has a new original 176-world-pixel-tall visual canvas and placement preview. The existing 2x2 Tile/TE, saved coordinates and right-click base remain unchanged; no replacement/migration is required. Dedicated Server never loads the image.
- Boss HP/defense: **60,000,000 / 240**. Each Pylon: **1,000,000 / 120**. Stack pool: 140% of average pull maximum HP; failed Spread: 70% maximum HP; failed Pylon pulse: 35%, clamped nonlethal; lance: 60%. The enormous decorative body still has no contact damage.
- Intro 1.5 s, Pylon cue 0.5 s plus 8 s active, Stack/Spread 2.25 s each, exposure 10 s or penalized 5 s, Reset 0.5 s. Lances warn for 42 ticks / 0.7 s, fire for 14 ticks, and repeat every 66 ticks / 1.1 s. The two-ray cap and exact locked 88-pixel damage corridor are unchanged. The existing eight-exposure/three-Overload caps may make this intentional over-tuning impractical to clear.
- Faster casing opening/orbits, recoil, exposure shock rings, brighter charge/fire, speed streaks, layered vanilla impacts and short stronger camera kicks replace the softer motion. Camera Shake OFF and Reduced Effects remain available without hiding danger information. There is no full-screen white flash or gameplay hit-stop.
- At delivery: 56 domain tests passed, including five focused instant-revival/lockout/race/cleanup cases; actual ModSources `0.2.1` packaged with 0 warnings/errors. The later user-run Host & Play log observation is recorded above; the agent did not operate that game session.

### Historical giant Boss and observation lances — 0.2.0

The following records the preceding build; `0.2.1` values and recovery rules above supersede its timings, damage, protocol and channel behavior.

- Original generated black-ice/ceramic containment body, approximately 820 pixels tall at full reveal, with a roughly 1,100-pixel orbital apparatus. Side structures open on exposure; broken seals, faint aurora, drifting shards, core brackets, charge/fire effects and terminal dissipation are client-only. The body source is RGBA with a transparent exterior, not extracted artwork.
- One stationary authority-owned Boss NPC remains. Its 144x144 central aperture is the only Boss hitbox, 360 pixels above the Foundation Core. Decorative mass has no contact damage. Boss/Pylon HP and the six-state loop are unchanged. Pylons use larger original code-drawn containment cages and wider placement.
- Stack still shares its experimental pool within 7 tiles. Spread now shows 14-tile-radius circles and requires 28-tile center separation. Circle boundaries and server checks share the same tuning constants.
- Pylon/Exposure phases aim at the current Alive roster, lock the observed positions at cast start, show the complete 88-pixel-wide danger corridor for 72 ticks, then fire for 18 ticks. Single/double shots alternate every 102 ticks (1.7 s), capped at two rays regardless of party size. No beams run during intro, Stack, Spread or Reset; phase exit cancels the current volley.
- Each volley hits each participant at most once, for experimental 35% maximum HP. Downed players are excluded and post-revive immunity is honored. Lethal lance damage enters the existing Raid Down/revive path, not ordinary Terraria death hooks. Hits interrupt revival through the existing HP observation. No terrain is destroyed and outsiders are not damaged by this custom attack.
- Protocol v3 appends an optional bounded volley to full combat snapshots: serial, start tick and at most two finite unit rays. The server publishes assignments immediately; clients do not choose aim, timing or hits. Packet IDs and all client request shapes remain unchanged. Both players must update to `0.2.0`; v2 clients are intentionally incompatible. [ADR-0010](../adr/0010-giant-boss-observation-lances.md) records this development expansion.
- Client settings offer reduced decoration and camera-shake disablement without removing danger indicators. Vanilla Boss 3 remains the BGM; vanilla sound IDs supply temporary charge/fire cues. No music/SFX recording is bundled.
- Source/asset review and focused geometry/timing/immutability tests plus the actual tModLoader command build are this iteration's checks. The new in-game size, hit readability, 2-client aim synchronization and frame cost are user-run checks, not inferred from compilation.
- Completed checks: all 51 deterministic domain tests passed; the actual compiled combat codec round-tripped absent/single/double volleys from a shared buffer, preserving locked rays and consuming the exact payload only; actual ModSources `0.2.0` packaged with 0 warnings/errors. No game GUI or live server was operated for this pass.

### Confirmed 0.1.2 targeted playtest

The user reported readable shrinking circles. The two-player Host & Play authority log confirms a Down, an interrupted first attempt, and then a successful revive from tick 4745 to 4865 (exactly 120 ticks). A later second Down expired after 1,800 ticks with the shared token consumed, correctly ending as `Defeat / RecoveryImpossible`. No recovery payload-size rejection, read underflow or Convergence runtime exception appeared in that inspected run. This is not a `0.1.2` clear claim; the earlier `0.1.1` clear remains user-reported.

### Playtest hotfix 0.1.2

- `0.1.1` server logs confirmed repeated `first_severance.revive_payload_size` rejection with `Read underflow 31 of 35 bytes`. Down and revive readers incorrectly treated the shared receive stream's remaining length as the packet payload size. Both now read exactly one uint nonce; header/sender/rate checks follow decoding. IDs, payloads and protocol version 2 are unchanged.
- The screenshot shows screen-length orange spokes during **Spread**, not a Down attack. Circle segments were stretching the full MagicPixel texture; drawing now samples an explicit 1x1 source rectangle. Rings and Down particles are presentation only. Spread still deals damage once to players closer than 16 tiles at resolution; damage tuning is unchanged.
- Authority logs now record phase transitions, damage source/life, Down/revive/cancellation/timeout events and exact terminal cause. No player names, addresses, positions or per-frame records are added. Diagnostics cannot interrupt cleanup or domain transitions.
- The client displays the localized ending cause in chat and for 10 seconds on the HUD, with more prominent Down instructions and a separate eliminated state. All-Downed still ends immediately; it does not wait for the 30-second personal deadline.
- Focused compiled-code checks reproduced both old shared-buffer failures and passed the fixed standalone/shared-buffer, zero-nonce and truncated-input cases. The subsequent user render confirmation and logged held-use revival are recorded above.
- Separate `0.1.1` logs also showed caught exceptions in Simple Whip `GoldRush_Shot` and Calamity `SepulcherMinion`. Their relationship to this fight is unproven; no third-party code or enabled-Mod settings were changed.

### Historical initial development combat experiment — 0.1.1

These are the initial implementation values, not the current balance or recovery contract. See the `0.2.1` section and current encounter/revival specs for active rules.

- Authority composes the existing loop and revive domains after Ready; Boss/Pylons are tracked by NPC slot/type and a per-Fight token. Observed `OnKill`, not arbitrary disappearance, supplies actor deaths. Normal NPC hit gating is provisional for cooperative clients, not a verified anti-cheat boundary.
- `SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset` runs with the existing timers. Boss HP is 1,200,000 and each Pylon has 25,000 HP for this experiment. Only exposed Boss/current Pylons accept player hits.
- Stack follows a round-robin Alive target and shares a fixed experimental HP-damage pool (90% of the pull roster's average maximum HP). Spread hits each overlapping Alive participant once for 40% maximum HP. Failed Pylons pulse 25%, clamped nonlethal. These direct Raid-owned HP changes intentionally do not claim the unmeasured Terraria mitigation/death pipeline. Stack target reissue and a live Barrier remain deferred.
- `/convergence-down` requests the sender's experimental Downed state. Raid-owned damage can also Down a participant without sending lethal HP through ordinary death hooks. The shared service owns the 30-second Down deadline, 2-second channel, 1/2/3 tokens, 35% restored HP, and post-revive timers.
- Kit starts validate the current sender binding, Alive state, held item, range and available token. The server chooses the nearest unreserved Downed ally; channels check held use, movement, damage, hooks/mounts and range. Health corrections have a per-participant revision and are applied on the owning client as well as authority. Weakness reduces generic damage by 20% for 10 seconds.
- The initiator may use `/convergence-cancel` during combat. Victory, Defeat, cancel, Core/actor loss, unload and exceptions clean owned NPCs and player projections. Ordinary Terraria death or a roster disconnect aborts this experiment; reconnect/re-entry is not enabled. Cleanup restores incapacitated players to at least 35% HP.
- Protocol v2 introduced bounded Down/revive requests and a combat section; v3 extends it with the observation-lance volley above. No audio file is extracted or packaged.
- Foundation-smoke validation remains explicit for this development experiment because it does not edit World terrain, build a Barrier, or grant progression/rewards. Production arena and lethal-hook gates are not declared complete. See [ADR-0009](../adr/0009-development-combat-experiment.md).

### Implemented and connected

- tModLoader Mod source skeleton, feature registration, project metadata, Calamity hard reference for Stage A, and a Windows-confirmed compatibility-version policy.
- Generic encounter identifiers, definitions, catalog, lifecycle transitions, one-session authority coordinator, runtime/factory boundary, cleanup scope, retry backlog, and terminal snapshot outbox.
- Feature-neutral immutable termination descriptors carrying generic reason plus bounded schema/version/cause data; constructor-validated definition-owned mappings for World unload, internal failure, and fatal protocol failure; external preemption and replica/tombstone validation.
- Versioned packet envelope parsing, explicit packet IDs, direction guard, bounded rejection behavior, typed First Severance activate/Ready/cancel/snapshot handling, and initial/join snapshot delivery.
- Read-only encounter replica ordering by Encounter Sequence, revision, and authority tick, including terminal tombstones.
- Pure server/SP `Common/Raids/Revive` state machine: stable participants and connection epochs, Downed deadlines, batched revive arbitration, channel leases/nonces, token reservation/consumption, reconnect grace, same-tick wipe commit, bounded snapshots, projections, and exact-Fight cleanup.
- Dependency-free domain harness covering the revive domain, immutable arena objects and scan results, roster/Ready/Core lease boundaries, six-state loop policy/deadline behavior, Calamity gate-result invariants, terminal mapping, coordinator external endings, creation failure, and retained tombstones.
- Repository policy, YAML validation, CI, ADRs, provenance rules, and repository-local Raid/source-research Skills.

### Implemented First Severance foundation

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

### Not implemented

- Attack replay/preview controls, auto-rescue and NPC participant adapters. The real-client, console-authorized auxiliary is separate; see Current build and the [single-operator runbook](../runbooks/SINGLE_OPERATOR_TESTING.md).
- General outsider warning/ejection/admission, adversarial teleport handling and progression-call runtime instrumentation. Participant-only development containment is implemented above.
- Production normal-hit collector/mitigation, general projectile attacks, robust observer/rejoin support, and expanded multiplayer/latency verification. Player-carried Stack target reissue is no longer applicable to the current fixed-site design.
- General gameplay lethal-hit interception into Raid Down and Calamity self-revive coexistence. The developer-only protection uses a scoped `PreKill` cancellation, not the production lethal-to-Down adapter.
- Rewards, final hand-cleaned sprites/audio/VFX, final tuning, and release packaging. The new original giant-body/VFX pass remains provisional. Experimental UI is localized in English/Japanese.
- Optional local SQLite/FTS/embedding cache generator; the committed Markdown/front-matter catalog exists, but no binary-search database is built or required.
- Standalone progression/content that would replace the current hard Calamity dependency.

### Verification state

| Gate | State |
|---|---|
| Documentation/catalog, repository/Skills and YAML | 0.2.17 scoped result: [check record](../evidence/2026-09-07-recovery-tempo-checks.json) |
| Dependency-free domain tests | 0.2.17:93 passed, including untimed recovery, simultaneous Prism, new shared geometry and retained solo/Stack rules |
| Compiled preparation/combat codec | v16:308 round trips/48 malformed cases passed for1–4members, including untimed Down and larger Prism bound |
| `dotnet build ConvergenceMod.csproj` | Actual0.2.17 ModSources build/package passed, zero warnings/errors |
| Client load / Build + Reload equivalent | Preceding0.2.16 played by user;0.2.17 restart/load not_run |
| Single Player / one-client Host & Play | Repaired0.2.15 path retained; actual one-member combat-start evidence still unavailable |
| Steam-friend preparation | `0.1.1` logs confirm local Host & Play server and two joined players |
| Combat/BGM/Down/revive in game | User confirms preceding Stack issue resolved; new0.2.17 recovery/attacks/mix remain user-owned not_run |
| Dedicated Server load/2-client smoke | 0.2.8 server/world load passed; two-client joining/combat not_run |
| Calamity lethal-hook instrumentation | Not run; blocks the live Downed adapter |

The confirmed runtime is Terraria `1.4.4.9`, tModLoader stable `v2026.07.3.0`, Calamity `2.2.4`, and Calamity Music `2.1`. See the [sanitized Windows baseline](../evidence/2026-09-05-windows-baseline.json). The latest local development pack also loads Simple Whip Addon `1.15.12`, WingSlot Extra `1.4.5`, Cheat Sheet `0.7.8.1`, Magic Storage `0.7.0.11`, and its Serous Common Library `1.0.6.2` dependency. The subsequent `0.1.1` Host & Play observation above is separate from that earlier Dedicated Server baseline; it does not establish the full compatibility matrix.

### Current accepted direction

- Primary implementation/verification workstation: Windows desktop.
- MacBook: documentation, review, Git, lightweight domain edits; no runtime claim without the pinned game toolchain.
- First playable Raid: `First Severance` for 2–4 players after Exo Mechs and Supreme Calamitas; temporary build-gated one-player development checks are available, not a public solo rebalance.
- Active stages: I opening plus clockwise Stack relay; II lattice/Spread/twin rotating blades; III remote arms/half-field beams/Stack/Spread/central crush; explicit Final at HP0 with eight clockwise Stack/Spread/dodge stations. Each phase must finish its first full score before advancing; current thresholds50%/25%/0.
- Boss accepted boundary: one logical NPC/life pool, with user-requested giant original presentation. The one-body boundary does not limit visual size or decorative complexity.
- Recovery: untimed Raid-only Downed, no Eliminated; ally-used nonconsumed instant item, server-owned recipient-only60-second revival lockout, no shared tokens. All-Down remains immediate Defeat.
- Long term: ship/validate the Calamity addon first, then migrate through the accepted staged path toward a standalone Content Mod.

### Next change

User-owned Host & Play after restarting on matching0.2.17 packages: confirm Down can wait out the recipient lockout without Eliminated; observe simultaneous Prism targets, borderless forecasts, two faster sword revolutions, changing Phase-III safe strips and progressively faster/shifting Final. Listen to the less dominant effects versus raised music. Keep server/client logs for contact disagreement; observe ordinary end/cancel cleanup and Reduced Effects as convenient. Do not reopen accepted Stack synchronization without fresh evidence or demand a full release matrix for this short development check. Solo is opt-in; no extra client/NPC is needed. No game/server launch or other Mod/save change belongs to this pass.

Ordinary workflow remains user GUI plus a friend unless solo is explicitly requested. Art/audio acceptance remains human review; the research record does not claim unobserved video frames/audio were watched, nor that another broad test suite is required before this short observation.

## Snapshot: docs/encounters/first-severance/IMPLEMENTATION_PLAN.md

## First Severance Implementation Plan

### Immediate development experiment

Current pass is **0.2.17**: remove active Eliminated/Down expiry while retaining recipient lockout and all-Down Defeat; simultaneous all-standing-player opening Prism; two faster sword revolutions; short-forecast accelerating horizontal flood bands; borderless beam auras; shifting and progressively faster Final; lower SFX and slightly raised own BGM. Work order: domain recovery/geometry and bounded replica → client/audition mix → focused domain/codec/actual package → owning docs/catalog → user-owned Host & Play. [ADR-0021](../adr/0021-untimed-recovery-and-simultaneous-prism.md) owns structural changes, [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](../encounters/first-severance/REVIVE_SPEC.md) behavior, [Status](../STATUS.md) evidence. Preserve accepted Stack sync, HP, field size and other Mods/saves. No GUI/game/server launch. The paragraphs below are preceding work orders, not current requirements.

Preceding pass **0.2.16**: preserve the user-confirmed Stack sync and existing HP/recovery; make warning audio prominent, add rapid high-resolution twin-blade unsheathing/accelerated rotation, unify broad-beam anticipation, append lethal central hand crush to Phase III, and intensify Final beads/comb. Work order: deterministic geometry/authority → client materials/audio → focused domain and compiled codec/build → owning docs/catalog → user-owned Host & Play. [Encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md), [visual spec](../encounters/first-severance/VISUAL_SPEC.md) and [audio cues](../AUDIO_CUE_SHEET.md) own behavior; [Status](../STATUS.md) owns results.

Current user-requested pass is **0.2.14**: build-gated single-client Host & Play activation with the unchanged multiplayer score, ordinary immediate Defeat on the sole participant's Down, and bounded success/failure HUD cinematics. Work order: feature admission and one-member runtime/replica seams → disposable result timeline and client layers → focused enabled/disabled domain and codec/build checks → owning docs and user-owned GUI handoff. [ADR-0020](../adr/0020-development-solo-admission-and-terminal-hud.md), [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md) and [visual spec](../encounters/first-severance/VISUAL_SPEC.md) own policy/behavior; [Status](../STATUS.md) owns evidence. No NPC, cheats, public configuration, new art/audio, save, Mod-list or GUI/server operation. Paragraphs below are preceding requests, not additional work.

Current user-requested pass is **0.2.13**: preserve the multiplayer Raid while adding wider Spread, four clockwise Stack relays, mandatory first-cycle phase HP gates, grid/Spread/sword Phase II, distant arms/half-field Phase III and an explicit HP-zero Final with eight Stack/Spread/dodge stations. Work order: pure stage score/geometry → exact-Fight runtime and bounded descriptor → client shell/arms/sword/cinematics/audio → focused domain/codec/actual package → owning docs/catalog. [ADR-0019](../adr/0019-phase-scores-and-terminal-survival.md), the [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md), [visual spec](../encounters/first-severance/VISUAL_SPEC.md) and [audio cues](../AUDIO_CUE_SHEET.md) own these decisions; [Status](../STATUS.md) owns actual results. No GUI/server/Mod-list/save change. Subsequent paragraphs are prior work orders, not added scope.

Current bounded request (`0.2.12`): use preceding two/three-player telemetry to freeze forgiving roster-scaled HP; give Stack one true circle with exterior arrows; replace radial shell breakup with connected eclosion; add every-standing-player Core lasers from grid three; strengthen pressure/high-pitched firing audio and create a disposable Victory ending. The [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md) owns mechanics/scaling, [visual spec](../encounters/first-severance/VISUAL_SPEC.md) owns emergence/readability, and [audio cue sheet](../AUDIO_CUE_SHEET.md) owns original effects. [ADR-0018](../adr/0018-roster-health-and-core-salvos.md) owns health/salvo replication and terminal presentation boundaries. Work order: authority/DTO and focused geometry/scaling checks → client rig/audio → actual package and compiled codec → final owning-document/catalog update. [Status](../STATUS.md) alone records completion and pending observations. No GUI, server launch or Mod-list change; playtesting belongs to the user plus friend. One-person operation remains opt-in. Chronological paragraphs below are prior work orders, not additional current scope.

The current user-requested pass is `0.2.1`: reusable instant revival with a recipient-only 60-second lockout and no token/item cost; a larger decorative Core preserving existing tile/TE identity; deliberately excessive stats and faster, more forceful attacks/VFX. [ADR-0011](../adr/0011-instant-revival-and-recipient-lockout.md) supersedes the channel/token parts of the older work order below. Verification is focused domain/codec checks, one matching tModLoader build, then the user's GUI reload/two-player loop. Production lethal hooks, rejoin and release gates remain deferred.

The next user-authorized pass is `0.2.0`: original giant Null Cantor body/rig, readable shield/exposure transformation, large Spread circles, and frequent locked-aim observation lances. [ADR-0010](../adr/0010-giant-boss-observation-lances.md) extends the experiment without pulling in multipart gameplay, rewards, dependency removal or ordinary lethal hooks. Implementation is feature-local ray assignments/collision plus client-only art/VFX. Verification is a focused geometry/timing/bounds run, repository checks, one actual packaged build, then a user-operated two-player loop; broad acceptance gates below remain future work.

The user requested one in-game combat loop, vanilla Boss music, and Down/revive after reaching Ready with a Steam friend. Development `0.1.1` therefore composes a bounded experimental adapter ahead of the production gates below. [ADR-0009](../adr/0009-development-combat-experiment.md) and the [Status](../STATUS.md) record its scope: temporary NPCs, no terrain/Barrier mutation, no ordinary lethal interception, no rewards, and no claim that Slices 3–7 are complete. GUI playtesting belongs to the user; verification for this iteration is limited to compilation, a focused existing domain run and the requested two-player experiment.

This plan takes the current inert `FirstSeverance` bootstrap to its first playable vertical slice without weakening server authority, cleanup, or multiplayer evidence. The identity rename is complete; activation remains denied until the slice that owns each required adapter can prove it safe.

### Non-negotiable boundaries

- Server or Single Player local authority owns lifecycle, roster, ticks, assignments, entity spawn, feature damage gates, committed actor life, Downed/Revive, victory, and cleanup.
- Clients send bounded intent and render read-only snapshots/events. No custom packet reports DPS, position-check success, revive completion, life, or player identity as truth.
- `Common` does not depend on `Content` or `Client`; feature behavior stays in `Content/Encounters/FirstSeverance`.
- Calamity access stays in `Common/Compatibility/Calamity` behind project-owned contracts.
- Every transient actor is registered to exact `FightId` ownership immediately and cleanup is idempotent.
- Terminal snapshot/outcome is committed before the active runtime and player projections are released.
- Existing numeric packet IDs are never renumbered during the feature rename.

The first slice assumes cooperative multiplayer with unmodified clients. Terraria/tModLoader supplies ordinary movement and combat facts observed by the server; custom-message authority is not a claim of anti-cheat against a modified client. The exact normal-hit and lethal-hook seams must be measured on the pinned runtime before their respective adapters are enabled.

### Ownership target

| Area | Responsibility |
|---|---|
| `Common/Encounters/Runtime` | generic lifecycle, exact-Fight ownership, update/cleanup contracts, terminal publication |
| `Common/Raids/Revive` | reusable pure Downed/Revive authority state machine |
| `Common/Networking` | envelope, sender/direction validation, bounded transport, generic replica plumbing |
| `Content/Encounters/FirstSeverance` | roster composition, arena adapter, loop executor, Boss/Pylons/mechanics, tuning, feature snapshot |
| `Client/Encounters/FirstSeverance` | markers, countdowns, Boss presentation, VFX/audio/accessibility from replicated state |
| `Common/Compatibility/Calamity` | progression and lethal-hook compatibility facts only |

Do not build a generic mechanic DSL for this first consumer.

### Slice 0 — Windows baseline and evidence

Status: **Complete (2026-09-05).** The confirmed versions and sanitized run are recorded in the [Version Matrix](../VERSION_MATRIX.md) and [Windows baseline evidence](../evidence/2026-09-05-windows-baseline.json). Host & Play remains a later gameplay-integration gate; the Slice 0 Dedicated Server/two-client baseline passed.

1. Follow [Windows Development](../runbooks/WINDOWS_DEVELOPMENT.md) and place the checkout at `ModSources/Convergence`.
2. Record exact installed versions and commit with the build-record template.
3. Run repository checks, YAML/catalog checks, and the dependency-free domain harness.
4. Run `dotnet build`, tModLoader Build + Reload, Single Player load, Dedicated Server load, and a two-client empty-Mod smoke.
5. If candidate versions fail, change only the compatibility/version decision in a dedicated commit; do not mix encounter edits.

Exit: a reproducible baseline exists. Candidate runtime versions may be marked confirmed only from this evidence.

### Slice 1 — Atomic feature rename, still inert

Status: **Complete (2026-09-05).** Directory, files, namespaces, types, tests, stable key, failure-code prefix, and active documentation now use the target identity. The availability policy still rejects activation and the world adapter remains inert.

Use `git mv` and update the whole identifier family in one commit:

- directory `Content/Encounters/ThirdSeverance` → `Content/Encounters/FirstSeverance`;
- filenames, namespaces, types, tests, project links, registration, current comments;
- key `third_severance` → `first_severance`;
- feature failure-code prefix `third_severance.*` → `first_severance.*`;
- active docs and client target paths.

Do not mechanically rewrite historical ADRs, research observations, or changelog history. Do not create a compatibility alias because the feature is unpublished. Do not renumber packet enums. Keep availability rejection and the inert world adapter.

Exit: source and domain harness compile under the target name with no active world mutation.

### Slice 2 — Replace the obsolete immutable plan

Status: **Complete (2026-09-05).** The active source now contains only the bounded six-state loop, persistent Boss life policy, roster-scaled Pylon/Stack values, append-only feature terminal causes, and the feature-neutral coordinator termination bridge. Activation and the world adapter remain fail-closed.

Delete the multipart/Part Break/Effigy/Last Stand assumptions from the active plan and tests. Introduce typed feature state for:

```text
SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset
```

The plan owns bounded durations, maximum loop count, Overload threshold, roster-scaled Pylon count, Stack share count, and success/failure edges. Boss HP remains persistent; exposure has no required damage budget. Encode the provisional `2 / 2 / 3` Stack share requirement for `2 / 3 / 4` pull participants and the current eight-exposure cap. Cap exhaustion has one edge only: generic `Defeat` plus feature cause `LoopCapExceeded`. Add the append-only byte `FirstSeveranceTerminalCause` values/mapping from the spec; do not overload the generic `EncounterEndReason` or preserve the legacy `AdvanceHardEnrage`/Last Stand edge. The validator checks reachability, bounded cycles, terminal outcomes, valid timing, unique mechanic ownership, and complete generic/cause mappings.

Extend the generic End boundary with an immutable feature-neutral termination descriptor containing generic reason plus bounded feature schema/cause data. A runtime returns that descriptor for feature-owned endings. Add a constructor-validated, definition-owned data mapping for external `WorldUnload`, `InternalFailure`, and `ProtocolFailure` endings so the coordinator can produce the corresponding First Severance cause without re-entering mutable runtime logic. Route Reset, runtime-exception catch, and the future fatal-protocol entry point through the descriptor before terminal publication/cleanup; a missing or incompatible mapping rejects definition construction. Keep runtime-creation failure as a pre-acceptance activation failure with no feature tombstone.

Exit: dependency-free tests cover 2/3/4-player plans, the `2 / 2 / 3` Stack table, early Pylon success, Pylon failure/third Overload, full loop, exposure victory, deadline ordering, direct loop-cap `Defeat + LoopCapExceeded`, and every terminal mapping with no enrage phase. Activation is still denied.

### Slice 3 — Arena, Core, roster, and preparation

Status: **In progress (preparation transport connected, 2026-09-05).** The development Foundation Core right-click now enters the server/SP activation path. Authority resolves the prospective Arena and deterministic 2–4 roster before acceptance, transitions `Validating -> Preparing`, consumes Ready/cancel intents, observes Core/participant loss and timeout, publishes bounded snapshots/validation results, and performs exact-Fight cleanup. The combat gate remains closed and logical Barrier presentation/correction is still pending.

The remaining Slice 3 work is logical Barrier presentation/correction plus repeated runtime cleanup smoke checks. It must preserve the closed combat gate; actor ownership and combat replication remain prerequisites owned by Slice 4.

Implement the first real tModLoader adapters:

- Foundation Core ModItem/ModTile/ModTileEntity;
- server resolver from request anchor to exact Core TE and validated 320x140 prospective arena;
- Calamity progression gate through the compatibility boundary;
- bounded 2–4 player selection, stable Participant IDs, Ready, timeout, cancel;
- logical Barrier presentation/correction and outsider policy;
- typed activate/ready/cancel/snapshot handlers with nonce, distance, rate, side, and lifecycle validation.

All validation completes before world mutation. The Foundation Core does not own the encounter. During Preparing, a valid Foundation Core Tile/TE break cancels and cleans the session. During Active, normal player break/explosion/wiring/liquid attempts are rejected; an unexpected Tile/TE loss invalidates and aborts the exact Fight, while an explicit admin/debug abort uses the same cleanup path. Keep the actual combat transition disabled until actor ownership and feature replication exist.

For the current Development Build only, `DevelopmentPreparationSmoke` downgrades incomplete foundation plus existing container/foreign-TE/protected content from fatal to warning so two clients can exercise transport and Ready state in an ordinary World. Core identity, World bounds/conflict, requester range, duplicate Core, and the 2–4 roster remain strict. The default validator remains strict, and this relaxation must be removed before Barrier or combat world mutation is enabled.

Exit: repeat start/Ready/cancel/Foundation-Core-break/disconnect/unload cycles leave no stale state on Dedicated Server; protected Active break is rejected and injected unexpected Tile/TE loss aborts cleanly.

### Slice 4 — Boss, Pylons, measured hit pipeline, and replication foundation

Implement a single stationary/floating Boss NPC and separate Pylon NPCs. If the provisional ring/arms placeholder is used, it remains draw-only rather than extra NPCs. Add:

- exact-Fight actor registry and defensive entity validation;
- authoritative Boss damage gate and life pool;
- server-owned Pylon state/death collection, early completion, failure pulse, and Overload;
- feature snapshot and bounded deltas/cues;
- read-only client timer, Boss state, and Pylon state.

Before enabling actor damage, instrument the pinned normal Terraria/tModLoader hit pipeline in Single Player, Host & Play host/non-host, and Dedicated Server for representative vanilla and Calamity damage classes, projectile ownership/multihit, damage modification, life/death ordering, and sync. Choose the latest reliable server/SP-observed hook that can validate participant, exact Fight/actor ownership, active substate, and damage gate before progress is committed. Never add a `ReportDamage` packet. If the gate cannot be enforced consistently for unmodified clients, keep activation denied. This is an encounter-correctness gate, not a claim of anti-cheat against modified clients.

Slice 4 may exercise SpawnIntro/Pylon actors and the Boss gate behind a test/debug harness, but it must stop fail-closed before unresolved Stack/Spread behavior. It does not claim the complete loop. Suggested client-to-server requests remain limited to Activate, Ready state, allowed Cancel, Snapshot, and later Revive.

Feature snapshot minimum:

- Encounter Sequence, Fight ID, lifecycle, generic end reason, feature terminal cause (`None` while live), revision, authority tick;
- frozen roster, bindings/epochs, connection/Alive/Downed state;
- active substate, start/resolve tick, loop index, Overload;
- Boss handle, life ratio, damage-gate/visual state;
- bounded Pylon handles and alive/completed states;
- current actor/substate and Pylon result needed by the Slice 4 harness;
- nested bounded `RaidReviveSnapshot` when enabled.

Do not synchronize every tick, hit, particle, or audio sample.

Exit: the measured hit-pipeline record exists; wrong-Fight/nonparticipant/out-of-window actor hits are rejected at the proven seam; two clients see the same Boss/Pylon diagnostic state and recover via a full snapshot. Production activation and progression past Stack remain denied.

### Slice 5 — Complete loop, Stack/Spread authority, and terminal reducer

Complete the active loop executor and phase-deadline ordering here. Add server-observed position sampling at declared resolve ticks, the fixed server-owned Stack damage pool, provisional `2 / 2 / 3` required share counts, frozen-roster round-robin target selection from the loop index, exactly one circular-forward target reissue with a new assignment revision/full 180-tick telegraph, bounded `TargetUnavailable` soft failure after no replacement or a second invalidation, pairwise Spread checks, one failure application per participant, CoreExposure/Reset, and client-only telegraphs. Downed/Eliminated/disconnected participants are excluded consistently from Stack occupants/divisors and current Spread requirements.

The First Severance runtime is the sole reducer of feature-observed subsystem candidates. For each tick it applies permitted actor hits, applies pre-mechanic lethal/interrupt/connection facts, samples the resulting Alive set for the due mechanic, applies mechanic-created lethal facts, processes one revive-start batch, and calls the Revive commit exactly once. It then uses the spec's actor/invariant/gameplay terminal order before any nonterminal phase transition. Add Stack assignment revision/reissue, Spread set, resolve tick/result, exposure, generic end reason, and feature terminal cause to full snapshot, terminal event/delta, and tombstone. Do not let the Revive boundary return a competing early coordinator transition. Coordinator-owned unload/exception/fatal-protocol endings use the Slice 2 immutable external mapping and preempt any uncommitted feature result.

For every feature-owned first-slice terminal, store the cause and return one direct End descriptor. Do not request `TransitionTo(Resolving)` on that tick; the current update contract cannot request transition and End together, and the coordinator already enters Cleanup/publishes its terminal snapshot from End. External forced endings enter through the coordinator bridge rather than an `EncounterRuntimeUpdate`. A future separately specified result/reward ceremony may use `Resolving` on an earlier tick.

Exit: two clients see the same complete repeated loop and recover it from a full snapshot; host/non-host assignments, 2/3/4 players, head-split arithmetic, one-reissue behavior, deadline-tick Downed/disconnect exclusion, latency, reordered packets, closing-tick Victory, safety-terminal priority, direct loop-cap `Defeat + LoopCapExceeded`, feature-cause round trips, and simulated recovery terminal candidates match on authority and every client.

### Slice 6 — Death-hook instrumentation, then revive adapter

First run a separate instrumentation spike on the pinned versions in Single Player, Host & Play, and Dedicated Server. Determine tModLoader/Calamity hook order and behavior for vanilla death, Calamity personal revive effects, immunity, life mutation, and duplicate callbacks. Do not connect `PreKill` merely because the pure domain exists.

If no reliable supported coexistence seam exists, stop this slice and keep activation denied. The backlog preserves ordinary death plus deterministic Raid re-entry as a contingency, but it cannot be implemented without an explicit user decision and new/superseding ADR/spec/tests.

After the spike defines an accepted adapter policy, implement:

- authority-only lethal interception scoped to an active First Severance participant;
- `ModPlayer` control/damage/targeting projection and defensive reset;
- non-consumable provisional `Resuscitation Kit` ModItem;
- `RequestStartRevive(targetParticipantId, requestNonce)` and `RequestCancelRevive(channelNonce)` bounded DTOs;
- server resolution of sender from `whoAmI`, current binding/epoch, held item, range, state, token, reservation, and exact Fight;
- movement, damage, teleport, item change/release, mount/hook, range, disconnect, and target-invalid cancellation;
- server-owned life restoration, invulnerability, weakness, and one synchronized result;
- victory/cancel/defeat normalization of Downed/Eliminated player bodies.

The item never reports completion. The shared token is consumed only by a successful authority completion. Observe raw held-item/use/release state before applying control suppression. While `IsReviving`, the current coarse `SuppressItemUse` projection means suppress non-revive actions while preserving the accepted revival-item lease/animation; it must not cancel itself. If necessary, split that projection into `SuppressNonReviveItemUse`. Release or item change interrupts the exact lease once.

Exit: the complete revive matrix passes without double death, duplicated revive resources, permanent player flags, or stale slot mutation.

### Slice 7 — First-playable acceptance

Enable progression-gated activation only after slices 0–6 pass. Run:

- Single Player diagnostics where applicable;
- Host & Play and Dedicated Server with 2, 3, and 4 participants;
- 100/200/300 ms latency and loss/reorder injection;
- host and non-host Downed/revive, simultaneous Downed, disconnect/rejoin, slot reuse;
- Pylon success/failure/third Overload; Stack/Spread failures; 4–6 exposure tuning runs;
- same-tick Boss-zero/all-Downed Victory precedence and all declared terminal collision cases;
- victory, Defeat, cancel, protected Foundation Core break, unexpected Foundation Core Tile/TE loss, Boss missing, exception, reload, and World unload cleanup;
- visual accessibility at supported resolutions/UI scales.

Only then call the first Raid playable. The provisional vertical-slice duration target is 2–4 minutes; 5–12 minutes remains the eventual expanded-Raid product goal. Production art, audio, rewards, and final balance remain separate work.

### Packet and command rules

For each request, parse fixed/bounded data fully, then validate sender, side, protocol, Encounter Sequence, Fight ID, revision/nonce, current roster binding/epoch, lifecycle, range/item when relevant, and rate limit before mutation. Unknown, stale, duplicate, and malformed input is rejected with bounded logs.

Requests carry intent only:

- activation carries a candidate anchor, never a validated Core;
- Ready changes only the sender's state;
- revive carries a target stable Participant ID and nonce, never duration/success/life;
- cancel carries the exact current lease nonce;
- snapshot request carries no authority state.

### Cleanup inventory

Register as soon as created:

- Boss and Pylon NPC handles;
- mechanic/damage Projectile handles;
- active assignments and temporary feature state;
- Barrier and Core busy projection;
- participant control/targeting projections;
- revive service/channels/reservations;
- feature snapshot/outbox subscription.

Cleanup verifies the expected Fight ID, stops spawning, publishes the terminal state, removes owned actors, restores players safely, resets the Foundation Core if its Tile/TE still exists, clears feature state, and releases the session. Calling the same exact-Fight cleanup twice succeeds as an empty no-op; a stale-Fight cleanup never clears the current fight.

### Definition of Done for the first Raid slice

- 2–4 players can activate, Ready, enter, clear, fail, cancel, disconnect, and cleanly reload on the pinned Dedicated Server environment.
- The exact accepted loop is the only live combat route.
- Boss damage is impossible outside Core exposure and victory is authority-owned.
- Pylon, Stack, Spread, Downed, Revive, Overload, and tokens behave identically for host and non-host.
- No custom feature packet can directly declare participants, DPS, position-check results, revive completion, life, or victory; inherited Terraria movement/combat trust is documented rather than presented as modified-client anti-cheat.
- Every terminal/error/unload route leaves no actor, reservation, projection, Core busy flag, or active session.
- Repository checks, catalog/YAML checks, domain harness, tModLoader build/load, and relevant multiplayer evidence are recorded.
- Names, timing, damage, and art still labeled provisional are not misreported as final.

## Snapshot: docs/handoff/WINDOWS.md

## Windows Handoff — 2026-09-04

This checkpoint transfers Convergence from planning/bootstrap work on the MacBook to primary implementation on minami's Windows desktop.

Post-transfer update (2026-09-05): the Windows baseline, atomic identity rename, and Slice 2 immutable-loop/termination work are complete. The historical transfer details below are retained for provenance; [Status](../STATUS.md) and the [implementation plan](../encounters/first-severance/IMPLEMENTATION_PLAN.md) own the current next action.

### Repository checkpoint

- Remote: `git@github.com:Minamium/tmod.git`
- Branch: `main`
- Local source directory required by tModLoader: `ModSources\Convergence`
- Base code/policy commit before this documentation handoff: `d5f8758` (`build: add domain test CI and staged dependency policy`)
- Expected state after transfer: `git pull --ff-only origin main`, then clean `git status --short`

Use `git log -1 --oneline` to record the exact handoff commit after pulling. Do not hard-reset an existing Windows checkout with uncommitted work.

### Read order

1. [`README.md`](../../README.md)
2. [Documentation Home](../README.md)
3. [Project Status](../STATUS.md)
4. [First Severance overview](../encounters/first-severance/README.md)
5. [First Severance implementation plan](../encounters/first-severance/IMPLEMENTATION_PLAN.md)
6. [Windows development runbook](../runbooks/WINDOWS_DEVELOPMENT.md)
7. [Version matrix](../VERSION_MATRIX.md)
8. [Architecture](../ARCHITECTURE.md) and [Network Architecture](../NETWORK_ARCHITECTURE.md)
9. [ADR-0002](../adr/0002-server-authoritative-encounters.md), [ADR-0003](../adr/0003-in-world-logical-arena.md), [ADR-0005](../adr/0005-server-authoritative-downed-revive.md), [ADR-0006](../adr/0006-staged-calamity-independence.md), [ADR-0007](../adr/0007-first-severance-vertical-slice.md)

### Decisions transferred

- Start from the first encounter, named `First Severance` / `第一断絶`, rather than designing around a “third” event.
- Keep the first Boss visually simple: one Boss NPC/body and one life pool, with no separately damageable presentation parts.
- The only first-slice combat loop is Pylon DPS check → Stack → Spread → Core exposure → repeat while HP remains.
- Boss damage is accepted only during exposure. HP persists; exposure has no separate damage-budget failure.
- Stack is a server-owned head-split damage pool rather than only an “everyone inside” check.
- Raid lethal damage becomes Downed; another player uses a dedicated item and channels a server-validated revive. Shared token defaults are 1/2/3 for 2/3/4 players.
- Server/SP owns all gameplay outcomes. Clients send bounded intent and render replicas.
- Windows is primary for tModLoader, Calamity, Host & Play, and Dedicated Server validation. The MacBook remains a valid secondary docs/review machine.
- The initial product is a Calamity addon, with the accepted staged route toward removing the hard dependency later.

Boss/lore/facility/item proper names other than `First Severance` are provisional. The loop order, simple one-body constraint, multiplayer authority, and ally-item recovery direction are accepted. The central Core + broken ring + two arms placeholder, one-Pylon-per-roster layout, missed-Pylon pulse/short exposure, three-Overload threshold, Stack `2 / 2 / 3` shares, revive token counts, damage, HP, radii, timings, arena details, and eight-exposure cap are deterministic prototype policy, not final user-approved balance or art.

The first vertical slice targets roughly 2–4 minutes. The broader 5–12-minute goal belongs to an eventual expanded Raid. Loop-cap exhaustion currently commits `Defeat(LoopCapExceeded)` directly; do not retain the legacy hard-enrage/Last Stand edge.

The network design assumes cooperative play with unmodified clients. No Convergence packet can directly declare DPS, position-check success, life, revive completion, or victory, but this is not anti-cheat against a client modifying Terraria's ordinary movement/combat replication.

### Current implementation truth

There is no playable Boss. The current source uses `FirstSeverance` / `first_severance` and deliberately rejects activation. It contains:

- a generic encounter/runtime/cleanup and replica bootstrap with feature-neutral terminal descriptors and definition-owned external failure mappings;
- an inert Core-anchored arena plus the validated six-state Pylon/Stack/Spread/Core loop;
- a pure, substantially tested Downed/Revive domain;
- repository checks and a dependency-free domain harness.

It does not contain the Core Tile/TE, Boss/Pylon NPCs, live Stack/Spread resolution, live transport/feature snapshots, revive item, death/control adapter, production presentation, or rewards. The pinned Windows build/load/server baseline is recorded separately; see [Status](../STATUS.md) for the exact inventory.

### MacBook environment audit

Observed before transfer:

- MacBook Air with Apple Silicon M4, 16 GB RAM, macOS 26.6.2;
- Steam, VS Code 1.131.0, Git 2.50.1, GitHub CLI 2.97.0, Homebrew, Python 3.13.2, and Xcode Command Line Tools present;
- Terraria, tModLoader, .NET SDK, Calamity, C# Dev Kit/Rider, and Aseprite not found;
- repository checkout was outside a tModLoader `ModSources/Convergence` tree, so `..\tModLoader.targets` was absent;
- repository/YAML checks could run, but no honest Mod build/load/server claim could be made.

tModLoader can be developed on macOS, but this machine was not prepared for it and the project needs Windows/Dedicated Server evidence anyway. Do not spend time duplicating the full runtime on the Mac before the Windows baseline is stable.

### First Windows session

1. [x] Install/confirm the tools and candidate versions in the [runbook](../runbooks/WINDOWS_DEVELOPMENT.md).
2. [x] Clone/pull as `ModSources\Convergence`.
3. [x] Run catalog, repository, YAML, and domain checks.
4. [x] Run command-line build, Build + Reload, Single Player, Dedicated Server, and two-client baseline.
5. [x] Fill `build-record.local.json` from the template; update the version matrix only with observed evidence.
6. [x] Perform the isolated `ThirdSeverance` → `FirstSeverance` rename while activation remains denied.
7. [x] Replace the obsolete immutable plan with the simple repeated loop and update domain tests.
8. Begin Slice 3 Core/Arena/roster/Ready preparation while keeping combat activation denied.
9. Before enabling Boss/Pylon damage, record the normal-hit pipeline for host/non-host and Dedicated Server as required by the implementation/test plans.
10. Continue slices in the [implementation plan](../encounters/first-severance/IMPLEMENTATION_PLAN.md).

### Blocking runtime research

Before enabling Boss/Pylon damage, instrument representative Terraria/tModLoader/Calamity hits in Single Player, Host & Play, and Dedicated Server. Prove the server/SP-observed seam that validates exact-Fight actor ownership, participant eligibility, phase damage gate, committed life/death, and sync. Do not add a custom damage-report packet or call ordinary Terraria replication cheat-proof.

Instrument pinned tModLoader/Calamity lethal handling in Single Player, Host & Play, and Dedicated Server. Establish exactly how `PreKill`, Calamity personal revive mechanics, life/cooldown changes, sync, and duplicate callbacks interact. Until an explicit adapter policy and tests exist, keep death interception disconnected and activation fail-closed.

### Do not accidentally implement

Part Break, Targeted Line/Bait, Personal Effigies, Split Reality, hard-enrage/Last Stand phases, multipart Crown/Wings/Heart Casing, final art/music/rewards, Solo, or Standalone progression are outside the first slice. Their ideas remain in the [backlog](../encounters/first-severance/BACKLOG.md).

This exclusion narrows implementation order only; it does not cap the eventual Calamity-scale Raid or standalone Mod. Promote a deferred idea later only with a fresh player-facing contract, authority/replication/cleanup model, tests, and explicit scope decision.

### Handoff completion check

The transfer is complete: the Windows checkout and exact baseline evidence were established without relying on chat history. Continued work begins from Slice 3 after Slice 2 verification is committed; current truth remains in [Status](../STATUS.md).

## Snapshot: docs/encounters/first-severance/ENCOUNTER_SPEC.md

## First Severance Encounter Specification

### Development experiment override

The separate two-client assistance in [ADR-0015](../adr/0015-console-only-single-pull-assist.md) permits a dedicated-server-console-authorized real auxiliary client to receive one-Fight invulnerability. That permission does not itself lower admission requirements or auto-Ready/revive. Preparing for a valid assisted lease allows ten minutes for window switching; normal preparation remains sixty seconds. The protection applies only during combat and clears before Defeat death. Ordinary players have no in-game grant command. The newer one-client debug path below needs no auxiliary or protection; [the runbook](../runbooks/SINGLE_OPERATOR_TESTING.md) owns both procedures.

The user-requested `0.2.0` expansion in [ADR-0010](../adr/0010-giant-boss-observation-lances.md) adds the original giant Boss presentation and observation lances. `0.2.1` deliberately over-tunes stats and accelerates attacks/presentation; [ADR-0011](../adr/0011-instant-revival-and-recipient-lockout.md) replaces held revival/shared tokens with an instant reusable kit and recipient-only 60-second lockout. These changes preserve one logical Boss life pool, authority and release boundaries. All damage/timing numbers are provisional, not a guarantee of a fair or clearable fight.

The user-authorized experiment in [ADR-0009](../adr/0009-development-combat-experiment.md) runs before production integration is complete. Current `0.2.12` HP follows the frozen-roster table below; Boss/Pylon defense remains **240 / 120**. Stack uses the missing-roster fraction below. Spread overlaps receive 70% maximum HP once; failed Pylons pulse 35%, clamped nonlethal. Both Stack and Spread cost zero HP on success. General Terraria lethal events are not intercepted. [ADR-0012](../adr/0012-pattern-sequences-and-defeat-death.md) adds independent-origin attacks, the introduction and normal participant death on Defeat. [Status](../STATUS.md) owns implementation and test results.

### One-member development start and results — 0.2.14

While the server/SP build's `ConvergenceDevelopmentSolo` flag is enabled, a single eligible real player may activate the existing Core and manually Ready, including ordinary Host & Play with no guest. The flag defaults on during development and must be compiled off before public release; [ADR-0020](../adr/0020-development-solo-admission-and-terminal-hud.md) owns admission, wire bounds and release safety. All Core/field/progression checks still apply. The preparation message explicitly identifies SOLO DEBUG. No NPC, extra window, invulnerability, automatic revival or persisted player/world setting is introduced.

Initial admission and the fresh Ready-to-combat scan use the same non-optional compiled policy. Revalidation still rejects a removed Core, newly blocked field or world conflict; it must not reintroduce a separate minimum-two default. Current implementation and observed regressions are recorded in [Status](../STATUS.md).

Solo debug uses **Core HP 5,000,000, two Pylons at 300,000 HP each**, the existing two-player workload. Attack sequence, geometry, damage, timers, phase HP gates, infinite Raid flight and Final survival are unchanged. Targeting uses the sole real player. Stack uses the normal missing-roster fraction: one present out of one means zero damage; missing the fixed marker means the full fraction. Spread still resolves all pairs; with one participant there are no overlaps. These natural count-dependent outcomes are not a dedicated solo redesign or a multiplayer balance claim. Two/three/four-member HP tables and behavior remain unchanged.

If the sole participant becomes Downed, the ordinary authority commit immediately ends the Raid as AllParticipantsDowned/Defeat; it does not wait for an impossible solo revival. Existing participant death and exact-Fight cleanup follow. This does not bypass an external Mod's death-cancelling hook. Public-release minimum remains two until a separate solo/companion design is approved.

Accepted **Victory and Defeat** now replace the participant's HUD with a bounded result cinematic, also over the local death screen. Gameplay cleanup and player death are immediate, not delayed for presentation. Duration, camera, typography and effects are owned by the [visual spec](../encounters/first-severance/VISUAL_SPEC.md#success-and-failure-designations--0214). Cancelling or unloading is not a success/failure cinematic. The start log records `solo_debug` and the compiled admission flag so one-member diagnostics cannot be confused with multiplayer calibration.

### Ordered phase scores and terminal survival — 0.2.13

The phase structure originated in0.2.13; attack tuning in this section is updated for0.2.17. [ADR-0021](../adr/0021-untimed-recovery-and-simultaneous-prism.md) removes Eliminated/Down expiry while retaining the60-second recipient restriction and immediate all-Down Defeat.

This section supersedes the earlier immediate 50%-HP transition, grid-only Phase II and HP-zero Victory. The frozen 2/3/4-player HP table, defense, Pylon tuning, zero-damage full Stack/safe Spread, instant reusable revival and existing defeat/cleanup rules remain. [ADR-0019](../adr/0019-phase-scores-and-terminal-survival.md) owns authority and replication. Final means surviving all required actions, **not** a no-hit challenge; ordinary fixed damage, Down and revival remain available.

| Stage | Full action list required before first exit | HP floor / next stage |
|---|---|---|
| I / Sealed | Existing opening Pylon → fixed Stack → Spread → full Core exposure; then four clockwise Stack sites, with Spread after each | 50% → six-second Unbound eclosion |
| II / Unbound | Grid block → Spread → rotating blade → Spread → grid block → Spread | 25% → five-second distant retreat |
| III / Distant | Three expanding horizontal floods → left-half beam → Stack → Spread → right-half beam → three expanding horizontal floods → Stack → Spread → central hand crush | 0% → four-second Final designation |
| Final | Eight clockwise Stack sites; each followed by Spread, then alternating bullet rain / slicing comb | HP remains 0; one full accelerating68.567-second score → Victory |

The first full Core exposure is never interrupted by reaching 50%. After it, four sites proceed top → right → bottom → left around the arena center. Every site is a **stationary world coordinate for that action**, not a following player or moving damage disc. Current valid radius remains 112px; only that radius is circular, with directional chevrons and ordinal label. Ellipse radii are 560px horizontally and 300px vertically. Final uses the same ellipse with eight 45-degree positions. Distant's two assemblies use the top and bottom sites.

Damage clamps at the next stage's floor, including lethal NPC hits; overkill is discarded. At a floor the aperture closes and HUD shows HP LOCK while attacks continue. The score's first completion releases a pending transition. If the floor was not reached on that cycle, another cycle begins; later threshold arrival exits at a damage-action deadline, since the required full cycle has already been demonstrated. Nothing can skip the first score or kill the Boss before Final. Logical HP zero is replicated; the temporary NPC's technical one HP is not a second health bar. Final is not counted as an ordinary numbered phase, so future IV/V can be inserted before it. The old eight-exposure hard cap is not applied to this staged score; Pylon Overload and recovery failure still apply.

#### Travel and spread tuning

Spread radius remains **320px**; two centers must be at least **640px / 40 tiles** apart. Overlap still costs70% maximum HP once per affected player; no overlap costs zero. Initial opening Stack/Spread retain four seconds. Non-Final score Stack remains150 ticks, first site180; Spread180. Final uses zero-based station `i=0..7`: Stack180 at first, then`150-3i` (147→129); Spread`180-6i` (180→138); dodge block`240-8i` (240→184). Thus each successive station tightens every category without shrinking the actual marker or changing shares. The full score is4,114ticks; category entry/phase cinematics outside Final are unchanged.

These are estimates, not verified equipment-independent dodgeability: assume sustained10px/tick movement,30 ticks reaction and6 ticks settling/network margin. Four example Spread stations at arena-center offsets(±330,±330) have660px minimum separation. From every example station, `(distance-112)/10+36` fits each particular next Stack deadline, including the accelerated Final. Real turning, latency and gear reduce margin; user playtesting remains necessary. Infinite equipped-wing/rocket flight is unchanged.

#### New attack modules

- **Grid blocks:** 450 ticks each, with existing 60-tick opening rest and warning/live/cadence rules; Spread occupies the gap between blocks. The global grid serial continues, and from serial 3 the existing per-standing-player 144px Core lasers remain. Grid/Core share their original one-hit-per-volley ledger.
- **Simultaneous opening Prism:** each of eight fast steps locks one predicted ray for every currently Alive connected participant, all on one authority tick. Red→blue→green→amber then reverse retains28 warning/12 live ticks and42-tick step cadence. There is no per-player turn to wait for. Whole-combo/category rests remain; charges and Stillness are unchanged. One fixed120 hit maximum per player per step even where several targeted rays cross. `LanceTelegraph` logs bounded`target_slots` and ray count.
- **Rotating blades:** two opposing88px-wide rays reach the field edge. Harmless windup180, rapid unsheathing144–156 and24 ticks of full-length warning remain. Motion is now **two clockwise revolutions over300ticks**, with angular progress`2*(0.55t+0.45t²)` turns: mean speed1.6× the preceding one-turn attack, and continuously accelerating. Peak tangential speed at radius180 is10.93px/tick; move farther inward for more margin. Recovery60 ticks is harmless. One fixed120 hit per blade **per revolution**; distinct turn/pulse IDs prevent the first turn's hit cap making the second harmless. Prior-tick swept angle prevents tip tunneling. Smooth damage-zone aura replaces hard side rails.
- **Expanding horizontal floods:** `RemoteClaws` remains the stable state ID,600 ticks/three200-tick pulses. A short48-tick smooth arm charge forecasts two full-width horizontal bands and one genuine safe strip. At48, thin beams deploy across the2560px field over12 ticks; their authoritative half-width accelerates to the forecast band over42 more ticks with`g(t)=t³(4-3t)`. Final full widths hold until142;142–180 cooling is harmless. The safe strip is192px high, centered at relative Y−260,+200,−40, rotated by one site for the later block. The two bands fill everything above/below it, including the sides; a42px-high player has150px center clearance. Launch side alternates left/right. One fixed120 hit cap per player per whole pulse, not separately for each band. Body rig, forecast, growing surface and collision share the same schedule; no player-velocity rule or new world actor.
- **Half-field beam:** 180-tick large charge of the entire selected half, 60 live ticks, 60 recovery ticks. First is left, second right; each uses exactly half the 2560x1120 field. One fixed120 hit maximum. The complete player body must leave the boundary, not just its center. The opposite half is safe from this action.
- **Central hand crush:** new final action of the Distant score,300 ticks. The fixed720×600px rectangle at the field center is warned from entry. Both giant hands brace for150 ticks and close inward in12 ticks; only ages162–179 are lethal. Side and upper/lower pockets outside the marked rectangle remain safe; the approaching arms themselves do not damage. Contact routes through existing lethal-to-Down recovery (not permanent death or a new death hook), retaining post-revive immunity and authorized debug protection. One hit ledger entry per participant; retract and all remaining recovery are harmless. It allocates no gameplay hand NPCs or new requests.
- **Terminal bullet rain:** five waves of24 beads, with24-tick per-wave appearance, within the shrinking dodge block. Normalized terminal progress`p=i/7` sets first release`36-floor(8p)`, wave interval`32-floor(10p)`, speed`10+3.5p`px/tick. Tangential displacement reaches240px; lifetime is`115/(speed/10)` or the action deadline. Radius12px, swept body checks and one fixed120 hit per wave remain. No hazard/ledger survives into the next Stack.
- **Terminal slicing comb:** six alternating-axis pulses. Cadence`40-round(12p)` ticks, harmless warning`ceil(cadence*.7)`, live until`cadence-4`, then four harmless recovery ticks.56px full width and160px pitch retain104px open lanes; boundary teeth are included (at most17 vertical/eight horizontal). Offset is`((floor(pulse/2)*53 + i*37) mod160)-80`px. Each repeated axis shifts53px rather than sharing a fixed parity, so no stationary whole-player position remains safe through its three shots. Every complete pulse fits the shortened action. One fixed120 hit per pulse; axes never fire simultaneously.

All new hazards respect Alive membership, bound slot/epoch, post-revival immunity and the existing Raid-owned lethal-to-Down adapter. They do not overlap the prescribed Stack/Spread deadlines: the previous action expires before the next one begins. Last-stage damage cannot cause an early win. Administrative abort, cancel, unload and existing recovery failure preempt normal progression according to the unchanged terminal contract.

Protocol16 requires matching peers for the changed deterministic timing, simultaneous Prism bound and untimed Down interpretation. Stable substate/packet IDs, request shapes and saved data are unchanged. The [ADR-0019](../adr/0019-phase-scores-and-terminal-survival.md) score/authority/cleanup model remains. Stack sync/112px acceptance, Spread640 separation, frozen HP, instant item use and60-second recipient lockout are unchanged.

`PhaseChanged` now records action index, completed phase cycles and resolve tick. `HpGateReached` distinguishes HP waiting from a stuck fight; `ScoreAttackFired` identifies new ray pulses; `SpreadResolved` logs alive/overlapping counts. Core telemetry now measures the **damageable segment above its phase floor**, closing on HpGateReached so mandatory waiting does not dilute DPS; the full `boss_life` remains separate. Previous reports using the whole persistent pool must be interpreted with their recorded build.

### Forgiving roster HP and Core salvos — 0.2.12

The accepted roster chooses one immutable `FirstSeverancePartyScaling` at combat creation. No live-player count, Down, elimination, reconnect, difficulty multiplier or client DPS report recalculates it. Pylons in subsequent loops use that same tuning; the Boss still has one persistent pool.

| Frozen participants | Boss/Core HP | Each Pylon HP | Total Pylon pool | Required party Pylon DPS in 14 s |
|---:|---:|---:|---:|---:|
| 2 | 5,000,000 | 300,000 | 600,000 | 42,857 |
| 3 | 9,000,000 | 500,000 | 1,500,000 | 107,143 |
| 4 | 13,000,000 | 600,000 | 2,400,000 | 171,429 |

Calibration uses the preceding two-player clear's Core/Pylon effective rates (76,287 / 81,081 DPS) and three-player clear's rates (199,501 / 233,161 DPS), not raw equipment damage. At those rates, two-player Core damage requires about 65.5 open seconds and a Pylon pool 7.4 seconds; three-player Core about 45.1 seconds and a Pylon pool 6.4 seconds. These deliberately leave development margin. These are damageable-time estimates, not predicted full fight durations: shield/mechanic/transition time and reduced output while dodging are separate. The builds and phase structures differed; the measured ratio is not a universal per-extra-player factor. Four-player tuning is unmeasured extrapolation. Sanitized observations and limitations live in [the check record](../evidence/2026-09-06-eclosion-salvos-checks.json).

From **grid volley serial 3 onward**, each warning also locks one Boss-origin laser toward each currently connected Alive participant. The serial continues across Phase-II scheduling windows; Downed/Eliminated participants are not new targets. Origin is the Boss's fixed damage Core, length **3,000 px**, full damage width **144 px**. Direction freezes for the entire **60-tick warning and 20-tick live window**; no live homing, new projectile actor or player-speed test. Grid and Core lasers share one per-participant hit ledger: **one fixed 120-HP hit maximum for the whole volley**, including multiple intersections. A player may dodge both by leaving the locked corridor and choosing a grid cell. All old cleanup/recovery/timing rules remain.

Protocol **v12** appends a bounded 0–4 count and unit-direction pairs to the existing grid descriptor; source and dimensions derive from shared constants. All peers update together. Actor `SyncNPC` extra AI carries the frozen count and bounded current life so local HP bars match the authority pool, including a full-life spawn. No client request or saved field is added; [ADR-0018](../adr/0018-roster-health-and-core-salvos.md) owns this extension. `CombatStarted` records `hp_policy=FrozenRosterDevelopment`; grid logs include `core_beams`, firing damage/cap, and hits distinguish `source=CoreSalvo` from `source=Lattice`.

### Boss stages and fixed assembly — 0.2.11

The existing Pylon → Stack → Spread → Core-exposure cycle is **Phase I / Sealed**. The single Boss is visually enclosed in a damaged spherical restraint. Core HP is one persistent roster-scaled pool, not separate phase health bars (the original 0.2.11 pool was fixed at 4,000,000). During a Phase-I exposure, the first authority observation reaching **50% of that pool** enters `PhaseTransition`. Excess damage is clamped at this floor, including a normally lethal NPC hit; the transformation cannot be skipped by one large hit.

`PhaseTransition` lasts **360 ticks / 6 seconds**, shields the Core and cancels outgoing attacks. It keeps the exact Fight, frozen roster, recovery deadlines and containment/flight capability. Clients focus the camera on the Boss, temporarily omit normal HUD layers and show limbs prying the shell open, followed by emergence and unfolding. No persistent input lock, zoom or UI flag is written. The feature-local ordered stage plan selects the next attack module; future stages can append a definition/module without introducing Boss-specific branches in the global coordinator. [ADR-0017](../adr/0017-boss-stages-fixed-stack-and-lattice.md) owns this structure and its original protocol v11; the v12 extension is above.

**Phase II / Unbound** opens the Core continuously and replaces the Pylon/Stack/Spread cycle with `Lattice`, a personal-dodge phase. Vertical and horizontal beams span the entire field, at **160 px / 10-tile spacing**, **24 px full damage width**, leaving 136-px clear cells. Four deterministic patterns shift the grid in 40-px increments; horizontal/vertical shifts differ. One descriptor derives at most 32 rays (21–23 in the current field), not a grid of NPCs/projectiles. Every volley has **60 harmless warning ticks, 20 live ticks**, with starts at least **102 ticks apart** and an additional 60-tick rest after every fourth volley. First warning starts 60 ticks after a window opens; casts are never truncated or caught up in a burst.

Each Alive participant intersecting any live line takes at most **one fixed 120-HP hit per volley**, even at a crossing. Recovery protection and Raid-owned lethal-to-Down are retained; no player-velocity condition or shared attendance check exists in Phase II. Remaining in a safe cell is harmless. The phase is divided into 18-second scheduling/telemetry windows without healing or returning to Phase I. The existing cap of eight completed damage windows counts both phases; a threshold-interrupted Phase-I exposure is not completed. Zero final-stage HP wins before the deadline/cap check.

Stack is now a **fixed assembly site**, 880 px above the foundation's ground center (320 px above the Boss Core), with its existing 112-px / 7-tile inclusive radius. The snapshot sends the actual `StackX/StackY`, never a player slot. In 0.2.12, only this valid radius is circular; exterior gathering arrows, a straight countdown bar and a localizable instruction mark the place. No inner shrinking circle or outer circular result wave suggests a different radius. Its location does not move when a participant moves or becomes Downed. Attendance and penalties below are otherwise unchanged; Spread is unchanged.

Authority diagnostics add `boss_phase` / `phase_start_tick` to phase changes, `GridTelegraph` / `GridFired`, `source=Lattice` damage, and `StackAttendance` with per-slot distance/inside/alive state at resolution. Damage windows distinguish `Lattice` from `CoreExposure` and end the threshold exposure with `outcome=PhaseTransition`. Logs do not infer a hit from decorative effects or assign weapon DPS to individual players.

### Grounded containment field — current 0.2.11

New Foundation Core placement uses a 12x4-tile plinth, anchored on supporting ground/platforms with clear solid-block airspace 80 tiles to either side and 70 tiles above its bottom. Placement preview shows the intended **160x70** rectangle and rejects solid obstructions; server activation and all-Ready recheck the actual arena without terrain mutation. This halves each dimension of the preceding 320x140 field (one quarter of its area), not merely its area. Old 2x2 Cores remain readable/clickable; mine and replace to obtain the larger foundation. The ground under the plinth is the field floor, not the old floating center.

During Active, the complete participant body is confined to that rectangle, including dash, knockback and teleport movement. Server authority and owning-client prediction use the same boundaries; other players are unaffected in this development pass. Connected standing participants gain inexhaustible equipped wing/rocket flight; without wings, holding Jump provides field-supported lift. Downed/Eliminated participants cannot fly. End/cancel/disconnect/unload revoke the capability. This is temporary Raid state, not saved equipment or a public cheat toggle. [ADR-0016](../adr/0016-ground-containment-and-continuous-emission.md) owns authority, protocol v10 and legacy-tile compatibility.

The Boss aperture is at field center, **560 world pixels** above the plinth ground. Pylons fit around that airborne center. Steel columns, central iris and the outer seal deploy over the six-second safe intro. For participants, the world outside the field is opaque black, including foreground objects/projectiles; HUD remains above the mask outside cinematic windows. The mask derives from the same bounds under camera movement, zoom and UI scale, and persists in Reduced Effects. It disappears with the local combat lease/cleanup; outsiders retain ordinary world rendering. [Visual spec](../encounters/first-severance/VISUAL_SPEC.md) owns presentation.

### Identity and scope

| Field | Value | Decision state |
|---|---|---|
| Event | `First Severance` / `第一断絶` | Accepted development name |
| Target key | `first_severance` | Accepted; code rename completed |
| Party | 2–4 frozen pull participants | Accepted |
| Progression | Post Exo Mechs and Supreme Calamitas, Shadowspec-level loadouts | Accepted for Calamity Stage A |
| Boss | `The Null Cantor` / `無響の唱導者` | Provisional working title |
| Location | Polar containment/research facility | Concept accepted; proper name TBD |
| First-slice duration | About 2–4 minutes | Provisional vertical-slice playtest target |
| Eventual full-Raid duration | About 5–12 minutes | Product goal; not a first-slice acceptance claim |

The development implementation proves a multiplayer Raid loop plus a first original giant-Boss art/attack pass, not final encounter complexity. A balanced public solo/companion mode, rewards, final production art/audio and final tuning remain out of scope; the one-member debug exception above is solely for faster iteration.

### Decision boundary

Accepted direction is the ordered Pylon → head-split Stack → Spread → Core-exposure loop, multiplayer-first 2–4-player scope, a simple one-NPC Boss, server-owned encounter results, and ally-item Downed/Revive. The exact one-Pylon-per-roster rule, timings, marker sizes, Stack share count, damage values, Overload threshold, exposure penalty, HP, loop cap, and silhouette components are **provisional prototype policy** until Windows multiplayer evidence is reviewed. They are written here so the first implementation is deterministic, not because minami approved them as final balance.

In this document, **Foundation Core** means the activation Tile/Tile Entity. **Boss Core** means the single Boss life pool exposed during `CoreExposure` or Phase-II `Lattice`.

### Lifecycle and active loop

The generic lifecycle permits either a direct terminal End or an optional resolving stage:

```text
Idle projection -> Validating -> Preparing -> Active
                                      |          |
                                      |          +-> TransitionTo(Resolving) -> later End(reason)
                                      +------------> End(reason)
                                                        -> Cleanup -> Idle projection
```

First Severance uses the direct `End(termination)` path for every first-slice terminal. The runtime stores the selected feature terminal cause in the immutable descriptor and returns `EncounterRuntimeUpdate.End` from `Preparing` or `Active`; the current coordinator enters `Cleanup`, publishes the terminal projection, and starts cleanup. It does **not** request `Resolving` and End on the same update. `Resolving` remains available only for a future separately specified result/reward stage that transitions on one tick and ends on a later tick.

`Active` owns these stages (the enclosing generic lifecycle is unchanged):

```text
SpawnIntro
  -> PylonCheck
  -> Stack
  -> Spread
  -> CoreExposure
       -> boss HP > 50% at deadline: Reset -> PylonCheck
       -> boss HP reaches 50%: PhaseTransition (6s) -> Lattice (Phase II)
                                                        -> repeat grid windows
                                                        -> HP = 0: End(Victory) -> Cleanup
```

Boss HP persists through every loop/stage. Normal encounter damage is permitted only during `CoreExposure` or `Lattice`; all other substates use an authoritative shield. There is no separate damage quota or failure merely for low damage in one window.

### Provisional timing table

All timers are server ticks at 60 ticks/second. They are prototype defaults, not protocol constants.

| Substate | Duration | Early exit |
|---|---:|---|
| `SpawnIntro` | 360 ticks / 6 s | none |
| Pylon telegraph | 90 ticks / 1.5 s | none |
| Pylon active window | 840 ticks / 14 s | all current-loop Pylons destroyed |
| Stack telegraph | 240 ticks / 4 s | none |
| Spread telegraph | 240 ticks / 4 s | none |
| Normal Phase-I Core exposure | 1080 ticks / 18 s | Boss HP reaches 50% |
| Penalized Phase-I Core exposure after failed Pylons | 720 ticks / 12 s | Boss HP reaches 50% |
| `Reset` | 120 ticks / 2 s | none |
| `PhaseTransition` | 360 ticks / 6 s | none |
| Phase-II `Lattice` scheduling window | 1080 ticks / 18 s | Boss HP reaches zero |

The provisional initial safety cap is eight completed exposures. If Boss HP remains when that cap is reached, authority immediately commits `Defeat` with reason `LoopCapExceeded`. The first slice has no hard-enrage or Last Stand transition; those remain deferred. Playtest data may change or remove the cap, but the implemented edge must never be an outcome OR.

### Pylon DPS check

- The provisional first prototype spawns one authority-owned Pylon NPC per frozen pull roster member: 2, 3, or 4.
- Any connected Alive participant may damage any Pylon. There is no player-to-Pylon affinity.
- Count and health are fixed from the pull roster and do not rescale after a participant becomes Downed or disconnects. Remaining players may cover unfinished Pylons.
- Suggested symmetric placement is left/right for two, triangle for three, and quadrants for four. Exact positions must derive from the server-resolved arena layout.
- Server-owned entity damage/life decides success. Clients never submit DPS totals or success.
- Destroying every Pylon advances immediately to Stack.
- At deadline, remaining Pylons are removed through encounter ownership; failure adds one `Overload`, applies a survivable raid-wide pulse, and marks the later exposure as penalized.
- `Overload == 3` causes Defeat. One Pylon failure is a recoverable soft failure.

Pylon HP is a typed feature tuning value. Set it from Windows telemetry so a clean party has meaningful margin; do not encode a client-reported or class-specific DPS requirement.

#### Provisional HP calibration and authority diagnostics — 0.2.10

Historical 0.2.10 tuning was a fixed 4,000,000-HP Core and 250,000 HP per Pylon. It has been superseded by the 0.2.12 frozen-roster table above. The passive diagnostic definitions below remain current; the old calibration must not be used as today's HP setting.

Server/SP diagnostics are passive and scoped to the owning Fight. `CombatStarted` records HP/window tuning. `DamageWindowStarted`, two-second `DamageProgress`, and one `DamageWindowEnded` describe each Pylon/Core window; `PylonDestroyed` records each confirmed kill, and `CombatDpsSummary` totals Core and Pylon damage/open time separately at every ending, including interruption. No per-hit log, player name/position, client-reported DPS, packet change or gameplay decision is introduced.

- `effective_damage` is the authority-observed net target HP reduction, capped to actual target HP; overkill, shields, decorative targets and damage outside the window are excluded. It is party progress, **not per-player attribution or raw weapon DPS**.
- `open_seconds` excludes the Pylon's shield cue and all other phases. `window_dps` averages from that window's opening; `recent_dps` covers only the interval since its previous sample. Time is authority ticks at 60 Hz, not measured wall-clock seconds.
- `target_hp_remaining`, `window_progress_pct`, per-ordinal `pylon_hp`, persistent `boss_life`/`boss_remaining_pct`, and `overload` expose progress. `required_dps_by_deadline` is the remaining target HP divided by remaining open time; it is unavailable after a missed deadline. For the Core this is the rate to finish **in this exposure**, not a mandatory damage quota.
- A Pylon disappears from the live actor list only after its authority-confirmed death; diagnostic state retains zero HP for that ordinal. Missing/unconfirmed actors mark `hp_observation=Incomplete` rather than being counted as kills. Terminal partial windows are recorded before owned actors are removed; repeated cleanup cannot double-count totals. A logging failure cannot interrupt combat or cleanup.

### Stack / 頭割り

- Authority publishes a fixed world-space center above the Boss, plus the resolve tick; no participant is selected to carry the circle. See the current stage section for its ground-relative coordinates.
- At the resolve tick, authority counts connected Alive centers within the inclusive 7-tile radius of that fixed point. Downed/Eliminated/disconnected participants do not count, but the frozen pull roster never shrinks.
- Every pull participant is required: `2 / 3 / 4` occupants for `2 / 3 / 4` players. Full attendance is success and deals **zero damage**.
- Otherwise each connected Alive participant takes `ceil(their maximum HP × (rosterCount - occupants) / rosterCount)` once, including participants outside the circle so abandoning the group does not grant immunity. One missing player is 50% in a two-player pull, about 33.3% in three, or 25% in four. This is direct Raid damage, honoring recovery protection and lethal-to-Down, without armor mitigation.
- An incomplete Stack adds no Overload or extra failure debuff; it advances to Spread unless its damage produces a terminal result. Clients never supply attendance, damage or success.
- A Down does not move the marker or require target reissue. Ordinary death/disconnection still aborts the experiment. This replaces player-following assignment, not the missing-roster damage rule.

### Spread / 散開

- Every connected Alive participant at assignment receives a marker and the same server resolve tick.
- A participant who becomes Downed or invalid before resolution is removed from the required set.
- At the deadline, authority performs pairwise position checks. Current experimental presentation radius is 14 tiles and minimum center separation is 28 tiles: the two visible danger discs must not overlap. Exact equality is safe. Stack retains its separate 7-tile radius.
- Each failed participant receives the failure result once even if overlapping multiple players. Pair iteration order must not multiply damage.
- No overlap means zero damage, including a lone remaining Alive participant. A failed participant loses 70% maximum HP once through the existing Raid damage path.
- Success or soft failure advances to Core exposure.

### Core exposure and victory

#### Drawing-based attack sequences (current tuning 0.2.9)

- Repeated attack during Pylon active windows and Core exposure only. The first cast starts after the Pylon's 90-tick shield cue, or 60 ticks after exposure opens. Stack/Spread/intro/Reset remain beam-free recovery and assignment windows. The 0.2.7 user request restores fast steps within a category while preserving the deliberate category transitions: intro extended to 360 ticks in 0.2.8, Pylon opening cue 90, Stack/Spread 240 each, exposure opening rest 60 and Reset 120. Other major-phase lengths and live travel speed are unchanged.
- Authority selects an Alive focus in frozen-roster round-robin order per sequence. Later steps keep that focus unless unavailable, then choose the next Alive participant. Fixed beams lock their warning. Energy bodies track during harmless windup only; a live charge never changes focus or heading. At most two beam rays or one compact energy body exist globally, not per player.
- **Pylon / Pursuit Prism:** eight angled beams, red → blue → green → yellow → yellow → green → blue → red, with a matching 1–8 HUD count. Each predicts 18 ticks of observed velocity, clamped to ±220 px X / ±160 px Y. Angles from downward vertical are −0.55, −0.23, +0.09, +0.41 radians. Origin is 800 px behind the predicted point; length 2,600 px, full width 88 px. Warning 28 ticks, live 12, cadence 42; one sequence occupies 334 ticks. Each shot leaves two harmless ticks before the next warning.
- **Exposure / Dash–Stillness:** right charge → stop → left charge → stop, with 72-tick step cadence. Steps 1/3 are physical energy bodies, not long horizontal cuts. A 42-tick harmless windup comprises an 18-tick tracking orbit and a 24-tick stationary locked hold. Orbit radii remain 520px horizontal / 330px vertical, repositioning at most 64px/tick; aim leads observed velocity by six ticks capped at ±120px per axis. During tracking, heading turns at most 0.12 radians/tick and settles on the final target at lock. At `FireTick - 24`, origin and heading freeze, bright rails/crossbar and a latch sound signal the hold. Damage begins only at FireTick, at 104px/tick along that frozen heading for 22 ticks. The pre-lock damage fix from 0.2.5 is retained, not reverted with cadence. No live steering, artificial velocity check or player control lock. Engine `immune && immuneTime > 0` remains honored for charges; real Calamity dash compatibility and the shorter anticipation need the user smoke.
- **Stillness steps:** at the current target position, two downward 640-px-wide, 2,600-px-long curtains leave a locked empty 128-px column. Origin is 1,200 px overhead; warning 36 ticks, live 12. A normal stationary player box fits the gap; continuing horizontally enters a curtain. The four-step sequence occupies 264 ticks, fitting even the 720-tick penalized exposure after its 60-tick initial rest.
- The energy body has a 164px tail and 50px half-width. Its damage rectangle includes one tick of travel plus the nose radius (maximum length 318px) to prevent tunnelling at high speed. The exact rectangle has white-edged live drawing. The long dim segmented wake and the entire approach remain harmless. Client extrapolation never chooses a target or collision outcome; a late sample cannot extend the lifetime.
- A new sequence starts only if its complete duration fits the phase; the next sequence retains its extra 60-tick gap. Shorter complete sequences can repeat more times inside the unchanged Pylon/exposure windows. Sequence/step durations derive from the same warning/live constants as the volley, avoiding independent schedule drift. Early Pylon completion/phase exit cancels any pending attack. No catch-up bursts, terrain damage, new NPCs or Projectile slots. Boss casting/recoil animates even for off-body attacks.
- Server/SP samples each Alive participant's observed box against the same bounded descriptor and tick-derived geometry the client draws. In 0.2.9, fixed beams (`ObservationLance`, `PursuitPrism`, `Stillness`) deal **120 direct HP per hit**, independent of maximum HP and armor. The separate contact-style energy charges (`SweepRight`, `SweepLeft`) retain 60% maximum HP. A participant takes at most one hit per step, including both curtains. Recovery protection, Raid-owned lethal-to-Down and outsider exclusion are unchanged. Stack/Spread/Pylon penalties retain their own percentage rules; the beam change does not change Raid attendance mechanics.
- The exact-Fight runtime owns the current assignment and hit ledger. Full snapshots carry one bounded read-only volley; client sound, muzzle bloom, particles and camera shake are non-authoritative. Phase exit and every existing Fight cleanup discard the volley/ledger. No extra NPC or Projectile slot is allocated.

#### Exposure rules

- Exposure changes the Boss visual state to `Exposed` and opens the authoritative damage gate.
- A clean Pylon check grants the normal 1080-tick window; a failed check grants 720 ticks.
- Boss life is one persistent authority-owned pool. Any provisional ring or arm visuals are not hitable parts.
- On each authority tick, permitted hits and life changes resolve before the deadline closes. At 50% in Phase I, transition takes priority over Reset; at zero in Phase II, Victory takes priority over another grid window or the cap.
- If Phase-I HP is above 50% at the deadline, close the damage gate, clear loop assignments, run `Reset`, and begin another Pylon check. Phase II instead retains its open gate across scheduling windows.
- Eventual balance target: a clean representative group should need roughly 4–6 successful exposures. The deliberately excessive `0.2.1` pass does not claim that target or even practical clearability within the existing eight-exposure cap.

### Downed and recovery interaction

Downed/Revive follows [Revive Specification](../encounters/first-severance/REVIVE_SPEC.md) and ADR-0011; the latter supersedes ADR-0005's held channel/shared tokens for this feature.

- Downed participants cannot attack, move normally, use items, hook, mount, take encounter damage, or be selected for Pylons/Stack/Spread/Boss targeting.
- An Alive participant may use the nonconsumed kit within 8 tiles in every `Active` substate. Authority resolves it instantly; flight/movement does not cancel a channel because no held channel exists. The recipient gets 35% health, 3-second protection and a 60-second cannot-receive-revival deadline; it does not stop that player rescuing someone else. No shared tokens or damage-weakness debuff apply.
- The frozen pull roster does not shrink. A revived participant returns to the same stable Participant ID.
- If every current participant becomes Downed in the same committed authority tick, Revive produces one Defeat candidate; absent a same-tick Boss Victory, the encounter ends in Defeat once and later revive input cannot undo it.
- On committed **Defeat**, every connected participant receives one ordinary Terraria death, after Raid Down/protection is cleared. Nonparticipants are excluded. The owner invokes `Player.KillMe` from the accepted exact-Fight terminal; normal engine respawn, death synchronization and character-difficulty penalties apply, including Hardcore character loss. No forced death on Victory, cancellation, invalidation or unload. Other Mods' `PreKill` hooks are not bypassed; a cancellation is logged and remains a runtime compatibility check.

### Authority tick and terminal precedence

The feature runtime settles one authority tick in this order:

1. collect the complete bounded client-intent batch and server-observed Terraria hit, position, connection/epoch, and control facts without committing a phase edge;
2. validate exact-Fight actor ownership/damage gates and apply accepted Boss/Pylon hit results; collect Boss/Pylon terminal candidates and any participant lethal facts, but do not resolve a due Stack/Spread yet;
3. apply pre-mechanic participant lethal transitions and final observed connection/epoch facts in stable Participant-ID order; the current instant feature does not collect channel interrupts;
4. sample the resulting connected Alive set and server positions, resolve due Pylon/Stack/Spread/exposure edges and active lances once, and apply mechanic-created lethal transitions before revival requests;
5. revalidate the one complete, stably ordered revive-start batch after all invalidations (sender, held kit, nearest eligible target/range/lockout), then call `RaidReviveService.CommitTick` exactly once; instant completions and same-tick failure settle there;
6. gather actor/invariant, Boss-life, Overload/loop-cap, and Revive terminal candidates observed by the feature and choose exactly one by the feature priority below;
7. if no terminal exists, commit at most one nonterminal substate edge; otherwise store the generic end reason and bounded feature terminal cause and return one direct End descriptor;
8. publish one coherent feature/generic terminal projection and tombstone before cleanup releases actors or player projections.

This ordering makes a target that becomes Downed or disconnected exactly on a Stack/Spread resolve tick invalid **before** that mechanic samples participants, while mechanic damage can still create an all-Downed Defeat candidate in the same single commit. Boss HP reaching zero still wins a gameplay Defeat candidate, including all-Downed or timeout. A nested Revive failure may remain diagnostic state but never publishes a second `EncounterEnded`.

Feature-owned candidates use this complete priority:

```text
EncounterActorMissing
  > AnchorDestroyed
  > Invalidated (including explicit AdministrativeAbort)
  > Victory
  > Defeat
  > Cancelled
  > nonterminal substate edge
```

Safety/validity endings therefore override a coincident gameplay result whose authority can no longer be trusted. `Cancelled` is accepted only during its declared preparation state; an active-fight cancel request is rejected rather than competing with Victory/Defeat.

`WorldUnload`, an unhandled `InternalFailure`, and a fatal `ProtocolFailure` originate outside this reducer and unconditionally preempt an uncommitted feature result in that order. The coordinator synthesizes their generic/cause pair from the immutable mapping registered with the encounter definition; it does not re-enter a failed feature tick. The implemented external-termination bridge publishes the combined terminal projection before cleanup. Feature replication and activation remain disabled until their later Core/roster/actor/transport gates pass.

### Player-count rules

| Pull roster | Pylons | Revival limit | Stack | Spread |
|---:|---:|---:|---|---|
| 1, development flag only | 2 | first Down ends Raid | 1 actual member required | no pair to overlap |
| 2 | 2 | recipient-only 60 s | 2 required shares | all connected Alive assigned |
| 3 | 3 | recipient-only 60 s | 3 required shares | all connected Alive assigned |
| 4 | 4 | recipient-only 60 s | 4 required shares | all connected Alive assigned |

The encounter does not scale by Boss HP alone. Pylon count, actor density, safe space, and tuning may vary by roster, but the sequence and authority rules remain identical.

### Terminal outcomes and feature cause

The feature snapshot/tombstone carries both the existing generic `EncounterEndReason` and an explicit byte-valued `FirstSeveranceTerminalCause`. Values are append-only and never renumbered: `None=0`, `BossLifeZero=1`, `OverloadLimit=2`, `AllParticipantsDowned=3`, `RecoveryImpossible=4`, `LoopCapExceeded=5`, `UserCancelled=6`, `FoundationCoreLost=7`, `BossActorMissing=8`, `RuntimeInvariantBroken=9`, `AdministrativeAbort=10`, `WorldUnload=11`, `ProtocolFailure=12`, and `InternalFailure=13`. This preserves a bounded, localizable cause such as `LoopCapExceeded` even though the generic reason is only `Defeat`.

| Cause | Generic end reason | Feature terminal cause |
|---|---|---|
| Boss HP reaches zero during exposure | `Victory` | `BossLifeZero` |
| Third Overload | `Defeat` | `OverloadLimit` |
| All participants Downed at one committed tick | `Defeat` | `AllParticipantsDowned` |
| Revive-domain timeout with no accepted recovery path | `Defeat` | `RecoveryImpossible` |
| Provisional loop cap reached with HP remaining | `Defeat` | `LoopCapExceeded` |
| User cancel during allowed preparation state | `Cancelled` | `UserCancelled` |
| Foundation Core Tile/TE unexpectedly lost | `AnchorDestroyed` | `FoundationCoreLost` |
| Required Boss actor missing | `EncounterActorMissing` | `BossActorMissing` |
| Runtime invariant broken | `Invalidated` | `RuntimeInvariantBroken` |
| Explicit admin/debug abort | `Invalidated` | `AdministrativeAbort` |
| World unload | `WorldUnload` | `WorldUnload` |
| Protocol failure | `ProtocolFailure` | `ProtocolFailure` |
| Unhandled internal failure | `InternalFailure` | `InternalFailure` |

Normal player attempts to break the Foundation Core during Active are rejected by world protection and are not a terminal event. An explicit admin/debug abort uses the ordinary Abort cleanup route. All terminal paths publish a final snapshot before releasing exact-Fight ownership. Cleanup is idempotent and removes Boss, Pylons, encounter projectiles, mechanic assignments, Barrier/player projections, revive channels, and Foundation Core busy state when the Tile/TE still exists.

### Telegraph and fairness requirements

- Every mechanic has a server resolve tick and redundant shape/motion/text or sound language; never color-only.
- Client clocks interpolate presentation only. Latency must not move the authority deadline.
- Eventual balance should make a single ordinary Stack/Spread error recoverable and readable; the current intentionally excessive damage is not accepted final balance.
- No frame-perfect input, invisible off-screen hit, required class, or fourfold projectile multiplication.
- Damage numbers, telegraph radii, and exact HP remain provisional until 2/3/4-player Windows telemetry exists.

### First-slice exclusions

Part Break, the separate targeted-bait mechanic/phase from the historical backlog, Personal Effigies, Split Reality, Last Stand, multiple Boss parts, route selection, finished rewards and final music remain deferred. The explicitly requested giant first-pass art and repeated observation lances above are now in the development scope; they do not import the remaining [Backlog](../encounters/first-severance/BACKLOG.md).

## Optional search-design sketch (not implemented)


Markdown and YAML produce reviewable diffs, survive branch merges, work offline,
and are readable by both people and coding agents. SQLite, embeddings, or a local
full-text/semantic index may be generated from the catalog later, but it is
disposable cache. Store it below the dedicated ignored cache directory, for
example `docs/.cache/docs-index.sqlite`; never make a binary index the only copy
of knowledge.

Recommended future local tables, if search volume justifies them:

| Table | Key fields | Source |
|---|---|---|
| `documents` | `doc_id`, path, type, status, reviewed date, content hash | front matter and normalized full document |
| `topics` | topic key, owning `doc_id` | `source_of_truth_for` |
| `aliases` | composite primary key `(normalized_alias, doc_id)` | `aliases` |
| `relations` | from `doc_id`, to `doc_id`, fixed relation `related` | `related_docs` |
| `chunks` | `doc_id`, heading, text hash, optional embedding | generated Markdown sections |

One compact optional SQLite schema is:

```sql
PRAGMA foreign_keys = ON;

CREATE TABLE index_meta (
    singleton INTEGER PRIMARY KEY CHECK (singleton = 1),
    schema_version INTEGER NOT NULL,
    source_commit TEXT NOT NULL CHECK (length(source_commit) = 40)
);
CREATE TABLE documents (
    doc_id TEXT PRIMARY KEY,
    path TEXT NOT NULL UNIQUE,
    document_type TEXT NOT NULL,
    status TEXT NOT NULL,
    last_reviewed TEXT NOT NULL,
    content_sha256 TEXT NOT NULL CHECK (length(content_sha256) = 64)
);
CREATE TABLE owners (
    doc_id TEXT NOT NULL REFERENCES documents(doc_id) ON DELETE CASCADE,
    owner TEXT NOT NULL,
    PRIMARY KEY (doc_id, owner)
);
CREATE TABLE topics (
    topic TEXT PRIMARY KEY,
    doc_id TEXT NOT NULL REFERENCES documents(doc_id) ON DELETE CASCADE
);
CREATE TABLE aliases (
    normalized_alias TEXT NOT NULL,
    doc_id TEXT NOT NULL REFERENCES documents(doc_id) ON DELETE CASCADE,
    PRIMARY KEY (normalized_alias, doc_id)
);
CREATE TABLE relations (
    from_doc_id TEXT NOT NULL REFERENCES documents(doc_id) ON DELETE CASCADE,
    to_doc_id TEXT NOT NULL REFERENCES documents(doc_id) ON DELETE CASCADE,
    relation TEXT NOT NULL DEFAULT 'related' CHECK (relation = 'related'),
    PRIMARY KEY (from_doc_id, to_doc_id, relation)
);
CREATE TABLE chunks (
    doc_id TEXT NOT NULL REFERENCES documents(doc_id) ON DELETE CASCADE,
    ordinal INTEGER NOT NULL CHECK (ordinal >= 0),
    heading TEXT NOT NULL,
    body TEXT NOT NULL,
    chunk_sha256 TEXT NOT NULL CHECK (length(chunk_sha256) = 64),
    PRIMARY KEY (doc_id, ordinal)
);
```

Normalize aliases in the generator with Unicode NFKC, case folding, and
whitespace collapse before insertion; the composite key intentionally permits
one alias to find more than one document. FTS5 may be generated over
`chunks.heading` and `chunks.body` when the local SQLite build supports it.

Before opening the cache for search, run the catalog check. Rebuild the cache in
one transaction when `index_meta.schema_version` differs from the generator
schema, `index_meta.source_commit` differs from the current 40-character
`HEAD`, or the set of `(doc_id, path, content_sha256)` rows differs from
`documents.yml`. This also invalidates a cache for checked but uncommitted
document edits through `content_sha256`. Never write tokens, credentials,
private logs, or absolute personal paths.
