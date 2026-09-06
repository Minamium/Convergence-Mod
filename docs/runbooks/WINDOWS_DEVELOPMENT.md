---
doc_id: development.windows
document_type: runbook
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-06
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

## Clone in the correct source directory

tModLoader expects the repository root under its user-data `ModSources` directory and imports `..\tModLoader.targets`. Use tModLoader's Develop Mods UI to locate/open the source folder if Windows Documents is redirected by OneDrive.

Authenticate once. The recommended path is:

```powershell
gh auth login
gh auth status
```

From PowerShell in `ModSources`, use one of the following clone paths:

```powershell
gh repo clone Minamium/tmod Convergence
Set-Location Convergence
git switch main
git pull --ff-only origin main
```

If GitHub CLI is not installed, HTTPS also works:

```powershell
git clone https://github.com/Minamium/tmod.git Convergence
Set-Location Convergence
```

Use `git@github.com:Minamium/tmod.git` only after an SSH key has been registered and `ssh -T git@github.com` succeeds. Never copy a token into a tracked script or configuration file.

The GitHub repository may be named `tmod`, but the local directory must be `Convergence` so the internal Mod name, assembly, and root namespace agree. Do not nest another repository directory beneath it.

For an existing checkout, first run `git status --short`. Preserve or commit local work before `git pull --ff-only`; never reset it away as a setup shortcut.

## Verify the toolchain

```powershell
git --version
gh auth status
py -3 --version
rg --version
dotnet --info
dotnet --list-sdks
git rev-parse HEAD
git status --short
```

`gh auth status` may be skipped when using HTTPS or an already configured SSH remote. If `py` is unavailable, verify `python --version` instead. Confirm that SDK `8.0.424` or an allowed patch in the same feature band is selected. Confirm `..\tModLoader.targets` exists from the repository root. Never copy tModLoader or Calamity binaries into this repository.

## Repository and domain checks

```powershell
python -m pip install --requirement tools/requirements-ci.txt
python tools/docs_catalog.py --check
python tools/repository_checks.py
python tools/validate_yaml.py
dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj --configuration Release
```

If `python` is unavailable but `py` is installed, use `py -3` consistently. These checks do not replace a real Mod build.

## Real Mod build and reload

```powershell
dotnet build ConvergenceMod.csproj
```

Then:

1. Start the pinned tModLoader stable build.
2. Enable Calamity and its official Music dependency.
3. Open `Workshop -> Develop Mods`.
4. Run `Build + Reload` for Convergence.
5. Enter and exit a Single Player world.
6. Record exact runtime versions and the first relevant error/warning if a gate fails.

Both command-line build and in-game Build + Reload are required for this baseline because they exercise different packaging/load paths. Routine development records applicable user-owned GUI checks as pending until performed, as described in the Verification Matrix.

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

Copy [`../evidence/build-record.example.json`](../evidence/build-record.example.json) to the ignored root file `build-record.local.json`, fill it from actual logs, and keep it local until sanitized evidence is intentionally reviewed for commit.

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
