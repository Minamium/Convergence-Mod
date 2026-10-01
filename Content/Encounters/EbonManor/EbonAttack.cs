#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor;

internal sealed record EbonAttackSource(EbonAttackPlan Plan) : IEntitySource { public string Context => "EbonManorAttack"; }

// Geometry of an accepted plan at any (fractional) tick. Server collision and
// client presentation call the same functions; nothing here is decorative.
internal static class EbonGeometry
{
    internal static Vector2 Direction(in EbonAttackPlan p) => p.Angle.ToRotationVector2();

    // Furniture rises beside its anchor during the warning (harmless), then is
    // yanked along its announced thread line with accelerating speed.
    internal static Vector2 Prop(in EbonAttackPlan p, float age)
    {
        var origin = new Vector2(p.X, p.Y);
        float t = age - p.Fire;
        if (t >= 0) return origin + Direction(p) * EbonRules.PropTravel(t);
        float lift = EbonRules.Ease((age - p.Born) / Math.Max(1, p.Fire - p.Born));
        return origin + new Vector2(0, -26 * lift + MathF.Sin(age * .21f + p.Born) * 3 * lift);
    }

    // Chandeliers hang 150px under their anchor, fall with gravity and burst
    // on the floor. Fall distance and ticks derive from the plan's length.
    internal const float Hang = 150, BodyToFloor = 120;
    internal static float FallDistance(in EbonAttackPlan p) => Math.Max(40, p.Length - Hang - BodyToFloor);
    internal static int ImpactTick(in EbonAttackPlan p) => p.Fire + EbonRules.ChandelierFallTicks(FallDistance(p));
    internal static Vector2 Chandelier(in EbonAttackPlan p, float age)
        => new(p.X, p.Y + Hang + Math.Min(FallDistance(p), EbonRules.ChandelierDrop(age - p.Fire)));
    internal static Vector2 Burst(in EbonAttackPlan p) => new(p.X, p.Y + p.Length - 30);

    internal static bool Line(Convergence.Common.Raids.Arena.RaidFieldGeometry field, Vector2 through, Vector2 d, out Vector2 a, out Vector2 b)
    {
        a = b = through;
        if (!field.ClipAxis(through.X, through.Y, d.X, d.Y, out float first, out float last)) return false;
        a = through + d * first; b = through + d * last; return true;
    }

    internal static float SpokeAngle(in EbonAttackPlan p, int k, float age)
        => p.Angle + MathHelper.TwoPi * k / p.Variant + p.Spin * EbonRules.WaltzTurn(age - p.Fire);
    internal static Vector2 Center(in EbonState s, float age)
    { var c = EbonRules.BossPosition(s.Field, s.Epoch, age); return new(c.X, c.Y); }
}

