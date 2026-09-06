# Contributing

The project is in its architecture and compatibility phase. Design feedback, reproducible bug reports, and test evidence are welcome. Unsolicited code, music, voice, sprites, or other assets are not accepted yet because the project license and contributor agreement have not been finalized.

## Before opening a change

1. Follow [Read by task](docs/README.md#read-by-task); read architecture/setup details only when the change needs them.
2. Establish scope for protocol, save data, dependencies, public APIs, progression, or asset licensing before implementation. An existing issue or explicit user request can establish it; routine local work does not need a duplicate issue.
3. Keep one concern per pull request.
4. Do not include Calamity binaries, extracted assets, decompiled code, third-party recordings, or unlicensed samples.

## Required evidence

- Use the shared [Verification Matrix](.agents/skills/develop-convergence-raids/references/verification-matrix.md) and its single command entry point. Run the applicable checks once for the final inputs.
- Report actual results and relevant `not_run` checks with the remaining action. Include environment/player-count details for runtime checks, not for prose-only work.
- Update an ADR when changing an accepted architectural decision.
- Update `Assets/ATTRIBUTION.md` for every distributable asset.

## Multiplayer review rule

For changes to gameplay state, requests, or lifecycle, describe only the affected responsibilities:

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
