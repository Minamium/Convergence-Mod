using System;
using Convergence.Client.Encounters.GhostSamurai;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static SamuraiHazard TestWave(float dx = 1, float dy = 0, int born = 100)
        => new(SamuraiShape.SlashWave, 0, 0, dx, dy, SamuraiWaveRules.ChargedSlashWaveWidth,
            SamuraiWaveRules.ChargedSlashWaveHeight / 2, born, born + GhostSamuraiRules.GridFollowWarning,
            born + GhostSamuraiRules.GridFollowWarning + SamuraiWaveRules.WaveLife, GhostSamuraiRules.ChargeDamage);

    [DomainTest("Ghost Samurai grid wave tracks a moving target and reverse-schedules sixty-tick arrival")]
    private static void SamuraiGridWaveArrival()
    {
        foreach (float distance in new[] { 0f, 50f, 300f, 900f, 2100f, 3950f })
        for (int angle = 0; angle < 32; angle++)
        {
            float dx = MathF.Cos(angle * MathF.Tau / 32), dy = MathF.Sin(angle * MathF.Tau / 32);
            var h = TestWave(dx, dy, 0);
            float x = dx * distance, y = dy * distance;
            int flight = SamuraiWaveRules.FlightTicks(h, x, y, 10, 21);
            h = h with { ArrivalTick = Math.Max(h.Fire + flight, h.Born + 144) };
            var grid = GhostSamuraiRules.GridLine(true, 7, x, y, h.ArrivalTick - 144);
            AssertEqual(true, h.IsValid && grid.IsValid, "both full warnings have nonnegative births");
            AssertEqual(108, h.Fire - h.Born, "grid wave minimum warning");
            AssertEqual(84, grid.Fire - grid.Born, "grid warning intact");
            var aim = SamuraiSlashAim.Spawn(h, h.Fire - 4);
            for (int tick = 1; tick <= 300 && !aim.Locked; tick++)
            {
                // Target and boss moving together must not break the arrival
                // clock or pin the first grid wave to the original target point.
                aim = aim.TrackArrival(h, tick, 0, tick * 3, dx, dy, x, y + tick * 3, 10, 21);
                AssertEqual(true, aim.IsValid(h), "moving release is bounded and transferable");
            }
            AssertEqual(true, aim.Locked, "bounded late lock");
            var final = aim.Geometry(h);
            AssertEqual(60, final.Fire + flight - grid.Fire, "first full-hitbox contact is one second after grid");
            AssertEqual(true, SamuraiWaveRules.Geometry(final, final.Fire + flight).Hits(final.Fire + flight, x, y + aim.Tick * 3, 10, 21), "full wave reaches final reference");
            AssertEqual(aim, aim.TrackArrival(h, aim.Tick + 1, 5000, 5000, 0, 1, 0, 0, 10, 21), "late dash cannot bend a locked wave");
        }
    }

    [DomainTest("Ghost Samurai moving arrival snapshots reject rollback and preserve warning under late teleports")]
    private static void SamuraiArrivalWire()
    {
        var h = TestWave(born: 0) with { ArrivalTick = 144 };
        var first = SamuraiSlashAim.Spawn(h, h.Fire - 4);
        var moving = first.TrackArrival(h, 40, 0, 0, 1, 0, 600, 0, 10, 21);
        var locked = moving.TrackArrival(h, 120, 0, 0, 1, 0, 3000, 0, 10, 21);
        AssertEqual(true, locked.Locked && locked.IsValid(h), "teleport locks with a future release, never rewinds damage");
        AssertEqual(124, locked.ReleaseTick, "four real ticks remain for late lock");
        AssertEqual(true, locked.CanReplace(first), "final snapshot repairs missed mutable timing");
        AssertEqual(false, moving.CanReplace(locked), "old moving packet cannot restore a locked attack");
        using var bytes = new System.IO.MemoryStream();
        locked.Write(new System.IO.BinaryWriter(bytes)); bytes.Position = 0;
        AssertEqual(locked, SamuraiSlashAim.Read(new System.IO.BinaryReader(bytes), h), "late peer receives final geometry and time together");
        AssertEqual(false, (locked with { ReleaseTick = 500 }).IsValid(h), "unbounded warning rejected");
        AssertEqual(false, (h with { Shape = SamuraiShape.RushVisual, Damage = 0 }).IsValid, "arrival schedule is wave-only");
    }

    [DomainTest("Ghost Samurai slash wave is a finite travelling hitbox and never a runtime line or body hit")]
    private static void SamuraiWaveGeometry()
    {
        var h = TestWave();
        AssertEqual(24f, SamuraiWaveRules.ChargedSlashWaveSpeed, "wave speed in pixels/tick");
        AssertEqual(64f, h.Length, "longitudinal width");
        AssertEqual(480f, h.Radius * 2, "lateral height");
        AssertEqual(false, h.Hits(h.Fire, 0, 0, 10, 21), "runtime manual hurt cannot hit for a wave");
        AssertEqual(false, GhostSamuraiRules.RushCanContact(h, h.Fire, true), "no charged boss contact");
        for (int tick = h.Fire - 1; tick <= h.End; tick++)
        {
            var geometry = SamuraiWaveRules.Geometry(h, tick);
            float x = Math.Max(0, tick - h.Fire) * SamuraiWaveRules.ChargedSlashWaveSpeed;
            AssertEqual(tick >= h.Fire && tick < h.End, geometry.Hits(tick, x, 0, 10, 21), "only current wave footprint active");
            AssertEqual(false, geometry.Hits(tick, x + 43, 0, 10, 21), "no full forecast-line damage");
            AssertEqual(false, geometry.Hits(tick, x, 262, 10, 21), "can clear wave's lateral edge");
        }
        int touching = 0;
        for (int tick = h.Fire; tick < h.End; tick++)
            if (SamuraiWaveRules.Geometry(h, tick).Hits(tick, 900, 0, 10, 21)) touching++;
        AssertEqual(true, touching is >= 3 and <= 4, "stationary reference exposed only a few ticks, not the whole wave lifetime");
        var tracked = SamuraiSlashAim.Spawn(h, h.Fire - GhostSamuraiRules.AimLockLead);
        tracked = tracked.Advance(h, tracked.LockTick, 30, 40, 0, 1);
        AssertEqual(true, tracked.Locked && tracked.IsValid(h), "normal wave preserves late aim");
        AssertEqual(tracked, tracked.Advance(h, tracked.LockTick + 1, 300, 400, 1, 0), "released wave cannot re-aim");
        AssertEqual(false, (h with { Radius = 241 }).IsValid, "wire cannot enlarge wave hitbox");
        AssertEqual(false, (h with { Length = 65 }).IsValid, "wire cannot enlarge thickness");
    }

    [DomainTest("Ghost Samurai outer circle covers the constrained field even when centered near a corner")]
    private static void SamuraiCircleFieldCoverage()
    {
        var field = SamuraiArenaBounds.Create(4000, 4000, 60000, 20000);
        AssertEqual(5600f, GhostSamuraiRules.Phase3CircleOuterRadius, "field-sized finite radius");
        foreach (float x in new[] { field.Left, field.CenterX, field.Right })
        foreach (float y in new[] { field.Top, field.CenterY, field.Bottom })
        foreach (float px in new[] { field.Left, field.Right })
        foreach (float py in new[] { field.Top, field.Bottom })
        {
            var h = GhostSamuraiRules.CircleStep(1, x, y, 0);
            float distance = MathF.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
            AssertEqual(true, distance < h.Radius, "no outer-edge refuge anywhere in the field");
            if (distance > h.Length) AssertEqual(true, h.Hits(h.Fire, px, py, 1, 1), "remote corner remains dangerous");
            AssertEqual(false, h.Hits(h.Fire, x, y, 10, 21), "intended central hole preserved");
            AssertEqual(false, h.Hits(h.Fire, x + h.Radius + 11, y, 10, 21), "finite outer edge retained");
        }
    }

    [DomainTest("Ghost Samurai sound owner deduplicates grid and plays every sequential release and shout once")]
    private static void SamuraiAudioCadence()
    {
        var cues = new SamuraiCueClock(); var fight = Guid.NewGuid();
        cues.Advance(fight, 0);
        int sounds = 0;
        cues.Advance(fight, 84);
        for (int line = 0; line < 30; line++) if (cues.Try(0, 84)) sounds++;
        AssertEqual(1, sounds, "thirty grid entities yield one slash");
        AssertEqual(true, cues.Try(3, 84), "a genuinely simultaneous wave retains its heavy swing cue");
        AssertEqual(false, cues.Try(3, 84), "duplicate wave cue suppressed");
        cues.Clear(); cues.Advance(fight, 0); sounds = 0;
        for (int strike = 0; strike < 8; strike++)
        {
            int at = GhostSamuraiRules.DirectionalSpawnTime(strike) + GhostSamuraiRules.SlashWarning;
            cues.Advance(fight, at);
            if (cues.Try(0, at)) sounds++;
            AssertEqual(false, cues.Try(0, at), "duplicate draw/update never repeats a sword");
        }
        AssertEqual(8, sounds, "all eight swords audible");
        cues.Clear(); cues.Advance(fight, 0); sounds = 0;
        for (int step = 0; step < 4; step++)
        {
            int at = GhostSamuraiRules.CircleStep(step, 0, 0, 0).Fire;
            cues.Advance(fight, at); if (cues.Try(0, at)) sounds++;
        }
        AssertEqual(4, sounds, "both swords and both wind strikes audible");
        cues.Clear(); cues.Advance(fight, 0);
        const int fire = 102; int shout = fire - GhostSamuraiRules.DashShoutDelay;
        cues.Advance(fight, shout - 1); AssertEqual(false, cues.Try(1, shout), "no early shout");
        cues.Advance(fight, shout); AssertEqual(true, cues.Try(1, shout), "shout before final lock");
        AssertEqual(false, cues.Try(0, fire), "no dash release before reaction gap");
        cues.Advance(fight, fire); AssertEqual(true, cues.Try(0, fire), "slash at dash start");
        AssertEqual(78, fire - shout, "preparation shout moved sixty ticks earlier");
    }

    [DomainTest("Ghost Samurai audio clock bounds catch-up memory and never replays pre-join or previous-fight cues")]
    private static void SamuraiAudioLateJoin()
    {
        var cues = new SamuraiCueClock(); var fight = Guid.NewGuid();
        cues.Advance(fight, 100);
        AssertEqual(false, cues.Try(0, 99), "pre-join event excluded even within catch-up window");
        AssertEqual(true, cues.Try(0, 100), "current event audible on first frame");
        cues.Advance(fight, 104);
        AssertEqual(true, cues.Try(1, 101), "small packet delay catches final lock once");
        AssertEqual(false, cues.Try(1, 101), "late duplicate silent");
        cues.Advance(fight, 110);
        AssertEqual(false, cues.Try(0, 105), "historical cue not replayed after a long stall");
        for (int tick = 111; tick < 10000; tick++)
        {
            cues.Advance(fight, tick);
            for (int kind = 0; kind < 3; kind++) cues.Try(kind, tick);
            AssertEqual(true, cues.Count <= 15, "memory independent of fight duration");
        }
        cues.Advance(Guid.NewGuid(), 0);
        AssertEqual(true, cues.Try(0, 0), "new fight starts fresh");
        AssertEqual(false, cues.Try(0, 9999), "previous fight schedule cannot sound");
        cues.Clear(); AssertEqual(0, cues.Count, "world unload clears cues");
    }
}
