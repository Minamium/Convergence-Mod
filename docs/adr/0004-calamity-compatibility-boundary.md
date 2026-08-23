# ADR-0004: Isolated Calamity Compatibility Boundary

- Status: Accepted
- Date: 2026-08-23
- Decider: Minamium

## Context

Calamity is a required dependency whose public API, public source mirror, internals, and license have different stability and reuse implications. Direct references throughout content would make every update a cross-cutting change.

## Decision

All Calamity access lives under `Common/Compatibility/Calamity`. Prefer documented Mod Calls and type-check every result. Use direct public types only inside the adapter when necessary. Do not use reflection, IL patches, publicizers, copied implementation, or vendored Calamity assets/binaries.

`build.txt` declares a minimum of 2.2.2. Runtime compatibility additionally requires `2.2.2 <= version < 2.3.0`; outside the range, Encounter activation is disabled.

## Consequences

Positive:

- dependency updates have a small review surface;
- content can be tested against stable gateway contracts;
- licensing/provenance boundaries remain visible.

Costs and risks:

- adapter methods need defensive result validation;
- some desirable integrations may wait for a public supported API;
- the exact Workshop binary still requires local smoke testing.

## Alternatives

- Direct Calamity references in each feature: rejected as cross-cutting ABI coupling.
- Copying Calamity code: rejected for maintenance and license reasons.
- Reflection/IL patching internals: rejected for initial releases due to fragility.

