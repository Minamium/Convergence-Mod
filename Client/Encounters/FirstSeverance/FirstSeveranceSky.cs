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
    private Texture2D? mist;
    // IsActive includes the fade-out tail. Track the requested scene separately
    // so a new Fight can reactivate during that tail and repeated calls are no-ops.
    internal bool IsSceneRequested => active;
    public override void Activate(Vector2 position, params object[] args) => active = true;
    public override void Deactivate(params object[] args) => active = false;
    public override bool IsActive() => active || fade > 0f;
    public override void Reset() { active = false; fade = 0f; }
    internal void Unload()
    {
        Reset(); cathedral = null;
        Texture2D? old = mist; mist = null;
        if (old is not null) Main.QueueMainThreadAction(old.Dispose);
    }
    public override float GetCloudAlpha() => 1f - fade * .92f;

    public override void Update(GameTime gameTime)
    {
        if (Main.dedServ || Main.gameMenu) { Reset(); return; }
        var combat = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat;
        bool ending = ModContent.GetInstance<FirstSeverancePrototypePresentation>().IsEnding;
        if (!ending && (combat is null || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) || !local.IsConnected))
            active = false;
        fade = Math.Clamp(fade + (active ? .012f : -.025f), 0f, 1f);
    }

    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || fade <= 0 || maxDepth < 0 || minDepth >= 0) return;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        cathedral ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/Backgrounds/HollowCathedral", AssetRequestMode.ImmediateLoad);
        Texture2D painting = cathedral.Value;
        float time = RitualRenderClock.Time / 60f;
        var combat = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat;
        float unrest = combat?.BossPhase == FirstSeveranceBossPhase.Final
            ? .5f + .5f * FirstSeveranceChoreography.FinalProgress(combat.ActionIndex) : .18f;
        var state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        double tick = state.EstimatedAuthorityTick;
        float discharge = 0;
        if (combat?.LanceVolley is { } lance)
            discharge = FirstSeveranceVisualCurves.ReleaseImpulse(tick, lance.FireTick);
        if (combat?.GridVolley is { } grid)
            discharge = Math.Max(discharge, FirstSeveranceVisualCurves.ReleaseImpulse(tick, grid.FireTick));
        if (combat?.Substate == FirstSeveranceSubstate.PhaseTransition)
        {
            float transition = FirstSeveranceStageVisuals.RuptureAge(combat, tick);
            discharge = FirstSeveranceVisualCurves.Window(transition, .36, .39)
                * (1 - FirstSeveranceVisualCurves.Window(transition, .39, .6));
        }
        // Illumination responds behind the terrain. It never changes the world
        // weather or covers players/forecasts with a full-screen flash.
        float exposure = reduced ? .65f : .78f - unrest * .10f - discharge * .16f;
        float scale = Math.Max(Main.screenWidth / (float)painting.Width, Main.screenHeight / (float)painting.Height) * 1.08f;
        if (!reduced) scale *= 1 + .012f * MathF.Sin(time * .19f);
        // Overscan covers all aspect ratios; bounded parallax never exposes an edge.
        Vector2 drift = reduced ? Vector2.Zero : new Vector2(
            MathF.Sin(Main.screenPosition.X * .0005f + time * .065f) * 12,
            MathF.Sin(Main.screenPosition.Y * .0006f + time * .082f) * 8);
        batch.Draw(painting, new Vector2(Main.screenWidth, Main.screenHeight) * .5f + drift,
            null, Color.White * (fade * exposure), 0f,
            new Vector2(painting.Width, painting.Height) * .5f, scale, SpriteEffects.None, 0f);
        if (reduced) return;
        EnsureMist();
        Texture2D fog = mist!;
        // Broad moving cathedral light with stratified occlusion; these are
        // textured depth planes, not hazard-like straight luminous lines.
        for (int layer = 0; layer < 5; layer++)
        {
            float seed = FirstSeveranceRaidVfx.Seed(layer + 31);
            Vector2 point = new(Main.screenWidth * (.1f + seed * .8f) + drift.X * .5f,
                Main.screenHeight * (.27f + MathF.Sin(time * .06f + layer) * .035f));
            Color light = new(118, 89, 154, 0);
            batch.Draw(fog, point, null, light * (fade * (.12f + discharge * .24f)),
                -.9f + seed * .7f, fog.Size() * .5f,
                new Vector2(Main.screenHeight * 1.3f / fog.Width, (100 + seed * 160) / fog.Height), SpriteEffects.None, 0);
        }
        // Separate depth layers move even when the camera/player is stationary.
        // The painting remains the architectural anchor, not an animated hazard.
        for (int layer = 0; layer < 7; layer++)
        {
            float depth = layer / 6f;
            Vector2 point = new(Main.screenWidth * (.5f + MathF.Sin(time * (.045f + depth * .04f) + layer * 2) * .28f),
                Main.screenHeight * (.20f + depth * .70f) + MathF.Sin(time * .12f + layer) * 24);
            Color veil = layer % 2 == 0 ? new(74, 58, 91) : new(16, 16, 25);
            batch.Draw(fog, point + drift * depth * 2, null, veil * (fade * (.32f + unrest * .15f)),
                MathF.Sin(time * .07f + layer) * .06f, fog.Size() * .5f,
                new Vector2(Main.screenWidth * (1.2f + depth * .5f) / fog.Width, (130 + depth * 110) / fog.Height), SpriteEffects.None, 0);
        }
        // Uneven abandoned puppet strings in the cathedral's upper fly loft.
        // Deliberately dim and behind terrain: never resemble attack forecasts.
        for (int cord = 0; cord < 11; cord++)
        {
            float x = (cord + .3f) * Main.screenWidth / 11;
            float length = Main.screenHeight * (.13f + (cord * 7 % 5) * .052f);
            Vector2 prior = new(x, 0);
            for (int n = 1; n <= 18; n++)
            {
                float t=n/18f;
                Vector2 p=new(x + MathF.Sin(time*.28f+cord)*t*t*9 + drift.X*.4f*t, length*t);
                Vector2 delta=p-prior;
                batch.Draw(TextureAssets.MagicPixel.Value,prior,new Rectangle(0,0,1,1),new Color(138,123,112)*(fade*.17f),
                    delta.ToRotation(),new Vector2(0,.5f),new Vector2(delta.Length(),1.3f),SpriteEffects.None,0);
                prior = p;
            }
            batch.Draw(TextureAssets.MagicPixel.Value,new Rectangle((int)prior.X-2,(int)prior.Y,4,8),
                new Rectangle(0,0,1,1),new Color(135,116,91)*(fade*.16f));
        }
        // Sparse falling ash behind terrain, player sprites and danger markers.
        for (int i = 0; i < 22; i++)
        {
            float x = ((i * 157.71f + MathF.Sin(time * .10f + i) * 18) % Main.screenWidth + Main.screenWidth) % Main.screenWidth;
            float y = (i * 113.9f + time * (3 + i % 4)) % Main.screenHeight;
            batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)x, (int)y, 1, 2),
                new Rectangle(0, 0, 1, 1), new Color(180, 169, 152) * (fade * .20f));
        }
    }
    private void EnsureMist()
    {
        if (mist is not null) return;
        const int width = 192, height = 64;
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f), v = y / (height - 1f);
                float envelope = MathF.Pow(MathF.Sin(u * MathF.PI) * MathF.Sin(v * MathF.PI), 2);
                float curl = .5f + .20f * MathF.Sin(u * 26 + MathF.Sin(v * 11) * 2)
                    + .15f * MathF.Sin(u * 53 - v * 19) + .10f * MathF.Sin(u * 109 + v * 31);
                pixels[y * width + x] = Color.White * (envelope * Math.Clamp(curl, 0, 1));
            }
        mist = new Texture2D(Main.instance.GraphicsDevice, width, height);
        mist.SetData(pixels);
    }
}
