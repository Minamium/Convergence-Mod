using System;
using System.IO;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Ghost Samurai field stays anchored above the summoner feet and constrains every complete player body")]
    private static void SamuraiFieldBounds()
    {
        var field = SamuraiArenaBounds.Create(4000, 3000, 60000, 20000);
        AssertEqual(true, field.IsValid, "valid summon field");
        AssertEqual(4000f, field.CenterX, "summoner X captured");
        AssertEqual(2440f, field.CenterY, "center raised by half the unchanged height");
        AssertEqual(3000f, field.Bottom, "summoner feet become the bottom");
        AssertEqual(2560f, field.HalfWidth * 2, "same width as Doll field");
        AssertEqual(1120f, field.HalfHeight * 2, "same height as Doll field");
        for (int player = 0; player < 4; player++)
        foreach (var movement in new[] { (-10000f, -10000f), (60000f, 20000f), (4000f, 2500f), (float.NaN, 3000f) })
        {
            int width = 20 + player * 2, height = 42 + player * 2;
            var point = field.ClampBody(movement.Item1, movement.Item2, width, height, 2);
            AssertEqual(true, float.IsFinite(point.X) && float.IsFinite(point.Y), "dash/hook/mount/teleport result finite");
            AssertEqual(true, field.Contains(point.X, point.Y) && field.Contains(point.X + width, point.Y + height), "whole body remains inside");
            AssertEqual(point, field.ClampBody(point.X, point.Y, width, height, 2), "correction is idempotent");
        }
        AssertEqual((3990f, 2900f), field.ClampBody(3990, 2900, 20, 42), "ordinary inside movement unchanged");
        AssertEqual(false, field.Contains(1000, 3000), "distant outsider not admitted by field entry");
    }

    [DomainTest("Ghost Samurai world-edge summon rejection never changes arena size")]
    private static void SamuraiFieldWorldEdges()
    {
        foreach (var point in new[] { (400f, 3000f), (4000f, 400f), (59600f, 19600f), (10f, 10f), (3000f, 19990f) })
            AssertEqual(false, SamuraiArenaBounds.Create(point.Item1, point.Item2, 60000, 20000).IsValid, "reject before spawning; no shrinking or buried downward shift");
        var top = SamuraiArenaBounds.Create(1312, 1152, 60000, 20000);
        AssertEqual(true, top.IsValid, "full field fits at margins");
        AssertEqual(32f, top.Left, "left margin");
        AssertEqual(32f, top.Top, "top margin");
        AssertEqual(2560f, top.Right - top.Left, "width unchanged");
        AssertEqual(1120f, top.Bottom - top.Top, "height unchanged");
    }

    [DomainTest("Ghost Samurai arena wire preserves exact frozen geometry and rejects malformed or truncated bounds")]
    private static void SamuraiFieldWire()
    {
        var field = SamuraiArenaBounds.Create(4000.5f, 3000.25f, 60000, 20000);
        using var stream = new MemoryStream(); field.Write(new BinaryWriter(stream));
        byte[] data = stream.ToArray(); stream.Position = 0;
        AssertEqual(16, data.Length, "four bounded floats, no per-player packets");
        AssertEqual(field, SamuraiArenaBounds.Read(new BinaryReader(stream)), "late join obtains exact centered field");
        for (int length = 0; length < data.Length; length++)
        {
            bool rejected = false;
            try { SamuraiArenaBounds.Read(new BinaryReader(new MemoryStream(data, 0, length))); } catch (IOException) { rejected = true; }
            AssertEqual(true, rejected, "partial field never installed");
        }
        foreach (var invalid in new[] { field with { CenterX = float.NaN }, field with { HalfWidth = 1281 },
            field with { HalfHeight = float.PositiveInfinity }, field with { HalfHeight = 0 }, field with { CenterY = 0 } })
        {
            using var bytes = new MemoryStream(); invalid.Write(new BinaryWriter(bytes)); bytes.Position = 0;
            bool rejected = false;
            try { SamuraiArenaBounds.Read(new BinaryReader(bytes)); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "bounded geometry checked before replica mutation");
        }
    }
}
