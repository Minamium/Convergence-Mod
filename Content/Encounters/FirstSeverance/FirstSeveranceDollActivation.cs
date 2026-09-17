namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeveranceDollActivation
{
    internal const string RequiresDoll = "first_severance.activation_requires_doll";
    internal static bool CanActivate(bool active, bool dead, bool ghost, bool heldDoll)
        => active && !dead && !ghost && heldDoll;

    // Shared Preparing protection is not a Doll identity. Callers must prove
    // the exact Doll encounter/preparation/core, including on remote replicas.
    internal static bool ShowAttendant(FirstSeveranceCoreProtectionState state, bool ownsDollPreparation = false)
        => ownsDollPreparation && state == FirstSeveranceCoreProtectionState.Preparing;
}
