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
            // Shield conduits travel into the Core, visibly explaining what this
            // satellite is sustaining. All curves are decorative, not beams.
            Vector2 last = center;
            for (int n = 1; n <= (reduced ? 12 : 24); n++)
            {
                float t = n / (float)(reduced ? 12 : 24);
                Vector2 next = Vector2.Lerp(center, core, t) + new Vector2(0,
                    MathF.Sin(t * MathF.PI) * (42 + 8 * MathF.Sin((float)tick * .018f)));
                float flow = .5f + .5f * MathF.Sin(t * 17 - (float)tick * .14f);
                FirstSeveranceBossVisuals.Line(spriteBatch, last, next,
                    FirstSeveranceAttackAccents.Neon(ice, (.12f + .32f * flow) * assemble), 1 + flow * 1.7f);
                last = next;
            }
            // Final timeout warning belongs to the actual remaining pylons, not
            // an inferred local damage result. These arches are purely cosmetic.
            float gathering = FirstSeveranceVisualCurves.CastTension(tick, combat.ResolveTick - 90d, combat.ResolveTick);
            pressure = gathering;
            float close = FirstSeveranceVisualCurves.PreRelease(tick, combat.ResolveTick, 30);
            Color cue = FirstSeveranceAttackAccents.Magenta;
            for (int i = 0; i < 4; i++)
                FirstSeveranceAttackAccents.Arc(spriteBatch, center, 95 - gathering * 35,
                    i * MathF.Tau / 4 + gathering, .85f, cue * gathering, 3);
            FirstSeveranceBossVisuals.Ring(spriteBatch, center, 42 + close * 9, Color.White * close, 3);
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
        // A small matching reliquary shard replaces the rotating item icon.
        // This is still one 56x88 target; the surrounding cage is decorative.
        Texture2D texture = ModContent.Request<Texture2D>("Convergence/Assets/Textures/NPCs/NullCantorBody").Value;
        var shard = new Rectangle((int)(texture.Width * .3f), 0, (int)(texture.Width * .4f), texture.Height);
        spriteBatch.Draw(texture, center - screenPos, shard, npc.dontTakeDamage ? Color.Gray : Color.White, 0f,
            new Vector2(shard.Width * .5f, texture.Height * .36f), 115f / texture.Height, SpriteEffects.None, 0f);
        for (int i = 0; i < 3; i++)
            FirstSeveranceAttackAccents.Arc(spriteBatch, center, 44 - pressure * 6,
                i * MathF.Tau / 3 + (float)(tick % 36000) * .009f, .64f,
                FirstSeveranceAttackAccents.Neon(ice, assemble * .65f), 1.6f);
        return false;
    }
}
