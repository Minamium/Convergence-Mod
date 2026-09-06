using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Damage telemetry excludes shielding and clamps deadline/overkill")]
    private static void DamageWindowBoundaries()
    {
        foreach (int participants in new[] { 2, 3, 4 })
        {
            int pool = 250_000 * participants;
            var window = new FirstSeveranceDamageWindow(pool, 90, 930);
            var cue = window.Sample(0, pool);
            AssertEqual(0UL, cue.ElapsedTicks, "no shield time in DPS");
            AssertEqual(0d, cue.WindowDps, "no divide by zero");
            AssertEqual(pool / 14d, cue.RequiredDps!.Value, "roster-wide deadline rate");
            var expired = window.Sample(2000, 100_000);
            AssertEqual(840UL, expired.ElapsedTicks, "timeout bounded to active window");
            AssertEqual(null, expired.RequiredDps, "missed deadline has no finite required rate");
            var killed = window.Sample(2000, -1_000_000);
            AssertEqual(pool, killed.EffectiveDamage, "overkill excluded");
            AssertEqual(100d, killed.ProgressPercent, "clear is exactly complete");
            AssertEqual(0d, killed.RecentDps, "duplicate tick is finite");
        }
        var aborted = new FirstSeveranceDamageWindow(100, 90, 930).Sample(30, 100);
        AssertEqual(0UL, aborted.ElapsedTicks, "abort in shield has no open time");
    }

    [DomainTest("Damage telemetry separates recent and whole-window DPS")]
    private static void DamageWindowRates()
    {
        var window = new FirstSeveranceDamageWindow(4_000_000, 100, 1180);
        var first = window.Sample(220, 3_800_000);
        AssertEqual(100_000d, first.WindowDps, "two-second effective DPS");
        AssertEqual(100_000d, first.RecentDps, "first interval DPS");
        var second = window.Sample(340, 3_700_000);
        AssertEqual(75_000d, second.WindowDps, "whole-window mean");
        AssertEqual(50_000d, second.RecentDps, "recent interval excludes older damage");
        AssertEqual(7.5d, second.ProgressPercent, "phase HP progress");
        var nextExposure = new FirstSeveranceDamageWindow(3_700_000, 2000, 2720);
        AssertEqual(0, nextExposure.Sample(2000, 3_700_000).EffectiveDamage, "persistent boss life is not damage in next window");
        AssertEqual(88_068.83333333333d, FirstSeveranceDamageWindow.Rate(2_113_652, 1440), "observed two-exposure DPS");
    }
}
