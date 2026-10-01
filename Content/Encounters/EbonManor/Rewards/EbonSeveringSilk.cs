#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Num = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Severing Silk / 断ち糸 (REWARDS.md "Rogue"). Throws spools that leave pinned silk strands; a stealth
// strike throws bird-shaped embroidery scissors that tighten and then part every strand at once.
// Ownership: the owner client alone throws and spawns; spool, strand and scissors are native netImportant
// projectiles, so peers draw the same state with no new packet. The sever timeline runs on a per-client
// tick stamp taken the first time a client sees the arming, never on arrival order of the projectiles.
internal static class EbonSilkUtil
{
    internal static Num N(Vector2 v) => new(v.X, v.Y);
    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
    // Segmented bodies share one root; a root is hit once per release, as in the Doll rewards.
    internal static int Root(NPC npc) => npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
    internal static long Now => (long)Main.GameUpdateCount;
    internal static bool Sane(Projectile p) => p.owner >= 0 && p.owner < Main.maxPlayers
        && float.IsFinite(p.ai[0]) && float.IsFinite(p.ai[1]) && float.IsFinite(p.ai[2])
        && Finite(p.position) && Finite(p.velocity);
}

public sealed class EbonSeveringSilk : CalamityRogueArmament
{
    public override string Texture => EbonRewardItems.Icon(nameof(EbonSeveringSilk), ItemID.Shuriken);

    public override void SetDefaults()
    {
        EbonRewardItems.Defaults(Item, EbonRewardKind.Rogue);
        Item.DamageType = RogueClass;
        Item.width = 30; Item.height = 34;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.autoReuse = true; Item.noMelee = true; Item.noUseGraphic = true;
        Item.shootSpeed = EbonRewardRules.SpoolSpeed;
        Item.shoot = ModContent.ProjectileType<EbonSilkSpool>();
    }

    public override bool CanUseItem(Player player) => EbonRewardItems.Usable(player);

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool stealth = HasStealthStrike(player);
        Vector2 aim = EbonRewardItems.Aim(velocity, player.direction);
        Vector2 center = player.MountedCenter, origin = center + aim * 16;
        if (!Collision.CanHitLine(center, 1, 1, origin, 1, 1)) origin = center;
        int index;
        if (stealth)
        {
            // The scissors fly to the cursor: ai[1] is the flight length in ticks (reach quantised to 24 px).
            int flight = EbonSilkMath.FlightTicks(Vector2.Distance(origin, Main.MouseWorld));
            index = Projectile.NewProjectile(source, origin, aim, ModContent.ProjectileType<EbonSilkScissors>(),
                damage, knockback, player.whoAmI, 0, flight, 0);
        }
        else
            index = Projectile.NewProjectile(source, origin, aim * EbonRewardRules.SpoolSpeed,
                ModContent.ProjectileType<EbonSilkSpool>(), damage, knockback, player.whoAmI, origin.X, origin.Y, 0);
        MarkStealthStrike(index, stealth);
        return false;
    }
}

// The thrown spool: ai[0..1] = throw origin, where the strand starts. Pierces one NPC (x1.0) and stops at
// the second, a tile or timeout; the owner then pins a strand from the origin to where it stopped.
public sealed class EbonSilkSpool : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    private bool abort;
    internal Vector2 Origin => new(Projectile.ai[0], Projectile.ai[1]);
    public override string Texture => "Terraria/Images/Projectile_1";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true; Projectile.DamageType = EbonRewardItems.DamageClassFor(EbonRewardKind.Rogue);
        Projectile.penetrate = 2; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.timeLeft = EbonRewardRules.SpoolLife; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(EbonSilkUtil.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(EbonSilkUtil.Root(target));
    public override bool OnTileCollide(Vector2 oldVelocity) => true;

    public override void AI()
    {
        if (!EbonSilkUtil.Sane(Projectile) || !EbonRewardItems.Usable(Main.player[Projectile.owner]))
        { abort = true; Projectile.Kill(); return; }
        if (Projectile.velocity.Y < 16f) Projectile.velocity.Y += EbonRewardRules.SpoolGravity;
        Projectile.rotation += .45f * (Projectile.velocity.X >= 0 ? 1 : -1);
    }

    public override void OnKill(int timeLeft)
    {
        if (abort || Projectile.owner != Main.myPlayer) return;
        EbonSilkStrand.Pin(Projectile.GetSource_FromThis(), Origin, Projectile.Center, Projectile.damage, Projectile.knockBack, Projectile.owner);
    }
}

