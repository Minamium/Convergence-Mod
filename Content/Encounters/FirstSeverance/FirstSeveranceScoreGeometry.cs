using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceScoreRay(FirstSeveranceLanceRay Ray, bool Live, int Pulse, float Charge);
internal readonly record struct FirstSeveranceScoreBullet(float X, float Y, float PreviousX, float PreviousY, bool Live, int Wave);

// Deterministic geometry shared by authority and fractional client drawing.
// No random/client targets, projectile slots or persistent world actors.
internal static class FirstSeveranceScoreGeometry
{
    internal const int FixedDamage = 120;
    internal const float FieldHalfWidth = 1280, FieldHalfHeight = 560;
    internal const float BladeHalfWidth = 44, BulletRadius = 12;
    internal const int BladeCount = 2;
    internal const int CrushRushTick = 150, CrushImpactTick = 162, CrushReleaseTick = 180;
    internal const float CrushHalfWidth = 360, CrushHalfHeight = 300;
    internal const int SlicerPulses = 3;
    internal const int FloodInterval = 200, FloodFireTick = 48, FloodDeployTicks = 12,
        FloodGrowTicks = 42, FloodEndTick = 182, FloodFadeTick = 196;
    internal const float FloodSafeHalfHeight = 96;
    internal const int BulletWaves = 5, BulletsPerWave = 24;
    internal static bool HasHazards(FirstSeveranceSubstate state) => state is FirstSeveranceSubstate.RotatingBlade
        or FirstSeveranceSubstate.RemoteClaws or FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RemoteCrush
        or FirstSeveranceSubstate.FinalBullets or FirstSeveranceSubstate.FinalSlicer;
    internal static float Smooth(float value) { float t = Math.Clamp(value, 0, 1); return t * t * t * (10 + t * (-15 + t * 6)); }
    private static float BladeTravel(double age)
    {
        float t = Math.Clamp((float)((age - FirstSeveranceChoreography.BladeWindup) / FirstSeveranceChoreography.BladeSpin), 0, 1);
        return FirstSeveranceChoreography.BladeTurns * (.55f * t + .45f * t * t);
    }
    internal static float BladeAngle(double age) => -MathF.PI * .5f + MathF.Tau * BladeTravel(age);
    internal static int BladeTurn(double age) => Math.Min(FirstSeveranceChoreography.BladeTurns - 1, (int)BladeTravel(age));
    internal static float BladeExtension(double age) => Smooth((float)((age - 144) / 12))
        * (1 - Smooth((float)((age - FirstSeveranceChoreography.BladeEnd - 12) / 45)));
    // Reveal all three locked combs before the first can hurt. The last reveal
    // always gets a full 0.7–0.9 second reading window, even late in Final.
    internal static int SlicerReveal(int pulse) => pulse * 12;
    internal static int SlicerFire(int step) => SlicerReveal(2) + 54 - (int)MathF.Round(FirstSeveranceChoreography.FinalProgress(step) * 12);
    internal static int SlicerEnd(int step) => SlicerFire(step) + 6;
    internal static int SlicerCadence(int step) => 24 - (int)MathF.Round(FirstSeveranceChoreography.FinalProgress(step) * 6);
    internal const int SlicerPitch = 112;
    internal static int BulletStartTick(int step, int wave) => 36 - (int)(FirstSeveranceChoreography.FinalProgress(step) * 8)
        + wave * (32 - (int)(FirstSeveranceChoreography.FinalProgress(step) * 10));
    internal static float BulletSpeed(int step) => 10 + FirstSeveranceChoreography.FinalProgress(step) * 3.5f;
    internal static float FloodSafeY(int step, int pulse) => ((pulse + (step >= 4 ? 1 : 0)) % 3) switch
        { 0 => -260, 1 => 200, _ => -40 };

    internal static float FloodSafeHalfHeightFor(int pulse)
        => pulse % 2 == 0 ? FirstSeveranceLanceTuning.StackRadius + 36 : FloodSafeHalfHeight;

