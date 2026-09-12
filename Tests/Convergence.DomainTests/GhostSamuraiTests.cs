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

    [DomainTest("Ghost Samurai paired cadence warns eight sequential strikes and preserves all four double hits")]
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
            AssertEqual(true, live <= 1, "eight hits remain sequential");
            AssertEqual(live == 1, GhostSamuraiRules.DirectionalBeat(tick) == SamuraiBeat.Strike, "pose beat follows active slash rather than newest warning");
            float pose = GhostSamuraiRules.DirectionalPose(tick);
            AssertEqual(true, Math.Abs(pose - GhostSamuraiRules.DirectionalPose(tick - .001f)) < .002f, "blade pose continuous at every beat");
        }
        AssertEqual(8, spawns, "four pairs of warning entities");
        AssertEqual(true, peak <= 4, "bounded overlapping warnings");
        for (int pair = 0; pair < 4; pair++)
        {
            AssertEqual(12, hazards[pair * 2 + 1].Fire - hazards[pair * 2].Fire, "rapid second sword");
            if (pair < 3) AssertEqual(36, hazards[pair * 2 + 2].Fire - hazards[pair * 2 + 1].Fire, "breath between pairs");
        }
        AssertEqual(GhostSamuraiRules.RecoveryTime, GhostSamuraiRules.DirectionalDuration - hazards[7].End, "eighth live window finishes before recovery");
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
            AssertEqual(true, next > previous && (next - previous) * GhostSamuraiRules.DashDistance < 150, "finite bounded faster movement");
            previous = next;
        }
        AssertEqual(true, GhostSamuraiRules.DashStandOff > 650, "retreat farther than the old position");
        AssertEqual(true, GhostSamuraiRules.DashDistance / GhostSamuraiRules.DashLive > 1300f / 24, "faster traversal");
    }

    [DomainTest("Ghost Samurai charge combo preserves late locks three releases and the longer third hold")]
    private static void SamuraiChargeCombo()
    {
        foreach (var phase in Enum.GetValues<SamuraiPhase>())
        foreach (bool grid in new[] { false, true })
        {
            int count = GhostSamuraiRules.ChargeCount(phase), lastEnd = 0;
            AssertEqual(phase == SamuraiPhase.Phase1 ? 1 : 3, count, "P1 single; P2 and provisional P3 triple");
            for (int pass = 0; pass < count; pass++)
            {
                int start = GhostSamuraiRules.ChargeStart(pass, grid);
                int fire = start + GhostSamuraiRules.ChargeWindup(pass, grid);
                AssertEqual(true, fire - start >= 30, "every strike retains a readable warning");
                AssertEqual(pass, GhostSamuraiRules.ChargePass(start, phase, grid), "new pass starts at exact boundary");
                if (pass > 0) AssertEqual(GhostSamuraiRules.ChargeComboGap, start - lastEnd, "prior attack ended before next warning");
                lastEnd = fire + GhostSamuraiRules.ChargeLive;
            }
            AssertEqual(GhostSamuraiRules.RecoveryTime, GhostSamuraiRules.ChargeDuration(phase, grid) - lastEnd, "complete final recoil");
            for (int tick = 1; tick <= GhostSamuraiRules.ChargeDuration(phase, grid); tick++)
                AssertEqual(true, Math.Abs(GhostSamuraiRules.ChargePose(tick, phase, grid) - GhostSamuraiRules.ChargePose(tick - .001f, phase, grid)) < .002f,
                    "continuous charge/release/recoil and next windup");
        }
        AssertEqual(true, GhostSamuraiRules.ChargeThirdWarning > GhostSamuraiRules.ChargeSecondWarning, "third attack has longer tension");
        AssertEqual(-1f, GhostSamuraiRules.ChargePose(0, SamuraiPhase.Phase2, true), "grid's held pose joins follow-up without a snap");
    }

    [DomainTest("Ghost Samurai grid ends before the follow-up and immediate movement can escape the locked band")]
    private static void SamuraiGridFollowDodge()
    {
        AssertEqual(112f, GhostSamuraiRules.GridSpacing - GhostSamuraiRules.GridHalfWidth * 2, "narrower but player-sized safe cells");
        int born = GhostSamuraiRules.GridFollowStart;
        var h = new SamuraiHazard(SamuraiShape.Slash, -900, 0, 1, 0, 1800, GhostSamuraiRules.ChargeHalfWidth,
            born, born + GhostSamuraiRules.GridFollowWarning, born + GhostSamuraiRules.GridFollowWarning + GhostSamuraiRules.ChargeLive, GhostSamuraiRules.ChargeDamage);
        var aim = SamuraiSlashAim.Spawn(h, GhostSamuraiRules.GridEnd);
        aim = aim.Advance(h, GhostSamuraiRules.GridEnd, -900, 0, 1, 0);
        AssertEqual(true, aim.Locked && aim.IsValid(h), "lock coincides with grid release");
        AssertEqual(24, h.Fire - aim.LockTick, "0.4 seconds to leave the grid cell");
        var line = GhostSamuraiRules.GridLine(true, 7, 0, 0, GhostSamuraiRules.GridPrelude);
        AssertEqual(false, line.Live(aim.LockTick), "grid no longer hits when movement begins");
        AssertEqual(true, h.Hits(h.Fire, 0, 0, 10, 21), "standing still is punished");
        AssertEqual(false, h.Hits(h.Fire, 0, 8 * (h.Fire - aim.LockTick), 10, 21), "immediate 8px/tick perpendicular movement clears full body");
        AssertEqual(true, h.Hits(h.Fire, 0, 8 * (h.Fire - aim.LockTick - 6), 10, 21), "hesitation consumes the dodge window");
        AssertEqual(true, GhostSamuraiRules.MaximumHazards >= GhostSamuraiRules.GridVerticalLineCount
            + GhostSamuraiRules.GridHorizontalLineCount + 1 + GhostSamuraiRules.MaximumWisps, "reserve the overlapping charge even at wisp cap");
    }

    [DomainTest("Ghost Samurai aimed slash tracks until final lock then preserves dash dodge geometry")]
    private static void SamuraiLateAim()
    {
        foreach (float width in new[] { GhostSamuraiRules.ChargeHalfWidth, GhostSamuraiRules.DashHalfWidth })
        {
            var h = new SamuraiHazard(SamuraiShape.Slash, -900, 0, 1, 0, 1800, width, 10, 82, 94, 380);
            var aim = SamuraiSlashAim.Spawn(h, h.Fire - GhostSamuraiRules.AimLockLead);
            for (int tick = h.Born + 1; tick <= aim.LockTick; tick++)
                aim = aim.Advance(h, tick, -900, tick * 2, 1, 0);
            AssertEqual(true, aim.Locked, "final authority update locks");
            AssertEqual(aim.LockTick * 2f, aim.Y, "follows a moving target through the final aim tick");
            var final = aim.Geometry(h);
            AssertEqual(aim, aim.Advance(h, aim.LockTick + 1, 4000, 4000, 0, 1), "no late chase after lock");
            AssertEqual(false, final.Hits(h.Fire - 1, 0, aim.Y, 10, 21), "last warning tick stays harmless");
            AssertEqual(true, final.Hits(h.Fire, 0, aim.Y, 10, 21), "held target is hit at declared fire");
            AssertEqual(false, final.Hits(h.Fire, 0, aim.Y + 14 * GhostSamuraiRules.AimLockLead, 10, 21), "14px/tick perpendicular dash clears both attack widths");
            if (width == GhostSamuraiRules.ChargeHalfWidth)
                AssertEqual(true, final.Hits(h.Fire, 0, aim.Y + 8 * GhostSamuraiRules.AimLockLead, 10, 21), "wide cut favors a dash over ordinary running");
        }
    }

    [DomainTest("Ghost Samurai aim snapshots reject stale duplicate-conflict truncation and invalid lock state")]
    private static void SamuraiAimWire()
    {
        var h = new SamuraiHazard(SamuraiShape.Slash, 400, 500, 1, 0, 1800, 150, 100, 172, 184, 380);
        var initial = SamuraiSlashAim.Spawn(h, 156);
        var moving = initial.Advance(h, 130, 600, 800, 0, 1);
        var locked = moving.Advance(h, 156, 650, 900, 0, 1);
        AssertEqual(true, locked.CanReplace(initial), "missed intermediate updates repaired by full locked snapshot");
        AssertEqual(false, moving.CanReplace(locked), "delayed moving warning cannot overwrite final line");
        AssertEqual(true, locked.CanReplace(locked), "identical resend idempotent");
        AssertEqual(false, (locked with { X = 700 }).CanReplace(locked), "same-tick conflicting update rejected");
        AssertEqual(false, (locked with { LockTick = 155 }).CanReplace(moving), "immutable lock schedule");
        foreach (var snapshot in new[] { initial, moving, locked, SamuraiSlashAim.Spawn(h, h.Born) })
        {
            using var stream = new MemoryStream(); snapshot.Write(new BinaryWriter(stream)); byte[] bytes = stream.ToArray(); stream.Position = 0;
            AssertEqual(24, bytes.Length, "bounded full aim state");
            AssertEqual(snapshot, SamuraiSlashAim.Read(new BinaryReader(stream), h), "aim snapshot roundtrip");
            for (int n = 0; n < bytes.Length; n++)
            {
                bool rejected = false;
                try { SamuraiSlashAim.Read(new BinaryReader(new MemoryStream(bytes, 0, n)), h); } catch (IOException) { rejected = true; }
                AssertEqual(true, rejected, "truncated aim rejected");
            }
        }
        foreach (var invalid in new[] { moving with { X = float.NaN }, moving with { DY = 3 }, moving with { Tick = 99 },
            moving with { Tick = 157 }, moving with { LockTick = 171 }, moving with { X = 500001 } })
        {
            using var stream = new MemoryStream(); invalid.Write(new BinaryWriter(stream)); stream.Position = 0;
            bool rejected = false;
            try { SamuraiSlashAim.Read(new BinaryReader(stream), h); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid aim rejected before state mutation");
        }
    }

    [DomainTest("Ghost Samurai wisp burst scheduling increases opportunities by exactly half and stays bounded")]
    private static void SamuraiWispBurstCadence()
    {
        int bursts = 0;
        for (int opportunity = 0; opportunity < 100; opportunity++)
        {
            int count = GhostSamuraiRules.WispBursts(opportunity);
            bursts += count;
            AssertEqual(true, count is 1 or 2, "at most one delayed extra burst");
            if (opportunity % 2 == 1) AssertEqual((opportunity + 1) * 3 / 2, bursts, "every two opportunities yield three bursts");
        }
        int pairs = 0;
        for (int tick = 0; tick < GhostSamuraiRules.DirectionalDuration; tick++)
        {
            int step = GhostSamuraiRules.DirectionalSpawnStep(tick);
            if (step >= 0 && step % 2 == 0) pairs++;
        }
        AssertEqual(4, pairs, "paired strikes preserve the original four base wisp opportunities");
        AssertEqual(18, GhostSamuraiRules.MaximumWisps, "1.5 times original live and reserved wisp cap");
        AssertEqual(49, GhostSamuraiRules.MaximumHazards, "hard combined budget including grid follow-up");
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
