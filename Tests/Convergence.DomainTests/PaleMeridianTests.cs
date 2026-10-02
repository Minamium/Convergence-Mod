using System;
using System.Collections.Generic;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static readonly int[] MeridianNoteTicks =
        { 12, 36, 60, 84, 108, 126, 144, 162, 180, 192, 204, 216, 228, 237, 246, 255, 264, 270, 276, 282, 288, 294 };

    [DomainTest("Pale Meridian build: four seats, 22 notes on a falling cadence ladder, then the wind")]
    private static void PaleMeridianBuildScore()
    {
        AssertEqual("108,180,228,264", string.Join(",", PaleMeridianScore.Seats), "seats");
        AssertEqual(92, PaleMeridianScore.Launch(0), "first part launch");
        AssertEqual(248, PaleMeridianScore.Launch(3), "last part launch");
        for (int i = 1; i < PaleMeridianScore.PartCount - 1; i++)
            AssertEqual(true, PaleMeridianScore.Seats[i + 1] - PaleMeridianScore.Seats[i] < PaleMeridianScore.Seats[i] - PaleMeridianScore.Seats[i - 1],
                "the gaps between seats shrink (72, 48, 36)");
        for (int i = 1; i < PaleMeridianScore.Cadence.Length; i++)
            AssertEqual(true, PaleMeridianScore.Cadence[i] < PaleMeridianScore.Cadence[i - 1], "each seated part fires faster");
        var notes = new List<int>();
        for (int age = 0; age < PaleMeridianScore.Ignite; age++)
        {
            MeridianShot shot = PaleMeridianScore.Shot(age);
            if (shot == MeridianShot.Note) notes.Add(age);
            else AssertEqual(MeridianShot.None, shot, $"only notes before the overcharge (age {age})");
        }
        AssertEqual(string.Join(",", MeridianNoteTicks), string.Join(",", notes), "the 22 build notes");
        AssertEqual(PaleMeridianScore.NoteCount, notes.Count, "note count");
        AssertEqual(PaleMeridianScore.NoteCount, PaleMeridianScore.BuildPhrase.Length, "one phrase step per build note");
        int[] perStage = new int[PaleMeridianScore.PartCount + 1];
        foreach (int age in notes) perStage[PaleMeridianScore.Parts(age)]++;
        AssertEqual("4,4,4,4,6", string.Join(",", perStage), "notes per stage");
        for (int i = 0; i < PaleMeridianScore.PartCount; i++)
        {
            int seat = PaleMeridianScore.Seats[i];
            AssertEqual(MeridianShot.Note, PaleMeridianScore.Shot(seat), "every part seats on a note (a downbeat)");
            AssertEqual(4 * (i + 1), PaleMeridianScore.NotesBefore(seat), "a seat starts a new bar of the tune");
            AssertEqual(i, PaleMeridianScore.Parts(seat - 1), "not seated a tick early");
            AssertEqual(i + 1, PaleMeridianScore.Parts(seat), "seated on its tick");
        }
        foreach (int step in PaleMeridianScore.BuildPhrase)
            AssertEqual(true, step >= 0 && step < DollWeaponTuning.LadderLength, "build notes stay on the ladder");
        foreach (int step in PaleMeridianScore.OverchargePhrase)
            AssertEqual(true, step >= 0 && step < DollWeaponTuning.LadderLength, "overcharge notes stay on the ladder");
        AssertEqual(PaleMeridianScore.BuildPhrase[^1], DollWeaponTuning.LadderLength - 1, "the build runs up to C7 into the wind");

        int[] wind = new int[PaleMeridianScore.WindSteps];
        for (int step = 1; step <= PaleMeridianScore.WindSteps; step++) wind[step - 1] = PaleMeridianScore.WindTick(step);
        AssertEqual("318,324,328,331,334,337,339,341,343,345,347,348", string.Join(",", wind), "ratchet ticks");
        for (int i = 2; i < wind.Length; i++) AssertEqual(true, wind[i] - wind[i - 1] <= wind[i - 1] - wind[i - 2], "ratchet gaps never grow");
        float previous = PaleMeridianScore.KeyAngle(PaleMeridianScore.KeyRise);
        for (float age = PaleMeridianScore.KeyRise; age <= PaleMeridianScore.Ignite + 40; age += .125f)
        {
            float angle = PaleMeridianScore.KeyAngle(age);
            AssertEqual(true, angle >= previous - 1e-4f, "the key only turns forward");
            AssertEqual(true, angle - previous <= MathF.PI / 4 * .2f, $"the key turns continuously (age {age})");
            previous = angle;
        }
        AssertDollNear(3 * MathF.PI, PaleMeridianScore.KeyAngle(PaleMeridianScore.Ignite), 1e-4, "12 steps of 45 degrees before the release");
        AssertDollNear(3 * MathF.PI + MathF.PI / 4, PaleMeridianScore.KeyAngle(PaleMeridianScore.Ignite + 2), 1e-4, "45 degrees every 2 ticks once released");
        AssertEqual(0, PaleMeridianScore.KeyFrame(PaleMeridianScore.KeyAngle(PaleMeridianScore.Ignite)), "the key faces front at the release");
        AssertEqual(1, PaleMeridianScore.KeyFrame(PaleMeridianScore.KeyAngle(PaleMeridianScore.Ignite + 2)), "then 45 degrees");
        AssertEqual(2, PaleMeridianScore.KeyFrame(PaleMeridianScore.KeyAngle(PaleMeridianScore.Ignite + 4)), "then edge-on");
        AssertEqual(1f, PaleMeridianScore.KeyRiseAmount(PaleMeridianScore.KeyRise + PaleMeridianScore.KeyRiseTicks), "risen after 6 ticks");
        AssertEqual(0f, PaleMeridianScore.KeyRiseAmount(PaleMeridianScore.KeyRise), "hidden at the rise tick");
    }

    [DomainTest("Pale Meridian overcharge: a round every 3 ticks, a heavy every 36 on the downbeat note")]
    private static void PaleMeridianOvercharge()
    {
        int rounds = 0, heavies = 0;
        for (int age = PaleMeridianScore.Ignite; age < PaleMeridianScore.Ignite + 600; age++)
        {
            MeridianShot shot = PaleMeridianScore.Shot(age);
            if (shot != MeridianShot.None) rounds++;
            if (shot == MeridianShot.Heavy)
            {
                heavies++;
                AssertEqual(0, (age - PaleMeridianScore.Ignite) % 36, "heavy rounds every 36 ticks from the release");
                AssertEqual(true, PaleMeridianScore.Note(age) >= 0, "a heavy round rings a note");
                AssertEqual(0, (age - PaleMeridianScore.Ignite) / PaleMeridianScore.OverchargeNotePeriod % 4, "on the bar's downbeat");
            }
            int note = PaleMeridianScore.Note(age);
            AssertEqual((age - PaleMeridianScore.Ignite) % PaleMeridianScore.OverchargeNotePeriod == 0, note >= 0,
                "overcharge notes only on the 9-tick grid");
            if (note >= 0) AssertEqual(true, shot != MeridianShot.None, "every note is a real round");
        }
        AssertEqual(200, rounds, "200 rounds per 600 ticks of overcharge");
        AssertEqual(17, heavies, "17 heavy rounds per 600 ticks");
        AssertEqual(PaleMeridianScore.OverchargePhrase[0], PaleMeridianScore.Note(PaleMeridianScore.Ignite), "the tune restarts at the release");
    }

    [DomainTest("Pale Meridian release tiers and finisher factors: 6x at the tier-3 node, lattice from tier 2")]
    private static void PaleMeridianTiers()
    {
        (int Age, int Tier)[] edges = { (0, 0), (107, 0), (108, 1), (227, 1), (228, 2), (347, 2), (348, 3), (5000, 3) };
        foreach (var (age, tier) in edges) AssertEqual(tier, PaleMeridianScore.Tier(age), $"tier at age {age}");
        AssertEqual("0,1.5,2.5,4", string.Join(",", PaleMeridianScore.MeridianFactors), "meridian factors");
        AssertEqual("0,0,1.5,2", string.Join(",", PaleMeridianScore.LatticeFactors), "lattice factors");
        AssertEqual(false, PaleMeridianScore.HasFinisher(0), "no finisher before the first part seats");
        AssertEqual(false, PaleMeridianScore.HasLattice(1), "no lattice before the third part seats");
        AssertEqual(true, PaleMeridianScore.HasLattice(2), "lattice from tier 2");
        AssertDollNear(6, PaleMeridianScore.MeridianFactor(3) + PaleMeridianScore.LatticeFactor(3), 1e-6, "6x at the tier-3 node");
        AssertEqual(12_012, PaleMeridianScore.MeridianRaw(3, 2002) + PaleMeridianScore.LatticeRaw(3, 2002), "12,012 raw at base 2002");
    }

    [DomainTest("Pale Meridian budget is within 3% of the 0.3.70 baseline per cycle, sustained and from a cold press")]
    private static void PaleMeridianBudget()
    {
        int ranged = RitualArmamentRules.Damage(RitualArmamentKind.Ranged);
        AssertEqual(DollWeaponBudget.MeridianBuild.BaseDamage, ranged, "base damage is frozen");
        double build = PaleMeridianScore.BuildRaw(ranged);
        AssertEqual(28_622d, build, "22 notes x0.65 = 1301 each");
        AssertEqual(true, DollWeaponBudget.Within(build, DollWeaponBudget.MeridianBuild.CycleRaw),
            $"build {build} within 3% of {DollWeaponBudget.MeridianBuild.CycleRaw}");
        AssertEqual(true, PaleMeridianScore.Ignite == DollWeaponBudget.MeridianBuild.CycleTicks, "the build lasts as long as before");
        double period = PaleMeridianScore.OverchargePeriodRaw(ranged);
        AssertEqual(true, DollWeaponBudget.Within(period, DollWeaponBudget.MeridianOverdrive.CycleRaw), $"overcharge per 36 ticks {period}");
        double perSecond = DollWeaponBudget.PerSecond(period, PaleMeridianScore.HeavyPeriod);
        AssertEqual(true, DollWeaponBudget.Within(perSecond, DollWeaponBudget.MeridianOverdrive.PerSecond), $"sustained {perSecond}/s");

        // Cold press: tick 0 is the press, a round fired at age a lands on tick a - 1, a release after age r lands its
        // meridian MeridianFirstHit ticks and its lattice LatticeFirstHit(node) ticks after tick r. Releasing at r
        // >= 600 is plain holding. The nearest node lands the lattice soonest.
        int window = DollWeaponBudget.ColdWindowTicks;
        double Raw(int release, int tick, float node)
        {
            double raw = 0;
            int age = tick + 1;
            if (age <= release) raw += PaleMeridianScore.RoundRaw(age, ranged);
            int tier = PaleMeridianScore.Tier(release);
            if (tick == release + PaleMeridianLattice.MeridianFirstHit) raw += PaleMeridianScore.MeridianRaw(tier, ranged);
            if (tick == release + PaleMeridianLattice.LatticeFirstHit(node)) raw += PaleMeridianScore.LatticeRaw(tier, ranged);
            return raw;
        }
        double best = 0;
        int bestRelease = -1;
        foreach (float node in new[] { PaleMeridianLattice.NodeMin, 640f, PaleMeridianLattice.NodeMax })
        {
            double value = DollWeaponBudget.BestColdWindow(window + 100, (release, tick) => Raw(release, tick, node), out int release);
            if (value > best) { best = value; bestRelease = release; }
        }
        double holding = DollWeaponBudget.ColdWindow(tick => Raw(int.MaxValue / 2, tick, PaleMeridianLattice.NodeMin));
        AssertEqual(true, DollWeaponBudget.Within(holding, DollWeaponBudget.MeridianColdWindow), $"holding from a cold press {holding}");
        AssertEqual(true, best <= DollWeaponBudget.MeridianColdWindow * (1 + DollWeaponBudget.Tolerance),
            $"best cold-press window {best} (release at {bestRelease}) at most 3% over {DollWeaponBudget.MeridianColdWindow}");
        AssertEqual(true, DollWeaponBudget.Within(best, DollWeaponBudget.MeridianColdWindow), "best cold window within 3%");
        AssertEqual(144_859d, best, "the best timed release (both finisher hits inside the window)");
    }

    [DomainTest("Pale Meridian: releasing and rebuilding never beats holding the overcharge")]
    private static void PaleMeridianReleaseNeverBeatsHolding()
    {
        int ranged = RitualArmamentRules.Damage(RitualArmamentKind.Ranged);
        double sustained = PaleMeridianScore.OverchargePeriodRaw(ranged) / PaleMeridianScore.HeavyPeriod;
        double worst = 0;
        for (int release = 0; release < 4000; release++)
        {
            // The shortest cycle: the release tick, the pack-away, then an immediate new press.
            double average = PaleMeridianScore.CycleRaw(release, ranged) / (release + PaleMeridianScore.StowTicks);
            worst = Math.Max(worst, average / sustained);
            AssertEqual(true, average < sustained, $"release at {release} averages {average:F1}/tick against {sustained:F1}");
            // An item swap removes the gun at once (no pack-away): still below holding with a new press one tick later.
            double swapped = PaleMeridianScore.CycleRaw(release, ranged) / (release + 1);
            AssertEqual(true, swapped < sustained, $"release at {release} then a swap averages {swapped:F1}/tick against {sustained:F1}");
        }
        AssertEqual(true, worst < .94, $"the best release cycle stays well under holding ({worst:F3})");
        AssertDollNear(21.45, PaleMeridianScore.CycleRaw(PaleMeridianScore.Ignite, ranged) / ranged, .01,
            "release at the ignition: 14.30 + 1.15 + 6 = 21.45 B");
    }

    [DomainTest("Pale Meridian lattice is a small '#' and never a staff of four evenly spaced lines")]
    private static void PaleMeridianLatticeShape()
    {
        Span<MeridianLatticeLine> lines = stackalloc MeridianLatticeLine[PaleMeridianLattice.MaxLines];
        AssertEqual(0, PaleMeridianLattice.Lines(1, lines), "no lattice at tier 1");
        AssertEqual(3, PaleMeridianLattice.Lines(2, lines), "tier 2: two parallels and one perpendicular");
        AssertEqual(5, PaleMeridianLattice.Lines(3, lines), "tier 3: two parallels and three perpendiculars");
        foreach (int tier in new[] { 2, 3 })
        {
            int n = PaleMeridianLattice.Lines(tier, lines);
            float spacing = PaleMeridianLattice.Spacing(tier), half = PaleMeridianLattice.HalfSpan(tier);
            AssertEqual(tier == 3 ? 240f : 160f, half, "half span (480 / 320 px lines)");
            AssertEqual(tier == 3 ? 120f : 100f, spacing, "spacing");
            // Offsets of each family, the meridian (across 0) included with the parallels.
            var parallels = new List<float> { 0 };
            var perpendiculars = new List<float>();
            int rings = 0;
            for (int i = 0; i < n; i++)
            {
                MeridianLatticeLine line = lines[i];
                AssertEqual(half, line.HalfSpan, "every lattice line has the tier's span");
                if (line.Perpendicular) { perpendiculars.Add(line.Along); AssertEqual(0f, line.Across, "perpendiculars cross the meridian"); }
                else { parallels.Add(line.Across); AssertEqual(0f, line.Along, "parallels are centred on the node"); AssertEqual(true, line.Across != 0, "no parallel on the meridian"); }
                rings = Math.Max(rings, line.Ring);
                AssertEqual(line.Ring == 0, line.Perpendicular && line.Along == 0, "ring 0 is the perpendicular through the node");
            }
            AssertEqual(tier == 3 ? 2 : 1, rings, "ripple rings");
            foreach (List<float> family in new[] { parallels, perpendiculars })
            {
                family.Sort();
                AssertEqual(true, family.Count <= 3, "at most three lines in one direction");
                AssertEqual(false, HasEvenRun(family, 4), "never four or more evenly spaced parallel lines");
                for (int i = 0; i < family.Count; i++) AssertDollNear(-family[family.Count - 1 - i], family[i], 1e-4, "symmetric about the node");
            }
            AssertEqual(tier == 3 ? "-120,0,120" : "-100,0,100", string.Join(",", parallels), "parallel offsets (with the meridian)");
            AssertEqual(tier == 3 ? "-120,0,120" : "0", string.Join(",", perpendiculars), "perpendicular offsets");
        }
    }

    // True when `count` of the sorted offsets form an arithmetic progression.
    private static bool HasEvenRun(List<float> offsets, int count)
    {
        for (int a = 0; a < offsets.Count; a++)
        for (int b = a + 1; b < offsets.Count; b++)
        {
            float step = offsets[b] - offsets[a];
            int run = 2;
            float next = offsets[b] + step;
            for (int c = b + 1; c < offsets.Count; c++)
                if (MathF.Abs(offsets[c] - next) < .5f) { run++; next += step; }
            if (run >= count) return true;
        }
        return false;
    }

    [DomainTest("Pale Meridian live windows: nothing during the forecast or split, packets light and pass")]
    private static void PaleMeridianLiveWindows()
    {
        Vector2 origin = new(1000, 800), axis = Vector2.Normalize(new Vector2(3, -1));
        const float node = 640;
        Span<MeridianSegment> live = stackalloc MeridianSegment[PaleMeridianLattice.MaxSegments];
        float length = PaleMeridianLattice.Length(node);
        float lastHead = 0, lastTail = 0;
        for (int age = 0; age < PaleMeridianLattice.MeridianEnd; age++)
        {
            int n = PaleMeridianLattice.Live(origin, axis, node, 3, false, age, live);
            bool expected = age >= PaleMeridianLattice.MeridianFire && age < PaleMeridianLattice.MeridianFire + PaleMeridianLattice.MeridianLive;
            AssertEqual(expected ? 1 : 0, n, $"meridian live at age {age}");
            if (n == 0) continue;
            float tail = Vector2.Dot(live[0].A - origin, axis), head = Vector2.Dot(live[0].B - origin, axis);
            AssertEqual(true, head >= lastHead - 1e-3f && tail >= lastTail - 1e-3f, "the packet only moves outward");
            AssertEqual(true, head <= length + 1e-2f && tail >= -1e-3f, "the packet stays on the line");
            AssertEqual(14f, live[0].HalfWidth, "the meridian is 28 px wide");
            lastHead = head;
            lastTail = tail;
        }
        AssertDollNear(length, lastHead, .01, "the head reaches the far end (node + 640)");
        AssertEqual(0, PaleMeridianLattice.Live(origin, axis, node, 0, false, 12, live), "tier 0 has no meridian");

        int start = PaleMeridianLattice.LatticeStart(node);
        AssertEqual(true, start <= -PaleMeridianLattice.SplitLead - 10, "the lattice starts before the head reaches the node");
        for (int age = start; age < PaleMeridianLattice.LatticeEnd; age++)
        {
            int n = PaleMeridianLattice.Live(origin, axis, node, 3, true, age, live);
            int expected = 0;
            for (int ring = 0; ring < PaleMeridianLattice.Rings; ring++)
            {
                int s = age - ring * PaleMeridianLattice.RippleStep;
                if (s >= 0 && s < PaleMeridianLattice.LatticeLive) expected += ring == 0 ? 2 : 4;
            }
            AssertEqual(expected, n, $"lattice segments at age {age}");
            if (age < 0) AssertEqual(0, n, "the split is harmless");
            for (int i = 0; i < n; i++) AssertEqual(10f, live[i].HalfWidth, "lattice lines are 20 px wide");
        }
        AssertEqual(0, PaleMeridianLattice.Live(origin, axis, node, 1, true, 3, live), "no lattice at tier 1");
        // Every real spawn age passes the line's state check (Age > EarliestAge), across the whole node range.
        for (float n = PaleMeridianLattice.NodeMin; n <= PaleMeridianLattice.NodeMax; n += 1)
            AssertEqual(true, PaleMeridianLattice.LatticeStart(n) > PaleMeridianLattice.EarliestAge, $"lattice start at node {n} is accepted");
        AssertEqual(true, PaleMeridianLattice.LatticeStart(PaleMeridianLattice.NodeMax) > PaleMeridianLattice.EarliestAge,
            "the farthest node's lattice is accepted");
        // The split begins as the meridian's head passes the node.
        foreach (float n in new[] { PaleMeridianLattice.NodeMin, 640f, PaleMeridianLattice.NodeMax })
        {
            int splitTick = -PaleMeridianLattice.LatticeStart(n) - PaleMeridianLattice.SplitLead - 1;
            float headAge = PaleMeridianLattice.NodeAge(n);
            AssertEqual(true, splitTick + 1 >= headAge - 1e-3f && splitTick + 1 < headAge + 1, $"split right after the head passes node {n}");
            AssertEqual(true, PaleMeridianLattice.LatticeFirstHit(n) > PaleMeridianLattice.MeridianFirstHit, "the lattice fires after the meridian");
        }
    }

    [DomainTest("Pale Meridian collision: node boxes are hit, far boxes never, same at every rotation, SAT matches sampling")]
    private static void PaleMeridianCollision()
    {
        Span<MeridianSegment> live = stackalloc MeridianSegment[PaleMeridianLattice.MaxSegments];
        const float node = 500;
        for (int turn = 0; turn < 8; turn++)
        {
            float angle = turn * MathF.Tau / 8 + .2f;
            Vector2 axis = new(MathF.Cos(angle), MathF.Sin(angle)), across = new(-axis.Y, axis.X);
            Vector2 origin = new(4000, 3000), centre = origin + axis * node;
            bool meridianHit = false, latticeHit = false, farHit = false, pastHit = false, offLatticeHit = false;
            for (int age = 0; age < PaleMeridianLattice.MeridianEnd; age++)
            {
                int n = PaleMeridianLattice.Live(origin, axis, node, 3, false, age, live);
                for (int i = 0; i < n; i++)
                {
                    meridianHit |= PaleMeridianLattice.Hits(live[i], centre, new Vector2(20));
                    farHit |= PaleMeridianLattice.Hits(live[i], centre + across * 300, new Vector2(20));
                    pastHit |= PaleMeridianLattice.Hits(live[i], origin + axis * (PaleMeridianLattice.Length(node) + 60), new Vector2(20));
                }
            }
            for (int age = PaleMeridianLattice.LatticeStart(node); age < PaleMeridianLattice.LatticeEnd; age++)
            {
                int n = PaleMeridianLattice.Live(origin, axis, node, 3, true, age, live);
                for (int i = 0; i < n; i++)
                {
                    latticeHit |= PaleMeridianLattice.Hits(live[i], centre, new Vector2(20));
                    // Inside a '#' cell, between the lines, nothing is lit.
                    offLatticeHit |= PaleMeridianLattice.Hits(live[i], centre + axis * 60 + across * 60, new Vector2(10));
                }
            }
            AssertEqual(true, meridianHit && latticeHit, $"a 40x40 box at the node takes both (turn {turn})");
            AssertEqual(false, farHit, "300 px across from the node the meridian never hits");
            AssertEqual(false, pastHit, "past the line's end nothing hits");
            AssertEqual(false, offLatticeHit, "the gaps of the '#' are safe");
        }
        // The separating-axis test agrees with dense point sampling of the rectangle.
        var random = new Random(9071);
        int disagreements = 0;
        for (int i = 0; i < 500; i++)
        {
            Vector2 a = new((float)random.NextDouble() * 400, (float)random.NextDouble() * 400);
            Vector2 b = a + new Vector2((float)random.NextDouble() * 300 - 150, (float)random.NextDouble() * 300 - 150);
            var segment = new MeridianSegment(a, b, 2 + (float)random.NextDouble() * 14);
            Vector2 boxCentre = new((float)random.NextDouble() * 500 - 50, (float)random.NextDouble() * 500 - 50);
            Vector2 boxHalf = new(4 + (float)random.NextDouble() * 30, 4 + (float)random.NextDouble() * 30);
            bool sat = PaleMeridianLattice.Hits(segment, boxCentre, boxHalf);
            bool sampled = SampledOverlap(segment, boxCentre, boxHalf, out bool marginal);
            if (sat != sampled && !marginal) disagreements++;
        }
        AssertEqual(0, disagreements, "SAT and dense sampling agree away from the boundary");
    }

    // Samples the segment's rectangle on a fine grid; `marginal` when the answer depends on sub-sample detail.
    private static bool SampledOverlap(MeridianSegment segment, Vector2 centre, Vector2 half, out bool marginal)
    {
        Vector2 delta = segment.B - segment.A;
        float length = delta.Length();
        Vector2 u = delta / length, v = new(-u.Y, u.X);
        bool inside = false, near = false;
        const float step = .25f;
        for (float s = 0; s <= length; s += step)
        for (float w = -segment.HalfWidth; w <= segment.HalfWidth; w += step)
        {
            Vector2 p = segment.A + u * s + v * w;
            float dx = MathF.Abs(p.X - centre.X) - half.X, dy = MathF.Abs(p.Y - centre.Y) - half.Y;
            if (dx <= 0 && dy <= 0) inside = true;
            if (MathF.Abs(MathF.Max(dx, dy)) < .6f) near = true;
        }
        marginal = near;
        return inside;
    }

    [DomainTest("Pale Meridian node clamps and line state validation")]
    private static void PaleMeridianNodeAndValidity()
    {
        AssertEqual(160f, PaleMeridianLattice.Node(50), "near cursor clamps to 160");
        AssertEqual(1200f, PaleMeridianLattice.Node(5000), "far cursor clamps to 1200");
        AssertEqual(160f, PaleMeridianLattice.Node(float.NaN), "non-finite input falls back to the nearest node");
        AssertEqual(1840f, PaleMeridianLattice.Length(1200), "the line overshoots the node by 640 px");
        Vector2 origin = new(100, 100), axis = Vector2.UnitX;
        AssertEqual(true, PaleMeridianLattice.Valid(origin, axis, 640, 3, true), "a tier-3 lattice is valid");
        AssertEqual(false, PaleMeridianLattice.Valid(origin, axis * 2, 640, 3, false), "a non-unit direction is rejected");
        AssertEqual(false, PaleMeridianLattice.Valid(origin, axis, 2000, 3, false), "a node out of range is rejected");
        AssertEqual(false, PaleMeridianLattice.Valid(new Vector2(float.NaN, 0), axis, 640, 3, false), "a non-finite origin is rejected");
        AssertEqual(false, PaleMeridianLattice.Valid(origin, axis, 640, 1, true), "a tier-1 lattice is rejected");
        AssertEqual(false, PaleMeridianLattice.Valid(origin, axis, 640, 0, false), "a tier-0 meridian is rejected");
        AssertEqual(false, PaleMeridianLattice.Valid(origin, axis, 640, 7, false), "an unknown tier is rejected");
    }

    [DomainTest("Pale Meridian parts fly continuously onto their seats from in front of the owner, clear of the face")]
    private static void PaleMeridianPartFlight()
    {
        // Level aim to the right, both facings' frames being mirror images: the gun frame is the world with +y up.
        Vector2 pivot = new(2000, 1500), axis = Vector2.UnitX;
        Vector2 muzzle = PaleMeridianRig.World(pivot, axis, false, PaleMeridianRig.Muzzle);
        Vector2 Frame(Vector2 world) => new(world.X - pivot.X, pivot.Y - world.Y);
        for (int part = 0; part < PaleMeridianScore.PartCount; part++)
        {
            int launch = PaleMeridianScore.Launch(part), seat = PaleMeridianScore.Seats[part];
            Vector2 previous = PaleMeridianScore.PartOffset(launch, part);
            AssertEqual(PaleMeridianScore.PartStart(part), previous, "each part starts at its start point");
            Vector2 centre = Frame(DollSpritePlacement.World(DollArtAnchors.MeridianParts.Seats[part] + PaleMeridianRig.PartSize[part] * .5f,
                DollArtAnchors.MeridianGun.Muzzle, muzzle, 0, DollFlip.None));
            Vector2 start = centre + previous;
            AssertEqual(true, start.X >= 40 && start.Y >= centre.Y + 10, $"part {part} appears ahead of the owner and above its seat");
            for (float age = launch; age <= seat + 4; age += .0625f)
            {
                Vector2 offset = PaleMeridianScore.PartOffset(age, part);
                AssertEqual(true, Vector2.Distance(offset, previous) <= 3.5f, $"part {part} moves continuously at age {age}");
                previous = offset;
                // The owner's eyes and brow: within 10 px of the centre line, 9-21 px above the centre.
                Vector2 at = centre + offset;
                AssertEqual(false, MathF.Abs(at.X) <= 10 && at.Y >= 9 && at.Y <= 21, $"part {part} flies clear of the face at age {age}");
            }
            AssertEqual(Vector2.Zero, PaleMeridianScore.PartOffset(seat, part), "seated exactly on its tick");
            AssertEqual(Vector2.Zero, PaleMeridianScore.PartOffset(seat + 100, part), "and stays seated");
            AssertEqual(true, PaleMeridianScore.PartOffset(seat - 3, part).Length() <= 3.01f, "only the 3 px settle remains");
        }
    }

    [DomainTest("Pale Meridian art fits the design anchors: muzzle, grip and key seat within one dot")]
    private static void PaleMeridianArtFit()
    {
        Vector2 muzzleTexel = DollArtAnchors.MeridianGun.Muzzle;
        AssertEqual(DollArtAnchors.MeridianGun.Muzzle, DollArtAnchors.MeridianBare.Muzzle, "bare gun shares the muzzle");
        AssertEqual(DollArtAnchors.MeridianGun.Grip, DollArtAnchors.MeridianBare.Grip, "bare gun shares the grip");
        AssertEqual(DollArtAnchors.MeridianGun.KeySeat, DollArtAnchors.MeridianBare.KeySeat, "bare gun shares the key seat");
        AssertEqual(3, DollArtAnchors.MeridianGun.K, "the gun is the k = 3 rung");
        AssertEqual(174f, DollArtAnchors.MeridianGun.Width * DollSpritePlacement.WorldPerTexel, "174 px long, never rescaled");
        // The shared rule: within one dot, and a dot is 2 world px whatever the export rung k.
        float tolerance = DollSpritePlacement.WorldPerTexel;
        Vector2 pivot = new(2000, 1500);
        for (int turn = 0; turn < 16; turn++)
        {
            float angle = turn * MathF.Tau / 16 + .05f;
            Vector2 axis = new(MathF.Cos(angle), MathF.Sin(angle));
            foreach (float gravity in new[] { 1f, -1f })
            {
                bool mirrored = PaleMeridianRig.Mirrored(axis, gravity);
                DollFlip flip = mirrored ? DollFlip.Vertical : DollFlip.None;
                Vector2 muzzle = PaleMeridianRig.World(pivot, axis, mirrored, PaleMeridianRig.Muzzle);
                AssertDollNear(PaleMeridianRig.MuzzleAt(pivot, axis), muzzle, 1e-3f, "the design muzzle is on the aim");
                Vector2 Drawn(Vector2 texel) => DollSpritePlacement.World(texel, muzzleTexel, muzzle, angle, flip);
                AssertDollNear(muzzle, Drawn(muzzleTexel), tolerance, "drawn muzzle on the design muzzle");
                AssertDollNear(PaleMeridianRig.World(pivot, axis, mirrored, PaleMeridianRig.Grip), Drawn(DollArtAnchors.MeridianGun.Grip),
                    tolerance, $"drawn grip on the design grip (turn {turn}, gravity {gravity})");
                AssertDollNear(PaleMeridianRig.World(pivot, axis, mirrored, PaleMeridianRig.KeySeat), Drawn(DollArtAnchors.MeridianGun.KeySeat),
                    tolerance, $"drawn key seat on the design key seat (turn {turn}, gravity {gravity})");
                // The gun's top side (the key seat) stays above its aim axis from the owner's point of view.
                Vector2 top = Drawn(DollArtAnchors.MeridianGun.KeySeat) - muzzle;
                float side = axis.X * top.Y - axis.Y * top.X;
                AssertEqual(mirrored ? side > 0 : side < 0, true, "never drawn upside down");
            }
        }
        // The parts' seats and the key's foot lie on their canvases.
        foreach (Vector2 seat in DollArtAnchors.MeridianParts.Seats)
            AssertEqual(true, seat.X >= 0 && seat.Y >= 0 && seat.X < DollArtAnchors.MeridianGun.Width && seat.Y < DollArtAnchors.MeridianGun.Height,
                "part seats are on the gun canvas");
        AssertEqual(PaleMeridianScore.PartCount, DollArtAnchors.MeridianParts.Seats.Length, "one seat per part");
        AssertEqual(PaleMeridianScore.PartCount, PaleMeridianRig.PartSize.Length, "one size per part");
        foreach (Vector2 size in PaleMeridianRig.PartSize)
            AssertEqual(true, size.X >= 1 && size.Y >= 1 && size.X <= DollArtAnchors.MeridianParts.FrameWidth
                && size.Y <= DollArtAnchors.MeridianParts.FrameHeight, "each part fits its cell");
        AssertEqual(4 * DollArtAnchors.MeridianParts.FrameWidth, DollArtAnchors.MeridianParts.Width, "four part cells");
        AssertEqual(4 * DollArtAnchors.MeridianKey.FrameWidth, DollArtAnchors.MeridianKey.Width, "four key frames");
        AssertEqual(new Vector2(DollArtAnchors.MeridianKey.FrameWidth / 2f, DollArtAnchors.MeridianKey.FrameHeight),
            DollArtAnchors.MeridianKey.ShaftBottom, "the key stands on the bottom centre of its frame");
    }
}
