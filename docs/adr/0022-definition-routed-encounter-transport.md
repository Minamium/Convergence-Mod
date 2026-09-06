---
doc_id: adr.0022
document_type: adr
status: accepted
owners:
  - networking
last_reviewed: 2026-09-07
source_of_truth_for:
  - architecture.definition_routed_transport
aliases:
  - definition packet routes
related_code:
  - Common/Networking/EncounterPacketRoutes.cs
  - Common/Networking/EncounterSnapshotTransport.cs
  - Common/Networking/Protocol/EncounterRouteCodec.cs
related_docs:
  - project.network-architecture
  - project.architecture
---

# ADR-0022: Definition-routed encounter transport

## Decision

Common owns envelope/direction validation, a definition-key adapter registry, active-session routing, snapshot repair and the single server snapshot-outbox drain. Each feature registers one packet adapter alongside its definition. Common operation IDs are shared, not globally claimed by the first feature. No second encounter, assembly split or mechanic DSL is introduced.

Protocol 17 retains numeric packet IDs and the existing fixed header. Except RequestSnapshot, a byte-length ASCII definition key follows it (1–64 lowercase letters/digits/underscore). Empty is reserved for a common Idle Snapshot, followed only by its ulong authority tick. RequestSnapshot remains header-only and asks the server for the actual current session, independent of a feature selection.

Live client intents must match the server's definition, sequence and Fight before the feature decodes/revalidates its bounded operation. Activation selects a registered definition but still resolves the real Core, sender, roster and policies server-side; the route grants no gameplay authority. Feature codecs remain responsible for their complete typed payload before mutation. No receive path drains a shared stream to EOF.

Full non-idle snapshots use the owning feature adapter. Generic Idle updates the ordered replica and clears feature projections. Registration rejects duplicate/invalid keys; unload clears the registry; world changes clear repair-rate state. Client-only presentation never runs on Dedicated Server.

## Compatibility and verification

All peers need the matching protocol-17 package. No save migration or gameplay tuning changes. Existing header/feature body contracts are retained behind the new selector. Focused tests cover independent handler registration with the same operation IDs, invalid/unknown selectors, stale/cross-Fight selection, bounded/truncated decoding and shared-buffer suffix preservation. Real Mod build and user-owned Host & Play load/Ready/snapshot/cleanup remain distinct evidence.

This replaces FirstSeverancePacketSystem's global operation registrations and outbox ownership, not the accepted authority, replica ordering or exact-Fight cleanup decisions.
