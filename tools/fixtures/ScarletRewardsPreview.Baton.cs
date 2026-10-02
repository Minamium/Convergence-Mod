// Offline frames of the Scarlet Baton's black blood (docs/encounters/crimson-foundry/REWARDS.md, "Magic - Scarlet
// Baton"): a full score conducted stroke by stroke (each written behind its ember-gold bead, then dormant, the full
// score warming), the Tutti igniting them in writing order a sixteenth apart, and the Black-Blood River through all
// eight. Another player's two dormant strokes sit dimmed at the side. The spans come from BatonRules exactly as the
// game's BatonInkSystem emits them and the projectiles collide with them. Not a playtest.
//
//   pwsh tools/preview-scarlet.ps1 -Rewards -Only baton
#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using NVector = System.Numerics.Vector2;

internal sealed class BatonRewardsScene : IRewardsPreviewScene
{
    private const int Strokes = 8, Gesture = CrimsonRewardRules.GestureTicks, Cast = Strokes * Gesture + 6; // 150
    public string Name => "baton";
    public int[] Ticks { get; } =
    {
        4, 7, 9, 12, 15, 30, 60, 100, 144, Cast + 4, Cast + 9, Cast + 12, Cast + 22, Cast + 40, Cast + 60,
        Cast + 72, Cast + 80, Cast + 88, Cast + 96, Cast + 108, Cast + 118, Cast + 130,
    };

    private readonly BatonCurve[] curves = new BatonCurve[Strokes];
    private readonly NVector[][] points = new NVector[Strokes][];
    private readonly float[][] along = new float[Strokes][];
    private readonly int[] counts = new int[Strokes];
    private readonly BatonCurve[] remote = new BatonCurve[2];
    private readonly NVector[] riverPoints = new NVector[CrimsonRewardRules.RiverMaxVertices];
    private readonly float[] riverAlong = new float[CrimsonRewardRules.RiverMaxVertices];
    private readonly bool[] riverBreaks = new bool[CrimsonRewardRules.RiverMaxVertices];
    private int riverCount;
    private float riverLength;
    private Vector2 builtFor = new(float.NaN);

    // The score around the scene's anchor: two bars of 4/4 written across the field, each stroke steered by a different
    // sweep (still, across, along), so every clean mark, bend and lean shows.
    private void Build(Vector2 c)
    {
        if (builtFor == c) return;
        builtFor = c;
        NVector[] sweeps = { new(0, 0), new(60, 10), new(-30, -70), new(10, 60), new(-50, 30), new(0, 0), new(80, -20), new(-20, -40) };
        for (int k = 0; k < Strokes; k++)
        {
            int gesture = k % 4;
            var steering = BatonRules.Steer(gesture, 1, sweeps[k]);
            var center = new NVector(c.X - 330 + 190 * (k % 4), c.Y - 170 + 230 * (k / 4) + (gesture == BatonRules.Up ? -30 : gesture == BatonRules.Down ? 30 : 0));
            curves[k] = BatonRules.Curve(center, steering.Half, steering.Bend, steering.Skew);
            points[k] = new NVector[BatonRules.MaxStrokeSamples];
            along[k] = new float[BatonRules.MaxStrokeSamples];
            counts[k] = BatonRules.SampleStroke(curves[k], points[k], along[k]);
        }
        for (int k = 0; k < remote.Length; k++)
        {
            var steering = BatonRules.Steer(k + 1, -1, NVector.Zero);
            remote[k] = BatonRules.Curve(new NVector(c.X + 520, c.Y - 120 + 150 * k), steering.Half, steering.Bend, steering.Skew);
        }
        riverCount = BatonRules.BuildRiver(curves, riverPoints, riverAlong, riverBreaks, out riverLength);
    }

    public void Emit(ScarletInkCanvas canvas, Vector2 center, int tick)
    {
        Build(center);
        Span<BatonInkSpan> spans = stackalloc BatonInkSpan[2];
        int written = 0;
        for (int k = 0; k < Strokes; k++) if (tick >= k * Gesture) written++;
        float warmth = written >= Strokes ? 1 : 0;
        for (int k = 0; k < Strokes; k++)
        {
            int clock = tick - k * Gesture;
            if (clock < 0) continue;
            int n;
            float seed = k * 1.37f;
            if (tick < Cast) n = BatonRules.DormantSpans(clock, along[k][counts[k] - 1], warmth, spans);
            else
            {
                int ignite = Cast + CrimsonRewardRules.IgniteTick(k);
                if (tick < ignite) { spans[0] = BatonRules.ScheduledSpan(along[k][counts[k] - 1], tick - Cast, warmth); n = 1; }
                else
                {
                    float t = tick - ignite;
                    if (t >= BatonRules.StrokeEnd(k, true)) continue;
                    spans[0] = BatonRules.IgnitedSpan(t, along[k][counts[k] - 1], k, true);
                    n = 1;
                }
            }
            for (int i = 0; i < n; i++)
                EmitSpan(canvas, new ScarletInkStyle((ScarletInkLook)spans[i].Look, true, seed, spans[i].Opacity, false, spans[i].Warmth, 0),
                    points[k], along[k], ReadOnlySpan<bool>.Empty, counts[k], spans[i]);
            // The pen's droplets leave the gem (a stand-in point by the conductor) for the stroke's first ticks.
            if (tick < Cast && clock < CrimsonRewardRules.WriteTicks)
                for (int d = 0; d <= clock / 2; d++)
                {
                    float age = clock - 2 * d;
                    Vector2 gem = center + new Vector2(-560, 60), pen = Xna(points[k][0]);
                    Vector2 v = Vector2.Normalize(pen - gem) * 18 + new Vector2(0, -1.5f);
                    Vector2 at = gem + v * age + new Vector2(0, .11f * age * age);
                    canvas.Droplet(new ScarletInkStyle(ScarletInkLook.Live, true, k + d * .3f, .8f, false, 0, 0), at - v * .6f, at, 2.6f, age);
                }
        }
        // Another player's two dormant strokes: dimmed to half by the renderer.
        foreach (var curve in remote)
            canvas.Quadratic(new ScarletInkStyle(ScarletInkLook.Dormant, false, 4.2f, 1, false, 0, 1), Xna(curve.A), Xna(curve.K), Xna(curve.B),
                CrimsonRewardRules.WriteRadius, CrimsonRewardRules.WriteRadius, 0, 0);
        // The river at 8 + S(9) after the cast.
        int age0 = tick - Cast - CrimsonRewardRules.RiverTick;
        int m = BatonRules.RiverSpans(age0, riverLength, spans);
        for (int i = 0; i < m; i++)
            EmitSpan(canvas, new ScarletInkStyle((ScarletInkLook)spans[i].Look, true, 9.1f, spans[i].Opacity, false, 0, 0),
                riverPoints, riverAlong, riverBreaks, riverCount, spans[i]);
    }

