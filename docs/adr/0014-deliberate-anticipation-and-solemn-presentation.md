---
doc_id: decision.deliberate-anticipation
document_type: adr
status: accepted
owners:
  - engineering
  - gameplay
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.deliberate_anticipation
aliases:
  - ADR-0014
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceLance.cs
  - Client/Encounters/FirstSeverance
related_docs:
  - decision.energy-charge-feedback
  - encounter.first-severance.spec
  - encounter.first-severance.visual
  - project.audio-cues
---

# ADR-0014: Deliberate anticipation and solemn presentation

Explicit user direction, 2026-09-06: longer preparation between attacks, larger readable Boss anticipation, and unsettling/solemn high-resolution imagery and music. This supersedes ADR-0013 only for live charge tracking and the Ninth/chiptune/background rendering choices. The one-body authority, hit ledger, transient ownership and cleanup remain unchanged.

The exact-Fight runtime tracks the energy body during harmless windup, freezes both position and heading 24 ticks before FireTick, then launches straight at the existing 104px/tick. There is no damage or movement during the locked hold. A visible static cue and distinct audio latch precede firing. This addresses observed 0.2.4 hits before direction lock. Exposure/sequence budgets and phase rests are extended coherently; the feature spec owns their numbers. No control lock, forced dash, extra target request or actor is introduced.

Protocol v7 retains the bounded v6 DTO and all explicit packet IDs. Its semantic version changes because fire/lock timing and derived geometry changed; old clients must not interpret the new trajectory using their old shared tuning. Clients extrapolate only the authority sample, never steer or decide damage. Tests cover lock-to-fire immobility, zero prefire damage, projection at first fire, swept collision, finite lifetime and full sequence fit.

Client-only assets use original high-resolution basalt/ivory/bronze art. Two procedural GPU textures are created only on the draw thread and disposed on Mod unload; ReLogic owns requested image assets. The existing spawn-safe sky lookup/state transition is retained. World reset clears requested sky/fade, trails and all voices. Dedicated Server loads no visual/audio resource.

The old Ninth recording is replaced with a newly authored composition/performance/render using a verified CC0 instrumental library kept in external working storage. Standalone effects use new synthesis without borrowed samples. Source/asset licensing for public release remains undecided. Human listening and the user's focused multiplayer smoke remain required; build success is not audiovisual or live timing approval.
