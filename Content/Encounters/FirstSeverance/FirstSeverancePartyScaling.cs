using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// Development tuning, frozen from the accepted roster, never current survivors.
internal readonly record struct FirstSeverancePartyScaling(byte ParticipantCount, int BossLife, int PylonLife)
{
    internal static FirstSeverancePartyScaling ForCount(int count) => count switch
    {
        1 => new(1, 10_000_000, 300_000), // Ordinary solo shares the two-player workload.
        2 => new(2, 10_000_000, 300_000),
        3 => new(3, 18_000_000, 500_000),
        4 => new(4, 26_000_000, 600_000),
        _ => throw new ArgumentOutOfRangeException(nameof(count)),
    };
}
