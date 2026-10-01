#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Moonloom Harp (docs/encounters/ebon-manor/REWARDS.md, "Ranged"): a lyre-shaped bow. Every arrow becomes a silver
// needle (ammo damage and conservation stay native through PickAmmo); where the needle ends it leaves a taut silk
// string back to the bow. The right click plucks every string, oldest first, one per sixteenth.
// Ownership: the owner client spends ammo, spawns the needle, the strings and the glissando schedule through native
// projectile replication (netImportant + netUpdate); peers only read replicated projectile state. No packet.
public sealed class EbonLoomHarp : ModItem
{
    // Where the needle leaves the bow, px ahead of the hand along the aim.
    internal const float Muzzle = 8;

    public override string Texture => EbonRewardItems.Icon(nameof(EbonLoomHarp), ItemID.Marrow);

    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

    public override void SetDefaults()
    {
        EbonRewardItems.Defaults(Item, EbonRewardKind.Ranged);
        Item.width = 26;
        Item.height = 48;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        // The bow is drawn in code (Client/.../LoomHarpVisuals) so its string can follow the draw.
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.useAmmo = AmmoID.Arrow;
        Item.shoot = ProjectileID.WoodenArrowFriendly;
        Item.shootSpeed = EbonRewardRules.ArrowSpeed;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (!EbonRewardItems.Usable(player)) return false;
        bool glissando = player.altFunctionUse == 2;
        // Nothing to pluck, nothing to do: the click does not even start an animation.
        if (glissando && !EbonHarpStrings.Any(player)) return false;
        // The glissando is a plain use that neither shoots nor touches ammo; the shot is the ordinary bow.
        Item.useTime = Item.useAnimation = glissando ? EbonLoomHarpRules.GlissandoUse : EbonRewardRules.UseTicks(EbonRewardKind.Ranged);
        Item.shoot = glissando ? ProjectileID.None : ProjectileID.WoodenArrowFriendly;
        Item.useAmmo = glissando ? AmmoID.None : AmmoID.Arrow;
        return true;
    }

    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2 && player.whoAmI == Main.myPlayer && EbonRewardItems.Usable(player))
            EbonHarpStrings.Glissando(player);
        return true;
    }

    // Whatever arrow was picked, a needle flies at a fixed speed so every string reaches the same distance.
    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        type = ModContent.ProjectileType<EbonNeedleArrow>();
        velocity = EbonRewardItems.Aim(velocity, player.direction) * EbonRewardRules.ArrowSpeed;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer || !EbonRewardItems.Usable(player)) return false;
        Vector2 aim = EbonRewardItems.Aim(velocity, player.direction);
        Vector2 muzzle = MuzzleOf(player, aim);
        // ai[0], ai[1]: the fire point, from which the string will run to wherever the needle ends.
        int index = Projectile.NewProjectile(source, muzzle, aim * EbonRewardRules.ArrowSpeed, type, damage, knockback,
            player.whoAmI, muzzle.X, muzzle.Y);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].netUpdate = true;
        return false;
    }

    // The hand that holds the bow, pushed slightly forward; never through a wall.
    internal static Vector2 MuzzleOf(Player player, Vector2 aim)
    {
        Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, aim.ToRotation() - MathHelper.PiOver2);
        Vector2 muzzle = hand + aim * Muzzle;
        return Collision.CanHitLine(player.MountedCenter, 1, 1, muzzle, 1, 1) ? muzzle : player.MountedCenter;
    }
}

// The owner's strings: bookkeeping lives in the replicated string projectiles themselves, so there is no list to
// clear on death, item change or world unload.
internal static class EbonHarpStrings
{
    private static int StringType => ModContent.ProjectileType<EbonHarpString>();

    internal static bool Any(Player player)
    {
        int type = StringType;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.type == type && p.ai[2] == 0) return true;
        return false;
    }

    // Idle strings of one owner, oldest first.
    private static List<Projectile> Idle(int owner)
    {
        int type = StringType;
        var strings = new List<Projectile>(EbonRewardRules.MaxStrings + 1);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.type == type && p.ai[2] == 0) strings.Add(p);
        if (strings.Count < 2) return strings;
        int[] timeLeft = new int[strings.Count];
        for (int i = 0; i < timeLeft.Length; i++) timeLeft[i] = strings[i].timeLeft;
        var sorted = new List<Projectile>(strings.Count);
        foreach (int i in EbonLoomHarpRules.OldestFirst(timeLeft)) sorted.Add(strings[i]);
        return sorted;
    }

    // Right click: schedule every idle string oldest first. The strings then run their own countdown.
    internal static void Glissando(Player player)
    {
        List<Projectile> strings = Idle(player.whoAmI);
        int count = Math.Min(strings.Count, EbonRewardRules.MaxStrings);
        for (int i = 0; i < count; i++)
        {
            strings[i].ai[2] = EbonLoomHarpRules.Schedule(i, i == count - 1);
            strings[i].netUpdate = true;
        }
    }

    // A needle ended at `to` after leaving the bow at `from`: the owner leaves a string between them, retiring
    // the oldest idle one when eight already stand. Its damage is the needle's own, once.
    internal static void Spawn(Projectile arrow, Vector2 from, Vector2 to)
    {
        if (arrow.owner < 0 || arrow.owner >= Main.maxPlayers || !EbonRewardItems.Usable(Main.player[arrow.owner])) return;
        if (!InWorld(from) || !InWorld(to) || Vector2.Distance(from, to) < EbonLoomHarpRules.MinStringLength) return;
        List<Projectile> idle = Idle(arrow.owner);
        for (int i = 0, drop = EbonLoomHarpRules.Evictions(idle.Count); i < drop && i < idle.Count; i++) idle[i].Kill();
        int index = Projectile.NewProjectile(arrow.GetSource_FromThis(), from, Vector2.Zero, StringType,
            EbonRewardItems.Hit(arrow.damage, EbonRewardRules.PluckMultiplier), arrow.knockBack * .5f, arrow.owner, to.X, to.Y, 0);
        if (index < 0 || index >= Main.maxProjectiles) return;
        Projectile created = Main.projectile[index];
        created.CritChance = arrow.CritChance;
        created.ArmorPenetration = arrow.ArmorPenetration;
        created.netUpdate = true;
    }

    internal static bool InWorld(Vector2 point)
        => float.IsFinite(point.X) && float.IsFinite(point.Y)
            && point.X >= 0 && point.Y >= 0 && point.X <= Main.maxTilesX * 16f && point.Y <= Main.maxTilesY * 16f;
}

