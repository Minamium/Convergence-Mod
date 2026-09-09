using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;

namespace Convergence.Content.Encounters.FirstSeverance;

// Per-owner, per-Fight receipt, separate from HP corrections (heals can coalesce
// with hits). Reliable ordered delivery precedes a terminal snapshot as well.
internal struct FirstSeveranceHitReceipt
{
    private uint revision;

    internal bool TryAccept(in EncounterPacketHeader header, FightId currentFight,
        ulong currentSequence, int damage)
    {
        if (header.ProtocolVersion != EncounterProtocol.CurrentVersion
            || header.PacketType != EncounterPacketType.RaidHit || currentFight.IsNone
            || header.FightId != currentFight || currentSequence == 0
            || header.EncounterSequence != currentSequence || damage <= 0
            || header.Revision <= revision) return false;
        revision = header.Revision;
        return true;
    }
}
