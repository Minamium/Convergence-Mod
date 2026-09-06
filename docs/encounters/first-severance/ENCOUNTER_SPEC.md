---
doc_id: encounter.first-severance.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-07
source_of_truth_for:
  - first_severance.encounter_loop
  - first_severance.mechanics
  - first_severance.terminal_outcomes
aliases:
  - First Severance
  - 第一断絶
  - Pylon Stack Spread Core
related_code:
  - Content/Encounters/FirstSeverance
  - Common/Raids/Revive
related_docs:
  - encounter.first-severance.plan
  - encounter.first-severance.visual
  - encounter.first-severance.revive
---

# First Severance Encounter Specification

## Development experiment override

The separate two-client assistance in [ADR-0015](../../adr/0015-console-only-single-pull-assist.md) permits a dedicated-server-console-authorized real auxiliary client to receive one-Fight invulnerability. That permission does not itself lower admission requirements or auto-Ready/revive. Preparing for a valid assisted lease allows ten minutes for window switching; normal preparation remains sixty seconds. The protection applies only during combat and clears before Defeat death. Ordinary players have no in-game grant command. The newer one-client debug path below needs no auxiliary or protection; [the runbook](../../runbooks/SINGLE_OPERATOR_TESTING.md) owns both procedures.

The user-requested `0.2.0` expansion in [ADR-0010](../../adr/0010-giant-boss-observation-lances.md) adds the original giant Boss presentation and observation lances. `0.2.1` deliberately over-tunes stats and accelerates attacks/presentation; [ADR-0011](../../adr/0011-instant-revival-and-recipient-lockout.md) replaces held revival/shared tokens with an instant reusable kit and recipient-only 60-second lockout. These changes preserve one logical Boss life pool, authority and release boundaries. All damage/timing numbers are provisional, not a guarantee of a fair or clearable fight.

The user-authorized experiment in [ADR-0009](../../adr/0009-development-combat-experiment.md) runs before production integration is complete. Current `0.2.12` HP follows the frozen-roster table below; Boss/Pylon defense remains **240 / 120**. Stack uses the missing-roster fraction below. Spread overlaps receive 70% maximum HP once; failed Pylons pulse 35%, clamped nonlethal. Both Stack and Spread cost zero HP on success. General Terraria lethal events are not intercepted. [ADR-0012](../../adr/0012-pattern-sequences-and-defeat-death.md) adds independent-origin attacks, the introduction and normal participant death on Defeat. [Status](../../STATUS.md) owns implementation and test results.

## One-member development start and results — 0.2.14

While the server/SP build's `ConvergenceDevelopmentSolo` flag is enabled, a single eligible real player may activate the existing Core and manually Ready, including ordinary Host & Play with no guest. The flag defaults on during development and must be compiled off before public release; [ADR-0020](../../adr/0020-development-solo-admission-and-terminal-hud.md) owns admission, wire bounds and release safety. All Core/field/progression checks still apply. The preparation message explicitly identifies SOLO DEBUG. No NPC, extra window, invulnerability, automatic revival or persisted player/world setting is introduced.

Initial admission and the fresh Ready-to-combat scan use the same non-optional compiled policy. Revalidation still rejects a removed Core, newly blocked field or world conflict; it must not reintroduce a separate minimum-two default. Current implementation and observed regressions are recorded in [Status](../../STATUS.md).

Solo debug uses **Core HP 5,000,000, two Pylons at 300,000 HP each**, the existing two-player workload. Attack sequence, geometry, damage, timers, phase HP gates, infinite Raid flight and Final survival are unchanged. Targeting uses the sole real player. Stack uses the normal missing-roster fraction: one present out of one means zero damage; missing the fixed marker means the full fraction. Spread still resolves all pairs; with one participant there are no overlaps. These natural count-dependent outcomes are not a dedicated solo redesign or a multiplayer balance claim. Two/three/four-member HP tables and behavior remain unchanged.

If the sole participant becomes Downed, the ordinary authority commit immediately ends the Raid as AllParticipantsDowned/Defeat; it does not wait for an impossible solo revival. Existing participant death and exact-Fight cleanup follow. This does not bypass an external Mod's death-cancelling hook. Public-release minimum remains two until a separate solo/companion design is approved.

