using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// One finite packet travelling along a locked cardinal lattice track. Sanctuary
// cuts clip it, never restart its head, tail, velocity or material coordinates.
internal readonly record struct FirstSeveranceGridPulse(
    FirstSeveranceLanceRay Track, FirstSeveranceLanceRay Segment, double Age)
{
    internal const int TransitTicks = 14, EmissionTicks = 6;
    internal const float TailFraction = .42f, HeadFraction = .14f;
    internal float HeadDistance => Track.Length * (float)(Age / TransitTicks);
    internal float TailDistance => HeadDistance - PacketLength;
    internal float PacketLength => Track.Length * EmissionTicks / TransitTicks;
    private float SegmentStart => (Segment.X - Track.X) * Track.DirectionX + (Segment.Y - Track.Y) * Track.DirectionY;
    internal float StartDistance => Math.Max(SegmentStart, TailDistance);
    internal float EndDistance => Math.Min(SegmentStart + Segment.Length, HeadDistance);
    internal float Length => Math.Max(0, EndDistance - StartDistance);
    internal float StartU => (StartDistance - TailDistance) / PacketLength;
    internal float EndU => (EndDistance - TailDistance) / PacketLength;
    internal float FlowOffset => StartDistance - HeadDistance;
    internal float HalfWidth => FirstSeveranceBeamIgnition.At(Track, Age).HalfWidth;
    internal FirstSeveranceLanceRay Bounds => new(
        Track.X + Track.DirectionX * StartDistance, Track.Y + Track.DirectionY * StartDistance,
        Track.DirectionX, Track.DirectionY, Length, HalfWidth);

    internal static float Envelope(float u) => Smooth(u / TailFraction) * Smooth((1 - u) / HeadFraction);
    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    internal bool Intersects(float x, float y, float halfWidth, float halfHeight)
    {
        if (Length <= 0 || Age <= 0 || Age >= TransitTicks + EmissionTicks) return false;
        float dx = x - Track.X, dy = y - Track.Y;
        float along = dx * Track.DirectionX + dy * Track.DirectionY;
        float across = Math.Abs(-dx * Track.DirectionY + dy * Track.DirectionX);
        float halfAlong = Math.Abs(Track.DirectionX) * halfWidth + Math.Abs(Track.DirectionY) * halfHeight;
        float halfAcross = Math.Abs(Track.DirectionY) * halfWidth + Math.Abs(Track.DirectionX) * halfHeight;
        float lo = Math.Max(StartDistance, along - halfAlong), hi = Math.Min(EndDistance, along + halfAlong);
        if (hi < lo) return false;
        // The profile is monotone on either side of its full-width plateau.
        // Cardinal tracks make this AABB/profile overlap exact, not a broad quad.
        float peak = Math.Clamp(TailDistance + PacketLength * .6f, lo, hi);
        float width = HalfWidth * Envelope((peak - TailDistance) / PacketLength);
        return width > 0 && across <= halfAcross + width;
    }
}