// Receiving-player native hostile damage, scoped to this feature by ADR-0029.
// No manual Hurt loop, no extra health subtraction, no client phase decisions.
public sealed class EbonAttack : ModProjectile
{
    internal EbonAttackPlan Plan;
    private bool loggedNativeHit;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathLaser;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true; Projectile.timeLeft = 600;
    }
    public override void OnSpawn(IEntitySource source) { if (source is EbonAttackSource a) Plan = a.Plan; }
    public override bool ShouldUpdatePosition() => false;
    internal bool TryBoss(out EbonBoss? boss)
    {
        boss = Plan.Boss >= 0 && Plan.Boss < Main.maxNPCs && Main.npc[Plan.Boss].active ? Main.npc[Plan.Boss].ModNPC as EbonBoss : null;
        return boss is not null && Plan.Fight != Guid.Empty && boss.State.Fight == Plan.Fight && boss.Fresh
            && boss.State.Live && Plan.Born >= boss.State.Epoch;
    }
    internal static bool Live(in EbonAttackPlan p, float age) => p.Kind == EbonAttackKind.Chandelier
        ? age >= p.Fire && age < p.End : age >= p.Fire && age < p.End;
    public override bool? CanDamage() => TryBoss(out var b) && Live(Plan, b!.VisualAge) ? null : false;
    public override bool CanHitPlayer(Player p) => TryBoss(out var b) && b!.State.CanFight(p.whoAmI);
    public override bool? CanHitNPC(NPC target) => false;
    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    { if (EbonRules.DebugOneDamagePlaytest) modifiers.SetMaxDamage(1); }
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        if (loggedNativeHit || Main.netMode == NetmodeID.Server || Main.myPlayer != target.whoAmI) return;
        loggedNativeHit = true;
        EbonPackets.Log($"event=AttackNativeImpact fight={Plan.Fight} kind={Plan.Kind} slot={target.whoAmI} intended_damage={Plan.Damage} native_source_damage={Projectile.damage} final_damage={info.Damage}");
    }
    public override bool? Colliding(Rectangle projectile, Rectangle target)
    {
        if (!TryBoss(out var boss) || !Live(Plan, boss!.VisualAge)) return false;
        float age = boss.VisualAge, point = 0;
        var field = boss.State.Field;
        switch (Plan.Kind)
        {
            case EbonAttackKind.Thread:
            {
                // swept capsule between the previous and current tick: fast furniture never tunnels
                var a = EbonGeometry.Prop(Plan, age - 1); var b = EbonGeometry.Prop(Plan, age);
                return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), a, b, Plan.Width * 2, ref point);
            }
            case EbonAttackKind.Chandelier:
                return age < EbonGeometry.ImpactTick(Plan)
                    ? Circle(target, EbonGeometry.Chandelier(Plan, age), EbonRules.ChandelierBodyRadius)
                    : Circle(target, EbonGeometry.Burst(Plan), EbonRules.BurstRadius);
            case EbonAttackKind.Waltz:
            {
                var c = EbonGeometry.Center(boss.State, age);
                for (int k = 0; k < Plan.Variant; k++)
                {
                    var d = EbonGeometry.SpokeAngle(Plan, k, age).ToRotationVector2();
                    if (!field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                    if (Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), c + d * 40, c + d * Math.Min(last, Plan.Length), Plan.Width * 2, ref point))
                        return true;
                }
                return false;
            }
            default:
                return EbonGeometry.Line(field, new(Plan.X, Plan.Y), EbonGeometry.Direction(Plan), out var from, out var to)
                    && Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), from, to, Plan.Width * 2, ref point);
        }
    }
    private static bool Circle(Rectangle target, Vector2 center, float radius)
    {
        float x = Math.Clamp(center.X, target.Left, target.Right), y = Math.Clamp(center.Y, target.Top, target.Bottom);
        return (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) <= radius * radius;
    }
    public override void AI()
    {
        bool valid = TryBoss(out var boss);
        Projectile.damage = EbonRules.NativeSourceDamage(Plan.Damage);
        if (boss is not null && valid)
        {
            float age = boss.VisualAge;
            Projectile.hostile = Live(Plan, age);
            Projectile.timeLeft = Math.Max(2, Plan.End + 20 - (int)age);
            Projectile.Center = Plan.Kind switch
            {
                EbonAttackKind.Thread => EbonGeometry.Prop(Plan, age),
                EbonAttackKind.Chandelier => age < EbonGeometry.ImpactTick(Plan) ? EbonGeometry.Chandelier(Plan, age) : EbonGeometry.Burst(Plan),
                EbonAttackKind.Waltz => EbonGeometry.Center(boss.State, age),
                _ => new(Plan.X, Plan.Y),
            };
            if (Main.netMode != NetmodeID.MultiplayerClient && age >= Plan.End + 20) Projectile.Kill();
        }
        else
        {
            Projectile.hostile = false;
            if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
        }
    }
    public override void SendExtraAI(BinaryWriter writer) => Plan.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var parsed = EbonAttackPlan.Read(reader);
        if (parsed is { } next && Main.netMode != NetmodeID.Server && (Plan.Fight == Guid.Empty || Plan == next)) Plan = next;
    }
}
