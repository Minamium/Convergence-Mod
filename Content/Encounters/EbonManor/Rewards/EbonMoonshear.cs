#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Moonshear (REWARDS.md#melee): giant tailor's shears as twin blades. Left click runs the four-stroke
// kata through one held stroke projectile; right click casts the Cut Line. Ordinary owner projectiles
// and native hit processing only: no packet. Client art enters through Client/.../MoonshearVisuals.cs.
public sealed class EbonMoonshear : ModItem
{
    public override string Texture => EbonRewardItems.Icon(nameof(EbonMoonshear), ItemID.BreakerBlade);

    public override void SetDefaults()
    {
        EbonRewardItems.Defaults(Item, EbonRewardKind.Melee);
        Item.width = Item.height = 56;
        Item.useStyle = ItemUseStyleID.Shoot;
        // As Soboro: each stroke owns its fixed clock and the next one starts the tick after it ends.
        Item.useTime = Item.useAnimation = 2;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<MoonshearStroke>();
        Item.shootSpeed = 1f;
    }

    public override bool MeleePrefix() => true;
    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (!EbonRewardItems.Usable(player) || player.noItems || player.CCed
            || MoonshearStroke.Busy(player) || MoonshearCutLine.Cutting(player)) return false;
        bool cut = player.altFunctionUse == 2;
        if (cut && (MoonshearCutLine.Any(player) || !player.GetModPlayer<MoonshearPlayer>().CutReady)) return false;
        // NullRefrain's rule: only the swung shears take true-melee bonuses; the remote cut is melee.
        Item.DamageType = cut ? DamageClass.Melee : EbonRewardItems.DamageClassFor(EbonRewardKind.Melee);
        return true;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer || Main.dedServ) return false;
        var state = player.GetModPlayer<MoonshearPlayer>();
        Vector2 aim = EbonRewardItems.Aim(velocity, player.direction);
        int index;
        if (player.altFunctionUse == 2)
        {
            // The chalk starts at the hand pointing at the cursor; native position + ai carry the line.
            int facing = aim.X < 0 ? -1 : 1;
            var point = OboroHandAnchor.Capture(player, facing).At(aim.ToRotation());
            Vector2 hand = player.MountedCenter + new Vector2(point.X, point.Y), cursor = Main.MouseWorld;
            NVector end = MoonshearMotion.ClampLine(new(hand.X, hand.Y), new(cursor.X, cursor.Y), new(aim.X, aim.Y));
            index = Projectile.NewProjectile(source, hand, Vector2.Zero, ModContent.ProjectileType<MoonshearCutLine>(),
                EbonRewardItems.Hit(damage, EbonRewardRules.CutMultiplier), knockback, player.whoAmI, end.X, end.Y, 0);
            if (index >= 0 && index < Main.maxProjectiles)
            {
                // Owner-only base for the pops, the repo's originalDamage idiom.
                Main.projectile[index].originalDamage = damage;
                Main.projectile[index].netUpdate = true;
                state.StartCutCooldown();
            }
            return false;
        }
        int stroke = state.TakeStroke();
        index = Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
            EbonRewardItems.Hit(damage, MoonshearMotion.Multiplier(stroke)), knockback, player.whoAmI, stroke, aim.ToRotation(), 0);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].netUpdate = true;
        return false;
    }
}

// One kata stroke held at the hand. ai[0] stroke, ai[1] aim, ai[2] age (native snapshots carry all three).
public sealed class MoonshearStroke : ModProjectile
{
    // Client presentation hook (the arpeggio note of a building hit); Content never references Client.
    internal static Action<Projectile, NPC, int>? Marked;
    private readonly HashSet<int> hitRoots = new();

