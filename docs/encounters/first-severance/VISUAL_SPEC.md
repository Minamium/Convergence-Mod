---
doc_id: encounter.first-severance.visual
document_type: spec
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-04
source_of_truth_for:
  - first_severance.visual_mvp
aliases:
  - First Severance boss visual
  - Null Cantor visual
related_code:
  - Content/Encounters/ThirdSeverance
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.backlog
---

# First Severance Minimum Visual Specification

The first Boss is deliberately simple so network authority and encounter readability can be proven before production art.

## Accepted visual boundary

- one simple Boss NPC/body and one authority-owned Boss life pool;
- no separately damageable Crown, Wings, arms, rings, casing, or other multipart actors;
- an unmistakable, non-color-only difference between shielded and exposed gameplay states;
- placeholder art is sufficient for the first playable acceptance target.

The exact silhouette, component count, palette, proportions, and animation are not yet user-accepted production design.

## Provisional prototype silhouette

- one central floating Core/body;
- one incomplete or broken annular ring behind it;
- two short lateral locking/support arms;
- no humanoid anatomy, face, eye, crown, wings, robe, or multiple breakable body parts;
- ring and arms are presentation attached to one Boss NPC, not separately damageable actors.

A single placeholder texture is acceptable for the first runtime. Preferred later separation is body, ring, Core/emissive mask, with rotation/pulse/glow driven by code. These components may be replaced without an ADR as long as the accepted one-body/readability boundary remains true. Pylons are separate gameplay NPCs with their own simple placeholder.

## Gameplay states

| State | Gameplay meaning | Minimum presentation |
|---|---|---|
| `Dormant` / spawn | introduction, no damage | closed/dim Core, slow ring, arms inward |
| `Shielded` | Pylon, Stack, Spread, Reset; Boss invulnerable | opaque shell, stable ring, clear non-damageable feedback |
| `Exposed` | Core damage window | opened/brighter Core, broken ring displaced or faster, unmistakable hit window |

`Shielded` and `Exposed` are accepted gameplay meanings. `Dormant`, the exact state names, and every presentation detail in the table are provisional implementation vocabulary. State change is replicated gameplay information. Rotation, particles, light flicker, afterimages, and interpolation are disposable client presentation.

## Readability rules

- Telegraphs and player markers always render above decorative Boss effects.
- Shielded hits must produce a clear harmless response without client-decided damage.
- Exposed must remain distinguishable in grayscale and reduced-VFX mode.
- Stack and Spread use different shapes and motion, not color alone.
- The 2/3/4-player marker layouts must be tested at 100–150% UI scale and common resolutions.
- No visual component may imply a targetable part unless it has an authority-owned hit rule.

## Palette direction

Retain the project palette—ice white/pale cyan, charcoal/black metal, warning red, restrained dark gold—but use placeholder flat colors initially. Exact palette, animation, lore ornament, shader, and high-detail sprite are provisional production decisions.

## Explicitly deferred

Crown, Wings, Heart Casing, humanoid masks, multiple rings/Cores, large appendage rigs, multipart break states, and elaborate transformations belong to [Backlog](BACKLOG.md). They are not required to implement or clear the first encounter.
