namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeveranceDollActivation
{
    internal const string RequiresDoll = "first_severance.activation_requires_doll";
    internal static bool CanActivate(bool active, bool dead, bool ghost, bool heldDoll)
        => active && !dead && !ghost && heldDoll;

    // No persistent 'installed doll' flag: cancel/terminal/world reload returns
    // to the empty pedestal. The reused native NPC is never a roster member.
    internal static bool ShowAttendant(FirstSeveranceCoreProtectionState state)
        => state == FirstSeveranceCoreProtectionState.Preparing;
}
