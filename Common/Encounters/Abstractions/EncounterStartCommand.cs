using Convergence.Common.Foundation.Geometry;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterStartCommand(
    int SenderWhoAmI,
    string DefinitionKey,
    TilePoint RequestedAnchor,
    uint RequestNonce);