// A pinned silk strand. Position = origin, ai[0..1] = end point, ai[2] = state: 0 pinned (harmless), 1 armed
// by the scissors (tighten, part, recoil), -1 retired to make room for a newer strand. ExtraAI carries the
// sever rank (ripple order), the number of strands severed together and the cut point along the strand.
public sealed class EbonSilkStrand : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    private long armStamp = -1, retireStamp = -1;
    private int rank, count;
    private float cut = .5f;
    private bool resync;
    public override string Texture => "Terraria/Images/Projectile_1";

    internal Vector2 End => new(Projectile.ai[0], Projectile.ai[1]);
    internal int State => (int)Projectile.ai[2];
    // 1 on the arming tick; the strands of one sever share it exactly (see EbonSilkMath.SeverPhase).
    internal int Phase => armStamp < 0 ? 0 : (int)(EbonSilkUtil.Now - armStamp) + 1;
    internal int RetiredFor => retireStamp < 0 ? -1 : (int)(EbonSilkUtil.Now - retireStamp);
    internal int Rank => rank;
    internal int Count => count;
    internal float Cut => cut;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = EbonRewardItems.DamageClassFor(EbonRewardKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = EbonRewardRules.StrandLife; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    // Harmless until severed; live for two ticks, once per root.
    public override bool? CanDamage() => EbonSilkMath.IsLive(Phase) ? null : false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(EbonSilkUtil.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(EbonSilkUtil.Root(target));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => EbonRewardRules.BoxTouchesSegment(new Num(targetHitbox.Left, targetHitbox.Top), new Num(targetHitbox.Right, targetHitbox.Bottom),
            EbonSilkUtil.N(Projectile.Center), EbonSilkUtil.N(End), EbonRewardRules.StrandWidth);

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)rank); writer.Write((byte)count); writer.Write((byte)MathF.Round(cut * 255f));
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        rank = Math.Min((int)reader.ReadByte(), EbonRewardRules.MaxStrands - 1);
        count = Math.Min((int)reader.ReadByte(), EbonRewardRules.MaxStrands);
        cut = Math.Clamp(reader.ReadByte() / 255f, EbonSilkMath.CutMin, EbonSilkMath.CutMax);
    }

    public override void AI()
    {
        if (!EbonSilkUtil.Sane(Projectile) || !EbonSilkMath.Worthy(EbonSilkUtil.N(Projectile.Center), EbonSilkUtil.N(End))
            || !EbonRewardItems.Usable(Main.player[Projectile.owner]))
        { Projectile.Kill(); return; }
        // Another projectile (the scissors) changed this one's state, possibly after this tick's update already
        // ran, and native Update clears netUpdate first: publish from our own AI so the flag survives.
        if (resync && Projectile.owner == Main.myPlayer) { resync = false; Projectile.netUpdate = true; }
        if (armStamp < 0 && Projectile.ai[2] >= 1) armStamp = EbonSilkUtil.Now;
        if (retireStamp < 0 && Projectile.ai[2] < 0) retireStamp = EbonSilkUtil.Now;
        // The carrier outlives its natural timeout until the sever or retirement has played out.
        if (armStamp >= 0 || retireStamp >= 0) Projectile.timeLeft = Math.Max(Projectile.timeLeft, 3);
        if (Phase >= EbonSilkMath.EndPhase || RetiredFor >= EbonSilkMath.RetireTicks) Projectile.Kill();
    }

    // Owner only: the scissors tighten this strand and fix its place in the ripple.
    internal void Arm(int rank, int count, float cut, int damage)
    {
        this.rank = rank; this.count = count; this.cut = cut;
        Projectile.damage = damage;
        Projectile.ai[2] = 1; armStamp = EbonSilkUtil.Now;
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, EbonSilkMath.EndPhase + 3);
        Projectile.netUpdate = resync = true;
    }
    internal void Retire()
    {
        Projectile.ai[2] = -1; retireStamp = EbonSilkUtil.Now;
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, EbonSilkMath.RetireTicks + 3);
        Projectile.netUpdate = resync = true;
    }

    // Owner only: pin a strand from a to b. Past MaxStrands the oldest pinned strand is retired.
    internal static void Pin(IEntitySource source, Vector2 a, Vector2 b, int damage, float knockback, int owner)
    {
        if (!EbonRewardItems.Usable(Main.player[owner]) || !EbonSilkMath.Worthy(EbonSilkUtil.N(a), EbonSilkUtil.N(b))) return;
        int type = ModContent.ProjectileType<EbonSilkStrand>();
        var pinned = new List<Projectile>();
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.type == type && p.ModProjectile is EbonSilkStrand { State: 0 }) pinned.Add(p);
        while (pinned.Count >= EbonRewardRules.MaxStrands)
        {
            var left = new int[pinned.Count]; var any = new bool[pinned.Count];
            for (int i = 0; i < left.Length; i++) { left[i] = pinned[i].timeLeft; any[i] = true; }
            int oldest = EbonSilkMath.Oldest(left, any);
            ((EbonSilkStrand)pinned[oldest].ModProjectile).Retire();
            pinned.RemoveAt(oldest);
        }
        Projectile.NewProjectile(source, a, Vector2.Zero, type, damage, knockback, owner, b.X, b.Y, 0);
    }
}

