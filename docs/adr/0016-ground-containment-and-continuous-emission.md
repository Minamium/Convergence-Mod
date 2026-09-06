---
doc_id: decision.ground-containment-emission
document_type: adr
status: accepted
owners:
  - engineering
  - networking
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.ground_containment_emission
aliases:
  - ADR-0016
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceContainmentPlayer.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceEmissionVisuals.cs
related_docs:
  - encounter.first-severance.spec
  - research.wotg-raid-benchmark
---

# ADR-0016: Ground containment and continuous emission

## Decision

The user's development expansion supersedes ADR-0011's unchanged 2x2-only visual and ADR-0009's non-enforcing development boundary. New placements use a separate 12x4 FoundationPlinthTile identity; existing 2x2 Tile/TE saves retain their dimensions and interaction. Mining an old Core returns the item that places the new footprint. No automatic saved-tile rewrite, terrain deletion or generated wall grid occurs.

The authority resolves the actual TE footprint into a ground center and validates the 320x140 airspace before preparation and again immediately before combat. Solid interior terrain is fatal; a full-width manufactured floor is not required. The floor under the placed foundation is the rectangle's bottom. The physical boundary uses the full ArenaBounds, not the older two-tile-inset logical BarrierBounds.

The exact-Fight combat runtime retains the immutable validated layout and refreshes a participant-only containment/flight capability. Server corrections check the connection epoch and clamp the whole player body, with two-pixel inward hysteresis and a rate-limited vanilla teleport correction. The local owning client predicts the same constraint; remote replicas do not move other players. A renewable three-second local lease fails closed. Cleanup, disconnect, world entry and normal terminal paths revoke it. Downed/Eliminated participants remain confined but cannot use flight. Ordinary players/outsiders receive no flight or confinement; general outsider admission remains outside this development expansion. This cooperative-client movement adapter is not anti-cheat.

Protocol v10 changes CoreX/CoreY semantics to the resolved ground center. Bounds are derived from shared constants, so no packet IDs or request shapes change; all peers must update together. The Boss aperture is positioned at the field center, with decorative columns rising from the base. Pure client rendering owns all column/field/rig/beam art and creates no authoritative actors.

An emission instance persists through warning, gathering, launch and bounded cooling instead of switching separate visual objects at FireTick. Harmless tracking samples use local Hermite interpolation; the locked hold settles before firing. Live danger rails use exact authority geometry/ticks, not the texture alpha, cosmetic wavefront or interpolated target. Up to four retained casts cover overlapping tails; phase cancellation removes live cues and fades harmless residue. Graphics assets are requested only on client render paths and released through the content manager.

## Evidence and limits

The [source benchmark](../research/WOTG_RAID_BENCHMARK.md) distinguishes WotG/Calamity observations from independent implementation. No third-party code, shaders, sprites or recordings are included. Bounds/envelope regression checks, actual package/loading and remaining multiplayer/visual checks are recorded only in [Status](../STATUS.md). Source inspection and smooth mathematical curves do not establish WotG-equivalent visual quality, dash/recall behavior under latency, or compatibility with all equipment Mods.
