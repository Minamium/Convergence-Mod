---
doc_id: decision.single-pull-debug-assist
document_type: adr
status: accepted
owners:
  - engineering
  - networking
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.single_pull_debug_assist
aliases:
  - ADR-0015
related_code:
  - Content/Encounters/FirstSeverance/Development
  - Content/Encounters/FirstSeverance/Revive/FirstSeveranceRaidPlayer.cs
related_docs:
  - development.single-operator-testing
  - encounter.first-severance.spec
  - project.network-architecture
---

# ADR-0015: Console-only, exact-Fight auxiliary protection

The user explicitly requests two real clients for one-person testing, with an invulnerable auxiliary that cannot be enabled through ordinary gameplay. This is not a solo roster rule or an NPC participant. Normal GUI/friend testing remains the default unless solo assistance is explicitly requested.

## Decision

Only a Dedicated Server launched with `-convergence-dev-assist` registers a Console-only command. It queues mutations on the game thread, validates a currently alive slot **and connection epoch**, and arms one next preparation while Idle. A bounded ten-minute pending permit is consumed once even if it cannot match the next frozen roster. It binds an immutable roster member and exact Fight; names are diagnostic labels, never authority. Preparing owns idempotent lease cleanup and extends only this assisted Ready window to ten minutes.

The combat runtime suppresses Raid HP damage/Down only after ordinary eligibility and geometry checks, recording bounded would-hit diagnostics. Roster membership, Stack denominator, Spread and targeting remain unchanged. Explicit debug Down remains possible. The lease is revoked on cancellation, terminal/Defeat before ordinary death, participant-loss termination, World/Mod unload, or console `off`; never inherited by another Fight or a reused slot.

Protocol v8 appends one strict Boolean byte to each combat participant projection. This is server-to-client observation, not a permission request. No packet IDs or client request shapes change. Connected Alive participants alone may carry `true`. Owner-side RaidPlayer uses exact-Fight scope plus a five-second renewable TTL for ordinary damage/DoT/death protection; disconnected, Downed, expired or cleaned projections fail closed. Preparation/ambient staging outside combat is not protected. Other Mods' death-cancelling hooks are not bypassed, and this cooperative-client mechanism is not anti-cheat.

## Consequences and evidence

Both clients/server must load protocol v8 together. No public toggle, persisted character flag, name allowlist, auto-rescue, attack replay, NPC or global coordinator feature switch is added. A protected auxiliary changes survival/difficulty, so assisted runs cannot establish normal balance or all-downed behavior. Console rearming is required for each pull. Usage, pinned tML API evidence and limits are in the [single-operator runbook](../runbooks/SINGLE_OPERATOR_TESTING.md); actual automated/runtime results belong only in [Status](../STATUS.md).
