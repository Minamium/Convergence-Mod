---
doc_id: decision.instant-revival-recipient-lockout
document_type: adr
status: accepted
owners:
  - engineering
  - gameplay
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.instant_revival
aliases:
  - ADR-0011
related_code:
  - Common/Raids/Revive
  - Content/Encounters/FirstSeverance/Revive
related_docs:
  - encounter.first-severance.revive
  - project.network-architecture
  - project.status
---

# ADR-0011: Reusable instant revival with recipient-only lockout

- Decision: explicit user requests on 2026-09-06.
- Supersedes: the channel/shared-token recovery policy in [ADR-0005](0005-server-authoritative-downed-revive.md) for active First Severance, and the matching experimental choices in [ADR-0009](0009-development-combat-experiment.md). Identity, tick ordering, authority, ordinary-lethal-hook gates and cleanup remain intact.
- Extends: [ADR-0010](0010-giant-boss-observation-lances.md); its ray structure stays unchanged, but protocol version 4 carries a per-recipient recovery deadline.

## Decision

The kit is a non-consumable tool with no shared-token cost, recipe or self-revival. An Alive participant may use it within eight tiles of a connected Downed ally while moving, airborne, grappling or mounted if Terraria allows ordinary item use. The authority picks the nearest eligible ally. Completion occurs on the same authority tick as the accepted request, after same-tick damage and invalidation. There is no held-use, movement or damage-interruption interval.

The target recovers 35% HP with three seconds of immunity, then cannot receive another revival until exactly 3,600 authority ticks after success. The old ten-second damage weakness is removed from this feature. The lockout does not stop that player rescuing other allies. It survives another Down and binding/epoch changes within the service, and is cleared with the exact Fight. Thirty-second Down expiry and all-Downed failure remain unchanged.

The pure reusable domain keeps its tested legacy channel/token configuration, while First Severance explicitly selects zero-duration, no-token, 60-second-lockout settings. A zero-duration reservation is adjudicated and completed inside one tick; it cannot become a gameplay channel. Stable ordered batches choose one winner when two rescuers target the same ally. The adapter revalidates held item, binding, Alive/Downed state, range, deadline and lockout before that batch. No client declares a success, timer, position or health outcome.

The existing nonce-only request is preserved. The local player sends ordinary equipment/control synchronization before it; this is inherited cooperative Terraria state, not custom authoritative input. Queued requests receive their validation reply after tick revalidation, not a premature success that would hide a later rejection with the same nonce.

Protocol v4 appends an unsigned 64-bit lockout deadline to each of at most four combat participant records. Numeric packet IDs and client request layouts stay unchanged. The old token counter remains a reserved zero field, not a resource. A ModBuff icon, local HUD and body countdown project the deadline; deleting a client buff cannot clear server policy. Older peers must reload the matching Mod.

## Additional explicit development requests

The same user-authorized build adds a roughly 176-pixel-canvas Core monument over the original 2x2 tile/TE and clickable base, preserving existing placements without terrain migration. Boss/Pylon HP, defenses, damage and combat cadence are intentionally over-tuned in the encounter spec; this is not a balanced-clear claim. More forceful client effects do not change hit geometry or server time and retain reduced-effects/shake-disable settings.

## Verification boundary

Focused domain checks cover same-tick completion, exact lockout expiry, repeat rescue with no resource, recipient/rescuer separation, racing requests and timeout/cleanup. A matching tModLoader package and codec check are required. The user owns GUI reload and the two-player playtest; no expanded release matrix or ordinary death-hook implementation is implied.
