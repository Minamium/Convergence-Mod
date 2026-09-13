using System;
using System.Linq;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Impaling second wave displaces every first-wave standing safe position")]
    private static void ImpalingSecondWaveShift()
    {
        foreach (int step in new[] {1,4})
        foreach (ulong seed in new ulong[] { 0, 2657, 8465, 92782, 12345678, ulong.MaxValue })
        {
            var first = FirstSeveranceImpalingSwords.At(step,132,4000,4000,seed);
            var second = FirstSeveranceImpalingSwords.At(step,238,4000,4000,seed);
            int safe = 0;
            for (float x = 2730; x <= 5270; x += 2)
            {
                if (first.Any(s => s.FullRay.Intersects(x,3440,10,21))) continue;
                safe++;
                AssertEqual(true, second.Any(s => s.FullRay.Intersects(x,3440,10,21)), "must move from first-wave gap");
            }
            AssertEqual(true,safe > 0,"first wave still dodgeable");
        }
    }

    [DomainTest("Impaling jets share width and preserve randomized tight and generous safe gaps")]
    private static void ImpalingSwordCoverage()
    {
        foreach (ulong seed in new ulong[] { 0, 2657, 8465, 92782, 12345678, ulong.MaxValue })
        foreach (int step in new[] { 1, 4 })
        for (int wave = 0; wave < 2; wave++)
        {
            int age = FirstSeveranceImpalingSwords.FireBase(wave) + 24;
            var swords = FirstSeveranceImpalingSwords.At(step, age, 4000, 4000, seed);
            AssertEqual(true, swords.Count <= FirstSeveranceImpalingSwords.Count, "bounded wrapped fragments");
            AssertEqual(true, swords.All(s => s.Live && s.Extension == 1
                && s.FullRay.HalfWidth == FirstSeveranceImpalingSwords.HalfWidth), "one shared live width");
            AssertEqual(true, swords.SequenceEqual(FirstSeveranceImpalingSwords.At(step, age, 4000, 4000, seed)),
                "same accepted seed on another peer");
            var authority = FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.HalfField,
                step, age, 4000, 4000, seed);
            bool Hits(float x) => authority.Any(r => FirstSeveranceScoreGeometry.RayHits(
                FirstSeveranceSubstate.HalfField, r, age, x, 3440, 10, 21));
            for (int side = 0; side < 2; side++)
            {
                float start = side == 0 ? 2720 : 4000;
                bool dense = (step < 4) == (side == 0);
                var ordered = swords.Where(s => s.FullRay.X >= start + 52 && s.FullRay.X <= start + 1228)
                    .OrderBy(s => s.FullRay.X).ToArray();
                for (int i = 1; i < ordered.Length; i++)
                {
                    float left = ordered[i-1].FullRay.X + 52, right = ordered[i].FullRay.X - 52;
                    AssertEqual(true, right - left >= (dense ? 26 : 62) - .01f
                        && right - left < (dense ? 58 : 103), "bounded random gap, not random beam width");
                    AssertEqual(false, Hits((left + right) / 2), "whole player fits both sides");
                }
                AssertEqual(true, ordered.Length >= 4, "multiple meaningful safe gaps");
            }
            AssertEqual(true, swords.Any(s => s.FullRay.DirectionY < 0)
                && swords.Any(s => s.FullRay.DirectionY > 0), "mixed top/bottom jets");
            AssertEqual(true, swords.Select(s => s.Fire).Distinct().Count() > 3, "irregular stagger");
        }
        AssertEqual(false, FirstSeveranceImpalingSwords.At(1,132,4000,4000,1)
            .SequenceEqual(FirstSeveranceImpalingSwords.At(1,132,4000,4000,2)), "new action changes spacing");
        AssertEqual(0, FirstSeveranceImpalingSwords.At(1, 300, 4000, 4000).Count, "action cleanup");
    }

    [DomainTest("Impaling warnings and shared ignition never damage beyond visible tip")]
    private static void ImpalingInsertionBoundaries()
    {
        foreach (int step in new[] {1, 4})
            for (int age = 0; age < 300; age++)
                foreach (var sword in FirstSeveranceImpalingSwords.At(step, age, 4000, 4000))
                {
                    AssertEqual(true, float.IsFinite(sword.Extension) && sword.Extension is >= 0 and <= 1, "finite insertion");
                    if (age <= sword.Fire || age >= sword.Retract) AssertEqual(false, sword.Live, "forecast/retraction harmless");
                    if (age == sword.Fire + FirstSeveranceBeamIgnition.TravelTicks) AssertEqual(1f, sword.Extension, "shared insertion complete");
                    if (sword.Live && sword.Extension < .8f)
                    {
                        var actual = FirstSeveranceImpalingSwords.BeamAt(sword, age);
                        float y = sword.FullRay.Y + sword.FullRay.DirectionY * (actual.Length + 70);
                        AssertEqual(false, actual.Intersects(actual.X, y, 10, 21), "unreached far tip not damaging");
                    }
                }
    }
}
