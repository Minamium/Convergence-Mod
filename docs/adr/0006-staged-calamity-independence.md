# ADR-0006: Staged Calamity Independence

- Status: Accepted
- Date: 2026-08-31
- Decider: Minamium

## Context

Convergence starts as a post-Exo Mechs/Supreme Calamitas addon because that gives the first multiplayer Raid a concrete progression point, equipment baseline, and audience. The long-term product goal is a Calamity-scale original Content Mod whose Raid architecture can operate without Calamity.

Removing the dependency before the first Raid is stable would require original progression, classes, materials, recipes, rewards, balance, World content, and a larger compatibility matrix at the same time as the multiplayer foundation. Treating Calamity as permanent would instead leak external concepts into Encounter and Raid domains and make later removal a rewrite.

ADR-0004 remains the Stage A integration rule: all Calamity access stays in `Common/Compatibility/Calamity`, unsupported versions fail closed, and no Calamity implementation or assets are copied.

## Decision

Dependency removal proceeds in three explicit stages.

### Stage A — Calamity addon

`build.txt` keeps the hard `CalamityMod` reference. The compatibility adapter supplies progression, class integration, and balance context. The first Raid, its multiplayer authority, and its release gates are completed in this stage.

### Stage B — Portable Raid core

Project-owned ports represent progression gates, class categories, equipment/balance tiers, and dependency capabilities. Encounter, Networking, Arena, and Raid-domain code consume those ports and do not reference Calamity types or names. The Calamity adapter implements the ports; dependency-free tests exercise their consumers.

Stage B is an architectural migration, not a second assembly requirement. The modular monolith remains until a demonstrated build or ownership boundary justifies extraction.

### Stage C — Standalone content Mod

Original progression, materials, equipment, recipes, World content, balance, and rewards replace the Stage A inputs. The hard `modReferences = CalamityMod` entry is removed only after the Standalone build/load and multiplayer matrix pass.

Calamity coexistence, if retained, becomes optional. Whether that uses a weak reference, a separate compatibility package, or no adapter is a later ADR based on tModLoader packaging and release evidence.

## Consequences

Positive:

- the first Raid can ship against a concrete endgame baseline without making that dependency permanent;
- server authority, packet schemas, arena ownership, cleanup, and Raid mechanics remain project-owned;
- the replacement work has a named milestone and measurable exit conditions;
- copied Calamity code, assets, names, and private implementation remain unnecessary.

Costs and risks:

- Stage B adds ports and compatibility tests that are not required for the first addon release;
- Stage C is a major content and balance project, not a dependency-line deletion;
- maintaining optional coexistence may multiply the release matrix and can be rejected if the cost is too high.

## Alternatives

- Remove Calamity before the first Raid: rejected because it combines foundation risk with an entire original progression stack.
- Keep Calamity as a permanent hard dependency: rejected because it conflicts with the long-term product goal and encourages architectural leakage.
- Copy or reimplement Calamity content as a shortcut: rejected for provenance, rights, originality, and maintenance reasons.
- Split the compatibility adapter into another assembly immediately: deferred until a real build or release boundary demonstrates the need.
