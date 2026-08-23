# Verification Matrix

Record exact version, commit, enabled Mods, player count, network conditions, and log locations for every run.

## Always

- `python3 tools/repository_checks.py`
- `python3 tools/validate_yaml.py`
- audit-only review: `python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py --audit-only .`
- C# namespace and nullable review
- Markdown link, JSON, XML, and attribution validation through repository checks

## C# changes

- pure Raid-domain transitions: `dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj`
- `dotnet build ConvergenceMod.csproj` where pinned tModLoader targets exist
- tModLoader Build + Reload
- Single Player smoke
- Dedicated Server headless load

Use the exact `ModSources/Convergence` setup and evidence fields in `docs/DEVELOPMENT.md`. Record the absolute client/server log paths used for the run; do not infer a platform-specific user-data path.

## Multiplayer gameplay changes

- Host & Play and Dedicated Server
- 2, 3, and 4 participants
- host and non-host as mechanic target
- host and non-host Downed
- simultaneous Downed, revive interrupted, token exhaustion, and all-Downed wipe
- disconnect/rejoin, slot reuse, join in progress, old packet, and phase-boundary death
- 100/200/300 ms RTT plus loss/reordering where practical
- cleanup after cancel, Core destruction, Boss despawn, World unload, and internal failure

## Arena changes

- World edges and invalid Core identity
- too-small space, protected tiles, liquid, chest, wiring, and conflicting event policies
- dash, hook, mount, knockback, recall, pylon, bed, and external teleport
- participant escape and outsider entry
- no permanent world mutation after failure or unload

Never convert a missing test environment into a pass. Record it as unverified and keep activation or release gates closed when correctness depends on it.
