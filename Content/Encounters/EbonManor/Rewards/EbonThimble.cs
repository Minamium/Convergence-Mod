#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;
using Score = Convergence.Content.Encounters.EbonManor.Rewards.EbonThimbleScore;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Ebon Thimble (docs/encounters/ebon-manor/REWARDS.md#magic). Hold: silk from the fingertips lifts one
// piece of the manor's furniture per beat (12 mana each). Release: the pieces are yanked at the cursor one
// per sixteenth, and a full set of eight brings a grand piano down on it.
// Ownership: only the owner client samples the cursor, spends mana and spawns/changes pieces; every state
// change travels in the ordinary projectile sync (netImportant + netUpdate). Peers derive all motion from
// that state. Art and sound live in Client/Encounters/EbonManor/Rewards/ThimbleVisuals.cs.
public sealed class EbonThimble : ModItem
{
    public override string Texture => EbonRewardItems.Icon(nameof(EbonThimble), ItemID.BandofRegeneration);

    public override void SetDefaults()
    {
        EbonRewardItems.Defaults(Item, EbonRewardKind.Magic);
        Item.width = 24; Item.height = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true; Item.noUseGraphic = true; Item.autoReuse = false; Item.channel = true;
        Item.mana = EbonRewardRules.ManaPerPiece; Item.shootSpeed = 1;
        Item.shoot = ModContent.ProjectileType<EbonThimbleChannel>();
    }

    public override bool CanUseItem(Player player)
        => EbonRewardItems.Usable(player) && player.ownedProjectileCounts[Item.shoot] == 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Projectile.NewProjectile(source, player.MountedCenter, EbonRewardItems.Aim(velocity, player.direction),
            type, damage, knockback, player.whoAmI);
        return false;
    }
}

internal static class ThimbleSupport
{
    internal static Vector2 Xna(NVector v) => new(v.X, v.Y);
    internal static NVector Num(Vector2 v) => new(v.X, v.Y);
    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
    // Wraps the bob/sway clocks well inside float precision; the periods divide it closely enough to be invisible.
    internal static float Clock => (float)(Main.GameUpdateCount % 6283UL);
    internal static bool ValidOwner(Projectile p) => p.owner >= 0 && p.owner < Main.maxPlayers;
}

// The held controller. Never damages. ai[0] ticks (since use start; since the release once Releasing),
// ai[1] pieces lifted, ai[2] phase (EbonThimbleScore.Channeling / Spent / Releasing); velocity is the aim axis,
// rotation the front-arm direction both of which every client recomputes the same way.
public sealed class EbonThimbleChannel : ModProjectile
{
    internal int Age => (int)Projectile.ai[0];
    internal int Lifted => (int)Projectile.ai[1];
    internal int Phase => (int)Projectile.ai[2];
    private bool IsOwner => Projectile.owner == Main.myPlayer;

