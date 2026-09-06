using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Networking.Protocol;

namespace Convergence.Common.Networking;

// Pure registry: one adapter per definition, common operation IDs may be reused by every definition.
internal sealed class EncounterPacketRoutes
{
    private readonly Dictionary<string, IEncounterPacketHandler> handlers = new(StringComparer.Ordinal);
    internal IEnumerable<IEncounterPacketHandler> Handlers => handlers.Values;

    internal void Register(string key, IEncounterPacketHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!EncounterRouteCodec.IsValidKey(key) || !handlers.TryAdd(key, handler))
            throw new InvalidOperationException($"Encounter packet route '{key}' is invalid or already registered.");
    }

    internal bool TryGet(string key, out IEncounterPacketHandler handler) => handlers.TryGetValue(key, out handler!);
    internal void Clear() => handlers.Clear();

    internal static bool MatchesSession(string key, in EncounterPacketHeader header, in EncounterSnapshot current)
        => !header.FightId.IsNone && header.EncounterSequence != 0
            && current.DefinitionKey == key && current.FightId == header.FightId
            && current.EncounterSequence == header.EncounterSequence;
}
