using System;
using System.IO;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ghost Samurai circle steps have one fixed center and four non-overlapping timed areas")]
    private static void SamuraiCircleSchedule()
    {
        SamuraiHazard previous = default;
        for (int step = 0; step < 4; step++)
        {
            var h = GhostSamuraiRules.CircleStep(step, 1234, 5678, 100);
            AssertEqual(true, h.IsValid, "bounded circle descriptor");
            AssertEqual(1234f, h.X, "all steps retain captured X");
            AssertEqual(5678f, h.Y, "all steps retain captured Y");
            AssertEqual(step % 2 == 1, h.IsOuter, "inner outer inner outer");
            AssertEqual(step >= 2, h.IsWind, "second pair has distinct wind presentation");
            AssertEqual(36, h.Fire - h.Born, "every step gets its own harmless forecast");
            AssertEqual(step < 2 ? 12 : 24, h.End - h.Fire, "blade and sustained wind windows");
            if (step > 0)
            {
                AssertEqual(60, h.Fire - previous.Fire, "one second between impacts");
                AssertEqual(true, h.Born >= previous.End, "next forecast starts after previous danger");
            }
            using var packet = new MemoryStream(); h.Write(new BinaryWriter(packet)); packet.Position = 0;
            AssertEqual(h, SamuraiHazard.Read(new BinaryReader(packet)), "late-join snapshot contains exact center and schedule");
            previous = h;
        }
        AssertEqual(18, 100 + GhostSamuraiRules.Phase3CircleDuration - previous.End, "final wind finishes before recovery");
        AssertEqual(true, 4 + GhostSamuraiRules.MaximumWisps < GhostSamuraiRules.MaximumHazards, "four actors fit existing budget with lingering wisps");
    }

    [DomainTest("Ghost Samurai disk and donut collision match full player bounds and leave finite safe regions")]
    private static void SamuraiCircleGeometry()
    {
        for (int step = 0; step < 4; step++)
        {
            var h = GhostSamuraiRules.CircleStep(step, 0, 0, 0);
            foreach (int players in new[] { 1, 2, 3, 4 })
            for (int player = 0; player < players; player++)
            {
                float angle = player * MathF.Tau / players;
                float safe = h.IsOuter ? 160 : 320, danger = h.IsOuter ? 400 : 160;
                AssertEqual(false, h.Hits(h.Fire, MathF.Cos(angle) * safe, MathF.Sin(angle) * safe, 10, 21), "entire player fits intended safe zone");
                AssertEqual(true, h.Hits(h.Fire, MathF.Cos(angle) * danger, MathF.Sin(angle) * danger, 10, 21), "danger zone hits");
            }
            AssertEqual(false, h.Hits(h.Fire - 1, 300, 0, 10, 21), "last telegraph tick harmless");
            AssertEqual(false, h.Hits(h.End, 300, 0, 10, 21), "end tick harmless");
            AssertEqual(false, h.Hits(h.Fire, GhostSamuraiRules.Phase3CircleOuterRadius + 50, 0, 10, 21), "outside finite outer radius safe");
            if (h.IsOuter)
            {
                AssertEqual(false, h.Hits(h.Fire, 0, 0, 10, 21), "center hole stays safe");
                AssertEqual(true, h.Hits(h.Fire, 235, 0, 10, 21), "hitbox crossing inner boundary is unsafe");
                AssertEqual(true, h.Hits(h.Fire, GhostSamuraiRules.Phase3CircleOuterRadius + 5, 0, 10, 21), "hitbox overlapping outer boundary still touches donut");
                AssertEqual(false, h.Hits(h.Fire, GhostSamuraiRules.Phase3CircleOuterRadius + 11, 0, 10, 21), "whole hitbox beyond outer border safe");
            }
            else
            {
                AssertEqual(true, h.Hits(h.Fire, 245, 0, 10, 21), "disk sees near side of hitbox");
                AssertEqual(false, h.Hits(h.Fire, 251, 0, 10, 21), "whole hitbox clears disk");
            }
        }
    }

    [DomainTest("Ghost Samurai circle and harmless rush wire reject inconsistent radii damage and unbounded warnings")]
    private static void SamuraiNewShapeBounds()
    {
        var disk = GhostSamuraiRules.CircleStep(0, 0, 0, 0);
        var ring = GhostSamuraiRules.CircleStep(1, 0, 0, 0);
        var rush = new SamuraiHazard(SamuraiShape.RushVisual, -900, 0, 1, 0, 2100, 150, 0, 192, 204, 0);
        AssertEqual(true, rush.IsValid, "extended initial warning accepted");
        foreach (var invalid in new[] { disk with { Length = 1 }, disk with { Radius = 6001 }, ring with { Length = 0 },
            ring with { Length = ring.Radius }, ring with { Length = float.NaN }, rush with { Damage = 1 }, rush with { Fire = 301, End = 313 },
            disk with { Born = int.MaxValue, Fire = int.MinValue, End = int.MinValue + 12 } })
        {
            using var packet = new MemoryStream(); invalid.Write(new BinaryWriter(packet)); packet.Position = 0;
            bool rejected = false;
            try { SamuraiHazard.Read(new BinaryReader(packet)); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid shape rejected before authority/replica mutation");
        }
    }

    [DomainTest("Ghost Samurai rush contact exists only during locked body traversal and never across the visual line")]
    private static void SamuraiRushContactWindow()
    {
        foreach (bool charged in new[] { false })
        {
            int live = charged ? GhostSamuraiRules.ChargeLive : GhostSamuraiRules.DashLive;
            var h = new SamuraiHazard(SamuraiShape.RushVisual, -900, 0, 1, 0, 2100, 150, 0, 192, 192 + live, 0);
            for (int age = 0; age < h.End + 30; age++)
            {
                AssertEqual(age >= h.Fire && age < h.End, GhostSamuraiRules.RushCanContact(h, age, true), "exact strike frames only");
                AssertEqual(false, GhostSamuraiRules.RushCanContact(h, age, false), "unlocked attack cannot damage");
                AssertEqual(false, h.Hits(age, 0, 0, 10, 21), "whole visual remains harmless in every frame");
            }
        }
        AssertEqual(false, GhostSamuraiRules.BodyContact(-900, 0, -850, 0, 0, 0, 10, 21), "no hit far ahead of current body");
        AssertEqual(true, GhostSamuraiRules.BodyContact(-150, 0, 150, 0, 0, 0, 10, 21), "fast body cannot tunnel through player");
        AssertEqual(false, GhostSamuraiRules.BodyContact(-150, 0, 150, 0, 0, 107, 10, 21), "outside body corridor safe");
        AssertEqual(true, GhostSamuraiRules.BodyContact(-150, -150, 150, 150, 0, 0, 10, 21), "diagonal swept contact");
        AssertEqual(false, GhostSamuraiRules.BodyContact(-150, -150, 150, 150, -140, 140, 10, 21), "diagonal bounding-box corner does not falsely hit");
    }

    [DomainTest("Ghost Samurai interception threatens steady flight but allows a late perpendicular dash")]
    private static void SamuraiRushDodge()
    {
        foreach (bool charged in new[] { false })
        foreach (float speed in new[] { 0f, 8f, 24f, 36f })
        {
            int duration = charged ? GhostSamuraiRules.ChargeLive : GhostSamuraiRules.DashLive;
            float distance = charged ? GhostSamuraiRules.ChargeDistance : GhostSamuraiRules.DashDistance;
            var d = GhostSamuraiRules.RushDirection(-900, 0, 0, 0, 0, speed, distance, duration);
            bool steadyHit = false, dashHit = false;
            float oldX = -900, oldY = 0;
            for (int tick = 0; tick < duration; tick++)
            {
                float travel = distance * GhostSamuraiRules.RushProgress(tick + 1, duration);
                float x = -900 + d.DX * travel, y = d.DY * travel;
                float elapsed = GhostSamuraiRules.DashAimLockTime + tick;
                steadyHit |= GhostSamuraiRules.BodyContact(oldX, oldY, x, y, 0, speed * elapsed, 10, 21);
                // Change velocity only after lock, perpendicular to the locked line.
                dashHit |= GhostSamuraiRules.BodyContact(oldX, oldY, x, y,
                    -d.DY * 14 * elapsed, speed * elapsed + d.DX * 14 * elapsed, 10, 21);
                oldX = x; oldY = y;
            }
            AssertEqual(true, steadyHit, "current velocity intercept catches representative run/wing speeds");
            AssertEqual(false, dashHit, "late 14px/tick lateral dash clears swept body");
        }
    }
}
