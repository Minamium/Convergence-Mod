using System;
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public sealed class WitnessLitany : ModProjectile
{
    internal float Age => Projectile.ai[0];
    internal bool Stealth => Projectile.ai[2] == 1;
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    internal Vector2 Crown => Projectile.Center + Axis * 220;
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = false;
        Projectile.DamageType = RitualArmamentItems.DamageClassFor(RitualArmamentKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] is < 0 or > 1) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualChannel.Valid(Projectile, owner, ModContent.ItemType<LastWitness>()) || Age >= RitualGrandScore.WitnessEnd)
        { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer && !owner.channel && Age < RitualGrandScore.WitnessFire)
        { Projectile.Kill(); return; }
        RitualChannel.Hold(Projectile, owner, Age < RitualGrandScore.WitnessMerge ? .08f : .025f);
        Projectile.ai[0]++;
        if (Projectile.owner != Main.myPlayer) return;
        int tick = (int)Age;
        if (tick < RitualGrandScore.WitnessMerge && tick % 29 == 16)
        {
            int index = tick / 29;
            Vector2 at = Crown + (index * MathHelper.TwoPi / 6).ToRotationVector2() * 190;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Axis * 22,
                ModContent.ProjectileType<WitnessEcho>(), RitualArmamentRules.ScaledDamage(Projectile.damage, .28f),
                Projectile.knockBack, Projectile.owner, 0, -1, 0);
        }
        if (tick == RitualGrandScore.WitnessFire)
        {
            int child = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Crown, Axis * 30,
                ModContent.ProjectileType<WitnessBlade>(), RitualArmamentRules.ScaledDamage(Projectile.damage, 5.4f),
                Projectile.knockBack, Projectile.owner, 0, -1, Stealth ? 1 : 0);
            CalamityRogueArmamentDamage.Mark(child, Stealth);
            Projectile.netUpdate = true;
        }
    }
}
