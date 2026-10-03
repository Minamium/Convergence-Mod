#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// The boss's replicated plans, gathered by one Main.ActiveProjectiles scan per update tick instead of one scan
// per signal / cue / pose request (about ten per frame). Read-only and presentation-only: the plans are the
// accepted descriptors (gestures, chorus calls), never a client decision. The cache key is
// (Fight, boss NPC, Main.GameUpdateCount): projectiles only change during an update, so every draw of the same
// tick sees the same list. The arrays grow (never shrink) so no plan is dropped and no frame allocates.
internal static class ScarletCueFrame
{
    private static CrimsonGesturePlan[] gestures = new CrimsonGesturePlan[48], known = new CrimsonGesturePlan[48];
    private static CrimsonChorusPlan[] choruses = new CrimsonChorusPlan[8];
    private static int gestureCount, knownCount, chorusCount, boss = -1;
    private static Guid fight;
    private static uint tick;
    private static bool filled;

    internal readonly ref struct View
    {
        internal View(ReadOnlySpan<CrimsonGesturePlan> gestures, ReadOnlySpan<CrimsonGesturePlan> known,
            ReadOnlySpan<CrimsonChorusPlan> choruses, bool member)
        { Gestures = gestures; Known = known; Choruses = choruses; Member = member; }
        internal ReadOnlySpan<CrimsonGesturePlan> Gestures { get; }
        // The plans a body or Vespera's orb may answer: every plan, except an aimed one whose lock has not arrived
        // (ScarletNotes.AimKnown). Only the signal, the Choir's cues and the casting pose's timing read Gestures.
        internal ReadOnlySpan<CrimsonGesturePlan> Known { get; }
        internal ReadOnlySpan<CrimsonChorusPlan> Choruses { get; }
        // Only a member of this fight (alive or Down: ScarletArticulation.Member) sees the new attack expression;
        // anyone else sees today's picture. The existing signal is unconditional.
        internal bool Member { get; }
    }

    internal static View Of(CrimsonBoss owner)
    {
        if (!filled || fight != owner.State.Fight || boss != owner.NPC.whoAmI || tick != Main.GameUpdateCount) Fill(owner);
        return new(gestures.AsSpan(0, gestureCount), known.AsSpan(0, knownCount), choruses.AsSpan(0, chorusCount), ScarletArticulation.Member(owner));
    }

    internal static void Reset()
    {
        Array.Clear(gestures); Array.Clear(known); Array.Clear(choruses);
        gestureCount = knownCount = chorusCount = 0; boss = -1; fight = Guid.Empty; tick = 0; filled = false;
    }

    private static void Fill(CrimsonBoss owner)
    {
        gestureCount = knownCount = chorusCount = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is CrimsonGesture gesture && gesture.TryBoss(out var parent) && parent == owner)
            {
                if (gestureCount == gestures.Length) Array.Resize(ref gestures, gestures.Length * 2);
                // The effective plan carries a tracking beam's aim: the authority's lock on Born once it has arrived, the
                // plan's issue-time Target before (a peer receives the lock a few ticks after Born); timing fields are the plan's.
                var plan = gesture.EffectivePlan(gesture.Plan.Born, true);
                gestures[gestureCount++] = plan;
                if (!ScarletNotes.AimKnown(plan, gesture.ForecastReady)) continue;
                if (knownCount == known.Length) Array.Resize(ref known, known.Length * 2);
                known[knownCount++] = plan;
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