Accepted **Victory and Defeat** now replace the participant's HUD with a bounded result cinematic, also over the local death screen. Gameplay cleanup and player death are immediate, not delayed for presentation. Duration, camera, typography and effects are owned by the [visual spec](VISUAL_SPEC.md#success-and-failure-designations--0214). Cancelling or unloading is not a success/failure cinematic. The start log records `solo_debug` and the compiled admission flag so one-member diagnostics cannot be confused with multiplayer calibration.

## Ordered phase scores and terminal survival — 0.2.13

The phase structure originated in0.2.13; attack tuning in this section is updated for0.2.17. [ADR-0021](../../adr/0021-untimed-recovery-and-simultaneous-prism.md) removes Eliminated/Down expiry while retaining the60-second recipient restriction and immediate all-Down Defeat.

This section supersedes the earlier immediate 50%-HP transition, grid-only Phase II and HP-zero Victory. The frozen 2/3/4-player HP table, defense, Pylon tuning, zero-damage full Stack/safe Spread, instant reusable revival and existing defeat/cleanup rules remain. [ADR-0019](../../adr/0019-phase-scores-and-terminal-survival.md) owns authority and replication. Final means surviving all required actions, **not** a no-hit challenge; ordinary fixed damage, Down and revival remain available.

| Stage | Full action list required before first exit | HP floor / next stage |
|---|---|---|
| I / Sealed | Existing opening Pylon → fixed Stack → Spread → full Core exposure; then four clockwise Stack sites, with Spread after each | 50% → six-second Unbound eclosion |
| II / Unbound | Grid block → Spread → rotating blade → Spread → grid block → Spread | 25% → five-second distant retreat |
| III / Distant | Three expanding horizontal floods → left-half beam → Stack → Spread → right-half beam → three expanding horizontal floods → Stack → Spread → central hand crush | 0% → four-second Final designation |
| Final | Eight clockwise Stack sites; each followed by Spread, then alternating bullet rain / slicing comb | HP remains 0; one full accelerating68.567-second score → Victory |

The first full Core exposure is never interrupted by reaching 50%. After it, four sites proceed top → right → bottom → left around the arena center. Every site is a **stationary world coordinate for that action**, not a following player or moving damage disc. Current valid radius remains 112px; only that radius is circular, with directional chevrons and ordinal label. Ellipse radii are 560px horizontally and 300px vertically. Final uses the same ellipse with eight 45-degree positions. Distant's two assemblies use the top and bottom sites.

Damage clamps at the next stage's floor, including lethal NPC hits; overkill is discarded. At a floor the aperture closes and HUD shows HP LOCK while attacks continue. The score's first completion releases a pending transition. If the floor was not reached on that cycle, another cycle begins; later threshold arrival exits at a damage-action deadline, since the required full cycle has already been demonstrated. Nothing can skip the first score or kill the Boss before Final. Logical HP zero is replicated; the temporary NPC's technical one HP is not a second health bar. Final is not counted as an ordinary numbered phase, so future IV/V can be inserted before it. The old eight-exposure hard cap is not applied to this staged score; Pylon Overload and recovery failure still apply.

### Travel and spread tuning

Spread radius remains **320px**; two centers must be at least **640px / 40 tiles** apart. Overlap still costs70% maximum HP once per affected player; no overlap costs zero. Initial opening Stack/Spread retain four seconds. Non-Final score Stack remains150 ticks, first site180; Spread180. Final uses zero-based station `i=0..7`: Stack180 at first, then`150-3i` (147→129); Spread`180-6i` (180→138); dodge block`240-8i` (240→184). Thus each successive station tightens every category without shrinking the actual marker or changing shares. The full score is4,114ticks; category entry/phase cinematics outside Final are unchanged.

These are estimates, not verified equipment-independent dodgeability: assume sustained10px/tick movement,30 ticks reaction and6 ticks settling/network margin. Four example Spread stations at arena-center offsets(±330,±330) have660px minimum separation. From every example station, `(distance-112)/10+36` fits each particular next Stack deadline, including the accelerated Final. Real turning, latency and gear reduce margin; user playtesting remains necessary. Infinite equipped-wing/rocket flight is unchanged.

### New attack modules

