# ADR-0005: Server-authoritative Raid Downed and Revive

- Status: Accepted for domain foundation; gameplay adapter gated
- Date: 2026-08-23
- Decider: Minamium

## Context

Terraria normally resolves zero life through player death and later respawn. The first Convergence Raid instead needs a fixed 2–4-player roster, an incapacitated Downed state, ally-channelled revival, shared revive tokens, disconnect/rejoin handling, and a deterministic Raid failure when no standing participant remains.

This transition crosses several unreliable boundaries: reusable Terraria player slots, client-originated input, lethal-damage hook ordering, same-tick deaths, packet delay, cleanup, and other Mods' revival effects. In particular, tModLoader invokes every registered `ModPlayer.PreKill` hook and combines their Boolean results; returning `false` does not prevent Calamity's `PreKill` logic from also running. Calamity 2.2.2 contains several personal revival effects with state-changing side effects. The evidence and required runtime spike are recorded in [Multiplayer Raid Prior Art](../research/MULTIPLAYER_RAID_PRIOR_ART.md).

## Decision

`Common/Raids/Revive` owns a Terraria-independent, server/Single Player-authoritative state machine. It is composed by the owning Raid feature only after a validated roster is frozen.

The domain owns:

- `Alive`, `Downed`, and `Eliminated` participant state;
- stable `ParticipantId` plus an authority-assigned player slot and monotonic connection epoch;
- Downed and disconnect deadlines in authority ticks;
- one active revive channel per reviver and one reservation per target;
- a channel nonce so delayed cancellation cannot affect a newer channel;
- shared-token reservation and consumption only on successful completion;
- recovery life ratio, invulnerability, and Weakness projections;
- terminal reasons for all-participant Downed, no available participant, and unrecoverable timeout;
- bounded snapshots/events and exact-`FightId` idempotent cleanup.

Initial tuning is two seconds to channel, thirty seconds before Downed expiry, thirty seconds of reconnect grace, and 1/2/3 shared tokens for a pull-time roster of 2/3/4. These values are balance inputs, not protocol constants.

The authority adapter must settle one Raid tick in a documented order and evaluate terminal failure only after that tick's eligible transitions are applied. Deadline equality has one rule: interruption, invalid target, and Downed expiry win over a revive completing on the same tick. Same-tick lethal transitions are batched before the single all-Downed evaluation.

The adapter also submits exactly one complete, non-empty, bounded revive-start batch for any authority tick that has start requests. Before mutation, the domain requires every entry to carry this service's `FightId` and the same authority tick, requires each stable reviver `ParticipantId` to occur at most once, and rejects a batch larger than the frozen participant roster. It then copies the batch into bounded storage and adjudicates it by authority tick, stable reviver `ParticipantId`, target, request nonce, binding, and `FightId`. Once at least one individual start is accepted, that tick's batch boundary is sealed and a second non-empty batch is rejected. A structurally valid batch whose individual starts are all rejected leaves both the batch boundary and authority clock unchanged, allowing a later valid lower-tick authority transition. Consequently, two valid players racing to reserve the same target produce the same winner regardless of packet arrival or collection order. No direct single-start entry point is exposed by the Third Severance boundary.

Clients may request a target and cancellation of an exact channel nonce. Transport resolves the sender from trusted `whoAmI`; the server supplies its current participant binding and revalidates range, movement, damage interruption, phase permission, token availability, and connection generation. Clients never report death, restored life, timer completion, token spending, or wipe success as authoritative facts.

Rejected commands never advance the authority clock. A cancellation with no matching active channel is an accepted idempotent no-op, but also does not advance that clock; a zero channel nonce is rejected before channel lookup. This prevents delayed or speculative cancellation traffic from making an earlier valid authority transition stale.

The actual tModLoader death/control/packet adapter remains disconnected and Third Severance activation remains denied until a pinned Single Player, Host & Play, and Dedicated Server instrumentation spike defines:

1. which process first observes each lethal-damage path;
2. ordering between Convergence and Calamity `PreKill` side effects;
3. normalization of life, immunity, mounts, grapples, controls, buffs, and death reason;
4. same-tick transition ordering and packet recovery;
5. cleanup behavior after cancel, wipe, Core loss, disconnect, and World unload.

## Consequences

Positive:

- multiplayer results have one owner and are testable without rendering or Terraria globals;
- stale Fight IDs, player-slot reuse, replayed requests, and old channel cancellation are rejected;
- a normal Boss can omit Raid revival without inheriting unused lifecycle state;
- the dangerous death-hook integration can be prototyped without making an incomplete Raid playable.

Costs and risks:

- lethal-damage interception and player control require narrow tModLoader adapters plus explicit Calamity coexistence tests;
- range, line-of-sight, movement, and damage interruption cannot be proven by the pure domain alone;
- snapshots/events and terminal ordering add protocol work before the first playable pull;
- reconnect identity still needs a server-side roster policy; a player name or client-supplied UUID is insufficient.

## Alternatives

- Store Downed only in `ModPlayer`: rejected because it fragments Raid authority and is vulnerable to slot reuse and partial cleanup.
- Let the client announce death or revive completion: rejected because latency and spoofing would decide gameplay.
- Use Terraria death/respawn and teleport the player back: rejected because it breaks shared tokens, all-Downed evaluation, and encounter continuity.
- Copy an accessory self-revive implementation from Calamity or Fargo's Souls: rejected because personal item effects do not provide cooperative Raid semantics and would violate the project's provenance boundary.
- Enable `PreKill` immediately and resolve conflicts later: rejected because multiple hooks can mutate the same lethal event even when death is cancelled.