    public override string Texture => EbonRewardItems.Icon(nameof(EbonMoonshear), ItemID.BreakerBlade);
    internal int Stroke => Math.Clamp((int)Projectile.ai[0], 0, MoonshearMotion.Strokes - 1);
    internal float Aim => Projectile.ai[1];
    internal int Age => (int)Projectile.ai[2];
    internal int Facing => MathF.Cos(Aim) < 0 ? -1 : 1;
    internal bool Finished => Age >= MoonshearMotion.Duration(Stroke);
    internal float BladeAngle(float age, bool upper) => MoonshearMotion.BladeAngle(Stroke, age, Aim, Facing, upper);
    internal Vector2 Hand(Player owner, float age)
    {
        var point = OboroHandAnchor.Capture(owner, Facing).At(MoonshearMotion.ArmAngle(Stroke, age, Aim, Facing));
        return owner.MountedCenter + new Vector2(point.X, point.Y);
    }
    internal Vector2 Pivot(Player owner, float age) => Pivot(owner, OboroHandAnchor.Capture(owner, Facing), age);
    private Vector2 Pivot(Player owner, OboroHandBasis hand, float age)
    {
        var point = hand.At(MoonshearMotion.ArmAngle(Stroke, age, Aim, Facing));
        return owner.MountedCenter + new Vector2(point.X, point.Y)
            + MoonshearMotion.AxisAngle(Stroke, age, Aim, Facing).ToRotationVector2() * MoonshearMotion.Thrust(Stroke, age);
    }

    // A finished stroke stays one tick so the next can begin seamlessly from its end pose.
    internal static bool Busy(Player player)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is MoonshearStroke stroke && !stroke.Finished) return true;
        return false;
    }

    internal static int Root(NPC npc) => npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? npc.realLife : npc.whoAmI;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = EbonRewardItems.DamageClassFor(EbonRewardKind.Melee);
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = EbonRewardRules.StrokeImmunity;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = MoonshearMotion.MaximumDuration + 4;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => MoonshearMotion.Live(Stroke, Age) ? null : false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    // Local immunity plus a root ledger: one stroke never multiplies on a segmented body.
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(Root(target)) ? false : null;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!EbonRewardItems.Usable(owner)) { Projectile.Kill(); return; }
        if (owner.HeldItem.type != ModContent.ItemType<EbonMoonshear>())
        {
            // Native held-item sync may trail the projectile on peers; the owner cancels at once.
            if (Projectile.owner == Main.myPlayer || ++Projectile.localAI[1] > 6) Projectile.Kill();
            return;
        }
        Projectile.localAI[1] = 0;
        if (!float.IsFinite(Aim) || !float.IsFinite(Projectile.ai[0]) || Projectile.ai[0] < 0 || Projectile.ai[0] > MoonshearMotion.Strokes - 1
            || !float.IsFinite(Projectile.ai[2]) || Projectile.ai[2] < 0 || Projectile.ai[2] > MoonshearMotion.MaximumDuration + 1)
        { Projectile.Kill(); return; }

        Projectile.ai[2]++;
        if (Age > MoonshearMotion.Duration(Stroke)) { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer && (Age == 1 || Age == MoonshearMotion.Release(Stroke) || Age % 6 == 0))
            Projectile.netUpdate = true;
        Projectile.Center = Pivot(owner, Age);
        Projectile.rotation = MoonshearMotion.AxisAngle(Stroke, Age, Aim, Facing);
        Projectile.direction = Projectile.spriteDirection = Facing;
        Projectile.velocity = Vector2.Zero;
    }

    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        if (!MoonshearMotion.Live(Stroke, Age)) return false;
        Player owner = Main.player[Projectile.owner];
        var hand = OboroHandAnchor.Capture(owner, Facing);
        Vector2 topLeft = targetHitbox.TopLeft(), size = targetHitbox.Size();
        // Soboro's swept capsule: nine sub-samples of the accepted curve across the last tick.
        for (int i = 0; i <= 8; i++)
        {
            float age = Math.Max(MoonshearMotion.Release(Stroke), Age - 1f + i / 8f);
            Vector2 root = Pivot(owner, hand, age);
            for (int blade = 0; blade < 2; blade++)
            {
                bool upper = blade == 0;
                if (!MoonshearMotion.Cuts(Stroke, upper)) continue;
                float collision = 0;
                if (Collision.CheckAABBvLineCollision(topLeft, size, root,
                        root + BladeAngle(age, upper).ToRotationVector2() * EbonRewardRules.ShearReach,
                        EbonRewardRules.ShearWidth, ref collision)
                    && Collision.CanHitLine(root, 1, 1, topLeft, targetHitbox.Width, targetHitbox.Height)) return true;
            }
        }
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitRoots.Add(Root(target));
        if (Projectile.owner != Main.myPlayer || !MoonshearMotion.Marks(Stroke)) return;
        int marks = Main.player[Projectile.owner].GetModPlayer<MoonshearPlayer>().Mark(target);
        if (!Main.dedServ) Marked?.Invoke(Projectile, target, marks);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
}

