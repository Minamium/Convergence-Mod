---
doc_id: decision.development-solo-admission
document_type: adr
status: accepted
owners:
  - engineering
  - networking
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.development_solo_admission
aliases:
  - ADR-0020
related_code:
  - Directory.Build.props
  - Content/Encounters/FirstSeverance/Development/FirstSeveranceDevelopmentPolicy.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceCoreResolver.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceEndingTimeline.cs
related_docs:
  - development.single-operator-testing
  - encounter.first-severance.spec
  - encounter.first-severance.visual
  - policy.release-process
  - project.status
---

# ADR-0020: Build-gated solo debug admission and disposable terminal HUD

**Partial supersession,2026-09-14:** [ADR-0025](0025-public-solo-admission.md) replaces the build/release gate with ordinary1–4-player admission. The historical opt-out instructions below are not current commands. Terminal presentation and normal all-Down cleanup remain applicable.

The user requests a single ordinary Host & Play client for quick development checks, without a companion NPC, invulnerability or a redesigned solo Boss. This supersedes the earlier blanket two-real-player minimum for this development build, not the separate console-only protection permission in [ADR-0015](0015-console-only-single-pull-assist.md). A balanced public solo/NPC mode remains undecided. The user also requests success and failure cinematics that temporarily replace HUD, extending the disposable terminal presentation in [ADR-0018](0018-roster-health-and-core-salvos.md).

## Authority and release boundary

`ConvergenceDevelopmentSolo` is an MSBuild property, default `true` during this development period. It defines `CONVERGENCE_DEVELOPMENT_SOLO`; the feature-local policy exposes its compiled value. Build with `-p:ConvergenceDevelopmentSolo=false` before any public release and verify solo rejection. There is no ModConfig UI, chat/packet toggle, saved field, name allowlist or grant to a client. The release checklist owns this mandatory opt-out.

The server/SP definition and Core resolver use this same policy to admit one rather than two eligible players. Pure arena/roster constructors require explicit opt-in for solo; their default still rejects it. Empty/oversized rosters, stale bindings, invalid Cores, occupied airspace and all other activation gates remain rejected. Preparation stays manual Ready. All participants are real frozen bindings; no fake second participant is inserted into the world or recovery system.

Runtime/projection capacity becomes one to four, independently of admission. One member uses the existing two-player HP/Pylon workload and unchanged phase score/attack geometry. Actual roster membership continues to govern targeting, Stack denominator and Spread pairs. The generic instant resource-free recovery settings can represent one participant; the legacy initial token-based factory remains two to four. No feature switch is added to `Common`. One Down therefore takes the existing AllParticipantsDowned route on the ordinary committed authority tick, followed by existing Defeat death and exact-Fight cleanup.

## Replication and terminal presentation

Protocol **v14** retains v13's field layout and stable packet IDs. The version changes because preparation/combat count bounds now accept one; all peers must update together. NPC extra-AI count bounds also accept one and resolve the two-player HP table without inventing a second roster member. New `solo_debug` and `solo_start_enabled` fields annotate the existing CombatStarted diagnostic. One-member preparation shows a clear SOLO DEBUG notice.

Only a preceding connected local participant's accepted Victory/Defeat starts the result cinematic. Victory uses 210 local ticks, Defeat 180, including when the local player has already died. It retains only cached visual pose/coordinates after gameplay has ended. A frame-local interface layer suppresses subsequent ordinary HUD layers; no persistent `hideUI`, inventory, input, death, world-time or zoom flag changes. Camera focus eases back; shake-off/Reduced Effects remain honored. Local expiry restores HUD even without another packet. New Fight, world/mod unload, stale lease without an accepted success/failure, cancellation and outsider status cannot retain or initiate the result overlay. Existing audio cues and art are reused; no assets or dependencies are added.

## Evidence boundary

Solo is a fast movement/VFX/attack iteration path, not evidence of two-screen synchronization, revival interaction, multiplayer balance, latency or 2/3/4-person fairness. Ordinary user-operated GUI plus friends remains the default; preparing extra windows or a server is not implied. Focused automated and user-owned runtime state belongs to [Status](../STATUS.md), behavior to the [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md), and operation to the [runbook](../runbooks/SINGLE_OPERATOR_TESTING.md).
