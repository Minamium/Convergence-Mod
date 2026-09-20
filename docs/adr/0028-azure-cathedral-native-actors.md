---
doc_id: adr.0028
document_type: adr
status: accepted
owners:
  - architecture
  - gameplay
last_reviewed: 2026-09-20
source_of_truth_for:
  - authority.azure_cathedral
aliases: []
related_code:
  - Content/Encounters/AzureCathedral
related_docs:
  - encounter.azure-cathedral.spec
  - adr.0026
---

# ADR-0028: Azure Cathedral native actors and hazards

## Decision

The requested independent worm/girl Raid is definition-routed `azure_cathedral`, termination schema4/version1, protocol58. Common packet operation IDs remain unchanged. Its runtime owns the pedestal lease, connected Ready roster, frozen HP scaling, attack clocks, target locks, head movement,23 native worm parts, the girl, hazards and one terminal commit. It neither imports a previous feature runtime nor adds a global feature switch. Active state stays ephemeral.

For this feature only, extend ADR-0002's native-damage exception to normal hostile Projectile and worm contact hits on the receiving participant. The authority supplies the attack geometry/live window, while native Terraria applies dodge/immunity/defense/accessory hooks. No manual Hurt loop, direct statLife mutation, custom damage result packet or assumed cheat resistance. NPC weapon damage stays native; body segments share their head's `realLife`. Both named actors must die. Native death/inter-mod accessories are not a Doll Downed/Revive contract.

This independently scoped decision follows the already pinned tML2026.07.3.0 native Projectile/Player API evidence linked in [ADR-0026](0026-crimson-score-and-native-projectiles.md). It does not change Doll, Ghost Samurai, Scarlet or Oboro authority. The new feature retains 1–8-member bounded ordered connection GUIDs, strict finite geometry/timing, safe unowned native envelopes, monotone accepted snapshots, server-rejected client ExtraAI authority mutation, and exact-Fight cleanup including partial construction.

Client-owned scene/audio/shader resources consume these projections. Missing/stale ownership disables hazards. Timeout/death/cancel/unload removes every owned projectile/NPC and releases the pedestal; no UI/input flags survive. Clients joining after Ready are spectators. Native music is background-only, not the source of hit timing.

## Evidence boundary

### 2026-09-20 follow-up (protocol59)

Retain the original decision; add bounded Stack/Spread plan/verdict/recipient-impact envelopes. `AzureChorusDirector`, owned by the one authority runtime, resolves the frozen roster at its deadline; `AzureChorus` is a harmless synchronized marker, and `AzureChorusStrike` gates the ordinary native hit to exactly its resolved receiving player. Late/stale verdicts cannot revert a resolved outcome. No new common packet IDs, client decisions, persistence or feature-runtime dependencies.

Defeat is an idempotent runtime latch, separate from actor existence. Only a living, already-summoned worm requires the full linked chain. Retire owned defeated segments without invalidating the survivor. The retained girl carries state until the single final result/cleanup. Unexpected disappearance of a living required actor remains invalidation. This addresses the observed duplicate death notifications and next-tick `EncounterActorMissing`; it is not permission to translate arbitrary missing actors into Victory.

### Verification

Pure codec/clock tests and an actual Mod package/load check cover deterministic contracts and construction. Linked-production GPU frames cover material composition, not game FPS, native worm hit forwarding under installed accessories, rejoin behavior or remote presentation. Those remain explicit owner playtests in the [feature spec](../encounters/azure-cathedral/ENCOUNTER_SPEC.md).
