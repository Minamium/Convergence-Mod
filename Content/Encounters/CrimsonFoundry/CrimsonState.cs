using System;
using System.IO;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal enum CrimsonStage : byte { Deployment, Ready, Countdown, Performance, Victory, Defeat }
internal readonly record struct CrimsonMember(byte Slot, Guid Connection, bool Ready, bool Out);
internal readonly record struct CrimsonState(Guid Fight, int Age, int MusicStart, int FinalStart, CrimsonStage Stage, CrimsonMember[] Members,
    int GroundX, int GroundY, byte DefeatedMask = 0, byte Phase = 0, int PhaseStart = 0, int UnlockAt = -1,
    short Target = -1, int PhraseStart = -1, int PhraseEnd = -1, CrimsonRhythmKind PhraseKind = CrimsonRhythmKind.Groove,
    int TargetLife = 3000000, int Life0 = 3000000, int Life1 = 3000000, int Life2 = 3000000, int Life3 = 3000000,
    bool PerformerDefeated = false, int CompletedCycles = 0)
{
    internal const int MaxMembers = 8;
    internal RaidFieldGeometry Field => RaidFieldGeometry.FromGround(GroundX, GroundY);
    internal bool Vulnerable(float now) => Fight != Guid.Empty && Stage == CrimsonStage.Performance
        && Phase == CrimsonPhaseRules.FinalPhase && UnlockAt >= 0 && now >= UnlockAt && !PerformerDefeated;
    internal bool SummonVulnerable(int index) => Fight != Guid.Empty && Stage == CrimsonStage.Performance
        && UnlockAt >= 0 && Age >= UnlockAt && index is >= 0 and < CrimsonInvocation.SummonCount
        && CrimsonPhaseRules.ActiveSource(Phase, DefeatedMask, PerformerDefeated, index);
    internal bool Contains(int slot) => Array.Exists(Members ?? Array.Empty<CrimsonMember>(), m => m.Slot == slot && !m.Out);
    internal bool SourceActive(int source, float now) => source == 3 ? Vulnerable(now) : SummonVulnerable(source);
    internal int LifeFor(int index) => index switch { 0 => Life0, 1 => Life1, 2 => Life2, _ => Life3 };
    internal int DamageFloor(int source) => Phase < 3 ? CrimsonPhaseRules.RetreatLife(TargetLife) : CompletedCycles == 0 ? 1 : 0;
    internal bool HeldAtFloor(int source) => LifeFor(source) <= DamageFloor(source);
    internal int BarLife => Phase == 3 ? Life0 + Life1 + Life2 + Life3 : LifeFor(Phase);
    internal int BarMax => CrimsonPhaseRules.BarMaximum(Phase, TargetLife);
    internal bool FormationAt(float now) => PhraseKind != CrimsonRhythmKind.Groove && PhraseStart >= 0 && now < PhraseEnd + 12;
    internal float Presence(int index, float now)
    {
        if (index is < 0 or >= 3 || (DefeatedMask & 1 << index) != 0) return 0;
        if (Phase == 3) return CrimsonInvocation.Ease((now - PhaseStart) / 45);
        if (index == Phase) return 1;
        return index == Phase - 1 ? 1 - CrimsonInvocation.Ease((now - PhaseStart) / 36) : 0;
    }
    internal bool CanReplace(in CrimsonState old)
    {
        if (old.Fight == Guid.Empty) return true;
        if (Fight != old.Fight || Age < old.Age || Phase < old.Phase || PhaseStart < old.PhaseStart) return false;
        if (Phase == old.Phase && CompletedCycles < old.CompletedCycles) return false;
        if (GroundX != old.GroundX || GroundY != old.GroundY) return false;
        if (old.Stage >= CrimsonStage.Countdown && Stage < old.Stage) return false;
        if (old.MusicStart >= 0 && TargetLife != old.TargetLife) return false;
        if (Phase == old.Phase && (PhaseStart != old.PhaseStart || old.UnlockAt >= 0 && UnlockAt != old.UnlockAt)) return false;
        if ((DefeatedMask & old.DefeatedMask) != old.DefeatedMask || old.PerformerDefeated && !PerformerDefeated) return false;
        if (old.Stage is CrimsonStage.Victory or CrimsonStage.Defeat && Stage != old.Stage) return false;
        return old.MusicStart < 0 || MusicStart == old.MusicStart;
    }
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight.ToByteArray()); w.Write(Age); w.Write(MusicStart); w.Write(FinalStart); w.Write((byte)Stage);
        w.Write(GroundX); w.Write(GroundY); w.Write(DefeatedMask);
        w.Write(Phase); w.Write(PhaseStart); w.Write(UnlockAt); w.Write(Target);
        w.Write(PhraseStart); w.Write(PhraseEnd); w.Write((byte)PhraseKind); w.Write(TargetLife);
        w.Write(Life0); w.Write(Life1); w.Write(Life2); w.Write(Life3); w.Write(PerformerDefeated); w.Write(CompletedCycles);
        w.Write((byte)Members.Length);
        foreach (var m in Members) { w.Write(m.Slot); w.Write(m.Connection.ToByteArray()); w.Write(m.Ready); w.Write(m.Out); }
    }
    internal static CrimsonState Read(BinaryReader r)
    {
        byte[] f = r.ReadBytes(16); int age = r.ReadInt32(), start = r.ReadInt32(), final = r.ReadInt32();
        var stage = (CrimsonStage)r.ReadByte(); int x = r.ReadInt32(), y = r.ReadInt32(); byte defeated = r.ReadByte();
        byte phase = r.ReadByte(); int epoch = r.ReadInt32(), unlock = r.ReadInt32(); short target = r.ReadInt16();
        int phrase = r.ReadInt32(), phraseEnd = r.ReadInt32(); var kind = (CrimsonRhythmKind)r.ReadByte();
        int maximum = r.ReadInt32(), a = r.ReadInt32(), b = r.ReadInt32(), c = r.ReadInt32(), d = r.ReadInt32();
        bool dead = r.ReadBoolean(); int cycles = r.ReadInt32(); int count = r.ReadByte();
        if (f.Length != 16 || new Guid(f) == Guid.Empty || age is < 0 or > 72000 || start is < -1 or > 72000
            || final is < -1 or > 72000 || !Enum.IsDefined(stage) || count is < 1 or > MaxMembers || phase > 3
            || defeated > 7 || phase < 3 && (defeated != 0 || dead || final >= 0) || phase == 3 && final != epoch
            || epoch < 0 || epoch > age || unlock is < -1 or > 72400 || target is < -1 or >= 255
            || !Enum.IsDefined(kind) || phrase is < -1 or > 72400 || phraseEnd is < -1 or > 72800
            || (phrase < 0) != (phraseEnd < 0) || phrase >= 0 && (phraseEnd <= phrase || phraseEnd - phrase > 720)
            || cycles is < 0 or > 1000
            || maximum is < 1 or > 17000000 || a < 0 || a > maximum || b < 0 || b > maximum
            || c < 0 || c > maximum || d < 0 || d > maximum
            || ((defeated & 1) != 0 && a != 0) || ((defeated & 2) != 0 && b != 0) || ((defeated & 4) != 0 && c != 0) || dead && d != 0
            || x is < 1600 or > 400000 || y is < 1440 or > 150000)
            throw new InvalidDataException("crimson.actor_invalid");
        var members = new CrimsonMember[count];
        for (int i = 0; i < count; i++)
        {
            byte slot = r.ReadByte(); byte[] bytes = r.ReadBytes(16); bool ready = r.ReadBoolean(), eliminated = r.ReadBoolean();
            if (slot >= 255 || bytes.Length != 16 || new Guid(bytes) == Guid.Empty
                || i > 0 && slot <= members[i - 1].Slot) throw new InvalidDataException("crimson.member_invalid");
            members[i] = new(slot, new Guid(bytes), ready, eliminated);
        }
        if (stage is CrimsonStage.Countdown or CrimsonStage.Performance && (start < 0 || unlock < 0))
            throw new InvalidDataException("crimson.clock_missing");
        if (target >= 0 && !Array.Exists(members, m => m.Slot == target)) throw new InvalidDataException("crimson.target_invalid");
        return new(new Guid(f), age, start, final, stage, members, x, y, defeated, phase, epoch, unlock,
            target, phrase, phraseEnd, kind, maximum, a, b, c, d, dead, cycles);
    }
}

