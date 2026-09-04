---
doc_id: development.windows
document_type: runbook
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-04
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

## Required tools and content

Install on the same Windows user profile:

- Steam Terraria matching the [Version Matrix](../VERSION_MATRIX.md);
- tModLoader stable matching the candidate pin;
- Calamity Mod `2.2.2` candidate plus its required official Music Mod;
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

Both command-line build and in-game Build + Reload are required because they exercise different packaging/load paths.

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

Candidate matrix entries become Confirmed only after:

- command-line build;
- Build + Reload;
- Single Player load;
- Dedicated Server load;
- two-client connection and initial packet smoke.

## Edit/verify loop

For each C# concern:

1. read [Status](../STATUS.md), the feature spec/plan, architecture/network docs, and relevant ADRs;
2. make one bounded change while activation stays fail-closed unless that slice owns activation;
3. update tests and documentation in the same concern;
4. run catalog, repository, YAML, domain, build, and relevant tModLoader checks;
5. inspect `git diff --check` and `git status --short`;
6. commit with a narrow message and attach the build-record summary to the task/PR.

Multiplayer/authority changes additionally require host/non-host, 2/3/4-player, latency, disconnect, and cleanup cases from [Test Plan](../TEST_PLAN.md).

## Troubleshooting boundaries

- Missing `..\tModLoader.targets`: checkout is not directly under the correct `ModSources` directory.
- Calamity reference failure: verify enabled/installed exact dependency and Music dependency; do not vendor `.tmod` files.
- Calamity `2.2.2` unavailable from the official Workshop: do not obtain an unofficial binary. Record the actually available official version and create a compatibility branch; keep the version-matrix candidate unchanged until the full gate passes.
- SDK selection failure: inspect `dotnet --info` and `global.json`; do not edit the target framework to bypass tModLoader.
- Build works but reload fails: treat the in-game error as a real blocker; do not mark the version confirmed.
- Client works but server fails: inspect dedicated-only graphics/audio/static initialization and side guards.
- Lethal hook ambiguity: stop the live revive adapter and perform the instrumentation spike described in the feature plan.

Record deviations in the handoff/evidence documents rather than leaving machine-specific knowledge only in chat.

## Upstream references

- [tModLoader repository and developer entry points](https://github.com/tModLoader/tModLoader)
- [ExampleMod at the candidate source commit](https://github.com/tModLoader/tModLoader/tree/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod)
- [Server configuration at the candidate source commit](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/release_extras/serverconfig.txt)
- [Starting a modded server](https://github.com/tModLoader/tModLoader/wiki/Starting-a-modded-server)