- **Grid blocks:** 450 ticks each, with existing 60-tick opening rest and warning/live/cadence rules; Spread occupies the gap between blocks. The global grid serial continues, and from serial 3 the existing per-standing-player 144px Core lasers remain. Grid/Core share their original one-hit-per-volley ledger.
- **Simultaneous opening Prism:** each of eight fast steps locks one predicted ray for every currently Alive connected participant, all on one authority tick. Red→blue→green→amber then reverse retains28 warning/12 live ticks and42-tick step cadence. There is no per-player turn to wait for. Whole-combo/category rests remain; charges and Stillness are unchanged. One fixed120 hit maximum per player per step even where several targeted rays cross. `LanceTelegraph` logs bounded`target_slots` and ray count.
- **Rotating blades:** two opposing88px-wide rays reach the field edge. Harmless windup180, rapid unsheathing144–156 and24 ticks of full-length warning remain. Motion is now **two clockwise revolutions over300ticks**, with angular progress`2*(0.55t+0.45t²)` turns: mean speed1.6× the preceding one-turn attack, and continuously accelerating. Peak tangential speed at radius180 is10.93px/tick; move farther inward for more margin. Recovery60 ticks is harmless. One fixed120 hit per blade **per revolution**; distinct turn/pulse IDs prevent the first turn's hit cap making the second harmless. Prior-tick swept angle prevents tip tunneling. Smooth damage-zone aura replaces hard side rails.
- **Expanding horizontal floods:** `RemoteClaws` remains the stable state ID,600 ticks/three200-tick pulses. A short48-tick smooth arm charge forecasts two full-width horizontal bands and one genuine safe strip. At48, thin beams deploy across the2560px field over12 ticks; their authoritative half-width accelerates to the forecast band over42 more ticks with`g(t)=t³(4-3t)`. Final full widths hold until142;142–180 cooling is harmless. The safe strip is192px high, centered at relative Y−260,+200,−40, rotated by one site for the later block. The two bands fill everything above/below it, including the sides; a42px-high player has150px center clearance. Launch side alternates left/right. One fixed120 hit cap per player per whole pulse, not separately for each band. Body rig, forecast, growing surface and collision share the same schedule; no player-velocity rule or new world actor.
- **Half-field beam:** 180-tick large charge of the entire selected half, 60 live ticks, 60 recovery ticks. First is left, second right; each uses exactly half the 2560x1120 field. One fixed120 hit maximum. The complete player body must leave the boundary, not just its center. The opposite half is safe from this action.
- **Central hand crush:** new final action of the Distant score,300 ticks. The fixed720×600px rectangle at the field center is warned from entry. Both giant hands brace for150 ticks and close inward in12 ticks; only ages162–179 are lethal. Side and upper/lower pockets outside the marked rectangle remain safe; the approaching arms themselves do not damage. Contact routes through existing lethal-to-Down recovery (not permanent death or a new death hook), retaining post-revive immunity and authorized debug protection. One hit ledger entry per participant; retract and all remaining recovery are harmless. It allocates no gameplay hand NPCs or new requests.
- **Terminal bullet rain:** five waves of24 beads, with24-tick per-wave appearance, within the shrinking dodge block. Normalized terminal progress`p=i/7` sets first release`36-floor(8p)`, wave interval`32-floor(10p)`, speed`10+3.5p`px/tick. Tangential displacement reaches240px; lifetime is`115/(speed/10)` or the action deadline. Radius12px, swept body checks and one fixed120 hit per wave remain. No hazard/ledger survives into the next Stack.
- **Terminal slicing comb:** six alternating-axis pulses. Cadence`40-round(12p)` ticks, harmless warning`ceil(cadence*.7)`, live until`cadence-4`, then four harmless recovery ticks.56px full width and160px pitch retain104px open lanes; boundary teeth are included (at most17 vertical/eight horizontal). Offset is`((floor(pulse/2)*53 + i*37) mod160)-80`px. Each repeated axis shifts53px rather than sharing a fixed parity, so no stationary whole-player position remains safe through its three shots. Every complete pulse fits the shortened action. One fixed120 hit per pulse; axes never fire simultaneously.

All new hazards respect Alive membership, bound slot/epoch, post-revival immunity and the existing Raid-owned lethal-to-Down adapter. They do not overlap the prescribed Stack/Spread deadlines: the previous action expires before the next one begins. Last-stage damage cannot cause an early win. Administrative abort, cancel, unload and existing recovery failure preempt normal progression according to the unchanged terminal contract.

Protocol16 requires matching peers for the changed deterministic timing, simultaneous Prism bound and untimed Down interpretation. Stable substate/packet IDs, request shapes and saved data are unchanged. The [ADR-0019](../../adr/0019-phase-scores-and-terminal-survival.md) score/authority/cleanup model remains. Stack sync/112px acceptance, Spread640 separation, frozen HP, instant item use and60-second recipient lockout are unchanged.

