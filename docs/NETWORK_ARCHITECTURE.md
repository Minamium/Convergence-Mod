---
doc_id: project.network-architecture
document_type: governance
status: accepted
owners:
  - networking
last_reviewed: 2026-09-13
source_of_truth_for:
  - architecture.network_authority
  - architecture.packet_policy
aliases:
  - network architecture
  - replication
related_code:
  - Common/Networking
  - Common/Encounters/Runtime
  - Common/Raids/Revive
related_docs:
  - project.architecture
  - encounter.first-severance.plan
  - verification.test-plan
---

# Network Architecture

## Current development protocol v37

Doll snapshots append one strict Boolean after the carried-pursuit section. When present, the single Final core cannon adds serial (`uint`), start tick (`ulong`), target slot (`short`) and unit direction (`float` X/Y): **23 bytes including the flag**. Its origin/dimensions and warning/fire/end are reconstructed from accepted Core coordinates and the P2 cannon constants. Reject nonfinite/nonunit directions, absent roster targets, invalid/overflow clocks, casts outside FinalBullets or its complete action window. No target/hit request is added. The feature attack controller owns scheduling, one-hit-per-member ledger and action/Fight cleanup; a late snapshot contains the entire locked cast. P3 spacing also changes: accepted action-start ticks seed shared deterministic gap positions. Old peers must not draw a different hazard from the same action descriptor. Existing Ghost Samurai payloads and stable packet IDs are retained.

## Preceding development protocol v36

Ghost Samurai appends SlashWave to its bounded native ExtraAI shape enum, bounds finite circle radii to3000px, and validates the dash lock against an18-tick shout gap. Existing shape/packet IDs are not renumbered and the native boss snapshot appends16 bytes for the fixed summon-centered field (X/Y center, half-width/height). It rejects invalid or changed same-Fight bounds. Other payload layouts are unchanged. Matching peers are required. The wave uses the explicitly requested native local-player damage/dodge path in [ADR-0023](adr/0023-ghost-samurai-native-wave-damage.md); this is a narrow exception to encounter-owned player-hit resolution, while server spawning, locked geometry, schedule and exact-Fight cleanup are retained. No new client hit request or global feature switch is introduced. [Ghost Samurai spec](encounters/ghost-samurai/ENCOUNTER_SPEC.md) owns tuning and [Status](STATUS.md) owns evidence.

The integrated protocol35 predecessor-beam snapshot and per-cast hit caps remain unchanged. Version36 includes both the current Doll behavior and this Ghost Samurai field/wave update.

## Preceding development protocol v34

Wire IDs/layouts remain unchanged from v33. Lattice descriptors now derive slower track staggering and finite travelling head/body/tail geometry instead of a whole-line rectangle; sanctuary cuts share one parent track clock. Authority collision and client fractional rendering use that same tapered profile. Embedded Spread follows the third volley's derived schedule. Older peers must be rejected because the unchanged bytes mean different timing/geometry, not because of a new packet. No new target requests, client RNG, actors, hit-ledger or cleanup ownership is added. [Beam ignition and lattice order](encounters/first-severance/ENCOUNTER_SPEC.md#beam-ignition-and-lattice-order) owns the rules. Ghost Samurai's [v33 feature](encounters/ghost-samurai/ENCOUNTER_SPEC.md) is retained unchanged.

## Preceding development protocol v32

Packet IDs, layouts and request bounds are unchanged from v31. The version advances because both peers now derive beam length/width during ignition and each lattice line's reveal/fire/end offset from the same descriptor. Older peers would draw full-width instant hazards against the new narrow growing authority geometry, so they must be rejected rather than silently mixed. [Beam ignition](encounters/first-severance/ENCOUNTER_SPEC.md#beam-ignition-and-lattice-order) owns the timing/geometry rules. No client RNG, target requests or per-line actors/packets are added; Fight ownership, hit ledgers and cleanup remain unchanged. The existing Ghost Samurai payload/behavior is preserved; its [feature spec](encounters/ghost-samurai/ENCOUNTER_SPEC.md) owns the preceding v31 addition.

## Preceding development protocol v28

