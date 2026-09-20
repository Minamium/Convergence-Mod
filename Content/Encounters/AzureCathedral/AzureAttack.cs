#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.AzureCathedral;

// Receiving-player native hostile damage, explicitly scoped in ADR-0028.
// No manual Hurt loop, no extra health subtraction, no client phase decisions.
public sealed class AzureAttack : ModProjectile
{
    internal AzureAttackPlan Plan;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathLaser;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true; Projectile.timeLeft = 600;
    }
    public override void OnSpawn(IEntitySource source) { if (source is AzureAttackSource a) Plan = a.Plan; }
    public override bool ShouldUpdatePosition() => false;
    internal bool TryGirl(out AzureBoss? girl)
    {
        girl = Plan.Girl >= 0 && Plan.Girl < Main.maxNPCs && Main.npc[Plan.Girl].active ? Main.npc[Plan.Girl].ModNPC as AzureBoss : null;
        return girl is not null && Plan.Fight != Guid.Empty && girl.State.Fight == Plan.Fight && girl.Fresh && girl.State.Live
            && (Plan.Kind == AzureAttackKind.MouthBeam ? girl.State.WormLife > 0 : girl.State.GirlLife > 0);
    }
    internal bool Geometry(AzureBoss girl, float age, bool forecast, out Vector2 from, out Vector2 to, out float radius)
    {
        Vector2 d = Plan.Angle.ToRotationVector2(); from = new(Plan.X, Plan.Y); radius = Plan.Width;
        if (Plan.Kind == AzureAttackKind.MouthBeam)
        {
            int slot = girl.State.WormSlot;
            if (slot < 0 || slot >= Main.maxNPCs || Main.npc[slot].ModNPC is not AzureWorm w || w.Fight != Plan.Fight)
            { to = from; return false; }
            var head = w.NPC;
            // The neck turns toward the stage; the mouth and jet use one angle.
            Vector2 center = new(girl.State.Field.CenterX, girl.State.Field.CenterY);
            float angle = (center - head.Center).ToRotation() + AzureRules.Sweep(age - Plan.Fire, Plan.End - Plan.Fire);
            d = angle.ToRotationVector2(); from = head.Center + d * 72;
            radius *= forecast ? 1 : AzureRules.Envelope(age - Plan.Fire, Plan.End - Plan.Fire);
            if (girl.State.Field.ClipAxis(from.X, from.Y, d.X, d.Y, out float first, out float last))
            { to = from + d * Math.Max(0, last); from += d * Math.Max(0, first); return last > 0; }
            to = from; return false;
        }
        if (forecast) { to = from + d * Plan.Length; return true; }
        float travel = Math.Max(0, age - Plan.Fire) * (Plan.Kind == AzureAttackKind.GlassRain ? 17 : 21);
        to = from + d * Math.Min(Plan.Length, travel);
        from += d * Math.Clamp(travel - 100, 0, Plan.Length);
        radius *= AzureRules.Envelope(age - Plan.Fire, Plan.End - Plan.Fire);
        return travel < Plan.Length + 100;
    }
    public override bool? CanDamage() => TryGirl(out var g) && g!.VisualAge >= Plan.Fire && g.VisualAge < Plan.End ? null : false;
    public override bool CanHitPlayer(Player p) => TryGirl(out var g) && g!.State.Contains(p.whoAmI);
    public override bool? CanHitNPC(NPC target) => false;
    public override bool? Colliding(Rectangle projectile, Rectangle target)
    {
        if (!TryGirl(out var g) || g!.VisualAge < Plan.Fire || g.VisualAge >= Plan.End
            || !Geometry(g, g.VisualAge, false, out var a, out var b, out float radius)) return false;
        float point = 0;
        return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), a, b, radius * 2, ref point);
    }
    public override void AI()
    {
        bool valid = TryGirl(out var g);
        Projectile.hostile = valid && g!.VisualAge >= Plan.Fire && g.VisualAge < Plan.End;
        if (g is not null && valid)
        {
            Projectile.timeLeft = Math.Max(2, Plan.End + 16 - (int)g.VisualAge);
            if (Geometry(g, g.VisualAge, g.VisualAge < Plan.Fire, out var a, out var b, out _)) Projectile.Center = (a + b) * .5f;
            if (Main.netMode != NetmodeID.MultiplayerClient && g.VisualAge >= Plan.End + 16) Projectile.Kill();
        }
        else if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter writer) => Plan.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var parsed = AzureAttackPlan.Read(reader);
        if (parsed is { } next && Main.netMode != NetmodeID.Server && (Plan.Fight == Guid.Empty || Plan == next)) Plan = next;
    }
}