`PhaseChanged` now records action index, completed phase cycles and resolve tick. `HpGateReached` distinguishes HP waiting from a stuck fight; `ScoreAttackFired` identifies new ray pulses; `SpreadResolved` logs alive/overlapping counts. Core telemetry now measures the **damageable segment above its phase floor**, closing on HpGateReached so mandatory waiting does not dilute DPS; the full `boss_life` remains separate. Previous reports using the whole persistent pool must be interpreted with their recorded build.

## Forgiving roster HP and Core salvos — 0.2.12

The accepted roster chooses one immutable `FirstSeverancePartyScaling` at combat creation. No live-player count, Down, elimination, reconnect, difficulty multiplier or client DPS report recalculates it. Pylons in subsequent loops use that same tuning; the Boss still has one persistent pool.

| Frozen participants | Boss/Core HP | Each Pylon HP | Total Pylon pool | Required party Pylon DPS in 14 s |
|---:|---:|---:|---:|---:|
| 2 | 5,000,000 | 300,000 | 600,000 | 42,857 |
| 3 | 9,000,000 | 500,000 | 1,500,000 | 107,143 |
| 4 | 13,000,000 | 600,000 | 2,400,000 | 171,429 |

Calibration uses the preceding two-player clear's Core/Pylon effective rates (76,287 / 81,081 DPS) and three-player clear's rates (199,501 / 233,161 DPS), not raw equipment damage. At those rates, two-player Core damage requires about 65.5 open seconds and a Pylon pool 7.4 seconds; three-player Core about 45.1 seconds and a Pylon pool 6.4 seconds. These deliberately leave development margin. These are damageable-time estimates, not predicted full fight durations: shield/mechanic/transition time and reduced output while dodging are separate. The builds and phase structures differed; the measured ratio is not a universal per-extra-player factor. Four-player tuning is unmeasured extrapolation. Sanitized observations and limitations live in [the check record](../../evidence/2026-09-06-eclosion-salvos-checks.json).

From **grid volley serial 3 onward**, each warning also locks one Boss-origin laser toward each currently connected Alive participant. The serial continues across Phase-II scheduling windows; Downed/Eliminated participants are not new targets. Origin is the Boss's fixed damage Core, length **3,000 px**, full damage width **144 px**. Direction freezes for the entire **60-tick warning and 20-tick live window**; no live homing, new projectile actor or player-speed test. Grid and Core lasers share one per-participant hit ledger: **one fixed 120-HP hit maximum for the whole volley**, including multiple intersections. A player may dodge both by leaving the locked corridor and choosing a grid cell. All old cleanup/recovery/timing rules remain.

Protocol **v12** appends a bounded 0–4 count and unit-direction pairs to the existing grid descriptor; source and dimensions derive from shared constants. All peers update together. Actor `SyncNPC` extra AI carries the frozen count and bounded current life so local HP bars match the authority pool, including a full-life spawn. No client request or saved field is added; [ADR-0018](../../adr/0018-roster-health-and-core-salvos.md) owns this extension. `CombatStarted` records `hp_policy=FrozenRosterDevelopment`; grid logs include `core_beams`, firing damage/cap, and hits distinguish `source=CoreSalvo` from `source=Lattice`.

## Boss stages and fixed assembly — 0.2.11

The existing Pylon → Stack → Spread → Core-exposure cycle is **Phase I / Sealed**. The single Boss is visually enclosed in a damaged spherical restraint. Core HP is one persistent roster-scaled pool, not separate phase health bars (the original 0.2.11 pool was fixed at 4,000,000). During a Phase-I exposure, the first authority observation reaching **50% of that pool** enters `PhaseTransition`. Excess damage is clamped at this floor, including a normally lethal NPC hit; the transformation cannot be skipped by one large hit.

`PhaseTransition` lasts **360 ticks / 6 seconds**, shields the Core and cancels outgoing attacks. It keeps the exact Fight, frozen roster, recovery deadlines and containment/flight capability. Clients focus the camera on the Boss, temporarily omit normal HUD layers and show limbs prying the shell open, followed by emergence and unfolding. No persistent input lock, zoom or UI flag is written. The feature-local ordered stage plan selects the next attack module; future stages can append a definition/module without introducing Boss-specific branches in the global coordinator. [ADR-0017](../../adr/0017-boss-stages-fixed-stack-and-lattice.md) owns this structure and its original protocol v11; the v12 extension is above.

