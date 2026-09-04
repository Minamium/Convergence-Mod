---
doc_id: encounter.first-severance.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-04
source_of_truth_for:
  - first_severance.encounter_loop
  - first_severance.mechanics
  - first_severance.terminal_outcomes
aliases:
  - First Severance
  - 第一断絶
  - Pylon Stack Spread Core
related_code:
  - Content/Encounters/ThirdSeverance
  - Common/Raids/Revive
related_docs:
  - encounter.first-severance.plan
  - encounter.first-severance.visual
  - encounter.first-severance.revive
---

# First Severance Encounter Specification

## Identity and scope

| Field | Value | Decision state |
|---|---|---|
| Event | `First Severance` / `第一断絶` | Accepted development name |
| Target key | `first_severance` | Accepted; code rename pending |
| Party | 2–4 frozen pull participants | Accepted |
| Progression | Post Exo Mechs and Supreme Calamitas, Shadowspec-level loadouts | Accepted for Calamity Stage A |
| Boss | `The Null Cantor` / `無響の唱導者` | Provisional working title |
| Location | Polar containment/research facility | Concept accepted; proper name TBD |
| First-slice duration | About 2–4 minutes | Provisional vertical-slice playtest target |
| Eventual full-Raid duration | About 5–12 minutes | Product goal; not a first-slice acceptance claim |

The first implementation proves a multiplayer Raid loop, not final encounter complexity. Solo is out of scope. Rewards, production art/audio, and final tuning are out of scope.

## Decision boundary

Accepted direction is the ordered Pylon → head-split Stack → Spread → Core-exposure loop, multiplayer-first 2–4-player scope, a simple one-NPC Boss, server-owned encounter results, and ally-item Downed/Revive. The exact one-Pylon-per-roster rule, timings, marker sizes, Stack share count, damage values, Overload threshold, exposure penalty, HP, loop cap, and silhouette components are **provisional prototype policy** until Windows multiplayer evidence is reviewed. They are written here so the first implementation is deterministic, not because minami approved them as final balance.

In this document, **Foundation Core** means the activation Tile/Tile Entity. **Boss Core** means the single Boss life pool exposed during `CoreExposure`.

## Lifecycle and active loop

The generic lifecycle permits either a direct terminal End or an optional resolving stage:

```text
Idle projection -> Validating -> Preparing -> Active
                                      |          |
                                      |          +-> TransitionTo(Resolving) -> later End(reason)
                                      +------------> End(reason)
                                                        -> Cleanup -> Idle projection
```

First Severance uses the direct `End(reason)` path for every first-slice terminal. The runtime stores the selected feature terminal cause and returns `EncounterRuntimeUpdate.End` from `Preparing` or `Active`; the current coordinator enters `Cleanup`, publishes the terminal projection, and starts cleanup. It does **not** request `Resolving` and End on the same update. `Resolving` remains available only for a future separately specified result/reward stage that transitions on one tick and ends on a later tick.

`Active` owns this repeated sequence:

```text
SpawnIntro
  -> PylonCheck
  -> Stack
  -> Spread
  -> CoreExposure
       -> boss HP > 0: Reset -> PylonCheck
       -> boss HP = 0: End(Victory) -> Cleanup
```

Boss HP persists through every loop. The Boss can take normal encounter damage only during `CoreExposure`; every other active substate uses an authoritative damage gate. There is no separate damage quota during exposure and no failure merely because the party dealt too little damage in one exposure.

## Provisional timing table

All timers are server ticks at 60 ticks/second. They are prototype defaults, not protocol constants.

| Substate | Duration | Early exit |
|---|---:|---|
| `SpawnIntro` | 180 ticks / 3 s | none |
| Pylon telegraph | 60 ticks / 1 s | none |
| Pylon active window | 600 ticks / 10 s | all current-loop Pylons destroyed |
| Stack telegraph | 180 ticks / 3 s | none |
| Spread telegraph | 180 ticks / 3 s | none |
| Normal Core exposure | 720 ticks / 12 s | Boss HP reaches zero |
| Penalized Core exposure after failed Pylons | 360 ticks / 6 s | Boss HP reaches zero |
| `Reset` | 90 ticks / 1.5 s | none |

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

## Stack / 頭割り

- At substate entry, authority orders the frozen roster by stable Participant ID, starts at `zeroBasedLoopIndex mod rosterCount`, scans forward circularly to the first connected Alive participant, and publishes that target, assignment revision, and resolve tick.
- The marker follows that participant for presentation. At the resolve tick, authority samples server-observed positions and counts connected Alive participants inside the marker. Downed, Eliminated, and disconnected participants never enter the occupant count or damage divisor.
- The provisional required share count is frozen from the pull roster: `2 / 2 / 3` occupants for `2 / 3 / 4` participants. The target counts if still valid and inside. Connected Alive participants outside the marker do not share the hit.
- Stack owns one fixed server-side integer raw-damage pool. Authority computes quotient/remainder by occupant count, orders occupants by stable Participant ID, gives the first `remainder` occupants one extra raw point, and then applies normal mitigation to each assigned share through the measured Terraria hit pipeline. The exact pool is conserved; the client never reports occupant count, divisor, damage, or success.
- Meeting the required share count is success. An under-soak is a soft failure: the same pool is divided among the smaller valid occupant set, so each share is larger, but it adds no Overload and has no separate instant-wipe command. The pool is tuned so one ordinary miss is recoverable for representative gear.
- The first time the target becomes invalid before or on resolution, authority cancels the old assignment, scans forward from that target's frozen-roster position to the next connected Alive participant, increments the assignment revision, and grants a new full 180-tick telegraph. Each Stack cast may be reissued only once.
- If no replacement exists, or the reissued target also becomes invalid, authority commits one `TargetUnavailable` soft-failure result, applies the provisional short Stack-failure debuff once to the remaining connected Alive participants, and advances to Spread after terminal resolution. It never loops or extends the cast again.

