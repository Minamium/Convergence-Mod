#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public sealed class NullRefrainSlash : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    private float age;
    private bool released;
    internal int Combo => Math.Clamp((int)Projectile.ai[0], 0, 2);
    internal float Duration => Math.Clamp(Projectile.ai[2], 10, 90);
    internal float Progress => age / Duration;
    internal float Reach => NullRefrainMotion.Reach(Combo);
    internal bool HasImpact { get; private set; }
    internal Vector2 Impact { get; private set; }
    internal int Facing => MathF.Cos(Projectile.ai[1]) >= 0 ? 1 : -1;
    internal float AngleAt(float progress) => NullRefrainMotion.Angle(Combo, Facing, Projectile.ai[1], progress);
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = 240;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => NullRefrainMotion.Live(Progress) ? null : false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;
    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers
            || !float.IsFinite(Projectile.ai[0]) || !float.IsFinite(Projectile.ai[1]) || !float.IsFinite(Projectile.ai[2]))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner) || owner.HeldItem.type != ModContent.ItemType<NullRefrain>() || Progress >= 1)
        { Projectile.Kill(); return; }
        age += 1f / Projectile.MaxUpdates;
        Projectile.Center = owner.MountedCenter;
        Projectile.rotation = AngleAt(Progress);
        owner.ChangeDir(Facing); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        if (!released && Progress >= .38f)
        {
            released = true;
            if (Projectile.owner == Main.myPlayer)
            {
                int count = Combo == 2 ? 3 : 1;
                for (int i = 0; i < count; i++)
                {
                    Vector2 axis = (Projectile.ai[1] + (i - (count - 1) * .5f) * .24f).ToRotationVector2();
                    float share = Combo == 2 ? .30f / 1.7f : .40f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, axis * 15,
                        ModContent.ProjectileType<RefrainEcho>(), RitualArmamentRules.ScaledDamage(Projectile.damage, share),
                        Projectile.knockBack * .5f, Projectile.owner, 0, -1, Combo == 2 ? 1 : 0);
                }
            }
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float previous = Math.Max(.24f, Progress - 1 / (Duration * Projectile.MaxUpdates));
        for (int i = 0; i <= 8; i++)
        {
            Vector2 axis = AngleAt(MathHelper.Lerp(previous, Progress, i / 8f)).ToRotationVector2();
            float collision = 0;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center + axis * 22, Projectile.Center + axis * Reach,
                Combo == 2 ? 32 : 22, ref collision)) return true;
        }
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitRoots.Add(RitualTargeting.Root(target));
        if (HasImpact) return;
        HasImpact = true; Impact = target.Center; Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(age); writer.Write(released); writer.Write(HasImpact);
        if (HasImpact) { writer.Write(Impact.X); writer.Write(Impact.Y); }
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        float received = reader.ReadSingle(); bool fired = reader.ReadBoolean(); bool hit = reader.ReadBoolean();
        Vector2 at = hit ? new(reader.ReadSingle(), reader.ReadSingle()) : Vector2.Zero;
        if (!float.IsFinite(received) || received < 0 || received > 100
            || !float.IsFinite(at.X) || !float.IsFinite(at.Y) || Math.Abs(at.X) > 1_000_000 || Math.Abs(at.Y) > 1_000_000)
        { Projectile.Kill(); return; }
        age = Math.Max(age, received); released |= fired;
        if (hit) { HasImpact = true; Impact = at; }
    }
}
