---
doc_id: research.ghost-samurai-luminance-20260918
document_type: research
status: provisional
owners:
  - gameplay
  - art
last_reviewed: 2026-09-18
source_of_truth_for:
  - research.ghost_samurai_luminance_api
aliases:
  - Ghost Samurai articulated presentation
related_code:
  - Client/Encounters/GhostSamurai
  - Client/Graphics/WorldGraphicsScope.cs
related_docs:
  - encounter.ghost-samurai.spec
  - research.sources
  - project.status
---

# Ghost Samurai: Luminance capability survey and independent rig

The requested refresh replaces whole-actor pose-sheet switching with independently articulated parts and dependency-backed materials. Implementation status belongs to [STATUS](../STATUS.md); player-facing behavior belongs to the [encounter spec](../encounters/ghost-samurai/ENCOUNTER_SPEC.md#表示と素材).

## Source and installed-version boundary

Inspected official [Luminance repository](https://github.com/LucilleKarma/Luminance/tree/b2468dfd2f299597602dc6826af781d436c29a57), pinned at `b2468dfd2f299597602dc6826af781d436c29a57`: README, feature/type inventory, build manifest and relevant implementation paths below. Its manifest and installed dependency both report **1.0.14**. Matching version text does not establish byte-for-byte source/binary identity; the final native build and headless API probes use the **installed** package. The official source is MIT; it remains an external dependency, with no source mirror, extracted art or library binary committed.

Target remains installed tModLoader2026.07.3.0 / Terraria1.4.4.9 / Calamity2.2.4 / .NET8 / C#12. No dependency upgrade is part of this change.

## Capability map and selection

This is a survey of the public feature families and source declarations, followed by detailed reading of the selected APIs. It is not a claim to have executed every library feature.

| Public family / inspected source area | Decision for this refresh |
|---|---|
| `Common/Easings`: EasingCurves, EasingType, PiecewiseCurve, PiecewiseRotation | Use installed Cubic InOut/Out for blade preparation/release and a PiecewiseCurve for the brief hit response. Smooth interpolation and shortest-angle transitions remain independently authored. |
| `Common/VerletIntergration`: VerletSettings, VerletSegment, VerletSimulations | Use five four-node talisman tethers, with fixed roots, bounded displacement and no tile/water collision. |
| `Core/Graphics/Primitives`: PrimitiveRenderer, PrimitiveSettings, circle/ring and pixelation variants | Use continuous world-space blade-tip ribbons. Keep the accepted attack-volume renderer; cosmetic ribbons must not define damage or fill safe gaps. No additional pixelation pass is needed. |
| `Core/Graphics/Shaders`: ShaderManager, ManagedShader, managed filters/recompilation | Use three original materials for the assembled spirit parts, ribbon and smoke mask. Reuse shader loading, uniform and texture bindings. No full-screen filter obscures forecasts. |
| `Core/Graphics/Particles`: ParticleManager, Particle, ManualParticleRenderer, MetaballType/Manager/Instance | Use bounded metaball smoke, rendered manually behind the actor. Generic particle classes were evaluated but would duplicate the same smoke; wisp art remains an independent layer. |
| `Core/Graphics/RenderTargets`: ManagedRenderTarget / RenderTargetManager | Use them through MetaballType's owned mask target; do not allocate per-frame textures or dispose dependency-owned assets. |
| Atlas manager/AtlasTexture and asset registries | Reference Luminance's runtime noise/bloom assets. A small original nine-part atlas already provides stable UVs; registering it into another packing system is unnecessary. |
| ScreenShakeSystem, camera pan/zoom, input blocker | Use a small local-participant heavy-cut shake, bounded and cancelled on cleanup. Respect existing ScreenShake / ReducedEffects options. Camera/input takeover is not appropriate during this fight. |
| CutsceneManager / cutscene base | Inspected lifecycle, but no blocking cutscene: the harmless96-tick victory rig must not delay authoritative cleanup, drops or the next fight. |
| Looping sound manager / sound instances | Keep existing fight audio ownership and exact attack cues. No new looping sound was requested. |
| State machines / PushdownAutomata | Keep the existing Boss AI/attack state machine. Replacing gameplay control for an animation request would add unrelated risk. |
| Common utilities: angles, vectors, math, movement, collision, reflection, text/random helpers | Use angle/vector helpers where needed; no client collision or gameplay authority is introduced. |
| UI information icons, mod-call and hooking infrastructure; balancing APIs | Inventory only; unrelated to actor animation. Do not adopt deprecated balancing/hooking features or add UI/gameplay changes. |

Selected implementation paths are linked under the same pinned tree: [easings](https://github.com/LucilleKarma/Luminance/tree/b2468dfd2f299597602dc6826af781d436c29a57/Common/Easings), [Verlet](https://github.com/LucilleKarma/Luminance/tree/b2468dfd2f299597602dc6826af781d436c29a57/Common/VerletIntergration), [primitives](https://github.com/LucilleKarma/Luminance/tree/b2468dfd2f299597602dc6826af781d436c29a57/Core/Graphics/Primitives), [metaballs](https://github.com/LucilleKarma/Luminance/tree/b2468dfd2f299597602dc6826af781d436c29a57/Core/Graphics/Particles/Metaballs). The README owns the full upstream feature list.

## Observations affecting correctness

- MetaballManager prepares targets through `Main.OnPreDraw`; its particle update can therefore run at draw frequency. Convergence advances smoke position/size/lifetime only in its once-per-game-tick presentation update. `UpdateParticle` leaves velocity zero. A native probe compares one versus eight render callbacks per tick.
- PrimitiveRenderer consumes world positions and performs screen conversion internally. The ribbon is supplied bounded world-space tips; smoothing is disabled so a curve cannot overshoot into unrelated space. Duplicate tips, teleports and separate swings split/reset trails.
- ManagedShader binding changes graphics state. The existing Scarlet scope was extracted without behavior changes into `WorldGraphicsScope`: caller sort/blend/sampler/depth/raster/effect/matrix and GPU bindings are restored. The installed FNA private-field accessor probe is retained after extraction.
- Verlet roots follow the accepted actor, with20 nodes total, five solver iterations, finite checks, a40px root bound and reset on teleport. No tiles or player position drive these cosmetic chains.
- Tick histories are exact-Fight and actual-NPC owned. Rendering interpolates saved states without appending history. Only accepted Victory starts the ending; intercepted lethal hits, wipe and timeout do not. Teardown is idempotent even when Player/ModContent was never initialized.

## Calamity observation, not copied implementation

The existing [Murasama survey](2026-09-16-oboro.md#murasama-motion-reference--2026-09-17) is reused: three beats, stronger final emphasis and hand-relative presentation. Those two inspected Murasama files do not implement the easing/oldPos approach now used by this rig.

Also inspected official [Catastrophe.cs](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/CalClone/Catastrophe.cs) at `1a8cebd27ec5615316b78f71973446b5528d2b78`: angular following, charge-facing rotation, and decreasing-opacity body/glow echoes from prior positions. Its source manifest is2.2.2 while installed Calamity is2.2.4; these are design observations, not proof of identical installed behavior. Calamity's governing repository terms remain external; no implementation, sprite or recording is imported.

Convergence independently combines four motion envelopes, two articulated arms, recorded blade-tip ribbons, small cosmetic overrun and the approved purple oni identity. [Asset attribution](../../Assets/ATTRIBUTION.md) owns the generated parts and original HLSL provenance. Exact-package CPU checks and an offline layout preview do not prove live GPU rendering, multiplayer synchronization or FPS; those remain explicit owner checks in [evidence](../evidence/2026-09-18-ghost-samurai-rig.json).
