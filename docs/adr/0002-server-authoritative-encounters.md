# ADR-0002: Server-Authoritative Encounter State

- Status: Accepted
- Date: 2026-08-23
- Decider: Minamium

## Context

The primary experience is a 2-4 player Raid with assignments, DPS checks, Downed/Revive, random targets, and failure recovery. Client-owned decisions would create desync, host-specific behavior, and easy packet abuse.

## Decision

Single Player or the server exclusively mutates gameplay state. Clients send bounded requests and maintain a read-only replica for UI/VFX/audio. The server validates sender, World state, distance, lifecycle, assignment, counts, and rate limits before accepting a request.

World-monotonic Encounter Sequence, Fight ID, protocol version, revision, and typed fixed/bounded schemas identify network state. A terminal tombstone prevents delayed packets from reviving an older fight. Presentation may interpolate, but gameplay uses server ticks and results.

## Consequences

Positive:

- Host & Play and Dedicated Server share one result model;
- stale or malicious requests can be rejected;
- reconnect and snapshot recovery have a clear source of truth;
- optional VFX/audio never changes difficulty.

Costs and risks:

- client prediction and correction require high-latency testing;
- every mechanic must define a snapshot/rejoin representation;
- server validation adds deliberate implementation work.

## Alternatives

- Host/client cooperative authority: rejected due to inconsistent truth.
- NPC `ai[]` as full state: rejected due to capacity, ownership, and lifecycle coupling.
- Client-reported mechanic success: rejected as untrusted input.