**Phase II / Unbound** opens the Core continuously and replaces the Pylon/Stack/Spread cycle with `Lattice`, a personal-dodge phase. Vertical and horizontal beams span the entire field, at **160 px / 10-tile spacing**, **24 px full damage width**, leaving 136-px clear cells. Four deterministic patterns shift the grid in 40-px increments; horizontal/vertical shifts differ. One descriptor derives at most 32 rays (21–23 in the current field), not a grid of NPCs/projectiles. Every volley has **60 harmless warning ticks, 20 live ticks**, with starts at least **102 ticks apart** and an additional 60-tick rest after every fourth volley. First warning starts 60 ticks after a window opens; casts are never truncated or caught up in a burst.

Each Alive participant intersecting any live line takes at most **one fixed 120-HP hit per volley**, even at a crossing. Recovery protection and Raid-owned lethal-to-Down are retained; no player-velocity condition or shared attendance check exists in Phase II. Remaining in a safe cell is harmless. The phase is divided into 18-second scheduling/telemetry windows without healing or returning to Phase I. The existing cap of eight completed damage windows counts both phases; a threshold-interrupted Phase-I exposure is not completed. Zero final-stage HP wins before the deadline/cap check.

Stack is now a **fixed assembly site**, 880 px above the foundation's ground center (320 px above the Boss Core), with its existing 112-px / 7-tile inclusive radius. The snapshot sends the actual `StackX/StackY`, never a player slot. In 0.2.12, only this valid radius is circular; exterior gathering arrows, a straight countdown bar and a localizable instruction mark the place. No inner shrinking circle or outer circular result wave suggests a different radius. Its location does not move when a participant moves or becomes Downed. Attendance and penalties below are otherwise unchanged; Spread is unchanged.

Authority diagnostics add `boss_phase` / `phase_start_tick` to phase changes, `GridTelegraph` / `GridFired`, `source=Lattice` damage, and `StackAttendance` with per-slot distance/inside/alive state at resolution. Damage windows distinguish `Lattice` from `CoreExposure` and end the threshold exposure with `outcome=PhaseTransition`. Logs do not infer a hit from decorative effects or assign weapon DPS to individual players.

## Grounded containment field — current 0.2.11

New Foundation Core placement uses a 12x4-tile plinth, anchored on supporting ground/platforms with clear solid-block airspace 80 tiles to either side and 70 tiles above its bottom. Placement preview shows the intended **160x70** rectangle and rejects solid obstructions; server activation and all-Ready recheck the actual arena without terrain mutation. This halves each dimension of the preceding 320x140 field (one quarter of its area), not merely its area. Old 2x2 Cores remain readable/clickable; mine and replace to obtain the larger foundation. The ground under the plinth is the field floor, not the old floating center.

During Active, the complete participant body is confined to that rectangle, including dash, knockback and teleport movement. Server authority and owning-client prediction use the same boundaries; other players are unaffected in this development pass. Connected standing participants gain inexhaustible equipped wing/rocket flight; without wings, holding Jump provides field-supported lift. Downed/Eliminated participants cannot fly. End/cancel/disconnect/unload revoke the capability. This is temporary Raid state, not saved equipment or a public cheat toggle. [ADR-0016](../../adr/0016-ground-containment-and-continuous-emission.md) owns authority, protocol v10 and legacy-tile compatibility.

The Boss aperture is at field center, **560 world pixels** above the plinth ground. Pylons fit around that airborne center. Steel columns, central iris and the outer seal deploy over the six-second safe intro. For participants, the world outside the field is opaque black, including foreground objects/projectiles; HUD remains above the mask outside cinematic windows. The mask derives from the same bounds under camera movement, zoom and UI scale, and persists in Reduced Effects. It disappears with the local combat lease/cleanup; outsiders retain ordinary world rendering. [Visual spec](VISUAL_SPEC.md) owns presentation.

## Identity and scope

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

## Decision boundary

Accepted direction is the ordered Pylon → head-split Stack → Spread → Core-exposure loop, multiplayer-first 2–4-player scope, a simple one-NPC Boss, server-owned encounter results, and ally-item Downed/Revive. The exact one-Pylon-per-roster rule, timings, marker sizes, Stack share count, damage values, Overload threshold, exposure penalty, HP, loop cap, and silhouette components are **provisional prototype policy** until Windows multiplayer evidence is reviewed. They are written here so the first implementation is deterministic, not because minami approved them as final balance.

In this document, **Foundation Core** means the activation Tile/Tile Entity. **Boss Core** means the single Boss life pool exposed during `CoreExposure` or Phase-II `Lattice`.

