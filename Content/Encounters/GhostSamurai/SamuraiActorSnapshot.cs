using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

internal readonly record struct SamuraiActorSnapshot(Guid Fight, int Age, SamuraiPhase Phase, SamuraiAttack Attack,
    SamuraiBeat Beat, int AttackTimer, int TransitionRemaining, int MaximumLife, SamuraiArenaBounds Arena)
{
    internal bool IsValid => Fight != Guid.Empty && Arena.IsValid && Age >= 0 && Enum.IsDefined(Phase) && Enum.IsDefined(Attack)
        && Enum.IsDefined(Beat) && AttackTimer is >= 0 and <= 1000
        && TransitionRemaining is >= 0 and <= GhostSamuraiRules.TransitionTime && MaximumLife is >= 1 and <= 500_000_000;
    internal bool CanReplace(Guid currentFight, int currentAge)
        => IsValid && (currentFight == Guid.Empty || currentFight == Fight && Age >= currentAge);
    internal void Write(BinaryWriter writer)
    {
        writer.Write(Fight.ToByteArray()); writer.Write(Age); writer.Write((byte)Phase); writer.Write((byte)Attack);
        writer.Write((byte)Beat); writer.Write(AttackTimer); writer.Write(TransitionRemaining); writer.Write(MaximumLife);
        Arena.Write(writer);
    }
    internal static SamuraiActorSnapshot Read(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(16);
        int age = reader.ReadInt32(); var phase = (SamuraiPhase)reader.ReadByte();
        var attack = (SamuraiAttack)reader.ReadByte(); var beat = (SamuraiBeat)reader.ReadByte();
        int timer = reader.ReadInt32(), transition = reader.ReadInt32(), life = reader.ReadInt32();
        if (bytes.Length != 16) throw new EndOfStreamException();
        var result = new SamuraiActorSnapshot(new Guid(bytes), age, phase, attack, beat, timer, transition, life, SamuraiArenaBounds.Read(reader));
        if (!result.IsValid) throw new InvalidDataException("ghost_samurai.actor_snapshot_invalid");
        return result;
    }
}
