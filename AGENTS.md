# Repository Working Agreement

This file applies to the entire repository.

## Before changing code

1. Read `docs/VERSION_MATRIX.md`, `docs/ARCHITECTURE.md`, and `docs/NETWORK_ARCHITECTURE.md`.
2. State the implementation plan, affected files, synchronization policy, and unresolved decisions.
3. Keep work inside the requested milestone; do not pre-build later boss phases or production assets.

## Non-negotiable boundaries

- Gameplay state and outcomes are server/Single Player authoritative.
- Clients send bounded requests and consume read-only snapshots/events.
- `Common` never depends on `Content` or presentation-only `Client` code.
- Encounter-specific behavior enters through definition-scoped policies and runtime factories; do not add feature switches to the global policy catalog, coordinator, or packet router.
- Calamity access stays in `Common/Compatibility/Calamity`.
- Every transient world resource has one owning runtime and an idempotent cleanup path.
- Active encounters are ephemeral and at most one may exist per World in the initial architecture.
- Dedicated Server paths must not initialize graphics or audio.

## Verification

- Always run `python3 tools/repository_checks.py` and `python3 tools/validate_yaml.py` (install `tools/requirements-ci.txt`).
- For C# changes, run both `dotnet build ConvergenceMod.csproj` and tModLoader Build + Reload in the pinned `ModSources/Convergence` environment.
- Multiplayer changes require relevant Single Player, Host & Play, Dedicated Server, 2/3/4-player, latency, disconnect, and cleanup evidence.
- If the pinned runtime is unavailable, report the missing verification explicitly; never claim a successful compile.

## Assets and external material

- Do not vendor Calamity binaries, source mirrors, extracted assets, or third-party recordings.
- Do not commit concept/raw asset directories or generated build output.
- Add an exact `Assets/ATTRIBUTION.md` record for every distributable image, audio, music, or font asset.
- No release or external contribution acceptance occurs until source and asset licenses are selected.

## Change discipline

- Prefer small, concern-focused commits.
- Update docs or an ADR with changes to authority, protocol, persistence, dependencies, module direction, or rights policy.
- Preserve terminal snapshots, stable packet IDs, machine-readable failure codes, and cleanup invariants.
