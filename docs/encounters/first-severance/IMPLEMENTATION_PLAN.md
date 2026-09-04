---
doc_id: encounter.first-severance.plan
document_type: plan
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-05
source_of_truth_for:
  - first_severance.implementation_sequence
aliases:
  - First Severance implementation plan
  - Windows implementation queue
related_code:
  - Content/Encounters/ThirdSeverance
  - Common/Encounters
  - Common/Raids/Revive
  - Common/Networking
related_docs:
  - encounter.first-severance.spec
  - project.status
  - handoff.windows
---

# First Severance Implementation Plan

This plan takes the current inert `ThirdSeverance` bootstrap to the first playable `FirstSeverance` vertical slice without weakening server authority, cleanup, or multiplayer evidence. Activation remains denied until the slice that owns each required adapter can prove it safe.

## Non-negotiable boundaries

- Server or Single Player local authority owns lifecycle, roster, ticks, assignments, entity spawn, feature damage gates, committed actor life, Downed/Revive, victory, and cleanup.
- Clients send bounded intent and render read-only snapshots/events. No custom packet reports DPS, position-check success, revive completion, life, or player identity as truth.
- `Common` does not depend on `Content` or `Client`; feature behavior stays in `Content/Encounters/FirstSeverance`.
- Calamity access stays in `Common/Compatibility/Calamity` behind project-owned contracts.
- Every transient actor is registered to exact `FightId` ownership immediately and cleanup is idempotent.
- Terminal snapshot/outcome is committed before the active runtime and player projections are released.
- Existing numeric packet IDs are never renumbered during the feature rename.

The first slice assumes cooperative multiplayer with unmodified clients. Terraria/tModLoader supplies ordinary movement and combat facts observed by the server; custom-message authority is not a claim of anti-cheat against a modified client. The exact normal-hit and lethal-hook seams must be measured on the pinned runtime before their respective adapters are enabled.

## Ownership target

| Area | Responsibility |
|---|---|
| `Common/Encounters/Runtime` | generic lifecycle, exact-Fight ownership, update/cleanup contracts, terminal publication |
| `Common/Raids/Revive` | reusable pure Downed/Revive authority state machine |
| `Common/Networking` | envelope, sender/direction validation, bounded transport, generic replica plumbing |
| `Content/Encounters/FirstSeverance` | roster composition, arena adapter, loop executor, Boss/Pylons/mechanics, tuning, feature snapshot |
| `Client/Encounters/FirstSeverance` | markers, countdowns, Boss presentation, VFX/audio/accessibility from replicated state |
| `Common/Compatibility/Calamity` | progression and lethal-hook compatibility facts only |

Do not build a generic mechanic DSL for this first consumer.

## Slice 0 — Windows baseline and evidence

Status: **Complete (2026-09-05).** The confirmed versions and sanitized run are recorded in the [Version Matrix](../../VERSION_MATRIX.md) and [Windows baseline evidence](../../evidence/2026-09-05-windows-baseline.json). Host & Play remains a later gameplay-integration gate; the Slice 0 Dedicated Server/two-client baseline passed.

1. Follow [Windows Development](../../runbooks/WINDOWS_DEVELOPMENT.md) and place the checkout at `ModSources/Convergence`.
2. Record exact installed versions and commit with the build-record template.
3. Run repository checks, YAML/catalog checks, and the dependency-free domain harness.
4. Run `dotnet build`, tModLoader Build + Reload, Single Player load, Dedicated Server load, and a two-client empty-Mod smoke.
5. If candidate versions fail, change only the compatibility/version decision in a dedicated commit; do not mix encounter edits.

Exit: a reproducible baseline exists. Candidate runtime versions may be marked confirmed only from this evidence.

## Slice 1 — Atomic feature rename, still inert

Use `git mv` and update the whole identifier family in one commit:

- directory `Content/Encounters/ThirdSeverance` → `Content/Encounters/FirstSeverance`;
- filenames, namespaces, types, tests, project links, registration, current comments;
- key `third_severance` → `first_severance`;
- feature failure-code prefix `third_severance.*` → `first_severance.*`;
- active docs and client target paths.

