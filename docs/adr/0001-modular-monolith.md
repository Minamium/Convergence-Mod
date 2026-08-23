# ADR-0001: Modular Monolith with Feature Modules

- Status: Accepted
- Date: 2026-08-23
- Decider: Minamium

## Context

The project starts with one multiplayer Raid but aims to grow toward many bosses, items, world features, and presentation systems. Multiple assemblies and generalized frameworks would slow the first vertical slice, while a flat `NPCs/Projectiles/Items` layout would couple unrelated future content.

## Decision

Use one tModLoader assembly with explicit layers and feature-first content directories. Shared foundation and runtime live in `Common`; each Encounter owns a vertical module under `Content/Encounters`; client presentation is isolated under `Client`.

Enforce `Common -> Content/Client` and `Content -> Client` import bans with repository checks. Extract shared mechanics only after a second concrete consumer exists.

## Consequences

Positive:

- standard tModLoader build/reload remains simple;
- an Encounter can be navigated and removed as a coherent feature;
- stable boundaries exist for later assembly extraction;
- the first Raid does not become the root of all future content.

Costs and risks:

- source boundaries are not runtime isolation;
- reviews must detect indirect coupling beyond import checks;
- feature modules may temporarily duplicate code before a safe shared abstraction emerges.

## Alternatives

- Multiple assemblies now: rejected as premature build and API overhead.
- Type-first global folders: rejected because feature ownership disappears at scale.
- A fully generic encounter DSL: rejected until real mechanics establish requirements.

