using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

// Mutable, full authority snapshot separated from immutable attack identity/times.
// Clients show these endpoints verbatim; they never aim using a local player.
internal readonly record struct SamuraiSlashAim(int Tick, int LockTick, float X, float Y, float DX, float DY)
{
    internal static SamuraiSlashAim Spawn(SamuraiHazard h, int lockTick)
        => new(h.Born, lockTick, h.X, h.Y, h.DX, h.DY);
    internal SamuraiHazard Geometry(SamuraiHazard h) => h with { X = X, Y = Y, DX = DX, DY = DY };
    internal bool IsValid(SamuraiHazard h) => h.Shape == SamuraiShape.Slash && Geometry(h).IsValid
        && LockTick >= h.Born && LockTick <= h.Fire - (LockTick == h.Born ? 0 : GhostSamuraiRules.AimLockLead)
        && Tick >= h.Born && Tick <= LockTick;
    internal bool Locked => Tick == LockTick;
    internal SamuraiSlashAim Advance(SamuraiHazard h, int tick, float x, float y, float dx, float dy)
    {
        if (Locked || tick <= Tick || tick > LockTick) return this;
        var next = new SamuraiSlashAim(tick, LockTick, x, y, dx, dy);
        if (!next.IsValid(h)) throw new ArgumentException("ghost_samurai.aim_invalid");
        return next;
    }
    internal bool CanReplace(SamuraiSlashAim current) => LockTick == current.LockTick
        && (Tick > current.Tick || this == current);
    internal void Write(BinaryWriter writer)
    {
        writer.Write(Tick); writer.Write(LockTick); writer.Write(X); writer.Write(Y); writer.Write(DX); writer.Write(DY);
    }
    internal static SamuraiSlashAim Read(BinaryReader reader, SamuraiHazard hazard)
    {
        var value = new SamuraiSlashAim(reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        if (!value.IsValid(hazard)) throw new InvalidDataException("ghost_samurai.aim_invalid");
        return value;
    }
}
