using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Actors;

public sealed class FirstSeverancePrototypePylon : ModNPC
{
    private byte partyCount = 2;

    internal void ConfigureParty(int count)
    {
        var tuning = FirstSeverancePartyScaling.ForCount(count);
        partyCount = tuning.ParticipantCount;
        NPC.lifeMax = tuning.PylonLife;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(partyCount);
        writer.Write(System.Math.Clamp(NPC.life, 0, NPC.lifeMax));
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        byte count = reader.ReadByte();
        int life = reader.ReadInt32();
        if (count is < FirstSeveranceRoster.MinimumCount or > FirstSeveranceRoster.MaximumCount || life < 0 || life > FirstSeverancePartyScaling.ForCount(count).PylonLife)
            throw new InvalidDataException("Invalid First Severance Pylon health.");
        ConfigureParty(count);
        NPC.life = life;
    }

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
        NPC.width = 56;
        NPC.height = 88;
        ConfigureParty(2);
        NPC.damage = 0;
        NPC.defense = 120;
        NPC.knockBackResist = 0f;
        NPC.value = 0f;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.lavaImmune = true;
        NPC.netAlways = true;
        NPC.dontTakeDamage = true;
        NPC.chaseable = false;
        NPC.scale = 1f;
    }

    public override bool CheckActive() => false;

    public override bool? CanBeHitByItem(Player player, Item item)
        => FirstSeveranceCombatAuthority.CanHitActor(NPC, player.whoAmI) ? null : false;

    public override bool? CanBeHitByProjectile(Projectile projectile)
        => FirstSeveranceCombatAuthority.CanHitActor(NPC, projectile.owner) ? null : false;

    public override void AI()
    {
        NPC.velocity = Vector2.Zero;
        NPC.dontTakeDamage = NPC.ai[2] != 1f;
        NPC.chaseable = !NPC.dontTakeDamage;
        float direction = ((int)NPC.ai[1] & 1) == 0 ? 1f : -1f;
        NPC.rotation += 0.012f * direction;
        NPC.timeLeft = NPC.activeTime;

        if (!Main.dedServ && Main.rand.Next(5) == 0)
        {
            int dust = Dust.NewDust(
                NPC.position,
                NPC.width,
                NPC.height,
                DustID.GemSapphire,
                0f,
                0f,
                135,
                default,
                NPC.dontTakeDamage ? 0.65f : 0.95f);
            Main.dust[dust].noGravity = true;
        }
    }

    public override Color? GetAlpha(Color drawColor)
    {
        return NPC.dontTakeDamage
            ? new Color(58, 77, 92, 200)
            : new Color(115, 225, 255, 255);
    }
}
