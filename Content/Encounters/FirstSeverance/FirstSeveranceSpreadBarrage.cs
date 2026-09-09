using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// Standalone Spread only: do not layer pursuit over lattice/flood mechanics.
// Reserve a quiet final 0.4 s for the actual Spread check in every window.
internal static class FirstSeveranceSpreadBarrage
{
    internal const int Count = 4, Opening = 6, Settle = 24;
    internal const int CastTicks = FirstSeveranceLanceTuning.PrismTelegraphTicks + FirstSeveranceLanceTuning.PatternActiveTicks;

    internal static FirstSeveranceSafeWindow? Window(FirstSeveranceSubstate state, int action,
        ulong started, ulong resolve, ulong tick)
    {
        if (tick < started || tick >= resolve) return null;
        if (state == FirstSeveranceSubstate.Spread)
            return new(FirstSeveranceSafeMechanic.Spread, started, resolve, 0, 0);
        return null;
    }

    internal static int ShotCount(in FirstSeveranceSafeWindow window)
        => window.ResolveTick > window.StartTick && window.ResolveTick - window.StartTick >= 180 ? 4 : 3;

    internal static ulong Start(in FirstSeveranceSafeWindow window, int step)
    {
        int count = ShotCount(window);
        if (step < 0 || step >= count || window.ResolveTick <= window.StartTick
            || window.ResolveTick - window.StartTick < (ulong)(Opening + CastTicks + Settle + (count - 1) * 34))
            throw new ArgumentException("Spread window cannot fit the spaced pursuit warnings.");
        ulong span = window.ResolveTick - window.StartTick - Opening - CastTicks - Settle;
        return window.StartTick + Opening + span * (ulong)step / (ulong)(count - 1);
    }
}
