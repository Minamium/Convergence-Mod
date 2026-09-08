#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Ordinary owner-created weapon projectiles, not a second Raid damage protocol.
// Clients render the accepted projectile pose; they never supply damage outcomes.
public abstract class NullCantorClawProjectile : ModProjectile
{
    private readonly HashSet<int> struckRoots = new();
    private int elapsedUpdates;
    internal float Age => elapsedUpdates / 3f;
    internal bool HasImpact { get; private set; }
    internal Vector2 Impact { get; private set; }
    internal float ImpactAge { get; private set; }
    internal Player Owner => Main.player[Projectile.owner];
    internal bool ValidOwner => Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers
        && !Main.player[Projectile.owner].noItems && !Main.player[Projectile.owner].CCed
        && RitualArmamentItems.Usable(Main.player[Projectile.owner])
        && Main.player[Projectile.owner].HeldItem.type == ModContent.ItemType<NullRefrain>();
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/NullCantorClaws";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    protected void Defaults(DamageClass damage)
    {
        Projectile.width = Projectile.height = 48;
        Projectile.friendly = true; Projectile.DamageType = damage;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 300; Projectile.extraUpdates = 2; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    protected void Advance() => elapsedUpdates++;
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => ValidOwner && !struckRoots.Contains(Root(target)) ? null : false;
    private static int Root(NPC n) => n.realLife >= 0 && n.realLife < Main.maxNPCs ? n.realLife : n.whoAmI;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        struckRoots.Add(Root(target));
        if (HasImpact) return;
        HasImpact = true; Impact = target.Center; ImpactAge = Age;
        Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((ushort)elapsedUpdates); writer.Write(HasImpact);
        if (HasImpact) { writer.Write(Impact.X); writer.Write(Impact.Y); writer.Write(ImpactAge); }
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        int updates = reader.ReadUInt16(); float age = updates / 3f; bool hit = reader.ReadBoolean();
        Vector2 at = hit ? new(reader.ReadSingle(), reader.ReadSingle()) : Vector2.Zero;
        float when = hit ? reader.ReadSingle() : 0;
        if (!float.IsFinite(age) || age < 0 || age > 100 || !float.IsFinite(at.X) || !float.IsFinite(at.Y)
            || Math.Abs(at.X) > 1_000_000 || Math.Abs(at.Y) > 1_000_000 || !float.IsFinite(when) || when < 0 || when > age)
        { Projectile.Kill(); return; }
        elapsedUpdates = Math.Max(elapsedUpdates, updates);
        if (hit) { HasImpact = true; Impact = at; ImpactAge = when; }
    }
    // The Client GlobalProjectile owns art; Dedicated Server never allocates it.
    public override bool PreDraw(ref Color lightColor) => false;
}

public sealed class NullCantorClawSwipe : NullCantorClawProjectile
{
    internal int Hand => Projectile.ai[0] == 0 ? 0 : 1;
    internal float Duration => Math.Clamp(Projectile.ai[2], 10, 90);
    internal float Progress => Age / Duration;
    internal int Facing => MathF.Cos(Projectile.ai[1]) >= 0 ? 1 : -1;
    internal float Aim => Projectile.ai[1];
    public override void SetDefaults() { Defaults(CalamityTrueMelee.Damage); Projectile.ownerHitCheck = true; }
    public override bool? CanDamage() => ValidOwner && NullCantorClawMotion.SwingLive(Progress) ? null : false;
    public override void AI()
    {
        if (!ValidOwner || !float.IsFinite(Projectile.ai[0]) || (Projectile.ai[0] != 0 && Projectile.ai[0] != 1)
            || !float.IsFinite(Aim) || !float.IsFinite(Projectile.ai[2]) || Progress >= 1)
        { Projectile.Kill(); return; }
        Advance(); Projectile.Center = Owner.MountedCenter;
        Owner.ChangeDir(Facing); Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = 2;
        float angle = Aim + NullCantorClawMotion.SwingAngle(Progress, Hand) * Facing;
        if (Hand == 0) Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
        else Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
    }
    internal Vector2 World(NVector p)
    {
        p.Y *= Facing; p = NullCantorClawMotion.Rotate(p, Aim);
        return Projectile.Center + new Vector2(p.X, p.Y);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!NullCantorClawMotion.SwingLive(Progress)) return false;
        // Swept finger capsules at eight subposes; no tunneling when attack speed grows.
        float begin = Math.Max(NullCantorClawMotion.SweepStart, Progress - 1f / ((Projectile.extraUpdates + 1) * Duration));
        for (int sample = 0; sample <= 8; sample++)
        {
            var pose = NullCantorClawMotion.SwingPose(MathHelper.Lerp(begin, Progress, sample / 8f), Hand);
            for (int finger = 0; finger < 5; finger++)
                for (int part = 0; part < 3; part++)
                {
                    var segment = NullCantorClawMotion.Segment(pose, finger, part);
                    float point = 0;
                    if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                        World(segment.Start), World(segment.End), segment.Radius * 2, ref point)) return true;
                }
            Vector2 palm = World(pose.Palm);
            float nearestX = Math.Clamp(palm.X, targetHitbox.Left, targetHitbox.Right);
            float nearestY = Math.Clamp(palm.Y, targetHitbox.Top, targetHitbox.Bottom);
            if (Vector2.DistanceSquared(palm, new(nearestX, nearestY)) <= MathF.Pow(37 * pose.Scale, 2)) return true;
        }
        return false;
    }
}

public sealed class NullCantorClawCrush : NullCantorClawProjectile
{
    public override void SetDefaults() => Defaults(DamageClass.Melee);
    public override bool? CanDamage() => ValidOwner && NullCantorClawMotion.CrushLive(Age) ? null : false;
    public override void AI()
    {
        if (!ValidOwner || !float.IsFinite(Projectile.Center.X) || !float.IsFinite(Projectile.Center.Y)
            || Projectile.Center.X < 16 || Projectile.Center.Y < 16
            || Projectile.Center.X > Main.maxTilesX * 16 - 16 || Projectile.Center.Y > Main.maxTilesY * 16 - 16
            || Age >= NullCantorClawMotion.CrushTicks)
        { Projectile.Kill(); return; }
        // Native spawn position is the bounded, clicked, immutable world coordinate.
        // Moving targets are not pulled in or followed after the click.
        Advance(); Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = 2;
        float direction = (Projectile.Center - Owner.Center).ToRotation();
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction - MathHelper.PiOver2 - .34f);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, direction - MathHelper.PiOver2 + .34f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => NullCantorClawMotion.CrushLive(Age) && NullCantorClawMotion.CrushHits(
            new(targetHitbox.Center.X - Projectile.Center.X, targetHitbox.Center.Y - Projectile.Center.Y),
            new(targetHitbox.Width * .5f, targetHitbox.Height * .5f));
}
