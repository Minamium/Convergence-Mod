using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeverancePrismTarget(int Slot, float X, float Y, float VelocityX, float VelocityY);

// Shared deterministic geometry, never client-selected targets or damage.
internal static class FirstSeveranceAttackPatterns
{
    internal const float StillnessSafeHalfWidth = 64f;
    // Category opening and complete-combo pauses stay deliberate; only the
    // steps inside each combo return to the earlier fast cadence.
    internal const int ExposureOpeningRestTicks = 60;
    internal const int SequenceRestTicks = 60;
    internal static int StepCount(FirstSeveranceSubstate phase)
        => phase == FirstSeveranceSubstate.PylonCheck ? 8 : 4;
    internal static int StepCadence(FirstSeveranceSubstate phase)
        => phase == FirstSeveranceSubstate.PylonCheck ? 42 : 72;
    internal static int SequenceTicks(FirstSeveranceSubstate phase)
        => (StepCount(phase) - 1) * StepCadence(phase) + StepTicks(phase, StepCount(phase) - 1);
    internal static int StepTicks(FirstSeveranceSubstate phase, int step)
        => phase == FirstSeveranceSubstate.PylonCheck
            ? FirstSeveranceLanceTuning.PrismTelegraphTicks + FirstSeveranceLanceTuning.PatternActiveTicks
            : step % 2 == 1
                ? FirstSeveranceLanceTuning.StillnessTelegraphTicks + FirstSeveranceCurtainComb.ActiveTicks
                : FirstSeveranceLanceTuning.TelegraphTicks + FirstSeveranceLanceTuning.ChargeActiveTicks;
    internal static int PrismColorIndex(int step) => step < 4 ? step : 7 - step;

    internal static FirstSeveranceLanceVolley CreatePrism(uint serial, ulong tick, byte step,
        IReadOnlyList<FirstSeverancePrismTarget> targets)
    {
        if (targets is null || targets.Count is < 1 or > FirstSeveranceLanceTuning.MaximumRays)
            throw new ArgumentException("Prism targets must be a bounded standing roster.");
        var rays = new List<FirstSeveranceLanceRay>(targets.Count);
        var slots = new HashSet<int>();
        foreach (var target in targets)
        {
            if (!slots.Add(target.Slot)) throw new ArgumentException("Duplicate prism target.");
            rays.Add(Create(serial, tick, FirstSeveranceSubstate.PylonCheck, step, target.Slot,
                target.X, target.Y, target.VelocityX, target.VelocityY).Rays[0]);
        }
        // TargetSlot is only the representative pose focus. All bounded world rays
        // are immutable assignments; neither client nor audio retargets a ray.
        return new(serial, tick, rays, FirstSeveranceAttackKind.PursuitPrism, step, targets[0].Slot);
    }

    internal static FirstSeveranceLanceVolley Create(uint serial, ulong tick,
        FirstSeveranceSubstate phase, byte step, int targetSlot,
        float targetX, float targetY, float velocityX, float velocityY)
    {
        if (!FirstSeveranceLanceTuning.IsAttackPhase(phase) || step >= StepCount(phase)
            || !float.IsFinite(targetX) || !float.IsFinite(targetY)
            || !float.IsFinite(velocityX) || !float.IsFinite(velocityY))
            throw new ArgumentException("Invalid attack pattern input.");
        if (phase == FirstSeveranceSubstate.PylonCheck)
        {
            int color = PrismColorIndex(step);
            float angle = (-0.55f + color * 0.32f);
            float dx = MathF.Sin(angle), dy = MathF.Cos(angle);
            float leadX = targetX + Math.Clamp(velocityX * 18f, -220f, 220f);
            float leadY = targetY + Math.Clamp(velocityY * 18f, -160f, 160f);
            var ray = new FirstSeveranceLanceRay(leadX - dx * 800f, leadY - dy * 800f, dx, dy);
            return new(serial, tick, new[] { ray }, FirstSeveranceAttackKind.PursuitPrism, step, targetSlot);
        }
        if (step is 1 or 3)
        {
            // A locked empty column: stationary is safe, continued horizontal
            // movement enters one of the two overhead curtains. No velocity test.
            const float width = 320f;
            return new(serial, tick, new[]
            {
                new FirstSeveranceLanceRay(targetX - StillnessSafeHalfWidth - width, targetY - 1200f, 0, 1, 2600f, width),
                new FirstSeveranceLanceRay(targetX + StillnessSafeHalfWidth + width, targetY - 1200f, 0, 1, 2600f, width),
            }, FirstSeveranceAttackKind.Stillness, step, targetSlot);
        }
        bool right = step == 0;
        var sweep = new FirstSeveranceLanceRay(targetX + (right ? -520f : 520f), targetY - 90f,
            right ? 1 : -1, 0, 164f, 50f);
        return new(serial, tick, new[] { sweep },
            right ? FirstSeveranceAttackKind.SweepRight : FirstSeveranceAttackKind.SweepLeft, step, targetSlot, tick);
    }
}

internal static class FirstSeveranceCombatRules
{
    // Beams are direct, fixed Raid damage; maximum life is used only by the
    // separate contact-style energy charge. Armor/recovery policy is unchanged.
    internal const int BeamDamage = 120;
    internal static int AttackDamage(FirstSeveranceAttackKind kind, int maximumLife)
    {
        if (maximumLife <= 0 || !Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(maximumLife));
        return kind is FirstSeveranceAttackKind.SweepRight or FirstSeveranceAttackKind.SweepLeft
            ? (int)Math.Max(1L, (long)maximumLife * 60 / 100)
            : BeamDamage;
    }

    internal static int StackDamage(int maximumLife, int rosterCount, int present)
    {
        if (maximumLife <= 0 || rosterCount is < FirstSeveranceRoster.MinimumCount or > FirstSeveranceRoster.MaximumCount || present < 0 || present > rosterCount)
            throw new ArgumentOutOfRangeException(nameof(present));
        return (int)(((long)maximumLife * (rosterCount - present) + rosterCount - 1) / rosterCount);
    }

    internal static bool ShouldExecuteDefeat(ulong previousSequence, FightId previousFight,
        ulong terminalSequence, FightId terminalFight, EncounterEndReason reason, bool connectedParticipant)
        => connectedParticipant && previousSequence != 0 && previousSequence == terminalSequence
            && !previousFight.IsNone && previousFight == terminalFight && reason == EncounterEndReason.Defeat;
}
