// Offline frames of the Sable Scythe's black blood (docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable Scythe"),
// through the production ScarletRewardInk path passes at actual size. Not a playtest, and the placeholder sprite is not
// drawn (the preview loads no vanilla textures): the white outline is the blade's three collision capsules, the grey line
// the haft, so the ink can be checked against the area that actually hurts.
//
//   pwsh tools/preview-scarlet.ps1 -Rewards -Only scythe
//
// scythe-measure: Over, Under, Over, Under, Whip (100 ticks) at a stationary reaper facing right: the swing wake (live
//   in the cut, cooling to residue), the Whip's crescent (live until 26, then its scar), glints at the aim crossings,
//   droplets and bone chips from a dummy, and the hanging staff gaining a line per connecting stroke (warm when full).
// scythe-reap: Staff Reap with five lines on a 96 px tall dummy: the middle line first and outward on sixteenths, each
//   head crossing 840 px in 4 ticks, live 10 ticks behind it, the Final Barline at 51, the whole staff drying together.
// These scenes mirror Client/Encounters/CrimsonFoundry/Rewards/ScytheVisuals.cs on the pure SableScytheMotion rules.
#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector = System.Numerics.Vector2;

internal static class ScythePreview
{
    internal static readonly int[] Starts = { 0, 18, 36, 54, 72 };
    internal static Vector2 X(NVector v) => new(v.X, v.Y);

    // The stroke playing at measure tick `tick`, and its age.
    internal static (int Stroke, float Age) At(float tick)
    {
        for (int i = Starts.Length - 1; i >= 0; i--)
            if (tick >= Starts[i]) return (i, Math.Min(tick - Starts[i], SableScytheMotion.Duration(i)));
        return (0, 0);
    }
    internal static SablePose Pose(float tick) { var (s, a) = At(tick); return SableScytheMotion.Pose(s, a); }
    internal static Vector2 Tip(Vector2 shoulder, float tick) => shoulder + X(SableScytheMotion.Tip(Pose(tick)));

    internal static void Line(SpriteBatch batch, Texture2D pixel, in ScarletView view, Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 pa = view.ToTarget(a), pb = view.ToTarget(b), d = pb - pa;
        if (d.LengthSquared() < 1e-4f) return;
        batch.Draw(pixel, pa, null, color, MathF.Atan2(d.Y, d.X), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }

    internal static void Box(SpriteBatch batch, Texture2D pixel, in ScarletView view, Vector2 min, Vector2 max, Color color)
    {
        Line(batch, pixel, view, min, new Vector2(max.X, min.Y), color, 1.5f);
        Line(batch, pixel, view, new Vector2(max.X, min.Y), max, color, 1.5f);
        Line(batch, pixel, view, max, new Vector2(min.X, max.Y), color, 1.5f);
        Line(batch, pixel, view, new Vector2(min.X, max.Y), min, color, 1.5f);
    }
}

internal sealed class ScytheMeasureScene : IRewardsPreviewScene
{
    public string Name => "scythe-measure";
    public int[] Ticks { get; } = { 2, 6, 8, 10, 12, 14, 16, 20, 24, 26, 28, 30, 44, 54, 62, 66, 80, 86, 88, 90, 92, 96, 99 };
    private static Vector2 Shoulder(Vector2 c) => c + new Vector2(-2, -6);
    // A dummy in front: every stroke connects, so the staff fills by the Whip.
    private static readonly Vector2 DummyMin = new(96, -60), DummyMax = new(150, 40);
    private static readonly int[] Engraved = { 7, 25, 43, 61, 87 };

