using Convergence.Common.Foundation.Geometry;

namespace Convergence.Common.Encounters.Abstractions;

// Basic authority policies accepted this request into the Validating lifecycle.
// RequestedAnchor is still untrusted until the feature resolves its server-side anchor.
internal readonly record struct AcceptedEncounterStart(
    int RequesterWhoAmI,
    TilePoint RequestedAnchor,
    uint RequestNonce);
