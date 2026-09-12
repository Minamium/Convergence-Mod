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
            for (int line = -2; line <= 2; line++)
            {
                float offset = line * GhostSamuraiRules.GridSpacing;
                var vertical = new SamuraiHazard(SamuraiShape.Slash, offset, -720, 0, 1, 1440, 20, 0, 84, 96, 280);
                var horizontal = new SamuraiHazard(SamuraiShape.Slash, -720, offset, 1, 0, 1440, 20, 0, 84, 96, 280);
                AssertEqual(false, vertical.Hits(84, x, y, 10, 21) || horizontal.Hits(84, x, y, 10, 21), "safe full hitbox");
                AssertEqual(true, vertical.Hits(84, offset, 0, 10, 21), "vertical line actually damages");
            }
        }
    }

    [DomainTest("Ghost Samurai wisps wait then travel steadily without retargeting")]
    private static void SamuraiWisps()
    {
        var h = new SamuraiHazard(SamuraiShape.Wisp, 400, 500, 1, 0, 0, 15, 72, 102, 282, 200);
        AssertEqual(400f, h.CenterX(101), "stationary warning");
        AssertEqual(false, h.Hits(101, 400, 500, 10, 21), "warning harmless");
        AssertEqual(440f, h.CenterX(112), "ten ticks of bounded motion");
        AssertEqual(true, h.Hits(112, 440, 500, 10, 21), "core hits");
        AssertEqual(false, h.Hits(112, 440, 550, 10, 21), "outside circle safe");
        AssertEqual(false, h.Hits(282, h.CenterX(282), 500, 10, 21), "expired wisp harmless");
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
