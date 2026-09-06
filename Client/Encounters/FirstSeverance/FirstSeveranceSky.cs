#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// Client background only; never changes world time/weather/biome or input.
internal sealed class FirstSeveranceSky : CustomSky
{
    internal const string Key = "Convergence:FirstSeverance";
    private bool active;
    private float fade;
    private Asset<Texture2D>? cathedral;
    // IsActive includes the fade-out tail. Track the requested scene separately
    // so a new Fight can reactivate during that tail and repeated calls are no-ops.
    internal bool IsSceneRequested => active;
    public override void Activate(Vector2 position, params object[] args) => active = true;
    public override void Deactivate(params object[] args) => active = false;
    public override bool IsActive() => active || fade > 0f;
    public override void Reset() { active = false; fade = 0f; }
    public override float GetCloudAlpha() => 1f - fade * .92f;

    public override void Update(GameTime gameTime)
    {
        if (Main.dedServ || Main.gameMenu) { Reset(); return; }
        var combat = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat;
        if (combat is null || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) || !local.IsConnected)
            active = false;
        fade = Math.Clamp(fade + (active ? .012f : -.025f), 0f, 1f);
    }

    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || fade <= 0 || maxDepth < 0 || minDepth >= 0) return;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        cathedral ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/Backgrounds/HollowCathedral", AssetRequestMode.ImmediateLoad);
        Texture2D painting = cathedral.Value;
        float scale = Math.Max(Main.screenWidth / (float)painting.Width, Main.screenHeight / (float)painting.Height) * 1.08f;
        // Overscan covers all aspect ratios; bounded parallax never exposes an edge.
        Vector2 drift = reduced ? Vector2.Zero : new Vector2(
            MathF.Sin(Main.screenPosition.X * .0005f) * 12,
            MathF.Sin(Main.screenPosition.Y * .0006f) * 8);
        batch.Draw(painting, new Vector2(Main.screenWidth, Main.screenHeight) * .5f + drift,
            null, Color.White * (fade * (reduced ? .65f : 1f)), 0f,
            new Vector2(painting.Width, painting.Height) * .5f, scale, SpriteEffects.None, 0f);
        if (reduced) return;
        float time = Main.GameUpdateCount % 216000 / 60f;
        // Sparse falling ash behind terrain, player sprites and danger markers.
        for (int i = 0; i < 22; i++)
        {
            float x = ((i * 157.71f + MathF.Sin(time * .10f + i) * 18) % Main.screenWidth + Main.screenWidth) % Main.screenWidth;
            float y = (i * 113.9f + time * (3 + i % 4)) % Main.screenHeight;
            batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)x, (int)y, 1, 2),
                new Rectangle(0, 0, 1, 1), new Color(180, 169, 152) * (fade * .20f));
        }
    }
}