## Lifecycle and active loop

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

## Provisional timing table

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

## Pylon DPS check

- The provisional first prototype spawns one authority-owned Pylon NPC per frozen pull roster member: 2, 3, or 4.
- Any connected Alive participant may damage any Pylon. There is no player-to-Pylon affinity.
- Count and health are fixed from the pull roster and do not rescale after a participant becomes Downed or disconnects. Remaining players may cover unfinished Pylons.
- Suggested symmetric placement is left/right for two, triangle for three, and quadrants for four. Exact positions must derive from the server-resolved arena layout.
- Server-owned entity damage/life decides success. Clients never submit DPS totals or success.
- Destroying every Pylon advances immediately to Stack.
- At deadline, remaining Pylons are removed through encounter ownership; failure adds one `Overload`, applies a survivable raid-wide pulse, and marks the later exposure as penalized.
- `Overload == 3` causes Defeat. One Pylon failure is a recoverable soft failure.

Pylon HP is a typed feature tuning value. Set it from Windows telemetry so a clean party has meaningful margin; do not encode a client-reported or class-specific DPS requirement.

### Provisional HP calibration and authority diagnostics — 0.2.10

Historical 0.2.10 tuning was a fixed 4,000,000-HP Core and 250,000 HP per Pylon. It has been superseded by the 0.2.12 frozen-roster table above. The passive diagnostic definitions below remain current; the old calibration must not be used as today's HP setting.

Server/SP diagnostics are passive and scoped to the owning Fight. `CombatStarted` records HP/window tuning. `DamageWindowStarted`, two-second `DamageProgress`, and one `DamageWindowEnded` describe each Pylon/Core window; `PylonDestroyed` records each confirmed kill, and `CombatDpsSummary` totals Core and Pylon damage/open time separately at every ending, including interruption. No per-hit log, player name/position, client-reported DPS, packet change or gameplay decision is introduced.

- `effective_damage` is the authority-observed net target HP reduction, capped to actual target HP; overkill, shields, decorative targets and damage outside the window are excluded. It is party progress, **not per-player attribution or raw weapon DPS**.
- `open_seconds` excludes the Pylon's shield cue and all other phases. `window_dps` averages from that window's opening; `recent_dps` covers only the interval since its previous sample. Time is authority ticks at 60 Hz, not measured wall-clock seconds.
- `target_hp_remaining`, `window_progress_pct`, per-ordinal `pylon_hp`, persistent `boss_life`/`boss_remaining_pct`, and `overload` expose progress. `required_dps_by_deadline` is the remaining target HP divided by remaining open time; it is unavailable after a missed deadline. For the Core this is the rate to finish **in this exposure**, not a mandatory damage quota.
- A Pylon disappears from the live actor list only after its authority-confirmed death; diagnostic state retains zero HP for that ordinal. Missing/unconfirmed actors mark `hp_observation=Incomplete` rather than being counted as kills. Terminal partial windows are recorded before owned actors are removed; repeated cleanup cannot double-count totals. A logging failure cannot interrupt combat or cleanup.

## Stack / 頭割り

- Authority publishes a fixed world-space center above the Boss, plus the resolve tick; no participant is selected to carry the circle. See the current stage section for its ground-relative coordinates.
- At the resolve tick, authority counts connected Alive centers within the inclusive 7-tile radius of that fixed point. Downed/Eliminated/disconnected participants do not count, but the frozen pull roster never shrinks.
- Every pull participant is required: `2 / 3 / 4` occupants for `2 / 3 / 4` players. Full attendance is success and deals **zero damage**.
- Otherwise each connected Alive participant takes `ceil(their maximum HP × (rosterCount - occupants) / rosterCount)` once, including participants outside the circle so abandoning the group does not grant immunity. One missing player is 50% in a two-player pull, about 33.3% in three, or 25% in four. This is direct Raid damage, honoring recovery protection and lethal-to-Down, without armor mitigation.
- An incomplete Stack adds no Overload or extra failure debuff; it advances to Spread unless its damage produces a terminal result. Clients never supply attendance, damage or success.
- A Down does not move the marker or require target reissue. Ordinary death/disconnection still aborts the experiment. This replaces player-following assignment, not the missing-roster damage rule.

## Spread / 散開

