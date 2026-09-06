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

The user-authorized experiment in [ADR-0009](../../adr/0009-development-combat-experiment.md) runs before production integration is complete. Current HP follows [frozen-roster scaling](../../../Content/Encounters/FirstSeverance/FirstSeverancePartyScaling.cs); actor defense lives in the [Boss](../../../Content/Encounters/FirstSeverance/Actors/FirstSeverancePrototypeBoss.cs) and [Pylon](../../../Content/Encounters/FirstSeverance/Actors/FirstSeverancePrototypePylon.cs) defaults. Stack uses the missing-roster fraction below. Spread overlaps receive 70% maximum HP once; failed Pylons pulse 35%, clamped nonlethal. Both Stack and Spread cost zero HP on success. General Terraria lethal events are not intercepted. [ADR-0012](../../adr/0012-pattern-sequences-and-defeat-death.md) adds independent-origin attacks, the introduction and normal participant death on Defeat. [Status](../../STATUS.md) owns implementation and test results.

## One-member development start and results — 0.2.14

While the server/SP build's `ConvergenceDevelopmentSolo` flag is enabled, a single eligible real player may activate the existing Core and manually Ready, including ordinary Host & Play with no guest. The flag defaults on during development and must be compiled off before public release; [ADR-0020](../../adr/0020-development-solo-admission-and-terminal-hud.md) owns admission, wire bounds and release safety. All Core/field/progression checks still apply. The preparation message explicitly identifies SOLO DEBUG. No NPC, extra window, invulnerability, automatic revival or persisted player/world setting is introduced.

Initial admission and the fresh Ready-to-combat scan use the same non-optional compiled policy. Revalidation still rejects a removed Core, newly blocked field or world conflict; it must not reintroduce a separate minimum-two default. Current implementation and observed regressions are recorded in [Status](../../STATUS.md).

Solo debug reuses the two-player workload in the [scaling policy](../../../Content/Encounters/FirstSeverance/FirstSeverancePartyScaling.cs). Attack sequence, geometry, damage, timers, phase HP gates, infinite Raid flight and Final survival are unchanged. Targeting uses the sole real player. Stack uses the normal missing-roster fraction: one present out of one means zero damage; missing the fixed marker means the full fraction. Spread still resolves all pairs; with one participant there are no overlaps. These natural count-dependent outcomes are not a dedicated solo redesign or a multiplayer balance claim. Two/three/four-member HP tables and behavior remain unchanged.

If the sole participant becomes Downed, the ordinary authority commit immediately ends the Raid as AllParticipantsDowned/Defeat; it does not wait for an impossible solo revival. Existing participant death and exact-Fight cleanup follow. This does not bypass an external Mod's death-cancelling hook. Public-release minimum remains two until a separate solo/companion design is approved.

Accepted **Victory and Defeat** now replace the participant's HUD with a bounded result cinematic, also over the local death screen. Gameplay cleanup and player death are immediate, not delayed for presentation. Duration, camera, typography and effects are owned by the [visual spec](VISUAL_SPEC.md#success-and-failure-designations--0214). Cancelling or unloading is not a success/failure cinematic. The start log records `solo_debug` and the compiled admission flag so one-member diagnostics cannot be confused with multiplayer calibration.

## Ordered phase scores and terminal survival — 0.2.13

The phase structure originated in0.2.13; attack tuning in this section is updated for0.2.17. [ADR-0021](../../adr/0021-untimed-recovery-and-simultaneous-prism.md) removes Eliminated/Down expiry while retaining the60-second recipient restriction and immediate all-Down Defeat.

This section supersedes the earlier immediate 50%-HP transition, grid-only Phase II and HP-zero Victory. Frozen-roster HP scaling, defense, Pylon tuning, zero-damage full Stack/safe Spread, instant reusable revival and existing defeat/cleanup rules remain. [ADR-0019](../../adr/0019-phase-scores-and-terminal-survival.md) owns authority and replication. Final means surviving all required actions, **not** a no-hit challenge; ordinary fixed damage, Down and revival remain available.

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

Matching peers are required for the current protocol; [Network Architecture](../../NETWORK_ARCHITECTURE.md) owns the current envelope and definition-route contract. Stable numeric substate/packet IDs and saved data remain unchanged. The [ADR-0019](../../adr/0019-phase-scores-and-terminal-survival.md) score/authority/cleanup model remains. Stack sync/112px acceptance, Spread640 separation, frozen HP, instant item use and60-second recipient lockout are unchanged.

