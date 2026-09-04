# Repository Working Agreement

This file applies to the entire repository.

## Before changing code

1. Read `docs/README.md`, `docs/STATUS.md`, `docs/VERSION_MATRIX.md`, `docs/ARCHITECTURE.md`, and `docs/NETWORK_ARCHITECTURE.md`.
2. For the first Raid, also read `docs/encounters/first-severance/README.md` and its implementation plan.
3. State the implementation slice, affected files, server/client ownership, cleanup path, verification, and unresolved decisions.
4. Keep work inside the requested slice; do not pull backlog mechanics or production assets into the MVP.

Current feature naming is `FirstSeverance` / `first_severance`. The isolated source/key/failure-prefix rename is complete; `ThirdSeverance` forms may remain only in deliberately historical records or completed-rename instructions, and no compatibility alias is required for the unpublished identifier.

## Repository Skills

- Use `.agents/skills/develop-convergence-raids` for Boss/Raid implementation or review. It contains authority, cleanup, module-routing, and multiplayer-verification checklists.
- Use `.agents/skills/research-tmodloader-sources` when API behavior or another public Mod implementation must be investigated. Record exact versions, source paths, licenses, observations, and independent design decisions.
- Keep always-on rules here and task-specific repeatable procedures in Skills. Repository Skills are excluded from `.tmod` packaging.

## Non-negotiable boundaries

- Gameplay state and outcomes are server/Single Player authoritative.
- Clients send bounded requests and consume read-only snapshots/events.
- `Common` never depends on `Content` or presentation-only `Client` code.
- Encounter-specific behavior enters through definition-scoped policies and runtime factories; do not add feature switches to global policy, coordinator, or packet-router code.
- Calamity access stays in `Common/Compatibility/Calamity`.
- Every transient world resource has one exact-Fight owning runtime and an idempotent cleanup path.
- Active encounters are ephemeral and at most one may exist per World initially.
- Dedicated Server paths must not initialize graphics or audio.
- Explicit packet IDs are never renumbered; parse bounded DTOs completely before authority validation/mutation.

## Documentation contract

- `docs/STATUS.md` is the canonical implementation-state record. Other documents may include a short context summary only when they link back to `docs/STATUS.md`; conflicting or detailed status belongs there.
- Feature spec owns player-visible behavior; feature plan owns work order; backlog cannot expand current scope.
- Accepted ADRs own structural decisions and are superseded, not silently rewritten.
- Indexed docs use the front-matter model in `docs/DOCUMENTATION_SYSTEM.md`.
- After indexed-doc changes, run `python3 tools/docs_catalog.py --write` and commit the generated catalog.
- Never commit personal absolute paths, credentials, raw logs, worlds/players, `.tmod` binaries, or local semantic-search databases.

## Verification

- Always run `python3 tools/docs_catalog.py --check`, `python3 tools/repository_checks.py`, and `python3 tools/validate_yaml.py` after documentation/repository changes.
- For Raid-domain changes, run `dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj`; this does not replace a tModLoader build.
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
