---
doc_id: encounter.first-severance.visual
document_type: spec
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-13
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
| Idle / Preparation | Idle shows only the plinth. The Theater Doll key opens preparation: field, suspension and empty coffin deploy, and the NPC remains intact on the plinth throughout deployment/Ready. [Activation flow](../../ARENA_INFRASTRUCTURE.md#activation-flow) owns the item and authority rules. |
| All-Ready / SpawnIntro | Capture only after Ready: suspension tension, disassembly/hold, accelerating intake, arrival light and late name reveal. Shared accepted start/end clock; camera follows the NPC to the shell. See [Doll Theater](DOLL_THEATER_VISUAL_SPEC.md) for the expanded intro. |
| Phase I / Sealed | Restored microfractured metal shell hides almost all of the body; only a few fingertips/hair escape the rim, no face overlaid in front. Preparation/combat share the original material. Shielded/exposed remain distinguishable; Pylon winch cables sustain the shield. |
| I → II | Eclosion: hands pierce/pry seams, casing catches then peels on connected hinges, head/torso slides out, folded limbs unfurl. Preserve depth passes and attached joints; no radial tile explosion or instantaneous sprite swap. |
| Phase II / Unbound | Retained porcelain arms/slant and old crown/body silhouette around a small veiled face. A mechanical circular socket and continuously rotating polished sphere replace the organic chest slit; no square/X Core overlay. Every field-origin attack still gets a corresponding load/recoil. |
| II → III / Distant | Current doll retreats and remotely controls the retained ivory/tendon/clawed arms. Authored claw/socket frames supplement rig motion; porcelain body arms remain. A physical sphere at the real foreground Core replaces the old target stamp; background body/arms are not extra hitboxes. |
| Final | Keep the same doll identity in Distant's form; dress/limbs fragment first and her face remains coherent longer. Progress follows accepted score, not local elapsed time or invented HP. |
| Victory | After accepted terminal: an oblique narrow dimensional slit appears; fragments arrest, then accelerate inward as the form disintegrates, the slit contracts, and a strong bounded flash/shake/sound extinguishes it. |
| Defeat | Distinct HUD-free failure designation/pressure collapse; no false victory on cancel/invalidation. |

Intro, phase changes and both results temporarily replace the local participant's HUD. Conditional camera focus returns smoothly; no persistent input lock, global `hideUI`, zoom/time/weather mutation or retained gameplay actor. Gameplay death, reward and cleanup happen immediately on the accepted terminal; the client keeps only disposable ending art. A new Fight/world unload cancels stale tails.

Native selected boss-bar style is retained, including vanilla. The Boss registers a compact new doll portrait rather than drawing a competing custom bar.

## Beams, swords and forecast readability

- The main eight-cast PursuitPrism uses the original Luminance-managed `PursuitPrismFlow` shader: rapid pressure-slit appearance, braked tension, an immediate full-width live volume with a flowing pearl core and turbulent colored skin, then dim residue. **Original per-cast colors, authority geometry and warning/live/end ticks remain unchanged.** The separate Spread pursuit and Final four-color score still use the accepted Core/Lacuna ribbon jet; the fine lattice and weapons are unchanged. No circular casting seal, arrows or thick edge rails are introduced. [Encounter timing](ENCOUNTER_SPEC.md#four-color-final-prism-score) remains authoritative.
- Broad floods/salvos use tightly layered laminar light and wavering filaments **inside a readable continuous footprint**. No flat opaque slabs, fake decorative safe gaps or glow spilling into the surviving strip. Future width is readable before the actual live interval.
- The single Phase-II Core beam has **one continuous forecast footprint and one center spine**, not parallel lane subdivisions. Its accepted violet Lacuna jet remains. A crater-like hole opens **within the rotating metal sphere**: physical inward displacement, raised metal lip, concave wall normals and dark depth all share one continuous profile, followed by tension hold, pressure widening and resealing. The bright throat graduates into the beam; the full collision corridor remains forecast and faintly covered at the root. No floating muzzle, seal, square-cut bright root or changed damage window. Use a persistent flow phase plus bounded release offset, not elapsed time multiplied by a changing speed that jumps on firing.
- Phase-I Stillness builds/fires dense narrow teeth from each side footprint's center outward. Each tooth gets its complete forecast; only live teeth brighten as dangerous. Never illuminate the whole slab early.
- Twin rotation keeps the accepted full wavering forecast and two-turn acceleration, but releases **two purple Lacuna jets**, not metal blades. Full-bright live light begins at the authority fire tick; charge remains visibly dim. No thick paired side rails or obsolete sword sprites/sounds.
- The legacy Iron Interdict/`HalfField` keeps its accepted dense/sparse top/bottom aura forecast and shifted second wave. Slits gather pressure then emit **purple Lacuna jets on both sides**, advancing along the unchanged six-tick insertion curve; genuine gaps and the harmless withdrawal remain. There is no metallic tip, left/right color split or sword contact sound.
- Final bullets are saturated purple energy nuclei with pearly inner light, an irregular flowing skin and bounded wavering wakes. The bright compact body denotes the unchanged radius; broad translucent haze/tails are not extra hitboxes. Birth still assembles at the authority spawn position; no circular reticles or rectangular white darts.
- Central crush uses a translucent inward-loading pressure membrane and bowed stress filaments between bracing hands, then compression light. No boxed warning or repeated arrow stamps.
- **No prediction chevrons, SAFE/Gather words, Stack ordinal/count stamps or heavy forecast-edge rails.** Stack's inward gathering guidance is intentional, not permission to restore arrow patterns on beams.

Shared geometry and accepted epochs drive both authority collision and live footprint; fractional time animates the surface only. [BeamMaterial](../../../Client/Encounters/FirstSeverance/FirstSeveranceBeamMaterial.cs), [EmissionVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceEmissionVisuals.cs), [ScoreVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceScoreVisuals.cs) and [ImpalingSwordVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceImpalingSwordVisuals.cs) own those paths.

## Luminance prototype boundary

Only `FirstSeverancePursuitBeamVisuals` uses the dependency: three bounded world-space draws (diffuse bloom, forecast **or** live body, pressure mouth), reusing six vertices and `ManagedShader`. No generic rendering framework, full-screen filter, RenderTarget or new particle actors. Luminance owns shader/texture load and disposal; existing per-Fight emitters own timing/cancellation. No network/authority or sound changes. Fractional time stops below the next authority tick; the complete corridor lights on the accepted live tick, not after an ornamental traveling front. Named shader passes remain separate. Restore touched texture/sampler slots as well as blend/depth/raster state before the surrounding world batch resumes.

**Rejected material and replacement:** the first prototype added a near-uniform positive color across the entire corridor, leaving a flat rectangle with sinusoidal wires. Do not restore that recipe. The replacement separates a pale hot core, colored convection, faster torn surface detail and a much dimmer diffuse corona. Two differently advected, domain-warped texture scales provide longitudinal motion and nonperiodic variation; no uniform full-width radiance floor. The immutable body reaches the collision boundary with only a narrow feather. Outer bloom is soft decoration, not an additional danger band. At release the mouth briefly flares and relaxes into the same downstream flow; after authority retirement the entire material rapidly becomes dim residue. Reduced Effects lowers peripheral detail/glare, not warning footprint or authority timing.

**Comparable-source evidence (accessed 2026-09-13):** WoTM / [WrathOfTheMachines](https://github.com/LucilleKarma/WrathOfTheMachines) at `5556a3adcbabffc6fc95685e34f1ee22cee31d9a`, version 1.0.4, [MIT](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/LICENSE). This pass studies continuous beams, not the earlier small flying-laser example. Observed: [CannonLaserbeam](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Content/NPCs/ExoMechs/Projectiles/CannonLaserbeam.cs) separates bloom/body and varies the width; [BlazingExoLaserbeam](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Content/NPCs/ExoMechs/Projectiles/BlazingExoLaserbeam.cs) adds source flares and noise-driven material; [HadesSuperLaserbeam](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Content/NPCs/ExoMechs/Projectiles/HadesSuperLaserbeam.cs) combines source light, a separate broad bloom and [HadesExoEnergyBlastShader](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Assets/AutoloadedEffects/Shaders/Primitives/HadesExoEnergyBlastShader.fx)'s scrolling maps and transverse falloff. Independent adaptation: use those layer responsibilities, not their shader equations, art, growing hitboxes, lifetime, long beam expansion, extra projectiles or 3D-cylinder machinery. Convergence's short, immediately live locked corridor and original four colors take priority. Layering is a design inference for this attack, not proof of equivalent in-game quality or performance.

API evidence, accessed 2026-09-13: official [Luminance](https://github.com/LucilleKarma/Luminance) at `b2468dfd2f299597602dc6826af781d436c29a57`, declared version 1.0.14; installed Workshop binary is also 1.0.14, but exact source/binary identity is unproven (GitHub `release` is still 1.0.12). [ShaderManager](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/Core/Graphics/Shaders/ShaderManager.cs) loads `.fxc` from `AutoloadedEffects/Shaders` under `ModName.FileName`; [ManagedShader](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/Core/Graphics/Shaders/ManagedShader.cs) exposes parameters, texture/sampler binding and named-pass `Apply`, with server guards. [MiscTexturesRegistry](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/Assets/MiscTexturesRegistry.cs) supplies `WavyBlotchNoise` and `TurbulentNoise` by runtime reference; no dependency assets are copied into Convergence. Library license: [MIT](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/LICENSE). Drawing uses Convergence's world matrix/full half-width convention instead of Luminance's trail-specific projection/UV convention. Runtime screen scaling, peer rendering and performance remain user-owned acceptance, not inferred from compilation.

## Stack and Spread verdicts

**Raid circle policy:** only player Spread-area circles and the Stack acceptance circle may use circular UI/reticles. Boss casting (including pursuit volleys), Pylon pressure, transformation/capture, bullet charging and recovery cues use material fractures, directional light or existing status text; no circular HUD ornaments. Physical sphere/joint surfaces are not HUD circles. This does not remove the deliberately authored weapon/minion magic circles or alter Oni presentation.

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
