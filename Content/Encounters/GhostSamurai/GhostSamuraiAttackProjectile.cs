using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

// Server-spawned network entity. Only SlashWave uses native Projectile damage,
// including the local player's standard immunity/dodge hooks. Other shapes retain
// the runtime's authority-owned hit path; no shape takes both paths.
public sealed class GhostSamuraiAttackProjectile : ModProjectile
{
    internal Guid Fight;
    internal int BossSlot = -1;
    internal SamuraiHazard Hazard;
    internal SamuraiWispMotion WispMotion;
    internal SamuraiSlashAim SlashAim;
    internal SamuraiHazard DisplayHazard => Hazard.HasAim ? SlashAim.Geometry(Hazard) : Hazard;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathLaser;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)GhostSamuraiRules.Phase3CircleOuterRadius + 1000;
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
            else if (Hazard.HasAim) SlashAim = SamuraiSlashAim.Spawn(Hazard, owned.AimLockTick);
        }
    }
    // The default -1 cooldown slot preserves ordinary player immunity and all
    // vanilla / ModPlayer dodge hooks. No forced Hurt or dodge suppression here.
    public override bool? CanDamage() => Hazard.Shape == SamuraiShape.SlashWave && SlashAim.Locked
        && TryGetAge(out float age) && DisplayHazard.Live(NativeAge(age)) ? null : false;
    public override bool? CanHitNPC(NPC target) => false;
    public override bool CanHitPlayer(Player target) => TryGetAge(out _)
        && Main.npc[BossSlot].ModNPC is GhostSamuraiBoss owner
        && target.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(owner);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Hazard.Shape == SamuraiShape.SlashWave && SlashAim.Locked && TryGetAge(out float age)
            && SamuraiWaveRules.Geometry(DisplayHazard, NativeAge(age)).Hits(NativeAge(age),
                targetHitbox.Center.X, targetHitbox.Center.Y, targetHitbox.Width * .5f, targetHitbox.Height * .5f);

    // Native projectiles update before PostUpdateWorld advances the SP/server
    // fight clock. MP snapshots already extrapolate into this update's age.
    private static float NativeAge(float age) => age + (Main.netMode == NetmodeID.MultiplayerClient ? 0 : 1);
    internal bool TryGetAge(out float age)
    {
        age = 0;
        if (BossSlot < 0 || BossSlot >= Main.maxNPCs || !Main.npc[BossSlot].active
            || Main.npc[BossSlot].ModNPC is not GhostSamuraiBoss boss || boss.Fight != Fight || Fight == Guid.Empty
            || !GhostSamuraiContainmentPlayer.FightActive(boss)) return false;
        age = boss.VisualAge;
        return true;
    }
    public override void AI()
    {
        if (!TryGetAge(out float age))
        {
            Projectile.hostile = false;
            if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
            return; // Late NPC synchronization may still arrive on the client.
        }
        Projectile.hostile = Hazard.Shape == SamuraiShape.SlashWave && SlashAim.Locked && DisplayHazard.Live(NativeAge(age));
        Projectile.damage = Hazard.Shape == SamuraiShape.SlashWave ? Hazard.Damage : 0;
        Projectile.Center = VisualCenter(Hazard.Shape == SamuraiShape.SlashWave ? NativeAge(age) : age);
        if (Projectile.hostile) Projectile.velocity = new Vector2(DisplayHazard.DX, DisplayHazard.DY) * SamuraiWaveRules.ChargedSlashWaveSpeed;
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, DisplayHazard.End - (int)age + 30);
            if (age >= DisplayHazard.End) Projectile.Kill();
        }
    }
    internal Vector2 VisualCenter(float age)
    {
        if (Hazard.Shape == SamuraiShape.SlashWave)
        {
            var h = DisplayHazard;
            return new Vector2(h.X, h.Y) + new Vector2(h.DX, h.DY) * Math.Max(0, age - h.Fire) * SamuraiWaveRules.ChargedSlashWaveSpeed;
        }
        return Hazard.Shape != SamuraiShape.Wisp ? new(DisplayHazard.X, DisplayHazard.Y)
            : Main.netMode == NetmodeID.MultiplayerClient ? new(WispMotion.VisualX(age), WispMotion.VisualY(age)) : new(WispMotion.X, WispMotion.Y);
    }

    internal static Vector2 RushCenter(SamuraiHazard h, float age)
        => new Vector2(h.X, h.Y) + new Vector2(h.DX, h.DY) * h.Length
            * GhostSamuraiRules.RushProgress(age - h.Fire + 1, h.End - h.Fire);

    internal void Aim(int age, Vector2 start, Vector2 direction)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Hazard.HasAim || SlashAim.Locked) return;
        SlashAim = SlashAim.Advance(Hazard, age, start.X, start.Y, direction.X, direction.Y);
        Projectile.Center = new(SlashAim.X, SlashAim.Y);
        if (SlashAim.Locked || age % GhostSamuraiRules.AimSyncInterval == 0) Projectile.netUpdate = true;
    }

    internal void AimArrival(int age, Vector2 start, Vector2 direction, Rectangle target)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Hazard.ArrivalTick == 0 || SlashAim.Locked) return;
        SlashAim = SlashAim.TrackArrival(Hazard, age, start.X, start.Y, direction.X, direction.Y,
            target.Center.X, target.Center.Y, target.Width * .5f, target.Height * .5f);
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
        : (!Hazard.HasAim || SlashAim.Locked) && DisplayHazard.Hits(age, p.Center.X, p.Center.Y, p.width * .5f, p.height * .5f);

    public override bool PreDraw(ref Color lightColor) => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Fight.ToByteArray()); writer.Write((short)BossSlot); Hazard.Write(writer);
        if (Hazard.Shape == SamuraiShape.Wisp) WispMotion.Write(writer);
        else if (Hazard.HasAim) SlashAim.Write(writer);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(16); int slot = reader.ReadInt16(); SamuraiHazard hazard = SamuraiHazard.Read(reader);
        SamuraiWispMotion motion = hazard.Shape == SamuraiShape.Wisp ? SamuraiWispMotion.Read(reader, hazard) : default;
        SamuraiSlashAim aim = hazard.HasAim ? SamuraiSlashAim.Read(reader, hazard) : default;
        if (bytes.Length != 16 || new Guid(bytes) == Guid.Empty || slot < 0 || slot >= Main.maxNPCs)
            throw new InvalidDataException("ghost_samurai.projectile_owner_invalid");
        // Client-created SyncProjectile packets never gain authority over a fight.
        if (Main.netMode == NetmodeID.Server) return;
        if (Fight != Guid.Empty && (Fight != new Guid(bytes) || BossSlot != slot || Hazard != hazard
            || (hazard.Shape == SamuraiShape.Wisp && !motion.CanReplace(WispMotion))
            || (hazard.HasAim && (!aim.CanReplace(SlashAim) || hazard.ArrivalTick == 0 && aim.LockTick != SlashAim.LockTick)))) return;
        Fight = new Guid(bytes); BossSlot = slot; Hazard = hazard;
        WispMotion = motion;
        SlashAim = aim;
    }
}
