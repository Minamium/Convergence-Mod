using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Networking.Protocol;

internal readonly record struct EncounterPacketHeader(
    ushort ProtocolVersion,
    EncounterPacketType PacketType,
    ulong EncounterSequence,
    FightId FightId,
    uint Revision);
