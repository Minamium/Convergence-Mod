using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

// Network entity only. Harmless forecast and damaging interval are distinct rules;
// all collisions are resolved by GhostSamuraiRuntime on the authority, exactly once.
public sealed class GhostSamuraiAttackProjectile : ModProjectile
{
    internal Guid Fight;
    internal int BossSlot = -1;
    internal SamuraiHazard Hazard;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathLaser;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.hostile = Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 600;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void OnSpawn(IEntitySource source)
    {
        if (source is GhostSamuraiAttackSource owned)
        {
            Fight = owned.Fight; BossSlot = owned.BossSlot; Hazard = owned.Hazard;
        }
    }
    public override bool? CanDamage() => false;
    internal bool TryGetAge(out float age)
    {
        age = 0;
        if (BossSlot < 0 || BossSlot >= Main.maxNPCs || !Main.npc[BossSlot].active
            || Main.npc[BossSlot].ModNPC is not GhostSamuraiBoss boss || boss.Fight != Fight || Fight == Guid.Empty) return false;
        age = boss.VisualAge;
        return true;
    }
    public override void AI()
    {
        if (!TryGetAge(out float age))
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
            return; // Late NPC synchronization may still arrive on the client.
        }
        Projectile.Center = new Vector2(Hazard.CenterX(age), Hazard.CenterY(age));
        if (Main.netMode != NetmodeID.MultiplayerClient && age >= Hazard.End) Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor) => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Fight.ToByteArray()); writer.Write((short)BossSlot); Hazard.Write(writer);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(16); int slot = reader.ReadInt16(); SamuraiHazard hazard = SamuraiHazard.Read(reader);
        if (bytes.Length != 16 || slot < 0 || slot >= Main.maxNPCs) throw new InvalidDataException("ghost_samurai.projectile_owner_invalid");
        // Client-created SyncProjectile packets never gain authority over a fight.
        if (Main.netMode == NetmodeID.Server) return;
        Fight = new Guid(bytes); BossSlot = slot; Hazard = hazard;
    }
}