    public void Emit(ScarletInkCanvas canvas, Vector2 center, int tick)
    {
        Vector2 shoulder = Shoulder(center);
        var (stroke, age) = ScythePreview.At(tick);
        int start = SableScytheMotion.LiveStart(stroke), end = SableScytheMotion.LiveEnd(stroke);
        // Wake: the hook tip over the last 9 ticks, hot in the live window for 3 ticks, then residue.
        const int perTick = 4;
        Span<Vector2> pts = stackalloc Vector2[9 * perTick + 1];
        Span<float> back = stackalloc float[9 * perTick + 1];
        Span<bool> hot = stackalloc bool[9 * perTick + 1];
        int n = 0;
        for (int i = 9 * perTick; i >= 0; i--)
        {
            float s = age - i / (float)perTick;
            if (s < Math.Max(0, start - 2) || s > end + 9) continue;
            pts[n] = shoulder + ScythePreview.X(SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, s)));
            back[n] = age - s; hot[n] = s >= start && s <= end && age - s <= 3; n++;
        }
        int split = n;
        while (split > 0 && hot[split - 1]) split--;
        if (split > 0 && canvas.Begin(new ScarletInkStyle(ScarletInkLook.Residue, true, 1.3f, .8f)))
        {
            for (int i = 0; i < Math.Min(n, split + 1); i++) { float taper = 1 - back[i] / 9; canvas.Point(pts[i], 14 * taper, taper); }
            canvas.End();
        }
        if (split < n && canvas.Begin(new ScarletInkStyle(ScarletInkLook.Live, true, 1.3f)))
        {
            for (int i = split; i < n; i++) canvas.Point(pts[i], 14 * (1 - back[i] / 9), back[i]);
            canvas.End();
        }
        // The Whip's crescent.
        float whip = tick - ScythePreview.Starts[4];
        if (whip > CrimsonRewardRules.WhipLiveStart && whip <= CrimsonRewardRules.WhipLiveEnd + CrimsonRewardRules.CrescentScar + 6)
        {
            bool live = whip <= CrimsonRewardRules.CrescentLiveEnd;
            if (canvas.Begin(new ScarletInkStyle(live ? ScarletInkLook.Live : ScarletInkLook.Residue, true, 2.1f)))
            {
                float fade = 1 - (whip - CrimsonRewardRules.CrescentLiveEnd) / CrimsonRewardRules.CrescentScar;
                for (int i = 0; i <= 24; i++)
                {
                    float written = CrimsonRewardRules.WhipLiveStart + i / 4f;
                    if (written > whip || written > CrimsonRewardRules.WhipLiveEnd) break;
                    canvas.Point(shoulder + ScythePreview.X(SableScytheMotion.Tip(SableScytheMotion.Pose(4, written))), CrimsonRewardRules.CrescentRadius,
                        live ? whip - written : fade);
                }
                canvas.End(bead: live && whip < CrimsonRewardRules.WhipLiveEnd);
            }
        }
        // Droplets from the dummy, the tick after each engraving.
        foreach (int e in Engraved)
        {
            float t = tick - e;
            if (t < 1.5f || t > 20) continue;
            for (int i = 0; i < 6; i++)
            {
                // From the hit along the swing, spread like the client's (a random turn of +-0.45 rad, speed 4.5-8).
                float spread = (ScarletRewardParticles.Hash(e * 7 + i, 3) - .5f) * .9f, speed = 4.5f + 3.5f * ScarletRewardParticles.Hash(e * 7 + i, 7);
                Vector2 along = ScythePreview.Tip(Shoulder(center), e) - ScythePreview.Tip(Shoulder(center), e - .25f);
                along = along.LengthSquared() > 1e-4f ? Vector2.Normalize(along) : Vector2.UnitX;
                Vector2 from = new Vector2(120, -10) + center;
                Vector2 v = new Vector2(along.X * MathF.Cos(spread) - along.Y * MathF.Sin(spread), along.X * MathF.Sin(spread) + along.Y * MathF.Cos(spread)) * speed
                    + new Vector2(0, -1.5f);
                Vector2 Drop(float u) => from + v * u + new Vector2(0, .28f * u * u);
                canvas.Droplet(new ScarletInkStyle(ScarletInkLook.Live, true, i, 1, false, 0, 0), Drop(t - 1.5f), Drop(t), 2.6f, t);
            }
        }
        // The hanging staff: a line per connecting stroke, written in with its ember-gold head; warm lips when full.
        int lines = 0; float newest = float.MaxValue;
        foreach (int e in Engraved) if (tick >= e) { lines++; newest = tick - e; }
        for (int i = 0; i < lines; i++)
        {
            Vector2 c = shoulder + ScythePreview.X(SableScytheMotion.HangingLine(i, 1));
            float reach = i == lines - 1 && newest < 6 ? Math.Clamp(newest / 6, .08f, 1) : 1;
            Vector2 from = c + new Vector2(-28, 0), to = Vector2.Lerp(from, c + new Vector2(28, 0), reach);
            canvas.Line(new ScarletInkStyle(ScarletInkLook.Dormant, true, i * 1.7f, 1, false, lines >= 5 ? 1 : 0), from, to, 2.5f, 2.5f, 0, 0, reach < 1);
        }
        // Another player's full staff, dimmed, for comparison.
        for (int i = 0; i < 5; i++)
        {
            Vector2 c = center + new Vector2(-330, -40) + ScythePreview.X(SableScytheMotion.HangingLine(i, 1));
            canvas.Line(new ScarletInkStyle(ScarletInkLook.Dormant, false, 4 + i, 1, false, 1), c - new Vector2(28, 0), c + new Vector2(28, 0), 2.5f, 2.5f, 0, 0);
        }
    }

    public void Particles(ScarletRewardParticles particles, Vector2 center, int tick, bool reduced)
    {
        Vector2 shoulder = Shoulder(center);
        // Glints at each crossing of the aim in a live window.
        var (stroke, age) = ScythePreview.At(tick);
        var (s0, a0) = ScythePreview.At(Math.Max(0, tick - 1));
        NVector now = SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, age)), before = SableScytheMotion.Tip(SableScytheMotion.Pose(s0, a0));
        if (s0 == stroke && SableScytheMotion.Live(stroke, (int)age) && MathF.Sign(now.Y) != MathF.Sign(before.Y) && now.X > 0)
        {
            particles.Spawn(ScarletParticleKind.Ember, 0, true, shoulder + ScythePreview.X(now), Vector2.Zero, 7, 18, reduced, tick);
            particles.Spawn(ScarletParticleKind.Ember, 0, true, shoulder + ScythePreview.X(now), Vector2.Zero, 5, 9, reduced, tick + .5f);
        }
        foreach (int e in Engraved)
            if (tick == e)
                particles.Spawn(ScarletParticleKind.BoneChip, 0, true, center + new Vector2(120, -10), new Vector2(2.5f, -2), 26, 2, reduced, tick);
    }

    public void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 center, int tick)
    {
        var batch = renderer.Batch;
        Vector2 shoulder = Shoulder(center);
        SablePose pose = ScythePreview.Pose(tick);
        Span<NVector> knots = stackalloc NVector[4];
        SableScytheMotion.Blade(pose, knots);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        ScythePreview.Box(batch, renderer.Pixel, view, center + DummyMin, center + DummyMax, new Color(120, 120, 140));
        // The reaper (a 20 x 42 stand-in) and the haft from the grip to the head.
        ScythePreview.Box(batch, renderer.Pixel, view, center - new Vector2(10, 21), center + new Vector2(10, 21), new Color(90, 90, 110));
        Vector2 grip = shoulder + ScythePreview.X(pose.Hand);
        ScythePreview.Line(batch, renderer.Pixel, view, grip, shoulder + ScythePreview.X(SableScytheMotion.Project(pose, new NVector(1.006f, -.947f))),
            new Color(150, 150, 160), 3);
        Color edge = new(255, 255, 255, 200);
        for (int i = 0; i + 1 < knots.Length; i++)
        {
            Vector2 a = shoulder + ScythePreview.X(knots[i]), b = shoulder + ScythePreview.X(knots[i + 1]);
            Vector2 d = b - a; if (d.LengthSquared() < 1e-4f) continue;
            Vector2 n = new Vector2(-d.Y, d.X); n.Normalize(); n *= SableScytheMotion.BladeRadius;
            ScythePreview.Line(batch, renderer.Pixel, view, a + n, b + n, edge, 1);
            ScythePreview.Line(batch, renderer.Pixel, view, a - n, b - n, edge, 1);
            ScythePreview.Line(batch, renderer.Pixel, view, a, b, new Color(255, 230, 200, 160), 1);
        }
        // The hook tip's path over the whole measure, faint.
        for (int t = 1; t <= 400; t++)
            ScythePreview.Line(batch, renderer.Pixel, view, ScythePreview.Tip(shoulder, (t - 1) * .2497f), ScythePreview.Tip(shoulder, t * .2497f), new Color(255, 255, 255) * .22f, 1);
        batch.End();
    }
}