`PhaseChanged` now records action index, completed phase cycles and resolve tick. `HpGateReached` distinguishes HP waiting from a stuck fight; `ScoreAttackFired` identifies new ray pulses; `SpreadResolved` logs alive/overlapping counts. Core telemetry now measures the **damageable segment above its phase floor**, closing on HpGateReached so mandatory waiting does not dilute DPS; the full `boss_life` remains separate. Previous reports using the whole persistent pool must be interpreted with their recorded build.

## Forgiving roster HP and Core salvos — 0.2.12

The accepted roster chooses one immutable `FirstSeverancePartyScaling` at combat creation. No live-player count, Down, elimination, reconnect, difficulty multiplier or client DPS report recalculates it. Pylons in subsequent loops use that same tuning; the Boss still has one persistent pool.

| Frozen participants | Boss/Core HP | Each Pylon HP | Total Pylon pool | Required party Pylon DPS in 14 s |
|---:|---:|---:|---:|---:|
| 2 | 5,000,000 | 300,000 | 600,000 | 42,857 |
| 3 | 9,000,000 | 500,000 | 1,500,000 | 107,143 |
| 4 | 13,000,000 | 600,000 | 2,400,000 | 171,429 |

Calibration uses the preceding two-player clear's Core/Pylon effective rates (76,287 / 81,081 DPS) and three-player clear's rates (199,501 / 233,161 DPS), not raw equipment damage. At those rates, two-player Core damage requires about 65.5 open seconds and a Pylon pool 7.4 seconds; three-player Core about 45.1 seconds and a Pylon pool 6.4 seconds. These deliberately leave development margin. These are damageable-time estimates, not predicted full fight durations: shield/mechanic/transition time and reduced output while dodging are separate. The builds and phase structures differed; the measured ratio is not a universal per-extra-player factor. Four-player tuning is unmeasured extrapolation. Sanitized observations and limitations live in [the check record](../../evidence/2026-09-06-eclosion-salvos-checks.json).

From **grid volley serial 3 onward**, each warning also locks one Boss-origin laser toward each currently connected Alive participant. The serial continues across Phase-II scheduling windows; Downed participants are not new targets. Origin is the Boss's fixed damage Core, length **3,000 px**, full damage width **144 px**. Direction freezes for the entire **60-tick warning and 20-tick live window**; no live homing, new projectile actor or player-speed test. Grid and Core lasers share one per-participant hit ledger: **one fixed 120-HP hit maximum for the whole volley**, including multiple intersections. A player may dodge both by leaving the locked corridor and choosing a grid cell. The current score and recovery specifications govern cleanup and scheduling.

The historical protocol **v12** added a bounded 0–4 count and unit-direction pairs to the existing grid descriptor; source and dimensions derive from shared constants. All peers update together. Actor `SyncNPC` extra AI carries the frozen count and bounded current life so local HP bars match the authority pool, including a full-life spawn. No client request or saved field is added; [ADR-0018](../../adr/0018-roster-health-and-core-salvos.md) owns this extension. `CombatStarted` records `hp_policy=FrozenRosterDevelopment`; grid logs include `core_beams`, firing damage/cap, and hits distinguish `source=CoreSalvo` from `source=Lattice`.

## Grounded containment field — current 0.2.11

New Foundation Core placement uses a 12x4-tile plinth, anchored on supporting ground/platforms with clear solid-block airspace 80 tiles to either side and 70 tiles above its bottom. Placement preview shows the intended **160x70** rectangle and rejects solid obstructions; server activation and all-Ready recheck the actual arena without terrain mutation. This halves each dimension of the preceding 320x140 field (one quarter of its area), not merely its area. Old 2x2 Cores remain readable/clickable; mine and replace to obtain the larger foundation. The ground under the plinth is the field floor, not the old floating center.

During Active, the complete participant body is confined to that rectangle, including dash, knockback and teleport movement. Server authority and owning-client prediction use the same boundaries; other players are unaffected in this development pass. Connected standing participants gain inexhaustible equipped wing/rocket flight; without wings, holding Jump provides field-supported lift. Downed participants cannot fly. End/cancel/disconnect/unload revoke the capability. This is temporary Raid state, not saved equipment or a public cheat toggle. [ADR-0016](../../adr/0016-ground-containment-and-continuous-emission.md) owns authority, protocol v10 and legacy-tile compatibility.

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

## Authority and lifecycle

Common lifecycle is Validating → Preparing → Active → terminal publication → exact-Fight cleanup. First Severance's current ordered scores are defined above, not by the historical six-state-only MVP. Boss HP persists; the damage gate is [IsDamageState](../../../Content/Encounters/FirstSeverance/FirstSeveranceBossPhases.cs) plus the pending phase floor. HP zero starts Final; only finishing Final produces Victory.

