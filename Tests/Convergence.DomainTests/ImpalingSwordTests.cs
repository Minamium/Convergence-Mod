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
        {
            var first = FirstSeveranceImpalingSwords.At(step,132,4000,4000);
            var second = FirstSeveranceImpalingSwords.At(step,238,4000,4000);
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

    [DomainTest("Impaling swords close one half and preserve tight warned gaps in the other")]
    private static void ImpalingSwordCoverage()
    {
        foreach (int step in new[] { 1, 4 })
            for (int wave = 0; wave < 2; wave++)
            {
                int age = FirstSeveranceImpalingSwords.FireBase(wave) + 24;
                var swords = FirstSeveranceImpalingSwords.At(step, age, 4000, 4000);
                AssertEqual(28, swords.Count, "bounded two-density pattern");
                AssertEqual(true, swords.All(s => s.Live && s.Extension == 1), "whole active hold matches warned corridors");
                bool Hits(float x, float y) => FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.HalfField,
                    step, age, 4000, 4000).Any(r => FirstSeveranceScoreGeometry.RayHits(
                        FirstSeveranceSubstate.HalfField, r, age, x, y, 10, 21));
                float denseStart = step < 4 ? 2720 : 4000;
                for (float x = denseStart + 10; x <= denseStart + 1270; x += 5)
                    foreach (float y in new[] {2901f, 3440f, 3979f})
                        AssertEqual(true, Hits(x, y), "no player-sized holes in dense half at any height");
                var sparse = swords.Where(s => s.Slot >= 20).OrderBy(s => s.FullRay.X).ToArray();
                for (int i = 1; i < sparse.Length; i++)
                {
                    float left = sparse[i-1].FullRay.X + sparse[i-1].FullRay.HalfWidth;
                    float right = sparse[i].FullRay.X - sparse[i].FullRay.HalfWidth;
                    AssertEqual(true, right - left >= 40 && right - left <= 72, "tight but whole-body gap");
                    AssertEqual(false, Hits((left + right) / 2, 3440), "gap actually safe on authority");
                }
                AssertEqual(true, swords.Any(s => s.FullRay.DirectionY < 0)
                    && swords.Any(s => s.FullRay.DirectionY > 0), "mixed top/bottom stabs");
                AssertEqual(true, swords.Select(s => s.Fire).Distinct().Count() > 3, "irregular stagger");
            }
        AssertEqual(0, FirstSeveranceImpalingSwords.At(1, 300, 4000, 4000).Count, "action cleanup");
    }

    [DomainTest("Impaling warnings and six-tick insertion never damage beyond visible tip")]
    private static void ImpalingInsertionBoundaries()
    {
        foreach (int step in new[] {1, 4})
            for (int age = 0; age < 300; age++)
                foreach (var sword in FirstSeveranceImpalingSwords.At(step, age, 4000, 4000))
                {
                    AssertEqual(true, float.IsFinite(sword.Extension) && sword.Extension is >= 0 and <= 1, "finite insertion");
                    if (age <= sword.Fire || age >= sword.Retract) AssertEqual(false, sword.Live, "forecast/retraction harmless");
                    if (age == sword.Fire + 6) AssertEqual(1f, sword.Extension, "six-tick insertion complete");
                    if (sword.Live && sword.Extension < .8f)
                    {
                        var actual = sword.FullRay with { Length = sword.FullRay.Length * sword.Extension };
                        float y = sword.FullRay.Y + sword.FullRay.DirectionY * (actual.Length + 70);
                        AssertEqual(false, actual.Intersects(actual.X, y, 10, 21), "unreached far tip not damaging");
                    }
                }
    }
}
