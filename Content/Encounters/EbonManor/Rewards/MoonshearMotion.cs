#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Moonshear's kata, Cut Line clock and owner bookkeeping (docs/encounters/ebon-manor/REWARDS.md#melee).
// Pure: no Terraria/XNA references, the domain tests link this file with EbonRewardRules.
//
// Offsets are radians from the aim, mirrored by facing (negative = raised, as Soboro). Axis is the
// shears' centre line through the pivot and Open the half-angle between the blades, so the upper
// blade is Axis - Open and the lower Axis + Open. Knot speeds are radians per tick; every table ends
// on the next stroke's first knot (D's 2pi wraps to A), so pose and angular speed carry across.
internal static class MoonshearMotion
{
    internal const int Down = 0, Rise = 1, Wide = 2, Snip = 3, Strokes = 4;
    internal const int MaximumDuration = 30, SnipClose = 19;
    internal static int Duration(int stroke) => stroke switch { Rise => 16, Snip => 30, _ => 18 };
    internal static int Release(int stroke) => stroke switch { Rise => 6, Snip => 14, _ => 7 };
    internal static int LiveEnd(int stroke) => stroke switch { Rise => 12, Snip => 20, _ => 14 };
    internal static bool Live(int stroke, int age) => age >= Release(stroke) && age < LiveEnd(stroke);
    // A and C cut with the upper blade, B with the lower, the Snip with both.
    internal static bool Cuts(int stroke, bool upper) => stroke == Snip || (stroke == Rise) != upper;
    internal static float Multiplier(int stroke) => stroke == Snip ? EbonRewardRules.SnipMultiplier : 1f;
    internal static bool Marks(int stroke) => stroke != Snip;

    // (tick, offset, speed) triples.
    private static readonly float[][] AxisKnots =
    {
        // A: the Snip's recoil lifts into a crest, then the upper blade falls through a diagonal.
        new[] { 0f, -.55f, -.12f, 5.4f, -1.62f, 0f, 7f, -1.42f, .24f, 10.5f, .05f, .52f, 14f, 1.15f, .20f, 18f, 1.52f, .07f },
        // B: a rounded turn below the body, then the lower blade rises past the guard.
        new[] { 0f, 1.52f, .07f, 2.4f, 1.66f, 0f, 6f, 1.20f, -.24f, 9f, -.05f, -.52f, 12f, -1.20f, -.24f, 16f, -1.80f, -.12f },
        // C: the rise carries over the head into a coil behind, then the widest sweep through the horizon.
        new[] { 0f, -1.80f, -.12f, 4.4f, -2.42f, 0f, 7f, -1.95f, .30f, 10.5f, -.30f, .62f, 14f, .95f, .24f, 18f, 1.50f, .08f },
        // D: the shears keep turning down and round the back, slow at the coil, drive over the top and
        // close on the aim at full reach; the follow-through recoils up into A's wind-up.
        new[] { 0f, 1.50f, .08f, 5f, 2.75f, .30f, 11f, 4.05f, .05f, 14f, 4.45f, .25f, 17.2f, 5.62f, .46f,
            SnipClose, MathF.Tau, .34f, 23.5f, 6.92f, 0f, 30f, MathF.Tau - .55f, -.12f },
    };
    private static readonly float[][] OpenKnots =
    {
        new[] { 0f, .15f, 0f, 5.4f, .40f, 0f, 14f, .28f, 0f, 18f, .26f, 0f },
        new[] { 0f, .26f, 0f, 6f, .34f, 0f, 12f, .28f, 0f, 16f, .30f, 0f },
        new[] { 0f, .30f, 0f, 5f, .52f, 0f, 14f, .46f, 0f, 18f, .32f, 0f },
        // Wide open behind, then snapping shut: the blades slide a little past each other, as shears do.
        new[] { 0f, .32f, 0f, 9.5f, .85f, 0f, 13.5f, .85f, 0f, SnipClose, 0f, -.16f, 21.8f, -.09f, 0f, 25f, 0f, .03f, 30f, .15f, 0f },
    };
    // Pixels the pivot drives forward along the axis: D pushes into the close.
    private static readonly float[] SnipThrust = { 0f, 0f, 0f, 13f, 0f, 0f, SnipClose, 18f, 0f, 26f, 0f, 0f, 30f, 0f, 0f };

