using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.EbonManor;

// Pure clocks and geometry: the same functions drive native collision, the
// boss's deterministic position and every client presentation.
internal static class EbonRules
{
    // Temporary rehearsal switch shared with the other in-development Raids:
    // every hostile native hit is capped at one damage until balance begins.
    internal const bool DebugOneDamagePlaytest = true;
    internal static int NativeSourceDamage(int intended)
        => intended <= 0 ? 0 : DebugOneDamagePlaytest ? 1 : intended;
    internal static int NativeFinalDamageLimit(int intended) => NativeSourceDamage(intended);

    internal const int Members = 8, Deploy = 120, Ending = 300, VictoryEnding = 420;

    // AutoMatador is a steady 125 BPM: one beat is 28.8 ticks, one bar 115.2.
    internal const float BeatTicks = 28.8f, BarTicks = BeatTicks * 4;
    internal const int CycleBeats = 64;                 // one 16-bar attack cycle
    internal const int Intro = 922;                     // ActOne cue: 8 bars to the A drop
    internal const int ActTwoLead = 230, FinaleLead = 346; // 2 and 3 bars of the next cue

    internal const float ActTwoThreshold = .62f, FinaleThreshold = .28f;
    internal static int Life(int members) => checked(5_200_000 + (members - 1) * 2_800_000);
    internal static int Floor(int maximum, EbonPhase phase)
        => phase switch
        {
            EbonPhase.ActOne => (int)Math.Ceiling(maximum * ActTwoThreshold),
            EbonPhase.ActTwo => (int)Math.Ceiling(maximum * FinaleThreshold),
            _ => 0,
        };
    internal static int Lead(EbonPhase phase) => phase == EbonPhase.ActTwo ? ActTwoLead : phase == EbonPhase.Finale ? FinaleLead : Intro;

    internal static int Beat(int epoch, int beat) => epoch + (int)MathF.Round(beat * BeatTicks);
    // The beat index that falls exactly on this tick, or -1.
    internal static int BeatAt(int epoch, int age)
    {
        if (age < epoch) return -1;
        int beat = (int)MathF.Round((age - epoch) / BeatTicks);
        return Beat(epoch, beat) == age ? beat : -1;
    }

    // Damage budgets before the rehearsal cap.
    internal const int ThreadDamage = 300, ChandelierDamage = 340, LoomDamage = 280,
        ShearsDamage = 340, WaltzDamage = 260;

    // Thrown furniture
    internal const float PropRadius = 46, PropLaunchSpeed = 9, PropAcceleration = 1.15f;
    internal static float PropTravel(float t) => t <= 0 ? 0 : PropLaunchSpeed * t + .5f * PropAcceleration * t * t;
    internal static int PropFlightTicks(float length)
    {
        // Solve PropTravel(t) = length for t (always a positive root).
        float a = .5f * PropAcceleration, b = PropLaunchSpeed;
        return (int)MathF.Ceiling((-b + MathF.Sqrt(b * b + 4 * a * length)) / (2 * a));
    }

    // Chandeliers
    internal const float ChandelierHalfWidth = 112, ChandelierBodyRadius = 118, ChandelierGravity = 3.1f,
        BurstRadius = 270;
    internal const int BurstTicks = 10;
    internal static float ChandelierDrop(float t) => t <= 0 ? 0 : .5f * ChandelierGravity * t * t;
    internal static int ChandelierFallTicks(float height) => (int)MathF.Ceiling(MathF.Sqrt(2 * height / ChandelierGravity));

    // Loom threads and shears
    internal const float LoomRadius = 14, LoomSpacing = 300, ShearsRadius = 74;
    internal const int LoomLive = 12, ShearsLive = 14;

    // Parasol waltz
    internal const float SpokeRadius = 13, SpokeReach = 2600;

    internal static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static float Envelope(float elapsed, float duration)
        => elapsed < 0 || elapsed >= duration ? 0 : Ease(elapsed / 4) * Ease((duration - elapsed) / 6);

    internal static (float X, float Y) Clamp(RaidFieldGeometry field, float x, float y, int w, int h)
    {
        var p = field.ClampBody(x, y, w, h, 0);
        return (Math.Clamp(p.X, field.Left + 2, field.Right - w - 2), Math.Max(field.Top + 2, p.Y));
    }

    // Noirette glides between four stations per 16-bar cycle (centre, left,
    // centre, right); each change takes the first bar of its 4-bar block, and
    // the waltz always starts on a centre block. A pure function of the accepted
    // clock, so the server's waltz geometry and every client's sprite agree.
    internal static readonly float[] Stations = { 0, -.2f, 0, .2f };
    internal static (float X, float Y) BossPosition(RaidFieldGeometry field, int epoch, float age)
    {
        float width = field.Right - field.Left, height = field.Bottom - field.Top;
        float bars = Math.Max(0, age - epoch) / BarTicks;
        int block = (int)(bars / 4) % Stations.Length;
        float within = bars % 4;
        float from = bars < 4 ? 0 : Stations[(block + Stations.Length - 1) % Stations.Length], to = Stations[block];
        float x = age < epoch ? 0 : from + (to - from) * Ease(within);
        float bob = MathF.Sin(age * .035f) * 10;
        return (field.CenterX + x * width, field.Top + height * .3f + bob);
    }
}
