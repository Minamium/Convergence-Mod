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
        Color ice = npc.dontTakeDamage ? new Color(86, 108, 126) : new Color(118, 235, 255);
        var combat = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat;
        if (combat is not null && combat.Substate == FirstSeveranceSubstate.PylonCheck)
            FirstSeveranceBossVisuals.Line(spriteBatch, center, FirstSeveranceBossVisuals.CoreCenter(combat), ice * 0.2f, 1.5f);

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
        Texture2D texture = TextureAssets.Npc[npc.type].Value;
        spriteBatch.Draw(texture, center - screenPos, null, Color.White, npc.rotation,
            texture.Size() * 0.5f, 1.1f, SpriteEffects.None, 0f);
        FirstSeveranceBossVisuals.Ring(spriteBatch, center, 40, ice * 0.7f, 2,
            (float)(Main.GameUpdateCount % 720) * 0.008f, 4);
        return false;
    }
}
