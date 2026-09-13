using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public sealed class MeridianBastion : ModProjectile
{
    internal float Age => Projectile.ai[0];
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    internal float Fade => Projectile.ai[2] < 0 ? 1 + Projectile.ai[2] / 20 : 1;
    internal bool Overdrive => Age >= RitualGrandScore.BatteryFire;
    internal Vector2 Muzzle(int lane, float? renderAge = null, Vector2? renderRoot = null, Vector2? renderAxis = null)
    {
        float age = renderAge ?? Age;
        float arrival = RitualGrandScore.Arrival(age, 0);
        // All rounds leave one barrel. Lane still selects the unchanged shot
        // cadence/ammo budget, not a separate floating gun position.
        return (renderRoot ?? Projectile.Center) + (renderAxis ?? Axis) * (122 - (1 - arrival) * 60);
    }
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = false;
        Projectile.DamageType = DamageClass.Ranged; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] is < -20 or > 0)
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualChannel.Valid(Projectile, owner, ModContent.ItemType<PaleMeridian>()))
        { Projectile.Kill(); return; }
        if (RitualChannel.Fade(Projectile)) return;
        if (Projectile.owner == Main.myPlayer && !owner.channel) { RitualChannel.Stop(Projectile); return; }
        RitualChannel.Hold(Projectile, owner, Overdrive ? .055f : .085f);
        Projectile.ai[0]++;
        int lane = RitualGrandScore.BatteryLane((int)Age);
        if (Projectile.owner != Main.myPlayer || lane < 0) return;
        // Initial item use reserves no ammo. Every actual shot calls PickAmmo
        // exactly once, retaining native ammo bonuses and conservation hooks.
        if (!owner.PickAmmo(owner.HeldItem, out _, out _, out int damage, out float knockback, out int ammo))
        { RitualChannel.Stop(Projectile); return; }
        Vector2 at = Muzzle(lane), focus = owner.MountedCenter + Axis * 1300;
        Vector2 direction = RitualArmamentItems.Aim(focus - at, owner.direction);
        bool heavy = Overdrive && (int)Age % 36 == 0;
        Projectile.NewProjectile(owner.GetSource_ItemUse_WithPotentialAmmo(owner.HeldItem, ammo), at, direction * (heavy ? 31 : 26),
            ModContent.ProjectileType<MeridianNeedle>(), RitualArmamentRules.ScaledDamage(damage, heavy ? 1.15f : Overdrive ? .62f : .95f),
            knockback, Projectile.owner, 0, -1, heavy ? 4 : 0);
    }
}
