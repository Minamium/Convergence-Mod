# Coding Standards

## Correctness before density

- Prefer small explicit state transitions over clever inheritance.
- Make invalid protocol values and lifecycle transitions rejectable.
- Keep gameplay state server-owned and presentation state disposable.
- Use stable value objects for Fight and participant identity.
- Do not store localized strings in authoritative state or wire messages.

## Dependency rules

- `Common` cannot import `Content` or `Client`.
- `Content` cannot import `Client`.
- Only the Calamity compatibility layer accesses Calamity APIs.
- Individual actors do not call the packet transport directly.
- World mutation is performed through an authoritative runtime service, not from UI or VFX.

## tModLoader boundaries

- Every hook is reviewed for the sides on which tModLoader calls it.
- `RightClick` sends a request; it does not start an Encounter locally.
- `ModTileEntity` anchors content but does not own the whole Encounter.
- Static registries and event handlers are cleared in `Unload`.
- Dedicated Server code must not initialize graphics or audio assets.

## Network code

- Assign explicit numeric packet IDs and never reuse retired values.
- Validate protocol, direction, length, enums, counts, coordinates, sender, distance, lifecycle, and rate limit.
- Use `whoAmI` as the sender; do not trust a player index in payload.
- Synchronize transitions and results, not decorative particles or every timer tick.
- Unknown/stale/malformed packets are logged at a bounded rate and ignored safely.

## Naming

- Public API is avoided before its compatibility contract is deliberate.
- Boolean names begin with `Is`, `Has`, `Can`, or `Should`.
- Stable keys use lowercase ASCII with underscores, such as `third_severance`.
- Failure codes are namespaced, such as `arena.protected_tile`.
- `Manager`, `Helper`, `Utils`, and `Data` require a more specific responsibility name.

## Comments and documentation

Comments explain authority, invariants, lifecycle, unusual API constraints, or why an apparently simpler solution is unsafe. They do not narrate obvious syntax. Accepted structural decisions live in ADRs.

