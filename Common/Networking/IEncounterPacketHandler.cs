#nullable enable

using System.IO;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Networking.Protocol;

namespace Convergence.Common.Networking;

internal interface IEncounterPacketHandler
{
    void PublishSnapshot(in EncounterSnapshot snapshot, int toClient);
    void ApplyIdleSnapshot(in EncounterSnapshot snapshot);

    bool TryHandle(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode);
}
