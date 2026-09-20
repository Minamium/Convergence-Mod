using System;
using System.Numerics;

namespace Convergence.Content.Encounters.AzureCathedral;

// Continuous curves, sampled by the authority; body parts inherit the travelled
// path rather than each orbiting its own small target.
internal static class AzureFlight
{
    internal static Vector2 DevourStaging(Vector2 from, Vector2 girl)
        => girl + new Vector2(from.X < girl.X ? -2200 : 2200, -400);
    internal static Vector2 DevourPosition(Vector2 from, Vector2 velocity, Vector2 girl, float t)
    {
        Vector2 stage=DevourStaging(from,girl);
        Vector2 direction=Vector2.Normalize(girl-stage);
        if(t<AzureRules.DevourRetreat)
        {
            Vector2 toStage=stage-from;
            Vector2 outgoing=velocity.LengthSquared()>1?Vector2.Normalize(velocity):toStage.LengthSquared()>1?Vector2.Normalize(toStage):-direction;
            return Curve(from,from+outgoing*700,stage+new Vector2(0,-600),stage,t/AzureRules.DevourRetreat);
        }
        Vector2 target=girl-direction*AzureRules.MouthReach;
        return Vector2.Lerp(stage,target,AzureRules.DevourTravel(t));
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
