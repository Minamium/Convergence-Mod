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

- **Soft forecast veil + sparse glints → brief pre-shot dim → white-core torn jet → squeeze into a thin residual light and clear** is the grammar for avoidable **non-lattice** Doll beams. The user's selected Nameless V/H/V triplet is the visual priority; [F15](../../research/WOTG_RAID_BENCHMARK.md#f15--nameless-portal-triplet-2026-09-13) records actual consecutive frames. This deliberately supersedes the preceding blanket bans on colored forecasts and non-additive contrast. The forecast has a soft transverse gradient, faint axial thread and sparse footprint glints, not an opaque rectangle, paired rails or uniformly bright fill. It remains faintly visible during the short dip immediately before firing.
- Initial eight-cast, standalone-Spread pursuit, Final four-color score, the separate Core jet during lattice, continuous Stillness bands, twin rotation and top/bottom/horizontal jets opt into the original `PortalBeam` material with their accepted fire/end ages. Independent dark and bright noise streams form a continuous white spine, colored torn folds and near-black channels; nonzero alpha preserves contrast over bright terrain. Do not replace the sustained hazard with separated comet blobs. Red/blue/green/gold keep their assignment/order with violet depths and pearl cores. Untimed Stack/Spread verdict visuals, weapons and Oni remain unchanged.
- The sustained main eight-cast retains its predecessor emitter during the next forecast. Both bodies and post-damage closures stay visible; no snapshot handoff cancels a live beam. Stillness uses the same continuous broad-volume adapter as Phase III, with narrower footprints owned by the [encounter spec](ENCOUNTER_SPEC.md#attack-modules). The orbiting charge also displays the folded energy body without a solid orb hiding its nose; fractional head motion and harmless end contraction join the existing approach/lock/rush.
- The shader receives **currently growing geometry**, not the eventual full hitbox. Launch, brief hold and amplification remain shared with authority through [Beam ignition](ENCOUNTER_SPEC.md#beam-ignition-and-lattice-order); no timing or hit-test change accompanies this material revision. **Contraction starts only after the accepted damaging end**, never while a full-width hit is possible. A dim outer carrier marks the live extent; dark interior folds are not safe gaps. The harmless end contracts rapidly to a thin line and fades instead of disappearing at full width.
- The single Phase-II Core jet has one soft forecast corridor and a connected luminous slit/throat. The rotating sphere's crater remains: inward displacement, raised lip, concave wall normals, dark depth, pressure hold and resealing. No floating muzzle, circular seal, square-cut root or parallel forecast subdivision. A persistent cast-age prevents phase jumps at release.
- **Lattice is the explicit exception:** its accepted thin axis/sparse-glint forecast, stagger, speed and finite **bright head → flowing body → long tail** remain unchanged in `RaidEnergy` / `RibbonPass`. The geometry and material coordinates stay attached to the original moving packet, including sanctuary clipping. It does not enter the portal-jet branch. [Encounter timing](ENCOUNTER_SPEC.md#beam-ignition-and-lattice-order) owns durations. Phase-I Stillness retains center-out teeth. Confined lattice/teeth/flood bands suppress outside corona so surviving gaps remain legible.
- Twin rotation braces on the soft forecast, emits purple jets and makes the existing two accelerating turns. The legacy `HalfField` top/bottom jets keep dense/sparse halves and shifted second wave. Horizontal floods retain their moving safe strip and embedded Stack/Spread holds. All three use the unchanged shared launch/amplification geometry; there are no sword sprites or metallic release sounds.
- Final bullets retain purple nuclei, pearly inner light, irregular flowing skin and bounded wakes; their radius/motion are unchanged. Central crush keeps inward-loading pressure between bracing hands, then compression light. Neither is a new beam or a circular HUD ornament.
- **No prediction chevrons, SAFE/Gather words, Stack ordinal/count stamps or heavy forecast-edge rails.** Stack's inward gathering guidance is intentional, not permission to restore arrow patterns on beams.

[BeamMaterial](../../../Client/Encounters/FirstSeverance/FirstSeveranceBeamMaterial.cs), [EmissionVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceEmissionVisuals.cs), [StageVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceStageVisuals.cs), [ScoreVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceScoreVisuals.cs) and [ImpalingSwordVisuals](../../../Client/Encounters/FirstSeverance/FirstSeveranceImpalingSwordVisuals.cs) adapt accepted clocks/geometry. [RaidVfx](../../../Client/Encounters/FirstSeverance/FirstSeveranceRaidVfx.cs) routes timed portal jets separately from unchanged lattice/verdict commands. F13/F14 remain historical ignition/flow findings; F15 owns the new triplet direction.

## Luminance Raid presentation

The eight-cast-only prototype boundary is superseded by the user's explicit Raid-wide visual refresh. [RaidVfx](../../../Client/Encounters/FirstSeverance/FirstSeveranceRaidVfx.cs) owns a frame-local, bounded command buffer and six reused vertices. Named passes separate forecast, live body, finite ribbon, corona, pressure mouth, orb, wake, compression, dimensional rift and flash. Existing per-Fight emitters/score and accepted result events still own clocks/cancellation; the renderer owns no gameplay actors, targets, hit tests or retained Fight particles. Batch overflow flushes rather than dropping danger geometry. Every frame, world unload and Mod unload discard commands. Restore texture/sampler slots 1–3 and graphics state; resume the existing world-space batch. No new full-screen RenderTarget/filter infrastructure.

**Material:** `PortalBeam` provides separate forecast, live, corona and source passes; `RaidEnergy` remains byte-identical for lattice and other accepted effects. Non-lattice jets deliberately have a high-contrast white core and dark folds rather than additive-only transparent fog; they retain a soft outer carrier and contract only after damage ends. Lattice retains its travelling packet and tapered tail. Source pressure and short glare connect to the jet. Confined bands have no outside corona. Reduced Effects lowers sparse-glint density and peripheral intensity without changing future extent or live geometry. Neither shader introduces screen/UI scaling or draw-time random target selection.

**Scene:** [GrandStage](../../../Client/Encounters/FirstSeverance/FirstSeveranceGrandStage.cs) samples accepted intro/transition/Final/terminal clocks. Pressure is explicitly drawn behind the authored shell/body. Ready deployment catches the hanging load without capturing the NPC early. Intro adds intake streams and arrival exposure; eclosion adds a pressure slit, braked hold and release; Final increases fragment stress. Victory holds the breaking silhouette, accelerates fragments into one oblique noisy dark slit, then snaps closed in a bounded flare matching existing ending audio. Defeat instead closes the stage around the surviving body. The result screen drops machine-status brackets; its physical-viewport coordinate contract is unchanged. Background cathedral lighting, moving occlusion and mist respond behind terrain, never on top of player markers.

**Research and limits:** [F12 — extreme beams, orbs and staged endings](../../research/WOTG_RAID_BENCHMARK.md#f12--極太赤ビームエネルギー弾終幕の連動2026-09-13追補) owns pinned WoTM/WotG observations, adopted/rejected techniques and rights. No third-party code, shader or visual assets were copied. Material previews verify only actual compiled GPU passes; quality parity, overlap/readability, multiplayer UI scaling and frame-time are not established until in-game observation. Preserve existing shell/rig/capture art and mechanics while reviewing this experimental full-Raid presentation.

API evidence, accessed 2026-09-13: official [Luminance](https://github.com/LucilleKarma/Luminance) at `b2468dfd2f299597602dc6826af781d436c29a57`, declared version 1.0.14; installed Workshop binary is also 1.0.14, but exact source/binary identity is unproven (GitHub `release` is still 1.0.12). [ShaderManager](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/Core/Graphics/Shaders/ShaderManager.cs) loads `.fxc` from `AutoloadedEffects/Shaders` under `ModName.FileName`; [ManagedShader](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/Core/Graphics/Shaders/ManagedShader.cs) exposes parameters, texture/sampler binding and named-pass `Apply`, with server guards. [MiscTexturesRegistry](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/Assets/MiscTexturesRegistry.cs) supplies `WavyBlotchNoise`, `TurbulentNoise` and `DendriticNoiseZoomedOut` by runtime reference; no dependency assets are copied into Convergence. Library license: [MIT](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/LICENSE). Drawing uses Convergence's world matrix/full half-width convention instead of Luminance's trail-specific projection/UV convention. Runtime screen scaling, peer rendering and performance remain user-owned acceptance, not inferred from compilation.

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
