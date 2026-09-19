# Convergence presentation direction

Read for animation, VFX, texture integration, animated UI or scene work across all content. [Art Direction](../../../../docs/ART_DIRECTION.md#luminance-presentation-policy) owns the shared Luminance quality policy and completion criteria. This reference selects implementation paths; feature specs retain appearance and tuning.

| Affected feature | Read only the relevant source of visual decisions |
|---|---|
| Requiem of the Hollow Doll / `FirstSeverance` | [Doll Theater](../../../../docs/encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) for body/capture/suspension; [Visual Spec](../../../../docs/encounters/first-severance/VISUAL_SPEC.md) for markers and scenes; [Weapons](../../../../docs/encounters/first-severance/WEAPONS.md) for weapon timelines |
| Ghost Samurai / Oboro | [Luminance presentation target](../../../../docs/encounters/ghost-samurai/ENCOUNTER_SPEC.md#luminance-presentation-target) for the violet oni, connected dual-sword motion and evolving slash/ghost materials; retain that feature's geometry and identity |
| Scarlet Invocation | [Luminance presentation v2](../../../../docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md#luminance-presentation-v2) for masked bodies, physical strokes, flexible appendages and score-linked material states |
| Another feature | Its owning specification and accepted source art |

## Select the implementation for the visible change

Trace the affected renderer's accepted clock, root/part/emitter anchors, geometry/material pass and resource owner. Choose the useful Luminance mechanism from the policy's [capability table](../../../../docs/ART_DIRECTION.md#choose-capabilities-by-visible-result), then inspect the nearest existing implementation. Reuse proven helpers where suitable; sharing techniques does not require sharing another Boss's colors, silhouettes or attack timing.

| Implementation question | Inspect only as relevant |
|---|---|
| Managed materials and existing world-space command buffers | [Doll Raid VFX](../../../../Client/Encounters/FirstSeverance/FirstSeveranceRaidVfx.cs) and its [pinned API evidence](../../../../docs/encounters/first-severance/VISUAL_SPEC.md#luminance-raid-presentation) |
| Primitive ribbons and shader state restoration | [Scarlet materials](../../../../Client/Encounters/CrimsonFoundry/ScarletMaterials.cs) |
| Easing, projected state, decorative Verlet or local cutscene/shake ownership | [Scarlet articulation](../../../../Client/Encounters/CrimsonFoundry/ScarletArticulation.cs) |
| Managed offscreen residue/Metaball lifecycle | [Scarlet atmosphere](../../../../Client/Encounters/CrimsonFoundry/ScarletAtmosphere.cs) |
| Ghost Samurai body revision | [Current atlas renderer](../../../../Client/Encounters/GhostSamurai/GhostSamuraiSpriteArt.cs) and [pose selection](../../../../Client/Encounters/GhostSamurai/SamuraiSpriteFrames.cs) are the migration baseline, not a finished articulation/material solution |

For Ghost Samurai motion work, preserve the accepted violet textures and attack clocks while replacing visible full-body pose jumps with connected part motion or authored in-betweens. Keep blade/hand/ghost-flame anchors together. A frame-count increase, crossfade or extra glow alone does not close the identified gap. For a narrow visibility/loading repair, preserve its scope and report that the broader motion target remains separate.

Verify unresolved API/coordinate/thread behavior against the pinned target and installed library. Use existing source evidence for unchanged questions; use the research skill only for an actual uncertainty. Follow the verification matrix's shader-export row when HLSL changes. Judge the rendered result under [presentation completion](../../../../docs/ART_DIRECTION.md#presentation-completion), with observed clips/sequences distinguished from unrun in-game checks.

## Motion with extreme contrast

- Prefer a sharply readable sequence: fast materialization/unsheathing, a short decelerated tension beat, explosive acceleration into release/contact, then a shaped recoil/aftermath. Use the feature's accepted rhythm; this is not an instruction to shorten every gameplay warning.
- Name each beat and give each its own curve. Ease-out suits fast arrival and braking; a brief nearly held pose retains micro-motion; accelerating closure suits a strike. Avoid one SmoothStep across the whole action, linear floating props, frozen frames, or a looping modulo reset at an impact boundary.
- Derive visible parts from a common fractional action clock. Keep position continuous across beat transitions; intentional impact velocity changes need a connected overshoot/recoil. Trails sample the same trajectory, not another slower animation. Join idle/attack/recovery parent joints without detached wrists or popping emitters.
- Drive sound, muzzle flash, local shake and impact from the same named event. Fire each accent once; suppress old beats on catch-up and pause. Add contrast before adding volume or repeated white flashes.

## Material and information hierarchy

- Preserve accepted original art unless replacement is requested. Reuse articulated atlas parts, UV regions, procedural masks and low-opacity moving layers; do not replace detailed surfaces with flat slabs, outlines or tiny inventory sprites stretched over the field.
- Follow the affected feature's materials and silhouette. Quiet surfaces can make brief energy peaks legible. Broad damage volumes need continuous coverage and readable negative space, not decorative gaps or ambiguous thin centerlines.
- Use the marker vocabulary and exclusions in that feature's active spec. Preserve accepted guidance; do not introduce labels or forecast ornaments merely because another encounter uses them.
- Foreground attacks remain more salient than scenery. Build background depth with independent drift, material flow, fog and phase tension behind terrain/players. Do not animate world time/weather or flood the combat view with opaque scenery.

## Implementation boundaries and useful checks

- State which clocks/geometry actually change. For a visual-only edit retain authority warning/live/end times and damage volumes. A harmless emerging prop stays outside the damage field or is clearly dim; it cannot mask a future safe slot. A requested faster damaging strike needs shared motion/collision updates and focused boundary checks.
- Prefer disposable client state keyed by the accepted Fight/projectile identity. Terminal art starts only from accepted Victory/Defeat and must not delay gameplay cleanup or rewards. New fights/world unload cancel old effects and sounds.
- Bound fragment/filament/layer counts, reuse texture resources, and release owned procedural surfaces. No per-frame texture allocation, gameplay RNG in drawing, server graphics initialization or unmanaged input/UI flags.
- Respect Reduced Effects and shake-off. Reduce displacement, density, exposure and sound layering without hiding danger. Dramatic extinction can use shaped pulses; avoid repetitive full-screen strobing.
- Verify modified curves/endpoints and the actual package once. Review source art and representative poses/frames when useful; clearly distinguish offline review from in-game readability, input behavior, multiplayer and FPS acceptance. Do not claim performance or smoothness from compilation.
