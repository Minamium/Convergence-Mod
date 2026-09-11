---
doc_id: project.art-direction
document_type: spec
status: accepted
owners:
  - art
last_reviewed: 2026-09-11
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

## Core statement

極地の巨大研究・収容施設が、封印対象の起動により工業設備から儀式装置へ読み替わる瞬間を描く。既存作品やCalamity assetの形・構図を借りず、低密度でも読める独自の幾何学表現から始める。

## Current First Severance presentation

First Severance retains the tragic white-haired Gothic **girl doll** inside an inhuman restraint structure. Do not simply enlarge the NPC face/costume: the user's refinement restores the old crown/body silhouette around a smaller, partly veiled face, keeps the accepted porcelain arms and limits pre-eclosion exposure. [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) owns those details, the conversation NPC and fragment capture into an already present coffin. Reuse the accepted hinged eclosion, field and terminal effects. [Presentation](encounters/first-severance/VISUAL_SPEC.md) still owns attack readability and kinetic contrast. One life pool/marked chest aperture remains one logical damage target; the giant decorative anatomy is not multipart collision.

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
- test 2/3/4-player overlap at 1080p, 1440p, ultrawide, and supported UI scales.

## Forbidden references

- recognizable EVA-like head/jaw/shoulder/restraint combinations;
- NERV-like logos/seals or other trademarked iconography;
- direct reproduction of existing angel/face/Core/mask motifs, cinematic framing, timing, or subtitles;
- extraction, repainting, tracing, or remixing Calamity sprites/textures/particles;
- prompts requesting a living artist's exact style.

## Bounded presentation changes

For timing/continuity work, reuse accepted art and follow the development Skill's [presentation direction](../.agents/skills/develop-convergence-raids/references/presentation-direction.md). Name the arrival/brake/release/recovery beats, retain authoritative warning/hit geometry and inspect only the affected scene/poses. For genuinely new art, select a silhouette, separate only required components, preserve native material detail and record provenance before merge. Broader multiplayer/accessibility acceptance is selected by risk, not automatically rerun on every visual edit.

Do not produce elaborate multipart art for deferred backlog mechanics.
