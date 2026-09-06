---
doc_id: decision.phase-scores-terminal-survival
document_type: adr
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.phase_scores_terminal_survival
aliases:
  - ADR-0019
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceChoreography.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceScoreGeometry.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceLoopStateMachine.cs
related_docs:
  - decision.roster-health-core-salvos
  - encounter.first-severance.spec
  - project.status
---

# ADR-0019: Ordered phase scores and HP-zero survival

## Decision

Extend the existing feature-local stage plan with immutable ordered action scores, Distant and an explicitly named Final stage. Supersede ADR-0017/0018's immediate half-HP transition, repeated grid-only scheduling/eight-window cap and immediate HP-zero Victory. Retain their frozen roster, Core-salvo geometry, exact-Fight ownership and recovery rules. There is still one logical Boss health pool, not multiple gameplay arms/NPCs. Exact tuning belongs to the [encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md).

The authority loop owns action index, entry/deadline ticks, completed phase cycles and HP floors. No first phase transition is possible until every action in that phase has completed. Damage clamps at the next stage's entry threshold; excess is discarded, never carried into a later stage. Once a phase has completed a full score, subsequent threshold arrival may transition at the current damage action's deadline; attacks are not cut short by firepower. Final is entered at logical HP zero only after the prior phase score is satisfied. The owned NPC remains at least one HP and non-damageable; completing Final alone requests existing Victory/BossLifeZero. No normal NPC death awards early Victory. The legacy no-stage domain plan remains a separate regression fixture, not live gameplay.

`FirstSeverancePrototypeCombatRuntime` owns the sword/hand/half-field/terminal damage ledgers. `FirstSeveranceScoreGeometry` derives finite world geometry from action identity and server time. Clients share geometry but never apply damage, assign targets, change phases or send completion. The logical arms, sword, broad half-field rays and bullets do not allocate world entities. Each action clears its ledgers; cancel/end/unload clears all remaining geometry through exact-Fight cleanup. Down/revival protection and terminal priority remain unchanged.

Protocol **v13** retains packet IDs and requests. Immediately after BossPhaseStartedTick, the combat snapshot adds ActionStartedTick (ulong), ActionIndex (signed byte: -1 for the legacy opening/transition or a bounded score index), and CompletedPhaseCycles (byte). Existing v12 grid/Core direction data follows. Constructors validate stage/action compatibility, finite coordinates, ordered ticks, zero Final HP, and the old bounded participant/volley contracts before replica application. All peers need the same package. Repair is still a full snapshot; action timing is not reconstructed from chat/audio or a local completion counter.

The distant Boss body and two arm rigs are presentation around the same visible central remote damage aperture; decorative hands cannot be damaged independently. Extra phase transitions use the existing conditional camera/HUD layer, never a persistent UI/input flag. Dedicated Server never loads textures/audio. A new original background-stage score and an offline continuously accelerated Final master use the existing music-slot API; playback never governs gameplay. The accepted Victory owns only a disposable 210-tick inward-collapse visual/audio tail after immediate gameplay cleanup.

## Extension and verification boundary

Insert future phases before the explicit Final definition, assigning stable new enum values and a score/adapter. Do not reinterpret Final's current numeric value as the number of ordinary phases. New replicated timing changes require matching protocol/tuning. No new dependency, save field or public debug command is introduced.

Focused checks cover 2/3/4-member overkill, every ordered action/threshold, HP-zero survival, exact warning/live boundaries, movement budgets, bounded DTO round trips and cleanup/terminal invariants. Human confirmation of actual dodgeability, camera/animation/audio quality and remote-client timing remains a user-owned multiplayer smoke. [Status](../STATUS.md) owns results, not this decision.
