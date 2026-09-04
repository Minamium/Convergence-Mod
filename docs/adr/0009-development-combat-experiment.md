---
doc_id: adr.0009
document_type: adr
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-05
source_of_truth_for:
  - first_severance.development_experiment_boundary
aliases:
  - Development combat experiment
related_code:
  - Content/Encounters/FirstSeverance/FirstSeverancePrototypeCombatRuntime.cs
  - Common/Networking/Protocol/EncounterProtocol.cs
related_docs:
  - project.status
  - encounter.first-severance.spec
  - encounter.first-severance.revive
---

# ADR-0009: Bounded development combat experiment

## Context

After the multiplayer Ready check, the user requested an immediately testable combat loop, Terraria Boss music, and Down/revive, with proportionate verification. The production plan still requires normal-hit, lethal-hook, Barrier and broader multiplayer evidence.

## Decision

Development `0.1.1` may start an explicitly named prototype runtime after all Ready. It composes the existing authoritative loop and revive domains, creates only owned temporary Boss/Pylon NPCs, renders markers/music on clients, and grants a development revive kit. It does not mutate terrain, create a Barrier, grant progression/rewards, or connect `PreKill` or another Mod's lethal hook. This is an exception to the production activation work order, not evidence that those gates passed.

The experiment's own HP-damage adapter and `/convergence-down` can submit server-resolved Downed commands. No client sends health, damage, identity, duration, position success or revive completion. A kit request contains one nonce and the server chooses the closest eligible ally. Kit/range/held-use/movement/damage checks preserve the accepted two-second channel. Ordinary death or disconnect ends this experiment rather than transferring participant identity.

Protocol version advances to 2 because full snapshot layout gains a bounded combat section. Existing IDs remain unchanged; request IDs 5 and 6 are added. Health corrections carry a participant revision and post-revive deadlines so the local owning client receives the same committed correction. The host and friend must reload the same `0.1.1` build. Terminal snapshots and exact-Fight cleanup retain their existing coordinator ownership.

## Consequences

The user can test the requested loop and recovery flow now. Direct experimental HP damage is not ordinary mitigated Terraria damage. Actor hit hooks inherit Terraria's cooperative-client trust and remain unverified across damage classes. Stack target reissue, production Barrier, normal lethal interception, rejoin support and broader multiplayer evidence remain unfinished. No production/release acceptance follows from this experiment. Current build/playtest results live only in [Status](../STATUS.md).
