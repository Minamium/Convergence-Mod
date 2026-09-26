using System;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

internal static class OboroArt
{
    internal static Texture2D Blade => ModContent.Request<Texture2D>("Convergence/Assets/Textures/Items/Oboro/Blade", AssetRequestMode.ImmediateLoad).Value;
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
    internal static void Sword(SpriteBatch b, Vector2 at, float angle, float length, Color tint, bool mirror = false)
        => b.Draw(Blade, at - Main.screenPosition, null, tint, angle + (mirror ? SourceAngle : -SourceAngle),
            mirror ? new Vector2(Grip.X, Blade.Height - Grip.Y) : Grip,
            length / SourceLength, mirror ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
    internal static void Afterimages(SpriteBatch b, OboroSwingPresentation history)
    {
        if (!history.Swinging) return;
        bool reduced = Reduced;
        // 小さい実体の刀だけを2枚残す。広い霊刃はManagedShaderが担当する。
        int shown = 0;
        for (int i = history.Count - 3; i >= 0 && shown < (reduced ? 1 : 2); i -= 3)
        {
            OboroEcho echo = history.Echo(i);
            float fade = OboroSwingPresentation.Opacity(echo.At, Main.GameUpdateCount, echo.Pose.Step);
            Sword(b, new(echo.Sword.X, echo.Sword.Y), echo.Sword.Angle, echo.Sword.Length,
                new Color(165, 101, 248) * (fade * .20f), echo.Sword.Facing < 0);
            shown++;
        }
    }
    internal static void Swing(SpriteBatch b, OboroBladePose pose, bool swinging)
    {
        if (!swinging) return;
        Vector2 center = new(pose.X, pose.Y);
        Sword(b, center, pose.Angle, pose.Length, Color.White, pose.Facing < 0);
        if (pose.Step == 2) { OboroFinisherArt.Draw(b, pose, Reduced); return; }
        int flames = Reduced ? 1 : 2;
        for (int i = 0; i < flames; i++)
        {
            float t = .28f + i * .18f, drift = (float)Main.GameUpdateCount * .08f + i * 2;
            Vector2 at = center + pose.Angle.ToRotationVector2() * (pose.Length * t)
                + (pose.Angle + MathF.PI / 2).ToRotationVector2() * (7 + MathF.Sin(drift) * 3);
            Flame(b, at, 12, .5f);
        }
    }
}
