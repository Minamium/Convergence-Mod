using System;
using System.Numerics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;

namespace Convergence.DomainTests;

// Shared Scarlet reward rules (docs/encounters/crimson-foundry/REWARDS.md). Per-weapon curves are tested in each
// weapon's own test file.
internal static partial class Program
{
    private static void AssertNear(float expected, float actual, float tolerance, string context)
        => AssertEqual(true, MathF.Abs(expected - actual) <= tolerance, $"{context} ({expected} vs {actual})");

    [DomainTest("Scarlet rewards space a release cascade in integer sixteenths of 128 BPM")]
    private static void ScarletRewardSixteenths()
    {
        int[] expected = { 0, 7, 14, 21, 28, 35, 42, 49, 56, 63, 70 };
        for (int k = 0; k < expected.Length; k++) AssertEqual(expected[k], CrimsonRewardRules.S(k), $"S({k})");
        for (int k = 0; k < 2000; k++)
            AssertEqual((int)Math.Round(k * 225d / 32, MidpointRounding.AwayFromZero), CrimsonRewardRules.S(k), $"S({k}) rounds k x 225/32");
        AssertEqual(28, CrimsonRewardRules.Beat, "a beat is S(4)");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonRewardRules.S(-1), "negative sixteenth");
        AssertEqual(51, CrimsonRewardRules.BarlineTick, "Final Barline at 16 + S(5)");
        AssertEqual(71, CrimsonRewardRules.RiverTick, "Black-Blood River at 8 + S(9)");
        AssertEqual(10, CrimsonRewardRules.HandSlamTick(0), "first hand at 10");
        AssertEqual(10 + 49, CrimsonRewardRules.HandSlamTick(7), "the eighth hand");
        AssertEqual(16 + 28, CrimsonRewardRules.StaffLineTick(4), "the fifth staff line");
    }

    [DomainTest("Scarlet reward tier and damage tables match the Ebon tier")]
    private static void ScarletRewardTier()
    {
        int[] damage = { 3600, 1900, 2400, 1000, 1800 }, use = { 2, 14, 18, 30, 18 };
        foreach (CrimsonRewardKind kind in Enum.GetValues<CrimsonRewardKind>())
        {
            AssertEqual(damage[(int)kind], CrimsonRewardRules.Damage(kind), $"{kind} damage");
            AssertEqual(use[(int)kind], CrimsonRewardRules.UseTicks(kind), $"{kind} use time");
            AssertEqual(kind == CrimsonRewardKind.Summon ? 0 : 8, CrimsonRewardRules.CritFor(kind), $"{kind} crit");
        }
        AssertEqual(1, CrimsonRewardRules.Scaled(1, .1f), "secondary hits never drop below 1");
        AssertEqual(1, CrimsonRewardRules.Scaled(-5, 2), "negative damage clamps");
        AssertEqual(4320, CrimsonRewardRules.Scaled(2400, 1.8f), "scaled from the live damage");
        AssertEqual(1500, CrimsonRewardRules.CovenantDamage, "Covenant item damage");
        AssertEqual(10, CrimsonRewardRules.CovenantMana, "Covenant mana");
        AssertEqual(100, Enum.GetValues<CrimsonRewardKind>().Length * CrimsonRewardRules.ReliquaryShareChance, "five equal 20% shares");
    }

    [DomainTest("Scarlet reward nominal budgets match the parity table")]
    private static void ScarletRewardNominalParity()
    {
        AssertNear(6.4f, CrimsonRewardRules.MeasureMultiplier, 1e-4f, "scythe measure");
        AssertEqual(100, CrimsonRewardRules.MeasureTicks, "scythe measure ticks");
        AssertNear(6f, CrimsonRewardRules.StaffReleaseMultiplier(5), 1e-4f, "full staff");
        AssertEqual(56, CrimsonRewardRules.StaffReleaseTicks(5), "full staff release");
        AssertNear(.8f, CrimsonRewardRules.StaffReleaseMultiplier(1), 1e-4f, "one line");
        AssertEqual(24, CrimsonRewardRules.StaffReleaseTicks(1), "one-line release");
        float scythe = CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.MeasureMultiplier + CrimsonRewardRules.StaffReleaseMultiplier(5),
            CrimsonRewardRules.MeasureTicks + CrimsonRewardRules.StaffReleaseTicks(5));
        AssertNear(286, scythe, .5f, "scythe 12.4x in 156 ticks");

        AssertNear(28.25f, CrimsonRewardRules.OrganBuildMultiplier(8) + CrimsonRewardRules.HymnMultiplier(8), 1e-4f, "organ full build");
        AssertEqual(136, CrimsonRewardRules.OrganBuildTicks(8) + CrimsonRewardRules.HymnUseTicks, "organ full build ticks");
        AssertNear(13f, CrimsonRewardRules.OrganBuildMultiplier(4) + CrimsonRewardRules.HymnMultiplier(4), 1e-4f, "four marks");
        AssertEqual(80, CrimsonRewardRules.OrganBuildTicks(4) + CrimsonRewardRules.HymnUseTicks, "four marks ticks");
        float organ = CrimsonRewardRules.PerTick(1900, 28.25f, 136);
        AssertNear(395, organ, .5f, "organ before ammo");

        AssertNear(23f, CrimsonRewardRules.BatonBuildMultiplier(8) + CrimsonRewardRules.TuttiMultiplier(8), 1e-4f, "baton full score");
        AssertEqual(168, CrimsonRewardRules.BatonBuildTicks(8) + CrimsonRewardRules.TuttiTicks, "baton full score ticks");
        AssertNear(329, CrimsonRewardRules.PerTick(2400, 23, 168), .5f, "baton");
        AssertEqual(64, CrimsonRewardRules.BatonMana * CrimsonRewardRules.MaxStrokes, "mana per score");

        AssertEqual(134, CrimsonRewardRules.CenserCycleTicks, "four-pour cycle");
        AssertNear(5.2f, CrimsonRewardRules.CenserCycleMultiplier, 1e-4f, "censer cycle");
        AssertNear(39, CrimsonRewardRules.PerTick(1000, 5.2f, 134), .5f, "censer per slot");

        AssertNear(37f, CrimsonRewardRules.QuillBuildMultiplier(8) + CrimsonRewardRules.SealedScoreMultiplier(8, true), 1e-4f, "full melody on the ink");
        AssertNear(23f, CrimsonRewardRules.QuillBuildMultiplier(8) + CrimsonRewardRules.SealedScoreMultiplier(8, false), 1e-4f, "off the ink");
        int unroll = (int)MathF.Round(400 / CrimsonRewardRules.ScoreSpeed) + CrimsonRewardRules.ScoreWindup;
        AssertEqual(170, CrimsonRewardRules.QuillBuildTicks(8) + unroll, "about 170 ticks");
        AssertNear(392, CrimsonRewardRules.PerTick(1800, 37, 170), .5f, "quill");

        AssertNear(400, CrimsonRewardRules.PerTick(CrimsonRewardRules.CovenantDamage, 16, 60), .01f, "Covenant low");
        AssertNear(500, CrimsonRewardRules.PerTick(CrimsonRewardRules.CovenantDamage, 20, 60), .01f, "Covenant high");

        // Ebon restated in the same unit (REWARDS.md#nominal-parity).
        AssertNear(281, CrimsonRewardRules.PerTick(3600, 15.9f, 204), .5f, "Moonshear");
        AssertNear(400, CrimsonRewardRules.PerTick(1900, 32, 152), .5f, "Moonloom Harp");
        AssertNear(333, CrimsonRewardRules.PerTick(2400, 40, 288), .5f, "Ebon Thimble");
        AssertNear(39, CrimsonRewardRules.PerTick(1000, 4.5f, 115), .5f, "Ballroom Chandelier");
        AssertNear(380, CrimsonRewardRules.PerTick(1800, 32, 152), 1.5f, "Severing Silk");
        AssertNear(470, CrimsonRewardRules.PerTick(10000, 10.8f, 230), .5f, "The Last Waltz");
    }

    [DomainTest("Scarlet partial builds are always worth less per tick than full ones")]
    private static void ScarletRewardPartialBuilds()
    {
        float FullScythe() => CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.MeasureMultiplier + CrimsonRewardRules.StaffReleaseMultiplier(5), 156);
        float swinging = CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.MeasureMultiplier, CrimsonRewardRules.MeasureTicks);
        float oneLine = CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.StaffReleaseMultiplier(1), CrimsonRewardRules.StaffReleaseTicks(1));
        AssertEqual(true, oneLine < swinging && swinging < FullScythe(), "a one-line release is worth less than swinging; a full staff more");
        for (int n = 1; n < 8; n++)
        {
            float organ = CrimsonRewardRules.PerTick(1900, CrimsonRewardRules.OrganBuildMultiplier(n) + CrimsonRewardRules.HymnMultiplier(n),
                CrimsonRewardRules.OrganBuildTicks(n) + CrimsonRewardRules.HymnUseTicks);
            AssertEqual(true, organ < CrimsonRewardRules.PerTick(1900, 28.25f, 136), $"organ {n} marks");
            float baton = CrimsonRewardRules.PerTick(2400, CrimsonRewardRules.BatonBuildMultiplier(n) + CrimsonRewardRules.TuttiMultiplier(n),
                CrimsonRewardRules.BatonBuildTicks(n) + CrimsonRewardRules.TuttiTicks);
            AssertEqual(true, baton < CrimsonRewardRules.PerTick(2400, 23, 168), $"baton {n} strokes");
            int unroll = (int)MathF.Round(400 / CrimsonRewardRules.ScoreSpeed) + CrimsonRewardRules.ScoreWindup;
            float quill = CrimsonRewardRules.PerTick(1800, CrimsonRewardRules.QuillBuildMultiplier(n) + CrimsonRewardRules.SealedScoreMultiplier(n, true),
                CrimsonRewardRules.QuillBuildTicks(n) + unroll);
            AssertEqual(true, quill < CrimsonRewardRules.PerTick(1800, 37, 170), $"quill {n} quills");
        }
        for (int n = 1; n < 5; n++)
            AssertEqual(true, CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.MeasureMultiplier + CrimsonRewardRules.StaffReleaseMultiplier(n),
                CrimsonRewardRules.MeasureTicks + CrimsonRewardRules.StaffReleaseTicks(n)) < FullScythe(), $"scythe {n} lines");
    }

    [DomainTest("Scarlet staff fills from the middle and only a full staff earns the barline")]
    private static void ScarletStaffShape()
    {
        int[] rows = { 0, -1, 1, -2, 2 };
        for (int k = 0; k < 5; k++) AssertEqual(rows[k], CrimsonRewardRules.StaffRow(k), $"line {k}");
        AssertEqual(14f, CrimsonRewardRules.StaffSpacing(20), "small targets clamp to 14");
        AssertEqual(36f, CrimsonRewardRules.StaffSpacing(400), "large targets clamp to 36");
        AssertEqual(20f, CrimsonRewardRules.StaffSpacing(80), "h / 4");
        AssertNear(CrimsonRewardRules.StaffLines * CrimsonRewardRules.StaffLineMultiplier, CrimsonRewardRules.StaffReleaseMultiplier(4) + CrimsonRewardRules.StaffLineMultiplier, 1e-4f, "no barline before five");
        AssertEqual(0f, CrimsonRewardRules.StaffReleaseMultiplier(0), "no lines, nothing");
        AssertEqual(true, CrimsonRewardRules.BarlineTick + CrimsonRewardRules.BarlineFall + CrimsonRewardRules.BarlineLive <= CrimsonRewardRules.StaffCutLife,
            "cuts end within 90 ticks of the cast");
        // Every line crosses any target at least 56 px tall; smaller ones take the middle three.
        for (float h = 56; h <= 400; h += 4)
        {
            float s = CrimsonRewardRules.StaffSpacing(h);
            AssertEqual(true, 2 * s <= h / 2, $"outer lines inside a {h} px target");
        }
    }

    [DomainTest("Scarlet hymn arms alternate outer and inner and never reach past 40 px")]
    private static void ScarletHymnArms()
    {
        float[] side = { -1, .5f, -.5f, 1, -1 };
        for (int k = 0; k < side.Length; k++) AssertEqual(side[k], CrimsonRewardRules.ArmSide(k), $"arm {k}");
        AssertEqual(-25f, CrimsonRewardRules.HandOffset(0, 100), "25% of the width");
        AssertEqual(40f, CrimsonRewardRules.HandOffset(3, 1000), "at most 40 px");
        AssertNear(20.25f, CrimsonRewardRules.HymnMultiplier(8), 1e-4f, "seven hands and the Clasp");
        AssertNear(15.75f, CrimsonRewardRules.HymnMultiplier(7), 1e-4f, "no Clasp below eight");
        AssertEqual(true, CrimsonRewardRules.HandSlamTick(CrimsonRewardRules.MaxHands - 1) + CrimsonRewardRules.HandLive + 24 <= CrimsonRewardRules.HandMaxLife,
            "the last hand and its scar end within 145 ticks");
    }

    [DomainTest("Scarlet censers line up behind the owner in rows of six")]
    private static void ScarletCenserIdle()
    {
        AssertEqual(new Vector2(-40, -70), CrimsonRewardRules.CenserIdleOffset(0, 1), "slot 0 behind a right-facing owner");
        AssertEqual(new Vector2(74, -78), CrimsonRewardRules.CenserIdleOffset(1, -1), "slot 1 behind a left-facing owner, 8 px higher");
        AssertEqual(new Vector2(-40, -110), CrimsonRewardRules.CenserIdleOffset(6, 1), "second row 40 px higher");
        AssertEqual(0f, CrimsonRewardRules.CenserSpread(0, 1, 50), "one censer centred");
        AssertEqual(-36f, CrimsonRewardRules.CenserSpread(0, 2, 200), "spread capped at 72");
        AssertEqual(36f, CrimsonRewardRules.CenserSpread(1, 2, 200), "symmetric");
        AssertEqual(-(32 + 96) / 3f, CrimsonRewardRules.CenserSpread(0, 3, 32), "narrow targets share (width + 96) / n");
    }

    [DomainTest("Scarlet stroke state codec round-trips exactly and rejects invalid floats")]
    private static void ScarletStrokeStateCodec()
    {
        foreach (var state in new[] { CrimsonStrokeState.Dormant, CrimsonStrokeState.Live, CrimsonStrokeState.Residue })
        {
            AssertEqual(true, CrimsonStrokeState.TryDecode(state.Encode(), out var back), $"{state.Kind} decodes");
            AssertEqual(state, back, $"{state.Kind} round trip");
        }
        for (int countdown = 0; countdown <= CrimsonStrokeState.MaxCountdown; countdown += 9)
            for (int rank = 0; rank <= CrimsonStrokeState.MaxRank; rank++)
                for (int cast = 0; cast <= CrimsonStrokeState.MaxCast; cast += 7)
                {
                    var state = CrimsonStrokeState.Scheduled(countdown, rank, cast);
                    float encoded = state.Encode();
                    AssertEqual(true, encoded == MathF.Floor(encoded) && encoded > 0 && encoded < 1 << 24, "an exact positive integer");
                    AssertEqual(true, CrimsonStrokeState.TryDecode(encoded, out var back) && back == state, "scheduled round trip");
                }
        var top = CrimsonStrokeState.Scheduled(127, 7, 63);
        AssertEqual(true, CrimsonStrokeState.TryDecode(top.Encode(), out var max) && max == top, "largest schedule");
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, -3f, .5f, top.Encode() + 1, -1.5f })
            AssertEqual(false, CrimsonStrokeState.TryDecode(bad, out _), $"rejects {bad}");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonStrokeState.Scheduled(128, 0, 0), "countdown has 7 bits");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonStrokeState.Scheduled(0, 8, 0), "rank has 3 bits");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonStrokeState.Scheduled(0, 0, 64), "cast has 6 bits");
        var tick = CrimsonStrokeState.Scheduled(2, 3, 4).Tick();
        AssertEqual(CrimsonStrokeState.Scheduled(1, 3, 4), tick, "counts down");
        AssertEqual(CrimsonStrokeState.Live, tick.Tick(), "ignites at zero");
        AssertEqual(CrimsonStrokeState.Dormant, CrimsonStrokeState.Dormant.Tick(), "dormant waits");
    }

    [DomainTest("Scarlet reward pair codec and serials are exact and bounded")]
    private static void ScarletRewardCodecs()
    {
        for (int x = -512; x <= 512; x += 31)
            for (int y = -512; y <= 512; y += 37)
            {
                float packed = CrimsonRewardRules.PackPair(x, y, CrimsonRewardRules.OffsetHalf);
                AssertEqual(true, packed == MathF.Floor(packed) && packed >= 0 && packed < 1 << 24, "exact in a float");
                AssertEqual(true, CrimsonRewardRules.TryUnpackPair(packed, CrimsonRewardRules.OffsetHalf, out int bx, out int by) && bx == x && by == y, "round trip");
            }
        float corner = CrimsonRewardRules.PackPair(512, 512, 512);
        AssertEqual(true, CrimsonRewardRules.TryUnpackPair(corner, 512, out int cx, out int cy) && cx == 512 && cy == 512, "corner");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonRewardRules.PackPair(513, 0, 512), "x out of range");
        foreach (float bad in new[] { float.NaN, -1f, .5f, corner + 1 })
            AssertEqual(false, CrimsonRewardRules.TryUnpackPair(bad, 512, out _, out _), $"rejects {bad}");
        AssertEqual(0, CrimsonRewardRules.NextSerial(1023), "serials wrap at 1024");
        AssertEqual(true, CrimsonRewardRules.SerialNewer(0, 1023), "0 follows 1023");
        AssertEqual(false, CrimsonRewardRules.SerialNewer(1023, 0), "1023 precedes 0");
        AssertEqual(false, CrimsonRewardRules.SerialNewer(5, 5), "a serial is not newer than itself");
        AssertEqual(true, CrimsonRewardRules.ValidInteger(3, 0, 5) && !CrimsonRewardRules.ValidInteger(3.5f, 0, 5)
            && !CrimsonRewardRules.Valid(float.NaN, 0, 1) && !CrimsonRewardRules.Valid(2, 0, 1), "ai validation");
    }

    [DomainTest("Scarlet reward capsule, disc and box tests are exact")]
    private static void ScarletRewardGeometry()
    {
        Vector2 min = new(0, 0), max = new(20, 40);
        AssertEqual(true, CrimsonRewardRules.CapsuleTouchesBox(new(-50, 20), new(50, 20), 0, min, max), "a line through the box");
        AssertEqual(true, CrimsonRewardRules.CapsuleTouchesBox(new(5, 5), new(6, 6), 0, min, max), "a segment inside");
        AssertEqual(false, CrimsonRewardRules.CapsuleTouchesBox(new(-50, -13), new(50, -13), 12, min, max), "13 px above, radius 12");
        AssertEqual(true, CrimsonRewardRules.CapsuleTouchesBox(new(-50, -12), new(50, -12), 12, min, max), "12 px above, radius 12");
        // Corner distance: the segment passes the box corner diagonally.
        Vector2 a = new(30, -10), b = new(50, 10);
        float d = CrimsonRewardRules.SegmentBoxDistance(a, b, min, max);
        AssertNear(MathF.Sqrt(200), d, 1e-3f, "closest to the corner (20, 0)");
        AssertEqual(true, CrimsonRewardRules.DiscTouchesBox(new(30, 20), 10, min, max), "disc touching the side");
        AssertEqual(false, CrimsonRewardRules.DiscTouchesBox(new(31, 20), 10, min, max), "disc 11 px away");
        AssertEqual(true, CrimsonRewardRules.CapsuleTouchesBox(new(30, 20), new(30, 20), 10, min, max), "a zero-length capsule is a disc");
        var random = new Random(7);
        for (int i = 0; i < 4000; i++)
        {
            Vector2 p = new(random.Next(-60, 80), random.Next(-60, 100)), q = new(random.Next(-60, 80), random.Next(-60, 100));
            float exact = CrimsonRewardRules.SegmentBoxDistance(p, q, min, max), sampled = float.MaxValue;
            for (int s = 0; s <= 400; s++) sampled = MathF.Min(sampled, CrimsonRewardRules.BoxDistance(Vector2.Lerp(p, q, s / 400f), min, max));
            AssertEqual(true, exact <= sampled + 1e-3f && sampled - exact < Vector2.Distance(p, q) / 400 + 1e-2f, "exact distance agrees with dense sampling");
        }
    }

    [DomainTest("Scarlet root ledger counts each segmented root once per part")]
    private static void ScarletRootLedger()
    {
        var ledger = new CrimsonRootLedger(4);
        AssertEqual(true, ledger.TryAdd(10), "first hit");
        AssertEqual(false, ledger.TryAdd(10), "a second segment of the same root");
        AssertEqual(true, ledger.TryAdd(11) && ledger.TryAdd(12) && ledger.TryAdd(13), "other roots");
        AssertEqual(false, ledger.TryAdd(14), "bounded");
        AssertEqual(false, ledger.TryAdd(-1), "invalid root");
        ledger.Clear();
        AssertEqual(true, ledger.TryAdd(10) && ledger.Count == 1, "cleared for the next part");
    }

    [DomainTest("Scarlet reward effect bounds and readability constants follow the spec")]
    private static void ScarletRewardBounds()
    {
        AssertEqual(256, CrimsonRewardRules.MaxInkPaths, "paths per frame");
        AssertEqual(8192, CrimsonRewardRules.MaxStripVertices, "strip vertices per frame");
        AssertEqual(24, CrimsonRewardRules.ReducedCount(48, true), "Reduced Effects halves droplets");
        AssertEqual(600, CrimsonRewardRules.ReducedCount(600, false), "normal particle cap");
        AssertNear(.398f, CrimsonRewardRules.Decibels(CrimsonRewardRules.RemoteShotDecibels), .001f, "-8 dB for other players");
        AssertEqual(.5f, CrimsonRewardRules.RemoteDormantOpacity, "other players' builds dimmed");
        AssertEqual(.85f, CrimsonRewardRules.RemoteLiveOpacity, "other players' releases");
        AssertEqual(0f, CrimsonRewardRules.InkOpen(0), "ink opens from nothing");
        AssertEqual(1f, CrimsonRewardRules.InkOpen(3), "fully open after 3 ticks");
        AssertEqual(1f, CrimsonRewardRules.InkOpen(40), "and stays open");
        // Reliquaries: centred on the ground, 36 px apart, 180 px up.
        AssertEqual(1000 - 72, CrimsonRewardRules.DropLeft(1000, 0, 4), "first of four");
        AssertEqual(1000 + 36, CrimsonRewardRules.DropLeft(1000, 3, 4), "last of four");
        AssertEqual(1000 - 18, CrimsonRewardRules.DropLeft(1000, 0, 1) + 0 * CrimsonRewardRules.DropSize, "a lone reliquary sits at the centre");
        AssertEqual(320, CrimsonRewardRules.DropTop(500), "180 px up");
    }
}
