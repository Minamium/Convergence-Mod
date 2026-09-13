---
name: develop-convergence-raids
description: Implement or review Convergence Boss/Raid behavior, presentation, and playtest feedback. Use for mechanics, authority, replication, cleanup, or user playtest/log reviews; skip unrelated wording, build setup, and repository housekeeping.
---

# Develop Convergence Raids

## Select context for the change

- Locate the repository using `AGENTS.md` and `ConvergenceMod.csproj`. Follow the agreement already in context; do not reload it and every linked document.
- Follow the context selection in AGENTS.md. [Read by task](../../../docs/README.md#read-by-task) routes to the affected feature and specification; reuse sections already read.
- Read the implementation plan when choosing or changing work order, the version matrix when building/researching APIs, and historical records only to resolve a specific question. Current scope and recovery rules come from the active feature spec and its superseding ADRs, not a frozen MVP list in this skill.
- Existing source/version evidence can answer unchanged questions. Investigate only remaining uncertainty; use the source-research skill for API or prior-art questions that actually need research.

Choose the mode implied by the request; no separate confirmation is needed:

- **Implementation:** make the requested bounded change and its applicable checks.
- **Audit-only:** inspect without editing, formatting, compiling, or producing build/bytecode output. Use Git status/diff to distinguish existing changes; use a scoped file inventory if Git is unavailable. Run static checks only when they help answer the review question.

## Load references only when needed

| Change | Reference |
|---|---|
| Animation, VFX, texture integration or background scenes | [Presentation direction](references/presentation-direction.md) and the affected feature's visual rules; do not transfer one Boss's appearance to another |
| Choosing a module or changing dependency direction | [Architecture map](references/architecture-map.md); follow the relevant architecture section if the boundary is unclear |
| Gameplay state, requests, actors, recovery, or cleanup | Relevant sections of [Raid authority checklist](references/raid-authority-checklist.md) |
| Selecting checks or reporting completion | Applicable rows and commands in [Verification matrix](references/verification-matrix.md) |

Reuse references already read in this task until relevant inputs or scope change. A display/text edit does not require a full multiplayer review.

## Implement the affected responsibility

For gameplay changes, identify the authority owner, bounded request and replica (if any), stable identity, and exact-Fight cleanup path. For client-only changes, check the read-only input and Dedicated Server guard. Keep the explanation proportional to what changes.

For new mechanics, define assignment, telegraph/resolve ticks, authority result, failure policy, recovery interaction, and cleanup in the owning spec/domain. Add adapters, DTOs, and presentation only as the feature needs them. Register transient resources immediately; keep an incomplete path unavailable until its required adapters and evidence exist. Apply the current development scope without reopening unrelated production gates.

Update the document that owns each changed fact. Do not copy the same implementation report into the spec, plan, README, and skill. Review the final diff for affected invariants and finish under the [AGENTS completion contract](../../../AGENTS.md#verification), selecting checks from the verification matrix.

Report concrete defects and missing applicable evidence. Distinguish an implementation defect from a future release gate or a user-owned playtest that has not run.

## Keep playtest feedback lightweight

When the user supplies gameplay feedback, logs or a recording, append one compact dated/build-labelled entry to [playtest feedback](../../../docs/history/PLAYTEST_FEEDBACK.md): user observation → decision/change → result or remaining check. Separate reported experience from measured evidence; link existing evidence instead of duplicating logs, transcripts or tuning tables. Combine the same session's follow-ups and omit entries with no new finding.

The ledger is optional history, never a startup reading requirement. Search only the relevant feature/version when needed; current rules remain in their owning specs. Do not turn each remark into an ADR, research report or additional test gate.
