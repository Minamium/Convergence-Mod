using System;
using Convergence.Content.Encounters.EbonManor;

namespace Convergence.Client.Encounters.EbonManor;

// Pure presentation clocks (no graphics types): shared by the sky, the
// renderers and the Terraria-independent domain tests.
// Hall lighting on AutoMatador's bar grid, from the accepted clock only.
internal readonly record struct EbonHallLight(float Fade, float Tear, float Tension, float Candles, float Pulse, float Exposure, float Flash);

// The entrance follows AutoMatador's intro on its true bar grid: a two-beat
// pickup, then bar 0. The quiet motif opens (0-2), bass and the groove enter
// (2), the build peaks (8), one bar breaks almost silent (9), the second build
// rises (10), the bass returns (12.5), two held beats (13.5) and the A drop (14).
internal static class EbonIntro
{
    internal const float Groove = 2, Threads = 4, Burst = 8, Break = 9, Bloom = 10, Gather = 12.5f, Hush = 13.5f, Drop = 14;
    internal static float Bars(in EbonState s, float age)
        => s.MusicStart < 0 ? -1 : (age - s.MusicStart - EbonRules.IntroPickup) / EbonRules.BarTicks;
    internal static int Tick(in EbonState s, float bar)
        => s.MusicStart + (int)MathF.Round(EbonRules.IntroPickup + bar * EbonRules.BarTicks);
}

internal static class EbonHall
{
    internal static EbonHallLight Light(in EbonState s, float age)
    {
        if (s.MusicStart < 0 || age < s.MusicStart) return default;
        float t = age - s.MusicStart, bars = EbonIntro.Bars(s, age);
        float fade = EbonVisualsMath.Ease(t / 90);
        // Moonlight carries the quiet opening; with the groove the candles light
        // outward one beat at a time and bring the hall up.
        float candles = Math.Clamp(MathF.Floor((bars - EbonIntro.Groove) * 4 + 1) / 8, 0, 1);
        float exposure = EbonVisualsMath.Lerp(.22f, .55f, EbonVisualsMath.Ease((bars + .5f) / 2.5f));
        exposure = EbonVisualsMath.Lerp(exposure, 1, EbonVisualsMath.Ease((bars - EbonIntro.Groove) / 2));
        // The break dims the hall around her weaving; the held beats before the drop hush it.
        exposure *= 1 - .3f * EbonVisualsMath.Ease((bars - EbonIntro.Break) * 3) * (1 - EbonVisualsMath.Ease((bars - EbonIntro.Bloom) * 3));
        exposure *= 1 - .32f * EbonVisualsMath.Ease((bars - EbonIntro.Hush) * 4) * (1 - EbonVisualsMath.Ease((bars - EbonIntro.Drop) * 8));
        float beat = s.UnlockAt >= 0 && age >= s.UnlockAt ? (age - s.UnlockAt) / EbonRules.BeatTicks % 1 : 1;
        float pulse = MathF.Exp(-beat * 4);
        float flash = s.UnlockAt >= 0 ? .8f * EbonVisualsMath.Pulse(age - s.UnlockAt, 12) : 0;
        flash = Math.Max(flash, .4f * EbonVisualsMath.Pulse(age - EbonIntro.Tick(s, EbonIntro.Burst), 14));
        float tension = 0, tear = 0;
        if (s.Phase != EbonPhase.ActOne && s.PhaseAt >= 0)
        {
            float since = age - s.PhaseAt;
            tension = s.Phase == EbonPhase.Finale ? 1 : EbonVisualsMath.Ease(since / EbonRules.BarTicks);
            exposure *= 1 - .3f * EbonVisualsMath.Pulse(since, 40);
            flash = Math.Max(flash, .9f * EbonVisualsMath.Pulse(age - s.Epoch, 14));
            if (s.Phase == EbonPhase.Finale) tear = Tear(since);
        }
        if (s.EndAt >= 0)
        {
            float end = age - s.EndAt;
            if (s.Stage == EbonStage.Victory)
            {
                tension *= 1 - EbonVisualsMath.Ease(end / 200);
                candles *= 1 - .6f * EbonVisualsMath.Ease((end - 120) / 150);
                exposure = EbonVisualsMath.Lerp(exposure, 1.12f, EbonVisualsMath.Ease((end - 60) / 180));
                flash = Math.Max(flash, .7f * EbonVisualsMath.Pulse(end, 16));
            }
            else
            {
                exposure *= 1 - .65f * EbonVisualsMath.Ease(end / 200);
                tension = Math.Max(tension, 1.5f * EbonVisualsMath.Ease(end / 160));
            }
        }
        return new(fade, tear, tension, candles, pulse, exposure, flash);
    }

    // The Finale rips the hall open: nothing on the first bar, one widening rip
    // per beat for two bars, and the last, largest rip on the A' downbeat itself.
    internal static float Tear(float since)
    {
        float bars = since / EbonRules.BarTicks;
        if (bars < 1) return 0;
        if (bars >= 3) return 1;
        float beats = (bars - 1) * 4, step = MathF.Floor(beats);
        float within = EbonVisualsMath.Ease((beats - step) * EbonRules.BeatTicks / 7);
        return MathF.Pow((step + within) / 9, 1.4f);
    }
}

internal static class EbonVisualsMath
{
    internal static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static float OutExpo(float x) => x <= 0 ? 0 : x >= 1 ? 1 : 1 - MathF.Pow(2, -10 * x);
    internal static float InCubic(float x) { x = Math.Clamp(x, 0, 1); return x * x * x; }
    internal static float Pulse(float t, float decay) => t < 0 ? 0 : MathF.Exp(-t / decay);
    internal static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
