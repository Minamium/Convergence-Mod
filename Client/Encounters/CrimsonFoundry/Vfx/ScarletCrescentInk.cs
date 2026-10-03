#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using NVector = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// The Sable Scythe crescent's look on the reward canvas (docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable
// Scythe", Presentation): the live body with the ember-gold bead on its apex, the break's spatter blots and the scar
// the body leaves, its droplets, plus where its cinder wake and drips leave it. Terraria-free on the Vfx seam, so the
// offline preview draws exactly what ScytheInk draws in the game. The body's samples are SableCrescentFlight.Body's,
// the capsules that collide; the shader opens each point over 3 ticks from its time, as the collision opens it (draw
// equals collide). Nothing trails a crescent as ink: a residue smear read as a hairline (dark on bright ground, a pair
// of rim lines on dark ground), so its wake is sparse cinders left across the arc's back, which never join into a line.
// Nothing it leaves is a line either: drops keep a tail no longer than their radius and the break spatters blots.
internal static class ScarletCrescentInk
{
    // Wake: a cinder every 2 ticks left at a hashed point across the arc's back edge, nearly still, life 12, 3-5 px.
    // Cinders cover what lies behind them instead of adding light, so the wake stays crimson on bright ground.
    internal const int WakeEvery = 2, WakeLife = 12;
    internal const float WakeSizeMin = 3, WakeSizeMax = 5, WakeBack = 3, WakeDrift = .5f;
    // Drips: a fine droplet leaves a tip of the arc with a little of the crescent's speed and falls for 8 ticks; a hit
    // sprays fuller droplets that fly for 14 (ScytheInk droplets). Every drop falls at 0.28 px/tick^2 and draws as a
    // bead with a tail of at most one radius, so a falling drop never stretches into a streak.
    internal const float DripCarry = .06f, DripPush = .5f, DripDrop = .3f, DripRadius = 1.7f, HitDropRadius = 2.2f;
    internal const int DripLife = 8, HitDropLife = 14;
    internal const float DropGravity = .28f, DropLag = 1.5f, DropTail = 1;
    // Spatter: three dried blots thrown ahead of the break along its travel, on one side; Reduced Effects removes them.
    internal static readonly float[] SpatterAlong = { 10, 20, 29 }, SpatterAcross = { 5, 11, 7 }, SpatterRadius = { 7, 5, 4 };

    private static Vector2 X(NVector v) => new(v.X, v.Y);
    private static NVector N(Vector2 v) => new(v.X, v.Y);

    // The live body at `age` ticks since the throw (the ignition blaze is the material's first ticks), closing over its
    // `remaining` live ticks; the bead sits on the apex while it flies.
    internal static void Body(ScarletInkCanvas canvas, in ScarletInkStyle style, Vector2 center, Vector2 heading, int kind, float age,
        float remaining, bool bead)
    {
        Span<NVector> at = stackalloc NVector[CrimsonRewardRules.CrescentSamples];
        Span<float> radius = stackalloc float[CrimsonRewardRules.CrescentSamples];
        SableCrescentFlight.Body(N(center), N(heading), kind, 1, at, radius);
        if (!canvas.Begin(style with { Look = ScarletInkLook.Live, Remaining = remaining })) return;
        float time = MathF.Max(0, age);
        for (int i = 0; i < at.Length; i++) canvas.Point(X(at[i]), radius[i], time);
        canvas.End(bead, SableCrescentFlight.ApexSample);
    }

    // A wake ember: at `across` (0..1, hashed by the caller) along the arc's back edge, a few px behind it, drifting back.
    internal static Vector2 WakeFrom(Vector2 center, Vector2 heading, int kind, float across)
    {
        float s = Math.Clamp(across, 0, 1) * 2 - 1, h = SableCrescentFlight.Depth(kind), w = SableCrescentFlight.Width(kind);
        Vector2 normal = new(-heading.Y, heading.X);
        return center + heading * (h * (1 - s * s) - h * .5f - SableCrescentFlight.Radius(kind) * (1 - (1 - CrimsonRewardRules.CrescentTipRadius) * s * s) - WakeBack)
            + normal * (s * w * .5f);
    }
    internal static Vector2 WakeVelocity(Vector2 heading) => -heading * WakeDrift + new Vector2(0, -.15f);
    internal static float WakeSize(float hash) => MathHelper.Lerp(WakeSizeMin, WakeSizeMax, Math.Clamp(hash, 0, 1));

