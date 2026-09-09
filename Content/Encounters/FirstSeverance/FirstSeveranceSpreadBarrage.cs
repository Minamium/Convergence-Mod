using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// Same pursuit casts as P1, overlapped rather than shortening their warning.
// Reserve a quiet final 0.4 s for the actual Spread check in every window.
internal static class FirstSeveranceSpreadBarrage
{
    internal const int Count = 8, Opening = 6, Settle = 24;
    internal const int CastTicks = FirstSeveranceLanceTuning.PrismTelegraphTicks + FirstSeveranceLanceTuning.PatternActiveTicks;

    internal static FirstSeveranceSafeWindow? Window(FirstSeveranceSubstate state, int action,
        ulong started, ulong resolve, ulong tick)
    {
        if (tick < started || tick >= resolve) return null;
        if (state == FirstSeveranceSubstate.Spread)
            return new(FirstSeveranceSafeMechanic.Spread, started, resolve, 0, 0);
        var embedded = FirstSeveranceSafeWindows.At(state, action, started, tick, 0, 0);
        return embedded is { Kind: FirstSeveranceSafeMechanic.Spread } w && tick < w.ResolveTick ? w : null;
    }

    internal static ulong Start(in FirstSeveranceSafeWindow window, int step)
    {
        if (step is < 0 or >= Count || window.ResolveTick <= window.StartTick
            || window.ResolveTick - window.StartTick < Opening + CastTicks + Settle + (Count - 1) * 9)
            throw new ArgumentException("Spread window cannot fit eight fair pursuit warnings.");
        ulong span = window.ResolveTick - window.StartTick - Opening - CastTicks - Settle;
        return window.StartTick + Opening + span * (ulong)step / (Count - 1);
    }
}
