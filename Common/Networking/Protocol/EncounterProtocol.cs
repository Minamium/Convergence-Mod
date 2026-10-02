namespace Convergence.Common.Networking.Protocol;

internal static class EncounterProtocol
{
    // Scarlet signature moves append technique IDs 17-19 (protocol 78 is the music-grid branch); matching peers only.
    public const ushort CurrentVersion = 79;
}