Exact numeric defaults live in [the raid plan](../../../Content/Encounters/FirstSeverance/FirstSeveranceEncounterPlan.cs), [party scaling](../../../Content/Encounters/FirstSeverance/FirstSeverancePartyScaling.cs), [attack patterns](../../../Content/Encounters/FirstSeverance/FirstSeveranceAttackPatterns.cs), [choreography](../../../Content/Encounters/FirstSeverance/FirstSeveranceChoreography.cs) and [lance tuning](../../../Content/Encounters/FirstSeverance/FirstSeveranceLance.cs). This spec owns intent and provisional behavior; other documents link here rather than repeating balance tables.

## Pylon DPS check

- The multiplayer plan spawns one authority-owned Pylon per frozen participant; the solo debug exception uses the two-player workload described above.
- Any connected Alive participant may damage any Pylon. There is no player-to-Pylon affinity.
- Count and health are fixed from the pull roster and do not rescale after a participant becomes Downed or disconnects. Remaining players may cover unfinished Pylons.
- Suggested symmetric placement is left/right for two, triangle for three, and quadrants for four. Exact positions must derive from the server-resolved arena layout.
- Server-owned entity damage/life decides success. Clients never submit DPS totals or success.
- Destroying every Pylon advances immediately to Stack.
- At deadline, remaining Pylons are removed through encounter ownership; failure adds one `Overload`, applies a survivable raid-wide pulse, and marks the later exposure as penalized.
- `Overload == 3` causes Defeat. One Pylon failure is a recoverable soft failure.

Pylon HP is a typed feature tuning value. Set it from Windows telemetry so a clean party has meaningful margin; do not encode a client-reported or class-specific DPS requirement.

## Authority diagnostics

Server/SP diagnostics are passive and scoped to the owning Fight. `CombatStarted` records HP/window tuning. `DamageWindowStarted`, two-second `DamageProgress`, and one `DamageWindowEnded` describe each Pylon/Core window; `PylonDestroyed` records each confirmed kill, and `CombatDpsSummary` totals Core and Pylon damage/open time separately at every ending, including interruption. No per-hit log, player name/position, client-reported DPS, packet change or gameplay decision is introduced.

- `effective_damage` is the authority-observed net target HP reduction, capped to actual target HP; overkill, shields, decorative targets and damage outside the window are excluded. It is party progress, **not per-player attribution or raw weapon DPS**.
- `open_seconds` excludes the Pylon's shield cue and all other phases. `window_dps` averages from that window's opening; `recent_dps` covers only the interval since its previous sample. Time is authority ticks at 60 Hz, not measured wall-clock seconds.
- `target_hp_remaining`, `window_progress_pct`, per-ordinal `pylon_hp`, persistent `boss_life`/`boss_remaining_pct`, and `overload` expose progress. `required_dps_by_deadline` is the remaining target HP divided by remaining open time; it is unavailable after a missed deadline. For the Core this is the rate to finish **in this exposure**, not a mandatory damage quota.
- A Pylon disappears from the live actor list only after its authority-confirmed death; diagnostic state retains zero HP for that ordinal. Missing/unconfirmed actors mark `hp_observation=Incomplete` rather than being counted as kills. Terminal partial windows are recorded before owned actors are removed; repeated cleanup cannot double-count totals. A logging failure cannot interrupt combat or cleanup.

## Stack / 頭割り

- Authority publishes the current stationary Stack site's world-space center and resolve tick. Opening, clockwise relay and Final sites are defined by the current score; no participant carries the circle.
- At the resolve tick, authority counts connected Alive centers within the inclusive 7-tile radius of that fixed point. Downed/disconnected participants do not count, but the frozen pull roster never shrinks.
- Every pull participant is required: `2 / 3 / 4` occupants for `2 / 3 / 4` players. Full attendance is success and deals **zero damage**.
- Otherwise each connected Alive participant takes `ceil(their maximum HP × (rosterCount - occupants) / rosterCount)` once, including participants outside the circle so abandoning the group does not grant immunity. One missing player is 50% in a two-player pull, about 33.3% in three, or 25% in four. This is direct Raid damage, honoring recovery protection and lethal-to-Down, without armor mitigation.
- An incomplete Stack adds no Overload or extra failure debuff; it advances to Spread unless its damage produces a terminal result. Clients never supply attendance, damage or success.
- A Down does not move the marker or require target reissue. Ordinary death/disconnection still aborts the experiment. This replaces player-following assignment, not the missing-roster damage rule.

## Spread / 散開

- Every connected Alive participant at assignment receives a marker and the same server resolve tick.
- A participant who becomes Downed or invalid before resolution is removed from the required set.
- At the deadline, authority performs pairwise position checks. The presentation radius and required separation are defined in Travel and spread tuning above. The two visible danger discs must not overlap; exact equality is safe.
- Each failed participant receives the failure result once even if overlapping multiple players. Pair iteration order must not multiply damage.
- No overlap means zero damage, including a lone remaining Alive participant. A failed participant loses 70% maximum HP once through the existing Raid damage path.
- Success or soft failure advances to Core exposure.

