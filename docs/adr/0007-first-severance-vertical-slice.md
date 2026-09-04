# ADR-0007: First Severance Vertical Slice

- Status: Accepted
- Date: 2026-09-04
- Decider: Minamium

## Context

The inert bootstrap was originally named `ThirdSeverance` and modeled a large seven-phase encounter with multipart Boss forms, Part Break, Targeted Line, Personal Effigies, exposure damage quotas, and Last Stand. That design was too broad for the project's first playable multiplayer implementation and made the event's “third” position carry lore/production decisions before a first event existed.

The first implementation must prove the difficult foundations: 2–4-player server authority, understandable cooperative mechanics, stable replication, exact cleanup, and a recoverable Downed/Revive system. Visual complexity and additional phases can be added only after that loop is reliable.

## Decision

The first Raid is developed as `First Severance` / `第一断絶`, with target feature name `FirstSeverance` and stable key `first_severance`.

Its first-playable active loop is:

```text
Boss spawn
  -> Pylon DPS check
  -> Stack
  -> Spread
  -> Core exposure
  -> repeat from Pylon while boss HP remains
```

Boss HP is persistent and damageable only during Core exposure. Stack is a real server-owned head-split damage pool; Spread is a server-resolved positional mechanic. Both use soft failure rather than an authored instant-wipe command. Pylons are open party objectives rather than participant-affinity objectives.

The accepted Boss constraint is one simple NPC/body and one life pool; presentation components are not breakable actors. A central Core, one broken ring, and two short side arms are only the provisional placeholder silhouette and may change without superseding this ADR.

Raid-only Downed and item-channel Revive are part of the first playable acceptance target. The existing pure authority domain is retained; the Terraria/Calamity lethal adapter stays disconnected until pinned Single Player, Host & Play, and Dedicated Server instrumentation defines safe coexistence.

Part Break, Targeted Line/Bait, Personal Effigies, Split Reality, Last Stand, multipart Crown/Wings/Heart Casing, final rewards, and production art/audio are deferred. Boss, facility, lore-collective, activation-item, and revival-item proper names remain provisional unless separately accepted.

Prototype values remain feature-spec policy rather than immutable ADR decisions: one Pylon per pull-roster member, Pylon failure pulse/short exposure, three Overloads, Stack required shares `2 / 2 / 3`, all durations/radii/damage/HP, an eight-exposure cap, and the exact placeholder art. The current deterministic loop-cap edge is direct `Defeat(LoopCapExceeded)`; a hard-enrage or Last Stand phase is deferred. The vertical slice targets roughly 2–4 minutes, while the eventual expanded Raid retains the broader 5–12-minute product goal.

The first slice is designed for cooperative multiplayer using unmodified clients. Server/SP owns feature state and no custom packet may declare DPS, position success, life, revive completion, or victory; this does not claim anti-cheat against a modified client forging Terraria's ordinary movement or combat replication. Enabling the damage gate requires recorded Host & Play and Dedicated Server hit-pipeline evidence.

For deterministic recovery, a Stack target invalidation permits exactly one authority reissue with a new assignment revision and a full configured telegraph; no candidate or a second invalidation resolves one bounded soft failure and advances after terminal selection. The owning feature reducer settles all same-tick mechanics and Revive commands once, then chooses the feature-local terminal priority before any nonterminal edge; coordinator-owned unload/exception/protocol termination preempts it through an immutable definition mapping. Normal Active player attempts to remove the Foundation Core Tile/TE are rejected; unexpected Tile/TE loss invalidates/aborts the Fight, while admin/debug abort is an explicit command.

The current unpublished `ThirdSeverance` code is renamed atomically in a dedicated Windows commit while activation remains denied. No runtime compatibility alias is required. Numeric packet IDs and historical ADR/research/changelog wording are not rewritten.

## Consequences

Positive:

- the first implementation has one repeatable, testable multiplayer loop;
- server authority and recovery behavior are validated before encounter breadth;
- Boss assets can begin with minimal placeholders;
- legacy ideas remain available without silently expanding MVP scope;
- the feature name no longer depends on unwritten first/second events.

Costs and risks:

- the current immutable plan and its domain tests must be simplified rather than treated as completed design;
- documentation and source use different feature names until the isolated rename commit;
- including Downed/Revive keeps a difficult compatibility gate in the first playable target;
- exact timings, damage, HP, and failure tuning still require Windows multiplayer telemetry.

## Alternatives

- Keep `Third Severance` and invent prerequisite lore: rejected because it overcommits narrative order and does not help the first implementation.
- Implement the complete multipart seven-phase plan first: rejected as too much networking, content, presentation, and balance risk at once.
- Defer Downed/Revive beyond the first playable Raid: rejected because cooperative recovery is part of the intended multiplayer identity.
- Make every Pylon player-specific: rejected for the first slice so surviving players can recover from Downed/disconnect pressure through coordination.
