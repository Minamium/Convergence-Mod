#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

// Shared geometry/timing, not client authority. Only the owning combat runtime
// constructs assignments or applies hits; a snapshot carries the locked rays.
internal static class FirstSeveranceLanceTuning
{
    internal const int TelegraphTicks = 72;
    internal const int ActiveTicks = 18;
    internal const int CadenceTicks = 102;
    internal const int MaximumRays = 2;
    internal const float Length = 2600f;
    internal const float HalfWidth = 44f;
    internal const float BossHeightAboveCore = 360f;
    internal const float StackRadius = 112f;
    internal const float SpreadRadius = 224f;
    internal const float SpreadSeparation = SpreadRadius * 2f;

    internal static bool IsAttackPhase(FirstSeveranceSubstate phase)
        => phase is FirstSeveranceSubstate.PylonCheck or FirstSeveranceSubstate.CoreExposure;
}

internal readonly record struct FirstSeveranceLanceRay(float X, float Y, float DirectionX, float DirectionY)
{
    internal bool IsValid => float.IsFinite(X) && float.IsFinite(Y)
        && MathF.Abs(X) <= 1_000_000f && MathF.Abs(Y) <= 1_000_000f
        && float.IsFinite(DirectionX) && float.IsFinite(DirectionY)
        && MathF.Abs(DirectionX * DirectionX + DirectionY * DirectionY - 1f) < 0.001f;

    // Four-axis SAT for a finite beam rectangle vs a player's axis-aligned box.
    // The visible danger rails use this same half-width; decorative glow is wider.
    internal bool Intersects(float centerX, float centerY, float halfWidth, float halfHeight)
    {
        float beamHalfLength = FirstSeveranceLanceTuning.Length * 0.5f;
        float beamHalfWidth = FirstSeveranceLanceTuning.HalfWidth;
        float dx = centerX - X - DirectionX * beamHalfLength;
        float dy = centerY - Y - DirectionY * beamHalfLength;
        float ax = MathF.Abs(DirectionX);
        float ay = MathF.Abs(DirectionY);
        return MathF.Abs(dx) <= halfWidth + ax * beamHalfLength + ay * beamHalfWidth
            && MathF.Abs(dy) <= halfHeight + ay * beamHalfLength + ax * beamHalfWidth
            && MathF.Abs(dx * DirectionX + dy * DirectionY)
                <= beamHalfLength + halfWidth * ax + halfHeight * ay
            && MathF.Abs(-dx * DirectionY + dy * DirectionX)
                <= beamHalfWidth + halfWidth * ay + halfHeight * ax;
    }
}

internal sealed class FirstSeveranceLanceVolley
{
    internal FirstSeveranceLanceVolley(uint serial, ulong startTick,
        IReadOnlyList<FirstSeveranceLanceRay> rays)
    {
        if (serial == 0 || startTick == 0 || startTick > ulong.MaxValue - FirstSeveranceLanceTuning.CadenceTicks
            || rays is null || rays.Count is < 1 or > FirstSeveranceLanceTuning.MaximumRays)
            throw new ArgumentException("Invalid observation-lance volley.");
        foreach (FirstSeveranceLanceRay ray in rays)
            if (!ray.IsValid)
                throw new ArgumentException("A lance requires a finite origin and unit direction.");

        Serial = serial;
        StartTick = startTick;
        Rays = FirstSeverancePlanCollections.Copy(rays, nameof(rays));
    }

    internal uint Serial { get; }
    internal ulong StartTick { get; }
    internal ulong FireTick => StartTick + FirstSeveranceLanceTuning.TelegraphTicks;
    internal ulong EndTick => FireTick + FirstSeveranceLanceTuning.ActiveTicks;
    internal IReadOnlyList<FirstSeveranceLanceRay> Rays { get; }
    internal bool IsFiring(ulong tick) => tick >= FireTick && tick < EndTick;
}
