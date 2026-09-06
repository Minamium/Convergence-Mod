---
doc_id: development.windows
document_type: runbook
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-07
source_of_truth_for:
  - development.windows_setup
aliases:
  - Windows setup
  - ModSources setup
related_code:
  - ConvergenceMod.csproj
  - global.json
  - build.txt
related_docs:
  - handoff.windows
  - verification.evidence
  - project.status
---

# Windows Development Runbook

Windows is the primary implementation and multiplayer-verification workstation for the first Raid. macOS remains usable for documentation, Git, code review, platform-independent edits, and its own runtime smoke when fully installed, but macOS evidence never substitutes for the Windows/Dedicated Server gate.

This runbook contains setup and baseline procedures. For routine edits, use [Read by task](../README.md#read-by-task) and the [Verification Matrix](../../.agents/skills/develop-convergence-raids/references/verification-matrix.md) to select only the needed procedures; reading this page does not require rerunning the entire baseline.

## Required tools and content

Install on the same Windows user profile:

- Steam Terraria matching the [Version Matrix](../VERSION_MATRIX.md);
- tModLoader stable `v2026.07.3.0`;
- Calamity Mod `2.2.4` plus official Music Mod `2.1`;
- Git and a configured GitHub authentication method;
- Python 3.10 or newer for repository checks;
- ripgrep (`rg`) for the documented cross-repository search commands;
- .NET SDK `8.0.424` matching `global.json` (normally x64 on an x64 desktop);
- VS Code with C# Dev Kit, or JetBrains Rider;
- recommended: GitHub CLI (`gh`) for browser-based authentication and cloning;
- optional Aseprite only when sprite production begins.

Do not silently update tModLoader or Calamity mid-feature. If Steam updates a dependency, capture the new exact versions and validate them on a compatibility branch before changing the matrix.

## One canonical source and local setup

Choose one Git checkout named `Convergence` inside an authorized writable workspace. The usual `<user-data>/ModSources/Convergence` may be that checkout or an NTFS junction to it; both editing and tModLoader must resolve to the same files. Do not make another ConvergenceEdit copy for each change. Preserve/commit local work before pulling; use branches or purpose-named worktrees only for genuinely parallel work, with a final source directory named Convergence and an explicit build target.

Copy [tools/local.example.props](../../tools/local.example.props) to the ignored repository-root `Convergence.local.props`, and set:

- `TModLoaderPath`: installed pinned tModLoader directory containing `tMLMod.targets`.
- `TModLoaderSavePath`: existing user-data directory containing `Mods`; never silently create an empty alternate profile.

The project prefers explicit local/MSBuild paths, then `TML_PATH`, with the traditional parent `../tModLoader.targets` as fallback. It fails clearly if no targets exist. Do not commit local props, dependency binaries or personal paths, or broaden global permissions to make a checkout writable. A new machine clones GitHub once (`gh repo clone Minamium/tmod Convergence`); authenticate through Git/gh, never tracked credentials.

## Diagnose or build

```powershell
python tools/dev.py doctor
python tools/dev.py build
```

Use the available Python 3 interpreter. Doctor is read-only and identifies the resolved checkout, branch, dirty state, SDK and installed targets. Build uses that source and normal tML packaging, preserving the previous package in ignored `.local/builds/<timestamp>`. Its JSON record contains the exact commit, dirty status, SHA256 source-file manifest, resolved environment, command/log and output package hash; a source change during the build fails attribution. `--tml` and `--save` are explicit overrides; `--release-candidate` compiles out solo admission but does not approve a release.

`dotnet build ConvergenceMod.csproj` remains supported (also via the static wrapper's `--with-dotnet`), but use the recorded build when handing off a package. Do not run both on unchanged compiled inputs.

Installed `tMLMod.targets` was inspected during consolidation: it sets .NET 8/C#12 and calls the bundled server build command with ProjectDir, TargetPath and ExtraBuildModFlags. No copied third-party build targets or hard-coded Steam directory is committed.

## Routine checks and user handoff

Install `tools/requirements-ci.txt` once per Python environment. Select the affected static/domain/codec checks using the [Verification Matrix](../../.agents/skills/develop-convergence-raids/references/verification-matrix.md); generate the catalog once after the documentation batch.

A successful recorded build has already packaged the Mod in the selected profile. Stop/restart or Reload Mods to load it; **another Build + Reload compilation is not required for unchanged source**. The user owns the actual load and affected Host & Play observation unless GUI control is explicitly requested. Record those as not_run until observed; packaging is not a runtime success claim.

The dedicated baseline below applies to toolchain changes/full acceptance, not every edit. Preserve the user's current Mod pack and saves; use a disposable profile only when that test explicitly needs one.

## Dedicated Server baseline

Use the server launcher bundled with the pinned tModLoader installation. In Steam, open `tModLoader -> Manage -> Browse local files`, then record the actual launcher name and pinned runtime version in the build record. The stable distribution normally supplies `start-tModLoaderServer.bat` and `serverconfig.txt`; use the names present in that exact installation rather than copying scripts from the internet.

Use tModLoader's UI to locate the actual user-data root for this Windows profile; do not assume an unredirected Documents path. Keep the disposable test configuration outside the repository, for example under `<test-root>\ConvergenceServer`. In the tModLoader UI using that same user-data root:

1. enable only Calamity, its official Music dependency, and Convergence;
2. save that set as a named Mod Pack such as `ConvergenceBaseline`;
3. verify the pack's generated `enabled.json` contains the expected internal Mod names;
4. create a disposable world for the pinned Terraria/tModLoader version.

Copy the bundled `serverconfig.txt` into the local test directory and set at least the exact disposable world path, `autocreate`, world name, port, and player limit. Do not store a real public-server password in the file. Launch the bundled server under the same Windows profile so it reads the Mod/Mod Pack content just created by the UI:

```powershell
Set-Location "<tmodloader-install>"
.\start-tModLoaderServer.bat -config "<test-root>\ConvergenceServer\serverconfig.txt" -modpack "ConvergenceBaseline\Mods\enabled.json"
```

For the pinned tModLoader source, a UI-saved modern Mod Pack is a directory whose loadable list is `Mods\ModPacks\<pack>\Mods\enabled.json`; a short `-modpack ConvergenceBaseline` instead resolves the legacy flat file `Mods\ModPacks\ConvergenceBaseline.json`. Keep the nested relative path quoted as above, or pass the verified absolute `enabled.json` path for the same user-data root. If an isolated `-tmlsavedirectory` is desired, first launch both the client/UI and server against that same directory and verify its `Mods`, `ModPacks`, and `Worlds` content; never create an empty alternate root for only one side and assume it can see the default profile. This is runtime evidence, not a reason to commit generated Mod Pack files.

Minimum baseline:

1. Start Dedicated Server headlessly with all required Mods enabled.
2. Start client A from the same pinned tModLoader install and join through `localhost`.
3. Start a second tModLoader client instance only by a supported Steam/tModLoader launch method for that pinned version, or join from a second Windows account/machine; label which topology was used.
4. Join client B and confirm both clients, server, and dependencies report matching versions.
5. Enter/exit the world, disconnect each client once, and restart the server.
6. Confirm no stale encounter state or unload exception appears.
7. Preserve only sanitized evidence—versions, commit, checksums, topology, result, and short error excerpts.

Do not commit Steam credentials, IP addresses, player identities, full personal paths, worlds, player files, `.tmod` binaries, or raw logs.

## Build record

The recorded build writes ignored .local/builds records automatically. Use [the evidence template](../evidence/build-record.example.json) only for additional runtime/baseline observations; sanitize before committing. Link unchanged baseline evidence instead of repeating it.

Future matrix entries become Confirmed only after:

- command-line build;
- Build + Reload;
- Single Player load;
- Dedicated Server load;
- two-client connection and initial packet smoke.

## Edit/verify loop

For each bounded change:

1. select only the context needed using [Read by task](../README.md#read-by-task); reuse context already read;
2. implement the current scope and update only affected tests/document owners;
3. run the wrapper once with the applicable flags and perform the runtime checks selected by the Verification Matrix;
4. inspect the final diff and report results plus any concrete remaining user playtest; use a narrow commit when committing.

Use [Test Plan](../TEST_PLAN.md) for the affected multiplayer/authority cases. Repeat passing checks only if their inputs/environment change or a failure/uncertainty warrants it. Full compatibility and release gates retain their complete evidence requirements.

## Troubleshooting boundaries

- Missing `..\tModLoader.targets`: checkout is not directly under the correct `ModSources` directory.
- Calamity reference failure: verify enabled/installed exact dependency and Music dependency; do not vendor `.tmod` files.
- Pinned Calamity version unavailable from the official Workshop: do not obtain an unofficial binary. Record the actually available official version and create a compatibility branch; keep the confirmed version matrix unchanged until the full gate passes.
- SDK selection failure: inspect `dotnet --info` and `global.json`; do not edit the target framework to bypass tModLoader.
- Build works but reload fails: treat the in-game error as a real blocker; do not mark the version confirmed.
- Client works but server fails: inspect dedicated-only graphics/audio/static initialization and side guards.
- Lethal hook ambiguity: stop the live revive adapter and perform the instrumentation spike described in the feature plan.

Record deviations in the handoff/evidence documents rather than leaving machine-specific knowledge only in chat.

## Upstream references

- [tModLoader repository and developer entry points](https://github.com/tModLoader/tModLoader)
- [ExampleMod at the confirmed source commit](https://github.com/tModLoader/tModLoader/tree/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod)
- [Server configuration at the confirmed source commit](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/release_extras/serverconfig.txt)
- [Starting a modded server](https://github.com/tModLoader/tModLoader/wiki/Starting-a-modded-server)
