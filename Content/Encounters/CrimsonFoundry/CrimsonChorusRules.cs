using System;
using System.IO;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal enum CrimsonChorusKind : byte { Stack, Spread }

// A phase-scoped, immutable cooperative interlude. Bits address the frozen
// ordered roster, never a client-supplied player slot or a synthetic member.
internal readonly record struct CrimsonChorusPlan(Guid Fight, short Boss, int Epoch,
    int Serial, byte Source, CrimsonChorusKind Kind, byte Members,
    int Born, int Fire, int End, int GroundX, int GroundY, CrimsonPoint Center)
{
    internal RaidFieldGeometry Field => RaidFieldGeometry.FromGround(GroundX, GroundY);
    internal void Validate()
    {
        if (Fight == Guid.Empty || Boss is < 0 or >= 200 || Epoch < 0 || Serial is < 1 or > 100000
            || Source > 3 || !Enum.IsDefined(Kind) || Members == 0 || Born < Epoch || Born > 72400
            || (long)Fire - Born is < 160 or > 720 || (long)End - Fire is < 24 or > 180 || End > 73500
            || GroundX is < 1600 or > 400000 || GroundY is < 1440 or > 150000 || !Center.Finite)
            throw new InvalidDataException("crimson.chorus_invalid");
        var f = Field;
        if (Center.X < f.Left + 240 || Center.X > f.Right - 240
            || Center.Y < f.Top + 240 || Center.Y > f.Bottom - 240)
            throw new InvalidDataException("crimson.chorus_outside_field");
    }
    internal bool MatchesRoster(int count) => count is >= 1 and <= 8 && (Members >> count) == 0;
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight.ToByteArray()); w.Write(Boss); w.Write(Epoch); w.Write(Serial);
        w.Write(Source); w.Write((byte)Kind); w.Write(Members);
        w.Write(Born); w.Write(Fire); w.Write(End); w.Write(GroundX); w.Write(GroundY);
        w.Write(Center.X); w.Write(Center.Y);
    }
    internal static CrimsonChorusPlan Read(BinaryReader r)
    {
        byte[] id = r.ReadBytes(16);
        if (id.Length != 16) throw new EndOfStreamException();
        var result = new CrimsonChorusPlan(new Guid(id), r.ReadInt16(), r.ReadInt32(), r.ReadInt32(),
            r.ReadByte(), (CrimsonChorusKind)r.ReadByte(), r.ReadByte(),
            r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), new(r.ReadSingle(), r.ReadSingle()));
        result.Validate(); return result;
    }
}

internal readonly record struct CrimsonChorusImpact(CrimsonChorusPlan Plan, byte Member, int Damage)
{
    internal void Validate()
    {
        Plan.Validate();
        if (Member >= 8 || (Plan.Members & (1 << Member)) == 0 || Damage is < 1 or > 4000)
            throw new InvalidDataException("crimson.chorus_impact_invalid");
    }
    internal void Write(BinaryWriter w) { Plan.Write(w); w.Write(Member); w.Write(Damage); }
    internal static CrimsonChorusImpact Read(BinaryReader r)
    {
        var impact = new CrimsonChorusImpact(CrimsonChorusPlan.Read(r), r.ReadByte(), r.ReadInt32());
        impact.Validate(); return impact;
    }
}

internal static class CrimsonChorusRules
{
    internal const float StackRadius = 220, SpreadRadius = 200;
    internal const int StackShareSource = 900, SpreadFailureSource = 900;
    internal const int ImpactTicks = 12, PhrasesBetween = 5;

    internal static (bool Resolved, byte FailedMask) ReadVerdict(BinaryReader reader, byte members)
    {
        byte resolved = reader.ReadByte(), failures = reader.ReadByte();
        if (resolved > 1 || (failures & ~members) != 0 || resolved == 0 && failures != 0)
            throw new InvalidDataException("crimson.chorus_verdict_invalid");
        return (resolved == 1, failures);
    }
    internal static bool CanReplaceVerdict(bool resolved, byte failures, bool nextResolved, byte nextFailures)
        => !resolved || nextResolved && failures == nextFailures;

    // Resolution is a single authority observation. Dead/disconnected members
    // are removed from the budget; a slot not in the announced mask cannot join.
    // These are native source budgets, not guaranteed or directly assigned HP loss.
    internal static int[] Resolve(CrimsonChorusKind kind, CrimsonPoint center,
        ReadOnlySpan<CrimsonPoint> positions, byte announced, byte living)
    {
        int count = positions.Length;
        if (!Enum.IsDefined(kind) || !center.Finite || count is < 1 or > 8
            || announced == 0 || (announced >> count) != 0 || (living >> count) != 0)
            throw new ArgumentException("crimson.chorus_roster_invalid");
        for (int i = 0; i < count; i++) if (!positions[i].Finite)
            throw new ArgumentException("crimson.chorus_position_invalid");
        int[] damage = new int[count]; int mask = announced & living;
        if (kind == CrimsonChorusKind.Stack)
        {
            int active = 0, gathered = 0;
            for (int i = 0; i < count; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                active++;
                if ((positions[i] - center).LengthSquared <= StackRadius * StackRadius)
                    gathered++;
            }
            int pool = active == 0 ? 0 : (StackShareSource * (active - gathered) + active - 1) / active;
            for (int i = 0; i < count; i++)
                if ((mask & (1 << i)) != 0)
                    damage[i] = pool;
        }
        else
        {
            float separation = SpreadRadius * 2;
            for (int i = 0; i < count; i++) for (int j = i + 1; j < count; j++)
            {
                if ((mask & (1 << i)) == 0 || (mask & (1 << j)) == 0) continue;
                if ((positions[i] - positions[j]).LengthSquared < separation * separation)
                    damage[i] = damage[j] = SpreadFailureSource;
            }
        }
        return damage;
    }
}
