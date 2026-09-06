# Verification Matrix

This is the shared policy for selecting routine checks. [Test Plan](../../../../docs/TEST_PLAN.md) owns detailed cases; [Release Process](../../../../docs/RELEASE_PROCESS.md) owns release gates. Read only the applicable rows/sections.

## Select checks by changed behavior

Run the static wrapper once after a completed edit batch. Add every applicable row below; choose by affected behavior and callers, not just file extension. Reuse passing results while their code, dependencies, and environment are unchanged. Repeat only checks affected by subsequent edits, failures, or unresolved concerns.

| Change | Additional verification |
|---|---|
| Wording, docs, Skills, repository configuration | Static checks; validate edited Skill structure when affected. No domain run, Mod build or game session for text-only edits |
| Python verification tooling | Exercise the changed command/failure branches plus static checks |
| Display/VFX C# or assets | Build changed C#; check affected loading/readability/accessibility and client-only guards. Domain tests only if shared geometry/rules change |
| Tuning values | Build changed C#; focused domain boundaries only if rule/geometry/scaling input changed. Hand off the changed tuning for user playtesting |
| Combat/recovery rules or linked domain sources/tests | `--with-domain --with-dotnet`; focused contracts and relevant user-owned Host & Play smoke |
| Communication, authority, identity, lifecycle or saving | Bounded codec/round-trip and affected stale/duplicate/cleanup contracts; matching build. Add latency/rejoin/slot-reuse or save migration cases only when affected |
| Local source/build environment | Resolve source/targets/dependencies and verify the changed setup/build path. Do not require an unrelated full gameplay matrix |
| Runtime/dependency upgrades or public release | Separate complete compatibility/release gates for the declared versions/platforms/player counts |

A 3/4-player case is required when roster scaling, assignment, bounds, or the changed contract depends on that count. Latency/fault cases follow affected timing, replication, or recovery behavior; do not rerun the whole matrix for unrelated decoration or wording.

## One command entry point

From the repository root, with the dependency from `tools/requirements-ci.txt` installed once per Python environment:

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py .
```

The wrapper runs catalog, repository-policy, and YAML checks. It does not require a second run of those individual commands. Use the available Python 3 executable (`python3`, `python`, `py -3`, or its resolved path); the wrapper reuses that interpreter.

Add flags to the same invocation as needed:

| Flag | Effect |
|---|---|
| `--write-catalog` | Regenerate indexed-doc metadata once, then check it; use after the document edit batch is settled |
| `--with-domain` | Run the Terraria-independent domain harness in Release configuration |
| `--with-dotnet` | Run `dotnet build ConvergenceMod.csproj` against pinned tModLoader targets |
| `--audit-only` | Read-only static verification; cannot combine with catalog writing or builds/domain execution |

For example, a domain change with indexed-doc updates uses `--write-catalog --with-domain --with-dotnet` in one invocation. No bytecode compilation pass is needed after successfully executing the Python checkers. Review `git diff --check` and the final diff once before completion.

For source-identified packages, use `python tools/dev.py build` instead of `--with-dotnet`, not in addition to it. `python tools/dev.py doctor` checks only the local environment. Reload the resulting package; do not recompile unchanged source just to enter Host & Play.

## Runtime evidence and development completion

Use [Windows Development](../../../../docs/runbooks/WINDOWS_DEVELOPMENT.md) for actual setup/build/load procedures, only when those checks apply. A pure domain run is not a Mod build; a packaged Mod is not proof of rendering or multiplayer behavior.

In an authorized development experiment, the user may own GUI reload/playtesting. Finish the applicable automated checks and report that handoff as `not_run` with the concrete smoke to perform. Do not launch extra sessions or expand to a release matrix merely to fill a generic checklist. Missing evidence still blocks any activation, compatibility, or release claim that depends on it.

For runtime evidence, record the exact build/commit (including dirty state), versions, relevant enabled Mods, topology/player count, and result. Link an unchanged environment record rather than copying it into every document. Report only applicable passed, failed, and `not_run` checks; a prose-only edit does not need a full build-record JSON.

## Find detailed cases when needed

- Recovery: [current Revive Spec](../../../../docs/encounters/first-severance/REVIVE_SPEC.md), then the relevant Test Plan sections. Legacy channel/token configurations have regression tests but do not dictate the active feature.
- Arena: validation, movement/teleport, outsiders, and cleanup sections in the Test Plan.
- Loop: phase outcomes, tick/terminal collisions, and exact-Fight cleanup sections in the Test Plan.
- Network/lifecycle: packet robustness, disconnect/rejoin, slot reuse, and cleanup fault-injection sections in the Test Plan.
