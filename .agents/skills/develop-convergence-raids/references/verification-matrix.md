# Verification Matrix

Record exact version, commit, enabled Mods, player count, network conditions, and sanitized evidence for every run.

## Always

- `python3 tools/docs_catalog.py --check`
- `python3 tools/repository_checks.py`
- `python3 tools/validate_yaml.py`
- audit-only review: `python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py --audit-only .`
- C# namespace/nullable/authority review
- Markdown links, JSON/XML/YAML, generated catalog, packaging, and attribution checks

## C# changes

- pure Raid domains: `dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj --configuration Release`
- `dotnet build ConvergenceMod.csproj` under pinned tModLoader targets
- tModLoader Build + Reload
- Single Player smoke
- Dedicated Server headless load

Use the exact `ModSources/Convergence` setup and evidence format in `docs/runbooks/WINDOWS_DEVELOPMENT.md` and `docs/evidence/README.md`. Personal absolute log paths may live in ignored local notes but never in a committed record.

## Multiplayer gameplay changes

- Host & Play and Dedicated Server
- 2, 3, and 4 frozen pull participants
- host/non-host as Pylon contributor, mechanic target, Downed target, and reviver
- simultaneous Downed, revive interruption/token exhaustion/all-Downed wipe
- disconnect/rejoin, epoch/slot reuse, join in progress, old packet, and phase-boundary death
- 100/200/300 ms RTT plus loss/reordering where practical
- cleanup after cancel, Core destruction, missing Boss/Pylon, Victory/Defeat, unload, and internal failure

## Arena changes

- World edge/safety margin and invalid Core identity
- protected tiles, liquid, chest, wiring, conflicting event policies
- 2/3/4 Pylon layout space
- dash, hook, mount, knockback, recall, pylon, bed, external teleport
- participant escape and outsider entry with current connection epoch
- no unintended permanent World mutation after failure/unload

## First Severance loop changes

- Pylon early success/deadline/third Overload
- Stack/Spread target invalidation and Downed exclusion
- each Spread participant fails at most once per resolve
- clean/penalized exposure and persistent HP
- life-zero wins over exposure closure on the same authority tick
- loop cap, terminal snapshot, and exact-Fight actor cleanup

Never convert a missing test environment into a pass. Record it as `not_run` or `blocked`, and keep activation/release gates closed when correctness depends on it.
