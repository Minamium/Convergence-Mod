---
doc_id: project.art-direction
document_type: spec
status: accepted
owners:
  - art
last_reviewed: 2026-09-12
source_of_truth_for:
  - project.art_direction
aliases:
  - art direction
  - visual language
related_code:
  - Assets
related_docs:
  - encounter.first-severance.visual
  - encounter.first-severance.backlog
---

# Art Direction

## Feature scope

Each encounter has its own silhouette, materials, motion and cue vocabulary. Shared guidance below concerns readability and presentation cost. The Doll's palette and Raid markers do not define an independent Boss.

| Feature | Appearance and motion owner |
|---|---|
| 不幸な人形劇 / `FirstSeverance` | [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) for the bound Gothic doll, restraint structure and reveal; [Visual Spec](encounters/first-severance/VISUAL_SPEC.md) for readability and scene rules |
| Ghost Samurai | [Boss spec](encounters/ghost-samurai/ENCOUNTER_SPEC.md) for the horned skeleton, blue-white ghost fire, dual swords, rig and attack presentation |

## Current Doll Play presentation

The bound white-haired girl doll remains inside an inhuman restraint structure. The accepted face, crown, arms, shell and Core composition are owned by Doll Theater; reuse those details rather than deriving a replacement from an older general style paragraph. One logical damage target does not imply multipart collision.

The following shape/palette notes apply to the Doll Play field. Follow its current visual spec when a local instruction refines these broad terms.

## Shape language

- Arena: horizontal/vertical industrial structure, columns, grids, repeated measurements.
- Boss: white hair, restrained sorrowful face, layered black dress, porcelain ball joints, tilted neck/waist and unequal cable tension. No upright mechanical keel or orbital god silhouette.
- Seal: vertically enclosed porcelain doll coffin, damaged petals, dark iron bindings and legible asymmetrical openings.
- Danger: triangles, segmented lines, converging motion.
- Stack: circle plus inward motion.
- Spread: diamond/radial marker plus outward motion.
- Pylon: stable numbered/shape glyphs distinguishable without color.
- Revive: restrained cross/linked-pulse language that cannot be mistaken for a damage marker.

## Palette

- ice white / pale cyan;
- charcoal / black metal;
- warning red;
- restrained oxidized dark gold.

Assignment markers must separate from both arena and Boss palette. Shape, motion, cadence, and text/icon reinforce color.

## Readability hierarchy

1. Player and gameplay telegraph.
2. Assignment/resolve countdown and safe/danger shape.
3. Boss damageable/undamageable state.
4. Pylon and revival interactable state.
5. Decorative particles/background/architecture.

Boss scale may be visually large, but hitbox and damage gate must be obvious. Decoration is darker/lower contrast than telegraphs. Reduced/Minimal VFX must preserve all mechanics.

## UI and accessibility

- short localized phase/substate names;
- readable countdown at 100–150% UI scale;
- distinct Stack, Spread, Pylon, Downed, and Revive shapes;
- no color-only assignment;
- warnings do not cover players, markers, or safe areas;
- screen shake, flash, chromatic effects, and dense particles are reducible;
- select relevant player-count overlap, resolution and UI-scale checks from the verification matrix; record untested configurations honestly.

## Forbidden references

- recognizable EVA-like head/jaw/shoulder/restraint combinations;
- NERV-like logos/seals or other trademarked iconography;
- direct reproduction of existing angel/face/Core/mask motifs, cinematic framing, timing, or subtitles;
- extraction, repainting, tracing, or remixing Calamity sprites/textures/particles;
- prompts requesting a living artist's exact style.

## Bounded presentation changes

For timing/continuity work, reuse accepted art and follow the development Skill's [presentation direction](../.agents/skills/develop-convergence-raids/references/presentation-direction.md). Name the arrival/brake/release/recovery beats, retain authoritative warning/hit geometry and inspect only the affected scene/poses. For genuinely new art, select a silhouette, separate only required components, preserve native material detail and record provenance before merge. Broader multiplayer/accessibility acceptance is selected by risk, not automatically rerun on every visual edit.

Do not produce elaborate multipart art for deferred backlog mechanics.
