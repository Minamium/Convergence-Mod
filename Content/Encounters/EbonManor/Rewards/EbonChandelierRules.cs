#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

internal enum ChandelierState : byte { Idle, Seek, Settle, Fall, Reweave }

// Pure timing, kinematics and animation curves for the Ballroom Chandelier
// (docs/encounters/ebon-manor/REWARDS.md#summon--ballroom-chandelier). The tuning numbers live in
// EbonRewardRules; this adds only what the minion and its presentation share. No Terraria/XNA
// references: the domain tests link this file.
internal static class EbonChandelierRules
{
    // Centre -> lowest point of a chandelier (the crash point), and how far a shattered one is reeled up.
    internal const float BodyHalfHeight = 34, ReelRise = 190, MinHover = 70, MaxLead = 24;
    internal const float AcquireRange = 1600, RetainRange = 2000;
    // ChandelierReel is mixed 0.12 s (about 7 ticks) after ChandelierShatter, when the pieces have turned back.
    internal const int AnticipationTicks = 10, ReelCueTicks = 7, SnipTicks = 22, ShatterTicks = 34, CatchTicks = 3;

    // --- Fall -------------------------------------------------------------------------------
    // Speed (px/tick) on the tick `age` ticks after the cut: DropSpeed + DropGravity per tick, capped at DropMax.
    internal static float FallSpeed(int age)
        => Math.Min(EbonRewardRules.DropSpeed + EbonRewardRules.DropGravity * Math.Max(age, 0), EbonRewardRules.DropMax);

    internal static float FallDistance(int ticks)
    {
        float distance = 0;
        for (int age = 0; age < ticks; age++) distance += FallSpeed(age);
        return distance;
    }

    // Ticks a fall needs to cover `distance` px (0 for none).
    internal static int FallTicks(float distance)
    {
        int ticks = 0;
        for (float travelled = 0; travelled < distance && ticks < 600; ticks++) travelled += FallSpeed(ticks);
        return ticks;
    }

    // How far ahead of a moving target the hover point leads it: one fall's worth of its velocity, bounded.
    internal static Vector2 Lead(Vector2 velocity, int fallTicks)
        => Vector2.Clamp(velocity * Math.Min(fallTicks, MaxLead), new Vector2(-220, -120), new Vector2(220, 120));

    // --- Beat schedule ------------------------------------------------------------------------
    // The whole update on which chandelier `index` drops: its beat falls between updates, so it lands on
    // the first update at or after it. Never earlier than `now`, at most one 4-beat cycle later.
    internal static long DropAt(int index, long now)
    {
        double beat = EbonRewardRules.DropTick(index, now - 1 + 1e-6);
        return (long)Math.Ceiling(beat - 1e-9);
    }

    // 0 until the drop is scheduled (wait < 0), then a smooth build over the last AnticipationTicks, 1 at the cut.
    internal static float Anticipation(int wait)
        => wait < 0 ? 0 : wait == 0 ? 1 : EbonRewardRules.Smooth(1f - wait / (float)AnticipationTicks);

    // --- Idle row -----------------------------------------------------------------------------
    // Offset from the owner's shoulders: a staggered row behind the owner (opposite to `facing`), small and
    // large chandeliers alternating in height so neighbours never overlap.
    internal static Vector2 IdleSlot(int index, int count, int facing)
    {
        count = Math.Max(count, 1);
        float offset = index - (count - 1) * .5f, spacing = Math.Clamp(560f / count, 46f, 78f);
        return new Vector2(offset * spacing - (facing < 0 ? -1 : 1) * 72f,
            -150f + (Math.Abs(index) % 2) * 28f - Math.Abs(offset) * 5f);
    }

    // Pendulum angle (radians) of a hanging chandelier at visual clock `t` (ticks): two incommensurate swings.
    internal static float Sway(float t, float phase)
        => .045f * MathF.Sin(t * .047f + phase) + .02f * MathF.Sin(t * .113f + phase * 1.7f);

    // --- Cut and reweave ------------------------------------------------------------------------
    // The severed thread's upper end whips away from the cut (px above the cut after `sinceSnip` ticks).
    internal static float Retract(float sinceSnip)
    {
        sinceSnip = Math.Max(sinceSnip, 0);
        return Math.Min(.9f * sinceSnip * sinceSnip, 900f);
    }

    // Outward displacement of a shattered piece over the reweave (u 0..1): 0, a sharp burst to 1 at u = .2,
    // then a smooth return to 0 as the pieces fly back together.
    internal static float Scatter(float u)
    {
        u = Math.Clamp(u, 0, 1);
        const float burst = .2f;
        if (u < burst) { float x = 1 - u / burst; return 1 - x * x * x; }
        return 1 - EbonRewardRules.Smooth((u - burst) / (1 - burst));
    }

    // Reel: cumulative rise (px) `age` ticks into the reweave, and the step applied on that tick.
    internal static float ReelOffset(int age) => ReelRise * EbonRewardRules.Smooth(age / (float)EbonRewardRules.ReweaveTicks);
    internal static float ReelStep(int age) => ReelOffset(age) - ReelOffset(age - 1);

    // Candle `candle` of `count` starts to catch at this many ticks after the shatter and is fully lit
    // CatchTicks later; the last candle is lit before the reweave ends.
    internal static float RelightStart(int candle, int count) => 11f + candle * 8f / Math.Max(count - 1, 1);
    internal static float Relit(float sinceImpact, int candle, int count)
        => Math.Clamp((sinceImpact - RelightStart(candle, count)) / CatchTicks, 0, 1);

    // --- Geometry -----------------------------------------------------------------------------
    // Does a circle touch an axis-aligned box (the shatter footprint against an NPC hitbox)?
    internal static bool CircleTouchesBox(Vector2 centre, float radius, Vector2 min, Vector2 max)
        => Vector2.DistanceSquared(centre, Vector2.Clamp(centre, min, max)) <= radius * radius;
}
