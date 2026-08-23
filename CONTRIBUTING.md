# Contributing

The project is in its architecture and compatibility phase. Design feedback, reproducible bug reports, and test evidence are welcome. Unsolicited code, music, voice, sprites, or other assets are not accepted yet because the project license and contributor agreement have not been finalized.

## Before opening a change

1. Read [Architecture](docs/ARCHITECTURE.md) and [Development setup](docs/DEVELOPMENT.md).
2. Open an issue for changes to protocol, save data, dependencies, public APIs, progression, or asset licensing.
3. Keep one concern per pull request.
4. Do not include Calamity binaries, extracted assets, decompiled code, third-party recordings, or unlicensed samples.

## Required evidence

- Run `python3 tools/repository_checks.py`.
- Run `python3 tools/validate_yaml.py` when YAML changes.
- Run `dotnet build ConvergenceMod.csproj` when C# changes.
- Build and reload with the pinned tModLoader/Calamity versions when C# changes.
- State whether the change was tested in Single Player, Host & Play, and Dedicated Server.
- Include player count, latency conditions, and relevant logs for multiplayer changes.
- Update an ADR when changing an accepted architectural decision.
- Update `Assets/ATTRIBUTION.md` for every distributable asset.

## Multiplayer review rule

Every gameplay pull request must identify:

- the authoritative owner of the new state;
- the client request, if any;
- snapshot/rejoin behavior;
- cleanup behavior;
- high-latency and disconnect behavior;
- which effects are client-only.

Gameplay results may not be decided from client-reported damage, positions, timers, or mechanic success.

## Commit style

Use short imperative Conventional Commit-style subjects where practical:

- `feat: add arena validation result model`
- `fix: reject stale ready requests`
- `docs: record rejoin identity decision`
- `refactor: isolate Calamity progression calls`
- `test: cover cleanup after core destruction`
