---
doc_id: project.decisions
document_type: governance
status: accepted
owners:
  - project
last_reviewed: 2026-09-07
source_of_truth_for:
  - project.open_decisions
aliases:
  - decisions
  - open questions
related_code: []
related_docs:
  - decisions.adr-index
  - project.status
---

# Decisions and Open Questions

The [ADR index](adr/README.md) owns structural decisions and their partial replacements. Accepted ADR bodies remain immutable. Current implementation/evidence is in [Status](STATUS.md), not here.

## Current constraints

- One Mod assembly with Common, feature-local Content and client presentation; one authority-coordinated encounter per World.
- First Severance's development path is enabled. Its production lethal-hook/rejoin adapters remain gated separately.
- Windows owns runtime verification; other platforms may edit/review without claiming game compatibility.
- Protocol and future save-schema versions are independent of the Mod version.
- The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) owns arena/admission/balance; the [recovery spec](encounters/first-severance/REVIVE_SPEC.md) owns recovery; [audio cues](AUDIO_CUE_SHEET.md) own the present phase mixes. Do not repeat values here.

## Unresolved production choices

- Final public names, source/asset licensing and contribution/AI-asset disclosure.
- Core recipe, final placement/progression restrictions and terrain tolerance.
- Journey/Mediumcore/Hardcore support, Calamity difficulty policy and lethal-hook coexistence.
- Server-side identity for robust rejoin; public solo/companion design (the development flag is not that design).
- Final balance based on multiplayer observations, rewards and possible future music packaging.

## Decision workflow

New ADRs are for authority, dependency direction, protocol compatibility, persistence, external dependencies and release/rights changes. Reversible tuning, wording and ordinary VFX adjustments update only their owning facts, without a new ADR, Skill or long research report. Record completion/evidence through Status links rather than copying the change narrative across documents.
