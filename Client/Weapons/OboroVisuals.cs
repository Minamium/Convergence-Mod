using System;
using System.Collections.Generic;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using OboroItem = Convergence.Content.Items.Oboro.Oboro;

namespace Convergence.Client.Weapons;

[Autoload(Side = ModSide.Client)]
public sealed class OboroVisuals : ModSystem
{
    private readonly List<(OboroBurst Burst, ulong At)> bursts = new();
    private readonly (ulong Generation, uint Swing)[] sounded = new (ulong, uint)[256];
    private readonly OboroSwingPresentation[] swings = new OboroSwingPresentation[256];
    public override void Load() => OboroPackets.DisplayBurst += ReceiveBurst;
    public override void Unload() { OboroPackets.DisplayBurst -= ReceiveBurst; ClearWorld(); }
    public override void ClearWorld() { bursts.Clear(); Array.Clear(sounded); Array.Clear(swings); }
    private void ReceiveBurst(OboroBurst burst)
    {
        if (bursts.Count >= 128) bursts.RemoveAt(0);
        bursts.Add((burst, Main.GameUpdateCount));
        SoundEngine.PlaySound(SoundID.Item71 with { Volume = .45f, Pitch = .25f, MaxInstances = 3 }, new(burst.X, burst.Y));
    }
    public override void PostUpdateEverything()
    {
        bursts.RemoveAll(x => Main.GameUpdateCount - x.At > 22);
        for (int slot = 0; slot < Main.maxPlayers; slot++)
        {
            Player player = Main.player[slot];
            if (!player.active) { swings[slot]?.Clear(); sounded[slot] = default; continue; }
            var state = player.GetModPlayer<OboroPlayer>();
            if (!state.Holding || player.dead) { swings[slot]?.Clear(); sounded[slot] = default; continue; }
            var visual = swings[slot] ??= new OboroSwingPresentation();
            Vector2 center = player.MountedCenter;
            visual.Update(state.View, state.VisualAge, state.SwingVisible, true, center.X, center.Y, player.direction, Main.GameUpdateCount);
            if (visual.Swinging) player.ChangeDir(state.View.Facing);
            if (visual.Swinging || visual.Settling)
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, visual.Pose.Angle - MathF.PI / 2);
            if (!visual.Swinging) continue;
            float p = visual.Pose.Progress;
            if (p < OboroRules.Windup(state.View.Step) || sounded[player.whoAmI] == (state.View.Generation, state.View.Swing)) continue;
            sounded[player.whoAmI] = (state.View.Generation, state.View.Swing);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = .8f, Pitch = state.View.Step == 2 ? -.6f : .15f, MaxInstances = 4 }, player.Center);
            if (state.View.Step == 2) SoundEngine.PlaySound(SoundID.Item71 with { Volume = .5f, Pitch = -.4f }, player.Center);
        }
    }
    public override void PostDrawTiles()
    {
        if (Main.gameMenu || Main.dedServ) return;
        SpriteBatch b = Main.spriteBatch;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (Player p in Main.ActivePlayers)
            {
                if (p.dead) continue;
                var state = p.GetModPlayer<OboroPlayer>();
                if (state.Holding)
                {
                    if (swings[p.whoAmI] is { } visual)
                    {
                        // Cull cosmetic work when the owner and its bounded echoes are offscreen.
                        Vector2 screen = Vector2.Transform(p.Center - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
                        float margin = 900 * Math.Abs(Main.GameViewMatrix.TransformationMatrix.M11);
                        if (screen.X > -margin && screen.X < Main.screenWidth + margin && screen.Y > -margin && screen.Y < Main.screenHeight + margin)
                        {
                            OboroArt.Afterimages(b, visual);
                            OboroArt.Swing(b, visual.Pose, visual.Swinging);
                        }
                    }
                    else OboroArt.Sword(b, p.MountedCenter + new Vector2(p.direction * 12, 8), p.direction == 1 ? -1.1f : -2.04f, 145, Color.White);
                }
                if (state.ZanshinRemaining > 0)
                    for (int i = 0; i < 3; i++)
                    {
                        float a = (float)Main.GameUpdateCount * .04f + i * MathHelper.TwoPi / 3;
                        OboroArt.Flame(b, p.Center + new Vector2(MathF.Cos(a) * 46, MathF.Sin(a) * 26), 32);
                    }
            }
            foreach (NPC npc in Main.ActiveNPCs)
            {
                var state = npc.GetGlobalNPC<OboroNpc>();
                int count = Math.Min(5, state.MarkCount);
                for (int i = 0; i < count; i++)
                    OboroArt.Flame(b, npc.Top + new Vector2((i - (count - 1) / 2f) * 14, -18), 24);
                if (state.FireRemaining > 0)
                    for (int i = 0; i < (OboroArt.Reduced ? 2 : 4); i++)
                        OboroArt.Flame(b, npc.Center + new Vector2((i - 1.5f) * Math.Min(npc.width / 4f, 40),
                            MathF.Sin((float)Main.GameUpdateCount * .1f + i) * 18), 46, .8f);
            }
            foreach (var entry in bursts)
            {
                float p = (Main.GameUpdateCount - entry.At) / 22f, alpha = 1 - p;
                Vector2 center = new(entry.Burst.X, entry.Burst.Y);
                foreach (float angle in entry.Burst.Angles)
                {
                    Vector2 axis = angle.ToRotationVector2() * (85 + p * 90);
                    OboroArt.Line(b, center - axis, center + axis, 15 * alpha, new Color(132, 55, 235, 0) * alpha);
                    OboroArt.Line(b, center - axis, center + axis, 3 * alpha, new Color(237, 217, 255) * alpha);
                    if (!OboroArt.Reduced) OboroArt.Sword(b, center - axis * (.9f - p), angle, 160, new Color(208, 164, 255) * (alpha * .6f));
                }
                OboroArt.Flame(b, center, 90 * alpha);
            }
        }
        finally { b.End(); }
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class OboroItemVisuals : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is OboroItem;
    public override bool PreDrawInInventory(Item item, SpriteBatch b, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = OboroArt.Blade;
        b.Draw(texture, position, null, Color.White, 0, texture.Size() * .5f, scale, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(Item item, SpriteBatch b, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D texture = OboroArt.Blade;
        b.Draw(texture, item.Center - Main.screenPosition, null, Color.White, rotation, texture.Size() * .5f, 70f / texture.Width, SpriteEffects.None, 0);
        OboroArt.Flame(b, item.Center + new Vector2(20, -12), 25);
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class OboroBuffVisuals : GlobalBuff
{
    public override bool PreDraw(SpriteBatch b, int type, int buffIndex, ref BuffDrawParams drawParams)
    {
        if (type != ModContent.BuffType<Zanshin>()) return true;
        drawParams.MouseRectangle = new((int)drawParams.Position.X, (int)drawParams.Position.Y, 32, 32);
        drawParams.TextPosition = drawParams.Position + new Vector2(0, 34);
        b.Draw(OboroArt.Spirit, drawParams.Position + new Vector2(16), null, drawParams.DrawColor,
            0, OboroArt.Spirit.Size() * .5f, 40f / OboroArt.Spirit.Height, SpriteEffects.None, 0);
        return false;
    }
}
