#nullable enable
using System;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Pure score, pose and spoke rules for The Last Waltz (docs/encounters/ebon-manor/REWARDS.md#the-last-waltz).
// No Terraria/XNA references: the domain tests link this file. All numbers come from EbonRewardRules and
// EbonRules; only the presentation timing of one fling (lift, pull) and the spoke envelope live here.
internal static class EbonLastWaltzRules
{
    // The first six beats of the eight-beat score fling a piece; the last two are the waltz.
    internal const int Slots = EbonRewardRules.CompanionSlots, Mana = 10, Flings = EbonRewardRules.ScoreBeats - 2;
    // One fling: Noirette lifts the piece for Lift ticks (cast pose), yanks it (pull pose, PullHold ticks) and it flies.
    internal const int Lift = 12, PullHold = 8, MaxFlight = 80, Anticipation = 9, Settle = 6, RestartCooldown = 30;
    // Spokes: staggered unfurl, a short fold at the end; below SpokeLive px a spoke is too short to touch anything.
    internal const float SpokeReach = 240, SpokeInner = 22, SpokeWidth = 22, SpokeLive = 24;
    internal const int SpokeStagger = 2, SpokeOpen = 8, SpokeClose = 8;

    internal static int ItemDamage => EbonRewardRules.Damage(EbonRewardKind.Summon) * EbonRewardRules.CompanionDamageScale;
    internal static int Cycle => EbonRewardRules.ScoreTicks;

    internal static bool CanSummon(int capacity, int owned) => capacity >= Slots && owned == 0;
    // Player buffs can arrive after their projectile, or be absent on observers: only the owner may read a
    // missing buff as an explicit dismissal.
    internal static bool DismissForMissingBuff(bool isOwner, bool hasBuff) => isOwner && !hasBuff;

    // --- Score clock -------------------------------------------------------------------------
    // Tick 0 means idle (no target). While a target exists the score runs 1..Cycle (230 ticks) and loops to 1.
    internal static bool ValidTick(float tick) => float.IsFinite(tick) && tick >= 0 && tick <= Cycle;
    internal static int Advance(int tick, bool hasTarget) => !hasTarget ? 0 : tick + 1 > Cycle ? 1 : tick + 1;
    // Beat k (0..7) lands on round(k * 28.8); beat 0 is bumped to tick 1 because tick 0 is idle.
    internal static int BeatTick(int beat) => Math.Max(1, (int)MathF.Round(beat * EbonRewardRules.BeatTicks));
    internal static int WaltzStart => BeatTick(Flings);
    internal static int SpokeLife => Cycle - WaltzStart - 1;
    internal static int YankTick(int piece) => BeatTick(piece) + Lift;
    internal static bool FlingAt(int tick, out int piece)
    {
        for (piece = 0; piece < Flings; piece++)
            if (BeatTick(piece) == tick) return true;
        piece = -1;
        return false;
    }
    internal static bool WaltzAt(int tick) => tick == WaltzStart;
    internal static bool GatherAt(int tick) => tick == WaltzStart - Anticipation;

    internal static float FlingSpeed(int flightTicks)
        => MathF.Min(EbonRewardRules.YankMax, EbonRewardRules.YankSpeed + EbonRewardRules.YankAcceleration * Math.Max(0, flightTicks));

    // Ticks since the most recent yank of this loop, or -1 before the first one (drives the pull recoil).
    internal static int SinceYank(int tick)
    {
        for (int piece = Flings - 1; piece >= 0; piece--)
            if (tick >= YankTick(piece)) return tick - YankTick(piece);
        return -1;
    }

    // --- Pose (Noirette.png cells: 0 idle, 1 sway, 2 float, 3 glide, 4 cast, 5 pull, 6 parasol, 7 command) ---
    internal const int FrameCount = 8;
    internal static int Frame(int tick, bool moving, int idle)
    {
        if (tick <= 0) return moving ? 3 : (idle % 420 is >= 300 and < 322) ? 1 : 2;
        if (tick >= Cycle - Settle) return 2;
        if (tick >= WaltzStart) return 6;
        // The last fling's recovery blends into a pointing "command" cell before the parasol opens.
        if (tick >= WaltzStart - Anticipation) return 7;
        for (int piece = Flings - 1; piece >= 0; piece--)
        {
            int since = tick - BeatTick(piece);
            if (since < 0) continue;
            return since < Lift ? 4 : since < Lift + PullHold ? 5 : 2;
        }
        return 2;
    }

    // --- Spokes ------------------------------------------------------------------------------
    internal static float SpokePhase(int identity) => (identity * 1.7f) % MathF.Tau;
    internal static float SpokeLength(int index, float age)
    {
        float open = EbonRewardRules.Smooth((age - index * SpokeStagger) / SpokeOpen);
        float close = 1 - EbonRewardRules.Smooth((age - (SpokeLife - SpokeClose)) / SpokeClose);
        return SpokeReach * open * close;
    }
    // One full turn around the target over the waltz, in beat steps (EbonRules.WaltzTurn is the boss's lilt),
    // normalised so the turn is exactly 2pi when the spokes fold.
    internal static float SpokeAngle(int index, float age, float phase)
        => phase + index * MathF.Tau / EbonRewardRules.Spokes
            + MathF.Tau * EbonRules.WaltzTurn(Math.Clamp(age, 0, SpokeLife)) / EbonRules.WaltzTurn(SpokeLife);
    // First spoke age at which the same spoke may touch the same NPC root again: the native 8-tick immunity,
    // and not before the next beat, so a body lying across a spoke is struck once per beat step (twice a waltz).
    internal static int SpokeRearm(int age)
        => Math.Max(age + EbonRewardRules.SpokeImmunity, (int)MathF.Ceiling((float)EbonRewardRules.NextBeatTick(age + .001)));
}
