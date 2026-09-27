#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.GhostSamurai;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using Terraria.UI;

namespace Convergence.Client.Encounters.GhostSamurai;

// The sky is drawn before terrain and actors. Its coordinates belong to the
// accepted arena, so camera motion never slides the ruins under the fight.
internal sealed class GhostSamuraiBattlefieldSky : CustomSky
{
    internal const string Key = "Convergence:GhostSamuraiBattlefield";
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private Guid fight;
    private SamuraiArenaBounds arena;
    private GhostSamuraiBoss? owner;
    private bool active;
    private float age;

    internal void Bind(GhostSamuraiBoss boss)
    {
        if (fight != boss.Fight) Reset();
        fight = boss.Fight;
        arena = boss.Arena;
        owner = boss;
        age = boss.VisualAge;
        active = true;
    }

    public override void Activate(Vector2 position, params object[] args) { }
    public override void Deactivate(params object[] args) => Reset();
    public override bool IsActive() => active && arena.IsValid && fight != Guid.Empty
        && owner is { ProjectionFresh: true } boss && boss.Fight == fight && boss.NPC.active
        && !Main.gameMenu && Main.LocalPlayer is { active: true, dead: false, ghost: false } player
        && player.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(boss);
    public override void Reset() { active = false; fight = Guid.Empty; arena = default; owner = null; age = 0; }
    public override float GetCloudAlpha() => IsActive() ? 0f : 1f;
    public override void Update(GameTime gameTime) { }

    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || Main.gameMenu || !IsActive() || minDepth >= 0 || maxDepth < 0) return;
        using var scope = new WorldGraphicsScope(batch);
        var device = Main.instance.GraphicsDevice;
        var shader = ShaderManager.GetShader("Convergence.SamuraiBattlefield");
        shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("clock", age / 60f);
        shader.TrySetParameter("reduced", GhostSamuraiRigArt.Reduced ? 1f : 0f);
        shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
        // One small world-space bleed keeps fractional raster/zoom edges filled.
        // The physical viewport mask below covers the bleed outside the field.
        const float bleed = 4f;
        Vector2 at = new(arena.Left - Main.screenPosition.X - bleed, arena.Top - Main.screenPosition.Y - bleed);
        Vector2 fieldSize = new(arena.Right - arena.Left, arena.Bottom - arena.Top);
        Vector2 size = fieldSize + new Vector2(bleed * 2);
        Vector2 uvMin = new(-bleed / fieldSize.X, -bleed / fieldSize.Y);
        Vector2 uvSize = size / fieldSize;
        for (int i = 0; i < 6; i++)
        {
            int corner = i switch { 0 => 0, 1 => 2, 2 => 1, 3 => 1, 4 => 2, _ => 3 };
            Vector2 cornerUv = new(corner % 2, corner / 2);
            quad[i] = new(new(at + cornerUv * size, 0), Color.White, uvMin + cornerUv * uvSize);
        }
        shader.Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiBattlefield : ModSystem
{
    private GhostSamuraiBattlefieldSky? sky;
    private bool activated;
    private FirstSeveranceFieldMaskLayout? fieldMask;

    public override void Load()
    {
        if (!Main.dedServ) SkyManager.Instance[GhostSamuraiBattlefieldSky.Key] = sky = new GhostSamuraiBattlefieldSky();
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || sky is null) return;
        GhostSamuraiBoss? boss = null;
        if (!Main.gameMenu && Main.LocalPlayer is { active: true, dead: false, ghost: false } player)
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.ModNPC is GhostSamuraiBoss candidate && candidate.Arena.IsValid
                    && candidate.Fight != Guid.Empty && candidate.ProjectionFresh
                    && player.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(candidate))
                { boss = candidate; break; }
        if (boss is not null)
        {
            sky.Bind(boss);
            if (!activated) SkyManager.Instance.Activate(GhostSamuraiBattlefieldSky.Key, Vector2.Zero);
            activated = true;
        }
        else Stop();
    }

    public override void PostDrawTiles()
    {
        fieldMask = null;
        if (Main.dedServ || Main.gameMenu || !activated || sky is null || !sky.IsActive()) return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is not GhostSamuraiBoss boss || !boss.Arena.IsValid
                || !Main.LocalPlayer.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(boss)) continue;
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            var transform = System.Numerics.Matrix3x2.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y)
                * new System.Numerics.Matrix3x2(view.M11, view.M12, view.M21, view.M22, view.M41, view.M42);
            var viewport = Main.instance.GraphicsDevice.Viewport;
            fieldMask = FirstSeveranceFieldMaskLayout.Capture(boss.Arena.Left, boss.Arena.Top,
                boss.Arena.Right, boss.Arena.Bottom, transform, viewport.Width, viewport.Height);
            break;
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.dedServ || Main.gameMenu || fieldMask is null) return;
        layers.Insert(0, new LegacyGameInterfaceLayer("Convergence: Samurai outside arena", () =>
        {
            if (fieldMask is { } mask)
            {
                SpriteBatch batch = Main.spriteBatch;
                Draw(mask.Top); Draw(mask.Bottom); Draw(mask.Left); Draw(mask.Right);
                void Draw(FirstSeveranceMaskRect rect)
                {
                    if (rect.Width <= 0 || rect.Height <= 0) return;
                    batch.Draw(TextureAssets.MagicPixel.Value, new Vector2(rect.X, rect.Y), new Rectangle(0, 0, 1, 1),
                        Color.Black, 0, Vector2.Zero, new Vector2(rect.Width, rect.Height), SpriteEffects.None, 0);
                }
            }
            return true;
        }, InterfaceScaleType.None));
    }

    private void Stop()
    {
        fieldMask = null;
        if (activated && SkyManager.Instance is { } manager)
            manager.Deactivate(GhostSamuraiBattlefieldSky.Key);
        sky?.Reset();
        activated = false;
    }
    public override void ClearWorld() => Stop();
    public override void OnWorldUnload() => Stop();
    public override void Unload() { Stop(); sky = null; }
}
