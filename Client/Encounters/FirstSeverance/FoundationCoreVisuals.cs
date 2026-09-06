#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// One world-space draw pass. Never uses the tile render-target's off-screen offset.
[Autoload(Side = ModSide.Client)]
public sealed class FoundationCoreVisuals : GlobalTile
{
    private Asset<Texture2D>? atlas;
    public override void Unload() => atlas = null;
    public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        => !FoundationCoreTileEntity.IsCoreType(type);

    public override bool PreDrawPlacementPreview(int i, int j, int type, SpriteBatch spriteBatch,
        ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
    {
        if (!FoundationCoreTileEntity.IsCoreType(type)) return true;
        if (frame.X == 0 && frame.Y == 0)
        {
            bool large = type == ModContent.TileType<FoundationPlinthTile>();
            DrawPlinth(spriteBatch, position + new Vector2(large ? 96 : 16, large ? 64 : 32),
                validPlacement ? Ice * .7f : Danger * .7f, large ? 192 : 64, screenSpace: true);
        }
        return false;
    }

    internal void DrawWorld(SpriteBatch batch, FirstSeveranceClientStateSystem state)
    {
        if (Main.dedServ) return;
        foreach (TileEntity entity in TileEntity.ByID.Values)
        {
            if (entity is not FoundationCoreTileEntity core || !core.IsTileValidForEntity(core.Position.X, core.Position.Y)) continue;
            Vector2 foot = core.GroundCenter;
            if (Math.Abs(foot.X - Main.screenPosition.X - Main.screenWidth * .5f) < Main.screenWidth + 256
                && Math.Abs(foot.Y - Main.screenPosition.Y - Main.screenHeight * .5f) < Main.screenHeight + 256)
                DrawPlinth(batch, foot, Color.White, core.FootprintWidth == 12 ? 192 : 64);
        }
        if (state.Combat is { } combat)
        {
            float intro = combat.Substate == FirstSeveranceSubstate.SpawnIntro
                ? 1 - Math.Clamp((float)((double)combat.ResolveTick - state.EstimatedAuthorityTick) / FirstSeveranceEncounterPlan.Instance.Timing.SpawnIntroTicks, 0, 1) : 1;
            DrawField(batch, new(combat.CoreX, combat.CoreY), intro, state.EstimatedAuthorityTick);
        }
        else if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<FoundationCoreItem>())
        {
            Vector2 ground = new(Player.tileTargetX * 16, (Player.tileTargetY + 1) * 16);
            bool valid = FoundationPlinthSpace.IsOpen(Player.tileTargetX, Player.tileTargetY + 1);
            Outline(batch, FirstSeveranceContainmentBounds.FromGround(ground.X, ground.Y), (valid ? Gold : Danger) * .6f, 1.5f);
            Utils.DrawBorderString(batch, valid ? "160 x 70 // AIRSPACE CLEAR" : "160 x 70 // CLEAR SOLID OBSTRUCTIONS",
                ground - Main.screenPosition + new Vector2(0, 18), valid ? Ice : Danger, .65f, .5f);
        }
    }

    internal void DrawPlinth(SpriteBatch batch, Vector2 foot, Color color, float width, bool screenSpace = false)
    {
        Sprite(batch, new(0, 230, 850, 226), foot, new(width, width / 3), 0, color, new(.5f, 1), screenSpace);
        if (!screenSpace)
            Line(batch, foot + new Vector2(-width * .30f, -14), foot + new Vector2(width * .30f, -14), Ice * .6f, 2);
    }

