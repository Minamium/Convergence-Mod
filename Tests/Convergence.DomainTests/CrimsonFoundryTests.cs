using System;
using System.IO;
using System.Text;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Crimson score preserves sample loop, warning lead and repeated offbeats")]
    private static void CrimsonScoreClock()
    {
        var score = CrimsonScore.Read(Encoding.UTF8.GetBytes("{\"SampleRate\":48000,\"LoopStartSample\":480000,\"LoopEndSample\":1440000,\"IntroTicks\":120,\"BeatTicks\":[300,600,900,1200,1500],\"Energy\":[0.4,0.9,0.95,0.9,0.8]}"));
        AssertEqual(480000, score.SampleAt(1800), "first loop does not replay intro");
        AssertEqual(480000, score.SampleAt(1800 + 1200 * 100), "no per-loop rounding drift");
        var fires = new System.Collections.Generic.HashSet<int>();
        int offbeats = 0;
        for (int age = 0; age < 3500; age++)
        {
            int now = age;
            score.Events(age, (id, fire, energy, offbeat) =>
            {
                AssertEqual(CrimsonScore.WarningTicks, fire - now, "full warning independent of attack density");
                AssertEqual(true, fires.Add(id), "cue emitted only once across loop");
                if (offbeat) offbeats++;
            });
        }
        AssertEqual(true, offbeats > 0 && fires.Count > 5, "strong passages include repeated offbeats");
    }

    [DomainTest("Crimson actor packets bound party size, epochs, clock and truncation")]
    private static void CrimsonActorCodec()
    {
        foreach (int count in new[] { 1, 3, 8 })
        {
            var members = new CrimsonMember[count];
            for (int i = 0; i < count; i++) members[i] = new((byte)i, Guid.NewGuid(), true, false);
            var state = new CrimsonState(Guid.NewGuid(), 300, 360, -1, CrimsonStage.Countdown, members, 8000, 6000);
            using var stream = new MemoryStream(); state.Write(new BinaryWriter(stream)); byte[] bytes = stream.ToArray(); stream.Position = 0;
            var restored = CrimsonState.Read(new BinaryReader(stream));
            AssertEqual(state.Fight, restored.Fight, "fight roundtrip");
            AssertEqual(state.MusicStart, restored.MusicStart, "scheduled music start roundtrip");
            AssertEqual(count, restored.Members.Length, "all members preserved, not nearest players");
            for (int i = 0; i < count; i++) AssertEqual(members[i], restored.Members[i], "membership identity roundtrip");
            for (int length = 0; length < bytes.Length; length++)
            {
                bool bad = false;
                try { CrimsonState.Read(new BinaryReader(new MemoryStream(bytes, 0, length))); } catch (IOException) { bad = true; }
                AssertEqual(true, bad, "all truncated packets fail");
            }
        }
        var duplicate = new CrimsonState(Guid.NewGuid(), 50, -1, -1, CrimsonStage.Ready,
            new[] { new CrimsonMember(0, Guid.NewGuid(), false, false), new CrimsonMember(0, Guid.NewGuid(), false, false) }, 8000, 6000);
        using var dup = new MemoryStream(); duplicate.Write(new BinaryWriter(dup)); dup.Position = 0;
        bool rejected = false; try { CrimsonState.Read(new BinaryReader(dup)); } catch (InvalidDataException) { rejected = true; }
        AssertEqual(true, rejected, "duplicate slot rejected before mutation");
    }

    [DomainTest("Crimson locked hazard codecs share visual width and native damage boundaries")]
    private static void CrimsonHazardCodec()
    {
        var hazard = new CrimsonHazard(Guid.NewGuid(), 2, 100, 160, 180, CrimsonShape.Slash, 4000, 5000, 1, 0, 1040, 22, 450);
        using var stream = new MemoryStream(); hazard.Write(new BinaryWriter(stream)); stream.Position = 0;
        AssertEqual(hazard, CrimsonHazard.Read(new BinaryReader(stream)), "geometry roundtrip");
        AssertEqual(false, hazard.Live(159.99f), "warning never damages");
        AssertEqual(0f, hazard.HitWidth(160), "pilot begins at zero width");
        AssertEqual(22f, hazard.HitWidth(163), "smooth opening reaches exact footprint");
        AssertEqual(false, hazard.Live(180), "end exclusive");
        foreach (var bad in new[] { hazard with { DX = float.NaN }, hazard with { Width = float.PositiveInfinity },
            hazard with { Fire = 110 }, hazard with { Boss = -1 }, hazard with { Fight = Guid.Empty } })
        {
            using var b = new MemoryStream(); bad.Write(new BinaryWriter(b)); b.Position = 0;
            bool rejected = false; try { CrimsonHazard.Read(new BinaryReader(b)); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid danger data rejected");
        }
    }

    [DomainTest("Crimson clients derive vulnerability from accepted stage and purge, not defaults")]
    private static void CrimsonDamageProjection()
    {
        var state = new CrimsonState(Guid.NewGuid(), 400, 100, -1, CrimsonStage.Ready,
            new[] { new CrimsonMember(0, Guid.NewGuid(), true, false) }, 8000, 6000);
        AssertEqual(false, state.Vulnerable(400), "preparation remains protected");
        state = state with { Stage = CrimsonStage.Performance };
        AssertEqual(true, state.Vulnerable(400), "client and server both expose native hurt");
        state = state with { PurgeTick = 500 };
        AssertEqual(false, state.Vulnerable(589.99f), "purge cannot be damaged early");
        AssertEqual(true, state.Vulnerable(590), "purge expiry unlocks every replica");
        AssertEqual(false, (state with { Stage = CrimsonStage.Victory }).Vulnerable(600), "ending protected");
        AssertEqual(false, default(CrimsonState).Vulnerable(600), "uninitialized actor is protected");
    }

    [DomainTest("Crimson shared pedestal footprint clips rays and contains players without drift")]
    private static void CrimsonFieldAndMotion()
    {
        var field = Convergence.Common.Raids.Arena.RaidFieldGeometry.FromGround(8000, 6000);
        AssertEqual(2560f, field.Right - field.Left, "same width as Doll");
        AssertEqual(1120f, field.Bottom - field.Top, "same height as Doll");
        AssertEqual(true, field.FitsWorld(8400, 2400), "validated grounded field");
        var clamped = field.ClampBody(0, 10000, 20, 42);
        AssertEqual(clamped, field.ClampBody(clamped.X, clamped.Y, 20, 42), "idempotent edge correction");
        foreach (float angle in new[] { 0f, .7853982f, 1.5707964f, 2.3561945f })
        {
            float dx = MathF.Cos(angle), dy = MathF.Sin(angle);
            AssertEqual(true, field.ClipAxis(field.CenterX, field.CenterY, dx, dy, out float first, out float last), "axis enters/exits field");
            AssertEqual(true, first < 0 && last > 0 && last - first < 4000, "full field is representable");
        }
        AssertEqual(false, field.ClipAxis(field.Right + 10, field.CenterY, 0, 1, out _, out _), "outside parallel lanes are not spawned");
        AssertEqual(0f, Convergence.Client.Encounters.CrimsonFoundry.CrimsonRigMotion.Recoil(-1), "no recoil before fire");
        AssertEqual(0f, Convergence.Client.Encounters.CrimsonFoundry.CrimsonRigMotion.ArmorTravel(-1), "no armor jump before cue");
        AssertEqual(1f, Convergence.Client.Encounters.CrimsonFoundry.CrimsonRigMotion.Assemble(70), "deployment settles continuously");
    }
}