    // The spatter beside a break at `apex` (side +1 or -1 across the heading): three residue blots ahead of it, drying
    // at `fade`, unless Reduced Effects removes them.
    internal static void Spatter(ScarletInkCanvas canvas, in ScarletInkStyle style, Vector2 apex, Vector2 heading, int kind, float side, float fade)
    {
        if (canvas.Reduced || fade <= 0) return;
        Vector2 normal = new(-heading.Y, heading.X);
        var residue = style with { Look = ScarletInkLook.Residue };
        for (int i = 0; i < SpatterRadius.Length; i++)
            canvas.Disc(residue, apex + heading * SpatterAlong[i] + normal * (side * SpatterAcross[i]), SpatterRadius[i], fade);
    }

    // The scar the body leaves where it ended: the body's arc as residue, drying from `fade` 1 to 0 over its 10-tick scar.
    internal static void Scar(ScarletInkCanvas canvas, in ScarletInkStyle style, Vector2 center, Vector2 heading, int kind, float fade)
    {
        if (fade <= 0) return;
        Span<NVector> at = stackalloc NVector[CrimsonRewardRules.CrescentSamples];
        Span<float> radius = stackalloc float[CrimsonRewardRules.CrescentSamples];
        SableCrescentFlight.Body(N(center), N(heading), kind, 1, at, radius);
        if (!canvas.Begin(style with { Look = ScarletInkLook.Residue })) return;
        for (int i = 0; i < at.Length; i++) canvas.Point(X(at[i]), radius[i], fade);
        canvas.End();
    }

    // Where a drip leaves (the arc's tip on `side`, +1 or -1 across the heading) and how it starts to fall.
    internal static Vector2 DripFrom(Vector2 center, Vector2 heading, int kind, float side)
        => center - heading * (SableCrescentFlight.Depth(kind) * .5f) + new Vector2(-heading.Y, heading.X) * (side * SableCrescentFlight.Width(kind) * .5f);
    internal static Vector2 DripVelocity(Vector2 velocityPerTick, Vector2 heading, float side)
        => velocityPerTick * DripCarry + new Vector2(-heading.Y, heading.X) * (side * DripPush) + new Vector2(0, DripDrop);

    // A drop `t` ticks after it left `from` at `velocity`, under gravity.
    internal static Vector2 DropAt(Vector2 from, Vector2 velocity, float t) => from + velocity * t + new Vector2(0, DropGravity * t * t);

    // A drop of live black blood at `t` ticks of its `life` (none before 1.5 ticks or after its life): a bead whose tail
    // follows its path for at most one radius. False when it is not drawn.
    internal static bool Drop(ScarletInkCanvas canvas, in ScarletInkStyle style, Vector2 from, Vector2 velocity, float t, float radius, int life)
    {
        if (t < DropLag || t > life) return false;
        Vector2 head = DropAt(from, velocity, t), tail = DropAt(from, velocity, t - DropLag), along = head - tail;
        float length = along.Length(), most = radius * DropTail;
        if (length > most) tail = head - along * (most / length);
        return canvas.Droplet(style with { Look = ScarletInkLook.Live }, tail, head, radius, t);
    }

    // A crescent drawn at a fraction of its tick: its centre and age one tick back at fraction 0, now at fraction 1 (the
    // span it just flew, as a stroke draws the span it just swept).
    internal static Vector2 DrawCenter(Vector2 center, Vector2 velocityPerTick, float fraction) => center - velocityPerTick * (1 - fraction);
    internal static float DrawAge(float age, float fraction) => MathF.Max(0, age - (1 - fraction));
}
