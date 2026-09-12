# Contributing

Convergence has a playable development Raid and independent Boss work. [Status](docs/STATUS.md) owns current implementation and verification results. Start with the [task map](docs/README.md#read-by-task) and the affected feature; the full documentation set is not a prerequisite.

## Shared development

Use a short-lived branch or an explicit worktree from the latest integrated `origin/main`. Preserve local changes before updating. Continue new work from integrated main after a feature is merged; old feature branches are historical starting points, not the next development baseline.

- Keep one concern per PR and name the affected feature. An existing issue or maintainer request can establish scope; routine agreed work does not need a second issue or repeated approval.
- Keep each feature's implementation, presentation, tests and spec together. Share changes to Common transport, lifecycle and cleanup through integrated main so another feature's fixes are retained.
- Resolve overlapping edits using the current code and owning spec. Preserve the higher integrated Mod version and required protocol changes; keep other features' Status entries when updating that document.
- Identify the actual checkout and output/profile for every package build. Feature branches use an explicitly separate output/profile. Install the shared playtest `Convergence.tmod` from integrated main; do not repoint another developer's ModSources junction or replace their shared package.
- Keep personal paths, local props, logs and saves outside tracked configuration. A contributor's build record describes their environment, not every workstation.

Use normal pushes and the repository's review/merge rules. Do not discard another person's work, force-switch their worktree, hard reset, force push, or restore an old source copy over current files. Current review ownership is recorded in [CODEOWNERS](.github/CODEOWNERS); contributing code does not automatically change maintainer permissions.

## A useful task or PR

State the desired outcome, affected feature, constraints that matter, and how the result can be observed. For example: a defeated or cancelled Ghost Samurai can be summoned again, with no projectiles left from the previous fight. Select the corresponding checks instead of attaching the entire multiplayer matrix.

Use the [PR template](.github/PULL_REQUEST_TEMPLATE.md). The [AGENTS completion contract](AGENTS.md#verification) applies to implementation work; the [Verification Matrix](.agents/skills/develop-convergence-raids/references/verification-matrix.md) owns commands and check selection. Report checks actually run and their results. Name the owner and concrete action for relevant manual checks that remain `not_run`.

For changes to gameplay state, requests, replication or cleanup, explain only the affected authority owner, bounded client request, snapshot/rejoin behavior and cleanup path. Gameplay results remain authoritative. UI/VFX-only work needs its read-only input, loading and client-only guards rather than unrelated lifecycle fields.

Update only the owner of each changed fact. Structural decisions use the relevant ADR process; every distributable asset needs an exact [Attribution](Assets/ATTRIBUTION.md) record. Small wording/tuning fixes do not require a new ADR, research report, or updates to every document.

## Repository name and existing clones

The GitHub repository is `Minamium/Convergence-Mod`. The local source directory, ModSources entry, assembly and namespace remain `Convergence`.

For an existing clone, update the URL without recloning or moving files:

```sh
git remote set-url origin https://github.com/Minamium/Convergence-Mod.git
git remote -v
git fetch origin
```

SSH users can use `git@github.com:Minamium/Convergence-Mod.git`. Worktrees belonging to the same clone share its remote configuration. Independent clones and forks should update the remote that points to this upstream, which may be named `upstream` instead of `origin`.

GitHub redirects Git operations from the old `Minamium/tmod` address. Update bookmarks and integrations to the new URL, and do not reuse the old repository name while collaborators depend on its redirect. See [GitHub's rename documentation](https://docs.github.com/en/repositories/creating-and-managing-repositories/renaming-a-repository).

## Contribution scope and licensing

Maintainer-requested or agreed collaboration proceeds within its accepted task scope. General unsolicited code, music, voice, sprites and other assets are not accepted while the project license and contributor terms remain unresolved. Design feedback, reproducible bug reports and test evidence are welcome.

Repository access and agreement to work on a task do not grant a public source or asset license. Public distribution follows [Release Process](docs/RELEASE_PROCESS.md); this workflow does not select licensing terms or waive provenance requirements.

Do not submit Calamity binaries, extracted assets, decompiled source or unlicensed material. For approved third-party recordings, follow the exact source/permission conditions in [AGENTS.md](AGENTS.md#assets-and-external-material) and record the committed form in [Attribution](Assets/ATTRIBUTION.md).

## Commit style

Prefer small commits with short imperative subjects, using Conventional Commit prefixes where useful: `feat:`, `fix:`, `docs:`, `refactor:`, or `test:`. Describe the resulting behavior rather than the conversation that led to it.
