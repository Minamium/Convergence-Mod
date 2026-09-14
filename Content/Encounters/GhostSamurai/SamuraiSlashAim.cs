using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

// Mutable, full authority snapshot separated from immutable attack identity/times.
// Clients show these endpoints verbatim; they never aim using a local player.
internal readonly record struct SamuraiSlashAim(int Tick, int LockTick, float X, float Y, float DX, float DY, int ReleaseTick = 0)
{
    internal static SamuraiSlashAim Spawn(SamuraiHazard h, int lockTick)
        => new(h.Born, lockTick, h.X, h.Y, h.DX, h.DY, h.Fire);
    internal SamuraiHazard Geometry(SamuraiHazard h) => h with { X = X, Y = Y, DX = DX, DY = DY, Fire = ReleaseTick,
            End = ReleaseTick + (h.ArrivalTick > 0 ? Math.Max(SamuraiWaveRules.WaveLife, h.ArrivalTick - ReleaseTick + 24) : h.End - h.Fire) };
    internal bool IsValid(SamuraiHazard h) => h.HasAim && Geometry(h).IsValid
        && (h.ArrivalTick == 0 ? ReleaseTick == h.Fire : ReleaseTick >= h.Fire && (long)ReleaseTick - h.Born <= 300)
        && (h.ArrivalTick == 0 || LockTick == ReleaseTick - GhostSamuraiRules.AimLockLead)
        && LockTick >= h.Born && LockTick <= ReleaseTick - (LockTick == h.Born ? 0
            : h.Shape == SamuraiShape.RushVisual ? GhostSamuraiRules.DashAimLockTime : GhostSamuraiRules.AimLockLead)
        && Tick >= h.Born && Tick <= LockTick;
    internal bool Locked => Tick == LockTick;
    internal SamuraiSlashAim Advance(SamuraiHazard h, int tick, float x, float y, float dx, float dy)
    {
        if (Locked || tick <= Tick || tick > LockTick) return this;
        var next = new SamuraiSlashAim(tick, LockTick, x, y, dx, dy, ReleaseTick);
        if (!next.IsValid(h)) throw new ArgumentException("ghost_samurai.aim_invalid");
        return next;
    }
    // Only the first grid wave has a mutable release schedule. Aim is frozen
    // before release and remains frozen even if its previous snapshot was lost.
    internal SamuraiSlashAim TrackArrival(SamuraiHazard h, int tick, float x, float y, float dx, float dy,
        float targetX, float targetY, float halfX, float halfY)
    {
        if (h.ArrivalTick == 0) return Advance(h, tick, x, y, dx, dy);
        if (Locked || tick <= Tick) return this;
        var geometry = h with { X = x, Y = y, DX = dx, DY = dy };
        int flight = SamuraiWaveRules.FlightTicks(geometry, targetX, targetY, halfX, halfY);
        int release = Math.Clamp(h.ArrivalTick - flight, h.Fire, h.Born + 300);
        // A moving/teleporting target cannot retroactively move release into the
        // past. Preserve the four-tick lock cue instead of speeding the wave up.
        release = Math.Max(release, tick + GhostSamuraiRules.AimLockLead);
        var next = new SamuraiSlashAim(tick, release - GhostSamuraiRules.AimLockLead, x, y, dx, dy, release);
        if (!next.IsValid(h)) throw new ArgumentException("ghost_samurai.arrival_aim_invalid");
        return next;
    }
    internal bool CanReplace(SamuraiSlashAim current) => this == current
        || !current.Locked && Tick > current.Tick && (ReleaseTick != current.ReleaseTick || LockTick == current.LockTick);
    internal void Write(BinaryWriter writer)
    {
        writer.Write(Tick); writer.Write(LockTick); writer.Write(X); writer.Write(Y); writer.Write(DX); writer.Write(DY); writer.Write(ReleaseTick);
    }
    internal static SamuraiSlashAim Read(BinaryReader reader, SamuraiHazard hazard)
    {
        var value = new SamuraiSlashAim(reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadInt32());
        if (!value.IsValid(hazard)) throw new InvalidDataException("ghost_samurai.aim_invalid");
        return value;
    }
}