    internal static float Axis(int stroke, float age) => Evaluate(AxisKnots[Index(stroke)], age, out _);
    internal static float AxisSpeed(int stroke, float age) { Evaluate(AxisKnots[Index(stroke)], age, out float speed); return speed; }
    internal static float Open(int stroke, float age) => Evaluate(OpenKnots[Index(stroke)], age, out _);
    internal static float Blade(int stroke, float age, bool upper) => Axis(stroke, age) + (upper ? -Open(stroke, age) : Open(stroke, age));
    internal static float Thrust(int stroke, float age) => stroke == Snip ? Evaluate(SnipThrust, age, out _) : 0f;

    internal static float AxisAngle(int stroke, float age, float aim, int facing) => aim + facing * Axis(stroke, age);
    internal static float BladeAngle(int stroke, float age, float aim, int facing, bool upper)
        => aim + facing * Blade(stroke, age, upper);
    // The elbow trails the axis by its own speed, so the lag is continuous across stroke joins.
    internal static float ArmAngle(int stroke, float age, float aim, int facing)
        => aim + facing * (Axis(stroke, age) - .14f * AxisSpeed(stroke, age));

    // Soboro's sub-tick policy: never show the harmless wind-up on a live tick or a live
    // pose on a recovery tick. ageTick is the accepted simulation age.
    internal static float DrawAge(int stroke, int ageTick, float fraction)
    {
        float age = Math.Clamp(ageTick - 1f + fraction, 0f, Duration(stroke));
        if (ageTick >= Release(stroke)) age = MathF.Max(age, Release(stroke));
        if (ageTick >= LiveEnd(stroke)) age = MathF.Max(age, LiveEnd(stroke));
        return age;
    }

    private static int Index(int stroke) => Math.Clamp(stroke, 0, Strokes - 1);

    private static float Evaluate(float[] knots, float t, out float speed)
    {
        int last = knots.Length - 3, i = 0;
        t = Math.Clamp(t, knots[0], knots[last]);
        while (i < last - 3 && t > knots[i + 3]) i += 3;
        float t0 = knots[i], a = knots[i + 1], sa = knots[i + 2];
        float t1 = knots[i + 3], b = knots[i + 4], sb = knots[i + 5];
        float span = t1 - t0, u = Math.Clamp((t - t0) / span, 0f, 1f), u2 = u * u, u3 = u2 * u;
        speed = ((6 * u2 - 6 * u) * a + (3 * u2 - 4 * u + 1) * span * sa
            + (-6 * u2 + 6 * u) * b + (3 * u2 - 2 * u) * span * sb) / span;
        return (2 * u3 - 3 * u2 + 1) * a + (u3 - 2 * u2 + u) * span * sa
            + (-2 * u3 + 3 * u2) * b + (u3 - u2) * span * sb;
    }

