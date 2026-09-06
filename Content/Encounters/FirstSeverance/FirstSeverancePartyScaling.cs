using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// Development tuning, frozen from the accepted roster, never current survivors.
internal readonly record struct FirstSeverancePartyScaling(byte ParticipantCount, int BossLife, int PylonLife)
{
    internal static FirstSeverancePartyScaling ForCount(int count) => count switch
    {
        1 => new(1, 5_000_000, 300_000), // Debug admission reuses the two-player workload.
        2 => new(2, 5_000_000, 300_000),
        3 => new(3, 9_000_000, 500_000),
        4 => new(4, 13_000_000, 600_000),
        _ => throw new ArgumentOutOfRangeException(nameof(count)),
    };
}
