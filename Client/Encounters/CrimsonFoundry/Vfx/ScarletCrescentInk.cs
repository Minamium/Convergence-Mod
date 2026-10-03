#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using NVector = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// The Sable Scythe crescent's look on the reward canvas (docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable
// Scythe", Presentation): the live body with the ember-gold bead on its apex, the break's spatter stroke and the scar
// the body leaves, plus where its ember wake and drips leave it. Terraria-free on the Vfx seam, so the offline preview
// draws exactly what ScytheInk draws in the game. The body's samples are SableCrescentFlight.Body's, the capsules that
// collide; the shader opens each point over 3 ticks from its time, as the collision opens it (draw equals collide).
// Nothing trails a crescent as ink: a residue smear read as a hairline (dark on bright ground, a pair of rim lines on
// dark ground), so its wake is sparse embers left across the arc's back, which never join into a line.
internal static class ScarletCrescentInk
{
    // Wake: one ember a tick left at a hashed point across the arc's back edge, nearly still, life 12, 3-5 px.
    internal const int WakeLife = 12;
    internal const float WakeSizeMin = 3, WakeSizeMax = 5, WakeBack = 3, WakeDrift = .5f;
    // Drips: a fine droplet leaves a tip of the arc with a little of the crescent's speed and falls for 14 ticks; a hit
    // sprays fuller droplets that fly for 20 (ScytheInk droplets).
    internal const float DripCarry = .06f, DripPush = .5f, DripDrop = .3f, DripRadius = 1.7f, HitDropRadius = 2.2f;
    internal const int DripLife = 14, HitDropLife = 20;
    // Spatter: one residue curve 28 px along the travel beside the break, radius 6 to 1.
    internal const float SpatterLength = 28, SpatterFrom = 6, SpatterTo = 1, SpatterSide = .55f, SpatterBend = 5;

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

    // The spatter stroke beside a break at `apex` (side +1 or -1 across the heading), drying at `fade`, unless Reduced
    // Effects removes it.
    internal static void Spatter(ScarletInkCanvas canvas, in ScarletInkStyle style, Vector2 apex, Vector2 heading, int kind, float side, float fade)
    {
        if (canvas.Reduced || fade <= 0) return;
        Vector2 normal = new(-heading.Y, heading.X);
        Vector2 a = apex + normal * (side * SableCrescentFlight.Width(kind) * .5f * SpatterSide);
        Vector2 b = a + heading * SpatterLength, c = (a + b) * .5f + normal * (side * SpatterBend);
        canvas.Quadratic(style with { Look = ScarletInkLook.Residue }, a, c, b, SpatterFrom, SpatterTo, fade, fade);
    }

    // The scar the body leaves where it ended: the body's arc as residue, drying from `fade` 1 to 0 over 20 ticks.
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

    // A crescent drawn at a fraction of its tick: its centre and age one tick back at fraction 0, now at fraction 1 (the
    // span it just flew, as a stroke draws the span it just swept).
    internal static Vector2 DrawCenter(Vector2 center, Vector2 velocityPerTick, float fraction) => center - velocityPerTick * (1 - fraction);
    internal static float DrawAge(float age, float fraction) => MathF.Max(0, age - (1 - fraction));
}
