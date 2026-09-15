using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// A single locked P2 cannon, independent of the lattice descriptor. Final can
// reuse its dimensions/ignition without inventing a fake grid or local target.
internal sealed class FirstSeveranceCoreCannonVolley
{
    internal const int OpeningTicks = 24;
    internal const int TelegraphTicks = FirstSeveranceGridVolley.TelegraphTicks;
    internal const int ActiveTicks = 60;
    internal const int DurationTicks = TelegraphTicks + ActiveTicks;
    internal uint Serial { get; }
    internal ulong StartTick { get; }
    internal ulong FireTick => StartTick + TelegraphTicks;
    internal ulong EndTick => FireTick + ActiveTicks;
    internal int TargetSlot { get; }
    internal FirstSeveranceLanceRay Ray { get; }

    internal FirstSeveranceCoreCannonVolley(uint serial, ulong start, int targetSlot, FirstSeveranceLanceRay ray)
    {
        if (serial == 0 || start == 0 || start > ulong.MaxValue - DurationTicks
            || targetSlot is < 0 or >= 255 || !ray.IsValid
            || ray.Length != FirstSeveranceGridVolley.CoreBeamLength || ray.HalfWidth != FirstSeveranceGridVolley.CoreBeamHalfWidth)
            throw new ArgumentException("Invalid core cannon descriptor.");
        Serial = serial; StartTick = start; TargetSlot = targetSlot; Ray = ray;
    }
    internal bool Intersects(ulong tick, float x, float y, float halfWidth, float halfHeight)
        => tick >= FireTick && tick < EndTick
            && FirstSeveranceBeamIgnition.At(Ray, (double)tick - FireTick).Intersects(x, y, halfWidth, halfHeight);
}