    internal static FirstSeveranceLanceRay FloodBand(int step, int pulse, int band, float groundX, float groundY)
    {
        float safeY = FloodSafeY(step, pulse);
        float safeHalf = FloodSafeHalfHeightFor(pulse);
        float top = band == 0 ? -FieldHalfHeight : safeY + safeHalf;
        float bottom = band == 0 ? safeY - safeHalf : FieldHalfHeight;
        int side = pulse % 2 == 0 ? -1 : 1;
        return new(groundX + side * FieldHalfWidth, groundY - FieldHalfHeight + (top + bottom) * .5f,
            -side, 0, FieldHalfWidth * 2, (bottom - top) * .5f);
    }

    internal static float FloodGrowth(double local)
    {
        float t = Math.Clamp((float)((local - FloodFireTick - FloodDeployTicks) / FloodGrowTicks), 0, 1);
        // Accelerates from a needle to a full volume, then eases into its hold.
        // This curve, not a decorative scale, also defines authoritative width.
        return t * t * t * (4 - 3 * t);
    }
    internal static float CrushClosure(double age) => Smooth((float)((age - CrushRushTick) / (CrushImpactTick - CrushRushTick)))
        * (1 - Smooth((float)((age - CrushReleaseTick) / 70)));
    internal static float ToEdge(float dx, float dy) => Math.Min(
        Math.Abs(dx) < .00001f ? float.MaxValue : FieldHalfWidth / Math.Abs(dx),
        Math.Abs(dy) < .00001f ? float.MaxValue : FieldHalfHeight / Math.Abs(dy));

    internal static IReadOnlyList<FirstSeveranceScoreRay> Rays(FirstSeveranceSubstate state, int step,
        double age, float groundX, float groundY, ulong actionSeed = 0)
    {
        var rays = new List<FirstSeveranceScoreRay>(16);
        float cy = groundY - FieldHalfHeight;
        if (age < 0) return rays;
        if (state == FirstSeveranceSubstate.RotatingBlade
            && age < FirstSeveranceChoreography.BladeWindup + FirstSeveranceChoreography.BladeSpin + FirstSeveranceChoreography.BladeRecovery)
        {
            for (int blade = 0; blade < BladeCount; blade++)
            {
                float angle = BladeAngle(age) + blade * MathF.PI, dx = MathF.Cos(angle), dy = MathF.Sin(angle);
                // Full corridors warned immediately; unsheathing finishes 24 ticks before live rotation.
                rays.Add(new(new(groundX, cy, dx, dy, ToEdge(dx, dy), BladeHalfWidth),
                    age >= FirstSeveranceChoreography.BladeWindup
                        && age < FirstSeveranceChoreography.BladeWindup + FirstSeveranceChoreography.BladeSpin,
                    BladeTurn(age) * BladeCount + blade, Smooth((float)age / FirstSeveranceChoreography.BladeWindup)));
            }
        }
        else if (state == FirstSeveranceSubstate.RemoteClaws && age < 600)
        {
            int pulse = (int)age / FloodInterval;
            double local = age - pulse * FloodInterval;
            if (local >= FloodFadeTick) return rays;
            float deployment = Smooth((float)((local - FloodFireTick) / FloodDeployTicks));
            float growth = FloodGrowth(local);
            float cooling = 1 - Smooth((float)((local - FloodEndTick) / (FloodFadeTick - FloodEndTick)));
            for (int band = 0; band < 2; band++)
            {
                var full = FloodBand(step, pulse, band, groundX, groundY);
                var ray = full with { HalfWidth = (10 + (full.HalfWidth - 10) * growth) * cooling,
                    Length = local < FloodFireTick ? full.Length : Math.Max(1, full.Length * deployment) };
                rays.Add(new(ray, local >= FloodFireTick && local < FloodEndTick,
                    pulse, Smooth((float)local / FloodFireTick)));
            }
        }
        else if (state == FirstSeveranceSubstate.HalfField && age < 300)
        {
            foreach (var sword in FirstSeveranceImpalingSwords.At(step, age, groundX, groundY))
                rays.Add(new(sword.FullRay with { Length = Math.Max(1, sword.FullRay.Length * sword.Extension) },
                    sword.Live, sword.Wave, sword.Charge));
        }
        else if (state == FirstSeveranceSubstate.RemoteCrush && age < 300)
        {
            // Fixed, completely warned central rectangle. Moving approach is harmless.
            rays.Add(new(new(groundX - CrushHalfWidth, cy, 1, 0, CrushHalfWidth * 2, CrushHalfHeight),
                age >= CrushImpactTick && age < CrushReleaseTick, 0, Smooth((float)age / CrushRushTick)));
        }
        else if (state == FirstSeveranceSubstate.FinalSlicer && age < FirstSeveranceChoreography.FinalHazardTicks(step))
        {
            for (int pulse = 0; pulse < SlicerPulses; pulse++)
            {
                int reveal = SlicerReveal(pulse), fire = SlicerFire(step) + pulse * SlicerCadence(step);
                if (age < reveal || age >= fire + 18) continue;
                foreach (var ray in FirstSeveranceRandomComb.Rays(actionSeed, step, pulse, groundX, cy))
                    rays.Add(new(ray, age >= fire && age < fire + 6, pulse,
                        Smooth((float)(age - reveal) / (fire - reveal))));
            }
        }
        return rays;
    }

