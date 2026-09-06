namespace Convergence.Content.Encounters.FirstSeverance.Development;

// Build-time admission only. Never a client request, public cheat, saved option,
// placeholder participant or change to the server's combat/recovery decisions.
internal static class FirstSeveranceDevelopmentPolicy
{
#if CONVERGENCE_DEVELOPMENT_SOLO
    internal const bool AllowSoloDebugStart = true;
#else
    internal const bool AllowSoloDebugStart = false;
#endif

    internal static int MinimumParticipants => MinimumFor(AllowSoloDebugStart);

    internal static int MinimumFor(bool allowSoloDebug)
        => allowSoloDebug ? FirstSeveranceRoster.MinimumCount : FirstSeveranceRoster.ProductionMinimumCount;

    // Both admission and the fresh Ready -> combat scan must use this policy.
    // Keep the general validator production-safe; don't silently default it to solo.
    internal static FirstSeveranceArenaValidationResult ValidateStartArena(FirstSeveranceArenaSurvey survey)
        => FirstSeveranceArenaValidator.Instance.Validate(survey,
            FirstSeveranceArenaValidationMode.DevelopmentContainment,
            allowSoloDebug: AllowSoloDebugStart);
}
