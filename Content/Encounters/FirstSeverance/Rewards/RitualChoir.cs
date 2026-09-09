#nullable enable
using System;
using System.IO;
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
    internal int Ordinal { get; private set; }
    internal int ChoirCount { get; private set; } = 1;
    internal bool IsLeader => Ordinal == 0;
    internal Vector2 ConcertCenter { get; private set; }
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/V2/ChoirOfTheUnmade";
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 18;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    }
    public override void SetDefaults()
    {
        Projectile.width = 38; Projectile.height = 66;
        Projectile.minion = true; Projectile.minionSlots = 1;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 18000;
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        int buff = ModContent.BuffType<ChoirOfTheUnmadeBuff>();
        if (!owner.active || owner.dead) { owner.ClearBuff(buff); Projectile.Kill(); return; }
        if (!owner.HasBuff(buff)) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        Projectile.ai[0] = (Projectile.ai[0] + 1) % (RitualArmamentChoreography.ChoirCycle * 100);
        Ordinal = 0; ChoirCount = 0;
        // Stable within one owner; unrelated vanilla minionPos cannot reorder the choir.
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.type == Type)
            { ChoirCount++; if (p.identity < Projectile.identity) Ordinal++; }
        float phase = Ordinal * 2.399963f;
        Vector2 home = owner.MountedCenter + new Vector2(MathF.Cos(phase) * (128 + Ordinal % 3 * 28),
            -120 + MathF.Sin(phase) * 45 + MathF.Sin(Age * .03f + phase) * 9);
        NPC? target = RitualTargeting.Acquire(Projectile, owner, manual: true);
        bool usable = RitualArmamentItems.Usable(owner) && !owner.noItems && !owner.CCed;
        Vector2 orbit = target is not null && usable
            ? target.Center + new Vector2(MathF.Cos(phase) * 270, -220 + MathF.Sin(phase) * 110) : home;
        Vector2 desiredCenter = (target is not null && usable ? target.Center : owner.MountedCenter) - new Vector2(0, 260);
        if (ConcertCenter == Vector2.Zero || Vector2.DistanceSquared(ConcertCenter, desiredCenter) > 1800 * 1800)
            ConcertCenter = desiredCenter;
        else ConcertCenter = Vector2.Lerp(ConcertCenter, desiredCenter, .14f);
        var seat = RitualArmamentChoreography.ChoirSeat(Ordinal, ChoirCount);
        float assemble = usable ? RitualArmamentChoreography.ChoirAssembly(Age) : 0;
        Vector2 destination = Vector2.Lerp(orbit, ConcertCenter + new Vector2(seat.X, seat.Y), assemble);
        Vector2 desired = (destination - Projectile.Center) * .12f;
        if (desired.LengthSquared() > 38 * 38) desired = Vector2.Normalize(desired) * 38;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .18f);
        Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.velocity.X * .009f * (1 - assemble), .16f);
        if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2400 * 2400)
        {
            Projectile.Center = home; Projectile.velocity = Vector2.Zero;
            if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        }
        int shot = RitualArmamentChoreography.ChoirShotAt((int)Age % RitualArmamentChoreography.ChoirCycle, Ordinal);
        if (target is null || !usable || Projectile.owner != Main.myPlayer || shot < 0) return;
        Projectile.ai[2] = shot;
        Vector2 at = Projectile.Center + new Vector2(0, -22);
        Vector2 aim = RitualArmamentItems.Aim(target.Center - at, owner.direction);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, aim * 16,
            ModContent.ProjectileType<ChoirNote>(), RitualArmamentRules.ScaledDamage(Projectile.damage,
                RitualArmamentRules.ChoirMultiplier(shot)), Projectile.knockBack, Projectile.owner, 0, target.whoAmI, shot == 2 ? 1 : 0);
        Projectile.netUpdate = true;
    }
}

// A persistent visual apparatus, rearmed rather than killed/recreated every shot.
// Ordinary Item use still consumes ammo/mana and controls attack speed.
public sealed class RitualArmamentPose : ModProjectile
{
    private float age, lifeAge;
    internal uint Serial { get; private set; } = 1;
    internal float Age => age;
    internal float LifeAge => lifeAge;
    internal RitualArmamentKind Kind => (RitualArmamentKind)(int)Projectile.ai[0];
    internal float Duration => Math.Clamp(Projectile.ai[1], 6, 90);
    internal bool Empowered => Projectile.ai[2] > .5f;
    internal float Open => RitualArmamentChoreography.Smooth(lifeAge / 4)
        * (1 - RitualArmamentChoreography.Smooth((age - Duration - 18) / 24));
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = false; Projectile.hostile = false;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 200; Projectile.netImportant = true;
    }
    internal void Rearm(Vector2 aim, int ticks, int accent)
    {
        if (Projectile.owner != Main.myPlayer) return;
        age = 0; Serial++; Projectile.velocity = aim;
        Projectile.ai[1] = Math.Clamp(ticks, 6, 90); Projectile.ai[2] = Math.Clamp(accent, 0, 1);
        Projectile.netUpdate = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !float.IsFinite(Projectile.ai[0])
            || !float.IsFinite(Projectile.ai[1]) || !float.IsFinite(Projectile.ai[2]) || !Enum.IsDefined(Kind)
            || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner) || owner.noItems || owner.CCed
            || owner.HeldItem.type != RitualArmamentItems.TypeFor(Kind) || age > Duration + 42)
        { Projectile.Kill(); return; }
        age++; lifeAge = Math.Min(60, lifeAge + 1); Projectile.timeLeft = 2;
        Vector2 aim = RitualArmamentItems.Aim(Projectile.velocity, owner.direction);
        Projectile.Center = owner.MountedCenter; Projectile.rotation = aim.ToRotation();
        if (age <= Duration)
        {
            owner.ChangeDir(aim.X >= 0 ? 1 : -1); owner.heldProj = Projectile.whoAmI;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        }
    }
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(Serial); writer.Write(age); writer.Write(lifeAge); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        uint serial = reader.ReadUInt32(); float elapsed = reader.ReadSingle(), lifetime = reader.ReadSingle();
        if (serial == 0 || !float.IsFinite(elapsed) || elapsed < 0 || elapsed > 140
            || !float.IsFinite(lifetime) || lifetime < 0 || lifetime > 60)
        { Projectile.Kill(); return; }
        if (serial < Serial) return;
        age = serial == Serial ? Math.Max(age, elapsed) : elapsed;
        Serial = serial; lifeAge = Math.Max(lifeAge, lifetime);
    }
}
