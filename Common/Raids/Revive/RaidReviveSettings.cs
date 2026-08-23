using System;

namespace Convergence.Common.Raids.Revive;

internal readonly record struct RaidReviveSettings(
    int ParticipantCount,
    int InitialTokenCount,
    ulong ChannelDurationTicks,
    ulong DownedTimeoutTicks,
    ulong DisconnectGraceTicks,
    ulong InvulnerabilityTicks,
    ulong WeaknessTicks,
    float RestoredLifeRatio)
{
    public const int MinimumParticipantCount = 2;
    public const int MaximumParticipantCount = 4;

    public bool IsValid => ParticipantCount is >= MinimumParticipantCount and <= MaximumParticipantCount
        && InitialTokenCount == ParticipantCount - 1
        && ChannelDurationTicks > 0
        && DownedTimeoutTicks > ChannelDurationTicks
        && DisconnectGraceTicks > 0
        && InvulnerabilityTicks > 0
        && WeaknessTicks > 0
        && float.IsFinite(RestoredLifeRatio)
        && RestoredLifeRatio is > 0f and <= 1f;

    public static RaidReviveSettings CreateInitial(int participantCount)
    {
        if (participantCount is < MinimumParticipantCount or > MaximumParticipantCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(participantCount),
                "Initial Raid revive rules support two to four participants.");
        }

        return new RaidReviveSettings(
            participantCount,
            participantCount - 1,
            ChannelDurationTicks: 120,
            DownedTimeoutTicks: 1_800,
            DisconnectGraceTicks: 1_800,
            InvulnerabilityTicks: 180,
            WeaknessTicks: 600,
            RestoredLifeRatio: 0.35f);
    }
}
