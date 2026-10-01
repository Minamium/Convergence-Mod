#nullable enable
using System;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Pure string state machine, ordering and curves for the Moonloom Harp (docs/encounters/ebon-manor/REWARDS.md,
// "Ranged"). No Terraria/XNA references: the domain tests link this file together with EbonRewardRules.
internal static class EbonLoomHarpRules
{
    // A needle that dies against a wall in front of the bow leaves no string.
    internal const float MinStringLength = 24;
    // Use animation of the right click; the glissando itself outlasts it.
    internal const int GlissandoUse = 24;

    // --- String state ---------------------------------------------------------------------
    // Projectile.ai[2], so every peer reads the same schedule from the native projectile sync:
    //   0          idle
    //   1 + code   scheduled; code = countdown | rank << 6 | last << 12 (countdown ticks until the pluck)
    //   -age       plucked; ages 1..PluckLive are live, the string is spent after that
    private const int CountdownMask = 63, RankShift = 6, LastShift = 12, MaxCode = CountdownMask | 63 << RankShift | 1 << LastShift;

    // A click schedules the rank-th oldest string for PluckTick(rank) ticks later. The extra tick keeps even the
    // first string in the scheduled state while it is replicated, so a peer always learns its rank and last flag.
    internal static float Schedule(int rank, bool last)
    {
        rank = Math.Clamp(rank, 0, EbonRewardRules.MaxStrings - 1);
        int countdown = Math.Clamp(EbonRewardRules.PluckTick(rank) + 1, 0, CountdownMask);
        return 1 + (countdown | rank << RankShift | (last ? 1 << LastShift : 0));
    }

    internal static bool IsWhole(float value) => float.IsFinite(value) && value == MathF.Floor(value);

    internal static bool TryDecode(float state, out int rank, out bool last, out int countdown)
    {
        rank = countdown = 0; last = false;
        if (!IsWhole(state) || state < 1 || state > 1 + MaxCode) return false;
        int code = (int)state - 1;
        countdown = code & CountdownMask;
        rank = (code >> RankShift) & 63;
        last = ((code >> LastShift) & 1) != 0;
        return rank < EbonRewardRules.MaxStrings;
    }

    internal static bool Valid(float state)
        => state == 0 || (IsWhole(state) && (state < 0 ? -state <= EbonRewardRules.PluckLive : TryDecode(state, out _, out _, out _)));

    internal static bool Live(float state) => state < 0 && -state <= EbonRewardRules.PluckLive;

    // What one simulation tick does to a string. Plucked: its live window begins this tick (age 1).
    // Spent: the live window is over and the string dies.
    internal readonly record struct Step(float State, bool Plucked, bool Spent);

    internal static Step Advance(float state)
    {
        if (state == 0 || !Valid(state)) return new Step(0, false, false);
        if (state < 0)
            return -state >= EbonRewardRules.PluckLive ? new Step(state, false, true) : new Step(state - 1, false, false);
        TryDecode(state, out _, out _, out int countdown);
        return countdown == 0 ? new Step(-1, true, false) : new Step(state - 1, false, false);
    }

    // --- Ordering -------------------------------------------------------------------------
    // Indices of `timeLeft` from the oldest string (least life left) to the newest; ties keep index order.
    internal static int[] OldestFirst(int[] timeLeft)
    {
        int[] order = new int[timeLeft.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        Array.Sort(order, (a, b) => timeLeft[a] != timeLeft[b] ? timeLeft[a].CompareTo(timeLeft[b]) : a.CompareTo(b));
        return order;
    }

    // Idle strings to retire before a new one is added, so that at most MaxStrings stand.
    internal static int Evictions(int idle) => Math.Max(0, idle + 1 - EbonRewardRules.MaxStrings);

    // --- Curves ---------------------------------------------------------------------------
    // Full until the last StringFade ticks, then a smooth fade to nothing.
    internal static float Fade(int timeLeft) => EbonRewardRules.Smooth(timeLeft / (float)EbonRewardRules.StringFade);

    // A struck string rings out over this many ticks (the projectile itself is gone after PluckLive).
    internal const float RingOut = 30, PluckPeak = 9;
    internal static float PluckAmplitude(float age) => age < 0 ? 0 : PluckPeak * MathF.Exp(-age / 5f);

    // A string crossed by an enemy: a short quiver.
    internal static float Quiver(float ticks, float peak) => ticks < 0 ? 0 : peak * MathF.Exp(-ticks / 7f);

    // The bow over one use cycle (progress 0 at the shot, 1 at the next): the released string rings for the first
    // RingEnd of it, then is drawn back toward the player so full draw lands on the next shot.
    internal const float RingEnd = .3f, BowRingPeak = 6, PulsePeak = 4;
    internal static float Pull(float progress) => EbonRewardRules.Smooth((progress - RingEnd) / (1 - RingEnd));
    // Ringing of the bowstring `ticks` after a release or a harp pluck; a drawn string does not ring.
    internal static float Ring(float ticks, float pull, float peak = BowRingPeak)
        => ticks < 0 ? 0 : peak * MathF.Exp(-ticks / 4.5f) * (1 - pull);
}
