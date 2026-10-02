// Offline frames of the Ember Censer's pours (docs/encounters/crimson-foundry/REWARDS.md, "Summon - Ember Censer"):
// three censers over one target, the second and third phased into the largest gap by CenserRules.ChooseStart, their
// live columns (PathLivePass with the fire flag) drawn from the same CenserRules geometry the minion collides with,
// and each finished pour drying where it fell into its embered scar (PathResiduePass). The pendulum line and a stand-in
// bowl mark the bodies (the placeholder art is a vanilla texture the preview cannot load). Not a playtest.
//
//   pwsh tools/preview-scarlet.ps1 -Rewards -Only censer
#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class CenserRewardsScene : IRewardsPreviewScene
{
    public string Name => "censer";
    public int[] Ticks { get; } = { 40, 46, 48, 50, 53, 57, 61, 64, 67, 72, 84, 104, 146, 150, 153, 157, 162, 168, 172, 180 };

    private const float TargetWidth = 80, TargetHeight = 110, FloorBelow = 190;
    private static readonly int[] Arrivals = { 0, 20, 44 };
    private readonly int[] starts = new int[Arrivals.Length];

    public CenserRewardsScene()
    {
        // Each newcomer is phased against the clocks of the censers already swinging when it arrives.
        for (int i = 0; i < Arrivals.Length; i++)
        {
            var others = new int[i];
            for (int j = 0; j < i; j++) others[j] = CenserRules.After(starts[j], Arrivals[i] - Arrivals[j]);
            starts[i] = CenserRules.ChooseStart(others);
        }
    }

    private static Vector2 Xna(System.Numerics.Vector2 v) => new(v.X, v.Y);
    private static float Floor(Vector2 center) => center.Y + FloorBelow;
    private static Vector2 TargetTop(Vector2 center) => new(center.X, Floor(center) - TargetHeight);

    private Vector2 Ring(Vector2 center, int i)
        => TargetTop(center) + new Vector2(CrimsonRewardRules.CenserSpread(i, Arrivals.Length, TargetWidth), -CrimsonRewardRules.CenserStation);

    private bool Clock(int i, int tick, out int clock)
    {
        clock = 0;
        if (tick < Arrivals[i]) return false;
        clock = CenserRules.After(starts[i], tick - Arrivals[i]);
        return true;
    }

    private static float Depth(Vector2 ring, float emitted, float floorY)
        => Math.Clamp(floorY - (ring.Y + CenserRules.Mouth(emitted).Y), 0, CrimsonRewardRules.PourMaxDepth);

    public void Emit(ScarletInkCanvas canvas, Vector2 center, int tick)
    {
        float floorY = Floor(center);
        for (int i = 0; i < Arrivals.Length; i++)
        {
            Vector2 ring = Ring(center, i);
            // Live: the column at this tick, from the mouth down to its foot on the floor.
            if (Clock(i, tick, out int clock) && CenserRules.TryPour(clock, out var pour))
                Column(canvas, ring, floorY, pour, clock, ScarletInkLook.Live, i * 3.1f + pour.Start * .37f, 0);
            // Residue: every pour of this censer that ended in the last 24 ticks, drying where it fell, the top first.
            for (int back = 1; back <= CrimsonRewardRules.PourScar; back++)
            {
                if (!Clock(i, tick - back, out int before) || !CenserRules.TryPour(before, out var ended) || ended.Age != ended.Live - 1) continue;
                var end = ended with { Age = ended.Live };
                Column(canvas, ring, floorY, end, end.Start + end.Live, ScarletInkLook.Residue, i * 3.1f + end.Start * .37f, back - 1);
            }
        }
    }

    // The same column the minion collides with (CenserRules), drawn top first; a residue fades from the top.
    private static void Column(ScarletInkCanvas canvas, Vector2 ring, float floorY, in CenserPour pour, float clock, ScarletInkLook look, float seed, float age)
    {
        Span<float> emitted = stackalloc float[CenserRules.MaxColumnPoints];
        Span<float> depths = stackalloc float[CenserRules.MaxColumnPoints];
        int all = CenserRules.Emissions(pour, clock, emitted);
        for (int k = 0; k < all; k++) depths[k] = Depth(ring, emitted[k], floorY);
        Span<System.Numerics.Vector2> column = stackalloc System.Numerics.Vector2[CenserRules.MaxColumnPoints];
        Span<float> radii = stackalloc float[CenserRules.MaxColumnPoints], times = stackalloc float[CenserRules.MaxColumnPoints];
        int n = CenserRules.Column(pour, clock, depths[..all], column, radii, times, out _);
        canvas.Begin(new ScarletInkStyle(look, true, seed, 1, true, 0, 0, Remaining: pour.Live - pour.Age)); // as CenserVisuals
        Vector2 previous = default; float previousTime = 0;
        for (int i = 0; i < n; i++)
        {
            Vector2 at = ring + Xna(column[i]);
            float u = n > 1 ? i / (n - 1f) : 1;
            float time = look == ScarletInkLook.Live ? times[i] : Math.Clamp(1 - (age + 4 * (1 - u)) / CrimsonRewardRules.PourScar, 0, 1);
            if (i > 0) Subdivide(canvas, previous, at, pour.Radius, previousTime, time);
            canvas.Point(at, pour.Radius, time);
            previous = at; previousTime = time;
        }
        canvas.End();
    }

    private static void Subdivide(ScarletInkCanvas canvas, Vector2 from, Vector2 to, float radius, float timeFrom, float timeTo)
    {
        int parts = (int)MathF.Ceiling(Vector2.Distance(from, to) / 10f);
        for (int s = 1; s < parts; s++)
        {
            float u = s / (float)parts;
            canvas.Point(Vector2.Lerp(from, to, u), radius, MathHelper.Lerp(timeFrom, timeTo, u));
        }
    }

    public void Particles(ScarletRewardParticles particles, Vector2 center, int tick, bool reduced)
    {
        float floorY = Floor(center);
        for (int i = 0; i < Arrivals.Length; i++)
        {
            Vector2 ring = Ring(center, i);
            if (!Clock(i, tick, out int clock)) continue;
            Vector2 mouth = ring + Xna(CenserRules.Mouth(clock));
            float h = ScarletRewardParticles.Hash(i * 5.3f, tick);
            if (h < .08f) particles.Spawn(ScarletParticleKind.Smoke, 0, true, mouth, new Vector2(0, -.5f), 34, 10, reduced, tick + i);
            if (h > .7f) particles.Spawn(ScarletParticleKind.Ember, 0, true, mouth, new Vector2((h - .85f) * 2, -.8f), 22, 4, reduced, tick * 3 + i);
            if (!CenserRules.TryPour(clock, out var pour)) continue;
            float x = ring.X + CenserRules.Mouth(pour.Start).X;
            for (int e = 0; e < 2; e++)
            {
                float a = ScarletRewardParticles.Hash(i + e * 7.7f, tick);
                particles.Spawn(ScarletParticleKind.Ember, 0, true, new Vector2(x + (a - .5f) * pour.Radius * 1.4f, floorY - 4),
                    new Vector2((a - .5f) * 3.2f, -1.4f - 2 * a), 22, 5, reduced, tick * 7 + e + i);
            }
        }
    }

    public void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 center, int tick)
    {
        var batch = renderer.Batch;
        Matrix world = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        Vector2 top = TargetTop(center);
        Line(batch, renderer.Pixel, new Vector2(center.X - 420, Floor(center)), new Vector2(center.X + 420, Floor(center)), 3, new Color(120, 110, 100));
        Box(batch, renderer.Pixel, new Rectangle((int)(top.X - TargetWidth / 2), (int)top.Y, (int)TargetWidth, (int)TargetHeight), new Color(200, 200, 210));
        for (int i = 0; i < Arrivals.Length; i++)
        {
            Vector2 ring = Ring(center, i);
            float angle = Clock(i, tick, out int clock) ? CenserRules.Angle(clock) : 0, tip = Clock(i, tick, out _) ? CenserRules.Tip(clock) : 0;
            Vector2 mouth = ring + Xna(CenserRules.MouthAt(angle));
            Line(batch, renderer.Pixel, ring, mouth, 2, new Color(170, 140, 70));
            // A stand-in bowl turned about the mouth by the pendulum and the tip.
            float rotation = -(angle + tip);
            batch.Draw(renderer.Pixel, mouth, null, new Color(40, 30, 34), rotation, new Vector2(.5f, 0), new Vector2(26, 18), SpriteEffects.None, 0);
            batch.Draw(renderer.Pixel, mouth, null, new Color(230, 220, 190), rotation, new Vector2(.5f, 1), new Vector2(26, 6), SpriteEffects.None, 0);
        }
        batch.End();
    }

    private static void Line(SpriteBatch batch, Texture2D pixel, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 d = b - a;
        batch.Draw(pixel, a, null, color, MathF.Atan2(d.Y, d.X), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }

    private static void Box(SpriteBatch batch, Texture2D pixel, Rectangle r, Color color)
    {
        Line(batch, pixel, new Vector2(r.Left, r.Top), new Vector2(r.Right, r.Top), 2, color);
        Line(batch, pixel, new Vector2(r.Left, r.Bottom), new Vector2(r.Right, r.Bottom), 2, color);
        Line(batch, pixel, new Vector2(r.Left, r.Top), new Vector2(r.Left, r.Bottom), 2, color);
        Line(batch, pixel, new Vector2(r.Right, r.Top), new Vector2(r.Right, r.Bottom), 2, color);
    }
}
