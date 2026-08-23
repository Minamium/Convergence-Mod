# ADR-0003: In-World Logical Arena

- Status: Accepted
- Date: 2026-08-23
- Decider: Minamium

## Context

Subworlds and generated tile walls complicate multiplayer synchronization, rejoin, return paths, Mod interoperability, and cleanup. The Raid still needs a fixed field and clear boundaries.

## Decision

Run the Raid in the normal World. A Core anchors a tile-space rectangle. The Barrier is logical: client rendering and predictive movement feedback plus authoritative server correction. Do not generate hundreds of temporary wall tiles.

The initial release permits one coordinator-managed Boss or Raid per World. This does not take ownership of vanilla or third-party encounters; activation validation rejects conflicts. Active session state does not persist across World load. Long-running World Events use a separate lifecycle/coordinator when introduced.

## Consequences

Positive:

- Dedicated Server and rejoin behavior stay within standard World semantics;
- cleanup does not restore large terrain edits;
- presentation can change without altering collision tiles.

Costs and risks:

- third-party teleport and direct World mutation require defensive correction and testing;
- nearby non-participants and spectators need an explicit policy;
- validation scans may cause a server hitch and require measurement.

## Alternatives

- Subworld/Dimension: rejected for the initial architecture.
- Generated tile walls: rejected for cleanup and compatibility risk.
- Instant death outside bounds: rejected as unreadable and latency-sensitive.
