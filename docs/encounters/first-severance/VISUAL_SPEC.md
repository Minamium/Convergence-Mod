---
doc_id: encounter.first-severance.visual
document_type: spec
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-06
source_of_truth_for:
  - first_severance.visual_mvp
aliases:
  - First Severance boss visual
  - Null Cantor visual
related_code:
  - Content/Encounters/FirstSeverance
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.backlog
---

# First Severance — Null Cantor Visual Pass

The user requested giant scale and spectacle comparable in ambition to Avatar of Emptiness / Nameless Deity, combined with this project's polar containment/ritual theme. `0.2.0` implements an original first pass, not a reproduction of those Bosses or a claim of equivalent finished animation/shader quality. The original small-placeholder scope is superseded for this pass by [ADR-0010](../../adr/0010-giant-boss-observation-lances.md).

## Accepted visual boundary

- one logical Boss NPC/body and one authority-owned Boss life pool, without a visual-size cap;
- no separately damageable Crown, Wings, arms, rings, casing, or other multipart actors;
- an unmistakable, non-color-only difference between shielded and exposed gameplay states;
- independent original art and layered client VFX for this requested development pass; final pixel cleanup and release art remain future work.

The exact silhouette, component count, palette, proportions, and animation are not yet user-accepted production design.

## Original design — 無響の唱導者 / The Null Cantor

- one faceless black-ice cathedral/keel body surrounding a dark circular aperture;
- frost-white ceramic ribs, charcoal metal, sparse cyan rim light and oxidized-gold restraints;
- four large lateral caliper-like structures; the two side groups separate and turn outward during exposure;
- broken industrial seal with six radial anchors, counter-rotating elliptical observation rings, subdued aurora and drifting fragments;
- no humanoid anatomy, face, eye, crown, wings, robe, or multiple breakable body parts;
- all rings/restraints are presentation attached to one Boss NPC, not separately damageable actors.

Runtime export: `Assets/Textures/NPCs/NullCantorBody.png`, a 1254x1254 RGBA original generated asset. The aperture pivot is approximately `(0.50, 0.44)` of the canvas. Code draws three source-rectangle groups (center, left, right) and adds independently authored geometry; the original file is not repainted from another work. Art brief/prompt remains in external working storage, with a summary in [Attribution](../../../Assets/ATTRIBUTION.md).

At full reveal, the sprite canvas is 820 world pixels tall; orbital geometry spans roughly 1,100 pixels. The Boss aperture is 360 pixels above the Foundation Core. Its clearly bracketed 144x144 NPC hitbox remains stationary while decoration moves. There is **no contact damage** on the enormous decorative body. Flight and ranged endgame weapons can attack the aperture. Pylons are separate 56x88 NPCs with code-drawn containment cages; left/right offsets are 360 pixels and the extra outer positions are 530 pixels.

## Gameplay states

| State | Gameplay meaning | Minimum presentation |
|---|---|---|
| `Dormant` / spawn | introduction, no damage | silhouette/seal reveal, working-title card, closed aperture |
| `Shielded` | Pylon, Stack, Spread, Reset; Boss invulnerable | dark body, diagonal braces across the aperture, inward tethers and gold ring |
| `Exposed` | Core damage window | separated side structures, absent braces/tethers, brighter aperture and four separated hitbox brackets |

`Shielded` and `Exposed` are accepted gameplay meanings. `Dormant`, the exact state names, and every presentation detail in the table are provisional implementation vocabulary. State change is replicated gameplay information. Rotation, particles, light flicker, afterimages, and interpolation are disposable client presentation.

## Readability rules

- Telegraphs and player markers always render above decorative Boss effects.
- Shielded hits must produce a clear harmless response without client-decided damage.
- Exposed uses changed structure/braces, not only color, including reduced-VFX mode.
- Stack and Spread use different shapes and motion, not color alone.
- The full 2/3/4-player/UI-resolution matrix is not claimed complete; first verify one user-run two-player loop.
- No visual component may imply a targetable part unless it has an authority-owned hit rule.

## Palette direction

Retain ice white/pale cyan, charcoal/black metal, warning red and restrained dark gold. Warm white-hot red/orange lances contrast with the cold body. There is no copied film cross-shaped explosion, logo, audio or recognizable face/angel anatomy.

## Attack and accessibility language

- Stack: 7-tile cyan circle, shrinking countdown ring and inward chevrons.
- Spread: 14-tile orange-red circles, shrinking countdown, outward chevrons and central diamonds. No two circles should overlap (28-tile center separation).
- Observation lance: thin locked center line, dashed edges marking the full future 88-pixel corridor, direction chevrons and a closing charge ring. After 72 ticks, a white-hot inner beam, warm outer beam and optional bloom fire for 18 ticks. Decorative bloom extends beyond the sharply marked actual width; the live core never visually shrinks below that width while damage is active.
- Render body/seals first, lance effects next, and player mechanic/revive markers last. Draw all line primitives from an explicit one-texel MagicPixel source to preserve the `0.1.2` spoke fix.
- Reduced Effects lowers rings/aurora/shards/glow/darkening and disables shake. All danger rails, live beams, assignment shapes and damage-window structure remain. Screen Shake can be independently disabled. No full-screen white flash or forced camera zoom.
- Vanilla Boss 3 remains music; charge/fire use runtime vanilla sound IDs. No external recording is included.
- Effects consume snapshots and are disposable: phase exit cancels a lance, Fight end leaves a brief harmless seal dissipation, World/Mod unload clears state and disposes the one procedural glow texture on the render thread. Dedicated Server never requests textures or plays cues.

## Explicitly deferred

Separate damageable Crown/Wings/Heart Casing, humanoid masks, multipart break states, elaborate phase transformations, custom music and final hand-cleaned animation remain in [Backlog](BACKLOG.md). The original large decorative restraints/orbits above are in the current user-requested pass and do not create multipart gameplay.
