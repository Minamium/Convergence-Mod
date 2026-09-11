---
doc_id: encounter.first-severance.visual
document_type: spec
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-11
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

# First Severance — Current Presentation Direction

Attack/field **development** art remains the baseline, not a claim of final production/Wrath of the Gods quality. The 2026-09-11 user direction replaces the abstract Boss identity with [Doll Theater](DOLL_THEATER_VISUAL_SPEC.md): a white-haired Gothic ball-jointed girl swallowed by a coffin and forced to fight while crookedly suspended. That document owns the new body/NPC/asset details. Prior passes, rejected designs and pinned API research are preserved in [visual evolution](../../history/2026-09-11-visual-evolution.md); do not apply them as additive requirements.

## Creative intent and motion

Aim for an ominous, solemn doll theater: ivory hair/porcelain, charcoal Gothic cloth, restrained aged brass and dark cathedral scenery. The new NPC/Boss uses deliberately simplified pixel clusters and hard sampling; it does not paste a high-resolution illustration into Terraria. Brief fluorescent pearl/ruby/violet attacks retain their accepted flowing material and readable footprint. Pixel density for the doll does not imply chiptune music or redoing every weapon/beam.

Wrath of the Gods' Avatar of Emptiness/Nameless Deity and the user's Evangelion references guide ambition, scale and tension, **not** copied characters, signatures, code or assets. [The benchmark](../../research/WOTG_RAID_BENCHMARK.md) distinguishes verified source observations from inaccessible/unverified video details.

