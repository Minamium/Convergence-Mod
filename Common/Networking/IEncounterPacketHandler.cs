#nullable enable

using System.IO;
using Convergence.Common.Networking.Protocol;

namespace Convergence.Common.Networking;

internal interface IEncounterPacketHandler
{
    bool TryHandle(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode);
}