    public override string Texture => "Terraria/Images/Projectile_1";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = false; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    // The fingertip silk leaves from: the same arm pose every client has just set.
    internal static Vector2 Hand(Projectile controller)
    {
        Player owner = Main.player[controller.owner];
        // A dedicated server has no arm frames to measure and nobody to look at the silk.
        return Main.dedServ ? owner.MountedCenter
            : owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, controller.rotation - MathHelper.PiOver2);
    }

    internal static Projectile? Find(int owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is EbonThimbleChannel) return p;
        return null;
    }

    public override void AI()
    {
        if (!ThimbleSupport.ValidOwner(Projectile) || !Score.ValidController(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2])
            || !ThimbleSupport.Finite(Projectile.velocity)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        int phase = Phase;
        // The volley outlives an item swap (the thrown pieces are already committed); holding does not.
        bool stopped = !EbonRewardItems.Usable(owner) || owner.noItems || owner.CCed
            || (phase != Score.Releasing && owner.HeldItem.type != ModContent.ItemType<EbonThimble>());
        if (stopped)
        {
            if (IsOwner) Stop();
            return;
        }

        Vector2 axis = EbonRewardItems.Aim(Projectile.velocity, owner.direction);
        if (IsOwner)
        {
            Vector2 wanted = EbonRewardItems.Aim(Main.MouseWorld - owner.MountedCenter, owner.direction);
            if (Vector2.Dot(wanted, axis) < .995f || Age % 15 == 0) Projectile.netUpdate = true;
            axis = wanted; Projectile.velocity = axis;
            owner.manaRegenDelay = Math.Max(owner.manaRegenDelay, 60);
        }
        owner.ChangeDir(axis.X >= 0 ? 1 : -1);
        Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter, true);
        Projectile.timeLeft = 2;
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        bool releasing = phase == Score.Releasing;
        float kick = releasing ? Score.LaunchKick(Age, Lifted) : Score.LiftKick(Age, Lifted);
        Projectile.rotation = Score.ArmAngle(owner.direction, axis.ToRotation(), releasing ? Score.Throw(Age, Lifted) : 0, kick, Age);
        owner.itemRotation = (axis * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        Projectile.ai[0]++;

        if (!IsOwner) return;
        if (releasing)
        {
            if (Age >= Score.VolleyEnd(Lifted)) Projectile.Kill();
            return;
        }
        if (!owner.channel) { Release(owner); return; }
        if (phase == Score.Channeling && Score.LiftDue(Age, Lifted)) Lift(owner);
    }

    // Item change, death, Down or a stun while holding: the raised pieces fall as cut silk.
    private void Stop()
    {
        if (Phase != Score.Releasing) EbonThimblePiece.DropHanging(Projectile.owner);
        Projectile.Kill();
    }

    private void Lift(Player owner)
    {
        int index = Lifted;
        // The first piece is paid by the native use (Item.mana); each further one is paid as it rises.
        // No automatic mana potion; when the mana runs out the silk simply stops lifting.
        if (index > 0 && !owner.CheckMana(owner.HeldItem, owner.GetManaCost(owner.HeldItem), pay: true, blockQuickMana: true))
        { Spend(); return; }
        int damage = EbonRewardItems.Hit(owner.GetWeaponDamage(owner.HeldItem), EbonRewardRules.PieceMultiplier);
        int slot = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Hand(Projectile), Vector2.Zero,
            ModContent.ProjectileType<EbonThimblePiece>(), damage, Projectile.knockBack, Projectile.owner, index, Score.Hanging, 0);
        if (slot < 0 || slot >= Main.maxProjectiles) { Spend(); return; }
        Projectile.ai[1] = index + 1;
        Projectile.netUpdate = true;
    }

    private void Spend()
    {
        Projectile.ai[2] = Score.Spent;
        Projectile.netUpdate = true;
    }

    private void Release(Player owner)
    {
        int lifted = Lifted;
        if (lifted <= 0) { Projectile.Kill(); return; }
        foreach (Projectile other in Main.ActiveProjectiles)
            if (other.owner == Projectile.owner && other.ModProjectile is EbonThimblePiece { State: Score.Hanging } piece) piece.Arm();
        if (Score.Finale(lifted))
        {
            var (start, targetY) = Score.PianoSpawn(ThimbleSupport.Num(Main.MouseWorld), Main.maxTilesX * 16f, Main.maxTilesY * 16f);
            int damage = EbonRewardItems.Hit(owner.GetWeaponDamage(owner.HeldItem), EbonRewardRules.PianoMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), ThimbleSupport.Xna(start), Vector2.Zero,
                ModContent.ProjectileType<EbonThimblePiano>(), damage, Projectile.knockBack * 2, Projectile.owner, 0, targetY, 0);
        }
        Projectile.ai[0] = 0; Projectile.ai[2] = Score.Releasing;
        Projectile.netUpdate = true;
    }
}

