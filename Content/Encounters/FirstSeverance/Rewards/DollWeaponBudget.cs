#nullable enable
using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Power budget of the five 0.3.70 reward weapons, which the 2026-10 refresh must reproduce within
// Tolerance. Figures are raw: before defense, no crit, one target, every hit landing, item damage
// already including TargetUpgrade, per-hit ScaledDamage rounding and ChoirRequiem's (int) truncation.
// DollWeaponLayerTests recomputes every constant from the legacy scores (NullCantorClawMotion,
// RitualGrandScore, RitualArmamentRules and the legacy projectile multipliers), so these numbers are
// pinned, not free. Each weapon PR's own test compares its new score per cycle, per second and over
// the best cold-press window against them. Pure: no Terraria types, linked into the domain tests.
//
// Cold-press window: tick 0 is the press; a hit counts if it lands on a tick in [0, ColdWindowTicks).
// A held projectile spawned on the press tick is first updated that tick, so score age a lands on
// tick a - 1. Spawned projectiles count on their spawn tick (travel ignored); held hit windows count
// on their first live tick; a returning blade counts its return 42 ticks after its throw.
internal static class DollWeaponBudget
{
    internal const double Tolerance = .03;
    internal const int ColdWindowTicks = 600;
    internal const double TicksPerSecond = 60;

    // One repeating part of a weapon's score: CycleRaw over CycleTicks at BaseDamage.
    internal readonly record struct Line(string Name, int BaseDamage, int CycleTicks, double CycleMultiplier, double CycleRaw)
    {
        internal double PerSecond => DollWeaponBudget.PerSecond(CycleRaw, CycleTicks);
    }

    // Lacrimosa's Claws (NullRefrain): one 402-tick cycle is the 360-tick charge, during which the
    // 28-tick swipes run (360/28 = 12.857 at x1.0), then the 42-tick crush at x4.2 (32340) while
    // swipes are blocked: 17.057x = 131,340, 19,603/s.
    internal static readonly Line Claw = new("Lacrimosa's Claws", 7700, 402, 360d / 28 + 4.2, 131_340);

    // Pale Meridian build, ages 1-347: 15 needles x0.95 (1902 each) = 14.25x.
    internal static readonly Line MeridianBuild = new("Pale Meridian build", 2002, 348, 14.25, 28_530);

    // Pale Meridian overdrive, per 36 ticks: 11 needles x0.62 (1241) + 1 heavy x1.15 (2302) = 7.97x,
    // 26,588/s with per-hit rounding (26,593/s unrounded). Ammo damage excluded.
    internal static readonly Line MeridianOverdrive = new("Pale Meridian overdrive", 2002, 36, 7.97, 15_953);

    // Lacuna Testament build, ages 1-409: 26 bolts x0.55 (1113 each) = 14.3x.
    internal static readonly Line LacunaBuild = new("Lacuna Testament build", 2024, 410, 14.3, 28_938);

    // Lacuna Testament beam: x2.0 (4048) per logical NPC root every 10 ticks = 24,288/s.
    internal static readonly Line LacunaBeam = new("Lacuna Testament beam", 2024, 10, 2.0, 4_048);

    // Choir of the Unmade, per voice, one 660-tick concert: ordinals 0-3 sing 6 notes and 4-7 sing 5
    // (x0.85, 823 each), then 15 chorus hits of (int)(968 x 1.05) = 1016. The mean of the two note
    // counts is 20.425x = 19,766.5, 1,797/s.
    internal static readonly Line ChoirConcert = new("Choir of the Unmade concert, per voice", 968, 660, 20.425, 19_766.5);

    // Choir chorus alone, per voice: 1016 every 12 ticks = 5,080/s.
    internal static readonly Line ChoirChorus = new("Choir of the Unmade chorus, per voice", 968, 12, 1.05, 1_016);

    // Last Witness, one 282-tick litany: 6 testimonies x0.28 (2710) + the blade x5.4 (52272, split
    // 0.70 out / 0.30 back) = 7.08x = 68,532, 14,581/s.
    internal static readonly Line Witness = new("Last Witness", 9680, 282, 7.08, 68_532);

    // The stealth verdict adds x0.60 of the blade's 52272 = 3.24x on top of a stealth litany.
    internal const double WitnessVerdictMultiplier = 3.24;
    internal const double WitnessVerdictRaw = 31_363;

    // Best raw damage in the first ColdWindowTicks after a cold press. The claws' crush is player-timed:
    // pressing it right after a swipe lands keeps 21 swipes plus the crush (pressing at tick 360 keeps 20).
    internal const double ClawColdWindow = 194_040;
    internal const double MeridianColdWindow = 141_442;   // the build, then overdrive to age 600
    internal const double LacunaColdWindow = 105_850;     // the build, then 19 beam hits from age 411
    internal const double ChoirColdWindow = 20_178;       // one concert of a 6-note voice
    internal const double WitnessColdWindow = 139_774;    // two litanies and the third one's first shard

    internal static double PerSecond(double raw, double ticks)
        => ticks > 0 && double.IsFinite(raw) ? raw * TicksPerSecond / ticks : 0;

    // True when `actual` is within +-tolerance of `baseline` (relative).
    internal static bool Within(double actual, double baseline, double tolerance = Tolerance)
        => double.IsFinite(actual) && baseline > 0 && Math.Abs(actual - baseline) <= baseline * tolerance;

    // Relative deviation, e.g. -0.021 for 2.1% under the baseline.
    internal static double Deviation(double actual, double baseline)
        => baseline > 0 ? (actual - baseline) / baseline : double.NaN;

    // Raw damage landing in [0, window) after a cold press; rawAt(tick) is the damage landing on that tick.
    internal static double ColdWindow(Func<int, double> rawAt, int window = ColdWindowTicks)
    {
        ArgumentNullException.ThrowIfNull(rawAt);
        double total = 0;
        for (int tick = 0; tick < window; tick++) total += rawAt(tick);
        return total;
    }

    // The best cold-press window over the player's timing choices (e.g. a release tick 0..timings-1):
    // rawAt(timing, tick) is the damage landing on `tick` under that timing.
    internal static double BestColdWindow(int timings, Func<int, int, double> rawAt, out int bestTiming,
        int window = ColdWindowTicks)
    {
        ArgumentNullException.ThrowIfNull(rawAt);
        if (timings <= 0) throw new ArgumentOutOfRangeException(nameof(timings));
        double best = double.NegativeInfinity;
        bestTiming = 0;
        for (int timing = 0; timing < timings; timing++)
        {
            double total = 0;
            for (int tick = 0; tick < window; tick++) total += rawAt(timing, tick);
            if (total > best)
            {
                best = total;
                bestTiming = timing;
            }
        }
        return best;
    }
}
