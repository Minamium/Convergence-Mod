using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.Client.Encounters.FirstSeverance;

// Pure authority-clock presentation policy. Never changes the user's volume.
internal static class FirstSeveranceMusicTimeline
{
    internal const int LeadTicks = 180, FadeTicks = 90;
    internal static FirstSeveranceBossPhase Previous(FirstSeveranceBossPhase phase) => phase switch
    {
        FirstSeveranceBossPhase.Final => FirstSeveranceBossPhase.Distant,
        FirstSeveranceBossPhase.Distant => FirstSeveranceBossPhase.Unbound,
        FirstSeveranceBossPhase.Unbound => FirstSeveranceBossPhase.Sealed,
        _ => phase,
    };
    internal static double Handoff(ulong start, ulong end) => Math.Max(start, (double)end - LeadTicks);
    internal static FirstSeveranceBossPhase Select(FirstSeveranceBossPhase phase, bool transition,
        ulong start, ulong end, ulong tick) => transition && tick < Handoff(start, end) ? Previous(phase) : phase;
    internal static float OutgoingCeiling(ulong start, ulong end, ulong tick)
        => 1 - Math.Clamp((float)((tick - Handoff(start, end)) / FadeTicks), 0, 1);
}