Default action phrasing is **fast materialization/unsheathing → a short decelerated tension beat with micro-motion → explosive acceleration → connected recoil/aftermath**. Individual accents may be fast inside a longer ritual. Smooth does not mean uniformly slow; use named curves, a common fractional clock and continuous joints/emitter/trails, not one long SmoothStep or unrelated modulo loops. [The development Skill's presentation reference](../../../.agents/skills/develop-convergence-raids/references/presentation-direction.md) owns the reusable implementation workflow.

For visual-only work, preserve server forecast/live/end ticks and damage volumes. Do not shorten a gameplay warning to improve a pose. Coordinate flash, sound and local shake with the same event, once.

## Field, foundation and Ready

New plinth and legacy Core tiles share a ground-aligned crop/pivot. The foundation uses its solid bottom course with a tiny sampling overlap, not the transparent canvas bottom. Placement preview and deployed art must agree without changing saved tiles, footprint or collision.

**Four suspension columns: two per side.** Outer columns are taller/thicker; inner columns shorter/slimmer. Each is one tight-cropped post seated in its base, not repeated capped segments with transparent seams. Horizontal gantries now carry hoists; fine cables suspend the doll coffin and then her unevenly loaded shoulder/wrists. The floating central iris is removed. Solid diagonal struts and the superseded two-column-only arrangement are rejected. Shared lift coordinates keep cable origins on the moving gantries; only interior cable vibration is decorative.

Preparation first deploys the full field/black exterior with a brief HUD-free cinematic and minimal progress hairline. It then shows one compact READY/unready control with a state indicator and ready/total count, plus a small world-anchored `Ready!` per accepted player. The all-Ready hold leads to the separate Boss/Raid introduction while the field remains deployed. [Arena infrastructure](../../ARENA_INFRASTRUCTURE.md#deployment-and-presentation--0241) owns timing and behavior; [audio](../../AUDIO_CUE_SHEET.md) owns preparation silence.

[FoundationCoreVisuals](../../../Client/Encounters/FirstSeverance/FoundationCoreVisuals.cs) owns the ground/suspension rig; [PreparationVisuals](../../../Client/Encounters/FirstSeverance/FirstSeverancePreparationVisuals.cs) owns its preparing presentation.

## Boss forms and endings

| Accepted state | Presentation |
|---|---|
| Before / Preparation | Completed coffin already at center; the plinth NPC disassembles, briefly hangs in pieces, then accelerates into that center before Ready. No change to admission or deployment duration. |
| Phase I / Sealed | Porcelain coffin hides almost all of the body; only a few fingertips/hair escape the rim, no face overlaid in front. Shielded/exposed remain distinguishable; Pylon winch cables sustain the shield. |
| I → II | Eclosion: hands pierce/pry seams, casing catches then peels on connected hinges, head/torso slides out, folded limbs unfurl. Preserve depth passes and attached joints; no radial tile explosion or instantaneous sprite swap. |
| Phase II / Unbound | Retained porcelain ball-jointed arms and slant; old crown/body restraint silhouette around a small, partly veiled face. Not an enlarged NPC portrait/costume. Every field-origin attack still gets a corresponding Boss load/recoil. |
| II → III / Distant | Body retreats in depth; two remote arms manifest at the field sides. The real foreground Core aperture remains the single NPC target. Background body/arms are not extra hitboxes. |
| Final | Keep the same doll identity in Distant's form; dress/limbs fragment first and her face remains coherent longer. Progress follows accepted score, not local elapsed time or invented HP. |
| Victory | After accepted terminal: an oblique narrow dimensional slit appears; fragments arrest, then accelerate inward as the form disintegrates, the slit contracts, and a strong bounded flash/shake/sound extinguishes it. |
| Defeat | Distinct HUD-free failure designation/pressure collapse; no false victory on cancel/invalidation. |

Intro, phase changes and both results temporarily replace the local participant's HUD. Conditional camera focus returns smoothly; no persistent input lock, global `hideUI`, zoom/time/weather mutation or retained gameplay actor. Gameplay death, reward and cleanup happen immediately on the accepted terminal; the client keeps only disposable ending art. A new Fight/world unload cancels stale tails.

Native selected boss-bar style is retained, including vanilla. The Boss registers a compact new doll portrait rather than drawing a competing custom bar.

## Beams, swords and forecast readability

- Preserve accepted narrow Prism/lattice plasma: full real corridor, pearl-hot spine, saturated flowing filaments and dim harmless residue. All-three Final previews use that same lattice material; [encounter timing](ENCOUNTER_SPEC.md#random-final-triples) is authoritative.
- Broad floods/salvos use tightly layered laminar light and wavering filaments **inside a readable continuous footprint**. No flat opaque slabs, fake decorative safe gaps or glow spilling into the surviving strip. Future width is readable before the actual live interval.
- Boss-origin Core salvos have an inward-loading chest lens, graduated filament neck and connected throat; no square-cut root or detached muzzle. Use a persistent flow phase plus bounded release offset, not elapsed time multiplied by a changing speed that jumps on firing.
- Phase-I Stillness builds/fires dense narrow teeth from each side footprint's center outward. Each tooth gets its complete forecast; only live teeth brighten as dangerous. Never illuminate the whole slab early.
- Twin blades use rapid rigid unsheathing, a full wavering danger aura and connected two-turn acceleration. No thick paired side rails.
- Iron Interdict tips appear rapidly outside top/bottom slits, brake/withdraw slightly, reach the entry plane continuously and accelerate through it. Clip at that plane instead of stretching/replacing the blade. Forecast the whole dense/sparse arrangement and preserve genuine gap boundaries.
- Central crush uses a translucent inward-loading pressure membrane and bowed stress filaments between bracing hands, then compression light. No boxed warning or repeated arrow stamps.
- **No prediction chevrons, SAFE/Gather words, Stack ordinal/count stamps or heavy forecast-edge rails.** Stack's inward gathering guidance is intentional, not permission to restore arrow patterns on beams.

Shared geometry and accepted epochs drive both authority collision and live footprint; fractional time animates the surface only. [BeamMaterial](../../../Client/Encounters/FirstSeverance/FirstSeveranceBeamMaterial.cs), [EmissionVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceEmissionVisuals.cs), [ScoreVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceScoreVisuals.cs) and [ImpalingSwordVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceImpalingSwordVisuals.cs) own those paths.

## Stack and Spread verdicts

**Stack:** one true acceptance circle with inward guidance; no outer false circle. Around each standing player, large faceted shell splinters appear in irregular hard beats. Their independent stick/slip vibration and branching cold-white arcs build friction without flat lightning sheets hiding the texture or player. On failure they rapidly contract toward the authority-sampled recipients, fracture and scatter; on success they lose tension without contracting and tumble away. Use accepted high-resolution shell art and bounded disposable masks.

**Spread:** on every accepted verdict, a brief large tapered **white cross flashes at the Boss's mouth**, with the same sharp high “ping” launch cue for success and failure. A thin immediate ruby ray targets each server-sampled recipient. Failure reaches/impacts the player; success stops short and dissolves into a conspicuous transverse pearl/rose plume with curling trails/glass sparks. Mixed verdicts are per recipient, never inferred from remote interpolated positions. Do not restrict launch effects to failure or create additional damage from the animation.

[MechanicVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceMechanicVisuals.cs) owns these accepted-result effects; [Audio Cue Sheet](../../AUDIO_CUE_SHEET.md) owns current timbres, not older visual-pass audio descriptions. Same-tick lethal results may finish cosmetically without reviving combat.

## Coordinate contract and regression prevention

The exterior black mask is world-attached foreground, **not scaled HUD**. Capture all corners from the actual world camera/matrix and physical `GraphicsDevice.Viewport` during world drawing; composite before HUD with `InterfaceScaleType.None`. Recompute each world frame, including camera/zoom/gravity changes, and clear on unload. Use accepted preparation or combat membership, not distance to the Boss.

Intro, phase-transition letterboxes, Victory/Defeat and the Ready button also compose in physical pixels. Do not divide UI-callback `Main.screenWidth/Height` by `UIScale`: tML's UI dispatch already changes those dimensions, which caused the observed 107% double-scaling bug. Changing the layer to `Game` alone is not equivalent to the full world transform. Mouse coordinates must match the Ready composition; head labels use world coordinates.

[FieldMaskLayout](../../../Client/Encounters/FirstSeverance/FirstSeveranceFieldMaskLayout.cs) owns pure projection/partition math; [PrototypePresentation](../../../Client/Encounters/FirstSeverance/FirstSeverancePrototypePresentation.cs) owns the world capture/interface composition. Tests cannot replace a peer's actual 107% UI/zoom observation.

## Scene, accessibility and ownership

The existing cathedral is a depth anchor, not a static full-screen replacement: independent parallax/breathing, moving mist layers, uneven abandoned puppet strings and phase tension sit behind terrain/players/forecasts. Scenery must not overpower actionable cues or mutate the world's biome/weather/time.

Reduced Effects lowers decorative density, displacement, exposure and sound layering where appropriate **without hiding danger**; shake-off is independent. Use isolated impact peaks, not repetitive full-screen strobing. Preserve the player's silhouette and readable negative space.

Resources/voice/fragment counts stay bounded; reuse authored atlases/procedural resources, with no per-frame texture allocation or Dedicated Server graphics/audio initialization. All client state is disposable and Fight/projectile-owned. Compilation and offline frames do not establish in-game smoothness, FPS or subjective quality.

[Weapons](WEAPONS.md) owns claw and non-melee macro-animation; do not restore their historical sword/short-cycle descriptions from archived Raid notes.
