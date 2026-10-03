using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static CrimsonChorusPlan ChorusExample() => new(Guid.Parse("f67a7558-f844-4266-b326-5cf7977fd508"),
        3, 500, 1, 1, CrimsonChorusKind.Stack, 15, 600, 824, 880, 8000, 6000, new(8000, 5440));

    [DomainTest("Scarlet Stack is harmless on success and charges only the missing fraction")]
    private static void ScarletChorusStackShares()
    {
        foreach (int count in new[] { 1, 2, 3, 4, 8 })
        {
            var positions = new CrimsonPoint[count]; Array.Fill(positions, new CrimsonPoint(8000, 5440));
            byte mask = (byte)((1 << count) - 1);
            var d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, positions[0], positions, mask, mask);
            foreach (int damage in d) AssertEqual(0, damage, "complete gather is harmless");
            positions[0] = new(8000 + CrimsonChorusRules.StackRadius + 1, 5440);
            d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, new(8000, 5440), positions, mask, mask);
            foreach (int damage in d) AssertEqual((900 + count - 1) / count, damage, "one missing share, bounded native failure budget");
        }
    }
    [DomainTest("Scarlet Stack boundary is inclusive and an empty gather cannot divide by zero")]
    private static void ScarletChorusStackBoundary()
    {
        CrimsonPoint[] p = { new(220, 0), new(0, 0) };
        var d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, new(0, 0), p, 3, 3);
        AssertEqual(0, d[0], "exact stack radius accepted");
        p[0] = p[1] = new(1000, 1000);
        d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, new(0, 0), p, 3, 3);
        AssertEqual(900, d[0], "no one gathered"); AssertEqual(900, d[1], "each assigned member receives a bounded verdict");
    }
    [DomainTest("Scarlet chorus removes dead members and never admits late or unannounced slots")]
    private static void ScarletChorusRosterLoss()
    {
        CrimsonPoint[] p = { new(0, 0), new(0, 0), new(0, 0), new(0, 0) };
        var d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, new(0, 0), p, 7, 13);
        AssertEqual(0, d[0], "both retained living members gathered");
        AssertEqual(0, d[1], "dead slot contributes no budget or damage");
        AssertEqual(0, d[2], "retained member"); AssertEqual(0, d[3], "late unannounced member excluded");
        d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, new(0, 0), p, 15, 0);
        foreach (int damage in d) AssertEqual(0, damage, "no damage after wipe");
    }
    [DomainTest("Scarlet Spread checks all pairs once and tangent or solo markers are harmless")]
    private static void ScarletChorusSpreadPairs()
    {
        float diameter = CrimsonChorusRules.SpreadRadius * 2;
        CrimsonPoint[] p = { new(0, 0), new(diameter, 0), new(diameter * 2, 0) };
        var d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Spread, new(0, 0), p, 7, 7);
        foreach (int damage in d) AssertEqual(0, damage, "tangent circles pass");
        p[1] = new(diameter - 1, 0);
        d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Spread, new(0, 0), p, 7, 7);
        AssertEqual(900, d[0], "first overlap"); AssertEqual(900, d[1], "second overlap"); AssertEqual(0, d[2], "uninvolved third member");
        Array.Fill(p, new CrimsonPoint(0, 0));
        d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Spread, new(0, 0), p, 7, 7);
        foreach (int damage in d) AssertEqual(900, damage, "multiple overlaps do not multiply the penalty");
        AssertEqual(0, CrimsonChorusRules.Resolve(CrimsonChorusKind.Spread, new(0, 0), new[] { new CrimsonPoint(0, 0) }, 1, 1)[0], "solo passes");
    }
    [DomainTest("Scarlet eight-player spread can fit complete separated markers in the unchanged field")]
    private static void ScarletChorusEightSpace()
    {
        var field = ChorusExample().Field; var positions = new CrimsonPoint[8];
        for (int i = 0; i < 8; i++) positions[i] = new(field.CenterX - 900 + (i % 4) * 600, field.CenterY - 280 + (i / 4) * 560);
        var d = CrimsonChorusRules.Resolve(CrimsonChorusKind.Spread, new(field.CenterX, field.CenterY), positions, 255, 255);
        for (int i = 0; i < 8; i++)
        {
            AssertEqual(0, d[i], "eight separated centers");
            float r=CrimsonChorusRules.SpreadRadius;
            AssertEqual(true, positions[i].X - r >= field.Left && positions[i].X + r <= field.Right
                && positions[i].Y - r >= field.Top && positions[i].Y + r <= field.Bottom, "complete markers inside field");
        }
    }
    [DomainTest("Scarlet chorus eight-beat calls and recovery stay bounded across the whole grid")]
    private static void ScarletChorusMusicalWindow()
    {
        for (int age = 0; age < 54000; age += 43)
        {
            var (born, fire, end) = CrimsonChorusRules.Schedule(age);
            var p = ChorusExample() with { Born = born + 1000, Fire = fire + 1000, End = end + 1000 };
            p.Validate();
            AssertEqual(true, fire - born >= 160 && end - fire >= 24, "readable musical call and isolated recovery");
        }
    }
    [DomainTest("Scarlet chorus calls and verdicts land on bar heads and the next phrase's pickup releases on its end")]
    private static void ScarletChorusScheduleGrid()
    {
        AssertEqual((900, 1125, 1238), CrimsonChorusRules.Schedule(900), "chorus asked on bar eight: called on the head, verdict two bars later");
        AssertEqual((1013, 1238, 1350), CrimsonChorusRules.Schedule(901), "a late tick waits for the next bar head");
        AssertEqual((0, 225, 338), CrimsonChorusRules.Schedule(0), "the grid starts at tick zero");
        for (int age = 0; age <= 70000; age += 7)
        {
            var (born, fire, end) = CrimsonChorusRules.Schedule(age);
            int bornBar = CrimsonMeter.BarAtOrAfter(born);
            AssertEqual(CrimsonMeter.BarTick(bornBar), born, "called on a bar head");
            AssertEqual(CrimsonMeter.BarTick(ScarletFirstBarAtOrAfter(age)), born, "the first bar head at or after the request");
            AssertEqual(CrimsonMeter.BarTick(bornBar + 2), fire, "verdict on the bar head eight beats after the call");
            AssertEqual(225, fire - born, "eight beats are exactly 225 ticks");
            AssertEqual(CrimsonMeter.BarTick(bornBar + 3), end, "one bar of recovery, ending on a bar head");
            AssertEqual(true, end - fire is 112 or 113, "the recovery is one bar (at least two beats)");
            AssertEqual(true, fire - born is >= 160 and <= 720 && end - fire is >= 24 and <= 180, "inside the descriptor bounds CrimsonChorusPlan accepts");
            var next = CrimsonChoreography.Create(end, 2, 1, true);
            AssertEqual(end, next.Start, "the next phrase opens exactly where the call ends");
            AssertEqual(end, next.Hits[0].Fire, "and its pickup crossflow releases there");
            AssertEqual(true, next.Hits[0].Warning >= fire + CrimsonChorusRules.ImpactTicks + 40, "its seals bloom two beats after the verdict, past the impact window");
        }
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonChorusRules.Schedule(-1), "negative request");
    }
    [DomainTest("Scarlet chorus and verdict codecs reject all truncated prefixes and malformed fields")]
    private static void ScarletChorusCodec()
    {
        var p = ChorusExample(); p.Validate();
        using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) p.Write(writer);
        byte[] bytes = stream.ToArray();
        using (var reader = new BinaryReader(new MemoryStream(bytes))) AssertEqual(p, CrimsonChorusPlan.Read(reader), "marker round trip");
        for (int n = 0; n < bytes.Length; n++)
        {
            bool fail = false; try { using var r = new BinaryReader(new MemoryStream(bytes, 0, n)); CrimsonChorusPlan.Read(r); } catch (Exception ex) when (ex is IOException or InvalidDataException) { fail = true; }
            AssertEqual(true, fail, "all marker prefixes reject");
        }
        var impact = new CrimsonChorusImpact(p, 2, 720);
        using var s = new MemoryStream(); using (var w = new BinaryWriter(s, System.Text.Encoding.UTF8, true)) impact.Write(w);
        bytes = s.ToArray();
        using (var r = new BinaryReader(new MemoryStream(bytes))) AssertEqual(impact, CrimsonChorusImpact.Read(r), "verdict round trip");
        for (int n = 0; n < bytes.Length; n++)
        {
            bool fail = false; try { using var r = new BinaryReader(new MemoryStream(bytes, 0, n)); CrimsonChorusImpact.Read(r); } catch (Exception ex) when (ex is IOException or InvalidDataException) { fail = true; }
            AssertEqual(true, fail, "all verdict prefixes reject");
        }
        foreach (var bad in new[] { p with { Kind = (CrimsonChorusKind)255 }, p with { Members = 0 }, p with { Fire = int.MinValue },
            p with { End = int.MaxValue }, p with { Center = new(float.NaN, 0) }, p with { Center = new(100, 100) }, p with { Source = 4 } })
        {
            bool fail = false; try { bad.Validate(); } catch (Exception ex) when (ex is IOException or InvalidDataException) { fail = true; }
            AssertEqual(true, fail, "invalid descriptor rejected");
        }
        AssertEqual(false, p.MatchesRoster(3), "bits cannot address missing native members");
        AssertEqual(true, p.MatchesRoster(4), "bounded roster");
        bool badImpact = false;
        try { (impact with { Member = 7 }).Validate(); } catch (Exception ex) when (ex is IOException or InvalidDataException) { badImpact = true; }
        AssertEqual(true, badImpact, "unannounced verdict target rejected");
    }
    [DomainTest("Scarlet chorus rejects nonfinite positions and invalid roster masks before resolution")]
    private static void ScarletChorusMalformedRoster()
    {
        bool rejected = false;
        try { CrimsonChorusRules.Resolve(CrimsonChorusKind.Spread, new(0, 0), new[] { new CrimsonPoint(float.NaN, 0) }, 1, 1); }
        catch (ArgumentException) { rejected = true; }
        AssertEqual(true, rejected, "NaN cannot win a distance comparison");
        rejected = false;
        try { CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, new(0, 0), new[] { new CrimsonPoint(0, 0) }, 2, 1); }
        catch (ArgumentException) { rejected = true; }
        AssertEqual(true, rejected, "unbounded mask");
    }
}
