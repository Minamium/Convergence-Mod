using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet single-note targets rotate through one two three four and eight living members")]
    private static void ScarletBeamRosterRotation()
    {
        foreach (int size in new[] { 1, 2, 3, 4, 8 })
            for (int ordinal = 0; ordinal < size * 8; ordinal++)
                AssertEqual(ordinal % size, CrimsonTrackingBeam.TargetIndex(ordinal, size), "one target, fair rotation");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonTrackingBeam.TargetIndex(-1, 2), "negative ordinal");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonTrackingBeam.TargetIndex(0, 9), "roster bound");
    }
    [DomainTest("Scarlet crossflows and aimed notes are shared evenly by every living roster size over whole cycles")]
    private static void ScarletTargetDistribution()
    {
        // The runtime's rule: one ordinal per aimed note identity (Final's two families of a note share it), drawn from
        // the crossflow rotation for pickups and closers and from the note rotation otherwise.
        for (int phase = 0; phase < 4; phase++) for (int living = 1; living <= 8; living++)
        {
            var crossflows = new int[living]; var aimed = new int[living];
            int crossflowOrdinal = 0, noteOrdinal = 0, earliest = 900;
            for (int serial = 1; serial <= 12 * living; serial++)
            {
                var rhythm = CrimsonChoreography.Create(earliest, serial, phase, serial % 12 == 1);
                var assigned = new HashSet<int>();
                foreach (var (hit, second) in CrimsonEnsemble.Notes(rhythm, phase))
                {
                    if (!CrimsonGesturePlan.NeedsTargetIdentity(CrimsonEnsemble.Technique(phase, serial, hit.Pulse, second)) || !assigned.Add(hit.Pulse)) continue;
                    if (CrimsonChoreography.IsCrossflow(hit.Pulse)) crossflows[CrimsonTrackingBeam.TargetIndex(crossflowOrdinal++, living)]++;
                    else aimed[CrimsonTrackingBeam.TargetIndex(noteOrdinal++, living)]++;
                }
                earliest = rhythm.End;
            }
            AssertEqual(true, crossflows.Max() - crossflows.Min() <= 1, $"crossflows shared evenly (phase {phase}, {living} living: {string.Join(",", crossflows)})");
            AssertEqual(true, aimed.Max() - aimed.Min() <= 1, $"aimed notes shared evenly (phase {phase}, {living} living: {string.Join(",", aimed)})");
            if (phase < 3) AssertEqual(true, crossflows.Min() > 0, "everyone takes crossflows in the Acts");
        }
    }
    [DomainTest("Scarlet tracking samples reject stale duplicate outside and post-lock updates")]
    private static void ScarletTrackingSamples()
    {
        var p = TechniqueExample(CrimsonTechnique.TrackingBeam);
        int locked = CrimsonTrackingBeam.LockAt(p);
        AssertEqual(true, CrimsonTrackingBeam.CanAccept(p, locked - 3, locked, p.Target), "final aim accepted");
        foreach (int tick in new[] { locked - 3, locked - 4, locked + 1, int.MaxValue })
            AssertEqual(false, CrimsonTrackingBeam.CanAccept(p, locked - 3, tick, p.Target), "stale/late aim denied");
        AssertEqual(false, CrimsonTrackingBeam.CanAccept(p, 550, 551, new(float.NaN, 5000)), "finite aim");
        AssertEqual(false, CrimsonTrackingBeam.CanAccept(p, 550, 551, new(0, 0)), "field bounded aim");
        AssertEqual(p.Born, locked, "Doll-style warning freezes on its first sample");
        using var bytes = new MemoryStream();
        using (var w = new BinaryWriter(bytes, System.Text.Encoding.UTF8, true)) CrimsonTrackingBeam.WriteAim(w, locked, p.Target);
        using var r = new BinaryReader(new MemoryStream(bytes.ToArray()));
        AssertEqual((locked, p.Target), CrimsonTrackingBeam.ReadAim(r), "aim codec");
        for (int n = 0; n < bytes.Length; n++)
        {
            bool rejected = false;
            try { using var truncated = new BinaryReader(new MemoryStream(bytes.ToArray(), 0, n)); CrimsonTrackingBeam.ReadAim(truncated); }
            catch (EndOfStreamException) { rejected = true; }
            AssertEqual(true, rejected, "truncated aim rejected");
        }
    }
    [DomainTest("Scarlet every source emits exactly one beam inside its fixed forecast")]
    private static void ScarletSingleBeamFootprint()
    {
        var p = TechniqueExample(CrimsonTechnique.TrackingBeam) with { End = 632, LastEnd = 632 };
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[4];
        for (byte source = 0; source < 4; source++)
        {
            p = p with { Source = source }; p.Validate();
            AssertEqual(1, CrimsonTechniqueGeometry.Write(p, p.Fire, strokes, true), "one forecast");
            var forecast = strokes[0];
            bool OnEdge(CrimsonPoint point) => Math.Abs(point.X - p.Field.Left) < .02f || Math.Abs(point.X - p.Field.Right) < .02f
                || Math.Abs(point.Y - p.Field.Top) < .02f || Math.Abs(point.Y - p.Field.Bottom) < .02f;
            AssertEqual(true, OnEdge(forecast.A) && OnEdge(forecast.B), "full edge-to-edge forecast, never starts at boss");
            for (float age = p.Fire; age < p.End; age += .25f)
            {
                if (age == p.Fire) { AssertEqual(0, CrimsonTechniqueGeometry.Write(p, age, strokes), "harmless ignition"); continue; }
                AssertEqual(1, CrimsonTechniqueGeometry.Write(p, age, strokes), "one live beam");
                AssertEqual(forecast.A, strokes[0].A, "same field-edge emitter");
                AssertEqual(true, strokes[0].Radius <= forecast.Radius && strokes[0].Radius >= 0, "honest expanding/fading width");
                AssertEqual(true, (strokes[0].B - forecast.A).LengthSquared <= (forecast.B - forecast.A).LengthSquared + 1, "honest extending reach");
            }
            AssertEqual(0, CrimsonTechniqueGeometry.Write(p, p.End, strokes), "no aftermath damage");
        }
    }
    [DomainTest("Scarlet beam target identity is required and its aim eases without overshoot")]
    private static void ScarletBeamIdentity()
    {
        var p = TechniqueExample(CrimsonTechnique.TrackingBeam);
        foreach (var forged in new[] { p with { TargetSlot = -1 }, p with { TargetConnection = Guid.Empty } })
        {
            bool rejected = false; try { forged.Validate(); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "connection-bound target");
        }
        var old = new CrimsonPoint(2000, 2000); var target = new CrimsonPoint(2500, 2500);
        for (int i = 0; i < 40; i++)
        {
            var next = CrimsonTrackingBeam.Follow(old, target);
            AssertEqual(true, next.X >= old.X && next.X <= target.X && next.Y >= old.Y && next.Y <= target.Y, "no overshoot");
            old = next;
        }
    }
}
