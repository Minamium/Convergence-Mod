using System;
using System.IO;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static CrimsonGesturePlan TechniqueExample(CrimsonTechnique technique, int step = 0)
    {
        var f = Convergence.Common.Raids.Arena.RaidFieldGeometry.FromGround(8000, 6000);
        var focus = new CrimsonPoint(8000, 5500);
        return new(Guid.Parse("3c051a1d-dd29-4844-8353-56347645a878"), 3, 500, 8, (byte)step,
            (byte)CrimsonTechniqueGeometry.Owner(technique), technique, (byte)step, 3, 1,
            520, 544 + step * 14, 600 + step * 14, 605 + step * 14, 600, 633,
            new(8000, 5230), CrimsonTechniqueGeometry.Stage(f, focus, technique, 8),
            CrimsonTechniqueGeometry.Target(f, focus, technique, 8), 8000, 6000, 450,
            technique is CrimsonTechnique.TrackingBeam or CrimsonTechnique.SideBeams or CrimsonTechnique.SpatialRift ? (short)0 : (short)-1,
            technique is CrimsonTechnique.TrackingBeam or CrimsonTechnique.SideBeams or CrimsonTechnique.SpatialRift ? Guid.Parse("3c051a1d-dd29-4844-8353-56347645a879") : Guid.Empty);
    }
    [DomainTest("Scarlet each apparition owns three nonrepeating physical techniques")]
    private static void ScarletTechniqueVariety()
    {
        for (int source = 0; source < 4; source++)
        {
            var seen = new HashSet<CrimsonTechnique>();
            CrimsonTechnique? previous = null;
            for (int serial = 0; serial < 100; serial++)
            {
                var current = CrimsonTechniqueGeometry.Select(source, serial);
                AssertEqual(source, CrimsonTechniqueGeometry.Owner(current), "species owns its vocabulary");
                AssertEqual(false, previous == current, "no adjacent repeated skill");
                seen.Add(current); previous = current;
            }
            AssertEqual(source == 3 ? 2 : 3, seen.Count, "full skill deck is reachable");
        }
    }
    [DomainTest("Scarlet retained techniques and tracking beam are bounded finite and harmless outside their notes")]
    private static void ScarletTechniqueGeometryBounds()
    {
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var technique in Enum.GetValues<CrimsonTechnique>())
        {
            var p = TechniqueExample(technique); p.Validate();
            AssertEqual(0, CrimsonTechniqueGeometry.Write(p, p.Fire - .01f, strokes), "no warning damage");
            AssertEqual(0, CrimsonTechniqueGeometry.Write(p, p.End, strokes), "exclusive end and harmless tail");
            for (float age = p.Fire; age < p.End; age += .25f)
            {
                int count = CrimsonTechniqueGeometry.Write(p, age, strokes);
                if ((p.Aimed || p.IsRift) && age == p.Fire) { AssertEqual(0, count, "zero-width ignition is harmless"); continue; }
                AssertEqual(true, count is > 0 and <= CrimsonTechniqueGeometry.MaximumStrokes, "bounded strokes");
                foreach (var stroke in strokes[..count])
                {
                    AssertEqual(true, stroke.A.Finite && stroke.B.Finite, "finite curve");
                    AssertEqual(true, stroke.Radius is > 0 and < 200, "bounded body/contact width");
                }
            }
        }
    }
    [DomainTest("Scarlet complete forecasts contain sampled damaging paths and physical widths")]
    private static void ScarletTechniqueForecastCoverage()
    {
        Span<CrimsonStroke> forecast = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var technique in Enum.GetValues<CrimsonTechnique>()) for (int step = 0; step < 3; step++)
        {
            var p = TechniqueExample(technique, step);
            int nf = CrimsonTechniqueGeometry.Write(p, p.Fire, forecast, true);
            for (float age = p.Fire; age < p.End; age += .5f)
            {
                int nl = CrimsonTechniqueGeometry.Write(p, age, live);
                for (int i = 0; i < nl; i++)
                {
                    var s = live[i]; var delta = s.B - s.A;
                    var n = delta.LengthSquared < .0001f ? new CrimsonPoint(1, 0)
                        : new CrimsonPoint(-delta.Y, delta.X) * (1 / MathF.Sqrt(delta.LengthSquared));
                    for (int k = 0; k < 3; k++) for (int side = -1; side <= 1; side++)
                    {
                        var point = CrimsonPoint.Lerp(s.A, s.B, k * .5f) + n * (side * s.Radius);
                        bool covered = false;
                        for (int j = 0; j < nf; j++)
                            covered |= CrimsonTechniqueGeometry.Intersects(forecast[j], point.X - 1, point.Y - 1, 2, 2);
                        AssertEqual(true, covered, $"forecast covers {technique} step{step} t{age} segment{i}");
                    }
                }
            }
        }
    }
    [DomainTest("Scarlet physical phrases leave player-sized space rather than unavoidable full-field damage")]
    private static void ScarletTechniqueSafeSpace()
    {
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var technique in Enum.GetValues<CrimsonTechnique>())
        {
            bool found = false;
            for (int x = 7000; x <= 9000 && !found; x += 100) for (int y = 5050; y <= 5800 && !found; y += 75)
            {
                bool safe = true;
                for (int step = 0; step < 3; step++)
                {
                    var p = TechniqueExample(technique, step);
                    int count = CrimsonTechniqueGeometry.Write(p, p.Fire, strokes, true);
                    for (int i = 0; i < count; i++) if (CrimsonTechniqueGeometry.Intersects(strokes[i], x, y, 20, 42)) safe = false;
                }
                found = safe;
            }
            AssertEqual(true, found, $"complete body can avoid {technique} example phrase");
        }
    }
    [DomainTest("Scarlet rush and crash body paths join every musical note without position resets")]
    private static void ScarletTechniqueBodyContinuity()
    {
        foreach (var t in new[] { CrimsonTechnique.MantleRush, CrimsonTechnique.CrownCrash })
        {
            var p = TechniqueExample(t);
            AssertEqual(p.From, p.Body(p.Begin), "windup begins at accepted body position");
            AssertEqual(p.Stage, p.Body(p.FirstFire), "windup joins first strike");
            for (int step = 0; step < 2; step++)
            {
                var left = TechniqueExample(t, step); var right = TechniqueExample(t, step + 1);
                AssertEqual(true, (left.Body(left.End) - right.Body(right.Fire)).LengthSquared < .001f, "burst sections connect");
            }
            var last = TechniqueExample(t, 2);
            AssertEqual(last.Target, last.Body(last.LastEnd), "final note reaches shared destination");
        }
    }
    [DomainTest("Scarlet physical codecs reject every truncation and cross-species forged descriptors")]
    private static void ScarletTechniqueCodec()
    {
        foreach (var technique in Enum.GetValues<CrimsonTechnique>())
        {
            var p = TechniqueExample(technique);
            using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) p.Write(writer);
            byte[] bytes = stream.ToArray();
            using (var r = new BinaryReader(new MemoryStream(bytes))) AssertEqual(p, CrimsonGesturePlan.Read(r), "native descriptor round trip");
            for (int length = 0; length < bytes.Length; length++)
            {
                bool rejected = false;
                try { using var reader = new BinaryReader(new MemoryStream(bytes, 0, length)); CrimsonGesturePlan.Read(reader); }
                catch (IOException) { rejected = true; }
                AssertEqual(true, rejected, "all truncated prefixes rejected");
            }
            if (!p.Aimed)
                CheckRejected(p with { Source = (byte)((p.Source + 1) % 4) });
            CheckRejected(p with { Stage = new(float.NaN, 5000) });
            CheckRejected(p with { Fire = int.MinValue });
            CheckRejected(p with { Steps = 0 });
            CheckRejected(p with { Technique = (CrimsonTechnique)255 });
        }
        void CheckRejected(CrimsonGesturePlan p)
        {
            bool rejected = false; try { p.Validate(); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid descriptor rejected before state replacement");
        }
    }
    [DomainTest("Scarlet capsule collision covers swept crossings round caps and complete player bodies")]
    private static void ScarletTechniqueCollision()
    {
        var dash = new CrimsonStroke(new(0, 0), new(700, 0), 30);
        AssertEqual(true, CrimsonTechniqueGeometry.Intersects(dash, 310, -21, 20, 42), "no fast-dash tunneling");
        AssertEqual(false, CrimsonTechniqueGeometry.Intersects(dash, 310, 31, 20, 42), "outside capsule is safe");
        AssertEqual(true, CrimsonTechniqueGeometry.Intersects(new(new(0, 0), new(0, 0), 50), 48, -2, 10, 4), "disk cap");
        AssertEqual(false, CrimsonTechniqueGeometry.Intersects(new(new(0, 0), new(0, 0), 50), 40, 40, 2, 2), "round corner not square cap");
    }
    [DomainTest("Doll attendant requires its own exact preparation rather than any protected pedestal")]
    private static void ScarletPedestalIsolation()
    {
        foreach (var state in Enum.GetValues<FirstSeveranceCoreProtectionState>())
        {
            AssertEqual(false, FirstSeveranceDollActivation.ShowAttendant(state, false), "foreign Raid or missing owner cannot display Doll");
            AssertEqual(state == FirstSeveranceCoreProtectionState.Preparing,
                FirstSeveranceDollActivation.ShowAttendant(state, true), "Doll preparation preserved");
        }
    }
}