Do not mechanically rewrite historical ADRs, research observations, or changelog history. Do not create a compatibility alias because the feature is unpublished. Do not renumber packet enums. Keep availability rejection and the inert world adapter.

Exit: source and domain harness compile under the target name with no active world mutation.

## Slice 2 — Replace the obsolete immutable plan

Delete the multipart/Part Break/Effigy/Last Stand assumptions from the active plan and tests. Introduce typed feature state for:

```text
SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset
```

The plan owns bounded durations, maximum loop count, Overload threshold, roster-scaled Pylon count, Stack share count, and success/failure edges. Boss HP remains persistent; exposure has no required damage budget. Encode the provisional `2 / 2 / 3` Stack share requirement for `2 / 3 / 4` pull participants and the current eight-exposure cap. Cap exhaustion has one edge only: generic `Defeat` plus feature cause `LoopCapExceeded`. Add the append-only byte `FirstSeveranceTerminalCause` values/mapping from the spec; do not overload the generic `EncounterEndReason` or preserve the legacy `AdvanceHardEnrage`/Last Stand edge. The validator checks reachability, bounded cycles, terminal outcomes, valid timing, unique mechanic ownership, and complete generic/cause mappings.

Extend the generic End boundary with an immutable feature-neutral termination descriptor containing generic reason plus bounded feature schema/cause data. A runtime returns that descriptor for feature-owned endings. Add a constructor-validated, definition-owned data mapping for external `WorldUnload`, `InternalFailure`, and `ProtocolFailure` endings so the coordinator can produce the corresponding First Severance cause without re-entering mutable runtime logic. Route Reset, runtime-exception catch, and the future fatal-protocol entry point through the descriptor before terminal publication/cleanup; a missing or incompatible mapping rejects definition construction. Keep runtime-creation failure as a pre-acceptance activation failure with no feature tombstone.

Exit: dependency-free tests cover 2/3/4-player plans, the `2 / 2 / 3` Stack table, early Pylon success, Pylon failure/third Overload, full loop, exposure victory, deadline ordering, direct loop-cap `Defeat + LoopCapExceeded`, and every terminal mapping with no enrage phase. Activation is still denied.

## Slice 3 — Arena, Core, roster, and preparation

Implement the first real tModLoader adapters:

- Foundation Core ModItem/ModTile/ModTileEntity;
- server resolver from request anchor to exact Core TE and validated 320x140 prospective arena;
- Calamity progression gate through the compatibility boundary;
- bounded 2–4 player selection, stable Participant IDs, Ready, timeout, cancel;
- logical Barrier presentation/correction and outsider policy;
- typed activate/ready/cancel/snapshot handlers with nonce, distance, rate, side, and lifecycle validation.

All validation completes before world mutation. The Foundation Core does not own the encounter. During Preparing, a valid Foundation Core Tile/TE break cancels and cleans the session. During Active, normal player break/explosion/wiring/liquid attempts are rejected; an unexpected Tile/TE loss invalidates and aborts the exact Fight, while an explicit admin/debug abort uses the same cleanup path. Keep the actual combat transition disabled until actor ownership and feature replication exist.

Exit: repeat start/Ready/cancel/Foundation-Core-break/disconnect/unload cycles leave no stale state on Dedicated Server; protected Active break is rejected and injected unexpected Tile/TE loss aborts cleanly.

## Slice 4 — Boss, Pylons, measured hit pipeline, and replication foundation

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

## Slice 5 — Complete loop, Stack/Spread authority, and terminal reducer

Complete the active loop executor and phase-deadline ordering here. Add server-observed position sampling at declared resolve ticks, the fixed server-owned Stack damage pool, provisional `2 / 2 / 3` required share counts, frozen-roster round-robin target selection from the loop index, exactly one circular-forward target reissue with a new assignment revision/full 180-tick telegraph, bounded `TargetUnavailable` soft failure after no replacement or a second invalidation, pairwise Spread checks, one failure application per participant, CoreExposure/Reset, and client-only telegraphs. Downed/Eliminated/disconnected participants are excluded consistently from Stack occupants/divisors and current Spread requirements.

