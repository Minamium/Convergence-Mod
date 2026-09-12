# Convergence presentation direction

Read for animation, VFX, texture integration or scene work. This reference owns shared implementation choices, not appearance or a second tuning table.

| Affected feature | Read only the relevant source of visual decisions |
|---|---|
| 不幸な人形劇 / `FirstSeverance` | [Doll Theater](../../../../docs/encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) for body/capture/suspension; [Visual Spec](../../../../docs/encounters/first-severance/VISUAL_SPEC.md) for markers and scenes; [Weapons](../../../../docs/encounters/first-severance/WEAPONS.md) for weapon timelines |
| Ghost Samurai | [Ghost Samurai spec](../../../../docs/encounters/ghost-samurai/ENCOUNTER_SPEC.md) for skull/oni identity, attacks and rig; do not import Doll materials or Raid Stack/Spread rules |
| Another feature | Its owning specification and accepted source art |

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
