---
doc_id: decision.public-solo-admission
document_type: adr
status: accepted
owners:
  - gameplay
last_reviewed: 2026-09-14
source_of_truth_for:
  - architecture.doll_public_admission
aliases:
  - multiplayer recommended not required
related_code:
  - Content/Encounters/FirstSeverance/Development/FirstSeveranceDevelopmentPolicy.cs
  - tools/check-package-admission.ps1
related_docs:
  - encounter.first-severance.spec
  - policy.release-process
---

# ADR-0025: Solo admission is a public gameplay contract

The owner explicitly requires multiplayer-recommended, solo-allowed play. The0.3.1 GitHub artifact allowed solo; a GUI rebuild omitted the opt-in symbol and the different Workshop artifact required two players. This is a packaging regression, not an authorized product restriction.

**Supersedes ADR-0020's build/release admission gate only.** The active feature always admits1–4 real players, subject to the same server-owned binding/Core/field/Ready checks. No preprocessor symbol, MSBuild release flag or public configuration disables solo. The historical internal DevelopmentPolicy name remains; it no longer grants a cheat. Existing one-player difficulty, manual Ready and all-Down defeat are unchanged. No new packet format or synthetic player is needed.

Remove release tooling/CI instructions to disable solo. Inspect actual packaged admission after every scripted build and before Workshop Publish; source flags are insufficient evidence. GUI builds must also work without external symbols. Keep existing public artifacts immutable; correct releases use a new version.

The owner plans companion-minion party substitution. It remains separate future work: [Backlog](../encounters/first-severance/BACKLOG.md#companion-party-substitution--planned) records membership, mechanics/recovery and cleanup questions. Do not count existing minions as participants or claim companion support to conceal an admission defect.
