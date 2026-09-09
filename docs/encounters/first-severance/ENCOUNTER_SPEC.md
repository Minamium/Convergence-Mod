---
doc_id: encounter.first-severance.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-09
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

## Raid damage and Adrenaline

Positive, accepted encounter damage clears **stored standard Adrenaline**, including a hit converted into Down, and pauses accumulation. Zero-damage Stack/Spread success, invulnerability and the auxiliary debug lease do not count as hits. Healing/replication does not replay this effect. Calamity's active Adrenaline burst remains unaffected by a hit; Draedon's Heart/Nanomachines pauses instead of clearing. Ordinary Terraria hits remain Calamity-owned. This Raid-specific reset intentionally applies even to very small accepted HP losses; it does not reproduce Calamity's tiny-hit partial-loss curve, shield absorption, Chalice or retaliation hooks. Fixed Raid damage and recovery rules are otherwise unchanged.

The cause was `ApplyRaidDamage`'s intentional direct HP/Down path bypassing native `OnHurt`. The isolated [compatibility bridge](../../../Common/Compatibility/Calamity/CalamityRaidHit.cs) performs only the gauge side effect; calling `Player.Hurt` or all of Calamity's `OnHurt` would replay unrelated damage/defense/death effects. [Network architecture](../../NETWORK_ARCHITECTURE.md#preceding-development-protocol-v24) owns delivery and duplicate protection.

Scoped API research: Calamity public source **2.2.2**, commit `1a8cebd27ec5615316b78f71973446b5528d2b78`, [CalamityPlayerHitHurt.cs](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayerHitHurt.cs) (`OnHurt` / private `LoseAdrenalineOnHurt`) establishes burst/Nanomachines exceptions; [CalamityPlayer.cs](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayer.cs) exposes the gauge, pause and sound fields. Normal-hit pause in `Balancing/BalancingConstants.cs` and Nanomachine pause in `Items/Accessories/DraedonsHeart.cs` are private 60-tick values; the bridge explicitly owns that compatibility constant. Installed **2.2.4** compiles against those public members, but live behavior still requires the owner smoke check. This is not a claim that the public checkout equals the installed version. [Custom license](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md): reference use allowed, proprietary assets/code; no foreign function or asset is vendored. Calamity sound styles are borrowed at runtime from the required dependency.

## Random Final triples

Current0.2.37 replaces the late-reveal triple schedule: three complete comb forecasts appear at action ages0,12,24ticks and remain at their locked coordinates. After the third reveal there is54→42ticks of harmless reading time with Final progress; then the three combs fire in reveal order at24→18tick intervals, each live for6ticks. Each fades for12 harmless ticks after firing. Later forecasts remain visible during earlier shots. Cyan/violet/rose differentiate the groups; each arrival has one short cue and emphasis, with no numeric labels or chevrons. No RNG runs at release: action-entry seed, pulse and step reconstruct the identical axis/offset from reveal through impact. Geometry stays112px pitch,24px full width, clipped to the field;120fixed damage and one accepted hit per participant/pulse. [Score geometry](../../../Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs) owns the exact curves and deadlines. The action includes all three releases and final fade. This replaces the0.2.27/28 sequential reveal timing described below as history.

Iron Interdict sparse sword offsets are now-16px in wave one and+64px in wave two (80px displacement, versus the previous32px). The dense half, stagger and warning schedule stay fixed. Authority and visuals use the same helper; whole-player positions safe in wave one are covered by wave two, requiring lateral repositioning. New gaps keep the same40–72px width.

Current0.2.27 overrides previous FinalSlicer descriptions: each slicer action has three pulses, each choosing vertical, horizontal or diagonal (either slope) by shared seeded pseudorandom selection; repeats are possible. Seed is the server-assigned action entry tick already in snapshots, mixed with step/pulse. Full width matches the lattice24px; pitch96px leaves72px clear lanes. Rays are clipped to the arena (tiny corner fragments below32px are omitted). Telegraph28→22ticks with terminal progress, active6ticks, recovery2ticks; cadence36→30ticks. The action ends after exactly three cadences, without the former six-pulse wait. Fixed120damage and once-per-participant-per-pulse authority damage remain. No new actors, resources or client input. Final bullets, Stack/Spread and phase gates are unchanged. Random triples do not promise that every stationary position is hit during each individual triple.

## Pursuit during every Spread — 0.2.37

Current0.2.39 supersedes the initial all-window/eight-shot design: pursuit occurs only in standalone Spread, never during embedded Lattice/RemoteClaws Spread or other attack categories. [Spread barrage](../../../Content/Encounters/FirstSeverance/FirstSeveranceSpreadBarrage.cs) uses3shots for windows shorter than180ticks, otherwise4. After a6-tick opening it distributes starts at least34ticks apart, retaining each28-tick warning and12-tick active interval; the last beam ends24ticks before Spread resolves. Server snapshots freeze all standing participants' aims including velocity lead per shot. Missed starts are skipped without catch-up bursts. Damage remains120fixed per cast/member; Spread success/overlap rules are unchanged. Independent pursuit can still hit during an otherwise successful standalone Spread.

The [authority runtime](../../../Content/Encounters/FirstSeverance/FirstSeveranceSpreadBarrageRuntime.cs) owns the bounded cast list and hit ledger, cleared on window/action/Fight end; the existing combat runtime retains tick ordering and terminal decisions. [Protocol](../../NETWORK_ARCHITECTURE.md#current-development-protocol-v26) owns the descriptor. Three/four-player crossings and real latency remain playtest checks, not guarantees from timing arithmetic. HP, other attacks, recovery and weapon behavior are unchanged.

## Weapon-only reward extension — 0.2.25

[Five Ritual Armaments](WEAPONS.md) owns current reward mechanics, art and initial damage budgets. Existing Victory still drops Null Refrain; twenty one-for-one Work Bench exchanges let every class choose a form without changing the Boss loot executor. The older no-recipe/three-stroke tuning below is historical where it conflicts with that weapon specification. Boss actions, HP, damage, field and recovery are unchanged.

## Iron Interdict and current spacing override

This supersedes the older half-field erasure, Spread spacing and Final forecast values below. The stable `HalfField` substate now presents **Iron Interdict**, defined by [ImpalingSwords](../../../Content/Encounters/FirstSeverance/FirstSeveranceImpalingSwords.cs): two irregularly staggered waves of top/bottom swords during the same five-second action. The first occurrence seals the left half; the second seals the right. Overlapping dense-side auras leave no safe gaps when fully inserted; the opposite side has narrow body-passable gaps whose positions change on the second wave. Whole future blade corridors are warned, then rigid blades insert in six ticks. Authority damage extends only as far as the visible tip; retraction is harmless. The existing fixed-damage, once-per-participant/per-wave cap is retained. No new actors, client targeting or saved state are introduced. The other RemoteClaws horizontal floods and safe-window mechanics remain.

Final slicing comb pitch is136px instead of160px, retaining56px beam width. Fire advances exactly three ticks (0.05s), while pulse cadence, ending ticks, action budgets and BGM stay unchanged; the live interval consequently grows by those three ticks. Shared [score geometry](../../../Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs) retains shifting axes and all six pulses.

Spread radius is352px (+10%), requiring704px /44 tiles between centers, with unchanged70%-HP overlap failure and zero-damage success. [SafeWindows](../../../Content/Encounters/FirstSeverance/FirstSeveranceSafeWindows.cs) moves lattice pockets apart so four whole acceptance discs preserve separation. Four horizontal flood destinations at center-relative X offsets−1110,−370,370,1110 still fit; ordinary four-corner travel estimates use offsets(±370,±370). Actual latency/gear dodgeability remains user-tested, not guaranteed by the domain checks.

## Center-out curtains and Phase-II Spread-only override

Phase-I Stillness uses dense narrow teeth instead of two instantly filled slabs. The same locked side footprints are partitioned by [CurtainComb](../../../Content/Encounters/FirstSeverance/FirstSeveranceCurtainComb.cs): each side reveals from its center outward, and fires in that order after each tooth's complete warning. Adjacent teeth leave no player-sized safe gaps; the central safe column is unchanged. Shared geometry/epochs control both authority collision and client light. One fixed beam-damage hit per participant per cast remains the cap, not one hit per tooth. The scheduler budgets the last tooth without changing the step cadence or category rests. No new actors, target tracking, HP adjustment or client damage claim is introduced.

Phase II has **no Stack**, either as a grid overlay or standalone action. Its first grid is ordinary; the Spread pocket/resolve on the third grid and the between-action Spreads remain. Phase-I, Phase-III and Final Stack rules are unchanged. This supersedes the former first-grid Stack assignment below and in historical evidence.

## Safe-window mechanics and victory weapon — 0.2.19

These changes preserve HP, beam damage, Stack/Spread failure rules and ordered phase gates. The [sanctuary schedule](../../../Content/Encounters/FirstSeverance/FirstSeveranceSafeWindows.cs) owns exact timing/placement:

- Phase II: the third lattice volley leaves four widely separated Spread pockets. Indicators resolve **while the matching grid is live**. Ordinary volleys retain aimed Core salvos from serial three; Spread-pocket volleys omit those salvos. Actual clipped rays and rendering share the same pockets, sized for whole player bodies. The former first-grid Stack is retired by the current override.
- Phase III expanding floods: Stack → Spread → Stack resolves during the three fully expanded safe-strip holds. Stack strips fit the complete acceptance circle plus a player body. The next pulse changes the strip only after the current hold; [score geometry](../../../Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs) owns current widths/hold/cooling deadlines.
- Final slicing combs retain their extended six-shot budgets and shifting lanes; the current forecast/spacing override above supersedes the original15-tick addition. Stack/Spread/bullet pacing is unchanged.
- Server/SP suppresses ordinary natural NPC/critter spawning world-wide during the active Fight via `EditSpawnRate` (`maxSpawns=0`, effectively infinite interval) and an empty `EditSpawnPool`. Existing NPCs/town residents are not deleted. Explicit scripted/statue/other-Mod spawns may bypass these hooks. Cleanup revokes the runtime predicate; no saved toggle.

**Null Refrain / 断唱** is a prototype melee reward: alternating quick cuts followed by a stronger extended third stroke, swept physical blade collision and one hit per target per stroke. [Weapon defaults and motion](../../../Content/Encounters/FirstSeverance/Rewards/NullRefrain.cs) own provisional stats. Holding attack repeats the combo; a pause/weapon change resets it. No recipe, command grant, forced movement, Calamity shortcut or extra damage request packet.

Only accepted **Victory after Final survival** creates one ordinary shared world item per frozen-roster participant at the Foundation Core. HP zero, Defeat, cancel and unload do not award it. The exact-Fight root marks grant attempts before Terraria item hooks, preventing duplicate grants on cleanup retry; failures are logged without blocking field/recovery cleanup. This is not an instanced inventory grant or the final production loot table.

API basis: [Version Matrix](../../VERSION_MATRIX.md), [pinned GlobalNPC.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/GlobalNPC.cs) and installed XML for `Item.NewItem`/draw/projectile hooks. The official [custom-swing example](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/Projectiles/ExampleCustomSwingProjectile.cs) informed hook selection only; implementation is independently authored, with no vendored source/assets or new dependency.

HP is unchanged pending discussion. [Pre-change playtests](../../evidence/2026-09-07-pre-0219-playtests.json) separate solo0.2.18, two-player0.2.17 and older three-player0.2.16 (superseded recovery/attack rules); do not average them into a universal per-player DPS factor.

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

The phase structure originated in0.2.13; current overlapping mechanics and timing are governed by the first section. [ADR-0021](../../adr/0021-untimed-recovery-and-simultaneous-prism.md) removes Eliminated/Down expiry while retaining the60-second recipient restriction and immediate all-Down Defeat.

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

- **Grid blocks:** the existing score retains its opening rest and inter-block Spread. Sanctuary tasks follow the first section; ordinary intervening volleys retain aimed Core salvos. Grid/Core share one hit per participant per volley.
- **Simultaneous opening Prism:** each of eight fast steps locks one predicted ray for every currently Alive connected participant, all on one authority tick. Red→blue→green→amber then reverse retains28 warning/12 live ticks and42-tick step cadence. There is no per-player turn to wait for. Whole-combo/category rests remain; charges and Stillness are unchanged. One fixed120 hit maximum per player per step even where several targeted rays cross. `LanceTelegraph` logs bounded`target_slots` and ray count.
- **Rotating blades:** two opposing88px-wide rays reach the field edge. Harmless windup180, rapid unsheathing144–156 and24 ticks of full-length warning remain. Motion is now **two clockwise revolutions over300ticks**, with angular progress`2*(0.55t+0.45t²)` turns: mean speed1.6× the preceding one-turn attack, and continuously accelerating. Peak tangential speed at radius180 is10.93px/tick; move farther inward for more margin. Recovery60 ticks is harmless. One fixed120 hit per blade **per revolution**; distinct turn/pulse IDs prevent the first turn's hit cap making the second harmless. Prior-tick swept angle prevents tip tunneling. Smooth damage-zone aura replaces hard side rails.
- **Expanding horizontal floods:** `RemoteClaws` remains the stable state ID. Connected arm charge forecasts two full-width bands; thin beams deploy horizontally, then widen with the shared accelerated curve. Current hold/cooling deadlines and alternating Stack/Spread strip sizes come from [score geometry](../../../Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs). Launch side and strip position change between pulses only. Each entire pulse shares one fixed-damage hit ledger across both bands; body rig, forecast, growth and collision share the schedule.
- **Half-field beam:** 180-tick large charge of the entire selected half, 60 live ticks, 60 recovery ticks. First is left, second right; each uses exactly half the 2560x1120 field. One fixed120 hit maximum. The complete player body must leave the boundary, not just its center. The opposite half is safe from this action.
- **Central hand crush:** new final action of the Distant score,300 ticks. The fixed720×600px rectangle at the field center is warned from entry. Both giant hands brace for150 ticks and close inward in12 ticks; only ages162–179 are lethal. Side and upper/lower pockets outside the marked rectangle remain safe; the approaching arms themselves do not damage. Contact routes through existing lethal-to-Down recovery (not permanent death or a new death hook), retaining post-revive immunity and authorized debug protection. One hit ledger entry per participant; retract and all remaining recovery are harmless. It allocates no gameplay hand NPCs or new requests.
- **Terminal bullet rain:** five waves of24 beads, with24-tick per-wave appearance, within the shrinking dodge block. Normalized terminal progress`p=i/7` sets first release`36-floor(8p)`, wave interval`32-floor(10p)`, speed`10+3.5p`px/tick. Tangential displacement reaches240px; lifetime is`115/(speed/10)` or the action deadline. Radius12px, swept body checks and one fixed120 hit per wave remain. No hazard/ledger survives into the next Stack.
- **Terminal slicing comb:** six alternating-axis pulses use [SlicerCadence/Fire/End](../../../Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs), including the additional harmless warning above. Repeated-axis offsets change between shots, never during a live beam; existing field coverage, open lanes, swept body checks and one hit per pulse remain. The score extends rather than dropping the last shot.

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

On **ordinary non-sanctuary volleys from grid serial 3 onward**, each warning also locks one Boss-origin laser toward each currently connected Alive participant. The serial continues across Phase-II scheduling windows; Downed participants are not new targets. Origin is the Boss's fixed damage Core, length **3,000 px**, full damage width **144 px**. Direction freezes for the entire **60-tick warning and 20-tick live window**; no live homing, new projectile actor or player-speed test. Grid and Core lasers share one per-participant hit ledger: **one fixed 120-HP hit maximum for the whole volley**, including multiple intersections. A player may dodge both by leaving the locked corridor and choosing a grid cell. The current score and recovery specifications govern cleanup and scheduling.

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

The development implementation includes a multiplayer Raid loop, original giant-Boss art/attacks and one prototype victory weapon. Balanced public solo/companions, a final production loot table, art/audio acceptance and final tuning remain separate; the one-member debug exception is solely for faster iteration.

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
