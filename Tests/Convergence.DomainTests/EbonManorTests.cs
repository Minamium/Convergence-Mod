using System;
using System.IO;
using System.Linq;
using Convergence.Client.Encounters.EbonManor;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Encounters.EbonManor;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ebon beat grid follows 125 BPM without drift and every beat tick is unique")]
    private static void EbonBeatGrid()
    {
        AssertEqual((int)MathF.Round(58 * EbonRules.BeatTicks), EbonRules.Intro, "two-beat pickup plus fourteen intro bars to the A drop");
        AssertEqual((int)MathF.Round(8 * EbonRules.BeatTicks), EbonRules.ActTwoLead, "two lead-in bars");
        AssertEqual((int)MathF.Round(12 * EbonRules.BeatTicks), EbonRules.FinaleLead, "three lead-in bars");
        const int epoch = 1234;
        int previous = epoch - 1;
        for (int beat = 0; beat < EbonRules.CycleBeats * 6; beat++)
        {
            int tick = EbonRules.Beat(epoch, beat);
            AssertEqual(true, tick > previous, "strictly increasing beats");
            AssertEqual(true, MathF.Abs(tick - epoch - beat * EbonRules.BeatTicks) <= .5f, "rounded, not accumulated");
            AssertEqual(beat, EbonRules.BeatAt(epoch, tick), "inverse on the beat");
            if (tick - 1 > previous) AssertEqual(-1, EbonRules.BeatAt(epoch, tick - 1), "no cue between beats");
            previous = tick;
        }
        AssertEqual(-1, EbonRules.BeatAt(epoch, epoch - 1), "no cue before the epoch");
    }

    [DomainTest("Ebon act floors hold the acts apart and scale every roster within the codec")]
    private static void EbonActFloors()
    {
        int last = 0;
        for (int members = 1; members <= EbonRules.Members; members++)
        {
            int max = EbonRules.Life(members);
            AssertEqual(true, max > last, "more players, more life"); last = max;
            int one = EbonRules.Floor(max, EbonPhase.ActOne), two = EbonRules.Floor(max, EbonPhase.ActTwo);
            AssertEqual(true, one > two && two > 0, "Act One floor above Act Two floor");
            AssertEqual(0, EbonRules.Floor(max, EbonPhase.Finale), "Finale can reach zero");
            AssertEqual((int)Math.Ceiling(max * EbonRules.ActTwoThreshold), one, "62 percent");
            AssertEqual((int)Math.Ceiling(max * EbonRules.FinaleThreshold), two, "28 percent");
            var state = EbonExample() with { Members = Enumerable.Range(0, members).Select(i => new EbonMember((byte)i, Guid.NewGuid(), true, false)).ToArray(), MaxLife = max, Life = max };
            AssertEqual(max, EbonRead(state)!.Value.MaxLife, "codec accepts the full roster life");
        }
    }

    [DomainTest("Ebon furniture and chandelier kinematics solve their own flight and fall times")]
    private static void EbonKinematics()
    {
        for (float length = 1; length < 6000; length += 37)
        {
            int ticks = EbonRules.PropFlightTicks(length);
            AssertEqual(true, EbonRules.PropTravel(ticks) >= length - .01f, "arrives by the solved tick");
            AssertEqual(true, EbonRules.PropTravel(ticks - 1) < length, "not a tick earlier");
            AssertEqual(true, EbonRules.PropTravel(ticks) >= EbonRules.PropTravel(ticks - 1), "monotone");
        }
        for (float height = 40; height < 1200; height += 13)
        {
            int ticks = EbonRules.ChandelierFallTicks(height);
            AssertEqual(true, EbonRules.ChandelierDrop(ticks) >= height - .01f, "lands by the solved tick");
            AssertEqual(true, EbonRules.ChandelierDrop(ticks - 1) < height, "not a tick earlier");
        }
        AssertEqual(0f, EbonRules.PropTravel(0), "no travel before fire");
        AssertEqual(0f, EbonRules.ChandelierDrop(-3), "no drop before fire");
    }

    [DomainTest("Ebon schedule stays inside sixteen bars and every hazard plan fits its codec")]
    private static void EbonScheduleCodec()
    {
        var field = RaidFieldGeometry.FromGround(8000, 6000);
        float width = field.Right - field.Left, height = field.Bottom - field.Top;
        foreach (var phase in new[] { EbonPhase.ActOne, EbonPhase.ActTwo, EbonPhase.Finale })
        {
            var table = EbonSchedule.Table(phase);
            AssertEqual(true, table.Count > 0, "each act attacks");
            foreach (var (bar, beat, _) in table)
                AssertEqual(true, bar is >= 0 and < 16 && beat is >= 0 and < 4, "inside the cycle");
            int epoch = 3000;
            for (int b = 0; b < EbonRules.CycleBeats * 2; b++)
                foreach (var raw in EbonSchedule.At(phase, b))
                {
                    var cue = EbonSchedule.Stitch(raw, b);
                    int born = EbonRules.Beat(epoch, b);
                    (EbonAttackKind Kind, int Fire, int End, float Length, float Width, byte Variant, float Spin)? plan = cue switch
                    {
                        EbonCue.Throw => (EbonAttackKind.Thread, EbonRules.Beat(epoch, b + 2), EbonRules.Beat(epoch, b + 2) + EbonRules.PropFlightTicks(5800) + 1, 5800, EbonRules.PropRadius, (byte)6, 0),
                        EbonCue.Chandelier => (EbonAttackKind.Chandelier, EbonRules.Beat(epoch, b + 3),
                            EbonRules.Beat(epoch, b + 3) + EbonRules.ChandelierFallTicks(height - 30 - 270) + EbonRules.BurstTicks, height - 30, EbonRules.ChandelierHalfWidth, (byte)1, 0),
                        EbonCue.LoomRising or EbonCue.LoomFalling => (EbonAttackKind.Loom, EbonRules.Beat(epoch, b + 3), EbonRules.Beat(epoch, b + 3) + EbonRules.LoomLive, 3000, EbonRules.LoomRadius, (byte)0, 0),
                        EbonCue.ShearsAcross or EbonCue.ShearsDown => (EbonAttackKind.Shears, EbonRules.Beat(epoch, b + 2), EbonRules.Beat(epoch, b + 2) + EbonRules.ShearsLive, 3000, EbonRules.ShearsRadius, (byte)0, 0),
                        EbonCue.Waltz => (EbonAttackKind.Waltz, EbonRules.Beat(epoch, b + 4), EbonRules.Beat(epoch, b + 16), EbonRules.SpokeReach, EbonRules.SpokeRadius, (byte)8, .0102f),
                        EbonCue.Web => (EbonAttackKind.Web, EbonRules.Beat(epoch, b + 4), EbonRules.Beat(epoch, b + 4) + EbonRules.WebLive, 3000, EbonRules.WebRadius,
                            (byte)(EbonRules.FinaleWebStrands - 1), 0),
                        _ => null,
                    };
                    if (plan is not { } p) continue;
                    // The web's last strand is born latest; it must still carry a full warning.
                    int bornAt = cue == EbonCue.Web ? EbonRules.WebBorn(epoch, b, EbonRules.FinaleWebStrands - 1) : born;
                    var attack = new EbonAttackPlan(Guid.NewGuid(), 3, p.Kind, bornAt, p.Fire, p.End, field.CenterX, field.Top + 30, .64f, p.Length, p.Width, 340, p.Variant, p.Spin);
                    using var m = new MemoryStream();
                    using (var w = new BinaryWriter(m, System.Text.Encoding.UTF8, true)) attack.Write(w);
                    m.Position = 0;
                    AssertEqual(attack, EbonAttackPlan.Read(new BinaryReader(m))!.Value, $"{phase} {cue} plan roundtrip");
                }
        }
        AssertEqual(EbonCue.Spread, EbonSchedule.Stitch(EbonCue.Stack, EbonRules.CycleBeats), "stitches alternate by cycle");
        AssertEqual(EbonCue.Stack, EbonSchedule.Stitch(EbonCue.Stack, 0), "first cycle keeps the table call");
        var bad = new EbonAttackPlan(Guid.NewGuid(), 3, EbonAttackKind.Thread, 100, 110, 200, 1, 1, 0, 100, 40, 300, 7);
        foreach (var invalid in new[] { bad, bad with { Fire = 130, Variant = 7 }, bad with { Fire = 130, Variant = 1, Spin = .01f },
                     bad with { Kind = EbonAttackKind.Waltz, Fire = 130, Variant = 2 }, bad with { Kind = EbonAttackKind.Loom, Fire = 130, Variant = 1 },
                     bad with { Fire = 130, Variant = 0, End = 600 }, bad with { Kind = EbonAttackKind.Web, Fire = 130, Variant = 16 } })
        {
            bool rejected = false;
            using var m = new MemoryStream();
            using (var w = new BinaryWriter(m, System.Text.Encoding.UTF8, true)) invalid.Write(w);
            m.Position = 0;
            try { EbonAttackPlan.Read(new BinaryReader(m)); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "malformed plan rejected");
        }
    }

    [DomainTest("Ebon glides between stations continuously and spins every waltz from the centre")]
    private static void EbonStationsAndWaltz()
    {
        var field = RaidFieldGeometry.FromGround(8000, 6000);
        const int epoch = 2000;
        var last = EbonRules.BossPosition(field, epoch, epoch);
        AssertEqual(field.CenterX, last.X, "starts centred");
        for (float age = epoch; age < epoch + EbonRules.BarTicks * 48; age += .5f)
        {
            var p = EbonRules.BossPosition(field, epoch, age);
            AssertEqual(true, MathF.Abs(p.X - last.X) < 6 && MathF.Abs(p.Y - last.Y) < 1, "no station jump");
            AssertEqual(true, p.X > field.Left + 200 && p.X < field.Right - 200 && p.Y > field.Top && p.Y < field.Bottom, "inside the hall");
            last = p;
        }
        foreach (var phase in new[] { EbonPhase.ActTwo, EbonPhase.Finale })
            for (int beat = 0; beat < EbonRules.CycleBeats * 3; beat++)
                if (EbonSchedule.At(phase, beat).Contains(EbonCue.Waltz))
                    for (int age = EbonRules.Beat(epoch, beat + 4); age < EbonRules.Beat(epoch, beat + 16); age++)
                        AssertEqual(true, MathF.Abs(field.CenterX - EbonRules.BossPosition(field, epoch, age).X) < .5f, $"{phase} waltz spins at the centre station");
    }

    [DomainTest("Ebon projection rejects regression malformed acts and every truncated prefix")]
    private static void EbonProjectionCodec()
    {
        var a = EbonExample() with { Stage = EbonStage.Performance, Age = 2400 };
        int max = a.MaxLife;
        var b = a with { Age = 2600, Phase = EbonPhase.ActTwo, PhaseAt = 2600, Life = EbonRules.Floor(max, EbonPhase.ActOne) };
        var c = b with { Age = 3400, Phase = EbonPhase.Finale, PhaseAt = 3400, Life = EbonRules.Floor(max, EbonPhase.ActTwo) };
        var d = c with { Age = 3900, Stage = EbonStage.Victory, EndAt = 3900, Life = 0 };
        AssertEqual(true, b.CanReplace(a), "acts advance"); AssertEqual(false, a.CanReplace(b), "no act regression");
        AssertEqual(false, (b with { Age = 2700, PhaseAt = 2650 }).CanReplace(b), "act clock cannot restart");
        AssertEqual(false, (c with { Life = c.Life + 1, Age = 3401 }).CanReplace(c), "no healing");
        AssertEqual(true, d.CanReplace(c), "committed victory");
        AssertEqual(b.PhaseAt + EbonRules.ActTwoLead, b.Epoch, "second act epoch after its lead-in");
        AssertEqual(true, b.Transition(b.PhaseAt + 10) && !b.Transition(b.Epoch), "lead-in is a transition");
        AssertEqual(false, (b with { Age = b.PhaseAt + 5 }).Live, "no hazards during the lead-in");
        foreach (var state in new[] { a, b, c, d })
        {
            AssertEqual(state.Phase, EbonRead(state)!.Value.Phase, "roundtrip");
            using var m = new MemoryStream();
            using (var w = new BinaryWriter(m, System.Text.Encoding.UTF8, true)) state.WriteEnvelope(w);
            byte[] bytes = m.ToArray();
            for (int count = 0; count < bytes.Length; count++)
            {
                bool rejected = false;
                try { using var r = new BinaryReader(new MemoryStream(bytes, 0, count)); EbonState.ReadEnvelope(r); }
                catch (Exception e) when (e is EndOfStreamException or InvalidDataException) { rejected = true; }
                AssertEqual(true, rejected, "truncated projection");
            }
        }
        foreach (var invalid in new[] { a with { PhaseAt = 10 }, b with { PhaseAt = a.UnlockAt - 1 }, b with { Stage = EbonStage.Countdown },
                     c with { Phase = (EbonPhase)7 }, d with { EndAt = -1 }, a with { Life = max + 1 }, a with { UnlockAt = a.MusicStart + 5 } })
        {
            bool rejected = false;
            try { EbonRead(invalid); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid projection rejected");
        }
    }

    [DomainTest("Ebon replicas accept the whole lifecycle from deployment to the curtain")]
    private static void EbonLifecycleReplication()
    {
        var fight = Guid.NewGuid();
        var roster = new[] { new EbonMember(0, Guid.NewGuid(), false, false) };
        int max = EbonRules.Life(1), music = 827;
        var deploy = new EbonState(fight, 30, -1, -1, -1, EbonStage.Deployment, roster, 8000, 6000, max, max, EbonPhase.ActOne, -1);
        var ready = deploy with { Age = 140, Stage = EbonStage.Ready, Members = new[] { roster[0] with { Ready = true } } };
        var countdown = ready with { Age = music, MusicStart = music, UnlockAt = music + EbonRules.Intro, Stage = EbonStage.Countdown, PhaseAt = music };
        var act = countdown with { Age = music + EbonRules.Intro + 60, Stage = EbonStage.Performance, Life = max - 10 };
        var two = act with { Age = act.Age + 900, Phase = EbonPhase.ActTwo, PhaseAt = act.Age + 900, Life = EbonRules.Floor(max, EbonPhase.ActOne) };
        var finale = two with { Age = two.Age + 900, Phase = EbonPhase.Finale, PhaseAt = two.Age + 900, Life = EbonRules.Floor(max, EbonPhase.ActTwo) };
        var curtain = finale with { Age = finale.Age + 900, Stage = EbonStage.Victory, EndAt = finale.Age + 900, Life = 0 };
        var chain = new[] { deploy, ready, countdown, act, two, finale, curtain };
        for (int i = 1; i < chain.Length; i++)
        {
            AssertEqual(true, chain[i].CanReplace(chain[i - 1]), $"step {i} replaces its predecessor on a client");
            AssertEqual(chain[i].Stage, EbonRead(chain[i])!.Value.Stage, $"step {i} parses");
            AssertEqual(false, chain[i - 1].CanReplace(chain[i]), $"step {i} cannot be undone");
        }
        AssertEqual(false, (countdown with { Age = countdown.Age + 5, MusicStart = music + 1, UnlockAt = music + 1 + EbonRules.Intro, PhaseAt = music + 1 }).CanReplace(countdown), "music clock is immutable once set");
    }

    [DomainTest("Ebon waltz steps on every beat yet keeps its average speed and never turns back")]
    private static void EbonWaltzSteps()
    {
        float previous = 0;
        for (float t = 0; t < EbonRules.BeatTicks * 16; t += .25f)
        {
            float turn = EbonRules.WaltzTurn(t);
            AssertEqual(true, turn >= previous - 1e-4f, "never turns back");
            AssertEqual(true, MathF.Abs(turn - t) <= EbonRules.WaltzLilt * EbonRules.BeatTicks / MathF.Tau + 1e-3f, "bounded lilt");
            previous = turn;
        }
        for (int beat = 1; beat <= 12; beat++)
            AssertEqual(true, MathF.Abs(EbonRules.WaltzTurn(beat * EbonRules.BeatTicks) - beat * EbonRules.BeatTicks) < .01f, "plan speed on every beat");
        AssertEqual(true, EbonRules.WaltzRate(.001f) > EbonRules.WaltzRate(EbonRules.BeatTicks / 2), "fastest on the beat");
        AssertEqual(0f, EbonRules.WaltzTurn(-5), "still before fire");
    }

    [DomainTest("Ebon web strands run wall to wall and keep a pending gathering circle clear")]
    private static void EbonWebStrands()
    {
        var field = RaidFieldGeometry.FromGround(8000, 6000);
        var gather = new System.Numerics.Vector2(field.CenterX, field.Bottom - 220);
        float clear = EbonStitchRules.StackRadius + EbonRules.WebRadius + EbonRules.WebMargin;
        bool OnWall(System.Numerics.Vector2 p) => MathF.Abs(p.X - field.Left) < .01f || MathF.Abs(p.X - field.Right) < .01f
            || MathF.Abs(p.Y - field.Top) < .01f || MathF.Abs(p.Y - field.Bottom) < .01f;
        for (int beat = 0; beat < 400; beat += 7)
            foreach (var withGather in new[] { false, true })
            {
                var strands = EbonWeb.Strands(field, EbonWeb.Seed(Guid.Parse("6f0b8c1e-3a8e-4a5e-9c34-1d2a0c5b7e90"), beat), EbonRules.FinaleWebStrands,
                    withGather ? gather : null);
                AssertEqual(EbonRules.FinaleWebStrands, strands.Count, "every call finds its strands");
                foreach (var (a, b) in strands)
                {
                    AssertEqual(true, OnWall(a) && OnWall(b) && System.Numerics.Vector2.Distance(a, b) >= EbonWeb.MinimumLength, "wall to wall across the hall");
                    if (withGather) AssertEqual(true, EbonWeb.Distance(gather, a, b) >= clear, "gathering circle stays clear");
                }
            }
        var again = EbonWeb.Strands(field, EbonWeb.Seed(Guid.Parse("6f0b8c1e-3a8e-4a5e-9c34-1d2a0c5b7e90"), 21), 8, null);
        var once = EbonWeb.Strands(field, EbonWeb.Seed(Guid.Parse("6f0b8c1e-3a8e-4a5e-9c34-1d2a0c5b7e90"), 21), 8, null);
        AssertEqual(true, again.SequenceEqual(once), "deterministic per call");
    }

    [DomainTest("Ebon entrance landmarks sit on the intro's bars and the drop is the attack epoch")]
    private static void EbonIntroLandmarks()
    {
        var s = EbonExample();
        AssertEqual(s.UnlockAt, EbonIntro.Tick(s, EbonIntro.Drop), "drop tick is the unlock");
        AssertEqual(s.MusicStart, EbonIntro.Tick(s, -.5f), "the pickup starts with the music");
        float[] marks = { EbonIntro.Groove, EbonIntro.Threads, EbonIntro.Burst, EbonIntro.Break, EbonIntro.Bloom, EbonIntro.Gather, EbonIntro.Hush, EbonIntro.Drop };
        for (int i = 1; i < marks.Length; i++) AssertEqual(true, marks[i] > marks[i - 1], "landmarks in order");
        AssertEqual(true, EbonHall.Light(s, EbonIntro.Tick(s, .5f)).Candles == 0 && EbonHall.Light(s, EbonIntro.Tick(s, 4.1f)).Candles == 1, "candles light with the groove");
    }

    [DomainTest("Ebon stitches punish only the unmet call and their plans are bounded")]
    private static void EbonStitchRulesAndCodec()
    {
        var center = new System.Numerics.Vector2(9000, 7000);
        var inside = new[] { center, center + new System.Numerics.Vector2(40, 0), center - new System.Numerics.Vector2(0, 30) };
        AssertEqual(0, EbonStitchRules.Resolve(EbonStitchKind.Stack, center, inside, 0b111, 0b111).Sum(), "everyone gathered");
        var apart = new[] { center, center + new System.Numerics.Vector2(900, 0), center - new System.Numerics.Vector2(900, 0) };
        AssertEqual(true, EbonStitchRules.Resolve(EbonStitchKind.Stack, center, apart, 0b111, 0b111).All(x => x > 0), "missed gathering is shared");
        AssertEqual(0, EbonStitchRules.Resolve(EbonStitchKind.Spread, center, apart, 0b111, 0b111).Sum(), "spread apart is safe");
        AssertEqual(true, EbonStitchRules.Resolve(EbonStitchKind.Spread, center, inside, 0b111, 0b111).Sum() > 0, "overlap tears");
        var plan = new EbonStitchPlan(Guid.NewGuid(), 4, EbonStitchKind.Spread, 0b101, 500, 500 + EbonStitchRules.Warning, 500 + EbonStitchRules.Warning + 84, center);
        using var m = new MemoryStream();
        using (var w = new BinaryWriter(m, System.Text.Encoding.UTF8, true)) plan.Write(w);
        m.Position = 0;
        AssertEqual(plan, EbonStitchPlan.Read(new BinaryReader(m))!.Value, "stitch roundtrip");
    }

    [DomainTest("Ebon hall tear rips once per beat and completes on the A-prime downbeat")]
    private static void EbonHallTear()
    {
        AssertEqual(0f, EbonHall.Tear(0), "whole hall at the act change");
        AssertEqual(0f, EbonHall.Tear(EbonRules.BarTicks - .5f), "first bar holds");
        float previous = 0;
        for (float t = 0; t < EbonRules.FinaleLead + 30; t += .25f)
        {
            float tear = EbonHall.Tear(t);
            AssertEqual(true, tear >= previous - 1e-6f && tear is >= 0 and <= 1, "monotone rip");
            previous = tear;
        }
        AssertEqual(true, EbonHall.Tear(EbonRules.FinaleLead - 1) < .9f, "the last rip belongs to the downbeat");
        AssertEqual(1f, EbonHall.Tear(EbonRules.FinaleLead), "open on the A-prime epoch tick");
        var s = EbonExample() with { Stage = EbonStage.Performance, Age = 5000, Phase = EbonPhase.Finale, PhaseAt = 4000, Life = 10 };
        AssertEqual(1f, EbonHall.Light(s, 5000).Tear, "stays open for the Finale");
        AssertEqual(0f, EbonHall.Light(EbonExample(), 100).Fade, "no hall before the music");
    }

    private static EbonState EbonExample() => new(Guid.NewGuid(), 500, 180, 180 + EbonRules.Intro, -1, EbonStage.Countdown,
        new[] { new EbonMember(0, Guid.NewGuid(), true, false) }, 8000, 6000, EbonRules.Life(1), EbonRules.Life(1), EbonPhase.ActOne, 180);
    private static EbonState? EbonRead(EbonState s)
    {
        using var m = new MemoryStream();
        using (var w = new BinaryWriter(m, System.Text.Encoding.UTF8, true)) s.WriteEnvelope(w);
        m.Position = 0;
        return EbonState.ReadEnvelope(new BinaryReader(m));
    }
}
