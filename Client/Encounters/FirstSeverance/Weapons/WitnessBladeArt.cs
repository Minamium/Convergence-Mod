#nullable enable
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// The one blade rung Last Witness draws, hanging and thrown: WitnessBlade (k = 2, 130 x 24 px). The shared rules fit
// the art to the hit shapes, so the spinning blade keeps this rung and the 56 px hit disc covers nearly all of the
// blade the player sees turning (WitnessBlade_L, k = 1, stays exported but is not drawn: its tip would reach 125 px).
// Pure (System.Numerics and the exported anchors): the presentation and the offline preview draw from it and the
// art-fit domain test checks it.
internal static class WitnessBladeArt
{
    internal const string Name = "WitnessBlade";
    internal const int K = DollArtAnchors.WitnessBlade.K;
    internal static readonly Vector2 Pivot = DollArtAnchors.WitnessBlade.Pivot;
    // Drawn anchors in the blade frame, world px from the balance point (+x toward the tip, +y toward the edge).
    internal static readonly Vector2 Tip = Local(DollArtAnchors.WitnessBlade.Tip);
    internal static readonly Vector2 Eye = Local(DollArtAnchors.WitnessBlade.Eye);
    // How far the drawn tip reaches from the balance point while the blade spins.
    internal static readonly float TipReach = Tip.Length();
    // The trailing spin arc rides the hit disc's rim, so its light shows exactly how far the blade bites; the shared
    // band for it is 0.8 to 1.0 of the drawn tip's reach.
    internal const float ArcRadius = WitnessRules.SpinRadius;

    internal static Vector2 Local(Vector2 texel) => (texel - Pivot) * DollSpritePlacement.WorldPerTexel;

    internal static bool ArcInBand(float radius, float tipReach)
        => float.IsFinite(radius) && float.IsFinite(tipReach) && radius >= .8f * tipReach && radius <= tipReach;
}
