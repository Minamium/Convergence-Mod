using System;
using System.IO;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Immutable visual result positions, captured before lethal native damage.
// Do not reconstruct a verdict at a dead player's respawn/current position.
internal static class CrimsonChorusImpactPositions
{
    internal const int TailTicks = 48;
    internal static void Write(BinaryWriter writer, CrimsonPoint[] positions)
    {
        writer.Write((byte)positions.Length);
        foreach (var p in positions) { writer.Write(p.X); writer.Write(p.Y); }
    }
    internal static CrimsonPoint[] Read(BinaryReader reader, bool resolved, byte members)
    {
        int count = reader.ReadByte();
        if (!resolved && count != 0 || resolved && (count is < 1 or > 8 || members >= 1 << count))
            throw new InvalidDataException("crimson.chorus_positions_invalid");
        var points = new CrimsonPoint[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = new(reader.ReadSingle(), reader.ReadSingle());
            if (!points[i].Finite || points[i].X is < 0 or > 400000 || points[i].Y is < 0 or > 150000)
                throw new InvalidDataException("crimson.chorus_position_invalid");
        }
        return points;
    }
}
