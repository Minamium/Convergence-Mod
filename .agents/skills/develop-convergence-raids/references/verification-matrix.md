# Verification Matrix

This is the shared policy for selecting routine checks; [AGENTS.md](../../../../AGENTS.md#verification) owns the implementation completion contract. [Test Plan](../../../../docs/TEST_PLAN.md) owns detailed cases; [Release Process](../../../../docs/RELEASE_PROCESS.md) owns release gates. Read only the applicable rows/sections.

## Select checks by changed behavior

Run the static wrapper once after a completed edit batch. Add every applicable row below; choose by affected behavior and callers, not just file extension. Reuse passing results while their code, dependencies, and environment are unchanged. Repeat only checks affected by subsequent edits, failures, or unresolved concerns.

| Change | Additional verification |
|---|---|
| Wording, docs, Skills, repository configuration | Static checks; validate edited Skill structure when affected. No domain run, Mod build or game session for text-only edits |
| README/documentation artwork | Static checks, rendered layout/image inspection, correct relative links and export provenance. No Mod build for assets excluded under docs |
| Python verification tooling | Exercise the changed command/failure branches plus static checks |
| Display/VFX C# or assets | Build changed C#; apply [presentation completion](../../../../docs/ART_DIRECTION.md#presentation-completion) to the changed scene, including observed motion/material frames and affected loading/accessibility/client-only guards. Domain tests only if shared geometry/rules change |
| Shader source or material bindings | Compile changed HLSL and verify exports through the [shader build procedure](../../../../docs/runbooks/WINDOWS_DEVELOPMENT.md#diagnose-or-build); inspect the actual compiled material and relevant draw-state/resource lifecycle. An offline GPU preview does not establish in-game quality or FPS |
| Tuning values | Build changed C#; focused domain boundaries only if rule/geometry/scaling input changed. Hand off the changed tuning for user playtesting |
| Combat/recovery rules or linked domain sources/tests | `--with-domain`; matching package build when production code changes, plus relevant task-owner Host & Play smoke |
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
| `--with-codec` | Run bounded compiled packet checks using PowerShell 7+. Reuse the selected domain/Mod build; if neither was selected, build only the linked-source harness |
| `--with-dotnet` | Optional MSBuild route with pinned targets and configured dependency assembly references; current installed Calamity packaging uses the native route below |
| `--audit-only` | Read-only static verification; cannot combine with catalog writing, domain/codec execution or builds |

For example, a domain change with indexed-doc updates uses `--write-catalog --with-domain` in one wrapper invocation, plus the applicable package build below. No bytecode compilation pass is needed after successfully executing the Python checkers. Review `git diff --check` and the final diff once before completion.

For current source-identified packages, use `python tools/dev.py build --native` with the [Windows runbook](../../../../docs/runbooks/WINDOWS_DEVELOPMENT.md#diagnose-or-build) and the task's selected output/profile. Use this instead of `--with-dotnet`, not in addition to it. `python tools/dev.py doctor` checks only the local environment. Reload the resulting package; do not recompile unchanged source just to enter Host & Play.

For packet changes, combine `--with-domain --with-codec` for the game-independent suite. To inspect an already-built Mod, pass that exact build's assembly to `pwsh -NoProfile -File tools/check-codec.ps1 -AssemblyPath <built-assembly>`. Native packaging does not refresh `bin/Debug`; an older DLL is not evidence for the current package. [Test Plan](../../../../docs/TEST_PLAN.md#repository-runnable-checks-and-ci) owns filtered-case discovery, tooling tests and the separate CI jobs.

## Runtime evidence and development completion

Use [Windows Development](../../../../docs/runbooks/WINDOWS_DEVELOPMENT.md) for actual setup/build/load procedures, only when those checks apply. A pure domain run is not a Mod build; a packaged Mod is not proof of rendering or multiplayer behavior.

The task owner may own GUI reload/playtesting under the agreed development workflow. Finish the applicable automated checks and report that handoff as `not_run` with the concrete smoke to perform. Do not launch extra sessions or expand to a release matrix merely to fill a generic checklist. Missing evidence still blocks any activation, compatibility, or release claim that depends on it.

For runtime evidence, record the exact build/commit (including dirty state), versions, relevant enabled Mods, topology/player count, and result. Link an unchanged environment record rather than copying it into every document. Report only applicable passed, failed, and `not_run` checks; a prose-only edit does not need a full build-record JSON.

## Find detailed cases when needed

- Recovery: [current Revive Spec](../../../../docs/encounters/first-severance/REVIVE_SPEC.md), then the relevant Test Plan sections. Legacy channel/token configurations have regression tests but do not dictate the active feature.
- Arena: validation, movement/teleport, outsiders, and cleanup sections in the Test Plan.
- Loop: phase outcomes, tick/terminal collisions, and exact-Fight cleanup sections in the Test Plan.
- Network/lifecycle: packet robustness, disconnect/rejoin, slot reuse, and cleanup fault-injection sections in the Test Plan.
