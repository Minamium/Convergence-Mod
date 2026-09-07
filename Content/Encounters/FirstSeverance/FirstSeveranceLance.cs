#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceAttackKind : byte
{
    ObservationLance = 0,
    PursuitPrism = 1,
    SweepRight = 2,
    Stillness = 3,
    SweepLeft = 4,
}

// Shared geometry/timing, not client authority. Only the owning combat runtime
// constructs assignments or applies hits; a snapshot carries the locked rays.
internal static class FirstSeveranceLanceTuning
{
    internal const int TelegraphTicks = 42;
    internal const int PrismTelegraphTicks = 28;
    internal const int StillnessTelegraphTicks = 36;
    internal const int PatternActiveTicks = 12;
    internal const int ChargeActiveTicks = 22;
    internal const int ChargeLockLeadTicks = 24;
    internal const int ActiveTicks = 14;
    internal const int CadenceTicks = 66;
    internal const int MaximumRays = 4;
    internal const float Length = 2600f;
    internal const float HalfWidth = 44f;
    internal const float BossHeightAboveCore = 560f;
    internal const float StackRadius = 112f;
    internal const float SpreadRadius = 352f;
    internal const float SpreadSeparation = SpreadRadius * 2f;

    internal static bool IsAttackPhase(FirstSeveranceSubstate phase)
        => phase is FirstSeveranceSubstate.PylonCheck or FirstSeveranceSubstate.CoreExposure;
}

