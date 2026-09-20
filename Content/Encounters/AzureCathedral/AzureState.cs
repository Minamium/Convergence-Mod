using System;
using System.IO;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.AzureCathedral;

internal enum AzureStage : byte { Deployment, Ready, Countdown, Performance, Victory, Defeat }
internal readonly record struct AzureMember(byte Slot, Guid Connection, bool Ready, bool Out);
internal readonly record struct AzureState(Guid Fight, int Age, int MusicStart, int UnlockAt, int EndAt,
    AzureStage Stage, AzureMember[] Members, int GroundX, int GroundY, int GirlMax, int WormMax,
    int GirlLife, int WormLife, short WormSlot, bool Enraged)
{
    internal RaidFieldGeometry Field => RaidFieldGeometry.FromGround(GroundX, GroundY);
    internal bool Contains(int slot) => Array.Exists(Members ?? Array.Empty<AzureMember>(), m => m.Slot == slot && !m.Out);
    internal bool Live => Fight != Guid.Empty && Stage == AzureStage.Performance;
    internal int TotalLife => GirlLife + WormLife;
    internal int TotalMax => GirlMax + WormMax;
    internal bool CanReplace(in AzureState old) => old.Fight == Guid.Empty || Fight == old.Fight && Age >= old.Age
        && Stage >= old.Stage && GroundX == old.GroundX && GroundY == old.GroundY
        && (old.MusicStart < 0 || MusicStart == old.MusicStart && UnlockAt == old.UnlockAt
            && GirlMax == old.GirlMax && WormMax == old.WormMax)
        && (old.WormSlot < 0 || WormSlot == old.WormSlot)
        && (!old.Enraged || Enraged) && (old.EndAt < 0 || EndAt == old.EndAt && Stage == old.Stage);
    internal void WriteEnvelope(BinaryWriter w)
    {
        w.Write(Fight != Guid.Empty); if (Fight == Guid.Empty) return;
        w.Write(Fight.ToByteArray()); w.Write(Age); w.Write(MusicStart); w.Write(UnlockAt); w.Write(EndAt); w.Write((byte)Stage);
        w.Write(GroundX); w.Write(GroundY); w.Write(GirlMax); w.Write(WormMax); w.Write(GirlLife); w.Write(WormLife);
        w.Write(WormSlot); w.Write(Enraged); w.Write((byte)Members.Length);
        foreach (var m in Members) { w.Write(m.Slot); w.Write(m.Connection.ToByteArray()); w.Write(m.Ready); w.Write(m.Out); }
    }
    internal static bool Presence(BinaryReader r) => r.ReadByte() switch { 0 => false, 1 => true, _ => throw new InvalidDataException("azure.presence") };
    internal static Guid Id(BinaryReader r)
    { var b = r.ReadBytes(16); if (b.Length != 16 || new Guid(b) == Guid.Empty) throw new InvalidDataException("azure.identity"); return new Guid(b); }
    internal static AzureState? ReadEnvelope(BinaryReader r)
    {
        if (!Presence(r)) return null;
        var fight = Id(r); int age = r.ReadInt32(), music = r.ReadInt32(), unlock = r.ReadInt32(), ending = r.ReadInt32();
        var stage = (AzureStage)r.ReadByte(); int x = r.ReadInt32(), y = r.ReadInt32();
        int gm = r.ReadInt32(), wm = r.ReadInt32(), gl = r.ReadInt32(), wl = r.ReadInt32();
        short worm = r.ReadInt16(); bool enraged = Presence(r); int count = r.ReadByte();
        if (age is < 0 or > 72000 || !Enum.IsDefined(stage) || music is < -1 or > 72000 || unlock is < -1 or > 72500
            || ending < -1 || ending > age || x is < 1600 or > 400000 || y is < 1440 or > 150000
            || gm is < 1 or > 15000000 || wm is < 1 or > 30000000 || gl < 0 || gl > gm || wl < 0 || wl > wm
            || worm is < -1 or >= 200 || count is < 1 or > AzureRules.Members
            || stage >= AzureStage.Countdown && stage <= AzureStage.Performance && (music < 0 || unlock != music + AzureRules.Intro)
            || (stage >= AzureStage.Victory) != (ending >= 0)) throw new InvalidDataException("azure.state");
        var members = new AzureMember[count];
        for (int i = 0; i < count; i++)
        {
            byte slot = r.ReadByte(); var id = Id(r); bool ready = Presence(r), gone = Presence(r);
            if (slot >= 255 || i > 0 && slot <= members[i - 1].Slot) throw new InvalidDataException("azure.roster");
            members[i] = new(slot, id, ready, gone);
        }
        return new(fight, age, music, unlock, ending, stage, members, x, y, gm, wm, gl, wl, worm, enraged);
    }
}

internal enum AzureAttackKind : byte { Icicle, MouthBeam, GlassRain }
internal readonly record struct AzureAttackPlan(Guid Fight, short Girl, AzureAttackKind Kind, int Born, int Fire, int End,
    float X, float Y, float Angle, float Length, float Width, int Damage)
{
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight != Guid.Empty); if (Fight == Guid.Empty) return;
        w.Write(Fight.ToByteArray()); w.Write(Girl); w.Write((byte)Kind); w.Write(Born); w.Write(Fire); w.Write(End);
        w.Write(X); w.Write(Y); w.Write(Angle); w.Write(Length); w.Write(Width); w.Write(Damage);
    }
    internal static AzureAttackPlan? Read(BinaryReader r)
    {
        if (!AzureState.Presence(r)) return null;
        var id = AzureState.Id(r); short girl = r.ReadInt16(); var kind = (AzureAttackKind)r.ReadByte();
        int born = r.ReadInt32(), fire = r.ReadInt32(), end = r.ReadInt32();
        float x = r.ReadSingle(), y = r.ReadSingle(), angle = r.ReadSingle(), length = r.ReadSingle(), width = r.ReadSingle(); int damage = r.ReadInt32();
        if (girl is < 0 or >= 200 || !Enum.IsDefined(kind) || born is < 0 or > 72000 || fire - (long)born is < 36 or > 180
            || end - (long)fire is < 1 or > 300 || !float.IsFinite(x) || !float.IsFinite(y) || Math.Abs(x) > 400000 || Math.Abs(y) > 150000
            || !float.IsFinite(angle) || Math.Abs(angle) > 20 || !float.IsFinite(length) || length is < 1 or > 4000
            || !float.IsFinite(width) || width is < 1 or > 200 || damage is < 1 or > 2000) throw new InvalidDataException("azure.attack");
        return new(id, girl, kind, born, fire, end, x, y, angle, length, width, damage);
    }
}
