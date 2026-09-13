using System;
using System.IO;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Ghost Samurai field stays centered on the summoner and constrains every complete player body")]
    private static void SamuraiFieldBounds()
    {
        var field = SamuraiArenaBounds.Create(4000, 3000, 60000, 20000);
        AssertEqual(true, field.IsValid, "valid summon field");
        AssertEqual(4000f, field.CenterX, "summoner X captured");
        AssertEqual(3000f, field.CenterY, "summoner Y captured, not a ground anchor");
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
        AssertEqual((3990f, 2979f), field.ClampBody(3990, 2979, 20, 42), "ordinary inside movement unchanged");
        AssertEqual(false, field.Contains(1000, 3000), "distant outsider not admitted by field entry");
    }

    [DomainTest("Ghost Samurai field shrinks symmetrically at world edges and rejects impossible centers")]
    private static void SamuraiFieldWorldEdges()
    {
        var edge = SamuraiArenaBounds.Create(400, 300, 60000, 20000);
        AssertEqual(true, edge.IsValid, "small but playable border field");
        AssertEqual(400f, edge.CenterX, "border handling never moves summon center");
        AssertEqual(300f, edge.CenterY, "border handling never grounds the field");
        AssertEqual(32f, edge.Left, "world margin preserved");
        AssertEqual(32f, edge.Top, "upper world margin preserved");
        var bottom = SamuraiArenaBounds.Create(59600, 19700, 60000, 20000);
        AssertEqual(59968f, bottom.Right, "right world margin");
        AssertEqual(19968f, bottom.Bottom, "bottom world margin");
        AssertEqual(false, SamuraiArenaBounds.Create(10, 10, 60000, 20000).IsValid, "unplayable world-edge summon rejected before spawn");
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

