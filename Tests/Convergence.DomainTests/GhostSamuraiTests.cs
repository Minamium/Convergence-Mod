using System;
using System.IO;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ghost Samurai actor snapshots reject stale fights and malformed packets")]
    private static void SamuraiActorWire()
    {
        var id = Guid.NewGuid();
        var state = new SamuraiActorSnapshot(id, 100, SamuraiPhase.Phase2, SamuraiAttack.GridSlash, SamuraiBeat.Telegraph, 70, 0, 2_400_000);
        AssertEqual(true, state.CanReplace(Guid.Empty, 0), "initial full state");
        AssertEqual(true, state.CanReplace(id, 99), "new authority time");
        AssertEqual(false, state.CanReplace(id, 101), "old authority time");
        AssertEqual(false, state.CanReplace(Guid.NewGuid(), 0), "different fight cannot overwrite live actor");
        using var stream = new MemoryStream(); state.Write(new BinaryWriter(stream)); byte[] data = stream.ToArray(); stream.Position = 0;
        AssertEqual(state, SamuraiActorSnapshot.Read(new BinaryReader(stream)), "all actor fields roundtrip");
        for (int n = 0; n < data.Length; n++)
        {
            bool rejected = false;
            try { SamuraiActorSnapshot.Read(new BinaryReader(new MemoryStream(data, 0, n))); } catch (IOException) { rejected = true; }
            AssertEqual(true, rejected, "truncated actor packet");
        }
        foreach (var malformed in new[] { state with { Fight = Guid.Empty }, state with { Phase = (SamuraiPhase)255 },
            state with { AttackTimer = -1 }, state with { TransitionRemaining = 91 }, state with { MaximumLife = int.MaxValue } })
        {
            using var packet = new MemoryStream(); malformed.Write(new BinaryWriter(packet)); packet.Position = 0;
            bool rejected = false;
            try { SamuraiActorSnapshot.Read(new BinaryReader(packet)); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "bounded actor data");
        }
    }

    [DomainTest("Ghost Samurai phase thresholds advance once and Phase3 keeps the enhanced pool")]
    private static void SamuraiPhases()
    {
        AssertEqual(SamuraiPhase.Phase1, GhostSamuraiRules.NextPhase(SamuraiPhase.Phase1, 661, 1000), "above P2 boundary");
        AssertEqual(SamuraiPhase.Phase2, GhostSamuraiRules.NextPhase(SamuraiPhase.Phase1, 660, 1000), "P2 boundary");
        AssertEqual(SamuraiPhase.Phase2, GhostSamuraiRules.NextPhase(SamuraiPhase.Phase1, 1, 1000), "burst does not skip P2");
        AssertEqual(SamuraiPhase.Phase2, GhostSamuraiRules.NextPhase(SamuraiPhase.Phase2, 331, 1000), "above P3 boundary");
        AssertEqual(SamuraiPhase.Phase3, GhostSamuraiRules.NextPhase(SamuraiPhase.Phase2, 330, 1000), "P3 boundary");
        AssertEqual(SamuraiPhase.Phase3, GhostSamuraiRules.NextPhase(SamuraiPhase.Phase3, 1, 1000), "no phase four");
        for (int choice = 0; choice < 4; choice++)
            AssertEqual(GhostSamuraiRules.SelectNextAttack(SamuraiPhase.Phase2, SamuraiAttack.Idle, choice),
                GhostSamuraiRules.SelectNextAttack(SamuraiPhase.Phase3, SamuraiAttack.Idle, choice), "P3 provisional continuation");
    }

    [DomainTest("Ghost Samurai selection never repeats and covers every permitted attack")]
    private static void SamuraiSelection()
    {
        foreach (SamuraiPhase phase in Enum.GetValues<SamuraiPhase>())
        {
            int count = phase == SamuraiPhase.Phase1 ? 3 : 4;
            for (int prior = 0; prior <= count; prior++)
            {
                var seen = new bool[5];
                for (int choice = 0; choice < count - (prior > 0 ? 1 : 0); choice++)
                {
                    int selected = (int)GhostSamuraiRules.SelectNextAttack(phase, (SamuraiAttack)prior, choice);
                    AssertEqual(true, selected > 0 && selected <= count && selected != prior, "permitted non-repeat");
                    AssertEqual(false, seen[selected], "one choice per alternative"); seen[selected] = true;
                }
                for (int i = 1; i <= count; i++) AssertEqual(i != prior, seen[i], "pool coverage");
            }
        }
    }

    [DomainTest("Ghost Samurai forecasts and collision share exact boundaries in every direction")]
    private static void SamuraiWarningGeometry()
    {
        for (int i = 0; i < 32; i++)
        {
            float angle = i * MathF.PI / 16, dx = MathF.Cos(angle), dy = MathF.Sin(angle);
            var h = new SamuraiHazard(SamuraiShape.Slash, 1000, 1000, dx, dy, 1400, 32, 0, 54, 64, 260);
            float x = h.X + dx * 700, y = h.Y + dy * 700;
            AssertEqual(true, h.IsValid, "finite normalized shape");
            AssertEqual(false, h.Hits(53, x, y, 10, 21), "last warning tick harmless");
            AssertEqual(true, h.Hits(54, x, y, 10, 21), "first live tick hits");
            AssertEqual(false, h.Hits(64, x, y, 10, 21), "end tick harmless");
            AssertEqual(false, h.Hits(54, x - dy * 70, y + dx * 70, 10, 21), "outside full warned width safe");
            AssertEqual(true, h.Hits(54, x - dy * 30, y + dx * 30, 1, 1), "inside edge hit");
        }
    }

    [DomainTest("Ghost Samurai grid leaves full player-sized safe cells for one to four players")]
    private static void SamuraiGridSafety()
    {
        for (int players = 1; players <= 4; players++)
        for (int slot = 0; slot < players; slot++)
        {
            float x = (slot % 2 == 0 ? -1 : 1) * GhostSamuraiRules.GridSpacing / 2;
            float y = (slot / 2 == 0 ? -1 : 1) * GhostSamuraiRules.GridSpacing / 2;
            foreach (bool vertical in new[] { true, false })
            for (int line = 0; line < (vertical ? GhostSamuraiRules.GridVerticalLineCount : GhostSamuraiRules.GridHorizontalLineCount); line++)
            {
                var h = GhostSamuraiRules.GridLine(vertical, line, 0, 0, 0);
                AssertEqual(true, h.IsValid, "expanded line fits bounded wire geometry");
                AssertEqual(false, h.Hits(h.Fire, x, y, 10, 21), "safe full hitbox");
                AssertEqual(true, h.Hits(h.Fire, h.X + h.DX * h.Length / 2, h.Y + h.DY * h.Length / 2, 10, 21), "every line damages");
                AssertEqual(false, h.Hits(h.Fire - 1, h.X, h.Y, 10, 21), "expanded warning harmless");
            }
        }
        var left = GhostSamuraiRules.GridLine(true, 0, 0, 0, 0);
        var right = GhostSamuraiRules.GridLine(true, GhostSamuraiRules.GridVerticalLineCount - 1, 0, 0, 0);
        var top = GhostSamuraiRules.GridLine(false, 0, 0, 0, 0);
        var bottom = GhostSamuraiRules.GridLine(false, GhostSamuraiRules.GridHorizontalLineCount - 1, 0, 0, 0);
        AssertEqual(GhostSamuraiRules.GridWidth, right.X - left.X, "outer columns span whole width");
        AssertEqual(GhostSamuraiRules.GridHeight, bottom.Y - top.Y, "outer rows span whole height");
        AssertEqual(true, GhostSamuraiRules.GridSpacing - GhostSamuraiRules.GridHalfWidth * 2 > 42, "full-height player clearance");
        AssertEqual(true, GhostSamuraiRules.MaximumHazards >= GhostSamuraiRules.GridVerticalLineCount
            + GhostSamuraiRules.GridHorizontalLineCount + GhostSamuraiRules.MaximumWisps, "full grid reserved even with maximum lingering wisps");
    }

    [DomainTest("Ghost Samurai wisps spread without targeting then home smoothly within speed and lifetime bounds")]
    private static void SamuraiWisps()
    {
        var h = new SamuraiHazard(SamuraiShape.Wisp, 400, 500, 1, 0, 0, 15, 72,
            72 + GhostSamuraiRules.SpreadDuration, 72 + GhostSamuraiRules.SpreadDuration + GhostSamuraiRules.WispLife, 200);
        var motion = SamuraiWispMotion.Spawn(h);
        AssertEqual(motion, motion.Advance(h, h.Born - 1, 0, 0), "future birth does not move");
        for (int age = h.Born; age < h.End; age++)
        {
            var previous = motion;
            motion = motion.Advance(h, age, 400, 2000);
            AssertEqual(true, motion.IsValid(h), "all ticks finite and speed bounded");
            AssertEqual(motion, motion.Advance(h, age, -1000, -1000), "one movement per authority tick");
            if (age < h.Fire)
            {
                AssertEqual(500f, motion.Y, "target below does not bend the outward spread");
                AssertEqual(400 + (age - h.Born) * GhostSamuraiRules.SpreadSpeed, motion.X, "outward speed");
                AssertEqual(false, motion.Hits(h, age, motion.X, motion.Y, 10, 21), "spread is harmless");
            }
            else
            {
                float dx = motion.VX - previous.VX, dy = motion.VY - previous.VY;
                AssertEqual(true, dx * dx + dy * dy < .101f, "bounded steering acceleration");
                AssertEqual(true, motion.Hits(h, age, motion.X, motion.Y, 10, 21), "authoritative moving core hits");
                AssertEqual(false, motion.Hits(h, age, motion.X, motion.Y + 50, 10, 21), "outside core safe");
                if (age == h.Fire) AssertEqual(true, motion.VX > 3 && motion.VY > 0 && motion.VY < .2f, "initial turn is gentle");
            }
        }
        AssertEqual(true, motion.Y > 900, "later homing closes toward target");
        AssertEqual(false, motion.Hits(h, h.End, motion.X, motion.Y, 10, 21), "expired core harmless");
        AssertEqual(motion, motion.Advance(h, h.End, 0, 0), "expired motion stops");
        AssertEqual(motion.VisualX(motion.Tick + GhostSamuraiRules.WispSyncInterval), motion.VisualX(motion.Tick + 1000), "client stops extrapolation if snapshots stall");
    }

    [DomainTest("Ghost Samurai directional cadence overlaps warnings without overlapping strikes or cutting off the fourth hit")]
    private static void SamuraiDirectionalTiming()
    {
        int spawns = 0, peak = 0;
        var hazards = new System.Collections.Generic.List<SamuraiHazard>();
        for (int tick = 0; tick <= GhostSamuraiRules.DirectionalDuration; tick++)
        {
            int step = GhostSamuraiRules.DirectionalSpawnStep(tick);
            if (step >= 0)
            {
                spawns++;
                hazards.Add(new(SamuraiShape.Slash, 0, 0, 1, 0, 1400, 32, tick,
                    tick + GhostSamuraiRules.SlashWarning, tick + GhostSamuraiRules.SlashWarning + GhostSamuraiRules.SlashLive, 260));
                if (step > 0) AssertEqual(true, tick < hazards[step - 1].Fire, "next warning precedes previous strike");
            }
            int live = 0, alive = 0;
            foreach (var h in hazards) { if (h.Live(tick)) live++; if (tick < h.End) alive++; }
            peak = Math.Max(peak, alive);
            AssertEqual(true, live <= 1, "four hits remain sequential");
            AssertEqual(live == 1, GhostSamuraiRules.DirectionalBeat(tick) == SamuraiBeat.Strike, "pose beat follows active slash rather than newest warning");
            float pose = GhostSamuraiRules.DirectionalPose(tick);
            AssertEqual(true, Math.Abs(pose - GhostSamuraiRules.DirectionalPose(tick - .001f)) < .002f, "blade pose continuous at every beat");
        }
        AssertEqual(4, spawns, "exactly four warning entities");
        AssertEqual(true, peak <= 2, "at most two slash entities coexist");
        AssertEqual(GhostSamuraiRules.RecoveryTime, GhostSamuraiRules.DirectionalDuration - hazards[3].End, "fourth live window finishes before recovery");
        AssertEqual(0f, GhostSamuraiRules.DirectionalPose(GhostSamuraiRules.DirectionalDuration), "last recoil reaches idle");
        AssertEqual(true, GhostSamuraiRules.AttackInterval(SamuraiPhase.Phase2) < GhostSamuraiRules.AttackInterval(SamuraiPhase.Phase1), "phase two has shorter interval");
    }

    [DomainTest("Ghost Samurai wisp snapshots round-trip and reject stale truncated or unbounded motion")]
    private static void SamuraiWispWire()
    {
        var h = new SamuraiHazard(SamuraiShape.Wisp, 400, 500, 1, 0, 0, 15, 72, 102, 282, 200);
        var motion = SamuraiWispMotion.Spawn(h).Advance(h, 73, 0, 0);
        AssertEqual(false, SamuraiWispMotion.Spawn(h).CanReplace(motion), "older full snapshot rejected");
        AssertEqual(true, motion.CanReplace(SamuraiWispMotion.Spawn(h)), "newer full snapshot accepted");
        using var stream = new MemoryStream(); motion.Write(new BinaryWriter(stream)); byte[] data = stream.ToArray(); stream.Position = 0;
        AssertEqual(motion, SamuraiWispMotion.Read(new BinaryReader(stream), h), "full trajectory roundtrip");
        for (int n = 0; n < data.Length; n++)
        {
            bool rejected = false;
            try { SamuraiWispMotion.Read(new BinaryReader(new MemoryStream(data, 0, n)), h); } catch (IOException) { rejected = true; }
            AssertEqual(true, rejected, "truncated motion snapshot rejected");
        }
        foreach (var invalid in new[] { motion with { Tick = h.Born - 1 }, motion with { Tick = h.End },
            motion with { X = float.NaN }, motion with { Y = float.PositiveInfinity }, motion with { VX = 5 }, motion with { VY = float.NaN } })
        {
            using var packet = new MemoryStream(); invalid.Write(new BinaryWriter(packet)); packet.Position = 0;
            bool rejected = false;
            try { SamuraiWispMotion.Read(new BinaryReader(packet), h); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid motion rejected before mutation");
        }
    }

    [DomainTest("Ghost Samurai dash is continuous and reaches its warned endpoint")]
    private static void SamuraiDash()
    {
        AssertEqual(0f, GhostSamuraiRules.DashProgress(0), "start");
        AssertEqual(1f, GhostSamuraiRules.DashProgress(GhostSamuraiRules.DashLive), "end");
        float previous = 0;
        for (int tick = 1; tick <= GhostSamuraiRules.DashLive; tick++)
        {
            float next = GhostSamuraiRules.DashProgress(tick);
            AssertEqual(true, next > previous && (next - previous) * GhostSamuraiRules.DashDistance < 90, "finite visible movement");
            previous = next;
        }
    }

    [DomainTest("Ghost Samurai hazard codec round-trips and rejects truncation and invalid fields")]
    private static void SamuraiCodec()
    {
        foreach (SamuraiShape shape in Enum.GetValues<SamuraiShape>())
        {
            var h = new SamuraiHazard(shape, 1000, 1200, 1, 0, 1400, 32, 100, 154, 164, 260);
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            h.Write(writer); byte[] payload = stream.ToArray(); stream.Position = 0;
            AssertEqual(h, SamuraiHazard.Read(new BinaryReader(stream)), "shape roundtrip");
            for (int size = 0; size < payload.Length; size++)
            {
                bool rejected = false;
                try { SamuraiHazard.Read(new BinaryReader(new MemoryStream(payload, 0, size))); }
                catch (IOException) { rejected = true; }
                AssertEqual(true, rejected, "truncated payload rejected");
            }
            foreach (var invalid in new[] { h with { X = float.NaN }, h with { DX = 5 }, h with { Fire = 1 },
                h with { Shape = (SamuraiShape)255 }, h with { Radius = -1 }, h with { End = h.Fire }, h with { Damage = int.MaxValue } })
            {
                AssertEqual(false, invalid.IsValid, "invalid shape validation");
                using var malformed = new MemoryStream(); invalid.Write(new BinaryWriter(malformed)); malformed.Position = 0;
                bool rejected = false;
                try { SamuraiHazard.Read(new BinaryReader(malformed)); } catch (InvalidDataException) { rejected = true; }
                AssertEqual(true, rejected, "invalid payload rejected before mutation");
            }
        }
    }
}
