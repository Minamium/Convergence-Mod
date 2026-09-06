# Repository Working Agreement

This file applies to the entire repository.

## Start with the smallest useful context

1. Read this agreement once per task. For code/behavior work, read only `docs/STATUS.md`'s Current build, Verification state, and Next change sections, then inspect the affected code.
2. Use the task table in `docs/README.md` to select additional sections. No blanket full-document, architecture, ADR, handoff, or research reading is required. Text-only corrections need only the affected passage and its conventions.
3. Reuse context already read in this task; reread only when relevant files change, scope expands, or a concrete uncertainty remains. Search headings/symbols with `rg` before opening long files.
4. Briefly state the change and applicable verification. Identify authority, replication, and cleanup owners when those responsibilities change; omit unrelated checklist fields.
5. Follow the current user-requested scope and active specification. Historical slices and backlog do not add work or reinstate superseded gates.

Current feature naming is `FirstSeverance` / `first_severance`. The isolated source/key/failure-prefix rename is complete; `ThirdSeverance` forms may remain only in deliberately historical records or completed-rename instructions, and no compatibility alias is required for the unpublished identifier.

## Repository Skills

- Use `.agents/skills/develop-convergence-raids` for Boss/Raid code or behavior implementation/review. Documentation wording and repository housekeeping do not need the gameplay workflow.
- Use `.agents/skills/research-tmodloader-sources` when API behavior or another public Mod implementation must be investigated. Record exact versions, source paths, licenses, observations, and independent design decisions.
- Keep always-on rules here and task-specific repeatable procedures in Skills. Repository Skills are excluded from `.tmod` packaging.

## Non-negotiable boundaries

- Gameplay state and outcomes are server/Single Player authoritative.
- Clients send bounded requests and consume read-only snapshots/events.
- `Common` never depends on `Content` or presentation-only `Client` code.
- Encounter-specific behavior enters through definition-scoped policies and runtime factories; do not add feature switches to global policy, coordinator, or packet-router code.
- Calamity access stays in `Common/Compatibility/Calamity`.
- Every transient world resource has one exact-Fight owning runtime and an idempotent cleanup path.
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
- Do not repeat passing checks for unchanged inputs/environment without a failure or unresolved concern. Full release and compatibility matrices belong to their declared gates, not every edit.
- Record relevant runtime checks as passed, failed, or `not_run` with the remaining action. A build is not a playtest; missing evidence cannot satisfy an activation, compatibility, or release gate that depends on it.

## Assets and external material

- Do not vendor Calamity binaries, source mirrors, extracted assets, or third-party recordings.
- Do not commit concept/raw asset directories or generated build output.
- Add an exact `Assets/ATTRIBUTION.md` record for every distributable image, audio, music, or font asset.
- No release or external contribution acceptance occurs until source and asset licenses are selected.

## Change discipline

- Prefer small, concern-focused commits.
- Update docs or an ADR with changes to authority, protocol, persistence, dependencies, module direction, or rights policy.
- Preserve terminal snapshots, stable packet IDs, machine-readable failure codes, and cleanup invariants.