// The right-click Cut Line. Native position = chalk start (the hand at the cast), ai[0..1] = end,
// ai[2] = age. The tear and touched list are processed by the owner like any player projectile;
// pops are short native children so peers see and hear each one.
public sealed class MoonshearCutLine : ModProjectile
{
    private struct Touch { internal int Index, Type, Root, RootType; internal float Along; }
    private const int MaxTouched = 32;
    private readonly HashSet<int> hitRoots = new();
    private readonly Touch[] touched = new Touch[MaxTouched];
    private readonly Touch[] pops = new Touch[MoonshearMotion.MaxPops];
    private int touchedCount, popCount, popNext, popsFired;

    public override string Texture => EbonRewardItems.Icon(nameof(EbonMoonshear), ItemID.BreakerBlade);
    internal Vector2 Start => Projectile.Center;
    internal Vector2 End => new(Projectile.ai[0], Projectile.ai[1]);
    internal int Age => (int)Projectile.ai[2];
    internal float LineAngle => (End - Start).ToRotation();

    internal static bool Any(Player player) => Find(player, false);
    // Forecast to tear the shears are drawing or racing the line, so no stroke can start.
    internal static bool Cutting(Player player) => Find(player, true);
    private static bool Find(Player player, bool cutting)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is MoonshearCutLine cut && (!cutting || cut.Age < MoonshearMotion.TearEnd)) return true;
        return false;
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)EbonRewardRules.CutRange + 200;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = MoonshearMotion.PopTick(MoonshearMotion.MaxPops) + 30;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => MoonshearMotion.TearLive(Age) ? null : false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(MoonshearStroke.Root(target)) ? false : null;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!EbonRewardItems.Usable(owner)) { Projectile.Kill(); return; }
        if (owner.HeldItem.type != ModContent.ItemType<EbonMoonshear>())
        {
            if (Projectile.owner == Main.myPlayer || ++Projectile.localAI[1] > 6) Projectile.Kill();
            return;
        }
        Projectile.localAI[1] = 0;
        if (!float.IsFinite(Projectile.ai[0]) || !float.IsFinite(Projectile.ai[1]) || !float.IsFinite(Projectile.ai[2])
            || Projectile.ai[2] < 0 || Projectile.ai[2] > MoonshearMotion.PopTick(MoonshearMotion.MaxPops) + 30
            || Vector2.DistanceSquared(Start, End) > MathF.Pow(EbonRewardRules.CutRange + 64, 2))
        { Projectile.Kill(); return; }

        Projectile.ai[2]++;
        Projectile.velocity = Vector2.Zero;
        int age = Age;
        if (Projectile.owner != Main.myPlayer) return;
        if (age == 1 || age == MoonshearMotion.TravelStart || age == MoonshearMotion.TearStart) Projectile.netUpdate = true;
        if (age == MoonshearMotion.TearEnd) Schedule(owner);
        if (age >= MoonshearMotion.TearEnd && (age - MoonshearMotion.TearEnd) % EbonRewardRules.PopSpacing == 0) Pop();
        // Residue lingers a little on peers; the owner ends once every pop has gone out.
        if (age >= MoonshearMotion.TearEnd + 12 && popNext >= popCount) Projectile.Kill();
    }

    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
        => MoonshearMotion.TearLive(Age) && EbonRewardRules.BoxTouchesSegment(new NVector(targetHitbox.Left, targetHitbox.Top),
            new NVector(targetHitbox.Right, targetHitbox.Bottom), ToN(Start), ToN(End), EbonRewardRules.CutWidth);

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        int root = MoonshearStroke.Root(target);
        hitRoots.Add(root);
        if (Projectile.owner != Main.myPlayer || touchedCount >= MaxTouched) return;
        touched[touchedCount++] = new Touch
        {
            Index = target.whoAmI, Type = target.type, Root = root, RootType = Main.npc[root].type,
            Along = MoonshearMotion.Along(ToN(target.Center), ToN(Start), ToN(End)),
        };
    }

    // Order the touched NPCs along the chalk, then queue one pop per mark they carried.
    private void Schedule(Player owner)
    {
        for (int i = 1; i < touchedCount; i++)
        {
            Touch item = touched[i];
            int j = i - 1;
            for (; j >= 0 && touched[j].Along > item.Along; j--) touched[j + 1] = touched[j];
            touched[j + 1] = item;
        }
        var state = owner.GetModPlayer<MoonshearPlayer>();
        for (int i = 0; i < touchedCount; i++)
        {
            int marks = state.TakeMarks(touched[i].Root, touched[i].RootType);
            for (int m = 0; m < marks && popCount < pops.Length; m++) pops[popCount++] = touched[i];
        }
    }

    private void Pop()
    {
        while (popNext < popCount)
        {
            Touch entry = pops[popNext++];
            if (!Target(entry, out NPC? npc)) continue;
            bool last = true;
            for (int i = popNext; i < popCount && last; i++) last = !Target(pops[i], out _);
            int baseDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage
                : (int)(Projectile.damage / EbonRewardRules.CutMultiplier);
            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), npc!.Center, Vector2.Zero,
                ModContent.ProjectileType<MoonshearPop>(), EbonRewardItems.Hit(baseDamage, EbonRewardRules.PopMultiplier),
                0, Projectile.owner, npc.whoAmI, popsFired++, last ? 1 : 0);
            if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].netUpdate = true;
            return;
        }
    }

    // The touched segment, or its body's root when that segment is gone.
    private static bool Target(Touch entry, out NPC? npc)
    {
        npc = Main.npc[entry.Index];
        if (npc.active && npc.life > 0 && npc.type == entry.Type) return true;
        npc = Main.npc[entry.Root];
        return npc.active && npc.life > 0 && npc.type == entry.RootType;
    }

    private static NVector ToN(Vector2 v) => new(v.X, v.Y);

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
}

