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
    private readonly FirstSeveranceDollVisuals doll = new();
    public override void Unload() { atlas = null; doll.Unload(); }
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
            double renderTick = ModContent.GetInstance<FirstSeverancePrototypePresentation>().RenderTick;
            // Preparation already deployed the field; do not collapse/rebuild it
            // when the second cinematic (Boss introduction) begins.
            DrawField(batch, new(combat.CoreX, combat.CoreY), 1, renderTick);
        }
        else if (state.Preparation is { } preparation && preparation.TryGetMemberByServerSlot(Main.myPlayer, out _))
        {
            double tick = state.EstimatedAuthorityTick + RitualRenderClock.Fraction;
            float deployment = Math.Clamp((float)((tick - preparation.EnteredTick) / FirstSeverancePreparationTimeline.DeploymentTicks), 0, 1);
            DrawField(batch, new(preparation.GroundX, preparation.GroundY), deployment, tick);
            bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
            Vector2 seat = new(preparation.GroundX,
                preparation.GroundY - FirstSeveranceLanceTuning.BossHeightAboveCore);
            float engaged = Window(deployment, .42, .58) * (1 - Window(deployment, .7, .9));
            // The lifting apparatus catches its load before Ready. The girl
            // remains intact here; capture belongs exclusively to SpawnIntro.
            FirstSeveranceRaidVfx.Pressure(batch, seat, Vector2.UnitY, 580, 320,
                tick - preparation.EnteredTick, deployment, engaged * .65f, Ice, reduced);
            FirstSeveranceRaidVfx.Flare(batch, seat, tick - preparation.EnteredTick,
                engaged * .6f, Ice, reduced, 2);
            FirstSeveranceRaidVfx.Flush(batch);
            doll.DrawPreparation(batch, preparation, tick,
                reduced);
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
        // Crop to the solid bottom course, not the atlas's transparent gutter.
        // One pixel of ground overlap avoids a sampling seam in world/preview.
        Sprite(batch, new(0, 230, 850, 212), foot + Vector2.UnitY, new(width, width / 3), 0, color, new(.5f, 1), screenSpace);
        if (!screenSpace)
            Line(batch, foot + new Vector2(-width * .30f, -14), foot + new Vector2(width * .30f, -14), Ice * .6f, 2);
    }

    private void DrawField(SpriteBatch batch, Vector2 ground, float intro, double tick)
    {
        // Latches arrive, the lift catches its weight, then the upper stage
        // engages. Cosmetic only: the complete containment outline exists at t0.
        float rise = DeploymentRise(intro);
        float ignition = DeploymentIgnition(intro);
        float age = (float)(tick % 36000) / 60f;
        var field = FirstSeveranceContainmentBounds.FromGround(ground.X, ground.Y);
        Vector2 center = new(field.CenterX, field.CenterY);
        Vector2 lifted = Vector2.Lerp(ground - new Vector2(0, 50), center, rise);
        // Full faint outline is present immediately: the collision never hides
        // behind the rising decorative segments.
        Outline(batch, field, Gold * (.28f + ignition * .5f), 2.5f);
        for (int side = -1; side <= 1; side += 2)
        {
            // Two independent, single-piece posts on each side. Outer posts
            // are taller/wider; never stack a whole capped column as a module.
            for (int tier = 0; tier < 2; tier++)
            {
                bool outer = tier == 0;
                float x = ground.X + side * (outer ? 230 + rise * 340 : 140 + rise * 190);
                Vector2 foot = new(x, ground.Y);
                float baseWidth = outer ? 196 : 144;
                float height = 70 + rise * ((field.Bottom - field.Top) * .5f + (outer ? 320 : 190));
                float seat = baseWidth / 3 * .68f;
                Sprite(batch, new(957, 10, 172, 720), foot - new Vector2(0, seat),
                    new(outer ? 128 : 84, height - seat), 0,
                    new Color(170, 172, 184), new(.5f, 1));
                // Draw the base in front: the foot socket overlaps the top course.
                DrawPlinth(batch, foot, new Color(170, 167, 172) * rise, baseWidth);
                Vector2 top = foot - new Vector2(0, height);
                // Textured horizontal gantry, then thin hanging cables. No diagonal
                // solid braces: both shoulders really hang from the raised capitals.
                float shoulderX = outer ? 460 : 248;
                Vector2 hanger = new(MathHelper.Lerp(top.X, lifted.X + side * shoulderX, ignition), top.Y + 16);
                if (ignition > .001f)
                {
                    Sprite(batch, new(1002, 90, 90, 570), (top + new Vector2(0, 16) + hanger) * .5f,
                        new(outer ? 28 : 22, Math.Abs(top.X - hanger.X) + 24), MathHelper.PiOver2,
                        new Color(169, 171, 184) * ignition, new(.5f));
                    // Rig-owned cords attach to these hoists and to the *moving*
                    // shoulders/wrists. No second set ending in empty mid-air.
                    Sprite(batch, new(836, 762, 402, 475), hanger, new(28, 38), 0,
                        Color.White * ignition, new(.5f));
                }
            }
            float wallX = side < 0 ? field.Left : field.Right;
            Line(batch, new(wallX, field.Bottom), new(wallX, field.Bottom - (field.Bottom - field.Top) * rise), new Color(15, 13, 20), 30);
            Line(batch, new(wallX, field.Bottom), new(wallX, field.Top), Ice * (.3f + ignition * .3f), 2);
            for (float y = field.Bottom - 48; y > field.Top; y -= 96)
            {
                float brightness = .18f + .3f * (.5f + .5f * MathF.Sin(age * 2 - y * .006f));
                Line(batch, new(wallX - 10, y + 15), new(wallX + 10, y - 15), Gold * brightness * ignition, 2);
            }
        }
        // The human-scale attendant and then the coffin replace the old floating
        // concentric machine emblem. Their lives are drawn by the owning state.
        foreach (Vector2 corner in new[] { new Vector2(field.Left, field.Top), new Vector2(field.Right, field.Top), new Vector2(field.Left, field.Bottom), new Vector2(field.Right, field.Bottom) })
            Sprite(batch, new(836, 762, 402, 475), corner, new(88, 105), 0, Color.White * ignition, new(.5f));
    }

    private static float DeploymentRise(float intro) => .14f * Arrive(intro - .04, .035)
        + .68f * Window(intro, .16, .38) + .18f * Window(intro, .47, .58);
    private static float DeploymentIgnition(float intro) => .78f * Arrive(intro - .42, .11)
        + .22f * Window(intro, .64, .74);

    internal static Vector2 HoistAnchor(Vector2 ground, int side, bool outer, float intro)
    {
        float rise=DeploymentRise(intro), ignition=DeploymentIgnition(intro);
        float start=outer?230+rise*340:140+rise*190;
        float height=70+rise*(FirstSeveranceLanceTuning.BossHeightAboveCore+(outer?320:190));
        return ground+new Vector2(side*MathHelper.Lerp(start,outer?460:248,ignition),-height+16);
    }

    private static void DrawSuspension(SpriteBatch batch, Vector2 from, Vector2 to,
        float opacity, float age, int side, int cable)
    {
        Vector2 previous = from;
        // Endpoints stay attached; only the tensioned cable's interior breathes.
        for (int segment = 1; segment <= 12; segment++)
        {
            float t = segment / 12f;
            Vector2 next = Vector2.Lerp(from, to, t);
            next.X += MathF.Sin(t * MathF.PI) * MathF.Sin(age * 1.4f + cable + side) * 1.3f;
            Line(batch, previous, next, new Color(34, 38, 47) * opacity, 4);
            Line(batch, previous + new Vector2(-.8f, 0), next + new Vector2(-.8f, 0),
                new Color(166, 187, 195) * (.52f * opacity), 1);
            previous = next;
        }
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