internal readonly record struct FirstSeveranceLanceRay(float X, float Y, float DirectionX, float DirectionY,
    float Length = FirstSeveranceLanceTuning.Length, float HalfWidth = FirstSeveranceLanceTuning.HalfWidth)
{
    internal bool IsValid => float.IsFinite(X) && float.IsFinite(Y)
        && MathF.Abs(X) <= 1_000_000f && MathF.Abs(Y) <= 1_000_000f
        && float.IsFinite(DirectionX) && float.IsFinite(DirectionY)
        && MathF.Abs(DirectionX * DirectionX + DirectionY * DirectionY - 1f) < 0.001f
        && float.IsFinite(Length) && Length is >= 32f and <= 3600f
        && float.IsFinite(HalfWidth) && HalfWidth is >= 4f and <= 320f;

    // Four-axis SAT for a finite beam rectangle vs a player's axis-aligned box.
    // The visible danger material uses this same half-width; soft bloom is decorative.
    internal bool Intersects(float centerX, float centerY, float halfWidth, float halfHeight)
    {
        float beamHalfLength = Length * 0.5f;
        float beamHalfWidth = HalfWidth;
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
        IReadOnlyList<FirstSeveranceLanceRay> rays,
        FirstSeveranceAttackKind kind = FirstSeveranceAttackKind.ObservationLance, byte step = 0,
        int targetSlot = -1, ulong motionTick = 0)
    {
        if (serial == 0 || startTick == 0 || startTick > ulong.MaxValue - 128
            || rays is null || rays.Count is < 1 or > FirstSeveranceLanceTuning.MaximumRays
            || !Enum.IsDefined(kind) || step > 7 || targetSlot is < -1 or >= 255)
            throw new ArgumentException("Invalid observation-lance volley.");
        foreach (FirstSeveranceLanceRay ray in rays)
            if (!ray.IsValid)
                throw new ArgumentException("A lance requires a finite origin and unit direction.");
        bool validPattern = kind switch
        {
            FirstSeveranceAttackKind.ObservationLance => step == 0 && rays.Count <= 2,
            FirstSeveranceAttackKind.PursuitPrism => targetSlot >= 0,
            FirstSeveranceAttackKind.SweepRight => step == 0 && rays.Count == 1 && targetSlot >= 0
                && rays[0].Length == 164f && rays[0].HalfWidth == 50f,
            FirstSeveranceAttackKind.SweepLeft => step == 2 && rays.Count == 1 && targetSlot >= 0
                && rays[0].Length == 164f && rays[0].HalfWidth == 50f,
            FirstSeveranceAttackKind.Stillness => step is 1 or 3 && rays.Count == 2 && targetSlot >= 0
                && rays[0].DirectionX == 0f && rays[0].DirectionY == 1f
                && rays[1].DirectionX == 0f && rays[1].DirectionY == 1f
                && rays[0].Y == rays[1].Y && rays[0].Length == rays[1].Length
                && MathF.Abs(rays[1].X - rays[0].X - rays[0].HalfWidth - rays[1].HalfWidth
                    - FirstSeveranceAttackPatterns.StillnessSafeHalfWidth * 2f) < 0.1f,
            _ => false,
        };
        if (!validPattern)
            throw new ArgumentException("Attack kind, step and ray shape must agree.");

        Serial = serial;
        StartTick = startTick;
        Kind = kind;
        Step = step;
        TargetSlot = targetSlot;
        Rays = FirstSeverancePlanCollections.Copy(rays, nameof(rays));
        MotionTick = motionTick;
        if (IsCharge ? motionTick < startTick || motionTick >= EndTick : motionTick != 0)
            throw new ArgumentException("Invalid energy-body sample tick.");
    }

    internal uint Serial { get; }
    internal ulong StartTick { get; }
    internal FirstSeveranceAttackKind Kind { get; }
    internal byte Step { get; }
    internal int TargetSlot { get; }
    internal ulong MotionTick { get; }
    internal bool IsCharge => Kind is FirstSeveranceAttackKind.SweepRight or FirstSeveranceAttackKind.SweepLeft;
    internal ulong LockTick => FireTick - FirstSeveranceLanceTuning.ChargeLockLeadTicks;
    internal int TelegraphTicks => Kind == FirstSeveranceAttackKind.PursuitPrism ? FirstSeveranceLanceTuning.PrismTelegraphTicks
        : Kind == FirstSeveranceAttackKind.Stillness ? FirstSeveranceLanceTuning.StillnessTelegraphTicks : FirstSeveranceLanceTuning.TelegraphTicks;
    internal int ActiveTicks => Kind == FirstSeveranceAttackKind.Stillness ? FirstSeveranceCurtainComb.ActiveTicks
        : Kind == FirstSeveranceAttackKind.ObservationLance
        ? FirstSeveranceLanceTuning.ActiveTicks : IsCharge ? FirstSeveranceLanceTuning.ChargeActiveTicks : FirstSeveranceLanceTuning.PatternActiveTicks;
    internal ulong FireTick => StartTick + (ulong)TelegraphTicks;
    internal ulong EndTick => FireTick + (ulong)ActiveTicks;
    internal IReadOnlyList<FirstSeveranceLanceRay> Rays { get; }
    internal bool IsFiring(ulong tick) => tick >= FireTick && tick < EndTick;

    internal FirstSeveranceLanceRay RayAt(int index, ulong tick)
    {
        FirstSeveranceLanceRay ray = Rays[index];
        if (!IsCharge)
            return ray;
        var head = ChargeHeadAt(tick);
        float speed = IsFiring(tick) ? 104f : 0f;
        // Include the previous-to-current head sweep: 104px/tick cannot tunnel
        // through a stationary player. The long visual wake is NOT a hitbox.
        float tail = ray.Length + speed;
        return ray with { X = head.X - ray.DirectionX * tail, Y = head.Y - ray.DirectionY * tail,
            Length = tail + ray.HalfWidth };
    }

    internal (float X, float Y) ChargeHeadAt(ulong tick)
    {
        var ray = Rays[0];
        if (!IsCharge || tick < FireTick || MotionTick < LockTick || tick <= MotionTick)
            return (ray.X, ray.Y);
        ulong end = Math.Min(tick, EndTick - 1);
        float travel = 0f;
        for (ulong sample = Math.Max(MotionTick + 1, FireTick); sample <= end; sample++)
            travel += 104f;
        return (ray.X + ray.DirectionX * travel, ray.Y + ray.DirectionY * travel);
    }

    // Called once per authority tick only. Clients extrapolate the last bounded
    // sample; they never steer from their own view of the target player.
    internal FirstSeveranceLanceVolley AdvanceCharge(ulong tick, float targetX, float targetY,
        float velocityX, float velocityY)
    {
        if (!IsCharge || tick <= MotionTick || tick >= EndTick)
            return this;
        if (!float.IsFinite(targetX) || !float.IsFinite(targetY)
            || !float.IsFinite(velocityX) || !float.IsFinite(velocityY))
            throw new ArgumentException("Invalid authority charge target.");
        var ray = Rays[0];
        float x = ray.X, y = ray.Y, dx = ray.DirectionX, dy = ray.DirectionY;
        // Tracking and the large orbital windup are harmless. Hold the exact
        // origin/heading for 24 ticks before launch, then never home while live.
        if (tick < LockTick)
        {
            float age = (tick - StartTick) / (float)(LockTick - StartTick);
            float side = Kind == FirstSeveranceAttackKind.SweepRight ? 1f : -1f;
            float angle = MathF.PI + MathF.PI * age;
            float wantedX = targetX + MathF.Cos(angle) * side * 520f;
            float wantedY = targetY - 90f + MathF.Sin(angle) * 330f;
            float mx = wantedX - x, my = wantedY - y;
            float distance = MathF.Sqrt(mx * mx + my * my);
            float move = Math.Min(1f, 64f / Math.Max(distance, 0.001f));
            x += mx * move;
            y += my * move;
        }
        if (tick <= LockTick)
        {
            float wanted = MathF.Atan2(targetY + Math.Clamp(velocityY * 6f, -120f, 120f) - y,
                targetX + Math.Clamp(velocityX * 6f, -120f, 120f) - x);
            float current = MathF.Atan2(dy, dx);
            float turn = MathF.IEEERemainder(wanted - current, MathF.Tau);
            float angle = tick == LockTick ? wanted : current + Math.Clamp(turn, -0.12f, 0.12f);
            dx = MathF.Cos(angle);
            dy = MathF.Sin(angle);
        }
        if (tick >= FireTick)
        {
            const float speed = 104f;
            x += dx * speed;
            y += dy * speed;
        }
        return new(Serial, StartTick, new[] { ray with { X = x, Y = y, DirectionX = dx, DirectionY = dy } },
            Kind, Step, TargetSlot, tick);
    }
}
