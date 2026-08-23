#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterRegistry
{
    private readonly Dictionary<string, EncounterDefinition> definitions = new(StringComparer.Ordinal);

    public IReadOnlyCollection<EncounterDefinition> Definitions => definitions.Values;

    public void Register(EncounterDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Key))
        {
            throw new ArgumentException("Encounter keys cannot be empty.", nameof(definition));
        }

        if (definition.MinimumParticipants < 1
            || definition.MaximumParticipants < definition.MinimumParticipants)
        {
            throw new ArgumentException(
                $"Encounter '{definition.Key}' has an invalid participant range.",
                nameof(definition));
        }

        ArenaProfile? defaultArena = definition.DefaultArena;
        if (defaultArena.HasValue && !defaultArena.Value.IsValid)
        {
            throw new ArgumentException(
                $"Encounter '{definition.Key}' has an invalid default arena.",
                nameof(definition));
        }

        if (!definitions.TryAdd(definition.Key, definition))
        {
            throw new InvalidOperationException($"Encounter key '{definition.Key}' is already registered.");
        }
    }

    public bool TryGet(string key, out EncounterDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            definition = null!;
            return false;
        }

        return definitions.TryGetValue(key, out definition!);
    }

    public void Clear()
    {
        definitions.Clear();
    }
}