// One raised piece of furniture. ai[0] piece index, ai[1] state (EbonThimbleScore.Hanging / Armed / Flying /
// Crashed / Dropped), ai[2] ticks in the state. Hanging and armed pieces follow the owner on a fan slot (computed
// from the owner's synced position, so peers need no packets); the owner alone launches, crashes and drops.
public sealed class EbonThimblePiece : ModProjectile
{
    private Vector2 tickStart;
    internal int Index => Math.Clamp((int)Projectile.ai[0], 0, EbonRewardRules.Pieces - 1);
    internal int State => (int)Projectile.ai[1];
    internal int Age => (int)Projectile.ai[2];
    private bool IsOwner => Projectile.owner == Main.myPlayer;

    public override string Texture => "Terraria/Images/Projectile_1";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 64;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 120;
        // Each piece is its own projectile and strikes the first body it meets exactly once.
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => State is Score.Flying or Score.Dropped;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => State == Score.Flying ? null : false;
    public override bool? CanHitNPC(NPC target) => State == Score.Flying ? null : false;
    public override bool PreDraw(ref Color lightColor) => false;
    // Bodies are hit with the full 64 px box; walls only stop the compact core, so a piece can still follow a corridor.
    private const int Core = 40;
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    { width = height = Core; fallThrough = false; return true; }
    private bool InsideTiles() => Collision.SolidCollision(Projectile.Center - new Vector2(Core * .5f), Core, Core);

