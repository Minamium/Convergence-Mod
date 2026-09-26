# Repository Working Agreement

This file applies to the entire repository.

## Start with the smallest useful context

1. Read this agreement once per task. For code/behavior work, read only `docs/STATUS.md`'s Current build, Verification state, and Next change sections, then inspect the affected code.
2. Use the task table in `docs/README.md` to select additional sections. No blanket full-document, architecture, ADR, handoff, or research reading is required. Text-only corrections need only the affected passage and its conventions.
3. Reuse context already read in this task; reread only when relevant files change, scope expands, or a concrete uncertainty remains. Search headings/symbols with `rg` before opening long files.
4. Briefly state the change and applicable verification. Identify authority, replication, and cleanup owners when those responsibilities change; omit unrelated checklist fields.
5. Follow the current user-requested scope and active specification. Historical slices and backlog do not add work or reinstate superseded gates.

Player-facing names and behavior come from the active encounter specification selected through `docs/README.md`. Preserve stable internal code, packet, asset, and document IDs unless an explicit migration is in scope; a display-name or GitHub repository rename does not rename them.

## Repository Skills

- Use `.agents/skills/develop-convergence-raids` for Boss/Raid behavior or any Convergence content presentation implementation/review, including NPCs, weapons and projectiles. Documentation wording and repository housekeeping do not need the gameplay workflow.
- Use `.agents/skills/research-tmodloader-sources` when API behavior or another public Mod implementation must be investigated. Record exact versions, source paths, licenses, observations, and independent design decisions.
- Keep always-on rules here and task-specific repeatable procedures in Skills. Repository Skills are excluded from `.tmod` packaging.

For visual work, apply the shared [Luminance presentation policy](docs/ART_DIRECTION.md#luminance-presentation-policy) and the affected feature's spec. This applies to Ghost Samurai and all other content; the policy owns the quality/completion criteria, while the Skill routes implementation details only when needed.

## Non-negotiable boundaries

- Gameplay state and outcomes are server/Single Player authoritative. Native receiving-player damage boundaries are explicitly scoped in [ADR-0023](docs/adr/0023-ghost-samurai-native-wave-damage.md) (Ghost Samurai wave), [ADR-0024](docs/adr/0024-native-raid-hurt-and-downed.md) (Doll damage calculation, not geometry or outcomes), [ADR-0026](docs/adr/0026-crimson-score-and-native-projectiles.md) (Crimson Foundry native hostile projectiles), and [ADR-0028](docs/adr/0028-azure-cathedral-native-actors.md) (Azure Cathedral native actors/projectiles). Do not generalize them to other features.
- Clients send bounded requests and consume read-only snapshots/events.
- `Common` never depends on `Content` or presentation-only `Client` code.
- Encounter-specific behavior enters through definition-scoped policies and runtime factories; do not add feature switches to global policy, coordinator, or packet-router code.
- Calamity access stays in `Common/Compatibility/Calamity`.
- Every transient world resource has one exact-Fight owning runtime and an idempotent cleanup path.
- Doll is multiplayer-recommended, not multiplayer-required. Preserve [ordinary solo admission](docs/encounters/first-severance/ENCOUNTER_SPEC.md#admission-and-arena) in GUI and public builds; never compile it out as a release precaution.
- Active encounters are ephemeral and at most one may exist per World initially.
- Dedicated Server paths must not initialize graphics or audio.
- Explicit packet IDs are never renumbered; parse bounded DTOs completely before authority validation/mutation.

## Documentation contract

- `docs/STATUS.md` is the canonical implementation-state record. Other documents may include a short context summary only when they link back to `docs/STATUS.md`; conflicting or detailed status belongs there.
- Feature spec owns player-visible behavior; feature plan owns work order; backlog cannot expand current scope.
- Accepted ADRs own structural decisions and are superseded, not silently rewritten.
- Indexed docs use the front-matter model in `docs/DOCUMENTATION_SYSTEM.md`.
- Update only the document owning the changed fact; add links rather than repeating status, tuning, or procedures elsewhere. Small tuning/text changes do not require a new ADR or research report.
- Regenerate the catalog once after the indexed-doc edits are settled; include it with the change. Do not regenerate after every individual edit.
- Never commit personal absolute paths, credentials, raw logs, worlds/players, `.tmod` binaries, or local semantic-search databases.

## Verification

- The command entry point and change-based requirements live in [.agents/skills/develop-convergence-raids/references/verification-matrix.md](.agents/skills/develop-convergence-raids/references/verification-matrix.md). Run its static wrapper once at completion, adding domain/build/runtime checks for the affected behavior.
- For implementation requests, finish the requested behavior and applicable automated checks. Investigate failures, fix those caused by the change, and rerun affected checks within the authorized local scope. An unresolved failure or unavailable prerequisite is a concrete blocker, not completion.
- Reuse passing checks for unchanged inputs/environment. Full release and compatibility matrices belong to their declared gates, not every edit.
- Record relevant runtime checks as passed, failed, or `not_run` with the remaining action. A build is not a playtest; missing evidence cannot satisfy an activation, compatibility, or release gate that depends on it.

## Assets and external material

- Do not vendor Calamity binaries, source mirrors, or extracted assets. Third-party recordings may be vendored only when the repository owner explicitly approves the exact work for this project, the source terms permit game use of the committed form, and `Assets/ATTRIBUTION.md` records the creator, source, terms, and exact modifications. Never treat game-use permission as permission for standalone redistribution; recheck the governing source terms before public release.
- Do not commit concept/raw asset directories or generated build output.
- Add an exact `Assets/ATTRIBUTION.md` record for every distributable image, audio, music, or font asset.
- Follow [CONTRIBUTING.md](CONTRIBUTING.md#contribution-scope-and-licensing) for agreed collaboration and unsolicited submissions. Public distribution requires the terms and gates in `docs/RELEASE_PROCESS.md`; repository access does not grant reuse rights.

## Change discipline

- Prefer small, concern-focused commits.
- Update docs or an ADR with changes to authority, protocol, persistence, dependencies, module direction, or rights policy.
- Preserve terminal snapshots, stable packet IDs, machine-readable failure codes, and cleanup invariants.

## Shared development

- Follow [Contributing](CONTRIBUTING.md#shared-development) for branches, integration and build destinations, including [cleanup after a merged PR](CONTRIBUTING.md#finish-merged-work). Preserve other contributors' changes and the current version/protocol when integrating.
- Keep task-specific branch/commit checkpoints in the relevant handoff, not in always-on rules. The completed Ghost Samurai integration is recorded in [Windows handoff](docs/handoff/WINDOWS.md#completed-ghost-samurai-integration).
