using System;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

internal sealed class FirstSeverancePylonVisuals : GlobalNPC
{
    // GlobalNPC is shared, not InstancePerEntity. These caches contain no NPC
    // state and must be static to satisfy tML's load-time GlobalType validation.
    private static Asset<Texture2D> cage;
    private static readonly FirstSeveranceAttackAccents accents = new();
    public override void Unload() { cage = null; accents.Unload(); }

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        => entity.type == ModContent.NPCType<FirstSeverancePrototypePylon>();

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (Main.dedServ)
            return false;
        Vector2 center = npc.Center;
        Color ice = npc.dontTakeDamage ? new Color(96, 96, 108) : FirstSeveranceBossVisuals.Ice;
        var combat = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat;
        double tick = ModContent.GetInstance<FirstSeverancePrototypePresentation>().RenderTick;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        float assemble = combat is null ? 1 : FirstSeveranceVisualCurves.Arrive(tick - combat.ActionStartedTick, 12);
        float pressure = 0;
        if (combat is not null && combat.Substate == FirstSeveranceSubstate.PylonCheck)
        {
            Vector2 core = FirstSeveranceBossVisuals.CoreCenter(combat);
            // Shield-bearing winch cables: readable solid metal, not a second
            // set of hostile laser forecasts. The old cage becomes a stage hoist.
            Vector2 last = center;
            for (int n = 1; n <= (reduced ? 12 : 24); n++)
            {
                float t = n / (float)(reduced ? 12 : 24);
                Vector2 next = Vector2.Lerp(center, core, t) + new Vector2(0,
                    MathF.Sin(t * MathF.PI) * (42 + 8 * MathF.Sin((float)tick * .018f)));
                float flow = .5f + .5f * MathF.Sin(t * 17 - (float)tick * .14f);
                FirstSeveranceBossVisuals.Line(spriteBatch, last, next,new Color(27,23,29)*assemble,4);
                FirstSeveranceBossVisuals.Line(spriteBatch, last+new Vector2(0,-1), next+new Vector2(0,-1),
                    new Color(168,145,112) * (.40f + .20f * flow) * assemble, 1.5f);
                last = next;
            }
            // Pressure travels along the physical spool, never orbiting UI arcs.
            pressure = FirstSeveranceVisualCurves.CastTension(tick, combat.ResolveTick - 90d, combat.ResolveTick);
            accents.ChargeFracture(spriteBatch, center, tick, combat.ResolveTick - 90d,
                combat.ResolveTick, FirstSeveranceAttackAccents.Magenta, reduced, .48f);
        }

        cage ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/NPCs/NullCantorRigAtlas");
        Texture2D metal = cage.Value;
        float atlasScale = metal.Width / 1254f;
        Rectangle strut = new((int)(740 * atlasScale), 0, (int)(205 * atlasScale), (int)(651 * atlasScale));
        for (int side = -1; side <= 1; side += 2)
        {
            float distance = 27 + (1 - assemble) * 34 - pressure * 4;
            for (int half = -1; half <= 1; half += 2)
                spriteBatch.Draw(metal, center + new Vector2(side * distance, half * 25) - screenPos,
                    strut, (npc.dontTakeDamage ? new Color(115, 119, 137) : Color.White) * assemble,
                    side * half * (.24f + pressure * .14f), new Vector2(strut.Width, strut.Height) * .5f,
                    new Vector2(23f / strut.Width, 78f / strut.Height), SpriteEffects.None, 0);
        }
        accents.Halo(spriteBatch, center, new Vector2(48, 108), ice, assemble * (reduced ? .15f : .38f));
        // Wound spool occupies the original 56x88 target. No new multipart actor.
        for (int coil=0;coil<7;coil++)
        {
            Vector2 at=center+new Vector2(0,(coil-3)*8);
            FirstSeveranceBossVisuals.Line(spriteBatch,at-new Vector2(20,0),at+new Vector2(20,0),new Color(32,27,34)*assemble,7);
            FirstSeveranceBossVisuals.Line(spriteBatch,at-new Vector2(18,2),at+new Vector2(18,-2),new Color(180,160,126)*assemble,2);
        }
        return false;
    }
}