The First Severance runtime is the sole reducer of feature-observed subsystem candidates. For each tick it applies permitted actor hits, applies pre-mechanic lethal/interrupt/connection facts, samples the resulting Alive set for the due mechanic, applies mechanic-created lethal facts, processes one revive-start batch, and calls the Revive commit exactly once. It then uses the spec's actor/invariant/gameplay terminal order before any nonterminal phase transition. Add Stack assignment revision/reissue, Spread set, resolve tick/result, exposure, generic end reason, and feature terminal cause to full snapshot, terminal event/delta, and tombstone. Do not let the Revive boundary return a competing early coordinator transition. Coordinator-owned unload/exception/fatal-protocol endings use the Slice 2 immutable external mapping and preempt any uncommitted feature result.

For every feature-owned first-slice terminal, store the cause and return one direct End descriptor. Do not request `TransitionTo(Resolving)` on that tick; the current update contract cannot request transition and End together, and the coordinator already enters Cleanup/publishes its terminal snapshot from End. External forced endings enter through the coordinator bridge rather than an `EncounterRuntimeUpdate`. A future separately specified result/reward ceremony may use `Resolving` on an earlier tick.

Exit: two clients see the same complete repeated loop and recover it from a full snapshot; host/non-host assignments, 2/3/4 players, head-split arithmetic, one-reissue behavior, deadline-tick Downed/disconnect exclusion, latency, reordered packets, closing-tick Victory, safety-terminal priority, direct loop-cap `Defeat + LoopCapExceeded`, feature-cause round trips, and simulated recovery terminal candidates match on authority and every client.

## Slice 6 — Death-hook instrumentation, then revive adapter

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

## Slice 7 — First-playable acceptance

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

## Packet and command rules

For each request, parse fixed/bounded data fully, then validate sender, side, protocol, Encounter Sequence, Fight ID, revision/nonce, current roster binding/epoch, lifecycle, range/item when relevant, and rate limit before mutation. Unknown, stale, duplicate, and malformed input is rejected with bounded logs.

Requests carry intent only:

- activation carries a candidate anchor, never a validated Core;
- Ready changes only the sender's state;
- revive carries a target stable Participant ID and nonce, never duration/success/life;
- cancel carries the exact current lease nonce;
- snapshot request carries no authority state.

## Cleanup inventory

Register as soon as created:

- Boss and Pylon NPC handles;
- mechanic/damage Projectile handles;
- active assignments and temporary feature state;
- Barrier and Core busy projection;
- participant control/targeting projections;
- revive service/channels/reservations;
- feature snapshot/outbox subscription.

Cleanup verifies the expected Fight ID, stops spawning, publishes the terminal state, removes owned actors, restores players safely, resets the Foundation Core if its Tile/TE still exists, clears feature state, and releases the session. Calling the same exact-Fight cleanup twice succeeds as an empty no-op; a stale-Fight cleanup never clears the current fight.

## Definition of Done for the first Raid slice

- 2–4 players can activate, Ready, enter, clear, fail, cancel, disconnect, and cleanly reload on the pinned Dedicated Server environment.
- The exact accepted loop is the only live combat route.
- Boss damage is impossible outside Core exposure and victory is authority-owned.
- Pylon, Stack, Spread, Downed, Revive, Overload, and tokens behave identically for host and non-host.
- No custom feature packet can directly declare participants, DPS, position-check results, revive completion, life, or victory; inherited Terraria movement/combat trust is documented rather than presented as modified-client anti-cheat.
- Every terminal/error/unload route leaves no actor, reservation, projection, Core busy flag, or active session.
- Repository checks, catalog/YAML checks, domain harness, tModLoader build/load, and relevant multiplayer evidence are recorded.
- Names, timing, damage, and art still labeled provisional are not misreported as final.
