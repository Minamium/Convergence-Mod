using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiSpriteArt
{
    internal const string AtlasPath = "Convergence/Assets/Textures/GhostSamurai/VioletActions";
    internal const string DeathPath = "Convergence/Assets/Textures/GhostSamurai/VioletDissolve";
    internal static Texture2D Atlas => Convergence.Client.Weapons.SpectralSpriteCutouts.Get(AtlasPath);
    internal static Texture2D Spirit => Convergence.Client.Weapons.OboroArt.Spirit;
    internal static void DrawSpirit(SpriteBatch batch, Vector2 screenPosition, float radius)
        => batch.Draw(Spirit, screenPosition - new Vector2(0, radius * .45f), null, Color.White,
            0, Spirit.Size() * .5f, radius * 4 / Spirit.Height, SpriteEffects.None, 0);
}