    // Swept from where the tick began so a 40 px/tick piece cannot pass through a thin body.
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (State != Score.Flying) return false;
        float point = 0;
        return projHitbox.Intersects(targetHitbox)
            || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), tickStart, Projectile.Center, Projectile.width, ref point);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (IsOwner && State == Score.Flying) Crash();
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (IsOwner && State == Score.Flying) Crash();
        return false;
    }

    internal void Arm()
    {
        Projectile.ai[1] = Score.Armed; Projectile.ai[2] = 0;
        Projectile.netUpdate = true;
    }
    internal void Crash()
    {
        Projectile.ai[1] = Score.Crashed; Projectile.ai[2] = 0;
        Projectile.velocity = Vector2.Zero; Projectile.tileCollide = false;
        Projectile.netUpdate = true;
    }
    internal void Drop()
    {
        Projectile.ai[1] = Score.Dropped; Projectile.ai[2] = 0;
        Projectile.velocity = new Vector2(Projectile.velocity.X * .3f, 0); Projectile.tileCollide = false;
        Projectile.netUpdate = true;
    }
    internal static void DropHanging(int owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is EbonThimblePiece piece && piece.State is Score.Hanging or Score.Armed) piece.Drop();
    }

    public override void AI()
    {
        tickStart = Projectile.Center;
        if (!ThimbleSupport.ValidOwner(Projectile) || !Score.ValidPiece(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2])
            || !ThimbleSupport.Finite(Projectile.velocity) || !ThimbleSupport.Finite(Projectile.Center)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Death, Down or a stun: the silk is cut wherever the piece is.
        if (IsOwner && (State is Score.Hanging or Score.Armed or Score.Flying) && !EbonRewardItems.Usable(owner)) Drop();
        int before = State;
        switch (State)
        {
            case Score.Hanging:
            case Score.Armed: Hang(owner); break;
            case Score.Flying: Fly(owner); break;
            case Score.Crashed: Settle(); break;
            default: Fall(); break;
        }
        if (State == before) Projectile.ai[2]++;
    }

    private static Vector2 Hand(Player owner, Projectile? controller)
    {
        Vector2 hand = controller is null ? owner.MountedCenter : EbonThimbleChannel.Hand(controller);
        return ThimbleSupport.Finite(hand) ? hand : owner.Center;
    }

    private void Hang(Player owner)
    {
        Projectile.timeLeft = 120; Projectile.tileCollide = false;
        float clock = ThimbleSupport.Clock;
        Vector2 target = owner.Center + ThimbleSupport.Xna(Score.Slot(Index, owner.direction, clock));
        Projectile? controller = EbonThimbleChannel.Find(Projectile.owner);
        Vector2 hand = Hand(owner, controller);
        // Rise from the fingertip with a small overshoot, then trail the owner on a soft spring.
        Vector2 center = State == Score.Hanging && Age < Score.LiftEaseTicks
            ? Vector2.Lerp(hand, target, Score.LiftEase(Age))
            : Vector2.Lerp(Projectile.Center, target, .2f);
        if (State == Score.Armed) center = Vector2.Lerp(center, hand, .12f * Score.Wind(Age, Index));
        Projectile.velocity = center - Projectile.Center;
        Projectile.Center = center;
        Projectile.rotation = Score.Sway(Index, clock, Projectile.velocity.X);
        if (!IsOwner) return;
        RefreshDamage(owner);
        // Pieces cannot outlive the hold that raised them; the release arms them first.
        if (State == Score.Hanging && controller is null) Drop();
        else if (State == Score.Armed && Score.LaunchDue(Age, Index)) Launch(owner);
    }

    // The yank samples the cursor at the moment each piece leaves, so sweeping the mouse rakes the volley.
    private void Launch(Player owner)
    {
        Vector2 aim = EbonRewardItems.Aim(Main.MouseWorld - Projectile.Center, owner.direction);
        Projectile.velocity = aim * EbonRewardRules.YankSpeed;
        Projectile.ai[1] = Score.Flying; Projectile.ai[2] = 0;
        Projectile.tileCollide = !InsideTiles();
        Projectile.netUpdate = true;
    }

    private void Fly(Player owner)
    {
        Projectile.timeLeft = 30;
        float speed = Projectile.velocity.Length();
        if (IsOwner)
        {
            RefreshDamage(owner);
            // Out of range, or wedged against a tile edge the collision callback never saw.
            if (Age >= Score.MaxFlight || (Age > 2 && speed < EbonRewardRules.YankSpeed * .5f)) { Crash(); return; }
        }
        if (speed > .001f) Projectile.velocity *= Score.NextSpeed(speed) / speed;
        // A piece raised inside a wall passes through until it is clear instead of dying at once.
        Projectile.tileCollide = Age >= 6 || !InsideTiles();
        Projectile.rotation += Score.Spin(Index);
    }

    private void Settle()
    {
        Projectile.velocity = Vector2.Zero; Projectile.tileCollide = false; Projectile.timeLeft = 30;
        if (Age >= Score.CrashLife + (IsOwner ? 0 : 30)) Projectile.Kill();
    }

    private void Fall()
    {
        Projectile.timeLeft = 30; Projectile.tileCollide = false;
        Projectile.velocity = new Vector2(Projectile.velocity.X * .98f, Math.Min(14f, Projectile.velocity.Y + .5f));
        Projectile.rotation += Score.Spin(Index) * .5f;
        if (Age >= Score.DropLife) Projectile.Kill();
    }

    // Always the live weapon: equipment and Mana Sickness changes apply to pieces already in the air.
    private void RefreshDamage(Player owner)
    {
        if (owner.HeldItem.type == ModContent.ItemType<EbonThimble>())
            Projectile.damage = EbonRewardItems.Hit(owner.GetWeaponDamage(owner.HeldItem), EbonRewardRules.PieceMultiplier);
    }
}

// The finale. ai[0] ticks since the release, ai[1] the height it lands at (the release cursor), ai[2] 0 while
// alive, n > 0 crashed for n - 1 ticks, n < 0 dropped for -n - 1 ticks. It waits unseen where the silk will
// drop it, appears PianoHeight above the cursor on EbonRewardRules.PianoTick(8), falls, and crashes on a tile or
// at the cursor height. Only the crash damages: PianoRadius, once per NPC root (segments share a root).
public sealed class EbonThimblePiano : ModProjectile
{
    private readonly HashSet<int> struckRoots = new();
    internal int Age => (int)Projectile.ai[0];
    internal float TargetY => Projectile.ai[1];
    internal bool Crashed => Projectile.ai[2] >= 1;
    internal bool Dropped => Projectile.ai[2] < 0;
    internal int CrashAge => (int)Projectile.ai[2] - 1;
    internal int DropAge => -(int)Projectile.ai[2] - 1;
    internal bool Pending => !Crashed && !Dropped && Age < Score.PianoAppears;
    internal int FallAge => Age - Score.PianoAppears;
    private bool IsOwner => Projectile.owner == Main.myPlayer;

