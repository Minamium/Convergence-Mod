using System;
using System.Numerics;

namespace Convergence.Content.Encounters.AzureCathedral;

// Continuous curves, sampled by the authority; body parts inherit the travelled
// path rather than each orbiting its own small target.
internal static class AzureFlight
{
    internal static Vector2 StagingHead(Vector2 girl, int side)
        => girl + new Vector2(side * 2200, -100);
    internal static Vector2 CeremonyDirection(Vector2 girl, int side)
        => Vector2.Normalize(girl - StagingHead(girl, side));
    internal static Vector2 StagingPart(Vector2 girl, int side, int index)
        => StagingHead(girl, side) - CeremonyDirection(girl, side) * (index * AzureRules.SegmentSpacing);
    // Every part has a staging pose. Moving only the head lets the old follow
    // chain fold across the mouth, even when the head is 2,200px away.
    internal static Vector2 Retreat(Vector2 from, Vector2 girl, int side, int index, float t)
    {
        float p = AzureRules.Ease(t / AzureRules.StagingTicks);
        return Vector2.Lerp(from, StagingPart(girl, side, index), p)
            + new Vector2(0, -160 * MathF.Sin(p * MathF.PI));
    }
    internal static Vector2 DevourPosition(Vector2 girl, int side, int index, float t)
    {
        Vector2 direction = CeremonyDirection(girl, side);
        return Vector2.Lerp(StagingHead(girl, side), girl - direction * AzureRules.MouthReach, AzureRules.DevourTravel(t))
            - direction * (index * AzureRules.SegmentSpacing);
    }
    internal static Vector2 MeltPosition(Vector2 girl, int side, int index, float t)
    {
        Vector2 direction = CeremonyDirection(girl, side);
        Vector2 head = Vector2.Lerp(StagingHead(girl, side), girl,
            AzureRules.Ease((t - AzureRules.MeltRush) / (AzureRules.MeltContact - AzureRules.MeltRush)));
        // Feed the chain through the melt front instead of piling all 45 parts
        // onto a stationary head. The head/tail dissolve delays match this flow.
        head += direction * (Math.Max(0, t - AzureRules.MeltContact) * 20);
        return head - direction * (index * AzureRules.SegmentSpacing);
    }
    internal static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        t = Math.Clamp(t, 0, 1); float s = 1 - t;
        return a * (s*s*s) + b * (3*s*s*t) + c * (3*s*t*t) + d * (t*t*t);
    }
    internal static Vector2 Steer(Vector2 velocity, Vector2 delta, float speed, float turn = .045f)
    {
        if (delta.LengthSquared() < 1) return velocity;
        float old = velocity.LengthSquared() < 1 ? MathF.Atan2(delta.Y, delta.X) : MathF.Atan2(velocity.Y, velocity.X);
        float difference = MathF.IEEERemainder(MathF.Atan2(delta.Y, delta.X) - old, MathF.Tau);
        float angle = old + Math.Clamp(difference, -turn, turn);
        float length = velocity.Length() + Math.Clamp(speed - velocity.Length(), -1.2f, 1.2f);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;
    }
}
