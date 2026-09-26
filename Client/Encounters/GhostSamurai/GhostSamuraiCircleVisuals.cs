using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiCircleVisuals
{
    // Analytic disk/annulus masking replaces strip-filled rectangles. Swirling
    // cut filaments never enter the safe hole; reduced mode keeps the same edge.
    internal static void Draw(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, Rectangle viewport, bool reduced)
        => GhostSamuraiEnergy.Field(batch, h, center, age, viewport, reduced);
}