- Every connected Alive participant at assignment receives a marker and the same server resolve tick.
- A participant who becomes Downed or invalid before resolution is removed from the required set.
- At the deadline, authority performs pairwise position checks. Current experimental presentation radius is 14 tiles and minimum center separation is 28 tiles: the two visible danger discs must not overlap. Exact equality is safe. Stack retains its separate 7-tile radius.
- Each failed participant receives the failure result once even if overlapping multiple players. Pair iteration order must not multiply damage.
- No overlap means zero damage, including a lone remaining Alive participant. A failed participant loses 70% maximum HP once through the existing Raid damage path.
- Success or soft failure advances to Core exposure.

## Core exposure and victory

### Drawing-based attack sequences (current tuning 0.2.9)

- Repeated attack during Pylon active windows and Core exposure only. The first cast starts after the Pylon's 90-tick shield cue, or 60 ticks after exposure opens. Stack/Spread/intro/Reset remain beam-free recovery and assignment windows. The 0.2.7 user request restores fast steps within a category while preserving the deliberate category transitions: intro extended to 360 ticks in 0.2.8, Pylon opening cue 90, Stack/Spread 240 each, exposure opening rest 60 and Reset 120. Other major-phase lengths and live travel speed are unchanged.
- Authority selects an Alive focus in frozen-roster round-robin order per sequence. Later steps keep that focus unless unavailable, then choose the next Alive participant. Fixed beams lock their warning. Energy bodies track during harmless windup only; a live charge never changes focus or heading. At most two beam rays or one compact energy body exist globally, not per player.
- **Pylon / Pursuit Prism:** eight angled beams, red → blue → green → yellow → yellow → green → blue → red, with a matching 1–8 HUD count. Each predicts 18 ticks of observed velocity, clamped to ±220 px X / ±160 px Y. Angles from downward vertical are −0.55, −0.23, +0.09, +0.41 radians. Origin is 800 px behind the predicted point; length 2,600 px, full width 88 px. Warning 28 ticks, live 12, cadence 42; one sequence occupies 334 ticks. Each shot leaves two harmless ticks before the next warning.
- **Exposure / Dash–Stillness:** right charge → stop → left charge → stop, with 72-tick step cadence. Steps 1/3 are physical energy bodies, not long horizontal cuts. A 42-tick harmless windup comprises an 18-tick tracking orbit and a 24-tick stationary locked hold. Orbit radii remain 520px horizontal / 330px vertical, repositioning at most 64px/tick; aim leads observed velocity by six ticks capped at ±120px per axis. During tracking, heading turns at most 0.12 radians/tick and settles on the final target at lock. At `FireTick - 24`, origin and heading freeze, bright rails/crossbar and a latch sound signal the hold. Damage begins only at FireTick, at 104px/tick along that frozen heading for 22 ticks. The pre-lock damage fix from 0.2.5 is retained, not reverted with cadence. No live steering, artificial velocity check or player control lock. Engine `immune && immuneTime > 0` remains honored for charges; real Calamity dash compatibility and the shorter anticipation need the user smoke.
- **Stillness steps:** at the current target position, two downward 640-px-wide, 2,600-px-long curtains leave a locked empty 128-px column. Origin is 1,200 px overhead; warning 36 ticks, live 12. A normal stationary player box fits the gap; continuing horizontally enters a curtain. The four-step sequence occupies 264 ticks, fitting even the 720-tick penalized exposure after its 60-tick initial rest.
- The energy body has a 164px tail and 50px half-width. Its damage rectangle includes one tick of travel plus the nose radius (maximum length 318px) to prevent tunnelling at high speed. The exact rectangle has white-edged live drawing. The long dim segmented wake and the entire approach remain harmless. Client extrapolation never chooses a target or collision outcome; a late sample cannot extend the lifetime.
- A new sequence starts only if its complete duration fits the phase; the next sequence retains its extra 60-tick gap. Shorter complete sequences can repeat more times inside the unchanged Pylon/exposure windows. Sequence/step durations derive from the same warning/live constants as the volley, avoiding independent schedule drift. Early Pylon completion/phase exit cancels any pending attack. No catch-up bursts, terrain damage, new NPCs or Projectile slots. Boss casting/recoil animates even for off-body attacks.
- Server/SP samples each Alive participant's observed box against the same bounded descriptor and tick-derived geometry the client draws. In 0.2.9, fixed beams (`ObservationLance`, `PursuitPrism`, `Stillness`) deal **120 direct HP per hit**, independent of maximum HP and armor. The separate contact-style energy charges (`SweepRight`, `SweepLeft`) retain 60% maximum HP. A participant takes at most one hit per step, including both curtains. Recovery protection, Raid-owned lethal-to-Down and outsider exclusion are unchanged. Stack/Spread/Pylon penalties retain their own percentage rules; the beam change does not change Raid attendance mechanics.
- The exact-Fight runtime owns the current assignment and hit ledger. Full snapshots carry one bounded read-only volley; client sound, muzzle bloom, particles and camera shake are non-authoritative. Phase exit and every existing Fight cleanup discard the volley/ledger. No extra NPC or Projectile slot is allocated.

