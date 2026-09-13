namespace Convergence.Client.Encounters.FirstSeverance;

internal static class RitualPresentationStep
{
    // Projectile.Update decrements the remaining-update counter BEFORE AI.
    // With no extraUpdates its only PostAI has numUpdates == -1, never zero.
    internal static bool IsFinal(int remainingUpdates) => remainingUpdates == -1;
}
