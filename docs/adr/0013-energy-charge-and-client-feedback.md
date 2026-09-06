---
doc_id: decision.energy-charge-feedback
document_type: adr
status: accepted
owners:
  - engineering
  - gameplay
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.energy_charge_feedback
aliases:
  - ADR-0013
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceLance.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceFeedback.cs
related_docs:
  - project.network-architecture
  - encounter.first-severance.spec
  - project.audio-cues
---

# ADR-0013: Bounded semi-homing energy body and local Raid feedback

Partial supersession: [ADR-0014](0014-deliberate-anticipation-and-solemn-presentation.md) replaces live steering with a harmless prelaunch lock and replaces the chiptune/earlier visual choices. The ownership and bounded replication decisions below remain; the original decision text is retained for history.

Decision: explicit user request, 2026-09-06. Supersedes only ADR-0012's deterministic horizontal sweep for exposure steps 1/3. Beam sequences, defeat consequences and reusable instant revival remain unchanged.

The existing exact-Fight runtime owns a single approaching/briefly steering/locked charge and its once-per-step hit ledger. Its bounded motion samples enter the existing immutable attack descriptor through protocol v6; clients project the sampled heading, never steer from their local target view. Phase exit, early end and all terminal cleanup remove the descriptor; no extra world actor, global switch or client hit request is introduced. The feature spec owns timing and dimensions. Ordinary engine immunity is respected for contact-like charges to allow real invulnerability dashes; percentage Stack/Spread rules are unchanged.

Dedicated Server computes motion only. Participant client presentation owns sounds, trails, shock rings, sky fade and BGM selection. Results come from existing committed mechanic revisions, not a client geometry guess. Short voice handles are bounded/pruned and stopped on cleanup; the sky owns no GPU allocation or persistent world setting. The one full-file chiptune loop follows the scene effect, without sample-accurate multiplayer synchronization or music-driven gameplay.

The Ninth composition/score basis is public domain; the arrangement, oscillator performance and recording are new. No Calamity/Wrath of the Gods code/assets, film audio, modern MIDI or sample library enters the repository. Exact exports/provenance are in `Assets/ATTRIBUTION.md`. This is development material, not a license/release approval. Installed-Mod i-frame behavior and attack readability need the user's short multiplayer smoke; compilation is not that evidence.
