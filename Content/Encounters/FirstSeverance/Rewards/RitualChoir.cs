#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public sealed class ChoirOfTheUnmadeBuff : ModBuff
{
    public override string Texture => "Convergence/Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem";
    public override void SetStaticDefaults()
    { Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<ChoirSentinel>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public sealed class ChoirSentinel : ModProjectile
{
    internal float Age => Projectile.ai[0];
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 12;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = 38; Projectile.height = 66;
        Projectile.minion = true; Projectile.minionSlots = 1;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 18000;
    }
    public override bool? CanDamage() => false; // The body is ornamental; notes own damage.
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        int buff = ModContent.BuffType<ChoirOfTheUnmadeBuff>();
        if (!owner.active || owner.dead) { owner.ClearBuff(buff); Projectile.Kill(); return; }
        if (!owner.HasBuff(buff)) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2; Projectile.ai[0]++;
        if (Projectile.ai[0] >= 36000) Projectile.ai[0] -= 36000;
        float phase = Projectile.minionPos * 2.399963f;
        Vector2 home = owner.MountedCenter + new Vector2(MathF.Cos(phase) * (100 + Projectile.minionPos % 3 * 28),
            -95 + MathF.Sin(phase) * 28 + MathF.Sin(Age * .035f + phase) * 10);
        NPC? target = RitualTargeting.Acquire(Projectile, owner, manual: true);
        Vector2 destination = home;
        if (target is not null && RitualArmamentItems.Usable(owner))
            destination = target.Center + new Vector2(MathF.Cos(phase) * 250,
                -190 + MathF.Sin(phase) * 90);
        Vector2 delta = destination - Projectile.Center;
        Vector2 desired = delta * .10f;
        if (desired.LengthSquared() > 28 * 28) desired = Vector2.Normalize(desired) * 28;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .16f);
        Projectile.rotation = Projectile.velocity.X * .012f;
        if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2400 * 2400)
        {
            Projectile.Center = home; Projectile.velocity = Vector2.Zero;
            if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        }
        int cadence = RitualArmamentRules.ChoirPeriod;
        if (target is null || !RitualArmamentItems.Usable(owner) || Projectile.owner != Main.myPlayer
            || (int)Age % cadence != Projectile.minionPos * 7 % cadence) return;
        int shot = (int)Projectile.ai[2];
        Projectile.ai[2] = (shot + 1) % 3;
        Vector2 aim = RitualArmamentItems.Aim(target.Center - Projectile.Center, owner.direction);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, aim * 16,
            ModContent.ProjectileType<ChoirNote>(), RitualArmamentRules.ScaledDamage(Projectile.damage,
                RitualArmamentRules.ChoirMultiplier(shot)), Projectile.knockBack, Projectile.owner, 0, target.whoAmI, shot == 2 ? 1 : 0);
        Projectile.netUpdate = true;
    }
}

public sealed class RitualArmamentPose : ModProjectile
{
    private float age;
    internal float Age => age;
    internal RitualArmamentKind Kind => (RitualArmamentKind)(int)Projectile.ai[0];
    internal float Duration => Math.Clamp(Projectile.ai[1], 6, 90);
    internal bool Empowered => Projectile.ai[2] > .5f;
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = false; Projectile.hostile = false;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 100;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !float.IsFinite(Projectile.ai[0])
            || !float.IsFinite(Projectile.ai[1]) || !float.IsFinite(Projectile.ai[2]) || !Enum.IsDefined(Kind))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner) || owner.HeldItem.type != RitualArmamentItems.TypeFor(Kind) || ++age > Duration)
        { Projectile.Kill(); return; }
        Vector2 aim = RitualArmamentItems.Aim(Projectile.velocity, owner.direction);
        Projectile.Center = owner.MountedCenter; Projectile.rotation = aim.ToRotation();
        owner.ChangeDir(aim.X >= 0 ? 1 : -1); owner.heldProj = Projectile.whoAmI;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
    }
}
