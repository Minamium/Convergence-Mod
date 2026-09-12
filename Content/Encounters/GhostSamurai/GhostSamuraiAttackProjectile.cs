using System;
using System.IO;
using Microsoft.Xna.Framework;
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
    internal SamuraiWispMotion WispMotion;
    internal SamuraiSlashAim SlashAim;
    internal SamuraiHazard DisplayHazard => Hazard.Shape == SamuraiShape.Slash ? SlashAim.Geometry(Hazard) : Hazard;
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
            if (Hazard.Shape == SamuraiShape.Wisp) WispMotion = SamuraiWispMotion.Spawn(Hazard);
            else SlashAim = SamuraiSlashAim.Spawn(Hazard, owned.AimLockTick);
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
        Projectile.Center = VisualCenter(age);
        if (Main.netMode != NetmodeID.MultiplayerClient && age >= Hazard.End) Projectile.Kill();
    }
    internal Vector2 VisualCenter(float age) => Hazard.Shape != SamuraiShape.Wisp ? new(SlashAim.X, SlashAim.Y)
        : Main.netMode == NetmodeID.MultiplayerClient ? new(WispMotion.VisualX(age), WispMotion.VisualY(age)) : new(WispMotion.X, WispMotion.Y);

    internal void Aim(int age, Vector2 start, Vector2 direction)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Hazard.Shape != SamuraiShape.Slash || SlashAim.Locked) return;
        SlashAim = SlashAim.Advance(Hazard, age, start.X, start.Y, direction.X, direction.Y);
        Projectile.Center = new(SlashAim.X, SlashAim.Y);
        if (SlashAim.Locked || age % GhostSamuraiRules.AimSyncInterval == 0) Projectile.netUpdate = true;
    }

    internal void AdvanceWisp(int age, Vector2 target)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Projectile.active || Hazard.Shape != SamuraiShape.Wisp
            || age < Hazard.Born || age >= Hazard.End) return;
        WispMotion = WispMotion.Advance(Hazard, age, target.X, target.Y);
        Projectile.Center = new(WispMotion.X, WispMotion.Y);
        // Stagger full snapshots; no target/steering is inferred from a client player.
        if (age == Hazard.Born || age == Hazard.Fire || (age + Projectile.identity) % GhostSamuraiRules.WispSyncInterval == 0)
            Projectile.netUpdate = true;
    }

    internal bool Hits(int age, Player p) => Hazard.Shape == SamuraiShape.Wisp
        ? WispMotion.Hits(Hazard, age, p.Center.X, p.Center.Y, p.width * .5f, p.height * .5f)
        : SlashAim.Locked && DisplayHazard.Hits(age, p.Center.X, p.Center.Y, p.width * .5f, p.height * .5f);

    public override bool PreDraw(ref Color lightColor) => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Fight.ToByteArray()); writer.Write((short)BossSlot); Hazard.Write(writer);
        if (Hazard.Shape == SamuraiShape.Wisp) WispMotion.Write(writer);
        else SlashAim.Write(writer);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(16); int slot = reader.ReadInt16(); SamuraiHazard hazard = SamuraiHazard.Read(reader);
        SamuraiWispMotion motion = hazard.Shape == SamuraiShape.Wisp ? SamuraiWispMotion.Read(reader, hazard) : default;
        SamuraiSlashAim aim = hazard.Shape == SamuraiShape.Slash ? SamuraiSlashAim.Read(reader, hazard) : default;
        if (bytes.Length != 16 || new Guid(bytes) == Guid.Empty || slot < 0 || slot >= Main.maxNPCs)
            throw new InvalidDataException("ghost_samurai.projectile_owner_invalid");
        // Client-created SyncProjectile packets never gain authority over a fight.
        if (Main.netMode == NetmodeID.Server) return;
        if (Fight != Guid.Empty && (Fight != new Guid(bytes) || BossSlot != slot || Hazard != hazard
            || (hazard.Shape == SamuraiShape.Wisp && !motion.CanReplace(WispMotion))
            || (hazard.Shape == SamuraiShape.Slash && !aim.CanReplace(SlashAim)))) return;
        Fight = new Guid(bytes); BossSlot = slot; Hazard = hazard;
        WispMotion = motion;
        SlashAim = aim;
    }
}
