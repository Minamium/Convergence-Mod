using System;
using System.IO;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// A normal owner-created melee projectile; damage uses Terraria's projectile
// path, not Raid requests. Only the first impact pose is replicated for cosmetics.
public sealed class NullRefrainSlash : ModProjectile
{
    private float age;
    internal int Combo => Math.Clamp((int)Projectile.ai[0], 0, 2);
    internal float Progress => age / Math.Clamp(Projectile.ai[2], 10, 90);
    internal float Reach => NullRefrainMotion.Reach(Combo);
    internal bool HasImpact { get; private set; }
    internal Vector2 Impact { get; private set; }
    internal int Facing => MathF.Cos(Projectile.ai[1]) >= 0 ? 1 : -1;
    internal float AngleAt(float progress) => NullRefrainMotion.Angle(Combo, Facing, Projectile.ai[1], progress);
    public override string Texture => "Convergence/Assets/Textures/Items/NullRefrain";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 100; Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => NullRefrainMotion.Live(Progress) ? null : false;
    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers
            || !float.IsFinite(Projectile.ai[0]) || !float.IsFinite(Projectile.ai[1]) || !float.IsFinite(Projectile.ai[2]))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<NullRefrain>()
            || owner.GetModPlayer<FirstSeveranceRaidPlayer>().IsRaidDowned || Progress >= 1)
        { Projectile.Kill(); return; }
        age++;
        Projectile.Center = owner.MountedCenter;
        Projectile.rotation = AngleAt(Progress);
        owner.ChangeDir(Facing);
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // Subsample the previous-to-current stroke so high attack speed cannot
        // tunnel through an enemy between two rendered blade poses.
        float duration = Math.Clamp(Projectile.ai[2], 10, 90);
        float previous = Math.Max(.24f, Progress - 1 / duration);
        for (int i = 0; i <= 8; i++)
        {
            float angle = AngleAt(MathHelper.Lerp(previous, Progress, i / 8f));
            Vector2 axis = angle.ToRotationVector2();
            float collision = 0;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center + axis * 22, Projectile.Center + axis * Reach,
                Combo == 2 ? 32 : 22, ref collision)) return true;
        }
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (HasImpact) return;
        HasImpact = true;
        Impact = target.Center;
        Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(age); writer.Write(HasImpact);
        if (HasImpact) { writer.Write(Impact.X); writer.Write(Impact.Y); }
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        float receivedAge = reader.ReadSingle(); bool hit = reader.ReadBoolean();
        Vector2 at = hit ? new(reader.ReadSingle(), reader.ReadSingle()) : Vector2.Zero;
        if (!float.IsFinite(receivedAge) || receivedAge < 0 || receivedAge > 100
            || !float.IsFinite(at.X) || !float.IsFinite(at.Y) || Math.Abs(at.X) > 1_000_000 || Math.Abs(at.Y) > 1_000_000)
        { Projectile.Kill(); return; }
        age = Math.Max(age, receivedAge);
        if (hit) { HasImpact = true; Impact = at; }
    }
}