// The stealth strike: bird-shaped embroidery scissors. ai[0] = age in ticks, ai[1] = flight length in ticks
// (shortened by the owner when a tile stops them). They cruise toward the cursor, brake, gape, then at the
// arming tick tighten every owner strand; the beak shuts on the sever phase and the 80 px snip hits.
public sealed class EbonSilkScissors : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    private long snipStamp = -1;
    private int baseDamage;
    private Vector2 heading;
    public override string Texture => "Terraria/Images/Projectile_1";

    internal int Age => (int)Projectile.ai[0];
    internal int Flight => Math.Clamp((int)Projectile.ai[1], 1, EbonRewardRules.ScissorsLife);
    // 0 while flying; 1 on the arming tick, shared with the strands armed on that tick.
    internal int Phase => snipStamp < 0 ? 0 : (int)(EbonSilkUtil.Now - snipStamp) + 1;
    internal Vector2 Heading => heading == Vector2.Zero ? Projectile.rotation.ToRotationVector2() : heading;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true; Projectile.DamageType = EbonRewardItems.DamageClassFor(EbonRewardKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.timeLeft = EbonRewardRules.ScissorsLife + EbonSilkMath.ScissorsEndPhase + 30; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    // The flight is harmless; the snip hits for two ticks (the same window as the strands), once per root.
    public override bool? CanDamage() => EbonSilkMath.IsLive(Phase) ? null : false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(EbonSilkUtil.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(EbonSilkUtil.Root(target));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => EbonSilkMath.CircleTouchesBox(EbonSilkUtil.N(Projectile.Center), EbonRewardRules.ScissorsRadius,
            new Num(targetHitbox.Left, targetHitbox.Top), new Num(targetHitbox.Right, targetHitbox.Bottom));

    // A tile stops the scissors where they are; the owner ends the flight and the others follow ai[1].
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.owner == Main.myPlayer && snipStamp < 0 && Age < Flight)
        { Projectile.ai[1] = Projectile.ai[0]; Projectile.netUpdate = true; }
        Projectile.velocity = Vector2.Zero;
        return false;
    }

    public override void AI()
    {
        if (!EbonSilkUtil.Sane(Projectile) || !EbonRewardItems.Usable(Main.player[Projectile.owner]))
        { Projectile.Kill(); return; }
        if (heading == Vector2.Zero)
        {
            heading = Projectile.velocity.LengthSquared() > .001f ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
            baseDamage = Projectile.damage;
        }
        int tick = Age;
        Projectile.ai[0]++;
        Projectile.rotation = heading.ToRotation();
        Projectile.velocity = heading * EbonSilkMath.FlightStep(tick, Flight);
        if (snipStamp < 0 && Age >= Flight)
        {
            snipStamp = EbonSilkUtil.Now;
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.damage = EbonRewardItems.Hit(baseDamage, EbonRewardRules.ScissorsMultiplier);
                ArmStrands();
                Projectile.netUpdate = true;
            }
        }
        if (snipStamp >= 0) Projectile.timeLeft = Math.Max(Projectile.timeLeft, 3);
        if (Phase >= EbonSilkMath.ScissorsEndPhase) Projectile.Kill();
    }

    // Every pinned strand of the owner tightens now; the ones whose cut lies nearest the scissors part
    // first in the ripple, but all go live on the same tick with x SeverMultiplier of the live damage.
    private void ArmStrands()
    {
        int type = ModContent.ProjectileType<EbonSilkStrand>();
        var found = new List<(EbonSilkStrand Strand, float Cut, float Distance)>();
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Projectile.owner || p.type != type || p.ModProjectile is not EbonSilkStrand { State: 0 } strand) continue;
            float cut = EbonSilkMath.CutParameter(EbonSilkUtil.N(p.Center), EbonSilkUtil.N(strand.End), EbonSilkUtil.N(Projectile.Center));
            found.Add((strand, cut, Vector2.Distance(Vector2.Lerp(p.Center, strand.End, cut), Projectile.Center)));
        }
        if (found.Count == 0) return;
        var distance = new float[found.Count]; var rank = new int[found.Count];
        for (int i = 0; i < distance.Length; i++) distance[i] = found[i].Distance;
        EbonSilkMath.Rank(distance, rank);
        int damage = EbonRewardItems.Hit(baseDamage, EbonRewardRules.SeverMultiplier);
        for (int i = 0; i < found.Count; i++) found[i].Strand.Arm(rank[i], found.Count, found[i].Cut, damage);
    }
}
