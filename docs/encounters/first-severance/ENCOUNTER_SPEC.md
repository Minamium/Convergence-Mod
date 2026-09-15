---
doc_id: encounter.first-severance.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-15
source_of_truth_for:
  - first_severance.encounter_loop
  - first_severance.mechanics
  - first_severance.terminal_outcomes
  - first_severance.public_identity
aliases:
  - Requiem of the Hollow Doll
  - First Severance
  - 不幸な人形劇
  - The Unfortunate Doll Play
  - Lacrimosa
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

# Requiem of the Hollow Doll — Encounter Specification

This is the current player-visible contract, reconciled with the implementation at the [Status](../../STATUS.md) checkpoint. Prior tuning/override paragraphs are preserved in [encounter evolution](../../history/2026-09-11-encounter-evolution.md), not instructions to restore old behavior.

## Identity and scope

**Requiem of the Hollow Doll** faces **ラクリモーサ — 縛られた心 / Lacrimosa — The Bound Heart**, a tragic suspended Doll in a cathedral theater. These are the current public names. The Raid title is separate from the approved BGM **EigHt — 不幸な人形劇**; do not rename the credited work or imply creator endorsement. The former First Severance / Null Cantor names survive only as historical names and stable internal `FirstSeverance` / `first_severance` / `NullCantor` content, packet, asset and document IDs; no save migration is performed. The cooperative development target is 2–4 players with post-Exo-Mechs/Supreme-Calamitas, Shadowspec-level equipment. The large body is **one logical HP pool**, not multiple damageable limbs.

