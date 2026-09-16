using System;
using System.IO;

namespace Convergence.Content.Items.Oboro;

internal enum OboroAction : byte { Hello, Swing, Zanshin }
internal readonly record struct OboroRequest(OboroAction Action, ulong Generation, uint Nonce, float Aim)
{
    internal void Write(BinaryWriter w) { w.Write((byte)Action); w.Write(Generation); w.Write(Nonce); w.Write(Aim); }
    internal static OboroRequest Read(BinaryReader r)
    {
        var value = new OboroRequest((OboroAction)r.ReadByte(), r.ReadUInt64(), r.ReadUInt32(), r.ReadSingle());
        if (!Enum.IsDefined(value.Action) || value.Nonce == 0 || !float.IsFinite(value.Aim) || Math.Abs(value.Aim) > MathF.PI)
            throw new IOException("oboro.invalid_request");
        return value;
    }
}

internal readonly record struct OboroSnapshot(byte Player, ulong Generation, uint Revision, uint Swing,
    byte Step, ushort Age, ushort Duration, float Aim, sbyte Facing, ushort Zanshin)
{
    internal bool Valid => Player < 255 && Generation > 0 && Revision > 0 && Step < 3 && Duration <= 84
        && Age <= Duration && (Duration == 0 || Duration >= 8) && Facing is -1 or 1
        && float.IsFinite(Aim) && Math.Abs(Aim) <= MathF.PI && Zanshin <= OboroRules.ZanshinTicks;
    internal bool NewerThan(OboroSnapshot previous) => Valid && (Generation > previous.Generation
        || Generation == previous.Generation && Revision > previous.Revision);
    internal void Write(BinaryWriter w)
    {
        w.Write(Player); w.Write(Generation); w.Write(Revision); w.Write(Swing); w.Write(Step);
        w.Write(Age); w.Write(Duration); w.Write(Aim); w.Write(Facing); w.Write(Zanshin);
    }
    internal static OboroSnapshot Read(BinaryReader r)
    {
        var v = new OboroSnapshot(r.ReadByte(), r.ReadUInt64(), r.ReadUInt32(), r.ReadUInt32(), r.ReadByte(),
            r.ReadUInt16(), r.ReadUInt16(), r.ReadSingle(), r.ReadSByte(), r.ReadUInt16());
        if (!v.Valid) throw new IOException("oboro.invalid_snapshot");
        return v;
    }
}

internal readonly record struct OboroBurst(float X, float Y, float[] Angles)
{
    internal void Write(BinaryWriter w) { w.Write(X); w.Write(Y); w.Write((byte)Angles.Length); foreach (float angle in Angles) w.Write(angle); }
    internal static OboroBurst Read(BinaryReader r)
    {
        float x = r.ReadSingle(), y = r.ReadSingle(); int count = r.ReadByte();
        if (count is < 1 or > OboroRules.MaximumMarks) throw new IOException("oboro.invalid_burst_count");
        var angles = new float[count];
        for (int i = 0; i < count; i++)
        {
            angles[i] = r.ReadSingle();
            if (!float.IsFinite(angles[i]) || Math.Abs(angles[i]) > 20) throw new IOException("oboro.invalid_burst_angle");
        }
        var v = new OboroBurst(x, y, angles);
        if (!float.IsFinite(v.X) || !float.IsFinite(v.Y) || Math.Abs(v.X) > 1_000_000 || Math.Abs(v.Y) > 1_000_000
            )
            throw new IOException("oboro.invalid_burst");
        return v;
    }
}