Packet IDs and layouts are unchanged. Preparation uses the full active server roster rather than radius-filtered candidates. Its shared180-tick deployment clock delays Ready acceptance and starts the Ready timeout afterward. Existing EnteredTick/ArenaBounds determine deployment and field geometry; no client submits a roster/teleport target. The server cancels on membership change before processing Ready/start, publishes30-tick preparation repairs, and retains all-Ready for45ticks before Active. All Ready flags are exact-slot/epoch/nonce authoritative. Both cinematic/field presentation and the new head labels consume those snapshots; matching peers are required. [Arena infrastructure](ARENA_INFRASTRUCTURE.md#deployment-and-presentation--0241) owns the behavior and cleanup.

## Preceding development protocol v27

The field layout and packet IDs are unchanged. Matching peers are required for the revised HP floors and immediate post-first-cycle transitions. Cleanup/Defeat may retain a bounded cosmetic combat projection for an in-progress HalfField or RemoteCrush action even without a same-tick Stack/Spread verdict. The authority cache binds exact sequence/Fight/terminal tick; codec validates the action interval, and clients never install this terminal payload as Active combat. This closes the same-tick lethal-impact audio hole. Existing mechanic-terminal rules remain. New Active/world cleanup clears the cosmetic cache; no actors, requests or capabilities survive.

## Preceding development protocol v26

The Spread descriptor layout is unchanged, but its count bound is now0–4 (maximum449bytes including count), with3casts in short windows and4in ordinary windows. Shared window validation accepts only standalone Spread, never embedded lattice/flood windows. Start validation uses the window-specific shot count and wider spacing. Matching peers are required for these changed bounds/timing; existing IDs, server ownership, hit caps and cleanup are retained. The preceding v25 values below are historical.

## Preceding development protocol v25

Combat snapshots append `SpreadCastCount : byte` (0–8) after mechanic impacts. Each cast contains `Serial:uint`, `StartTick:ulong`, `Step:byte`, `TargetSlot:int16`, `RayCount:byte` (1–roster count, maximum4), followed by that many six-float immutable lance rays. Kind is implicitly PursuitPrism and MotionTick is zero. Maximum added section:897bytes. Counts are checked before allocation; projection validation rejects duplicate serials, non-increasing steps/ticks, foreign focus slots, nonfinite rays, early starts, incompatible windows and casts intruding on the final Spread-settle interval. Truncated packets are rejected by the existing router boundary. Normal P1 casts and concurrent Spread casts share the Fight-owned serial allocator.

The feature-local Spread runtime owns at most eight descriptors and32 cast/member hit keys. It runs alongside the existing lance/grid/score update in the same authority tick, publishes locked targets immediately, and clears on action change, window end and exact-Fight cleanup. Renderer emitters (maximum12 including cooling casts) and bounded sound cursors consume these descriptors without retargeting. Existing packet IDs, requests, ownership, HP/recovery and persistence are unchanged. The shared Final read-ahead timing also requires matching peers. [Encounter specification](encounters/first-severance/ENCOUNTER_SPEC.md#random-final-triples) owns the timing and all-Spread behavior.

## Preceding development protocol v24

`RaidHit = 69` is a definition-routed, **server-to-owning-client only** event for accepted encounter HP damage. Its existing header carries exact Sequence/Fight and a per-participant positive hit revision; its entire body is one positive `Int32` damage amount. No client-provided hit, HP mutation or new saved state is introduced. The router rejects the reverse direction; the feature parses the bounded payload before checking its current connected local participant. The player's per-Fight receipt rejects wrong versions/Fights/sequences and duplicate/stale revisions; cleanup resets it. Calamity-specific effects remain in the compatibility adapter.

Authority sends the receipt immediately after invulnerability/debug/zero-damage rejection and before the HP/Down transition, including a terminal lethal hit. Reliable ordered Mod packets deliver it before that same connection's cleanup snapshot. It is deliberately independent of periodic HP corrections: a heal can mask a loss, and a terminal presentation snapshot must not reapply player state. Single Player applies directly once; remote-player projections do not repeat it. `RaidHitApplied` on the owning client can be correlated with authority `RaidDamage`. It is a receipt log, not proof that a disabled/active-burst/Nanomachine gauge should clear. Matching peers are required; existing IDs, geometry, HP and recovery contracts remain unchanged.

## Preceding development protocol v23

No wire fields change. Version23 requires identical peers for the wider Final comb pitch and shifted second sword wave; both still reconstruct through shared authority geometry.

## Preceding development protocol v22

Wire layout remains unchanged. Random Final geometry uses the already replicated server-owned `ActionStartedTick` (authority `SubstateEnteredTick`), step and pulse as an explicitly mixed integer seed, not client RNG. Identical seed reconstructs the same three orientations/offsets on authority, visual and sound paths. Version22 rejects older peers because derived geometry and action duration changed. No per-ray packet/resource or new global routing is introduced.

## Preceding development protocol v21

The wire layout, operation IDs, ownership and terminal contract remained v20. Version21 required matching peers because Iron Interdict sword geometry, denser/earlier Final slicers and wider Spread/lattice pockets derived locally from the accepted action epoch. A peer with older geometry must not render a different warning from authority collision. No per-sword packet or actor was added. [Encounter evolution](history/2026-09-11-encounter-evolution.md#iron-interdict-and-current-spacing-override) preserves this historical change; [current attack modules](encounters/first-severance/ENCOUNTER_SPEC.md#attack-modules) own present behavior.

## Preceding development protocol v20

The bounded combat snapshot appends a cosmetic mechanic result: authority tick (`ulong`), target count (`byte`,0–4 and at most the roster), then participant ID (`byte`), sampled X/Y (`float` each) and failed flag (strict Boolean) for each target. Maximum addition49 bytes. Identity must belong to the snapshot roster, IDs are unique, positions finite/bounded, and a success cannot mark a failed recipient. Gameplay damage/assignment/deadlines are unchanged; no client result/hit request or new operation ID is added.

The feature keeps at most one immutable same-tick Defeat presentation snapshot after runtime cleanup because common snapshot transport drains later. Only the exact terminal Sequence/Fight/tick can read it; attachment of a new Fight and world reset clear it. `Cleanup + Defeat` may carry this bounded combat-shaped **presentation record**, but the client never installs it as Active combat or reapplies its health/input protection. It is used only after a preceding matching local combat and is not replayed on fresh join. Existing authority terminals, one-Fight cleanup and lifecycle priority remain unchanged. All peers must update; [visual spec](encounters/first-severance/VISUAL_SPEC.md) owns the effect, [Status](STATUS.md) the evidence.

## Preceding development protocol v19

Packet IDs, field layouts and Fight ownership are unchanged. Stillness retains two locked footprint descriptors; [CurtainComb](../Content/Encounters/FirstSeverance/FirstSeveranceCurtainComb.cs) derives bounded center-out teeth and individual reveal/fire/end clocks. Authority hits and client presentation use the same helper; the volley/phase deadline includes the last tooth. No per-tooth actors or packets are added. Grid pattern bit2 is retired with Phase-II Stack: only0–3 and8–11 are accepted; Spread-pocket descriptors cannot carry Core salvos. All peers must update together because the derived geometry/timing and validation changed. [The encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#attack-modules) owns behavior; [Status](STATUS.md) owns evidence.

## Preceding development protocol v18

The grid `Pattern` byte introduced low bits0–1 for layout, bit2 for a Stack sanctuary and bit3 for four Spread sanctuaries. v18 accepted0–11; v19 retires the Stack bit above. Bounded geometry derives from the validated descriptor and action epoch, without serialized grid rays, new requests or saved Raid state. v18 also changed Final's derived timing.

## Preceding development protocol v17

[ADR-0022](adr/0022-definition-routed-encounter-transport.md) replaces feature-global operation registration with bounded definition routes and a common active-session snapshot publisher. The fixed header and operation IDs are unchanged; routed packets add one length byte plus 1–64 ASCII key bytes. Empty key is the common Idle Snapshot (one ulong authority tick); RequestSnapshot is header-only and always repairs the server's current session. All peers update together. First Severance's feature-body bounds and gameplay are unchanged.

## Preceding development protocol v16

[ADR-0021](adr/0021-untimed-recovery-and-simultaneous-prism.md) keeps the field layout but permits1–4 locked rays in a PursuitPrism volley, additionally bounded by the combat roster count. Its maximum attack section grows74→122bytes; other attack kinds retain their original shape/count constraints. TargetSlot is only Prism's representative pose focus. Authority supplies every ray simultaneously; clients never infer another target from local positions. The feature rejects Eliminated combat projections and treats zero DownedDeadlineTick as untimed Down when CombatState is Downed. The same shared geometry/timing defines expanding horizontal floods, two blade turns and accelerating/shifting Final attacks. All peers must update; packet IDs, nonce requests, persistence and exact-Fight ownership are unchanged.

Preceding v15 appended `RemoteCrush = 14` to the stable substate enum without changing the DTO. The score/phase indices remain bounded and constructor-validated, including after the current timing revisions. [Status](STATUS.md) owns verification.

## Preceding development protocol v14

The one-member debug admission in [ADR-0020](adr/0020-development-solo-admission-and-terminal-hud.md) changes the accepted preparation/combat roster bounds to **1–4**, without changing v13 field layout, packet IDs or client request shapes. NPC extra-AI accepts the same real count. Admission remains server/SP build-policy controlled; parsing one-member observations does not authorize a client to lower the server minimum. All peers require v14. Result cinematics are local consequences of existing accepted terminals, not new packets or delayed cleanup. [Status](STATUS.md) owns verification.

## Preceding development protocol v13

The stage/ordered-action descriptor in [ADR-0019](adr/0019-phase-scores-and-terminal-survival.md) extends v12 by10 bytes after BossPhaseStartedTick: ActionStartedTick (`ulong`), ActionIndex (`sbyte`, -1 or a score-bounded index), CompletedPhaseCycles (`byte`). The existing optional grid/Core payload follows unchanged. Stage/action compatibility, ordered ticks, finite core/Stack positions and zero Final HP are validated before snapshot application. Stable enum values are appended, not renumbered; all packet IDs/requests remain unchanged. Both sides derive new fixed geometry/timing from this matching version, never from client actions or audio playback. Later intermediate phases can precede the explicit Final stage.

The preceding v10–v12 field and ownership changes are recorded in [ADR-0016](adr/0016-ground-containment-and-continuous-emission.md), [ADR-0017](adr/0017-boss-stages-fixed-stack-and-lattice.md) and [ADR-0018](adr/0018-roster-health-and-core-salvos.md). Current results belong to [Status](STATUS.md).

## Preceding development protocol v9

`0.2.7` retains the entire v8 field layout, numeric packet IDs and request shapes. The protocol bumps because both peers derive warning/fire/lock/end deadlines from shared tuning, and the user restored fast steps inside attack combos while retaining the deliberate category-opening and full-combo pauses. All peers must update together; old timing must not silently render against new authority collision. See the [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) for current timing. Auxiliary permissions, snapshot bounds, ownership and cleanup are unchanged.

## Preceding development protocol v8

`0.2.6` appends `DebugAssistProtected : bool` after `ReviveLockoutUntilTick` in each combat participant record: 62 bytes per participant, at most four additional bytes for the roster. Decode reads a strict byte (`0` or `1`), rejects other values and rejects `true` on disconnected/non-Alive projections. All packet IDs, client requests, attack layout and v7 tuning are unchanged. All peers must update together.

This flag reflects an exact-Fight, console-granted developer lease, not a client request or persisted permission. The ordinary repair snapshots renew a five-second owner-client TTL; termination and invalid membership clear it immediately. Command/lease authority and cleanup are specified in [ADR-0015](adr/0015-console-only-single-pull-assist.md). `Common` receives no feature switch and no grant packet is exposed.

## Preceding development protocol v7

`0.2.5` retains the v6 field layout but bumps the protocol because peers derive new charge fire/lock deadlines and ray geometry from shared tuning. v6 peers must update; packet IDs are unchanged. `0.2.3` originally inserted `MotionTick : ulong` after attack target slot and before ray count. It is zero for fixed beams, or inside `[StartTick, EndTick)` for an energy charge. Charge rays encode head position, unit heading and fixed body dimensions (length 164 / half-width 50); `RayAt` produces the swept collision rectangle. Maximum attack section is 74 bytes (50 for one charge). Existing kind values and packet IDs are retained; v5 peers must update. The exact-Fight authority samples charge motion, publishes every four tracking ticks, at the prelaunch lock and at fire; the locked position/heading remain still for 24 harmless ticks, then straight motion extrapolates from that sample. All other phase, identity, terminal, bounded decoding and cleanup rules remain intact. See [ADR-0013](adr/0013-energy-charge-and-client-feedback.md).

## Preceding development protocol v5

`0.2.2` extends the nullable bounded attack section: present byte; nonzero uint serial; ulong start tick; byte kind (`ObservationLance=0`, `PursuitPrism=1`, `SweepRight=2`, `Stillness=3`, `SweepLeft=4`); byte step (`0..7`, validated per kind); signed short target slot (`-1..254`, nonnegative for new patterns); byte ray count (`1..2`); six floats per ray (origin X/Y, unit direction X/Y, length, half-width). Maximum section size is 66 bytes, 20 more than v4. Finite coordinates (absolute value ≤1,000,000), length 32..3,600, half-width 4..320, normalized direction, compatible kind/step/ray shape, phase and phase deadline are constructor-validated. Sweep motion is derived from server start tick, never sent per frame. Exact shared-buffer consumption remains mandatory.

All numeric packet IDs and nonce-only client requests are unchanged. Both peers must reload the same build; the header rejects old protocol versions. The same immutable attack descriptor drives authoritative hit geometry and client drawing, capped at two global rays for all roster sizes. A sequence holds an authority-selected Alive focus, re-locking each step; clients do not choose targets or outcomes.

The existing accepted terminal is also the Defeat death order, not a new client-to-server command. The owner checks its previous connected combat membership, exact sequence/Fight match and generic Defeat, marks that sequence executed, clears local Raid protection, and invokes normal `Player.KillMe` once. Subsequent snapshots cannot repeat it. Ordinary Terraria death synchronization remains inherited, not an anti-cheat guarantee; a client can violate Terraria's cooperative trust. Server cleanup logs valid bound recipients, and the owner logs whether a third-party `PreKill` hook cancelled death. A tombstone received without a preceding local combat, a stale/wrong Fight, outsiders, Victory, cancellation and unload never cause this action. See [ADR-0012](adr/0012-pattern-sequences-and-defeat-death.md).

## Development protocol v4

`0.2.1` appends `ReviveLockoutUntilTick : ulong` after WeaknessUntilTick in each bounded combat participant record (at most 32 extra bytes). The authority owns the 60-second recipient deadline. Existing numeric packet IDs, nonce-only client requests and the bounded lance section are unchanged; the legacy token counter is reserved zero. Host and clients must reload together.

First Severance selects reusable, instant, resource-free revival under [ADR-0011](adr/0011-instant-revival-and-recipient-lockout.md). The old channel/token transport discussion below is historical/future adapter context, not current held-use behavior. After same-tick damage, authority revalidates the collected requests and submits one stable batch, then commits zero-duration recoveries in that same tick. A queued request gets its validation reply only after revalidation. Local equipment/control sync precedes custom revive intent; neither it nor a buff icon is an authoritative completion claim.

## Development protocol v3

Development `0.2.0` appends a nullable observation-lance section after combat participants: one Boolean byte, then (when present) nonzero uint serial, ulong start tick, byte ray count `1..2`, and four finite float values per ray (origin X/Y and unit direction X/Y). Fire/end ticks and length/width come from the same versioned feature tuning on both sides. Maximum addition is 46 bytes. Constructor validation rejects invalid normalization, nonfinite/out-of-bound origins, invalid ticks, attacks outside Pylon/Exposure, and a volley extending beyond its phase. No new request or packet ID is added. Protocol v2 peers must reload the same `0.2.0` build. See [ADR-0010](adr/0010-giant-boss-observation-lances.md).

Development `0.1.1` adds request IDs 5 (`RequestPrototypeDown`) and 6 (`RequestReviveNearest`), each a single nonzero uint nonce after the existing envelope. Full snapshots gain a bounded combat section for phase/deadline, Boss life, tokens, Stack target and at most four participant control/health projections. Each HP correction has a monotonic participant revision; clients apply it once. Health, success and positions are never accepted from these request payloads. Existing IDs and terminal ordering stay unchanged. See [ADR-0009](adr/0009-development-combat-experiment.md) for the experimental exception and [Status](STATUS.md) for unverified seams.

## Implementation status

The `0.1.2` recovery hotfix does not change protocol v2 or packet layouts. Requests 5 and 6 decode their fixed uint nonce before header/sender/rate validation. The receiver must not compare `BaseStream.Length - Position` to 4: the supplied reader can belong to a shared buffer. See the pinned-source evidence in [First Severance API research](research/FIRST_SEVERANCE_SLICE3_APIS.md).

Implemented: protocol version/header codec, explicit packet-type values, direction checks, bounded rejection logging, typed handler routing, First Severance activate/Ready/cancel/full-preparation-snapshot transport, feature-neutral terminal descriptors/external mappings, and ordered read-only replica/tombstone behavior.

Not implemented: combat-state deltas/events, production hit/death integration, general outsider correction, or a complete rejoin policy. Participant containment and owning-client prediction are implemented. The development requests start combat after all Ready and request Down/revive intent, but never report hit or completion outcomes. See [Status](STATUS.md).

## Goals

- 2–4 players and Dedicated Server observe the same Raid state.
- Delay, duplication, reordering, stale fights, or high ping cannot change authority outcomes.
- No custom feature packet can directly declare participant identity, DPS, position-check success, assignment, revive completion, life, or victory.
- Join/rejoin/revision gaps recover from a bounded full snapshot.
- Cleanup and terminal state remain observable even after the active runtime releases.
- Presentation density does not scale custom traffic every tick/particle.

## Threat model and inherited Terraria trust

The first slice targets cooperative multiplayer with unmodified tModLoader/Calamity clients. Convergence authenticates custom-message direction/sender bindings and owns feature results, but Terraria's ordinary player movement, item, projectile, hit, and life replication still supplies some server-observed facts. “Server-observed” does not mean cryptographically trustworthy or cheat-proof against a modified client.

The implementation must not overclaim anti-cheat. It prevents custom Convergence packets from directly setting outcomes and validates exact Fight, actor ownership, lifecycle, damage gate, arena bounds, stable bindings, and plausible state at the latest measured authority seam. Full movement/hit anti-cheat, client attestation, and reconciliation against arbitrary modified clients are outside the first slice unless separately specified and tested.

## Authority state

Server/SP owns lifecycle/Fight identity, Core/Arena, frozen roster and epochs, Ready, active substate/deadlines/assignments, Boss/Pylon actor state, Stack/Spread resolution, Boss life/damage gate, Downed/Revive/tokens, Overload, outcome, and owned actor spawn. Normal combat enters through the measured Terraria/tModLoader hit pipeline; the feature authority decides whether the exact-Fight actor may accept that observed hit and derives progress only from committed actor life, never from a custom DPS report.

Clients own input intent, UI/subtitles/markers, decorative VFX/audio, presentation interpolation, and optional predictive Barrier clamp. Prediction never becomes truth.

## Lifecycle

```text
Idle -> Validating -> Preparing -> Active
                       |           | \
                       |           |  +-> [optional Resolving] -> End
                       +-----------+-----------------------------> End
External forced end ---------------------------------------------> End
                                                                   |
                                                                   v
                                                               Cleanup -> Idle
```

Raid Ready/roster is a `Preparing` substate, not a generic lifecycle addition. `Resolving` is optional and requires its own later tick; First Severance uses direct End for the initial slice. Every failure/cancel/unload reaches Cleanup; no exception path jumps directly to Idle. Terminal Encounter Sequence/Fight ID/revision/end reason is published before release.

## Packet envelope

Every custom packet begins with:

```text
ProtocolVersion : ushort
PacketType      : byte
EncounterSeq    : ulong
FightId         : 16 bytes
Revision        : uint
Payload         : fixed/bounded per-type data
```

Existing packet IDs are explicit and reserved:

| ID | Direction | Name | Current behavior |
|---:|---|---|---|
| 1 | C → S | `RequestActivate` | implemented for Foundation Core candidate anchor + request nonce |
| 2 | C → S | `RequestSetReady` | implemented for exact-Fight Ready/unready intent |
| 3 | C → S | `RequestCancel` | implemented for exact-Fight initiator cancel intent |
| 4 | C → S | `RequestSnapshot` | implemented with sender/rate validation |
| 5 | C → S | `RequestPrototypeDown` | single nonzero nonce; development-only self-Down intent |
| 6 | C → S | `RequestReviveNearest` | single nonzero nonce; authority chooses nearest eligible ally |
| 64 | S → C | `Snapshot` | implemented for generic lifecycle + bounded preparation projection |
| 65 | S → C | `StateChanged` | direction parsed; transport not implemented |
| 66 | S → C | `ParticipantChanged` | direction parsed; transport not implemented |
| 67 | S → C | `ValidationResult` | implemented for accepted/rejected request response |
| 68 | S → C | `EncounterEnded` | direction parsed; transport not implemented |

Future revive intent needs logical `RequestStartRevive(targetParticipantId, requestNonce)` and `RequestCancelRevive(channelNonce)` forms. Assign new unused explicit numeric IDs only in their implementation commit; never renumber existing values. Completion is server-to-client state/event, never a client request.

## Decode and validation order

1. bound and parse the complete type-specific DTO without mutation;
2. validate protocol, packet type, direction, enum/count/coordinate/string ranges;
3. derive sender from `whoAmI` and reject non-player/sentinel slots;
4. validate Encounter Sequence, exact Fight ID, revision/nonce, lifecycle;
5. resolve stable Participant ID and current server binding/connection epoch;
6. validate feature-specific Core/range/item/state/rate conditions;
7. submit an authority command for deterministic tick processing.

Never trust a player index/name/UUID, Core coordinate, distance, DPS/hit, mechanic success, life value, or timer carried by a custom payload. `RequestedAnchor` remains a candidate until the server resolves a real Foundation Core TE and validates prospective bounds. Range and position checks use the authority process's current Terraria state under the cooperative-client threat model above, not coordinates copied from the feature request.

Do not use `BinaryReader.BaseStream.Length` as a Mod packet boundary: tModLoader may expose a shared receive buffer. Fixed fields and individually bounded counts/strings are the safe contract.

## First Severance full snapshot

The feature snapshot is bounded by the 2–4 roster and actor caps. Minimum content:

- generic sequence/Fight/lifecycle/revision/authority tick/end reason plus the explicit byte-valued `FirstSeveranceTerminalCause` (`None` while live);
- frozen participant IDs, current slots/epochs, connection/Ready/combat state;
- validated arena/Core identity and Barrier state;
- active substate, start/resolve tick, loop index, Overload;
- Boss compact handle, life ratio, damage-gate and visual state;
- at most four Pylon handles and states;
- current Stack target/assignment revision/reissue state or Spread participant set, resolve tick, committed result;
- normal/penalized exposure state;
- bounded `RaidReviveSnapshot`: tokens, combat states/deadlines, current leases/nonces;
- owned actor/schema version needed to reject stale references.

The development v3 snapshot additionally carries the currently locked observation-lance volley described above. The exact-Fight runtime owns an at-most-four-participant once-per-volley hit ledger; it clears on phase exit and Fight cleanup. No client supplies a ray, target position or hit report. Assignments and first firing tick trigger immediate observable changes, with existing 30-tick snapshots as repair. Animation, particles and audio do not emit packets.

Player names/display strings are presentation lookup, not authority identity.

## Snapshot/delta policy

- Full snapshot accepts a newer Encounter Sequence and repairs revision gaps.
- Delta/event applies only to exact current sequence/Fight and expected newer revision.
- Replica orders `(EncounterSequence, Revision, AuthorityTick)` and retains terminal tombstones so delayed live state cannot revive an ended fight.
- Unknown/stale/duplicate messages do not mutate state; revision gaps request a snapshot.
- Server increments revision on observable coarse changes, not every hit/tick.
- Send phase start/resolve ticks and occasional clock correction; clients interpolate countdowns.
- Sync authoritative actor handles/states, not decorative particles/trails/audio samples.
- Live snapshots may coalesce, but terminal state has priority and bounded retention.

## Deterministic tick policy

The [feature specification](encounters/first-severance/ENCOUNTER_SPEC.md#authority-tick-and-terminal-precedence) and its combat orchestrator own the actual tick order. Parse complete bounded intents before mutation; settle damage before the single recovery commit; select one terminal; publish its snapshot/tombstone before cleanup. No network callback, actor, or recovery service independently ends the generic session. Do not duplicate the feature's evolving phase order here.

## External termination bridge

World unload, an unhandled runtime exception, and a future fatal protocol failure originate outside the feature reducer. They are unconditional coordinator preemptions in the order `WorldUnload > InternalFailure > ProtocolFailure`; they are not fabricated as same-tick feature inputs and the coordinator never re-enters a failed `Tick` to obtain metadata.

Slice 2 implements a feature-neutral immutable termination descriptor: generic `EncounterEndReason`, `ushort` feature terminal schema ID, byte schema version, and bounded opaque feature-cause byte. A runtime supplies a contract-validated descriptor for a feature-owned End. Each `EncounterDefinition` also supplies a constructor-validated data mapping for the three external generic reasons, allowing the coordinator to synthesize `WorldUnload`, `InternalFailure`, or `ProtocolFailure` feature metadata without calling mutable feature logic. Missing, duplicate, `None`, or incompatible mappings reject definition construction before that definition can register.

`EncounterCoordinator.Reset`, the runtime-exception catch, and the bounded queued external-termination entry point use that mapping, discard any uncommitted runtime update, then publish the combined generic/feature terminal snapshot and tombstone before exact-Fight cleanup. Competing queued external requests resolve `WorldUnload > InternalFailure > ProtocolFailure`. Session-creation failure occurs before an encounter is accepted and therefore returns a bounded activation failure rather than pretending that a live feature tombstone existed.

First Severance terminal replication carries the generic `EncounterEndReason` and the append-only feature cause defined in the encounter specification. Full snapshot, terminal delta/event, and retained tombstone must round-trip both fields. `Defeat + LoopCapExceeded` must survive reconnect/snapshot repair; unknown feature-cause values are rejected as protocol-invalid rather than coerced to another result.

## Measured normal-hit pipeline

Do not add a `ReportDamage` packet. Before claiming production-complete Boss/Pylon hit handling, instrument the pinned versions in Single Player, Host & Play host/non-host, and Dedicated Server for representative melee, ranged, magic, summon/minion, rogue, projectile, penetration/multihit, crit, and Calamity-modified hits. Record which process and hook order observe permission, damage modification, life mutation, death/check-dead, ownership metadata, and `netUpdate`.

The selected adapter must reject stale/wrong-Fight actors, nonparticipants, Pylon hits outside `PylonCheck`, and Boss hits outside the feature's current damage states and HP gate at a server/SP-observed seam proven by that evidence. Progress reads committed actor life/death once. If the pinned pipeline cannot enforce the feature gate consistently for ordinary unmodified clients, that production adapter must remain disabled. The authorized development adapter is not a production-completeness claim. Passing this gate proves encounter correctness for the stated threat model; it does not prove resistance to a modified Terraria client.

## Actor identity and ownership

Do not put GUIDs or strings in NPC/Projectile `ai[]`. Use a compact per-Fight owner token/actor ID in per-entity data and a server registry mapping it to exact Fight and `whoAmI`. Cleanup uses the registry first and a bounded defensive scan only as fallback.

## Barrier and player corrections

Clients may predict movement inside bounds. Authority samples its current Terraria position for the bound player, validates arena bounds/current epoch, and sends an exact-Fight correction. Other-Mod teleport is handled by validating the observed result, not an impossible exhaustive blacklist. This correction protects encounter geometry for cooperative clients; it is not a complete movement anti-cheat. Downed control state is a projection from authority and is defensively cleared on cleanup/load.

## Revive transport

Start request carries target stable Participant ID and request nonce only. Server validates sender/binding/held item/range/states/tokens and submits a bounded same-tick batch. Cancel must include the exact accepted channel nonce; stale cancel cannot affect a new channel. Movement/damage/teleport/item/hook/mount/disconnect interrupts originate from server-observed adapters. Raw held-item use/release is sampled before applying the reviver control projection: `SuppressItemUse` blocks non-revive actions but must preserve the accepted revive lease and must not self-cancel it. Server alone emits completion, life restore, immunity/weakness, and token change.

## Security and failure behavior

- Bound all payloads and request rates before allocation/work.
- Parse then validate then mutate; partial DTOs never reach the domain.
- Unknown/stale/malformed input is ignored with rate-limited structured logs.
- Feature packets from non-participants or multiplayer clients attempting S→C types are rejected.
- Ordinary Terraria movement/combat replication is handled under the cooperative unmodified-client threat model; log it as inherited trust rather than claiming custom-packet validation makes it cheat-proof.
- Never let a bad packet throw out of the server thread.
- Logs include Fight identity/reason/counts, not personal data or per-tick positions.

## Cleanup publication

On terminal outcome, stop new commands/spawns, commit the final feature/revive state, enqueue/broadcast terminal snapshot/`EncounterEnded`, then remove actors/projections and release the session. Cleanup is exact-Fight and idempotent. Delayed packets for the ended Fight hit the replica tombstone or server identity check and cannot mutate a new encounter.
