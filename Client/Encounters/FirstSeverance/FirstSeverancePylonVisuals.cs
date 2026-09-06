using System;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

internal sealed class FirstSeverancePylonVisuals : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        => entity.type == ModContent.NPCType<FirstSeverancePrototypePylon>();

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (Main.dedServ)
            return false;
        Vector2 center = npc.Center;
        Color ice = npc.dontTakeDamage ? new Color(96, 96, 108) : FirstSeveranceBossVisuals.Ice;
        var combat = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat;
        if (combat is not null && combat.Substate == FirstSeveranceSubstate.PylonCheck)
        {
            FirstSeveranceBossVisuals.Line(spriteBatch, center, FirstSeveranceBossVisuals.CoreCenter(combat), ice * 0.2f, 1.5f);
            // Final timeout warning belongs to the actual remaining pylons, not
            // an inferred local damage result. These arches are purely cosmetic.
            double tick = ModContent.GetInstance<FirstSeverancePrototypePresentation>().RenderTick;
            float gathering = FirstSeveranceVisualCurves.Window(tick, combat.ResolveTick - 90d, combat.ResolveTick);
            float close = FirstSeveranceVisualCurves.PreRelease(tick, combat.ResolveTick, 30);
            Color cue = FirstSeveranceAttackAccents.Magenta;
            for (int i = 0; i < 4; i++)
                FirstSeveranceAttackAccents.Arc(spriteBatch, center, 95 - gathering * 35,
                    i * MathF.Tau / 4 + gathering, .85f, cue * gathering, 3);
            FirstSeveranceBossVisuals.Ring(spriteBatch, center, 42 + close * 9, Color.White * close, 3);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 top = center + new Vector2(side * 25, -45);
            Vector2 bottom = center + new Vector2(side * 25, 45);
            FirstSeveranceBossVisuals.Line(spriteBatch, top, bottom, new Color(28, 45, 56), 10);
            FirstSeveranceBossVisuals.Line(spriteBatch, top, bottom, ice * 0.7f, 2);
            FirstSeveranceBossVisuals.Line(spriteBatch, top, center + new Vector2(0, -59), ice, 4);
            FirstSeveranceBossVisuals.Line(spriteBatch, bottom, center + new Vector2(0, 59), ice, 4);
        }
        for (int row = -1; row <= 1; row += 2)
            FirstSeveranceBossVisuals.Line(spriteBatch, center + new Vector2(-34, row * 32),
                center + new Vector2(34, row * 32), new Color(164, 140, 96), 5);
        // A small matching reliquary shard replaces the rotating item icon.
        // This is still one 56x88 target; the surrounding cage is decorative.
        Texture2D texture = ModContent.Request<Texture2D>("Convergence/Assets/Textures/NPCs/NullCantorBody").Value;
        var shard = new Rectangle((int)(texture.Width * .3f), 0, (int)(texture.Width * .4f), texture.Height);
        spriteBatch.Draw(texture, center - screenPos, shard, npc.dontTakeDamage ? Color.Gray : Color.White, 0f,
            new Vector2(shard.Width * .5f, texture.Height * .36f), 115f / texture.Height, SpriteEffects.None, 0f);
        FirstSeveranceBossVisuals.Ring(spriteBatch, center, 40, ice * 0.7f, 2,
            (float)(Main.GameUpdateCount % 720) * 0.008f, 4);
        return false;
    }
}
