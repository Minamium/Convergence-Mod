using System;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

internal static class OboroArt
{
    internal static Texture2D Blade => ModContent.Request<Texture2D>("Convergence/Assets/Textures/Items/Oboro/Blade").Value;
    internal static Texture2D Spirit => SpectralSpriteCutouts.Get("Convergence/Assets/Textures/Items/Oboro/Spirit");
    internal static bool Reduced => ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
    private static readonly Vector2 Grip = new(254, 1059);
    private const float SourceLength = 1230, SourceAngle = -.78f;
    internal static void Line(SpriteBatch b, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 d = end - start;
        b.Draw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, d.ToRotation(),
            new(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    internal static void Flame(SpriteBatch b, Vector2 at, float size, float opacity = 1)
        => b.Draw(Spirit, at - Main.screenPosition, null, Color.White * opacity, 0,
            Spirit.Size() * .5f, size / Spirit.Height, SpriteEffects.None, 0);
    internal static void Sword(SpriteBatch b, Vector2 at, float angle, float length, Color tint)
        => b.Draw(Blade, at - Main.screenPosition, null, tint, angle - SourceAngle, Grip,
            length / SourceLength, SpriteEffects.None, 0);
    internal static void Swing(SpriteBatch b, Player player, OboroPlayer state)
    {
        float p = state.VisualAge / state.View.Duration;
        float angle = state.View.Aim + state.View.Facing * OboroRules.Offset(state.View.Step, p);
        Vector2 center = player.MountedCenter + new Vector2(0, player.gfxOffY);
        if (OboroRules.Live(state.View.Step, p))
        {
            int segments = Reduced ? 9 : 18;
            float tail = Math.Max(OboroRules.Windup(state.View.Step), p - .16f);
            for (int i = 1; i <= segments; i++)
            {
                float a = state.View.Aim + state.View.Facing * OboroRules.Offset(state.View.Step, MathHelper.Lerp(tail, p, (i - 1f) / segments));
                float c = state.View.Aim + state.View.Facing * OboroRules.Offset(state.View.Step, MathHelper.Lerp(tail, p, i / (float)segments));
                float alpha = i / (float)segments;
                for (int layer = 0; layer < (Reduced ? 1 : 3); layer++)
                    Line(b, center + a.ToRotationVector2() * (OboroRules.Reach * (.82f + layer * .07f)),
                        center + c.ToRotationVector2() * (OboroRules.Reach * (.82f + layer * .07f)),
                        state.View.Step == 2 ? 17 - layer * 4 : 10 - layer * 2, new Color(144, 74, 245, 0) * (alpha * .55f));
            }
        }
        Sword(b, center, angle, OboroRules.Reach, Color.White);
        int flames = Reduced ? 2 : 4;
        for (int i = 0; i < flames; i++)
        {
            float t = .28f + i * .18f, drift = (float)Main.GameUpdateCount * .08f + i * 2;
            Vector2 at = center + angle.ToRotationVector2() * (OboroRules.Reach * t)
                + (angle + MathF.PI / 2).ToRotationVector2() * (20 + MathF.Sin(drift) * 12);
            Flame(b, at, state.View.Step == 2 && p < .48f ? 38 : 24, .85f);
        }
    }
}
