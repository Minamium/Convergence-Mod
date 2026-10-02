#nullable enable
using System;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

internal enum CrimsonStrokeKind : byte { Dormant, Scheduled, Live, Residue }

// The shared ink-state codec (REWARDS.md, Scarlet Baton "State and replication"): one exact integer in a float ai slot.
//   Dormant  = 0
//   Scheduled = 1 + countdown (7 bits) | rank << 7 (3 bits) | cast << 10 (6 bits)  -> 1..65536
//   Live     = -1
//   Residue  = -2
// The owner sets a schedule once, at the cast; every client then counts down on its own. Anything else
// (fractions, NaN, out of range) is invalid and the projectile kills itself.
internal readonly record struct CrimsonStrokeState(CrimsonStrokeKind Kind, int Countdown = 0, int Rank = 0, int Cast = 0)
{
    internal const int MaxCountdown = 127, MaxRank = 7, MaxCast = 63;
    private const int ScheduledBase = 1, ScheduledTop = ScheduledBase + (MaxCountdown | MaxRank << 7 | MaxCast << 10);

    internal static CrimsonStrokeState Dormant => new(CrimsonStrokeKind.Dormant);
    internal static CrimsonStrokeState Live => new(CrimsonStrokeKind.Live);
    internal static CrimsonStrokeState Residue => new(CrimsonStrokeKind.Residue);
    internal static CrimsonStrokeState Scheduled(int countdown, int rank, int cast)
    {
        if (countdown is < 0 or > MaxCountdown || rank is < 0 or > MaxRank || cast is < 0 or > MaxCast)
            throw new ArgumentOutOfRangeException();
        return new(CrimsonStrokeKind.Scheduled, countdown, rank, cast);
    }

    internal float Encode() => Kind switch
    {
        CrimsonStrokeKind.Dormant => 0,
        CrimsonStrokeKind.Live => -1,
        CrimsonStrokeKind.Residue => -2,
        CrimsonStrokeKind.Scheduled => ScheduledBase + (Countdown | Rank << 7 | Cast << 10),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };

    internal static bool TryDecode(float value, out CrimsonStrokeState state)
    {
        state = Dormant;
        if (!CrimsonRewardRules.ValidInteger(value, -2, ScheduledTop)) return false;
        int v = (int)value;
        if (v == -2) { state = Residue; return true; }
        if (v == -1) { state = Live; return true; }
        if (v == 0) return true;
        int packed = v - ScheduledBase;
        state = new(CrimsonStrokeKind.Scheduled, packed & MaxCountdown, packed >> 7 & MaxRank, packed >> 10 & MaxCast);
        return true;
    }

    // One tick of a scheduled stroke's local countdown; reaching zero ignites it.
    internal CrimsonStrokeState Tick()
        => Kind != CrimsonStrokeKind.Scheduled ? this : Countdown <= 1 ? Live : this with { Countdown = Countdown - 1 };
}
