---
doc_id: encounter.first-severance.backlog
document_type: backlog
status: accepted
owners:
  - gameplay
last_reviewed: 2026-09-04
source_of_truth_for:
  - first_severance.deferred_scope
aliases:
  - deferred mechanics
  - old Third Severance plan
related_code:
  - Content/Encounters/ThirdSeverance
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.visual
---

# First Severance Deferred Backlog

These ideas are preserved, but none belongs to the first playable Definition of Done. Their presence in the current legacy immutable plan does not make them accepted live behavior.

Simplifying the vertical slice is a sequencing decision, not a ceiling on the eventual Raid or Mod. A later Calamity-scale encounter may promote, replace, or reject these ideas after the multiplayer foundation is proven; until then they remain future ideation rather than hidden active scope.

## Deferred mechanics

- Part Break and route selection.
- Targeted Line and positional Bait.
- Personal Effigies/class-reflective clones and support-after-clear behavior.
- Split Reality 1:1, 2:1, or 2:2 objectives.
- Last Stand/fixed final sequence.
- A separate hard-enrage phase after loop exhaustion. The first slice instead ends directly with `Defeat(LoopCapExceeded)`.
- Exposure-specific damage quota or Overload for missing a damage budget.
- Player-affinity Pylons.
- Class-specific synergy bonuses.

## Deferred Boss construction

- separately targetable Crown, Wings, and Heart Casing;
- humanoid masks/faces or elaborate religious anatomy;
- multiple rings, multiple Cores, large appendage rigs;
- five-form transformation chain (`sealed`, `manifest`, `convergence`, `exposed`, `last_stand`);
- part-dependent silhouette damage and route-dependent animations.

## Deferred production

- final proper nouns and public branding;
- finished sprites, shaders, dense VFX, camera work, and cinematic transitions;
- phase-specific final score, dynamic stems, and Last Stand music;
- rewards, recipes, lore items, trophy/relic/vanity;
- Solo redesign;
- optional Calamity coexistence after the Standalone stage.

## Archived setting and presentation seeds

The original brief used Antarctica, an enormous underground industrial research base, and a supernatural sealed being that is observed or restrained by machinery rather than being merely a machine. Its themes were a giant seal, collective consciousness, and loss of individuality. Those themes remain useful original-IP prompts, but the exact geography, lore, and Boss identity are not approved production canon.

Early provisional naming included `Second Convergence` → `Third Severance` → `Fourth Ascension`, `Erebus Polar Citadel`, `Polar Foundation Core`, `Identity Barrier` / `Causal Shell`, and `Collective Convergence`. The active first-event development name is now `First Severance`; the old sequence is an idea archive, not a reason to renumber the feature or preserve legacy code.

Presentation seeds worth revisiting after gameplay stabilizes include oversized phase/countdown captions, long quiet preparation followed by a sudden large-scale transition, player-specific musical fragments for Personal Effigies, defeated Effigy motifs joining the full arrangement, and a final cadence that reaches complete tonal resolution only when every participant survives. None is part of first-slice acceptance.

## Archived reward and class-synergy seeds

- Avoid a simple numerical tier above Shadowspec; prefer unusual behavior, multiplayer synergy, and Raid utility.
- A high-slot-cost summon could record a teammate's recent attack pattern and reproduce a weakened project-owned analogue; a Solo version could sample the owner's pattern. Feasibility, performance, ownership, and copied-asset/IP risks require a separate design.
- A Whip could apply a short party mark that improves a future Part Break window while remaining near existing endgame Whips in raw DPS.
- Small optional class interactions once Part Break/route mechanics exist: Whip tags aiding Part Break, Rogue stealth attacks adding a bounded weakness, Magic building an exposure charge, true melee briefly stabilizing facing, or repeated Ranged hits applying bounded armor crack.
- Every class and classless/support loadout must retain a baseline path through every required mechanic; bonuses cannot make one class mandatory.

The original drop-list sketch covered one weapon per major Calamity class, one Whip, two or three accessories, one utility item or mount, vanity, trophy, relic, lore, and possible Expert/Master rewards. Counts and items remain ideation until progression and reward power budgets receive their own specification.

## Recovery contingency, not an automatic fallback

If pinned tModLoader/Calamity evidence shows that death interception cannot coexist reliably, the team may separately evaluate ordinary Terraria death followed by deterministic Raid re-entry. That alternative must specify re-entry timing/location, token/resource cost, roster and encounter-state continuity, invulnerability, exploit prevention, replication, and cleanup. It is not authorized by a failed spike and must not be silently substituted for Downed/Revive; changing to it requires an explicit decision and ADR/spec/test update.

## Promotion rule

An item moves out of backlog only after the basic loop and Downed/Revive pass the Dedicated Server matrix, the added behavior has a player-facing spec and server-authority model, scope/tradeoffs are approved, and tests/cleanup/replication ownership are defined. Do not reintroduce an old idea only to preserve obsolete code.
