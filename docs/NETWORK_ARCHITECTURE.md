---
doc_id: project.network-architecture
document_type: governance
status: accepted
owners:
  - networking
last_reviewed: 2026-09-05
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

## Implementation status

Implemented: protocol version/header codec, explicit packet-type values, direction checks, bounded rejection logging, typed handler routing, First Severance activate/Ready/cancel/full-preparation-snapshot transport, feature-neutral terminal descriptors/external mappings, and ordered read-only replica/tombstone behavior.

Not implemented: combat-state deltas/events, revive packets, actor mutation, Barrier correction, or a complete rejoin policy beyond the current bounded full snapshot request. Current custom requests can change only the preparation state; no packet can start combat or report combat outcomes. See [Status](STATUS.md).

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

Authority alone consumes gameplay randomness and transmits assignments or presentation seeds. Feature commands for one tick are collected/bounded and resolved in stable order where arrival order would matter, as already done for competing revive starts. The owning First Severance runtime—not an individual mechanic or the Revive boundary—selects the single transition returned to the generic coordinator.

One authority tick settles in this order:

1. collect complete bounded feature intent and server-observed Terraria hit, position, connection/epoch, and control facts;
2. validate exact-Fight actors/damage gates, apply accepted Boss/Pylon hit results, and collect participant lethal facts without resolving a due Stack/Spread;
3. apply pre-mechanic lethal transitions, channel interrupts, and final observed connection/epoch changes in deterministic type/Participant-ID order;
4. sample the now-current connected Alive set/positions, resolve the one due mechanic, and apply its resulting lethal transitions in Participant-ID order;
5. apply one complete stably ordered revive-start batch, then call `RaidReviveService.CommitTick` exactly once;
6. gather feature-observed terminal candidates and select `EncounterActorMissing > AnchorDestroyed > Invalidated > Victory > Defeat > Cancelled > nonterminal`;
7. commit at most one nonterminal edge, or store generic + feature terminal cause and return direct `EncounterRuntimeUpdate.End` without first requesting `Resolving`;
8. increment/publish one coherent feature/generic revision and terminal tombstone before cleanup releases state.

Thus a participant who becomes Downed/disconnected on a Stack/Spread deadline is excluded before sampling, while mechanic-created lethal damage still participates in the one Revive commit. Allowed Boss damage reducing life to zero wins gameplay Defeat candidates, but an actor/invariant failure observed by the feature takes priority because the gameplay result is no longer trustworthy. The Revive boundary may expose a failure fact to the feature snapshot, but it must not independently publish a competing `EncounterEnded` or return an early coordinator transition.

## External termination bridge

World unload, an unhandled runtime exception, and a future fatal protocol failure originate outside the feature reducer. They are unconditional coordinator preemptions in the order `WorldUnload > InternalFailure > ProtocolFailure`; they are not fabricated as same-tick feature inputs and the coordinator never re-enters a failed `Tick` to obtain metadata.

Slice 2 implements a feature-neutral immutable termination descriptor: generic `EncounterEndReason`, `ushort` feature terminal schema ID, byte schema version, and bounded opaque feature-cause byte. A runtime supplies a contract-validated descriptor for a feature-owned End. Each `EncounterDefinition` also supplies a constructor-validated data mapping for the three external generic reasons, allowing the coordinator to synthesize `WorldUnload`, `InternalFailure`, or `ProtocolFailure` feature metadata without calling mutable feature logic. Missing, duplicate, `None`, or incompatible mappings reject definition construction while activation remains denied.

`EncounterCoordinator.Reset`, the runtime-exception catch, and the bounded queued external-termination entry point use that mapping, discard any uncommitted runtime update, then publish the combined generic/feature terminal snapshot and tombstone before exact-Fight cleanup. Competing queued external requests resolve `WorldUnload > InternalFailure > ProtocolFailure`. Session-creation failure occurs before an encounter is accepted and therefore returns a bounded activation failure rather than pretending that a live feature tombstone existed.

First Severance terminal replication carries the generic `EncounterEndReason` and the append-only feature cause defined in the encounter specification. Full snapshot, terminal delta/event, and retained tombstone must round-trip both fields. `Defeat + LoopCapExceeded` must survive reconnect/snapshot repair; unknown feature-cause values are rejected as protocol-invalid rather than coerced to another result.

## Measured normal-hit pipeline

Do not add a `ReportDamage` packet. Before Boss/Pylon damage is enabled, instrument the pinned versions in Single Player, Host & Play host/non-host, and Dedicated Server for representative melee, ranged, magic, summon/minion, rogue, projectile, penetration/multihit, crit, and Calamity-modified hits. Record which process and hook order observe permission, damage modification, life mutation, death/check-dead, ownership metadata, and `netUpdate`.

The selected adapter must reject stale/wrong-Fight actors, nonparticipants, Pylon hits outside `PylonCheck`, and Boss hits outside `CoreExposure` at a server/SP-observed seam proven by that evidence. Progress reads committed actor life/death once. If the pinned pipeline cannot enforce the feature gate consistently for ordinary unmodified clients, activation remains denied. Passing this gate proves encounter correctness for the stated threat model; it does not prove resistance to a modified Terraria client.

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
