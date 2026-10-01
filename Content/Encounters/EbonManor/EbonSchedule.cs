using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.EbonManor;

internal enum EbonCue : byte
{
    Throw, Chandelier, LoomRising, LoomFalling, ShearsAcross, ShearsDown, Waltz, Stack, Spread, Web,
}

// The 16-bar attack cycle of each phase, written as (bar, beat, cue) on
// AutoMatador's beat grid. The runtime resolves targets and geometry; this
// table only says what the choreography calls on which beat.
internal static class EbonSchedule
{
    private static readonly (int Bar, int Beat, EbonCue Cue)[] ActOne =
    {
        (0, 0, EbonCue.Throw), (0, 1, EbonCue.Throw), (0, 2, EbonCue.Throw), (0, 3, EbonCue.Throw),
        (2, 0, EbonCue.LoomRising),
        (4, 0, EbonCue.Chandelier), (4, 2, EbonCue.Chandelier), (5, 0, EbonCue.Chandelier),
        (6, 0, EbonCue.Throw), (6, 1, EbonCue.Throw), (6, 2, EbonCue.Throw), (6, 3, EbonCue.Throw),
        (8, 0, EbonCue.LoomFalling), (9, 0, EbonCue.Throw), (9, 2, EbonCue.Throw),
        (10, 0, EbonCue.Chandelier), (10, 2, EbonCue.Chandelier), (11, 0, EbonCue.Chandelier),
        (12, 0, EbonCue.Throw), (12, 1, EbonCue.Throw), (12, 2, EbonCue.Throw), (12, 3, EbonCue.Throw),
        (13, 0, EbonCue.Throw), (13, 1, EbonCue.Throw),
    };
    private static readonly (int Bar, int Beat, EbonCue Cue)[] ActTwo =
    {
        (0, 0, EbonCue.Waltz), (3, 2, EbonCue.Throw), (3, 3, EbonCue.Throw),
        (4, 0, EbonCue.Stack), (4, 2, EbonCue.Chandelier), (5, 2, EbonCue.Chandelier),
        (6, 0, EbonCue.Web),
        (8, 0, EbonCue.ShearsAcross), (9, 0, EbonCue.ShearsDown),
        (10, 0, EbonCue.LoomRising), (11, 0, EbonCue.LoomFalling),
        (12, 0, EbonCue.Throw), (12, 1, EbonCue.Throw), (12, 2, EbonCue.Throw), (12, 3, EbonCue.Throw),
        (13, 0, EbonCue.Throw), (13, 1, EbonCue.Throw),
    };
    private static readonly (int Bar, int Beat, EbonCue Cue)[] Finale =
    {
        (0, 0, EbonCue.Throw), (0, 1, EbonCue.Throw), (0, 2, EbonCue.Throw), (0, 3, EbonCue.Throw),
        (1, 0, EbonCue.Throw), (1, 1, EbonCue.Throw), (1, 2, EbonCue.Throw), (1, 3, EbonCue.Throw),
        (2, 0, EbonCue.LoomRising), (2, 2, EbonCue.Chandelier), (3, 0, EbonCue.Chandelier),
        (4, 0, EbonCue.ShearsAcross), (4, 2, EbonCue.ShearsDown),
        (5, 0, EbonCue.Spread),
        (6, 0, EbonCue.Web),
        (8, 0, EbonCue.Waltz),
        (12, 0, EbonCue.LoomFalling), (12, 2, EbonCue.Throw), (12, 3, EbonCue.Throw), (13, 0, EbonCue.Throw), (13, 1, EbonCue.Throw),
        (14, 0, EbonCue.Chandelier), (14, 1, EbonCue.Chandelier), (14, 2, EbonCue.Chandelier), (14, 3, EbonCue.Chandelier),
    };

    internal static IReadOnlyList<(int Bar, int Beat, EbonCue Cue)> Table(EbonPhase phase) => phase switch
    {
        EbonPhase.ActOne => ActOne,
        EbonPhase.ActTwo => ActTwo,
        _ => Finale,
    };

    // Every cue on this beat of the phase (beat counted from the phase epoch).
    internal static IEnumerable<EbonCue> At(EbonPhase phase, int beat)
    {
        if (beat < 0) yield break;
        int bar = beat / 4 % 16, within = beat % 4;
        foreach (var entry in Table(phase))
            if (entry.Bar == bar && entry.Beat == within) yield return entry.Cue;
    }

    // Stitch calls alternate Stack/Spread by cycle so each phase shows both.
    internal static EbonCue Stitch(EbonCue cue, int beat)
        => cue is EbonCue.Stack or EbonCue.Spread && beat / EbonRules.CycleBeats % 2 == 1
            ? cue == EbonCue.Stack ? EbonCue.Spread : EbonCue.Stack : cue;

    internal static int Cycle(int beat) => Math.Max(0, beat) / EbonRules.CycleBeats;
}
