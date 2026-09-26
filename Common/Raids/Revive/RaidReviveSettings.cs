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
    float RestoredLifeRatio,
    bool UsesSharedTokens = true,
    ulong ReviveLockoutTicks = 0)
{
    public const int MinimumParticipantCount = 1;
    public const int MaximumParticipantCount = 8;
    public const int InitialMaximumParticipantCount = 4;

    public bool IsValid => ParticipantCount is >= MinimumParticipantCount and <= MaximumParticipantCount
        && (!UsesSharedTokens || ParticipantCount <= InitialMaximumParticipantCount)
        && InitialTokenCount == (UsesSharedTokens ? ParticipantCount - 1 : 0)
        && (DownedTimeoutTicks == 0 || DownedTimeoutTicks > ChannelDurationTicks)
        && DisconnectGraceTicks > 0
        && InvulnerabilityTicks > 0
        && float.IsFinite(RestoredLifeRatio)
        && RestoredLifeRatio is > 0f and <= 1f;

    public static RaidReviveSettings CreateInitial(int participantCount)
    {
        if (participantCount is < 2 or > InitialMaximumParticipantCount)
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

    public static RaidReviveSettings CreateInstantUnlimited(int participantCount)
    {
        if (participantCount is < MinimumParticipantCount or > MaximumParticipantCount)
            throw new ArgumentOutOfRangeException(nameof(participantCount));
        // The generic service needs no solo switch: one Downed member already
        // means all participants are Downed at the ordinary authority commit.
        return CreateInitial(2) with
        {
            ParticipantCount = participantCount,
            InitialTokenCount = 0,
            UsesSharedTokens = false,
            ChannelDurationTicks = 0,
            DownedTimeoutTicks = 0, // No elimination timer; an ally can outlast recipient lockout.
            WeaknessTicks = 0,
            ReviveLockoutTicks = 3_600,
        };
    }
}
