using System;
using System.IO;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal enum CrimsonStage : byte { Deployment, Ready, Countdown, Performance, Victory, Defeat }
internal readonly record struct CrimsonMember(byte Slot, Guid Connection, bool Ready, bool Out);
internal readonly record struct CrimsonState(Guid Fight, int Age, int MusicStart, int PurgeTick, CrimsonStage Stage, CrimsonMember[] Members)
{
    internal const int MaxMembers = 8;
    internal bool Contains(int slot) => Array.Exists(Members ?? Array.Empty<CrimsonMember>(), m => m.Slot == slot && !m.Out);
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight.ToByteArray()); w.Write(Age); w.Write(MusicStart); w.Write(PurgeTick); w.Write((byte)Stage);
        w.Write((byte)Members.Length);
        foreach (var m in Members) { w.Write(m.Slot); w.Write(m.Connection.ToByteArray()); w.Write(m.Ready); w.Write(m.Out); }
    }
    internal static CrimsonState Read(BinaryReader r)
    {
        byte[] f = r.ReadBytes(16); int age = r.ReadInt32(), start = r.ReadInt32(), purge = r.ReadInt32();
        var stage = (CrimsonStage)r.ReadByte(); int count = r.ReadByte();
        if (f.Length != 16 || new Guid(f) == Guid.Empty || age is < 0 or > 72000 || start is < -1 or > 72000
            || purge is < -1 or > 72000 || !Enum.IsDefined(stage) || count is < 1 or > MaxMembers)
            throw new InvalidDataException("crimson.actor_invalid");
        var members = new CrimsonMember[count];
        for (int i = 0; i < count; i++)
        {
            byte slot = r.ReadByte(); byte[] bytes = r.ReadBytes(16); bool ready = r.ReadBoolean(), eliminated = r.ReadBoolean();
            if (slot >= 255 || bytes.Length != 16 || new Guid(bytes) == Guid.Empty
                || i > 0 && slot <= members[i - 1].Slot) throw new InvalidDataException("crimson.member_invalid");
            members[i] = new(slot, new Guid(bytes), ready, eliminated);
        }
        if (stage is CrimsonStage.Countdown or CrimsonStage.Performance && start < 0) throw new InvalidDataException("crimson.clock_missing");
        return new(new Guid(f), age, start, purge, stage, members);
    }
}

internal enum CrimsonShape : byte { Slash, Bolt }
internal readonly record struct CrimsonHazard(Guid Fight, short Boss, int Born, int Fire, int End,
    CrimsonShape Shape, float X, float Y, float DX, float DY, float Length, float Width, int Damage)
{
    internal bool Live(float age) => age >= Fire && age < End;
    internal float Travel(float age) => Math.Clamp((age - Fire) / 16f, 0, 1) * Length;
    internal float Reach(float age) => Shape == CrimsonShape.Slash ? Length * Math.Clamp((age - Fire) / 4f, 0, 1) : 110;
    internal float HitWidth(float age) => Width * Math.Clamp((age - Fire) / 3f, 0, 1);
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight.ToByteArray()); w.Write(Boss); w.Write(Born); w.Write(Fire); w.Write(End); w.Write((byte)Shape);
        w.Write(X); w.Write(Y); w.Write(DX); w.Write(DY); w.Write(Length); w.Write(Width); w.Write(Damage);
    }
    internal static CrimsonHazard Read(BinaryReader r)
    {
        byte[] f = r.ReadBytes(16); short boss = r.ReadInt16(); int born = r.ReadInt32(), fire = r.ReadInt32(), end = r.ReadInt32();
        var shape = (CrimsonShape)r.ReadByte();
        float x = r.ReadSingle(), y = r.ReadSingle(), dx = r.ReadSingle(), dy = r.ReadSingle(), length = r.ReadSingle(), width = r.ReadSingle();
        int damage = r.ReadInt32();
        if (f.Length != 16 || new Guid(f) == Guid.Empty || boss is < 0 or >= 200 || born < 0 || fire - born != CrimsonScore.WarningTicks
            || end <= fire || end - fire > 45 || !Enum.IsDefined(shape) || !float.IsFinite(x) || !float.IsFinite(y)
            || Math.Abs(x) > 400000 || Math.Abs(y) > 150000 || !float.IsFinite(dx) || !float.IsFinite(dy)
            || Math.Abs(dx * dx + dy * dy - 1) > .02 || !float.IsFinite(length) || length is < 1 or > 2000
            || !float.IsFinite(width) || width is < 1 or > 120 || damage is < 1 or > 2000)
            throw new InvalidDataException("crimson.hazard_invalid");
        return new(new Guid(f), boss, born, fire, end, shape, x, y, dx, dy, length, width, damage);
    }
}