internal sealed class ScytheReapScene : IRewardsPreviewScene
{
    public string Name => "scythe-reap";
    public int[] Ticks { get; } = { 16, 17, 18, 20, 23, 25, 28, 32, 38, 44, 46, 51, 52, 54, 58, 62, 66, 72, 80, 86 };
    private static readonly Vector2 Dummy = new(260, -40);
    private const float DummyHalfHeight = 48, DummyHalfWidth = 30;

    private static SableStaffPlacement Placement(Vector2 center)
    {
        Span<NVector> min = stackalloc NVector[1], max = stackalloc NVector[1];
        min[0] = new NVector(center.X + Dummy.X - DummyHalfWidth, center.Y + Dummy.Y - DummyHalfHeight);
        max[0] = new NVector(center.X + Dummy.X + DummyHalfWidth, center.Y + Dummy.Y + DummyHalfHeight);
        return SableScytheMotion.Place(new NVector(center.X, center.Y), new NVector(center.X + 300, center.Y - 20), min, max);
    }

    public void Emit(ScarletInkCanvas canvas, Vector2 center, int tick)
    {
        var staff = Placement(center);
        for (int k = 0; k < SableScytheMotion.Cuts(5); k++)
        {
            SableCutPlan cut = SableScytheMotion.Cut(k, staff, center.X);
            float t = tick - cut.Fire;
            if (t < 0 || t > SableScytheMotion.CutEnd(k, 5)) continue;
            Vector2 start = ScythePreview.X(cut.Start);
            if (cut.Index == CrimsonRewardRules.StaffLines)
            {
                bool live = SableScytheMotion.BarlineLive(t);
                float head = SableScytheMotion.BarlineHead(t), fade = 1 - (t - 15) / CrimsonRewardRules.StaffScar;
                for (int side = -1; side <= 1; side += 2)
                {
                    if (!canvas.Begin(new ScarletInkStyle(live ? ScarletInkLook.Live : ScarletInkLook.Residue, true, 5 + side))) return;
                    for (int i = 0; i <= 16; i++)
                    {
                        float f = head * i / 16;
                        canvas.Point(start + new Vector2(side * 14, cut.Length * f), CrimsonRewardRules.BarlineRadius,
                            live ? Math.Max(0, SableScytheMotion.BarlineIgnited(t, f)) : fade);
                    }
                    canvas.End(bead: live && head < 1);
                }
                continue;
            }
            float lineHead = SableScytheMotion.LineHead(t), tail = SableScytheMotion.LineTail(t);
            if (tail > 0 && canvas.Begin(new ScarletInkStyle(ScarletInkLook.Residue, true, k * 1.1f)))
            {
                int samples = (int)(840 * tail / 10) + 2;
                for (int i = 0; i < samples; i++)
                {
                    float f = tail * i / (samples - 1);
                    canvas.Point(start + new Vector2(cut.Length * f, 0), CrimsonRewardRules.StaffRadius, SableScytheMotion.LineScarFade(t, f, k, 5));
                }
                canvas.End();
            }
            if (lineHead > tail && SableScytheMotion.LineLive(t) && canvas.Begin(new ScarletInkStyle(ScarletInkLook.Live, true, k * 1.1f)))
            {
                int samples = (int)(840 * (lineHead - tail) / 10) + 2;
                for (int i = 0; i < samples; i++)
                {
                    float f = tail + (lineHead - tail) * i / (samples - 1);
                    canvas.Point(start + new Vector2(cut.Length * f, 0), CrimsonRewardRules.StaffRadius, Math.Max(0, SableScytheMotion.LineIgnited(t, f)));
                }
                canvas.End(bead: t < CrimsonRewardRules.StaffHeadTicks);
            }
        }
    }

