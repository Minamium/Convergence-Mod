using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

// Only the runtime advances/steers this state. Clients receive a bounded full
// snapshot and extrapolate its velocity for display; they never choose a target.
internal readonly record struct SamuraiWispMotion(int Tick, float X, float Y, float VX, float VY)
{
    internal static SamuraiWispMotion Spawn(SamuraiHazard h) => new(h.Born, h.X, h.Y,
        h.DX * GhostSamuraiRules.SpreadSpeed, h.DY * GhostSamuraiRules.SpreadSpeed);

    internal bool IsValid(SamuraiHazard h) => Tick >= h.Born && Tick < h.End
        && float.IsFinite(X) && float.IsFinite(Y) && Math.Abs(X) <= 500000 && Math.Abs(Y) <= 500000
        && float.IsFinite(VX) && float.IsFinite(VY)
        && VX * VX + VY * VY <= GhostSamuraiRules.HomingSpeed * GhostSamuraiRules.HomingSpeed + .01f;

    internal SamuraiWispMotion Advance(SamuraiHazard h, int tick, float targetX, float targetY)
    {
        if (tick <= Tick || tick >= h.End) return this;
        float vx = VX, vy = VY;
        // Never sample target direction during the harmless outward spread.
        if (tick >= h.Fire)
        {
            float dx = targetX - X, dy = targetY - Y, distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance > .001f)
            {
                vx += (dx / distance * GhostSamuraiRules.HomingSpeed - vx) * GhostSamuraiRules.HomingStrength;
                vy += (dy / distance * GhostSamuraiRules.HomingSpeed - vy) * GhostSamuraiRules.HomingStrength;
            }
        }
        return new(tick, X + vx, Y + vy, vx, vy);
    }

    internal float VisualX(float age) => X + VX * Math.Clamp(age - Tick, 0, GhostSamuraiRules.WispSyncInterval);
    internal float VisualY(float age) => Y + VY * Math.Clamp(age - Tick, 0, GhostSamuraiRules.WispSyncInterval);

    internal bool Hits(SamuraiHazard h, float age, float x, float y, float halfX, float halfY)
    {
        if (!h.Live(age)) return false;
        float ax = Math.Max(0, Math.Abs(x - X) - halfX), ay = Math.Max(0, Math.Abs(y - Y) - halfY);
        return ax * ax + ay * ay <= h.Radius * h.Radius;
    }

    internal bool CanReplace(SamuraiWispMotion current) => Tick >= current.Tick;

    internal void Write(BinaryWriter w) { w.Write(Tick); w.Write(X); w.Write(Y); w.Write(VX); w.Write(VY); }
    internal static SamuraiWispMotion Read(BinaryReader r, SamuraiHazard h)
    {
        var state = new SamuraiWispMotion(r.ReadInt32(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        if (!state.IsValid(h)) throw new InvalidDataException("ghost_samurai.wisp_motion_invalid");
        return state;
    }
}
