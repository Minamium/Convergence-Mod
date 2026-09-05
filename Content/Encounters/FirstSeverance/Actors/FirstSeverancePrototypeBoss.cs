using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Actors;

public sealed class FirstSeverancePrototypeBoss : ModNPC
{
    internal const int MaximumLife = 60_000_000;

    public override string Texture =>
        "Convergence/Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem";

    public override void SetStaticDefaults()
    {
        NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
    }

    public override void OnKill()
    {
        FirstSeveranceCombatAuthority.RecordActorDeath(NPC);
    }

    public override void SetDefaults()
    {
        NPC.width = 144;
        NPC.height = 144;
        NPC.lifeMax = MaximumLife;
        NPC.damage = 0;
        NPC.defense = 240;
        NPC.knockBackResist = 0f;
        NPC.value = 0f;
        NPC.aiStyle = -1;
        NPC.boss = true;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.lavaImmune = true;
        NPC.netAlways = true;
        NPC.dontTakeDamage = true;
        NPC.chaseable = false;
        NPC.scale = 1f;
    }

    public override bool CheckActive() => false;

    // The client presentation system draws the enormous silhouette independently
    // of NPC culling. This NPC is only its clearly marked central damageable core.
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;

    public override bool? CanBeHitByItem(Player player, Item item)
        => FirstSeveranceCombatAuthority.CanHitActor(NPC, player.whoAmI) ? null : false;

    public override bool? CanBeHitByProjectile(Projectile projectile)
        => FirstSeveranceCombatAuthority.CanHitActor(NPC, projectile.owner) ? null : false;

    public override void AI()
    {
        NPC.velocity = Vector2.Zero;
        NPC.dontTakeDamage = NPC.ai[2] != 1f;
        NPC.chaseable = !NPC.dontTakeDamage;
        NPC.rotation = 0.025f * (float)System.Math.Sin(Main.GameUpdateCount / 25d);
        NPC.timeLeft = NPC.activeTime;

        if (!Main.dedServ && Main.rand.Next(3) == 0)
        {
            Color color = NPC.dontTakeDamage
                ? new Color(63, 118, 140)
                : new Color(126, 245, 255);
            int dust = Dust.NewDust(
                NPC.position,
                NPC.width,
                NPC.height,
                DustID.GemDiamond,
                0f,
                -0.35f,
                120,
                color,
                NPC.dontTakeDamage ? 0.85f : 1.2f);
            Main.dust[dust].noGravity = true;
        }
    }

    public override Color? GetAlpha(Color drawColor)
    {
        return NPC.dontTakeDamage
            ? new Color(82, 119, 130, 210)
            : new Color(185, 252, 255, 255);
    }
}