    public void Particles(ScarletRewardParticles particles, Vector2 center, int tick, bool reduced)
    {
        // The whole staff flares as the barline lands.
        if (tick != CrimsonRewardRules.BarlineTick + CrimsonRewardRules.BarlineFall) return;
        var staff = Placement(center);
        for (int k = 0; k < 5; k++)
        {
            SableCutPlan cut = SableScytheMotion.Cut(k, staff, center.X);
            for (int i = 0; i < 10; i++)
                particles.Spawn(ScarletParticleKind.Ember, 0, true, ScythePreview.X(cut.Start) + new Vector2(cut.Length * (i + .5f) / 10, 0),
                    new Vector2((ScarletRewardParticles.Hash(k * 10 + i, 1) - .5f) * 1.2f, -.8f - ScarletRewardParticles.Hash(k * 10 + i, 2)), 22, 5, reduced, k * 10 + i);
        }
    }

    public void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 center, int tick)
    {
        var batch = renderer.Batch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        Vector2 d = center + Dummy;
        ScythePreview.Box(batch, renderer.Pixel, view, d - new Vector2(DummyHalfWidth, DummyHalfHeight), d + new Vector2(DummyHalfWidth, DummyHalfHeight), new Color(120, 120, 140));
        ScythePreview.Box(batch, renderer.Pixel, view, center - new Vector2(10, 21), center + new Vector2(10, 21), new Color(90, 90, 110));
        // The release pose's hook tip, as a short haft line to the tip.
        SablePose pose = SableScytheMotion.ReleasePose(Math.Min(tick, 56));
        Vector2 shoulder = center + new Vector2(-2, -6), aimDir = Vector2.Normalize(d - center);
        float aim = MathF.Atan2(aimDir.Y, aimDir.X);
        Vector2 grip = shoulder + ScythePreview.X(SableScytheMotion.ToWorld(pose.Hand, aim, 1));
        Vector2 tip = shoulder + ScythePreview.X(SableScytheMotion.ToWorld(SableScytheMotion.Tip(pose), aim, 1));
        ScythePreview.Line(batch, renderer.Pixel, view, grip, tip, new Color(220, 220, 230, 180), 2);
        batch.End();
    }
}
