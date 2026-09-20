---
doc_id: project.art-direction
document_type: spec
status: accepted
owners:
  - art
last_reviewed: 2026-09-18
source_of_truth_for:
  - project.art_direction
  - presentation.luminance_policy
aliases:
  - art direction
  - visual language
  - Luminance presentation policy
related_code:
  - Assets
related_docs:
  - encounter.first-severance.visual
  - encounter.first-severance.backlog
  - encounter.ghost-samurai.spec
  - encounter.crimson-foundry.spec
---

# Art Direction

## Feature scope

Each encounter has its own silhouette, materials, motion and cue vocabulary. The shared Luminance policy below applies to every feature's presentation; feature specs own its artistic identity and gameplay. The Doll's palette and Raid markers do not define an independent Boss.

| Feature | Appearance and motion owner |
|---|---|
| Requiem of the Hollow Doll / `FirstSeverance` | [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) for the bound Gothic doll, restraint structure and reveal; [Visual Spec](encounters/first-severance/VISUAL_SPEC.md) for readability and scene rules |
| Ghost Samurai / Oboro | [Boss and weapon spec](encounters/ghost-samurai/ENCOUNTER_SPEC.md#luminance-presentation-target) for violet ghost fire, armored oni identity, dual swords, connected motion and attack materials |
| Scarlet Invocation / `CrimsonFoundry` | [Scarlet spec](encounters/crimson-foundry/ENCOUNTER_SPEC.md#luminance-presentation-v2) for the small performer, distinct apparitions, score and scarlet physical strokes |
| Other NPCs, weapons, projectiles, companions, fields and UI | Their owning spec plus this shared policy; they are not exempt because they are outside a Raid renderer |
| Cathedral of the White Night / `AzureCathedral` | [Azure spec](encounters/azure-cathedral/ENCOUNTER_SPEC.md#presentation) for the small cyan-haired swordswoman, linked ice-glass leviathan, cathedral and water-refraction materials |

## Luminance presentation policy

**Actively use Luminance as the shared presentation foundation across all content.** For a new visual implementation or a substantial visual revision, select and use its relevant, version-verified capabilities to produce continuous motion, evolving materials and coherent attack/scene choreography. A dependency declaration, an unused API call, extra glow, or more particles does not establish that outcome. Prefer the library's suitable existing facilities over rebuilding equivalent graphics infrastructure.

This applies to Boss/NPC bodies, weapon and companion motion, projectiles, warnings, impacts, transformations, entrances/endings, arena/background layers and animated UI. It governs client presentation, not server decisions, collision authority, networking or saved identity. Work on the requested surface; adopting this policy does not require an unrelated repository-wide rewrite during a small repair.

### Motion and source artwork

PNG/atlases remain valid source art, masks, key poses and authored animation. Preserve approved silhouettes, texture detail and feature colors. For principal animated bodies and effects, **selecting a few complete-image poses plus whole-image translation/rotation/scaling is not sufficient completion of a motion or presentation overhaul**. Nor is a crossfade between those poses evidence of articulated movement. Resolve the visible limitation through connected part pivots, overlapping masks/meshes, authored in-between frames or another demonstrated continuous technique.

Build anticipation, loading, release and recovery on a shared fractional action clock. Keep shoulders, hands, blades, emitters and trails attached through the transitions; use local follow-through for cloth, flame or ornaments. Luminance easing and projected states can coordinate this, but do not automatically supply a character rig. A deliberately small pixel NPC or static inventory icon can retain its appropriate authored treatment; it does not need a shader merely to tick a box. Explain a simpler choice briefly in the affected task when it changes the requested visual outcome, without adding an approval gate.

### Choose capabilities by visible result

Select only the rows needed for the affected scene; this is not a requirement to instantiate every subsystem on every actor.

| Visible result | Preferred Luminance use and design requirement |
|---|---|
| Connected body/weapon motion | Easings and presentation-state projection with feature-owned pivots/curves; bounded decorative Verlet chains where flexible parts need follow-through |
| Flowing flame, ghost energy, metal/cloth response or dissolution | Managed shaders, original material passes and runtime texture/noise references; animate local material coordinates and keep shape/detail readable on bright and dark terrain |
| Blade arcs, trails, ribbons, beams and shock fronts | Primitive rendering and managed materials where appropriate; sample the actual accepted motion/geometry so the trail connects to its emitter and the danger footprint remains honest |
| Embers, ink, residue and depth | Bounded particles or Metaballs when their blending is useful; use library-managed reusable targets for effects requiring offscreen composition |
| Fields, backgrounds and animated markers | Layered materials, bounded parallax and progress driven by accepted clocks; keep combat information above decoration and preserve the owning spec's marker vocabulary |
| Impact and cinematic punctuation | Local screen shake/cutscene facilities when called for, with existing user options and cancellation; no input lock, world-time mutation or extra gameplay delay |

Use the target in [Version Matrix](VERSION_MATRIX.md). The [pinned API evidence](encounters/first-severance/VISUAL_SPEC.md#luminance-raid-presentation) and existing [Scarlet implementation](encounters/crimson-foundry/ENCOUNTER_SPEC.md#luminance-presentation-v2) establish available starting points, not universal presets. Inspect only the relevant helper and verify unresolved calls against the installed version; public source and installed binary identity must not be assumed. Reuse project abstractions when they fit; keep each feature's shapes/colors/timings independent.

### Ownership and readable fallback

Preserve authoritative warning/live/end times, collision, targeting and rewards during a presentation-only change. Reconstruct local animation from accepted snapshots/clocks; decorative simulation never supplies hit coordinates. Give transient effects exact Fight/epoch/projectile ownership and bounded lifetime; reset them on replacement, teleport/discontinuity, cancellation and world/Mod teardown as applicable.

Respect library-owned shader/target/texture lifetimes. Create and release owned graphics resources on the correct graphics thread, restore the caller's SpriteBatch/device state, and avoid per-frame allocation. Dedicated Server paths must not initialize graphics. Reduced Effects/shake-off should reduce optional motion, residue, layers and exposure while retaining bodies, attack footprints and assignment information. Scope limits from a feature spec, such as Doll's existing pass/target budget, remain in force.

### Presentation completion

- Identify the intended visible improvement and the capabilities actually used. Review the affected start, hold, release, recovery and cancellation, not just a selected attractive still.
- For motion/material work, inspect a short clip or sufficiently sampled frame sequence from the changed renderer. Label whether it is offline output or the actual game. Show continuity, emitter attachment, material evolution and warning/live separation; a generated concept image or import-only check is not runtime presentation evidence.
- Run applicable build, shader-export, lifecycle and geometry checks through the [Verification Matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md). Check the relevant bright/dark background, zoom/UI scale, Reduced Effects and peer visibility cases. Measure frame-time/resource budgets when the change affects them; do not claim measured performance from a build or API-count.
- Report implementation/check results separately from remaining user-owned in-game acceptance. Keep that acceptance `not_run` with its concrete scene/action until observed; it does not require repeating unchanged whole-fight tests. A successful build or offline preview alone is not visual approval.

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
