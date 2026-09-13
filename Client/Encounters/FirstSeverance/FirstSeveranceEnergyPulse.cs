#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.Client.Encounters.FirstSeverance;

// Read-only presentation envelopes. Max-combined, never multiplied by ray/player count.
internal static class FirstSeveranceEnergyPulse
{
    private static readonly int SecondTurn = FindSecondTurn();
    private static int FindSecondTurn()
    {
        for (int a = FirstSeveranceChoreography.BladeWindup + 1; a < FirstSeveranceChoreography.BladeEnd; a++)
            if (FirstSeveranceScoreGeometry.BladeTurn(a) != FirstSeveranceScoreGeometry.BladeTurn(a - 1)) return a;
        return FirstSeveranceChoreography.BladeEnd;
    }
    internal static float Charge(double tick, double start, double fire)
    {
        if (tick < start || tick >= fire) return 0;
        float t = Math.Clamp((float)((tick - start) / Math.Max(1, fire - start)), 0, 1);
        float beat = MathF.Pow(.5f + .5f * MathF.Sin((float)(tick - start) * .31f), 3);
        return t * t * (.64f + .36f * beat);
    }
    internal static float Kick(double tick, double fire, double end)
    {
        if (tick < fire || tick >= end + 12) return 0;
        float age = (float)(tick - fire);
        float release = Math.Clamp((float)((end + 12 - tick) / 12), 0, 1);
        return (13 * MathF.Exp(-age / 12) + 2.5f) * release;
    }
    internal static float Shake(FirstSeveranceCombatProjection c, double tick)
    {
        float value = 0;
        void Cast(FirstSeveranceLanceVolley? cast)
        {
            if (cast is not { } v) return;
            value = Math.Max(value, Kick(tick, v.FireTick, v.EndTick));
        }
        Cast(c.LanceVolley); Cast(c.CarriedLance);
        foreach (var cast in c.SpreadLances) Cast(cast);
        if (c.CoreCannon is { } cannon)
            value = Math.Max(value, Math.Max(8 * Charge(tick, cannon.StartTick, cannon.FireTick), Kick(tick, cannon.FireTick, cannon.EndTick)));
        if (c.GridVolley is { } grid && grid.CoreBeams.Count > 0)
            value = Math.Max(value, Math.Max(8 * Charge(tick, grid.StartTick, grid.FireTick), Kick(tick, grid.FireTick, grid.CoreEndTick)));
        var window = FirstSeveranceSafeWindows.At(c.Substate, c.ActionIndex, c.ActionStartedTick,
            (ulong)Math.Max(0, tick), c.CoreX, c.CoreY);
        if (window?.Kind == FirstSeveranceSafeMechanic.Spread || c.Substate == FirstSeveranceSubstate.Spread)
        {
            double start = window?.StartTick ?? c.ActionStartedTick, fire = window?.ResolveTick ?? c.ResolveTick;
            value = Math.Max(value, 10 * Charge(tick, Math.Max(start, fire - 100), fire));
        }
        double age = tick - c.ActionStartedTick;
        if (c.Substate == FirstSeveranceSubstate.RotatingBlade)
        {
            value = Math.Max(value, 8 * Charge(age, 0, FirstSeveranceChoreography.BladeWindup));
            value = Math.Max(value, Kick(age, FirstSeveranceChoreography.BladeWindup, FirstSeveranceChoreography.BladeEnd));
            // The second revolution uses the same non-linear travel as the rays.
            value = Math.Max(value, Kick(age, SecondTurn, FirstSeveranceChoreography.BladeEnd));
        }
        if (c.Substate == FirstSeveranceSubstate.HalfField)
            for (int wave = 0; wave < 2; wave++)
                value = Math.Max(value, Kick(age, FirstSeveranceImpalingSwords.FireBase(wave), FirstSeveranceImpalingSwords.FireBase(wave) + 66));
        if (c.Substate == FirstSeveranceSubstate.RemoteClaws)
            for (int wave = 0; wave < 3; wave++)
                value = Math.Max(value, Kick(age, wave * FirstSeveranceScoreGeometry.FloodInterval + FirstSeveranceScoreGeometry.FloodFireTick,
                    wave * FirstSeveranceScoreGeometry.FloodInterval + FirstSeveranceScoreGeometry.FloodEndTick));
        if (c.Substate == FirstSeveranceSubstate.FinalSlicer)
            for (int pulse = 0; pulse < FirstSeveranceScoreGeometry.SlicerPulses; pulse++)
            {
                int fire = FirstSeveranceScoreGeometry.SlicerFire(c.ActionIndex) + pulse * FirstSeveranceScoreGeometry.SlicerCadence(c.ActionIndex);
                value = Math.Max(value, Kick(age, fire, fire + FirstSeveranceLanceTuning.PatternActiveTicks));
            }
        return Math.Min(18, value);
    }
}
