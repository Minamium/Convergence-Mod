#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// The boss's replicated plans, gathered by one Main.ActiveProjectiles scan per update tick instead of one scan
// per signal / cue / pose request (about ten per frame). Read-only and presentation-only: the plans are the
// accepted descriptors (gestures with their locked aim, chorus calls), never a client decision. The cache key is
// (Fight, boss NPC, Main.GameUpdateCount): projectiles only change during an update, so every draw of the same
// tick sees the same list. The arrays grow (never shrink) so no plan is dropped and no frame allocates.
internal static class ScarletCueFrame
{
    private static CrimsonGesturePlan[] gestures = new CrimsonGesturePlan[48];
    private static CrimsonChorusPlan[] choruses = new CrimsonChorusPlan[8];
    private static int gestureCount, chorusCount, boss = -1;
    private static Guid fight;
    private static uint tick;
    private static bool filled;

    internal readonly ref struct View
    {
        internal View(ReadOnlySpan<CrimsonGesturePlan> gestures, ReadOnlySpan<CrimsonChorusPlan> choruses, bool member)
        { Gestures = gestures; Choruses = choruses; Member = member; }
        internal ReadOnlySpan<CrimsonGesturePlan> Gestures { get; }
        internal ReadOnlySpan<CrimsonChorusPlan> Choruses { get; }
        // Only a member of this fight (alive or Down: ScarletArticulation.Member) sees the new attack expression;
        // anyone else sees today's picture. The existing signal is unconditional.
        internal bool Member { get; }
    }

    internal static View Of(CrimsonBoss owner)
    {
        if (!filled || fight != owner.State.Fight || boss != owner.NPC.whoAmI || tick != Main.GameUpdateCount) Fill(owner);
        return new(gestures.AsSpan(0, gestureCount), choruses.AsSpan(0, chorusCount), ScarletArticulation.Member(owner));
    }

    internal static void Reset()
    {
        Array.Clear(gestures); Array.Clear(choruses);
        gestureCount = chorusCount = 0; boss = -1; fight = Guid.Empty; tick = 0; filled = false;
    }

    private static void Fill(CrimsonBoss owner)
    {
        gestureCount = chorusCount = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is CrimsonGesture gesture && gesture.TryBoss(out var parent) && parent == owner)
            {
                if (gestureCount == gestures.Length) Array.Resize(ref gestures, gestures.Length * 2);
                // The effective plan carries a tracking beam's locked aim (it locks on Born); timing fields are the plan's.
                gestures[gestureCount++] = gesture.EffectivePlan(gesture.Plan.Born, true);
            }
            else if (projectile.ModProjectile is CrimsonChorus chorus && CrimsonChorus.TryBoss(chorus.Plan, out var conductor) && conductor == owner)
            {
                if (chorusCount == choruses.Length) Array.Resize(ref choruses, choruses.Length * 2);
                choruses[chorusCount++] = chorus.Plan;
            }
        }
        fight = owner.State.Fight; boss = owner.NPC.whoAmI; tick = Main.GameUpdateCount; filled = true;
    }
}