    public override string Texture => "Terraria/Images/Projectile_1";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1400;

    public override void SetDefaults()
    {
        Projectile.width = 120; Projectile.height = 56;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 120;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => !Crashed;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => Crashed && CrashAge <= 1 ? null : false;
    public override bool? CanHitNPC(NPC target) => struckRoots.Contains(RitualTargeting.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => struckRoots.Add(RitualTargeting.Root(target));
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    { fallThrough = false; return true; }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Crashed && Score.CircleTouchesBox(ThimbleSupport.Num(Projectile.Center), EbonRewardRules.PianoRadius,
            ThimbleSupport.Num(targetHitbox.TopLeft()), ThimbleSupport.Num(targetHitbox.BottomRight()));

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (IsOwner && !Crashed && !Dropped && !Pending) Crash();
        return false;
    }

    private void Crash()
    {
        Projectile.velocity = Vector2.Zero; Projectile.tileCollide = false;
        Projectile.ai[2] = 1;
        Projectile.netUpdate = true;
    }

    public override void AI()
    {
        if (!ThimbleSupport.ValidOwner(Projectile) || !Score.ValidPiano(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2])
            || !ThimbleSupport.Finite(Projectile.velocity) || !ThimbleSupport.Finite(Projectile.Center)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        Projectile.timeLeft = 120;
        if (Crashed)
        {
            Projectile.velocity = Vector2.Zero; Projectile.tileCollide = false;
            Projectile.ai[2]++;
            if (CrashAge >= Score.PianoCrashLife + (IsOwner ? 0 : 30)) Projectile.Kill();
            return;
        }
        if (Dropped)
        {
            Projectile.ai[2]--; Projectile.tileCollide = false;
            Projectile.velocity = new Vector2(0, Math.Min(18f, Projectile.velocity.Y + .4f));
            if (DropAge >= Score.PianoFade) Projectile.Kill();
            return;
        }
        // Death, Down or a stun: an unseen piano never arrives; a falling one fades.
        if (IsOwner && !EbonRewardItems.Usable(owner))
        {
            if (Pending) { Projectile.Kill(); return; }
            Projectile.ai[2] = -1; Projectile.netUpdate = true;
            return;
        }
        Projectile.ai[0]++;
        if (Pending)
        {
            Projectile.velocity = Vector2.Zero; Projectile.tileCollide = false;
            return;
        }
        Projectile.velocity = new Vector2(0, FallAge <= 0 ? EbonRewardRules.PianoSpeed : Score.NextFall(Projectile.velocity.Y));
        // The silk may drop it from inside rock (a cave ceiling above the cursor): it passes until it is in the open.
        Projectile.tileCollide = !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
        if (!IsOwner) return;
        if (owner.HeldItem.type == ModContent.ItemType<EbonThimble>())
            Projectile.damage = EbonRewardItems.Hit(owner.GetWeaponDamage(owner.HeldItem), EbonRewardRules.PianoMultiplier);
        if (Score.PianoLanded(Projectile.Bottom.Y + Projectile.velocity.Y, TargetY))
        {
            // Reached the cursor height: land exactly there so the crash is where the ring promised.
            Projectile.Bottom = new Vector2(Projectile.Bottom.X, Math.Min(Projectile.Bottom.Y + Projectile.velocity.Y, TargetY));
            Crash();
        }
    }
}