### Exposure rules

- Exposure changes the Boss visual state to `Exposed` and opens the authoritative damage gate.
- A clean Pylon check grants the normal 1080-tick window; a failed check grants 720 ticks.
- Boss life is one persistent authority-owned pool. Any provisional ring or arm visuals are not hitable parts.
- On each authority tick, permitted hits and life changes resolve before the deadline closes. At 50% in Phase I, transition takes priority over Reset; at zero in Phase II, Victory takes priority over another grid window or the cap.
- If Phase-I HP is above 50% at the deadline, close the damage gate, clear loop assignments, run `Reset`, and begin another Pylon check. Phase II instead retains its open gate across scheduling windows.
- Eventual balance target: a clean representative group should need roughly 4–6 successful exposures. The deliberately excessive `0.2.1` pass does not claim that target or even practical clearability within the existing eight-exposure cap.

## Downed and recovery interaction

Downed/Revive follows [Revive Specification](REVIVE_SPEC.md) and ADR-0011; the latter supersedes ADR-0005's held channel/shared tokens for this feature.

- Downed participants cannot attack, move normally, use items, hook, mount, take encounter damage, or be selected for Pylons/Stack/Spread/Boss targeting.
- An Alive participant may use the nonconsumed kit within 8 tiles in every `Active` substate. Authority resolves it instantly; flight/movement does not cancel a channel because no held channel exists. The recipient gets 35% health, 3-second protection and a 60-second cannot-receive-revival deadline; it does not stop that player rescuing someone else. No shared tokens or damage-weakness debuff apply.
- The frozen pull roster does not shrink. A revived participant returns to the same stable Participant ID.
- If every current participant becomes Downed in the same committed authority tick, Revive produces one Defeat candidate; absent a same-tick Boss Victory, the encounter ends in Defeat once and later revive input cannot undo it.
- On committed **Defeat**, every connected participant receives one ordinary Terraria death, after Raid Down/protection is cleared. Nonparticipants are excluded. The owner invokes `Player.KillMe` from the accepted exact-Fight terminal; normal engine respawn, death synchronization and character-difficulty penalties apply, including Hardcore character loss. No forced death on Victory, cancellation, invalidation or unload. Other Mods' `PreKill` hooks are not bypassed; a cancellation is logged and remains a runtime compatibility check.

## Authority tick and terminal precedence

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

## Player-count rules

| Pull roster | Pylons | Revival limit | Stack | Spread |
|---:|---:|---:|---|---|
| 1, development flag only | 2 | first Down ends Raid | 1 actual member required | no pair to overlap |
| 2 | 2 | recipient-only 60 s | 2 required shares | all connected Alive assigned |
| 3 | 3 | recipient-only 60 s | 3 required shares | all connected Alive assigned |
| 4 | 4 | recipient-only 60 s | 4 required shares | all connected Alive assigned |

The encounter does not scale by Boss HP alone. Pylon count, actor density, safe space, and tuning may vary by roster, but the sequence and authority rules remain identical.

## Terminal outcomes and feature cause

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

## Telegraph and fairness requirements

- Every mechanic has a server resolve tick and redundant shape/motion/text or sound language; never color-only.
- Client clocks interpolate presentation only. Latency must not move the authority deadline.
- Eventual balance should make a single ordinary Stack/Spread error recoverable and readable; the current intentionally excessive damage is not accepted final balance.
- No frame-perfect input, invisible off-screen hit, required class, or fourfold projectile multiplication.
- Damage numbers, telegraph radii, and exact HP remain provisional until 2/3/4-player Windows telemetry exists.

## First-slice exclusions

Part Break, the separate targeted-bait mechanic/phase from the historical backlog, Personal Effigies, Split Reality, Last Stand, multiple Boss parts, route selection, finished rewards and final music remain deferred. The explicitly requested giant first-pass art and repeated observation lances above are now in the development scope; they do not import the remaining [Backlog](BACKLOG.md).
