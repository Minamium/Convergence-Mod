---
doc_id: project.art-direction
document_type: spec
status: accepted
owners:
  - art
last_reviewed: 2026-09-05
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

## First Severance MVP

The active source of truth is [First Severance Visual Spec](encounters/first-severance/VISUAL_SPEC.md). Its accepted boundary is one simple Boss NPC/body, one life pool, no separately damageable presentation parts, and readable shielded/exposed states. The central Core/body, one broken ring, two short side arms, exact state names, and their motions are provisional placeholders. Crown/Wings/Heart Casing and other multipart designs are deferred and no longer shape the active code plan.

## Shape language

- Arena: horizontal/vertical industrial structure, columns, grids, repeated measurements.
- Boss MVP: central circle/Core, incomplete ring, two locking arms, strong negative space.
- Seal: concentric circles, radial anchors, broken continuity.
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

## Production order

1. placeholder body/ring/Core/Pylon and mechanic primitives;
2. in-game silhouette/hitbox/telegraph test;
3. 3–5 original concept options after mechanics are stable;
4. selected key poses and component separation;
5. pixel cleanup and animation;
6. grayscale/color-vision/reduced-VFX/multiplayer QA;
7. provenance entry before merge.

Do not produce elaborate multipart art for deferred backlog mechanics.
