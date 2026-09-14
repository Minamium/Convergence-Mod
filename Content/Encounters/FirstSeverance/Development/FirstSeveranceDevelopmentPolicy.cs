namespace Convergence.Content.Encounters.FirstSeverance.Development;

// Historical internal type name retained. Solo admission is ordinary product
// behavior in GUI/native/MSBuild and public builds, not a development cheat.
// Membership and every Core/Ready check remain server-owned; no fake participant.
internal static class FirstSeveranceDevelopmentPolicy
{
    internal const bool AllowSoloStart = true;
    // Legacy diagnostic name, retained for existing offline package inspectors.
    internal const bool AllowSoloDebugStart = AllowSoloStart;

    internal static int MinimumParticipants => MinimumFor(AllowSoloStart);

    internal static int MinimumFor(bool allowSoloDebug)
        => allowSoloDebug ? FirstSeveranceRoster.MinimumCount : FirstSeveranceRoster.ProductionMinimumCount;

    // Both admission and the fresh Ready -> combat scan must use this policy.
    // General validation fixtures may still request a two-member minimum explicitly.
    internal static FirstSeveranceArenaValidationResult ValidateStartArena(FirstSeveranceArenaSurvey survey)
        => FirstSeveranceArenaValidator.Instance.Validate(survey,
            FirstSeveranceArenaValidationMode.DevelopmentContainment,
            allowSoloDebug: AllowSoloStart);
}
