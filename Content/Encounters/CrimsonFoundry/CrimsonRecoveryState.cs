using System;
using System.IO;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Only health transitions are replicated. Ordinary Hurt/HP remains Terraria-owned.
internal readonly record struct CrimsonRecoveryState(uint Revision, bool Downed, int Life,
    float X, float Y, int ImmunityUntil, int LockoutUntil)
{
    internal bool Valid => Revision == 0 ? this == default
        : Revision <= 72000 && Life is >= 1 and <= 100000 && (!Downed || Life == 1)
          && float.IsFinite(X) && float.IsFinite(Y) && X is >= 0 and <= 400000 && Y is >= 0 and <= 150000
          && ImmunityUntil is >= 0 and <= 76000 && LockoutUntil is >= 0 and <= 76000;
    internal bool CanReplace(CrimsonRecoveryState old) => Revision > old.Revision || this == old;
    internal bool AcceptsFloor(uint generation, uint nonce, uint previousNonce, int nativeLife)
        => Revision > 0 && generation == Revision && !Downed && nonce > previousNonce && nativeLife <= 1;
    internal void Write(BinaryWriter w)
    { w.Write(Revision); w.Write(Downed); w.Write(Life); w.Write(X); w.Write(Y); w.Write(ImmunityUntil); w.Write(LockoutUntil); }
    internal static CrimsonRecoveryState Read(BinaryReader r)
    {
        var value = new CrimsonRecoveryState(r.ReadUInt32(), CrimsonState.ReadPresence(r), r.ReadInt32(),
            r.ReadSingle(), r.ReadSingle(), r.ReadInt32(), r.ReadInt32());
        if (!value.Valid) throw new InvalidDataException("crimson.recovery_state");
        return value;
    }
}

internal readonly record struct CrimsonRecoveryRequest(Guid Connection, uint Nonce, uint HealthRevision)
{
    internal void Write(BinaryWriter w) { w.Write(Connection.ToByteArray()); w.Write(Nonce); w.Write(HealthRevision); }
    internal static CrimsonRecoveryRequest Read(BinaryReader r)
    {
        byte[] connection = r.ReadBytes(16);
        uint nonce = r.ReadUInt32(), revision = r.ReadUInt32();
        if (connection.Length != 16 || new Guid(connection) == Guid.Empty || nonce == 0 || revision is 0 or > 72000)
            throw new InvalidDataException("crimson.recovery_request");
        return new(new Guid(connection), nonce, revision);
    }
}

// An older Alive projection must not release the local lethal-hit latch.
internal struct CrimsonDownLatch
{
    private uint revision;
    internal bool Pending { get; private set; }
    internal void Request(uint healthRevision) { revision = healthRevision; Pending = true; }
    internal void Observe(CrimsonRecoveryState state)
    { if (state.Downed || state.Revision > revision) Pending = false; }
}

internal struct CrimsonNativeFloor
{
    private bool armed;
    internal void Reset(int life) => armed = life > 1;
    internal bool Observe(int life, bool nativeLethalReceipt = false)
    {
        armed |= life > 1 || nativeLethalReceipt;
        return armed && life <= 1;
    }
}