## Core exposure

A successful Pylon check opens the normal exposure; a failed check opens the shorter penalized window. Independent-origin Prism, charge and Stillness combos run in Pylon/exposure attack windows. Each combo locks its harmless forecast before firing. Charge motion and contact share the same sampled path; Stillness attacks the lanes beside a locked safe gap, not a hidden player-velocity test. Fixed beam damage and per-step hit caps are separate from percentage attendance penalties. Exact tuning and geometry are shared by authority and client drawing.

The first complete exposure and following clockwise relay cannot be skipped at the phase HP floor. Required score completion, later threshold deadlines and Final survival use the ordered-phase rules above. Decorative shell/arms are not separate hitable pools.

## Downed and recovery interaction

The [Revive Specification](REVIVE_SPEC.md) is the only recovery-rule source: untimed Down, reusable instant kit, recipient lockout, no active Eliminated. The frozen roster does not shrink. General Terraria lethal events are not converted into Raid Down by this experiment.

On committed Defeat, clear Raid protection and request one ordinary Terraria death per connected participant, never outsiders. Other Mods' PreKill cancellation is logged, not bypassed; normal difficulty penalties (including Hardcore loss) apply. Victory/cancel/invalidation/unload do not force death. GUI cinematics cannot delay gameplay cleanup.

## Authority tick and terminal precedence

The feature runtime settles one authority tick in this order:

1. Validate runtime/Core identity and roster connection/death facts; apply queued Down/cancel intents.
2. Resolve due Stack/Spread using authority positions; observe exact-Fight Pylon and Boss state.
3. Update lance, lattice and score hazards with their owned hit ledgers.
4. Advance the loop once; record HP gates/DPS and any Pylon failure pulse.
5. Revalidate queued instant revives after all same-tick damage, commit recovery once and synchronize player projections.
6. Select the loop terminal (including completed Final) before a coincident recovery terminal. Otherwise apply substate entry and actor life updates.
7. Publish the accepted terminal snapshot/tombstone before exact-Fight cleanup.

[The combat runtime](../../../Content/Encounters/FirstSeverance/FirstSeverancePrototypeCombatRuntime.cs) is the single ordering owner. Actor/Core invalidity and external preemption outrank untrustworthy gameplay results. HP zero alone is not Victory. Collaborators cannot independently commit a generic lifecycle transition.

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

Safety/validity endings therefore override a coincident gameplay result whose authority can no longer be trusted. The initiator's development cancel is also accepted during Active at the queued-intent boundary, before mechanic resolution.

`WorldUnload`, an unhandled `InternalFailure`, and a fatal `ProtocolFailure` originate outside this reducer and unconditionally preempt an uncommitted feature result in that order. The coordinator synthesizes their generic/cause pair from the immutable mapping registered with the encounter definition; it does not re-enter a failed feature tick. The implemented external-termination bridge publishes the combined terminal projection before cleanup. The authorized development path already publishes feature replication and starts after Core/roster/Ready validation; deferred production hooks do not disable it.

## Terminal outcomes and feature cause

[FirstSeveranceTerminalCause](../../../Content/Encounters/FirstSeverance/FirstSeveranceTermination.cs) owns append-only wire values and generic mappings. Current Victory follows Final completion (the retained wire name is BossLifeZero), not the first observation of zero HP. Overload and all-Down remain gameplay Defeat. Legacy timeout/loop-cap causes remain decodable for compatibility/tests but are not scheduled by the active untimed, staged fight.

Every terminal publishes a snapshot/tombstone before cleanup. Cleanup releases exact-Fight actors, assignments, containment/flight, player/recovery projections and the Core lease once. WorldUnload/InternalFailure/ProtocolFailure use the definition-owned external mapping, not another feature tick.

## Telegraph and fairness requirements

- Every mechanic has a server resolve tick and redundant shape/motion/text or sound language; never color-only.
- Client clocks interpolate presentation only. Latency must not move the authority deadline.
- Eventual balance should make a single ordinary Stack/Spread error recoverable and readable; the current intentionally excessive damage is not accepted final balance.
- No frame-perfect input, invisible off-screen hit, required class, or fourfold projectile multiplication.
- Damage numbers, telegraph radii, and exact HP remain provisional until 2/3/4-player Windows telemetry exists.


## History

The [pre-consolidation snapshot](../../history/2026-09-07-pre-consolidation.md) preserves superseded single-cycle timing and staged implementation history. It is not current tuning or an activation gate.