    // --- Cut Line: forecast -> travel -> live tear -> pops ------------------------------
    internal const int TravelStart = EbonRewardRules.CutForecast;
    internal const int TearStart = TravelStart + EbonRewardRules.CutTravel;
    internal const int TearEnd = TearStart + EbonRewardRules.CutLive;
    internal const int MaxPops = 40, ChalkWidth = 14;
    internal static bool TearLive(int age) => age >= TearStart && age < TearEnd;
    internal static int PopTick(int pop) => TearEnd + Math.Max(0, pop) * EbonRewardRules.PopSpacing;
    internal static int Cooldown => TearEnd + EbonRewardRules.CutCooldown;
    internal static float Forecast(float age) => EbonRewardRules.Smooth(age / EbonRewardRules.CutForecast);
    // The shears bolt off the hand and settle onto the far end.
    internal static float Travel(float age)
    {
        float x = Math.Clamp((age - TravelStart) / EbonRewardRules.CutTravel, 0f, 1f);
        return 1 - (1 - x) * (1 - x) * (1 - x);
    }
    // Half-opening of the racing shears: three snips along the line, shut as the tear opens.
    internal static float RaceOpen(float age)
    {
        if (age < TravelStart) return .22f + .2f * EbonRewardRules.Smooth(age / EbonRewardRules.CutForecast);
        if (age >= TearStart) return .42f * MathF.Max(0, 1 - (age - TearStart) / 3f);
        float x = (age - TravelStart) / EbonRewardRules.CutTravel;
        return .42f * MathF.Abs(MathF.Cos(x * MathF.PI * 3));
    }
    internal static Vector2 ClampLine(Vector2 from, Vector2 to, Vector2 fallback)
    {
        Vector2 line = to - from;
        float length = line.Length();
        if (!float.IsFinite(length) || length < 48)
            return from + (fallback.LengthSquared() > 1e-6f ? Vector2.Normalize(fallback) : Vector2.UnitX) * 48;
        return length > EbonRewardRules.CutRange ? from + line / length * EbonRewardRules.CutRange : to;
    }
    internal static float Along(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a; float length = ab.LengthSquared();
        return length < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(point - a, ab) / length, 0, 1);
    }
}

// The kata counter: A -> B -> C -> D -> A, restarting after ComboResetTicks idle from a stroke's end.
internal sealed class MoonshearCombo
{
    private int next;
    private ulong idleFrom;
    private bool used;

    internal int Next => used ? next : MoonshearMotion.Down;

    internal int Take(ulong now)
    {
        if (!used || now > idleFrom + EbonRewardRules.ComboResetTicks) next = MoonshearMotion.Down;
        int stroke = next;
        next = (next + 1) % MoonshearMotion.Strokes;
        idleFrom = now + (ulong)MoonshearMotion.Duration(stroke);
        used = true;
        return stroke;
    }

    internal void Reset() { next = MoonshearMotion.Down; idleFrom = 0; used = false; }
}

// Owner-side chalk marks keyed by NPC slot with the NPC type that earned them: at most MaxMarks,
// all expiring MarkLife ticks after the newest; a slot reused by another NPC never inherits them.
internal sealed class MoonshearMarks
{
    private readonly int[] type, count;
    private readonly ulong[] stamp;

    internal MoonshearMarks(int capacity)
    {
        type = new int[capacity]; count = new int[capacity]; stamp = new ulong[capacity];
    }

    internal int Capacity => count.Length;
    internal int Count(int index) => (uint)index < (uint)count.Length ? count[index] : 0;
    internal ulong Stamp(int index) => (uint)index < (uint)stamp.Length ? stamp[index] : 0;

    internal int Add(int index, int npcType, ulong now)
    {
        if ((uint)index >= (uint)count.Length) return 0;
        if (count[index] > 0 && type[index] != npcType) count[index] = 0;
        type[index] = npcType;
        count[index] = Math.Min(EbonRewardRules.MaxMarks, count[index] + 1);
        stamp[index] = now;
        return count[index];
    }

    internal int Take(int index, int npcType)
    {
        if ((uint)index >= (uint)count.Length) return 0;
        int marks = type[index] == npcType ? count[index] : 0;
        count[index] = 0;
        return marks;
    }

    // liveType < 0: the NPC in this slot is gone or dead.
    internal bool Expire(int index, int liveType, ulong now)
    {
        if (Count(index) == 0) return false;
        if (liveType >= 0 && liveType == type[index] && now >= stamp[index]
            && now - stamp[index] <= (ulong)EbonRewardRules.MarkLife) return false;
        count[index] = 0;
        return true;
    }

    internal bool Any()
    {
        foreach (int marks in count) if (marks > 0) return true;
        return false;
    }

    internal void Clear() => Array.Clear(count);
}