// The silver needle. extraUpdates 1, penetrate 1: it ends on its first NPC, a tile, or ArrowRange px from the bow,
// and the owner leaves a string where it ended. Drawn by the Ebon pixel layer (Client/.../LoomHarpVisuals).
public sealed class EbonNeedleArrow : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.arrow = true;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        // The owner's kill (hit, tile, range) reaches peers, so a replica never flies on past its end.
        Projectile.netImportant = true;
        Projectile.timeLeft = 90;
    }

    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        Vector2 origin = new(Projectile.ai[0], Projectile.ai[1]);
        if (!EbonHarpStrings.InWorld(origin) || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Vector2.DistanceSquared(Projectile.Center, origin) >= EbonRewardRules.ArrowRange * EbonRewardRules.ArrowRange)
            Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
            EbonHarpStrings.Spawn(Projectile, new Vector2(Projectile.ai[0], Projectile.ai[1]), Projectile.Center);
    }
}

// A taut silk string from the fire point (Projectile.Center) to ai[0], ai[1]. ai[2] is its state (see
// EbonLoomHarpRules): idle strings are harmless and fade over their last StringFade ticks; a plucked string hurts
// every NPC crossing it for PluckLive ticks, once, and is spent. Drawn by the Ebon pixel layer.
public sealed class EbonHarpString : ModProjectile
{
    // Roots (realLife or whoAmI) already struck by this pluck, so a segmented NPC counts once. Only the owner
    // damages, and the field stays null in the type's template (clones share references).
    private List<int>? struck;

    public override string Texture => "Terraria/Images/Projectile_1";

    internal Vector2 From => Projectile.Center;
    internal Vector2 To => new(Projectile.ai[0], Projectile.ai[1]);
    internal float State => Projectile.ai[2];

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        // One hit per NPC for the projectile's whole life; the root ledger below covers segmented NPCs.
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = EbonRewardRules.StringLife;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanDamage() => EbonLoomHarpRules.Live(State);

    private bool Sane()
        => EbonHarpStrings.InWorld(From) && EbonHarpStrings.InWorld(To) && EbonLoomHarpRules.Valid(State)
            && Vector2.DistanceSquared(From, To) <= (EbonRewardRules.ArrowRange + 64) * (EbonRewardRules.ArrowRange + 64);

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || !Sane()) { Projectile.Kill(); return; }
        // Down in a Raid mid-glissando: the owner calls the schedule off and the string stands idle again.
        if (State > 0 && Projectile.owner == Main.myPlayer && !EbonRewardItems.Usable(owner))
        {
            Projectile.ai[2] = 0;
            Projectile.netUpdate = true;
            return;
        }
        EbonLoomHarpRules.Step step = EbonLoomHarpRules.Advance(State);
        if (step.Spent) { Projectile.Kill(); return; }
        Projectile.ai[2] = step.State;
        // A string called up by the glissando lives until its pluck on every peer, however old it was.
        if (EbonLoomHarpRules.TryDecode(step.State, out _, out _, out int countdown))
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, countdown + EbonRewardRules.PluckLive + 3);
        if (step.Plucked) Projectile.timeLeft = EbonRewardRules.PluckLive + 2;
    }

    // The pluck band: the segment thickened to PluckWidth, live only while plucked.
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => EbonLoomHarpRules.Live(State) && EbonRewardRules.BoxTouchesSegment(
            new NVector2(targetHitbox.Left, targetHitbox.Top), new NVector2(targetHitbox.Right, targetHitbox.Bottom),
            new NVector2(From.X, From.Y), new NVector2(To.X, To.Y), EbonRewardRules.PluckWidth);

    public override bool? CanHitNPC(NPC target) => struck?.Contains(Root(target)) == true ? false : null;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => (struck ??= new List<int>(4)).Add(Root(target));

    private static int Root(NPC npc) => npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
}
