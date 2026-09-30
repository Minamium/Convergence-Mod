---
doc_id: adr.0029
document_type: adr
status: accepted
owners:
  - architecture
  - gameplay
last_reviewed: 2026-10-01
source_of_truth_for:
  - authority.ebon_manor
aliases: []
related_code:
  - Content/Encounters/EbonManor
related_docs:
  - encounter.ebon-manor.spec
  - adr.0028
---

# ADR-0029: Ebon Manor native actors and hazards

## Decision

The new thread-mistress Raid is definition-routed `ebon_manor`, termination schema 5/version 1, protocol 76. Common packet operation IDs are unchanged. `EbonRuntime` owns the pedestal lease, connected Ready roster, frozen HP scaling, act clocks, target focus, Noirette's position, every hazard plan, stitch verdicts, recovery and one terminal commit. It imports no other feature's runtime and adds no global feature switch. Active state stays ephemeral.

For this feature only, extend ADR-0002's native-damage exception exactly as [ADR-0028](0028-azure-cathedral-native-actors.md) did for Cathedral: normal hostile `EbonAttack` projectiles and recipient-only `EbonStitchStrike` projectiles hit the receiving participant through native Terraria damage, so dodge, immunity, defense and accessory hooks apply once. The authority publishes each plan's geometry and live window; the server and every client evaluate the same pure geometry (`EbonGeometry` over `EbonRules`) at the accepted clock. No manual Hurt loop, direct life subtraction, custom damage packet or client decision. Noirette has no contact damage; weapon damage to her stays native, capped at each act floor by `NPC.HitModifiers.SetMaxDamage`, with a runtime clamp for DoT and `CheckDead` reporting defeat only in the Finale.

Bounded wire contracts: the full `EbonState` projection (1–8 ordered connection GUIDs, immutable music/unlock/end clocks, monotone stage and act, act start only after unlock, falling life) is appended to the definition-scoped Snapshot every 6 authority ticks and on observable changes, with NPC ExtraAI as a repair path. `EbonAttackPlan` validates finite coordinates, 20–240-tick warnings, 1–420-tick live windows and per-kind variant/spin rules; an act change rejects plans born before the new epoch. Stitch plans, verdicts and impacts copy the Cathedral chorus envelopes and explicit publication. Recovery binds the shared instant-unlimited `RaidReviveService` through a feature-local controller and appends health generation, Down anchor and immunity/lockout deadlines to each member, as ADR-0028's protocol 69 adapter does.

Client scene, audio, shader and music resources consume these projections and never feed back into authority. Timeout, death, cancel and unload remove every owned projectile and NPC and release the pedestal; clients joining after Ready are spectators. Native music is presentation only, not the hit clock.

## Evidence boundary

This extends an established, source-checked pattern rather than a new engine boundary: the pinned tML 2026.07.3.0 Projectile/Player and `HitModifiers` evidence linked from ADR-0026 and ADR-0028 applies unchanged. Terraria-independent tests cover the beat grid, act floors, kinematics, schedule-to-plan codec bounds, projection regression/truncation and stitch rules. Native package compilation proves construction only; native hits under installed accessories, matching-peer delivery, latency and rejoin behaviour remain owner playtests listed in the [feature spec](../encounters/ebon-manor/ENCOUNTER_SPEC.md#assets-and-pending-acceptance).
