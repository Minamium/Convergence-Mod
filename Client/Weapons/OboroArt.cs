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
        bool reduced = Reduced;
        // Recorded world-space poses persist across recovery and combo handoffs.
        // Never connect different cuts; echoes do not have any collision or damage.
        for (int i = 1; i < history.Count; i++)
        {
            OboroEcho previous = history.Echo(i - 1), current = history.Echo(i);
            if (previous.Swing != current.Swing || current.At - previous.At > 2) continue;
            float fade = OboroSwingPresentation.Opacity(previous.At, Main.GameUpdateCount, previous.Pose.Step);
            float turn = current.Pose.Angle - previous.Pose.Angle;
            int segments = Math.Clamp((int)MathF.Ceiling(Math.Abs(turn) / (reduced ? .16f : .08f)), 1, 32);
            for (int j = 0; j < segments; j++)
            {
                float a = j / (float)segments, c = (j + 1f) / segments;
                Vector2 from = Vector2.Lerp(new(previous.Pose.X, previous.Pose.Y), new(current.Pose.X, current.Pose.Y), a);
                Vector2 to = Vector2.Lerp(new(previous.Pose.X, previous.Pose.Y), new(current.Pose.X, current.Pose.Y), c);
                Vector2 axisA = (previous.Pose.Angle + turn * a).ToRotationVector2();
                Vector2 axisB = (previous.Pose.Angle + turn * c).ToRotationVector2();
                float strength = current.Pose.Step switch { 1 => .52f, 2 => 1.3f, _ => .72f };
                // Thin moonlit edge over a soft violet ribbon; no filled screen-wide fan.
                for (int layer = 0; layer < (reduced ? 1 : 3); layer++)
                {
                    float radius = OboroRules.Reach * (1 - layer * .045f);
                    Color tint = layer == 0 ? new Color(228, 199, 255, 0) : new Color(135, 57, 238, 0);
                    Line(b, from + axisA * radius, to + axisB * radius,
                        (layer == 0 ? 3 : 12 + layer * 3) * strength, tint * (fade * strength * (layer == 0 ? .65f : .28f)));
                }
            }
        }
        int stride = reduced ? 6 : 3;
        for (int i = history.Count - 2; i >= 0; i -= stride)
        {
            OboroEcho echo = history.Echo(i);
            float fade = OboroSwingPresentation.Opacity(echo.At, Main.GameUpdateCount, echo.Pose.Step);
            Sword(b, new(echo.Pose.X, echo.Pose.Y), echo.Pose.Angle, echo.Pose.Length,
                new Color(165, 101, 248) * (fade * .23f), echo.Pose.Facing < 0);
            if (!reduced)
                Flame(b, new Vector2(echo.Pose.X, echo.Pose.Y) + echo.Pose.Angle.ToRotationVector2() * (OboroRules.Reach * .76f),
                    18 + (1 - fade) * 22, fade * .16f);
        }
    }
    internal static void Swing(SpriteBatch b, OboroBladePose pose, bool swinging)
    {
        Vector2 center = new(pose.X, pose.Y);
        Sword(b, center, pose.Angle, pose.Length, Color.White, pose.Facing < 0);
        if (!swinging) return;
        if (pose.Step == 2) { OboroFinisherArt.Draw(b, pose, Reduced); return; }
        int flames = Reduced ? 2 : 4;
        for (int i = 0; i < flames; i++)
        {
            float t = .28f + i * .18f, drift = (float)Main.GameUpdateCount * .08f + i * 2;
            Vector2 at = center + pose.Angle.ToRotationVector2() * (pose.Length * t)
                + (pose.Angle + MathF.PI / 2).ToRotationVector2() * (20 + MathF.Sin(drift) * 12);
            Flame(b, at, 24, .7f);
        }
    }
}
