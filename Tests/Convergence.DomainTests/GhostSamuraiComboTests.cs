using System;
using System.IO;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ghost Samurai combos enter the intended phases without renumbering old attacks")]
    private static void SamuraiComboPools()
    {
        AssertEqual(5, (int)SamuraiAttack.Phase3CircleAttack, "old wire identity");
        AssertEqual(7, (int)SamuraiShape.SlashWave, "old wave identity");
        foreach (var phase in Enum.GetValues<SamuraiPhase>())
        {
            AssertEqual(true, GhostSamuraiRules.AttackAllowed(phase, SamuraiAttack.TripleVerticalSlash), "vertical available from phase1");
            AssertEqual(phase != SamuraiPhase.Phase1, GhostSamuraiRules.AttackAllowed(phase, SamuraiAttack.FrontalCleaveShockwave), "cleave starts phase2");
        }
    }

    [DomainTest("Ghost Samurai combos issue exactly three vertical events even with repeated ticks")]
    private static void SamuraiComboEvents()
    {
        var state = new SamuraiComboSnapshot(0, 1, false, 0, 0, 0, 0, 0);
        int issued = 0;
        for (int tick = 0; tick < 1000; tick++)
        for (int duplicate = 0; duplicate < 3; duplicate++)
        {
            int step = SamuraiComboRules.VerticalSpawnStep(tick);
            if (step >= 0 && state.TryAdvance(step + 1, out var next)) { state = next; issued++; }
        }
        AssertEqual(3, issued, "no fourth slash or duplicate dispatch");
        AssertEqual(false, state.TryAdvance(4, out _), "hard maximum");
        state = default;
        AssertEqual(true, state.TryAdvance(1, out state), "fresh state restarts first event");
        AssertEqual(false, state.TryAdvance(1, out _), "cleave cannot repeat");
        AssertEqual(true, state.TryAdvance(2, out state), "one shockwave emission event");
        AssertEqual(false, state.TryAdvance(2, out _), "shockwave event cannot repeat");
        AssertEqual(true, default(SamuraiComboSnapshot).IsValid(SamuraiAttack.Idle), "cleanup snapshot clears counters and facing lock");
    }

    [DomainTest("Ghost Samurai vertical lock leaves walking and dash escapes on both sides")]
    private static void SamuraiVerticalEscape()
    {
        var arena = SamuraiArenaBounds.Create(4000, 4000, 60000, 20000);
        int previousEnd = -1;
        for (int step = 0; step < 3; step++)
        {
            var h = SamuraiComboRules.Vertical(4000, arena, step * SamuraiComboRules.VerticalCadence);
            var aim = SamuraiSlashAim.Spawn(h, h.Fire - SamuraiComboRules.VerticalLockLead);
            aim = aim.Advance(h, aim.LockTick, 4100, arena.Top, 0, 1);
            AssertEqual(true, h.IsValid && aim.IsValid(h) && aim.Locked, "valid fixed vertical attack");
            AssertEqual(aim, aim.Advance(h, h.Fire, 4800, arena.Top, 0, 1), "moving target after lock cannot bend it");
            var hit = aim.Geometry(h);
            AssertEqual(true, hit.Hits(h.Fire, 4100, 3600, 10, 21), "remaining on the locked column is dangerous");
            foreach (int side in new[] { -1, 1 })
            {
                AssertEqual(false, hit.Hits(h.Fire, 4100 + side * 6 * SamuraiComboRules.VerticalLockLead, 3600, 10, 21), "6px/tick ground motion clears full player");
                AssertEqual(false, hit.Hits(h.Fire, 4100 + side * 18 * SamuraiComboRules.VerticalLockLead, 3600, 10, 21), "18px/tick dash clears full player");
            }
            AssertEqual(false, hit.Hits(h.Fire - 1, 4100, 3600, 10, 21), "all forecast ticks harmless");
            AssertEqual(false, hit.Hits(h.End, 4100, 3600, 10, 21), "live range ends exactly");
            AssertEqual(true, h.Born > previousEnd, "independent swings do not overlap");
            previousEnd = h.End;
        }
        AssertEqual(GhostSamuraiRules.RecoveryTime, SamuraiComboRules.VerticalDuration - previousEnd, "short recovery after third slash");
    }

    [DomainTest("Ghost Samurai cleave follows briefly then keeps the full rear side safe")]
    private static void SamuraiCleaveRear()
    {
        foreach (int face in new[] { -1, 1 })
        {
            var h = SamuraiComboRules.Cleave(4000, 3915, face, 48);
            var aim = SamuraiSlashAim.Spawn(h, h.Born + SamuraiComboRules.CleaveTrackTime);
            aim = aim.Advance(h, aim.LockTick, h.X, h.Y, face, 0);
            AssertEqual(true, h.IsValid && aim.IsValid(h), "bounded half-disc and early lock");
            AssertEqual(84, h.Fire - aim.LockTick, "1.4 seconds to circle behind");
            AssertEqual(aim, aim.Advance(h, h.Fire - 1, h.X, h.Y, -face, 0), "never turn around after lock");
            for (int count = 1; count <= 4; count++)
            for (int slot = 0; slot < count; slot++)
            {
                AssertEqual(false, h.Hits(h.Fire, h.X - face * (30 + slot * 40), 3979, 10, 21), "every full hitbox behind is safe");
                AssertEqual(true, h.Hits(h.Fire, h.X + face * (100 + slot * 400), 3600, 10, 21), "front-side distancing remains dangerous");
            }
            AssertEqual(false, h.Hits(h.Fire - 1, h.X + face * 200, h.Y, 10, 21), "windup harmless");
            AssertEqual(false, h.Hits(h.End, h.X + face * 200, h.Y, 10, 21), "slash fully ends before follow-up");
            AssertEqual(true, h.Hits(h.Fire, h.X - face * 5, h.Y, 10, 21), "a hitbox straddling the pivot is not fully behind");
            AssertEqual(false, h.Hits(h.Fire, h.X - face * 11, h.Y, 10, 21), "one pixel fully behind is safe");
        }
    }

    [DomainTest("Ghost Samurai rear dodge must be followed by jumping the bounded ground fronts")]
    private static void SamuraiShockJump()
    {
        var cleave = SamuraiComboRules.Cleave(4000, 3915, 1, 48);
        foreach (int side in new[] { -1, 1 })
        {
            var wave = SamuraiComboRules.Shock(4000, 4000, side, 2240, cleave.End);
            AssertEqual(true, wave.IsValid, "whole-field front fits packet and lifetime budget");
            AssertEqual(24, wave.Fire - cleave.End, "readable post-slash delay");
            AssertEqual(true, wave.End + GhostSamuraiRules.RecoveryTime < SamuraiComboRules.MaximumComboTime, "last front ends before timeout");
            int hitTick = wave.Fire + 8;
            float x = wave.X + side * SamuraiComboRules.ShockSpeed * 8;
            AssertEqual(true, wave.Hits(hitTick, x, 3979, 10, 21), "standing after rear dodge gets hit");
            AssertEqual(false, wave.Hits(hitTick, x, 3979 - 60, 10, 21), "60px jump clears the full player");
            AssertEqual(false, wave.Hits(wave.Fire - 1, wave.X, 3979, 10, 21), "ground forecast harmless");
            AssertEqual(false, wave.Hits(wave.End, x, 3979, 10, 21), "expired wave harmless");
            AssertEqual(false, wave.Hits(hitTick, wave.X - side * 200, 3979, 10, 21), "each front only hits its own current location");
        }
        AssertEqual(false, cleave.Hits(cleave.Fire, 3880, 3979, 10, 21), "the same location first dodges the cleave");
    }

    [DomainTest("Ghost Samurai cleave covers the forward field but preserves a reachable rear pocket at either edge")]
    private static void SamuraiCleaveField()
    {
        var arena = SamuraiArenaBounds.Create(4000, 4000, 60000, 20000);
        foreach (float x in new[] { arena.Left + 320, arena.CenterX, arena.Right - 320 })
        foreach (int face in new[] { -1, 1 })
        {
            var h = SamuraiComboRules.Cleave(x, arena.Bottom - GhostSamuraiRules.BodyHeight / 2f, face, 0);
            float edge = face > 0 ? arena.Right - 10 : arena.Left + 10;
            AssertEqual(true, h.Hits(h.Fire, edge, arena.Top + 21, 10, 21), "front upper corner covered");
            AssertEqual(true, h.Hits(h.Fire, edge, arena.Bottom - 21, 10, 21), "front lower corner covered");
            AssertEqual(false, h.Hits(h.Fire, x - face * 100, arena.Bottom - 21, 10, 21), "room for a full rear dodge at edge");
        }
        AssertEqual(true, (SamuraiComboRules.CleaveStandOff + 30) / 6 < SamuraiComboRules.CleaveWindup - SamuraiComboRules.CleaveTrackTime,
            "a nearby walking player has time to cross the pivot after lock; dash leaves more room");
    }

    [DomainTest("Ghost Samurai combo geometry and final lock repair late join without local targeting")]
    private static void SamuraiComboGeometryWire()
    {
        var arena = SamuraiArenaBounds.Create(4000, 4000, 60000, 20000);
        foreach (var h in new[] { SamuraiComboRules.Vertical(4000, arena, 100), SamuraiComboRules.Cleave(4000, 3900, -1, 100), SamuraiComboRules.Shock(4000, 4000, -1, 1500, 220) })
        {
            using var bytes = new MemoryStream(); h.Write(new BinaryWriter(bytes)); bytes.Position = 0;
            AssertEqual(h, SamuraiHazard.Read(new BinaryReader(bytes)), "all timing and geometry round-trip");
            if (!h.HasAim) continue;
            int at = h.Shape == SamuraiShape.VerticalSlash ? h.Fire - SamuraiComboRules.VerticalLockLead : h.Born + SamuraiComboRules.CleaveTrackTime;
            var first = SamuraiSlashAim.Spawn(h, at);
            var final = first.Advance(h, at, h.X + 100, h.Y, h.DX, h.DY);
            AssertEqual(true, final.CanReplace(first), "final lock repairs missing intermediate aim updates");
            AssertEqual(false, first.CanReplace(final), "late old targeting cannot reopen the aim");
            var targetChanged = final with { DX = -final.DX, X = final.X + 200, Tick = final.Tick + 1 };
            AssertEqual(false, targetChanged.CanReplace(final), "retarget after lock cannot mutate this strike");
        }
    }

    [DomainTest("Ghost Samurai combo pose snapshot rejects malformed counters facing locks and coordinates")]
    private static void SamuraiComboPoseWire()
    {
        var state = new SamuraiComboSnapshot(1, -1, true, 3500, 3200, 4000, 3915, 4000);
        var actor = new SamuraiActorSnapshot(Guid.NewGuid(), 100, SamuraiPhase.Phase2, SamuraiAttack.FrontalCleaveShockwave,
            SamuraiBeat.Telegraph, 72, 0, 2400000, SamuraiArenaBounds.Create(4000, 4000, 60000, 20000), 2, state);
        using var bytes = new MemoryStream(); actor.Write(new BinaryWriter(bytes)); byte[] data = bytes.ToArray(); bytes.Position = 0;
        AssertEqual(actor, SamuraiActorSnapshot.Read(new BinaryReader(bytes)), "target, timer, counter, facing, anchors, lock share snapshot");
        for (int length = 0; length < data.Length; length++)
        {
            bool rejected = false;
            try { SamuraiActorSnapshot.Read(new BinaryReader(new MemoryStream(data, 0, length))); } catch (IOException) { rejected = true; }
            AssertEqual(true, rejected, "every truncated snapshot rejected");
        }
        foreach (var invalid in new[] { state with { Step = 3 }, state with { Facing = 0 }, state with { Facing = 2 }, state with { AnchorX = float.NaN }, state with { GroundY = 600000 } })
        {
            AssertEqual(false, invalid.IsValid(SamuraiAttack.FrontalCleaveShockwave), "unbounded pose rejected");
        }
        data[data.Length - 21] = 2; // Boolean preceding the five floats.
        bool invalidFlag = false;
        try { SamuraiActorSnapshot.Read(new BinaryReader(new MemoryStream(data))); } catch (InvalidDataException) { invalidFlag = true; }
        AssertEqual(true, invalidFlag, "noncanonical boolean rejected");
        AssertEqual(false, state.IsValid(SamuraiAttack.Idle), "idle cannot retain a combo lock or counter");
    }

    [DomainTest("Ghost Samurai combo sword poses remain continuous through all three swings and recoil")]
    private static void SamuraiComboPoses()
    {
        for (int tick = 0; tick <= SamuraiComboRules.VerticalDuration; tick++)
        {
            float pose = SamuraiComboRules.VerticalPose(tick);
            AssertEqual(true, float.IsFinite(pose) && Math.Abs(pose - SamuraiComboRules.VerticalPose(tick - .001f)) < .002f, "vertical joint continuity");
        }
        AssertEqual(0f, SamuraiComboRules.VerticalPose(SamuraiComboRules.VerticalDuration), "third recoil reaches idle");
        for (int tick = 0; tick <= 250; tick++)
        {
            float Pose(float t) => SamuraiComboRules.SwingPose(t - SamuraiComboRules.CleaveApproach, SamuraiComboRules.CleaveWindup, SamuraiComboRules.CleaveLive, SamuraiComboRules.ShockDelay);
            AssertEqual(true, Math.Abs(Pose(tick) - Pose(tick - .001f)) < .002f, "cleave pose continuity");
        }
        AssertEqual(1f, SamuraiComboRules.ApproachProgress(SamuraiComboRules.CleaveApproach - 1), "boss settles at synchronized anchor before windup");
    }
}
