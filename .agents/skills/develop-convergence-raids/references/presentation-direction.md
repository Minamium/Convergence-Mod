# Convergence presentation direction

Read for animation, VFX, texture integration or scene work; skip for unrelated rules/network changes. [Visual Spec](../../../../docs/encounters/first-severance/VISUAL_SPEC.md) owns accepted appearance, and [Weapons](../../../../docs/encounters/first-severance/WEAPONS.md) owns weapon timelines. This reference owns implementation choices, not a second tuning table.

## Motion with extreme contrast

- Prefer a sharply readable sequence: fast materialization/unsheathing, a short decelerated tension beat, explosive acceleration into release/contact, then a shaped recoil/aftermath. Continuous does not mean uniformly slow. The user's claw example is a design direction, not an instruction to shorten every gameplay warning.
- Name each beat and give each its own curve. Ease-out suits fast arrival and braking; a brief nearly held pose retains micro-motion; accelerating closure suits a strike. Avoid one SmoothStep across the whole action, linear floating props, frozen frames, or a looping modulo reset at an impact boundary.
- Derive visible parts from a common fractional action clock. Keep position continuous across beat transitions; intentional impact velocity changes need a connected overshoot/recoil. Trails sample the same trajectory, not another slower animation. Join idle/attack/recovery parent joints without detached wrists or popping emitters.
- Drive sound, muzzle flash, local shake and impact from the same named event. Fire each accent once; suppress old beats on catch-up and pause. Add contrast before adding volume or repeated white flashes.

## Material and information hierarchy

- Preserve accepted original art unless replacement is requested. Reuse articulated atlas parts, UV regions, procedural masks and low-opacity moving layers; do not replace detailed surfaces with flat slabs, outlines or tiny inventory sprites stretched over the field.
- Keep the irregular, restrained metal/glass/cold-light world direction. Energy may be fluorescent and clear; quiet surfaces make its brief peaks legible. Broad damage volumes need continuous coverage and readable negative space, not decorative gaps or ambiguous thin centerlines.
- Retain the single valid Stack circle and existing guidance; no SAFE/Gather text, remaining-count stamps, prediction chevrons or heavy paired forecast rails. Consult current spec for exceptions rather than reviving historical paragraphs.
- Foreground attacks remain more salient than scenery. Build background depth with independent drift, material flow, fog and phase tension behind terrain/players. Do not animate world time/weather or flood the combat view with opaque scenery.

## Implementation boundaries and useful checks

- State which clocks/geometry actually change. For a visual-only edit retain authority warning/live/end times and damage volumes. A harmless emerging prop stays outside the damage field or is clearly dim; it cannot mask a future safe slot. A requested faster damaging strike needs shared motion/collision updates and focused boundary checks.
- Prefer disposable client state keyed by the accepted Fight/projectile identity. Terminal art starts only from accepted Victory/Defeat and must not delay gameplay cleanup or rewards. New fights/world unload cancel old effects and sounds.
- Bound fragment/filament/layer counts, reuse texture resources, and release owned procedural surfaces. No per-frame texture allocation, gameplay RNG in drawing, server graphics initialization or unmanaged input/UI flags.
- Respect Reduced Effects and shake-off. Reduce displacement, density, exposure and sound layering without hiding danger. Dramatic extinction can use shaped pulses; avoid repetitive full-screen strobing.
- Verify modified curves/endpoints and the actual package once. Review source art and representative poses/frames when useful; clearly distinguish offline review from in-game readability, input behavior, multiplayer and FPS acceptance. Do not claim performance or smoothness from compilation.
