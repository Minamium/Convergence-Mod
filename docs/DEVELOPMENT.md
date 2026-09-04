# Development Setup

## Pinned environment

Use [VERSION_MATRIX.md](VERSION_MATRIX.md) as the single compatibility source. The current candidate environment is:

- Terraria 1.4.4.9;
- tModLoader stable `v2026.06.3.6`;
- Calamity Mod `2.2.2` plus its official Music dependency;
- .NET 8 / C# 12.

The repository requests .NET SDK `8.0.424` through `global.json` and permits newer patches in the same feature band. tModLoader still owns the target framework and language baseline.

Do not silently upgrade one dependency. Compatibility changes use a dedicated branch/PR and rerun the multiplayer matrix.

## Checkout location

tModLoader creates `tModLoader.targets` in its `ModSources` directory. Clone or create a worktree with the exact internal Mod directory name:

```bash
cd "<tModLoader user data>/ModSources"
git clone https://github.com/Minamium/tmod.git Convergence
cd Convergence
```

The GitHub repository name may remain `tmod`; the local source directory must be `Convergence`. The assembly and root namespace use the same internal identity because tModLoader verifies the namespace against the internal Mod name.

## Repository checks

Run before every push:

```bash
python3 tools/repository_checks.py
python3 -m pip install --requirement tools/requirements-ci.txt
python3 tools/validate_yaml.py
```

This checks required policy files, UTF-8/LF text, broken relative Markdown links, generated binaries and sensitive filenames, case-colliding paths, required `.tmod` packaging masks, coarse dependency direction, file size, and attribution record completeness.

GitHub Actions runs the same dependency-free core check, pinned PyYAML syntax/minimal-schema validation, and the Terraria-independent domain harness with the SDK pinned by `global.json`. It intentionally does not claim to compile or load the Mod.

## Build and reload

With the repository under the pinned tModLoader `ModSources` directory, run the project build:

```bash
dotnet build ConvergenceMod.csproj
```

Then run the interactive tModLoader path:

1. Start the pinned tModLoader.
2. Open `Workshop -> Develop Mods`.
3. Run `Build + Reload` for Convergence.
4. Confirm the detected tModLoader and Calamity versions in the log.

Both checks are required. `dotnet build` uses `ConvergenceMod.csproj` and `Directory.Build.props` for nullable and analyzer policy. tModLoader's in-game Build + Reload performs its own Mod source compilation and packaging and does not use those project properties as a substitute. A success in either path alone is incomplete evidence.

A plain clone outside `ModSources` does not contain `../tModLoader.targets` and is not a valid build environment.

## Minimum smoke test

For every C# change:

1. Build + Reload without warnings introduced by the change.
2. Enter a Single Player World and exit cleanly.
3. Start a Dedicated Server with the Mod, Calamity, and Calamity Music enabled.
4. Join with two clients using identical Mod versions.
5. Confirm load/unload/reload leaves no active Encounter state.
6. Save the version evidence described in [TEST_PLAN.md](TEST_PLAN.md).

Milestone-specific changes must also run their relevant 2/3/4-player cases.

## Build records

Local reproducibility evidence belongs in `build-record.local.json`, which is ignored because it may contain machine-specific paths. Record:

- Git commit;
- OS and architecture;
- Terraria/tModLoader/Calamity versions;
- Calamity `.tmod` SHA-256 without copying the binary;
- build result;
- client and Dedicated Server load result.

Release evidence will use a sanitized, committed record once the first build succeeds.

## Current limitation

The repository bootstrap environment did not have the .NET SDK, tModLoader installation, Calamity binary, or Terraria runtime available. Repository checks pass locally, but the candidate version matrix remains unconfirmed until the smoke test above is executed in a real ModSources environment.