Marker radius, damage pool, and failure-debuff duration are provisional tuning inputs. The round-robin ordering, one-reissue limit, revision change, and full-telegraph behavior are protocol-visible first-slice rules; changing them requires matching spec/snapshot/test updates.

## Spread / 散開

- Every connected Alive participant at assignment receives a marker and the same server resolve tick.
- A participant who becomes Downed or invalid before resolution is removed from the required set.
- At the deadline, authority performs pairwise position checks. Initial presentation radius is 7 tiles and initial minimum center separation is 16 tiles; both are provisional.
- Each failed participant receives the failure result once even if overlapping multiple players. Pair iteration order must not multiply damage.
- Success or soft failure advances to Core exposure.

## Core exposure and victory

- Exposure changes the Boss visual state to `Exposed` and opens the authoritative damage gate.
- A clean Pylon check grants the normal 720-tick window; a failed check grants 360 ticks.
- Boss life is one persistent authority-owned pool. Any provisional ring or arm visuals are not hitable parts.
- On each authority tick, permitted hits and life changes resolve before the exposure deadline closes. If Boss HP reaches zero, authority commits Victory and never starts another loop.
- If HP remains at the deadline, close the damage gate, clear loop actors/assignments, run `Reset`, and begin another Pylon check.
- Balance target: a clean representative group should need roughly 4–6 successful exposures. This is a playtest metric, not a separate authority DPS check.

## Downed and recovery interaction

Downed/Revive follows [Revive Specification](REVIVE_SPEC.md) and ADR-0005.

- Downed participants cannot attack, move normally, use items, hook, mount, take encounter damage, or be selected for Pylons/Stack/Spread/Boss targeting.
- Revive may be attempted in every `Active` substate. The opportunity cost is lost movement and damage uptime, not an arbitrary phase lock.
- The frozen pull roster does not shrink. A revived participant returns to the same stable Participant ID.
- If every current participant becomes Downed in the same committed authority tick, Revive produces one Defeat candidate; absent a same-tick Boss Victory, the encounter ends in Defeat once and later revive input cannot undo it.

## Authority tick and terminal precedence

The feature runtime settles one authority tick in this order:

1. collect the complete bounded client-intent batch and server-observed Terraria hit, position, connection/epoch, and control facts without committing a phase edge;
2. validate exact-Fight actor ownership/damage gates and apply accepted Boss/Pylon hit results; collect Boss/Pylon terminal candidates and any participant lethal facts, but do not resolve a due Stack/Spread yet;
3. apply pre-mechanic participant facts to the Revive domain in deterministic event-type and stable-Participant-ID order: lethal transitions, channel interrupts, then the final observed connection/epoch changes for the tick;
4. sample the resulting connected Alive set and server positions, resolve the due Pylon/Stack/Spread/exposure edge once, and immediately apply any mechanic-created lethal transitions to the Revive domain in stable Participant-ID order;
5. apply the one complete, stably ordered revive-start batch after all invalidations, then call `RaidReviveService.CommitTick` exactly once; late commands for that tick are rejected;
6. gather actor/invariant, Boss-life, Overload/loop-cap, and Revive terminal candidates observed by the feature and choose exactly one by the feature priority below;
7. if no terminal exists, commit at most one nonterminal substate edge; otherwise store the generic end reason and bounded feature terminal cause and return direct `End(reason)`;
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

`WorldUnload`, an unhandled `InternalFailure`, and a fatal `ProtocolFailure` originate outside this reducer and unconditionally preempt an uncommitted feature result in that order. The coordinator must synthesize their generic/cause pair from the immutable mapping registered with the encounter definition; it must not re-enter a failed feature tick. The planned external-termination bridge publishes the combined terminal projection before cleanup. Until that bridge exists, feature replication and activation remain disabled.

## Player-count rules

| Pull roster | Pylons | Shared revive tokens | Stack | Spread |
|---:|---:|---:|---|---|
| 2 | 2 | 1 | 2 required shares | all connected Alive assigned |
| 3 | 3 | 2 | 2 required shares | all connected Alive assigned |
| 4 | 4 | 3 | 3 required shares | all connected Alive assigned |

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
- A single ordinary Stack/Spread error is recoverable and readable.
- No frame-perfect input, invisible off-screen hit, required class, or fourfold projectile multiplication.
- Damage numbers, telegraph radii, and exact HP remain provisional until 2/3/4-player Windows telemetry exists.

## First-slice exclusions

Part Break, Targeted Line/Bait, Personal Effigies, Split Reality, Last Stand, multiple Boss parts, route selection, finished rewards, final music, and production VFX are not part of this loop. They are preserved in [Backlog](BACKLOG.md), not silently implemented.