// One mark bursting on one NPC. ai[0] = NPC slot, ai[1] = arpeggio step, ai[2] = 1 on the last pop.
// It can only strike that NPC, once; it lingers a few ticks so peers always receive and show it.
public sealed class MoonshearPop : ModProjectile
{
    internal int Target => (int)Projectile.ai[0];
    internal int Step => (int)Projectile.ai[1];
    internal bool Last => Projectile.ai[2] >= 1;

    public override string Texture => EbonRewardItems.Icon(nameof(EbonMoonshear), ItemID.BreakerBlade);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = 10;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => target.whoAmI == Target ? null : false;
    // CanHitNPC already restricts the strike to the marked NPC: wherever it is, the pop lands.
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox) => true;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !float.IsFinite(Projectile.ai[0])
            || !float.IsFinite(Projectile.ai[1]) || Target < 0 || Target >= Main.maxNPCs)
        { Projectile.Kill(); return; }
        Projectile.velocity = Vector2.Zero;
        NPC npc = Main.npc[Target];
        if (npc.active) Projectile.Center = npc.Center;
    }
}

// Owner-side bookkeeping: the kata counter, Cut Line cooldown and chalk marks (visual for the owner only).
public sealed class MoonshearPlayer : ModPlayer
{
    private readonly MoonshearCombo combo = new();
    private MoonshearMarks? marks;
    private ulong cutReadyAt;

    internal MoonshearMarks? Marks => marks;
    internal bool CutReady
    {
        get
        {
            ulong now = Main.GameUpdateCount;
            // A clock that went backwards (new world) never strands the cooldown.
            return now >= cutReadyAt || cutReadyAt - now > (ulong)MoonshearMotion.Cooldown;
        }
    }

    internal int TakeStroke() => combo.Take(Main.GameUpdateCount);
    internal void StartCutCooldown() => cutReadyAt = Main.GameUpdateCount + (ulong)MoonshearMotion.Cooldown;

    internal int Mark(NPC target)
    {
        int root = MoonshearStroke.Root(target);
        return (marks ??= new MoonshearMarks(Main.maxNPCs)).Add(root, Main.npc[root].type, Main.GameUpdateCount);
    }
    internal int TakeMarks(int root, int type) => marks?.Take(root, type) ?? 0;

    public override void PostUpdate()
    {
        if (Player.HeldItem.type != ModContent.ItemType<EbonMoonshear>()) combo.Reset();
        if (marks is null || Player.whoAmI != Main.myPlayer) return;
        ulong now = Main.GameUpdateCount;
        for (int i = 0; i < marks.Capacity; i++)
        {
            if (marks.Count(i) == 0) continue;
            NPC npc = Main.npc[i];
            marks.Expire(i, npc.active && npc.life > 0 ? npc.type : -1, now);
        }
    }

    public override void UpdateDead() { combo.Reset(); marks?.Clear(); }
    public override void OnEnterWorld() { combo.Reset(); marks?.Clear(); cutReadyAt = 0; }
}