    private void DrawField(SpriteBatch batch, Vector2 ground, float intro, ulong tick)
    {
        float rise = Ease(Math.Clamp((intro - .04f) / .54f, 0, 1));
        float ignition = Ease(Math.Clamp((intro - .42f) / .32f, 0, 1));
        float age = tick % 36000 / 60f;
        var field = FirstSeveranceContainmentBounds.FromGround(ground.X, ground.Y);
        Vector2 center = new(field.CenterX, field.CenterY);
        // Full faint outline is present immediately: the collision never hides
        // behind the rising decorative segments.
        Outline(batch, field, Gold * (.28f + ignition * .5f), 2.5f);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = ground.X + side * (72 + rise * 265);
            Vector2 foot = new(x, ground.Y);
            DrawPlinth(batch, foot, new Color(170, 167, 172) * rise, 100);
            float height = 70 + rise * (field.Bottom - field.Top) * .5f;
            for (float h = 0; h < height; h += 256)
            {
                float segment = Math.Min(256, height - h);
                Sprite(batch, new(946, 0, 207, 748), foot - new Vector2(0, h), new(60, segment), 0,
                    Color.White, new(.5f, 1));
            }
            Vector2 top = foot - new Vector2(0, height);
            Vector2 latch = Vector2.Lerp(top, center + new Vector2(side * 145, -35), ignition);
            Line(batch, top, latch, new Color(30, 28, 34), 25);
            Line(batch, top, latch, Gold * .55f, 3);
            float wallX = side < 0 ? field.Left : field.Right;
            Line(batch, new(wallX, field.Bottom), new(wallX, field.Bottom - (field.Bottom - field.Top) * rise), new Color(15, 13, 20), 30);
            Line(batch, new(wallX, field.Bottom), new(wallX, field.Top), Ice * (.3f + ignition * .3f), 2);
            for (float y = field.Bottom - 48; y > field.Top; y -= 96)
            {
                float brightness = .18f + .3f * (.5f + .5f * MathF.Sin(age * 2 - y * .006f));
                Line(batch, new(wallX - 10, y + 15), new(wallX + 10, y - 15), Gold * brightness * ignition, 2);
            }
        }
        Vector2 lifted = Vector2.Lerp(ground - new Vector2(0, 50), center, rise);
        Sprite(batch, new(40, 469, 780, 780), lifted, new(265 + ignition * 55), age * .014f,
            Color.White * (.35f + rise * .65f), new(.5f));
        if (ignition > 0)
        {
            Ring(batch, center, 152 + MathF.Sin(age) * 2, Gold * ignition * .6f, 2, -age * .03f, 12);
            float travel = (age * .17f) % 1;
            for (int side = -1; side <= 1; side += 2)
                Line(batch, center + new Vector2(side * (175 + travel * 150), 0), center + new Vector2(side * (180 + travel * 150), 0), Ice * ignition * (1 - travel), 3);
        }
        foreach (Vector2 corner in new[] { new Vector2(field.Left, field.Top), new Vector2(field.Right, field.Top), new Vector2(field.Left, field.Bottom), new Vector2(field.Right, field.Bottom) })
            Sprite(batch, new(836, 762, 402, 475), corner, new(88, 105), 0, Color.White * ignition, new(.5f));
    }

    private static void Outline(SpriteBatch batch, FirstSeveranceContainmentBounds b, Color color, float width)
    {
        Line(batch, new(b.Left, b.Bottom), new(b.Left, b.Top), color, width);
        Line(batch, new(b.Left, b.Top), new(b.Right, b.Top), color, width);
        Line(batch, new(b.Right, b.Top), new(b.Right, b.Bottom), color, width);
        Line(batch, new(b.Right, b.Bottom), new(b.Left, b.Bottom), color, width);
    }

    private void Sprite(SpriteBatch batch, Rectangle source, Vector2 position, Vector2 size, float rotation, Color tint, Vector2 pivot, bool screenSpace = false)
    {
        atlas ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/Tiles/ContainmentAtlas", AssetRequestMode.ImmediateLoad);
        Texture2D texture = atlas.Value;
        float factor = texture.Width / 1254f;
        source = new((int)(source.X * factor), (int)(source.Y * factor), (int)(source.Width * factor), (int)(source.Height * factor));
        batch.Draw(texture, position - (screenSpace ? Vector2.Zero : Main.screenPosition), source, tint, rotation,
            new Vector2(source.Width, source.Height) * pivot, size / new Vector2(source.Width, source.Height), SpriteEffects.None, 0);
    }
}