Solo activation is ordinary supported behavior, not a debug-only or release-disabled exception; multiplayer remains recommended. Solo is not separately rebalanced. The additional [Doll summon weapon](WEAPONS.md#doll-companion--the-unbroken-promise) is currently not a roster member and does not fill Ready/Stack/revival roles. Companion substitution is an owner-selected future direction recorded in [Backlog](BACKLOG.md#companion-party-substitution--planned), not implemented behavior. Current implementation/test state belongs only to [Status](../../STATUS.md).

### Public naming

Use these names in localization, tooltips, README, Workshop and release notes. Uppercase cinematic typography is not a different name. Historical research/ADRs and stable identifiers retain their original spelling.

| Surface | English | Japanese |
|---|---|---|
| Raid | Requiem of the Hollow Doll | Requiem of the Hollow Doll |
| Boss | Lacrimosa — The Bound Heart | ラクリモーサ — 縛られた心 |
| Pre-Raid NPC | Unnamed Doll | 名もなき人形 |
| Pedestal / preparation key | Foundation Core / Theater Doll | ファウンデーション・コア / 開演の人形 |
| Damageable Pylon | Suspension Anchor | 懸架の楔 |
| Revival item | Resuscitation Kit | 蘇生キット |
| Reward box | Curtainfall Treasure Box | 閉幕の宝箱 |
| Ten-slot companion weapon | The Unbroken Promise | ほどけない約束 |

[Weapons](WEAPONS.md) owns the five reward names. `DollCovenant` and `NullCantor` are internal identifiers, not alternate public companion/Boss names. `Null Refrain` remains the legitimate melee weapon name; do not rename saved item/projectile types to match display text. The BGM keeps its original title and composer credit in [Attribution](../../../Assets/ATTRIBUTION.md#eight-不幸な人形劇-phase-masters--0246).

## Admission and arena

Accept **1–4 real connected players** in Single Player, Host & Play and Dedicated Server builds, including public releases. One player still marks Ready manually; one solo Down ends the Raid. Never require a compile symbol, private flag or second client merely to start. [ADR-0025](../../adr/0025-public-solo-admission.md) supersedes the old build-gated exception without changing attack patterns, HP scaling or network authority.

[Arena infrastructure](../../ARENA_INFRASTRUCTURE.md#activation-flow) owns the complete start procedure. Every active in-world server member is considered, regardless of distance; invalid/dead/ghost/over-capacity rosters reject the whole start instead of omitting someone. The requester still needs to be near the Core. The selected roster/binding epochs remain frozen; joining/leaving during preparation requires a fresh preparation.

The current grounded field is validated before use, physically contains participants and blacks out the exterior. New Core placement uses a larger plinth; old tiles remain readable. No terrain or save is rewritten. Preparation first gathers participants, deploys the field with HUD suppression, then accepts manual Ready. Each ready player has a small overhead `Ready!`; all Ready leads to a second combat-start cinematic, not another field rebuild.

Temporary infinite flight and containment are Fight-owned through preparation/combat; Down cannot fly. Ordinary natural mobs/critters are suppressed server-wide during combat by [AmbientSpawns](../../../Content/Encounters/FirstSeverance/FirstSeveranceAmbientSpawns.cs). Existing/town NPCs are not deleted; explicit scripted/statue/other-Mod spawns may bypass the natural-spawn hooks. Cleanup restores the normal behavior without a persistent toggle.

## Ordered phases and HP gates

| Phase | Required first action cycle | Next stage |
|---|---|---|
| I — Sealed | Pylons → opening Stack/Spread → first Core exposure → four clockwise fixed Stack sites, each followed by Spread | Unbound at 80% remaining HP |
| II — Unbound | Lattice → Spread → accelerating twin-blade double rotation → Spread → Lattice → Spread | Distant at 40% remaining HP |
| III — Distant | Remote floods → Iron Interdict → Stack/Spread → opposite-side Iron Interdict → remote floods → Stack/Spread → central crush | Final at zero logical HP |
| Final | Eight clockwise Stack/Spread stops, alternating dense bullets and four-color prism scores, then a final Core DPS check | Victory only after survival AND destroying the exposed check Core before its deadline |

[BossPhases](../../../Content/Encounters/FirstSeverance/FirstSeveranceBossPhases.cs) owns thresholds, transition lengths and hittable substates. [Choreography](../../../Content/Encounters/FirstSeverance/FirstSeveranceChoreography.cs) owns ordered actions, moving-site geometry and durations. [LoopStateMachine](../../../Content/Encounters/FirstSeverance/FirstSeveranceLoopStateMachine.cs) owns cycle completion and pending gates.

Each phase must finish its first required cycle. HP clamps at the next threshold until that cycle completes; excess damage is not banked. If the threshold is already reached, completion transitions immediately. **Once that phase has completed a cycle, reaching its threshold interrupts the current later action immediately**; it does not demand a second full lap. Transition cleanup retires that action's hazards. Final is survival under the usual damage/recovery rules, not a no-hit challenge.

Within an attack category, use quick repeated releases; keep the more deliberate breathing room between major categories. Presentation's fast arrival/brake/strike contrast does not authorize shortening harmless warnings.

### Final Core check

After all eight stops, restore **5% of maximum Boss HP once** as a final damage budget, with **10 seconds** to destroy the foreground sphere. This is the last Final action, not a new major phase or UI blackout. Existing overkill is not carried in. The deadline tick accepts damage; later hits cannot rescue a timed-out attempt. Early destruction wins immediately; timeout is `FinalDpsFailed` / Defeat with normal wipe, cleanup and no rewards. Normal Down/revival remains active and same-tick recovery/termination rules are unchanged.

The weakened sphere becomes clearly opaque, develops irregular emissive seams and leaks violet pressure while the distant body remains fragmented. It fires the accepted targeted purple cannon at a 132-tick cadence, after a 24-tick opening. Each shot locks one living roster target, warns for 60 ticks and fires for 60 ticks, with one hit maximum per member per cast. No partial cast is started at the deadline. [Choreography / FinalCheck](../../../Content/Encounters/FirstSeverance/FirstSeveranceChoreography.cs) owns this budget/timer; [CoreCannonVolley](../../../Content/Encounters/FirstSeverance/FirstSeveranceCoreCannonVolley.cs) owns the shared extended one-second live interval used during Final bullet actions too. HP is doubled from the preceding build; Pylon HP is unchanged. The exact roster table remains below in its code owner.

## Pylons, Core and tuning ownership

[PartyScaling](../../../Content/Encounters/FirstSeverance/FirstSeverancePartyScaling.cs) is the sole HP table; values freeze from the pull roster, never survivors or client DPS. Solo reuses the two-player HP/Pylon workload. [EncounterPlan](../../../Content/Encounters/FirstSeverance/FirstSeveranceEncounterPlan.cs) owns Pylon count, shield/open/deadline timing, exposure windows and Overload limit. Actor defaults own defense. Do not duplicate those tables in Status or the handoff.

Pylons are Phase-I shield/DPS gates, not idle decoration: everyone may attack any Pylon; all must be destroyed. Success advances to Stack and permits the normal later Core exposure. Deadline failure removes remaining owned Pylons, increments Overload, applies a nonlethal 35%-maximum-HP pulse and shortens that exposure. Three Overloads cause Defeat. No player-to-Pylon affinity or mid-fight rescaling exists.

The Core can lose its one persistent life pool only during the designated damage substates and above the pending phase floor. Shell/arms/background parts are not additional targets; Phase III retains a foreground aperture at the actual NPC position. Shielded intervals and transition/floor clamps must not be reported as weapon DPS loss without context.

## Stack / 頭割り

- A stationary, server-published **world coordinate**, never a player's moving position, is the gathering target. Opening/embedded sanctuaries and clockwise sites use shared feature geometry.
- At the authority deadline, count connected Alive centers inside the inclusive valid radius. [LanceTuning](../../../Content/Encounters/FirstSeverance/FirstSeveranceLance.cs) owns that radius and Spread separation.
- Every frozen-roster participant is required. Full attendance deals **zero HP**.
- Otherwise every connected Alive participant, including those outside the circle, receives `ceil(maximum HP × (roster count − occupants) / roster count)` once. Armor does not mitigate this Raid penalty; recovery protection and lethal-to-Down still apply.
- Downed members do not count as occupants and do not shrink the requirement. Failure adds no Overload. The score determines the following action.
- Show only the valid acceptance circle with inward guidance, not a misleading outer ring, Gather/SAFE label or remaining-count stamp. [Visual spec](VISUAL_SPEC.md#stack-and-spread-verdicts) owns fragments and verdict animation.

## Spread / 散開

Standing participants receive a shared server resolve tick. At resolution, pairwise authority positions determine whether their danger discs overlap; exact separation equality is safe. Every overlapping participant takes **70% maximum HP once**, irrespective of how many pairs overlap. No overlap means zero mechanic damage. Downed/invalid targets are removed from the required set.

[SpreadBarrage](../../../Content/Encounters/FirstSeverance/FirstSeveranceSpreadBarrage.cs) adds **three or four** concurrent all-standing-player pursuit shots only to **standalone Spread**. It budgets full forecasts, spaces releases and finishes before the Spread verdict; missed starts do not burst on catch-up. Embedded lattice/flood Spread and other attack categories never get the extra barrage. Safe Spread can still coincide with a legitimate independent pursuit hit.

Success/failure is authority sampled; ruby verdict rays are instantaneous result feedback, not new dodgeable projectiles or another damage source. Launch flash/sound is identical; only impact versus premature dissipation differs.

## Attack modules

- **Phase-I Prism:** eight rapid aimed casts for every standing participant simultaneously. Lock forecasts with movement lead before release; the colored forward/reverse order is shared. Main-sequence warnings remain 28 ticks and start-to-start cadence 42 ticks; firing lasts **45 ticks / 0.75 seconds**. Consecutive beams overlap live for 3 ticks, without moving the next warning/fire or increasing hits per cast. The full sequence reserves 367 ticks. Standalone Spread and Final retain their shorter existing windows; post-live contraction remains harmless.
- **Energy pursuit / Stillness:** right/left semi-homing energy charges alternate with two **continuous broad bands**, each 320 pixels wide (half the earlier footprint). Charge approach tracks then locks; dash-through is the intended motion. Stillness retains the 128-pixel safe column, 36-tick warning and 35-tick total live window, with shared thin-pilot amplification; there is no hidden “velocity must be zero” damage test. Category rest, charge steering/speed and damage are unchanged.
- **Phase-II lattice:** fine intersecting corridors use shared clipped grid geometry. Serial-three-and-later ordinary volleys add **one** Boss-origin Core beam. Authority cycles through the eligible standing frozen-roster members, locks the selected position at telegraph start and snapshots that one direction; it does not chase during charge/live. Final widths and the union's one-hit-per-player limit stay unchanged; per-line launch order follows the shared ignition contract below. Protocol29's historical four-ray decoder capacity remains for compatibility, not as the current emission count. The third-volley Spread pockets omit those salvos and resolve Spread during the live grid. **There is no Phase-II Stack.**
- **Twin rotation:** thin-axis forecast/brace followed by two purple magic jets making two accelerating revolutions. Final widths, angles and turn count remain; ignition grows the live geometry below; sword artwork and metallic release audio are retired. Stable internal `RotatingBlade` IDs remain.
- **Remote floods:** short smooth loading → horizontal deployment → accelerating widening leaving a moving safe strip. Stack → Spread → Stack resolves during the corresponding safe holds.
- **Vertical interdict:** the legacy `HalfField` state uses two staggered top/bottom purple-jet waves. Both halves share the former wider jet width. The dense side now has barely whole-body-safe gaps; the easier side has a larger minimum gap. Weighted spacing is deterministic from the accepted action-start tick, with different minimums per side, never local RNG. Wave two centers its jets in the first wave's gaps so remaining still fails. The later action swaps difficult/easier halves. Wrapped edge fragments prevent a free wall sanctuary; every warning and live hit uses the same shared geometry. Outside corona is suppressed so the narrow gaps remain visible. Exact widths/minimums/count bounds belong to `FirstSeveranceImpalingSwords`, not a second tuning table here.
- **Central crush:** remote hands brace, then rapidly close toward the center with a readable pressure forecast. Contact is a lethal Raid hit subject to the normal Down/recovery rules, not bypassed protection.
- **Final bullets:** increasingly dense purple energy nuclei with forecasted birth and bounded trails, alternating with the four-color prism score below. Each bullet action also issues one P2-style Core cannon after its opening beat: authority selects a standing participant in round-robin order and freezes aim at warning. It retains the full P2 warning, dimensions, ignition and fixed source-damage budget, with one hit attempt per participant per cast; bullet motion/density/radius are unchanged. The cast must fit wholly inside its action and is cleared on action/Fight cleanup. [CoreCannonVolley](../../../Content/Encounters/FirstSeverance/FirstSeveranceCoreCannonVolley.cs) owns constants; [Network architecture](../../NETWORK_ARCHITECTURE.md#preceding-development-protocol-v37) owns replication.

Exact motion and hit caps live in [AttackPatterns](../../../Content/Encounters/FirstSeverance/FirstSeveranceAttackPatterns.cs), [Lance](../../../Content/Encounters/FirstSeverance/FirstSeveranceLance.cs), [GridVolley](../../../Content/Encounters/FirstSeverance/FirstSeveranceGridVolley.cs), [SafeWindows](../../../Content/Encounters/FirstSeverance/FirstSeveranceSafeWindows.cs), [ImpalingSwords](../../../Content/Encounters/FirstSeverance/FirstSeveranceImpalingSwords.cs) and [ScoreGeometry](../../../Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs). Ordinary beams use fixed damage, separately from percentage Stack/Spread penalties; never infer collision from bloom.

## Beam ignition and lattice order

All avoidable Doll Raid beams warn with a thin locked axis, launch a narrow pilot from the origin, briefly hold narrow, then rapidly ease to the final width. [BeamIgnition](../../../Content/Encounters/FirstSeverance/FirstSeveranceBeamIgnition.cs) owns the shared width envelope: a 1.5px half-width pilot holds through 1.5 ticks, then quintic amplification completes at tick 7. Ordinary beams extend along their entire path in 3 ticks; **lattice alone instead carries a finite travelling ribbon**, described below. Authority uses integer samples; clients draw the same function at fractional time. Zero-length launch cannot damage even at its source. Rotation's swept samples also use the growing geometry. Short casts retain their end deadlines; no full-width damage precedes amplification. Fixed damage, targets, final widths, Final color order and hit ledgers are unchanged. Spread verdict feedback, energy-body contact, crush and player circles are not new dodgeable beams.

[GridVolley](../../../Content/Encounters/FirstSeverance/FirstSeveranceGridVolley.cs) deterministically shuffles full tracks from accepted serial/start/pattern, then distributes offsets over **30 ticks / 0.5 seconds**. Reveal and fire receive the same offset: every track retains its complete 60-tick warning and 20-tick travel interval. During the embedded Spread volley, whole lanes crossing the four sanctuary pockets are omitted. Remaining beams span edge to edge without internal holes, while preserving the original track clocks and safe body space. [GridPulse](../../../Content/Encounters/FirstSeverance/FirstSeveranceGridPulse.cs) sends the head across the full track in 14 ticks, followed by a tail delayed 6 ticks. The luminous packet tapers over its trailing 42% and leading 14%; the same profile bounds cardinal AABB collision. Space ahead of the head and behind the tail is harmless. No full-corridor hold, rectangular switch-off or damaging residue is left.

The complete staggered volley occupies 110 ticks with a 124-tick cadence, preserving a 14-tick recovery gap. Admission checks include the last stagger so no cast is truncated at the action deadline. Three complete volleys fit the existing 450-tick Lattice action. [SafeWindows](../../../Content/Encounters/FirstSeverance/FirstSeveranceSafeWindows.cs) derives embedded Spread from the third volley: it begins 84 ticks before that reveal and resolves 10 ticks after its last track fires. Pockets, assignments and penalties are retained; their timing follows the slower volley. Core salvos keep their original individual end tick and material. Existing snapshots reconstruct all geometry without extra fields/actors; matching protocol is required.

[CurtainComb](../../../Content/Encounters/FirstSeverance/FirstSeveranceCurtainComb.cs) retains its internal identifier but now describes one continuous band per side, not staggered teeth. Each band shares the Phase-III broad material and the production ignition geometry. Horizontal floods retain their own amplification, safe-strip assignments, holds and embedded mechanic deadlines.

[LanceLedger](../../../Content/Encounters/FirstSeverance/FirstSeveranceLanceLedger.cs) owns the main current assignment plus at most one carried predecessor and independent per-cast participant hit sets. A new forecast never erases or rearms its still-firing predecessor. Expiry is end-exclusive; phase/Fight cleanup clears both. Protocol 35 snapshots append the sustained-main policy and a bounded carried descriptor (at most four locked rays), allowing a peer without earlier snapshots to reconstruct the same overlap. Existing packet IDs and unrelated field layouts stay stable; malformed policies/order/counts are rejected. No client chooses hit duration or damage.

## Four-color Final prism score

Four complete forecasts appear in **red → blue → green → gold** order, all before any fires. They keep their locked coordinates through a reading window, then fire in the same order. Later forecasts remain visible during earlier shots; harmless residue is dim. No numeric labels or arrows. This uses PursuitPrism's final corridor width, shared ignition geometry, finite-rectangle collision, emission material, aperture and launch sound—not the retired thin lattice-tooth effect. Its original short live window is independent of the main eight-cast's sustained hold.

Two adjacent pursuit rays form each occupied band. Red/blue are complementary bands on one axis; green/gold complement one another on a different axis. Axes are selected from vertical, horizontal and the two diagonals using the existing server action seed. The eventual occupied bands of all four forecasts together cover the field, but each individual firing group leaves broad safe bands; there is no permanent hiding point through all colors. It is a movement/sequence-reading attack, not simultaneous field-wide damage. No randomness or retargeting occurs on release.

[ScoreGeometry](../../../Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs) owns the 30-tick reveal cadence, 84-tick reading window after the last reveal, and release cadence accelerating from 60 to 48 ticks. Width and 12-tick live duration reference `LanceTuning`; damage uses the shared native source budget below with at most one accepted hit per participant/color. [RandomComb](../../../Content/Encounters/FirstSeverance/FirstSeveranceRandomComb.cs) owns deterministic complementary geometry; its retained internal name does not imply the old random triples. [EmissionVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceEmissionVisuals.cs) supplies the shared renderer. Late snapshots derive the same locked positions and current reveal/live intervals, never replay earlier shots.

## Raid damage and Adrenaline

[ADR-0024](../../adr/0024-native-raid-hurt-and-downed.md) now routes Raid damage through the receiving player's native Hurt. Geometry and mechanic results remain authoritative. Fixed beam budgets mean source damage, not final HP loss: native defense/DR/shields/dodge may reduce or cancel a hit. Percentage mechanic penalties bypass armor only. The [revive specification](REVIVE_SPEC.md#native-damage-boundary) owns the1-HP floor, pending Down and compatibility limits.

Calamity handles its own Adrenaline/shield/hit reactions through the normal hooks; the old gauge-only bridge is removed. Do not manually reset Adrenaline on a dodge, shield cancellation, successful mechanic or mere HP snapshot. The native policy may differ from the former unconditional full-gauge reset, particularly for tiny hits. Installed-Mod behavior still requires a playtest.

The shared avoidable beam/bullet source budget is **500** (`CombatRules.BeamDamage`; `ScoreGeometry` references it), retuned from the former direct-HP120 budget after the0.3.4 owner test repeatedly resolved to1. It does not scale with player HP or guarantee500 HP loss. Contact charges, Stack/Spread percentages, crush, hit caps and Boss/Pylon HP are unchanged. With300 defense at full effectiveness and50% DR, the basic native calculation is100 before shields/accessories; this is a calibration example, not measured owner gear.

Chalice's deferred bleed is an equipment effect, not a Raid-applied bleeding debuff. Keep it native. Owner-only `NativeHurtDiagnostics` records source/direct damage, defense/DR and the Chalice buffer before/after each Raid intent. `NativeDotSample` samples an active bleed/negative-regen indicator at most once/second, including vanilla bleeding and debuff IDs. Do not sum buffer reductions as HP loss: potions can clear the buffer. See the [scoped compatibility finding](../../research/2026-09-14-implementation-review.md#chalice-follow-up--2026-09-15); lethal non-Hurt damage remains a known boundary, not a solved compatibility claim.

## Recovery, rewards and terminal outcomes

[Revive spec](REVIVE_SPEC.md) owns untimed Down, reusable instant Resuscitation Kit, the recipient-only lockout and all-Down defeat. There is **no active Eliminated**, channel or token system. Unexpected real death/non-Hurt sources and disconnection still invalidate the experiment; native Hurt floors are not production rejoin or universal death interception.

Accepted Defeat clears Raid protection and requests ordinary Terraria death for connected participants only. Other Mods' death cancellation is respected/logged; ordinary difficulty penalties, including Hardcore loss, apply. Victory/cancel/invalidation/unload never force this death.

Only accepted Victory after Final creates one shared world **Curtainfall Treasure Box** (`DollTreasureBox`) per frozen participant at the Core. [Weapons](WEAPONS.md#curtainfall-treasure-box) owns opening contents and one-for-one Work Bench exchanges. This is neither instanced inventory loot nor a final production reward table. Grant-attempt tracking prevents duplicate drops on cleanup retry.

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

[FirstSeveranceTerminalCause](../../../Content/Encounters/FirstSeverance/FirstSeveranceTermination.cs) owns append-only wire values and generic mappings. Victory requires completing survival and clearing the final Core check (retained wire name BossLifeZero). Its timeout uses FinalDpsFailed / Defeat. Overload and all-Down remain gameplay Defeat. Legacy recovery timeout/loop-cap causes remain decodable but are not scheduled by the active untimed recovery/staged fight.

Every terminal publishes a snapshot/tombstone before cleanup. Cleanup releases exact-Fight actors, assignments, containment/flight, player/recovery projections and the Core lease once. WorldUnload/InternalFailure/ProtocolFailure use the definition-owned external mapping, not another feature tick.

## Authority diagnostics

Server/SP diagnostics are passive and scoped to the owning Fight. `CombatStarted` records HP/window tuning. `DamageWindowStarted`, two-second `DamageProgress`, and one `DamageWindowEnded` describe each Pylon/Core window; `PylonDestroyed` records each confirmed kill, and `CombatDpsSummary` totals Core and Pylon damage/open time separately at every ending, including interruption. No per-hit log, player name/position, client-reported DPS, packet change or gameplay decision is introduced.

- `effective_damage` is the authority-observed net target HP reduction, capped to actual target HP; overkill, shields, decorative targets and damage outside the window are excluded. It is party progress, **not per-player attribution or raw weapon DPS**.
- `open_seconds` excludes the Pylon's shield cue and all other phases. `window_dps` averages from that window's opening; `recent_dps` covers only the interval since its previous sample. Time is authority ticks at 60 Hz, not measured wall-clock seconds.
- `target_hp_remaining`, `window_progress_pct`, per-ordinal `pylon_hp`, persistent `boss_life`/`boss_remaining_pct`, and `overload` expose progress. `required_dps_by_deadline` is the remaining target HP divided by remaining open time; it is unavailable after a missed deadline. For the Core this is the rate to finish **in this exposure**, not a mandatory damage quota.
- A Pylon disappears from the live actor list only after its authority-confirmed death; diagnostic state retains zero HP for that ordinal. Missing/unconfirmed actors mark `hp_observation=Incomplete` rather than being counted as kills. Terminal partial windows are recorded before owned actors are removed; repeated cleanup cannot double-count totals. A logging failure cannot interrupt combat or cleanup.

## Fairness and extension boundaries

Server/SP owns outcomes, ticks, geometry, damage and cleanup; client fractional clocks animate accepted facts only. Cooperative Terraria movement/weapon replication is not a modified-client anti-cheat claim. Visual/sound languages must remain useful without color or maximal effects; actual multiplayer dodgeability/performance requires measured playtests, not compilation.

Later phases belong in the feature phase plan/score and adapters; do not add global router switches or a second Raid without scope. HP, timing and damage remain development tuning. Compare equivalent versions, roster sizes and open windows before changing them, and do not inflate Boss HP to conceal weapon imbalance.
