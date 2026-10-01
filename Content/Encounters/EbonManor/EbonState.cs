using System;
using System.IO;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.EbonManor;

internal enum EbonStage : byte { Deployment, Ready, Countdown, Performance, Victory, Defeat }
internal enum EbonPhase : byte { ActOne, ActTwo, Finale }
internal readonly record struct EbonMember(byte Slot, Guid Connection, bool Ready, bool Out, EbonRecoveryState Recovery = default);

// One accepted authority projection. Clocks are immutable once set; stages and
// phases only advance; health only falls; the roster freezes at Countdown.
internal readonly record struct EbonState(Guid Fight, int Age, int MusicStart, int UnlockAt, int EndAt,
    EbonStage Stage, EbonMember[] Members, int GroundX, int GroundY, int MaxLife, int Life,
    EbonPhase Phase = EbonPhase.ActOne, int PhaseAt = -1)
{
    internal RaidFieldGeometry Field => RaidFieldGeometry.FromGround(GroundX, GroundY);
    internal bool Contains(int slot) => Array.Exists(Members ?? Array.Empty<EbonMember>(), m => m.Slot == slot && !m.Out);
    internal bool CanFight(int slot) => Array.Exists(Members ?? Array.Empty<EbonMember>(), m => m.Slot == slot && !m.Out && !m.Recovery.Downed);
    // The phase's first attack downbeat: the A drop for Act One, then the end of
    // each transition lead-in that plays the next cue's opening bars.
    internal int Epoch => Phase == EbonPhase.ActOne ? UnlockAt : PhaseAt + EbonRules.Lead(Phase);
    internal bool Transition(float age) => Stage == EbonStage.Performance && Phase != EbonPhase.ActOne && age < Epoch;
    internal bool Live => Fight != Guid.Empty && Stage == EbonStage.Performance && Life > 0 && Age >= Epoch;
    internal bool Cinematic => Stage is EbonStage.Deployment or EbonStage.Countdown or EbonStage.Victory or EbonStage.Defeat || Transition(Age);
    internal bool CanReplace(in EbonState old) => old.Fight == Guid.Empty || Fight == old.Fight && Age >= old.Age
        && Stage >= old.Stage && GroundX == old.GroundX && GroundY == old.GroundY
        && (old.MusicStart < 0 || MusicStart == old.MusicStart && UnlockAt == old.UnlockAt && MaxLife == old.MaxLife && Life <= old.Life)
        // Act One's clock is the music start, first assigned at AllReady; afterwards
        // an act's start tick is immutable.
        && Phase >= old.Phase && (Phase != old.Phase || PhaseAt == old.PhaseAt || old.MusicStart < 0)
        && (old.EndAt < 0 || EndAt == old.EndAt && Stage == old.Stage)
        && RecoveryCanReplace(old);
    private bool RecoveryCanReplace(in EbonState old)
    {
        if (old.Stage < EbonStage.Countdown) return true; // Preparation may rebuild the roster.
        if (Members.Length != old.Members.Length) return false;
        for (int i = 0; i < Members.Length; i++)
            if (Members[i].Slot != old.Members[i].Slot || Members[i].Connection != old.Members[i].Connection
                || old.Members[i].Out && !Members[i].Out || !Members[i].Recovery.CanReplace(old.Members[i].Recovery)) return false;
        return true;
    }
    internal void WriteEnvelope(BinaryWriter w)
    {
        w.Write(Fight != Guid.Empty); if (Fight == Guid.Empty) return;
        w.Write(Fight.ToByteArray()); w.Write(Age); w.Write(MusicStart); w.Write(UnlockAt); w.Write(EndAt); w.Write((byte)Stage);
        w.Write(GroundX); w.Write(GroundY); w.Write(MaxLife); w.Write(Life); w.Write((byte)Phase); w.Write(PhaseAt);
        w.Write((byte)Members.Length);
        foreach (var m in Members) { w.Write(m.Slot); w.Write(m.Connection.ToByteArray()); w.Write(m.Ready); w.Write(m.Out); m.Recovery.Write(w); }
    }
    internal static bool Presence(BinaryReader r) => r.ReadByte() switch { 0 => false, 1 => true, _ => throw new InvalidDataException("ebon.presence") };
    internal static Guid Id(BinaryReader r)
    { var b = r.ReadBytes(16); if (b.Length != 16 || new Guid(b) == Guid.Empty) throw new InvalidDataException("ebon.identity"); return new Guid(b); }
    internal static EbonState? ReadEnvelope(BinaryReader r)
    {
        if (!Presence(r)) return null;
        var fight = Id(r); int age = r.ReadInt32(), music = r.ReadInt32(), unlock = r.ReadInt32(), ending = r.ReadInt32();
        var stage = (EbonStage)r.ReadByte(); int x = r.ReadInt32(), y = r.ReadInt32(), max = r.ReadInt32(), life = r.ReadInt32();
        var phase = (EbonPhase)r.ReadByte(); int phaseAt = r.ReadInt32(); int count = r.ReadByte();
        if (age is < 0 or > 72000 || !Enum.IsDefined(stage) || !Enum.IsDefined(phase) || music is < -1 or > 72000
            || unlock is < -1 or > 73000 || ending < -1 || ending > age || x is < 1600 or > 400000 || y is < 1440 or > 150000
            || max < 1 || max > EbonRules.Life(EbonRules.Members) || life < 0 || life > max || count is < 1 or > EbonRules.Members
            || (stage >= EbonStage.Countdown) != (music >= 0) || music >= 0 && unlock != music + EbonRules.Intro
            || (phase == EbonPhase.ActOne ? phaseAt != music : phaseAt < unlock || phaseAt > age || stage < EbonStage.Performance)
            || (stage >= EbonStage.Victory) != (ending >= 0)) throw new InvalidDataException("ebon.state");
        var members = new EbonMember[count];
        for (int i = 0; i < count; i++)
        {
            byte slot = r.ReadByte(); var id = Id(r); bool ready = Presence(r), gone = Presence(r);
            if (slot >= 255 || i > 0 && slot <= members[i - 1].Slot) throw new InvalidDataException("ebon.roster");
            members[i] = new(slot, id, ready, gone, EbonRecoveryState.Read(r));
        }
        return new(fight, age, music, unlock, ending, stage, members, x, y, max, life, phase, phaseAt);
    }
}

