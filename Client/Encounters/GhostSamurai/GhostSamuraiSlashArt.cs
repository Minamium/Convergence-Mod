using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

internal enum SamuraiSlashArt { Normal, Heavy, Grid, Wind, Dash }

// Read-only decoration, sampled from the same accepted hazard clock as its outline.
// No particles/projectiles or persistent fight state. tML owns the five assets.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiSlashArt : ModSystem
{
    private static readonly string[] Names = { "NormalSlash", "HeavySlash", "GridSlash", "Kamaitachi", "DashFlash" };
    private static readonly Asset<Texture2D>[] Textures = new Asset<Texture2D>[5];

    public override void Unload() => Array.Clear(Textures);

    // Keep a visible material through the live window; only its initial energy
    // peak decays. The independent exact-volume outline never fades with it.
    internal static float Energy(float age, int fire, int end)
    {
        if (!float.IsFinite(age) || age < fire || age >= end || end <= fire) return 0;
        float t = (age - fire) / (end - fire);
        return .42f + .58f * (1 - t) * (1 - t);
    }

    internal static void Strip(SpriteBatch batch, SamuraiSlashArt art, Vector2 a, Vector2 b,
        float width, float opacity, Rectangle? clip = null, float u0 = 0, float u1 = 1)
    {
        if (Main.dedServ || opacity <= 0 || !float.IsFinite(opacity) || !float.IsFinite(width) || width <= 0) return;
        Vector2 delta = b - a;
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y) || delta.LengthSquared() < .01f) return;
        // Clip the entire ribbon, not just its spine; preserve UVs when an edge
        // leaves the field so the image cannot slide/stretch as the camera moves.
        if (clip is Rectangle view)
        {
            float enter = 0, exit = 1, inset = width * .5f + 1;
            if (!Axis(a.X, delta.X, view.Left + inset, view.Right - inset, ref enter, ref exit)
                || !Axis(a.Y, delta.Y, view.Top + inset, view.Bottom - inset, ref enter, ref exit)) return;
            b = a + delta * exit; a += delta * enter;
            float span = u1 - u0;
            u1 = u0 + span * exit; u0 += span * enter;
            delta = b - a;
        }
        if (delta.LengthSquared() < .01f) return;
        int index = (int)art;
        // ImmediateLoad avoids the transparent pending-texture placeholder which
        // previously made the boss disappear. Only first draw requests the asset.
        Texture2D texture = (Textures[index] ??= ModContent.Request<Texture2D>(
            "Convergence/Assets/Textures/GhostSamurai/Slashes/" + Names[index], AssetRequestMode.ImmediateLoad)).Value;
        int left = Math.Clamp((int)(u0 * texture.Width), 0, texture.Width - 1);
        int right = Math.Clamp((int)MathF.Ceiling(u1 * texture.Width), left + 1, texture.Width);
        Rectangle source = new(left, 0, right - left, texture.Height);
        batch.Draw(texture, a, source, Color.White * Math.Clamp(opacity, 0, 1),
            MathF.Atan2(delta.Y, delta.X), new Vector2(0, source.Height * .5f),
            new Vector2(delta.Length() / source.Width, width / source.Height), SpriteEffects.None, 0);
    }

    internal static void Arc(SpriteBatch batch, SamuraiSlashArt art, Vector2 center, float radius,
        float angle, float sweep, float width, float opacity, Rectangle clip, int facing = 1)
    {
        const int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float u0 = i / (float)segments, u1 = (i + 1) / (float)segments;
            float a = angle + sweep * u0, b = angle + sweep * u1;
            Vector2 first = center + new Vector2(facing * MathF.Cos(a), MathF.Sin(a)) * radius;
            Vector2 last = center + new Vector2(facing * MathF.Cos(b), MathF.Sin(b)) * radius;
            Strip(batch, art, first, last, width, opacity, clip, u0, u1);
        }
    }

    private static bool Axis(float origin, float delta, float low, float high, ref float enter, ref float exit)
    {
        if (low >= high) return false;
        if (Math.Abs(delta) < .00001f) return origin >= low && origin <= high;
        float first = (low - origin) / delta, last = (high - origin) / delta;
        if (first > last) (first, last) = (last, first);
        enter = Math.Max(enter, first); exit = Math.Min(exit, last);
        return enter < exit;
    }
}
