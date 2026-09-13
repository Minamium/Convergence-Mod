using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// The former comb now has one continuous band per side, like the Phase-III
// jets. Retain the internal helper/descriptor IDs and original total live time.
internal static class FirstSeveranceCurtainComb
{
    internal const int LaneCount = 1;
    internal const int CenterLane = LaneCount / 2;
    internal const int StaggerTicks = CenterLane;
    internal const int LaneActiveTicks = 35;
    internal const int ActiveTicks = StaggerTicks + LaneActiveTicks;

    internal static int Offset(int lane)
    {
        if ((uint)lane >= LaneCount) throw new ArgumentOutOfRangeException(nameof(lane));
        return Math.Abs(lane - CenterLane);
    }

    internal static FirstSeveranceLanceRay Ray(in FirstSeveranceLanceRay curtain, int lane)
    {
        _ = Offset(lane);
        float half = curtain.HalfWidth / LaneCount;
        float offset = (lane - CenterLane) * half * 2;
        return curtain with { X = curtain.X - curtain.DirectionY * offset,
            Y = curtain.Y + curtain.DirectionX * offset, HalfWidth = half };
    }

    internal static ulong RevealTick(FirstSeveranceLanceVolley volley, int lane)
        => volley.StartTick + (ulong)Offset(lane);
    internal static ulong FireTick(FirstSeveranceLanceVolley volley, int lane)
        => volley.FireTick + (ulong)Offset(lane);
    internal static ulong EndTick(FirstSeveranceLanceVolley volley, int lane)
        => FireTick(volley, lane) + LaneActiveTicks;
    internal static bool IsLive(FirstSeveranceLanceVolley volley, int lane, ulong tick)
        => tick >= FireTick(volley, lane) && tick < EndTick(volley, lane);

    internal static bool Intersects(FirstSeveranceLanceVolley volley, ulong tick,
        float x, float y, float halfWidth, float halfHeight)
    {
        if (volley.Kind != FirstSeveranceAttackKind.Stillness || !volley.IsFiring(tick)) return false;
        foreach (var curtain in volley.Rays)
            for (int lane = 0; lane < LaneCount; lane++)
                if (IsLive(volley, lane, tick) && FirstSeveranceBeamIgnition.At(Ray(curtain, lane),
                    (double)tick - FireTick(volley, lane)).Intersects(x, y, halfWidth, halfHeight))
                    return true;
        return false;
    }
}