    internal static IReadOnlyList<FirstSeveranceScoreBullet> Bullets(int step, double age, float groundX, float groundY)
    {
        var bullets = new List<FirstSeveranceScoreBullet>(BulletWaves * BulletsPerWave);
        if (age < 0 || age >= FirstSeveranceChoreography.FinalHazardTicks(step)) return bullets;
        float speed = BulletSpeed(step), rate = speed / 10;
        for (int wave = 0; wave < BulletWaves; wave++)
        {
            double t = age - BulletStartTick(step, wave);
            if (t < -24 || t >= 115 / rate) continue;
            for (int i = 0; i < BulletsPerWave; i++)
            {
                float angle = MathF.Tau * i / BulletsPerWave + wave * .17f + step * .13f;
                float dx = MathF.Cos(angle), dy = MathF.Sin(angle);
                float distance = ToEdge(dx, dy) - 28;
                // Tangential displacement makes rotating lanes rather than an unavoidable focal point.
                float travel = Math.Max(0, (float)t) * speed;
                float previous = Math.Max(0, (float)t - 1) * speed;
                float skew = 240 * Math.Clamp((float)t * rate / 105, 0, 1);
                float previousSkew = 240 * Math.Clamp(((float)t - 1) * rate / 105, 0, 1);
                float px = groundX + dx * (distance - travel) - dy * skew;
                float py = groundY - 560 + dy * (distance - travel) + dx * skew;
                bullets.Add(new(px, py, groundX + dx * (distance - previous) - dy * previousSkew,
                    groundY - 560 + dy * (distance - previous) + dx * previousSkew, t >= 0, wave));
            }
        }
        return bullets;
    }

    internal static bool RayHits(FirstSeveranceSubstate state, in FirstSeveranceScoreRay item,
        double age, float x, float y, float halfWidth, float halfHeight)
    {
        if (!item.Live) return false;
        if (item.Ray.Intersects(x, y, halfWidth, halfHeight)) return true;
        if (state != FirstSeveranceSubstate.RotatingBlade) return false;
        // Accelerated tips can travel farther than a player width per tick.
        double from = Math.Max(FirstSeveranceChoreography.BladeWindup, age - 1);
        for (int sample = 0; sample < 4; sample++)
        {
            float a = BladeAngle(from + (age - from) * sample / 4) + item.Pulse * MathF.PI;
            float dx = MathF.Cos(a), dy = MathF.Sin(a);
            var ray = new FirstSeveranceLanceRay(item.Ray.X, item.Ray.Y, dx, dy, ToEdge(dx, dy), BladeHalfWidth);
            if (ray.Intersects(x, y, halfWidth, halfHeight)) return true;
        }
        return false;
    }

    internal static bool BulletHits(in FirstSeveranceScoreBullet b, float x, float y, float halfWidth, float halfHeight)
    {
        if (!b.Live) return false;
        float dx = b.X - b.PreviousX, dy = b.Y - b.PreviousY;
        float length2 = dx * dx + dy * dy;
        float t = length2 < .001f ? 0 : Math.Clamp(((x - b.PreviousX) * dx + (y - b.PreviousY) * dy) / length2, 0, 1);
        return Math.Abs(x - b.PreviousX - dx * t) <= halfWidth + BulletRadius
            && Math.Abs(y - b.PreviousY - dy * t) <= halfHeight + BulletRadius;
    }
}