    public void Particles(ScarletRewardParticles particles, Vector2 center, int tick, bool reduced)
    {
        Build(center);
        for (int k = 0; k < Strokes; k++)
            if (tick == Cast + CrimsonRewardRules.IgniteTick(k))
                for (int i = 0; i < 6; i++)
                {
                    float seed = k * 5.17f + i * 1.3f, h = ScarletRewardParticles.Hash(seed, 3), q = ScarletRewardParticles.Hash(seed, 4);
                    Vector2 at = Xna(points[k][Math.Clamp((int)((i + .5f) / 6 * counts[k]), 0, counts[k] - 1)]);
                    particles.Spawn(ScarletParticleKind.Ember, 0, true, at, new Vector2((h - .5f) * 2f, -1.2f - 1.6f * q), 18 + 10 * q, 5, reduced, seed);
                }
        int age = tick - Cast - CrimsonRewardRules.RiverTick;
        if (age is >= 0 and <= CrimsonRewardRules.RiverHeadTicks && riverCount > 1)
        {
            float head = riverLength * age / CrimsonRewardRules.RiverHeadTicks;
            Vector2 at = PointAt(head);
            float seed = age * 3.7f, h = ScarletRewardParticles.Hash(seed, 5), q = ScarletRewardParticles.Hash(seed, 6);
            particles.Spawn(ScarletParticleKind.Ember, 0, true, at, new Vector2((h - .5f) * 2.4f, -1 - 1.5f * q), 20 + 10 * q, 6, reduced, seed);
            if (age % 3 == 0) particles.Spawn(ScarletParticleKind.Smoke, 0, true, at, new Vector2(0, -.6f), 30, 14, reduced, seed + .5f);
        }
    }

    private Vector2 PointAt(float u)
    {
        for (int i = 1; i < riverCount; i++)
            if (riverAlong[i] >= u)
                return Xna(NVector.Lerp(riverPoints[i - 1], riverPoints[i], Math.Clamp((u - riverAlong[i - 1]) / Math.Max(1e-5f, riverAlong[i] - riverAlong[i - 1]), 0, 1)));
        return Xna(riverPoints[riverCount - 1]);
    }

    // The same clipping as BatonInkSystem.EmitSpan: one path per unbroken stretch, the bead on the span's end.
    private static void EmitSpan(ScarletInkCanvas canvas, in ScarletInkStyle style, ReadOnlySpan<NVector> pts, ReadOnlySpan<float> along,
        ReadOnlySpan<bool> breaks, int count, in BatonInkSpan span)
    {
        if (count < 2 || span.To <= span.From) return;
        bool open = false;
        for (int i = 1; i < count; i++)
        {
            if (!breaks.IsEmpty && breaks[i]) { if (open) { canvas.End(); open = false; } continue; }
            float u0 = along[i - 1], u1 = along[i];
            if (u1 < span.From) continue;
            if (u0 >= span.To) break;
            float a = Math.Max(u0, span.From), b = Math.Min(u1, span.To), length = u1 - u0;
            if (!open)
            {
                if (!canvas.Begin(style)) return;
                open = true;
                canvas.Point(At(pts, i, a, u0, length), span.Radius, span.TimeAt(a));
            }
            canvas.Point(At(pts, i, b, u0, length), span.Radius, span.TimeAt(b));
            if (b < u1) break;
        }
        if (open) canvas.End(span.Bead);
    }

    private static Vector2 At(ReadOnlySpan<NVector> pts, int i, float u, float u0, float length)
        => Xna(length > 1e-5f ? NVector.Lerp(pts[i - 1], pts[i], Math.Clamp((u - u0) / length, 0, 1)) : pts[i]);

    private static Vector2 Xna(NVector v) => new(v.X, v.Y);
}