internal enum CrimsonShape : byte { Slash, Bolt }
internal readonly record struct CrimsonEffigyState(Guid Fight, short Boss, byte Index, int Born)
{
    internal void Write(BinaryWriter w) { w.Write(Fight.ToByteArray()); w.Write(Boss); w.Write(Index); w.Write(Born); }
    internal static CrimsonEffigyState Read(BinaryReader r)
    {
        byte[] fight = r.ReadBytes(16); short boss = r.ReadInt16(); byte index = r.ReadByte(); int born = r.ReadInt32();
        if (fight.Length != 16 || new Guid(fight) == Guid.Empty || boss is < 0 or >= 200 || index >= 3 || born is < 0 or > 72000)
            throw new InvalidDataException("crimson.effigy_invalid");
        return new(new Guid(fight), boss, index, born);
    }
}

internal readonly record struct CrimsonHazard(Guid Fight, short Boss, int Born, int Fire, int End,
    CrimsonShape Shape, float X, float Y, float DX, float DY, float Length, float Width, int Damage, byte Source = 3,
    int Epoch = 0, int Phrase = 0, byte Accent = 0)
{
    internal bool Live(float age) => age >= Fire && age < End;
    internal const float BoltTail = 210;
    internal float Travel(float age) => Math.Clamp((age - Fire) / 24f, 0, 1) * Length;
    internal float Reach(float age) => Shape == CrimsonShape.Slash ? Length * Math.Clamp((age - Fire) / 2f, 0, 1) : Math.Min(BoltTail, Travel(age));
    internal float HitWidth(float age) => Width * Math.Clamp((age - Fire) / 2f, 0, 1);
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight.ToByteArray()); w.Write(Boss); w.Write(Born); w.Write(Fire); w.Write(End); w.Write((byte)Shape);
        w.Write(X); w.Write(Y); w.Write(DX); w.Write(DY); w.Write(Length); w.Write(Width); w.Write(Damage); w.Write(Source);
        w.Write(Epoch); w.Write(Phrase); w.Write(Accent);
    }
    internal static CrimsonHazard Read(BinaryReader r)
    {
        byte[] f = r.ReadBytes(16); short boss = r.ReadInt16(); int born = r.ReadInt32(), fire = r.ReadInt32(), end = r.ReadInt32();
        var shape = (CrimsonShape)r.ReadByte();
        float x = r.ReadSingle(), y = r.ReadSingle(), dx = r.ReadSingle(), dy = r.ReadSingle(), length = r.ReadSingle(), width = r.ReadSingle();
        int damage = r.ReadInt32(); byte source = r.ReadByte(); int epoch = r.ReadInt32(), phrase = r.ReadInt32(); byte accent = r.ReadByte();
        if (f.Length != 16 || new Guid(f) == Guid.Empty || boss is < 0 or >= 200 || born is < 0 or > 72400
            || (long)fire - born < CrimsonRhythm.MinimumWarningTicks || (long)fire - born > CrimsonRhythm.MaximumWarningTicks
            || end <= fire || (long)end - fire > 90 || epoch < 0 || epoch > born || phrase < 0 || accent > 2
            || !Enum.IsDefined(shape) || !float.IsFinite(x) || !float.IsFinite(y) || Math.Abs(x) > 400000 || Math.Abs(y) > 150000
            || !float.IsFinite(dx) || !float.IsFinite(dy) || Math.Abs(dx * dx + dy * dy - 1) > .02
            || !float.IsFinite(length) || length is < 1 or > 4000 || !float.IsFinite(width) || width is < 1 or > 120
            || damage is < 1 or > 2000 || source > 3)
            throw new InvalidDataException("crimson.hazard_invalid");
        return new(new Guid(f), boss, born, fire, end, shape, x, y, dx, dy, length, width, damage, source, epoch, phrase, accent);
    }
}
