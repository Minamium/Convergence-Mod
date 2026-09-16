using System;
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
    internal static void Draw(SpriteBatch batch, GhostSamuraiBoss boss, Vector2 root, float pose)
    {
        var texture = Atlas;
        int frame = boss.TransitionRemaining > 0 ? 11 : SamuraiSpriteFrames.Select(boss.Attack, boss.Beat,
            boss.VisualAttackTimer, boss.VisualAge, pose);
        var c = SamuraiSpriteFrames.Cell(frame, texture.Width, texture.Height);
        var source = new Rectangle(c.X, c.Y, c.Width, c.Height);
        Vector2 pivot = new(c.Width * .5f, c.Height * .53f);
        // The right-facing dash leans forward within its cell; keep its chest on
        // the authority actor instead of shifting its hitbox to match the picture.
        if (frame == 10) pivot = new(c.Width * .73f, c.Height * .57f);
        float scale = 390f / c.Height;
        bool flip = boss.NPC.direction < 0;
        if (flip) pivot.X = c.Width - pivot.X;
        Color tint = Color.White * SamuraiSpriteFrames.BodyOpacity(boss.VisualAge);
        if (boss.NPC.justHit) tint = Color.Lerp(tint, new Color(241, 222, 255), .35f);
        // Modest follow-through keeps fixed key poses connected without changing
        // the telegraph clocks or moving the center used by the damage rules.
        float lean = frame is 5 or 7 or 9 ? boss.NPC.direction * pose * .04f : 0;
        batch.Draw(texture, root, source, tint, lean, pivot, scale,
            flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
    }
    internal static void DrawSpirit(SpriteBatch batch, Vector2 screenPosition, float radius)
        => batch.Draw(Spirit, screenPosition - new Vector2(0, radius * .45f), null, Color.White,
            0, Spirit.Size() * .5f, radius * 4 / Spirit.Height, SpriteEffects.None, 0);
}

[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiDissolve : ModSystem
{
    private static readonly System.Collections.Generic.List<(Guid Fight, Vector2 Center, ulong At)> deaths = new();
    internal static void Add(GhostSamuraiBoss boss)
    {
        if (Main.dedServ || deaths.Exists(x => x.Fight == boss.Fight)) return;
        if (deaths.Count >= 8) deaths.RemoveAt(0);
        deaths.Add((boss.Fight, boss.NPC.Center, Main.GameUpdateCount));
    }
    public override void ClearWorld() => deaths.Clear();
    public override void OnWorldUnload() => deaths.Clear();
    public override void Unload() => deaths.Clear();
    public override void PostUpdateEverything() => deaths.RemoveAll(x => Main.GameUpdateCount - x.At >= 60);
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || deaths.Count == 0) return;
        var texture = Convergence.Client.Weapons.SpectralSpriteCutouts.Get(GhostSamuraiSpriteArt.DeathPath);
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (var death in deaths)
            {
                float age = Main.GameUpdateCount - death.At;
                int frame = Math.Clamp((int)age / 6, 0, 9), col = frame % 5, row = frame / 5;
                int x = col * texture.Width / 5, y = row * texture.Height / 2;
                var source = new Rectangle(x, y, (col + 1) * texture.Width / 5 - x, texture.Height / 2);
                batch.Draw(texture, death.Center - Main.screenPosition, source, Color.White * Math.Min(1, (60 - age) / 16),
                    0, new Vector2(source.Width * .5f, source.Height * .62f), 440f / source.Height, SpriteEffects.None, 0);
            }
        }
        finally { batch.End(); }
    }
}
