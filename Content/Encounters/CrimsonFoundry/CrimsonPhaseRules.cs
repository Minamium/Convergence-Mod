using System;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Pure authority rules; a retreat is not a kill and never restores lost HP.
internal static class CrimsonPhaseRules
{
    internal const int FinalPhase = 3;
    internal const int TransitionTicks = 150;
    internal static int RetreatLife(int maximumLife)
        => maximumLife > 0 ? (maximumLife + 4) / 5 : throw new ArgumentOutOfRangeException(nameof(maximumLife));
    internal static bool ShouldRetreat(int phase, int index, int life, int maximumLife)
        => phase is >= 0 and < FinalPhase && index == phase && life <= RetreatLife(maximumLife);
    internal static bool ActiveSource(int phase, byte defeated, bool performerDefeated, int source)
        => phase is >= 0 and <= FinalPhase && source is >= 0 and <= FinalPhase
            && (phase == FinalPhase ? source == FinalPhase && !performerDefeated
                : source == phase && (defeated & (1 << source)) == 0);
    internal static bool Victory(int phase, byte defeated, bool performerDefeated, bool allPlayersOut)
        => !allPlayersOut && phase == FinalPhase && defeated == CrimsonInvocation.AllDefeated && performerDefeated;
    internal static int BarMaximum(int phase, int targetLife)
        => phase == FinalPhase ? targetLife + 3 * RetreatLife(targetLife) : targetLife;
}