internal enum EbonAttackKind : byte { Thread, Chandelier, Loom, Shears, Waltz, Web }

// An immutable authority hazard. Geometry for any tick derives from these
// fields and EbonRules only, on the server (collision) and on clients (art).
internal readonly record struct EbonAttackPlan(Guid Fight, short Boss, EbonAttackKind Kind, int Born, int Fire, int End,
    float X, float Y, float Angle, float Length, float Width, int Damage, byte Variant = 0, float Spin = 0)
{
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight != Guid.Empty); if (Fight == Guid.Empty) return;
        w.Write(Fight.ToByteArray()); w.Write(Boss); w.Write((byte)Kind); w.Write(Born); w.Write(Fire); w.Write(End);
        w.Write(X); w.Write(Y); w.Write(Angle); w.Write(Length); w.Write(Width); w.Write(Damage); w.Write(Variant); w.Write(Spin);
    }
    internal static EbonAttackPlan? Read(BinaryReader r)
    {
        if (!EbonState.Presence(r)) return null;
        var id = EbonState.Id(r); short boss = r.ReadInt16(); var kind = (EbonAttackKind)r.ReadByte();
        int born = r.ReadInt32(), fire = r.ReadInt32(), end = r.ReadInt32();
        float x = r.ReadSingle(), y = r.ReadSingle(), angle = r.ReadSingle(), length = r.ReadSingle(), width = r.ReadSingle();
        int damage = r.ReadInt32(); byte variant = r.ReadByte(); float spin = r.ReadSingle();
        if (boss is < 0 or >= 200 || !Enum.IsDefined(kind) || born is < 0 or > 72000 || fire - (long)born is < 20 or > 240
            || end - (long)fire is < 1 or > 420 || !float.IsFinite(x) || !float.IsFinite(y) || Math.Abs(x) > 400000 || Math.Abs(y) > 150000
            || !float.IsFinite(angle) || Math.Abs(angle) > 20 || !float.IsFinite(length) || length is < 1 or > 6000
            || !float.IsFinite(width) || width is < 1 or > 300 || damage is < 1 or > 2000
            || !float.IsFinite(spin) || Math.Abs(spin) > .05f
            || kind switch
            {
                EbonAttackKind.Thread => variant > 6 || spin != 0,
                EbonAttackKind.Chandelier => variant > 1 || spin != 0,
                EbonAttackKind.Waltz => variant is < 3 or > 10,
                EbonAttackKind.Web => variant > 15 || spin != 0,
                _ => variant != 0 || spin != 0,
            }) throw new InvalidDataException("ebon.attack");
        return new(id, boss, kind, born, fire, end, x, y, angle, length, width, damage, variant, spin);
    }
}
