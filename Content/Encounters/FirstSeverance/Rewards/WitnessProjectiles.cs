#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Last Witness v2 (WEAPONS.md "Rogue — Last Witness"; WitnessRules owns every number). One held score per item use:
// WitnessHang hangs the execution blade, speaks six testimonies (WitnessShard), and at 218 hurls the blade
// (WitnessThrownBlade), which bites its target for two Axiom turns and returns to the hang; a stealth throw also
// calls the Triangle Judgement (WitnessJudgement, the 0.2.x footprint and timing). Ownership: only the owner reads
// input and spawns children through native projectile replication; every count peers see rides ai/ExtraAI; no
// packets. Presentation and sound live in Client/Encounters/FirstSeverance/Weapons/WitnessVisuals.cs.
internal static class WitnessArt
{
    internal const string Root = "Convergence/Assets/Textures/Items/DollWeapons/";
}

// The held controller. ai[0] score age (real ticks), ai[1] testimonies spoken (0-6), ai[2] stealth (0/1); velocity is
// the aim axis. Never damages. Every client runs the pose and the count; only the owner spawns.
public sealed class WitnessHang : ModProjectile
{
    private bool facingHeld;

    internal float Age => Projectile.ai[0];
    internal int Spoken => Math.Clamp((int)Projectile.ai[1], 0, WitnessRules.Testimonies);
    internal bool Stealth => Projectile.ai[2] == 1;
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    // The side the blade hangs on: follows the aim until the seal, then held so the swing never flips mid-throw.
    internal int Facing { get; private set; } = 1;

    public override string Texture => WitnessArt.Root + "WitnessBlade";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = false;
        Projectile.DamageType = RitualArmamentItems.DamageClassFor(RitualArmamentKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    internal static bool Sane(Projectile p) => p.owner >= 0 && p.owner < Main.maxPlayers
        && float.IsFinite(p.ai[0]) && p.ai[0] >= 0 && float.IsFinite(p.ai[1]) && p.ai[1] >= 0 && p.ai[1] <= WitnessRules.Testimonies
        && p.ai[2] is 0 or 1 && float.IsFinite(p.velocity.X) && float.IsFinite(p.velocity.Y)
        && float.IsFinite(p.position.X) && float.IsFinite(p.position.Y);

    public override void AI()
    {
        if (!Sane(Projectile)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Item change, death, Down, CC or noItems end the score; so does its end. Release cancels only before the seal.
        if (!RitualChannel.Valid(Projectile, owner, ModContent.ItemType<LastWitness>()) || Age >= WitnessRules.End)
        { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer && !owner.channel && WitnessRules.ReleaseCancels(Age))
        { Projectile.Kill(); return; }
        RitualChannel.Hold(Projectile, owner, WitnessRules.TurnRate(Age));
        Projectile.ai[0]++;
        float age = Age;
        if (age < WitnessRules.Seal || !facingHeld)
        {
            Facing = Axis.X >= 0 ? 1 : -1;
            facingHeld = age >= WitnessRules.Seal;
        }
        // The front arm rides the blade on every client; DollWeaponArmDraw refines it per drawn frame.
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
            Axis.ToRotation() + Facing * WitnessRules.ArmBeta(age) - MathHelper.PiOver2);
        int spoken = Spoken;
        if (spoken < WitnessRules.Testimonies && age >= WitnessRules.TestimonyFire(spoken))
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Testify(spoken, age);
                Projectile.netUpdate = true;
            }
            Projectile.ai[1] = spoken + 1;
        }
        if (Projectile.owner != Main.myPlayer) return;
        if ((int)age == WitnessRules.Seal) Projectile.netUpdate = true;
        if ((int)age == WitnessRules.Throw) Hurl();
    }

    internal NVector2 Root => new(Projectile.Center.X, Projectile.Center.Y);
    internal float AimAngle => Axis.ToRotation();

    internal NVector2 BalancePoint(float age) => WitnessRules.BalancePoint(Root, AimAngle, Facing, WitnessRules.Pose(age));

    // Where a returning blade is caught: the rest pose of the hang, where it hangs again.
    internal Vector2 RestPoint()
    {
        NVector2 rest = WitnessRules.BalancePoint(Root, AimAngle, Facing, new WitnessPose(WitnessRules.HangBeta, WitnessRules.HangRadius));
        return new Vector2(rest.X, rest.Y);
    }

    internal static Vector2 CatchPoint(int owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is WitnessHang hang) return hang.RestPoint();
        return Main.player[owner].MountedCenter;
    }

    // Owner: testimony `birth` leaves its seat on the blade's edge as a seeking porcelain shard.
    private void Testify(int birth, float age)
    {
        WitnessPose pose = WitnessRules.Pose(age);
        NVector2 center = WitnessRules.BalancePoint(Root, AimAngle, Facing, pose);
        NVector2 seat = WitnessRules.BladeToWorld(center, WitnessRules.BladeAngle(AimAngle, Facing, pose), Facing,
            WitnessRules.SeatLocal(birth, age));
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(seat.X, seat.Y), Axis * WitnessRules.ShardLaunch,
            ModContent.ProjectileType<WitnessShard>(), RitualArmamentRules.ScaledDamage(Projectile.damage, WitnessRules.ShardMultiplier),
            Projectile.knockBack, Projectile.owner, 0, -1, birth % 3);
    }

    // Owner: the blade leaves 92 px out along the aim, spinning on from the whip.
    private void Hurl()
    {
        Vector2 axis = Axis, spawn = Projectile.Center + axis * WitnessRules.ReleaseRadius;
        int cap = WitnessRules.OutboundCap(Vector2.Distance(Main.MouseWorld, spawn));
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, axis * WitnessRules.OutboundSpeed,
            ModContent.ProjectileType<WitnessThrownBlade>(), RitualArmamentRules.ScaledDamage(Projectile.damage, WitnessRules.BladeMultiplier),
            Projectile.knockBack, Projectile.owner, 0, -1, WitnessThrownBlade.Pack(Stealth, Facing, false, cap));
        CalamityRogueArmamentDamage.Mark(index, Stealth);
        Projectile.netUpdate = true;
    }
}

// The thrown blade. ai[0] age (real ticks), ai[1] target/anchor NPC (-1 none), ai[2] packed flags (bit 0 stealth,
// bit 1 spinning counter-clockwise, bit 2 judgement called, bits 3+ the outbound cap). ExtraAI: phase, phase start,
// the anchor point and the rotation. The hit shape is a SpinRadius disc swept between ticks; once per logical root
// per window: the strike on the first contact, a bite on each half turn, the tear-free return.
public sealed class WitnessThrownBlade : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    private WitnessPhase phase;
    // The phase this tick's hits are paid at (set at the end of AI): every enemy struck on the contact tick takes
    // the strike share, even after the first of them has started the turns.
    private WitnessPhase struckAs;
    private int phaseStart;
    private Vector2 anchor;
    private bool started, resync;

    internal float Age => Projectile.ai[0];
    internal int Flags => (int)Projectile.ai[2];
    internal bool Stealth => (Flags & 1) != 0;
    internal int SpinSign => (Flags & 2) != 0 ? -1 : 1;
    internal bool Judged => (Flags & 4) != 0;
    internal int Cap => Math.Clamp(Flags >> 3, WitnessRules.OutboundMinTicks, WitnessRules.OutboundMaxTicks);
    internal WitnessPhase Phase => phase;
    internal int PhaseStart => phaseStart;
    internal int PhaseAge => Math.Max(0, (int)Age - phaseStart);
    internal Vector2 Anchor => anchor;
    // Owner only: the kill was the catch.
    internal bool Caught { get; private set; }

    internal static float Pack(bool stealth, int facing, bool judged, int cap)
        => (stealth ? 1 : 0) | (facing < 0 ? 2 : 0) | (judged ? 4 : 0)
            | Math.Clamp(cap, WitnessRules.OutboundMinTicks, WitnessRules.OutboundMaxTicks) << 3;

    public override string Texture => WitnessArt.Root + "WitnessBlade_L";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.DamageType = RitualArmamentItems.DamageClassFor(RitualArmamentKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 600;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;
    // The Axiom turns bite only on their half-turn ticks; outbound and return are live (once per root).
    public override bool? CanDamage() => phase != WitnessPhase.Turn || WitnessRules.TurnWindow(PhaseAge) >= 0 ? null : false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 from = Projectile.Center - Projectile.velocity, to = Projectile.Center;
        return WitnessRules.SweptDiscTouchesBox(new NVector2(from.X, from.Y), new NVector2(to.X, to.Y), WitnessRules.SpinRadius,
            new NVector2(targetHitbox.Left, targetHitbox.Top), new NVector2(targetHitbox.Right, targetHitbox.Bottom));
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.SourceDamage *= WitnessRules.Share(struckAs);
        if (struckAs == WitnessPhase.Turn) modifiers.Knockback *= 0;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitRoots.Add(RitualTargeting.Root(target));
        // The first contact bites: the blade brakes onto that target and turns in it.
        if (phase == WitnessPhase.Outbound && Projectile.owner == Main.myPlayer) StartTurn(target.whoAmI, (int)Age);
    }

    // 14 bytes. The creation message is sent before the blade's first update, so the anchor and rotation are only
    // taken once the sender has started its flight.
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)((byte)phase | (started ? 0x80 : 0)));
        writer.Write((ushort)Math.Clamp(phaseStart, 0, ushort.MaxValue));
        writer.Write(anchor.X); writer.Write(anchor.Y);
        writer.Write(Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        byte received = reader.ReadByte();
        int start = reader.ReadUInt16();
        Vector2 point = new(reader.ReadSingle(), reader.ReadSingle());
        float rotation = reader.ReadSingle();
        int kind = received & 0x7F;
        phase = kind <= (int)WitnessPhase.Return ? (WitnessPhase)kind : WitnessPhase.Return;
        phaseStart = Math.Min(start, 600);
        if ((received & 0x80) == 0) return;
        if (float.IsFinite(point.X) && float.IsFinite(point.Y)) anchor = point;
        if (float.IsFinite(rotation)) Projectile.rotation = rotation;
        started = true;
    }

    internal static bool Sane(Projectile p) => RitualTargeting.ValidState(p) && p.ai[0] >= 0 && p.ai[2] >= 0 && p.ai[2] < 256;

    public override void AI()
    {
        if (!Sane(Projectile)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Death, Down or elimination end the blade at once; an item change does not (it is already thrown).
        if (!RitualArmamentItems.Usable(owner)) { Projectile.Kill(); return; }
        // A phase change made in a hit callback is published from our own AI (native Update clears netUpdate first).
        if (resync && Projectile.owner == Main.myPlayer) { resync = false; Projectile.netUpdate = true; }
        if (!started)
        {
            started = true;
            Projectile.rotation = Projectile.velocity.ToRotation();
            anchor = Projectile.Center;
        }
        Projectile.ai[0]++;
        int age = (int)Age;
        // Without a contact the blade stops and turns in the air: where the cursor was when nothing is targeted, or
        // after OutboundMaxTicks while it homes on a target.
        if (phase == WitnessPhase.Outbound && age > (Projectile.ai[1] >= 0 ? WitnessRules.OutboundMaxTicks : Cap)) StartTurn(-1, age);
        if (phase == WitnessPhase.Turn && age - phaseStart >= WitnessRules.ReturnTick) StartReturn(age);
        int t = age - phaseStart;
        switch (phase)
        {
            case WitnessPhase.Outbound:
            {
                NPC? target = RitualTargeting.Acquire(Projectile, owner);
                if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, WitnessRules.OutboundSpeed);
                break;
            }
            case WitnessPhase.Turn:
            {
                NPC? held = RitualTargeting.Current(Projectile);
                if (held is not null) anchor = held.Center;
                Vector2 pull = (anchor - Projectile.Center) * WitnessRules.TurnFollow;
                Projectile.velocity = pull.Length() > WitnessRules.TurnMaxSpeed ? Vector2.Normalize(pull) * WitnessRules.TurnMaxSpeed : pull;
                // Each half-turn bite is its own window: every root may be bitten once in it.
                if (WitnessRules.TurnWindow(t) >= 0) Rearm();
                break;
            }
            default:
            {
                Vector2 point = WitnessHang.CatchPoint(Projectile.owner), delta = point - Projectile.Center;
                float distance = delta.Length();
                if (Projectile.owner == Main.myPlayer && distance <= WitnessRules.CatchRadius)
                {
                    Caught = true;
                    Projectile.Kill();
                    return;
                }
                if (Projectile.owner == Main.myPlayer && distance > WitnessRules.Leash) { Projectile.Kill(); return; }
                Vector2 want = distance > 1e-3f ? delta / distance * Math.Min(WitnessRules.ReturnSpeed, distance) : Vector2.Zero;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, distance <= WitnessRules.ReturnSpeed ? 1 : WitnessRules.ReturnResponse);
                break;
            }
        }
        Projectile.rotation += SpinSign * Step(t);
        if (!float.IsFinite(Projectile.rotation)) Projectile.rotation = 0;
        else if (MathF.Abs(Projectile.rotation) > 1e4f) Projectile.rotation %= MathF.Tau;
        struckAs = phase;
    }

    // Rotation advanced this tick: cruise out, the two Axiom turns closing on exactly 4 pi, then easing back.
    private float Step(int t) => phase switch
    {
        WitnessPhase.Turn => t >= 1 ? WitnessRules.TurnAngle(t) - WitnessRules.TurnAngle(t - 1) : WitnessRules.CruiseSpin,
        WitnessPhase.Return => WitnessRules.ReturnSpin(t),
        _ => WitnessRules.CruiseSpin,
    };

    private void Rearm()
    {
        hitRoots.Clear();
        Array.Clear(Projectile.localNPCImmunity);
    }

    private void StartTurn(int npc, int age)
    {
        phase = WitnessPhase.Turn;
        phaseStart = age;
        Projectile.ai[1] = npc >= 0 && npc < Main.maxNPCs ? npc : -1;
        anchor = npc >= 0 && npc < Main.maxNPCs ? Main.npc[npc].Center : Projectile.Center;
        if (Projectile.owner != Main.myPlayer) return;
        // A stealth blade calls the judgement on its target (or where it stopped) as the turns begin.
        if (Stealth && !Judged)
        {
            Projectile.ai[2] = Flags | 4;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), anchor, Vector2.Zero, ModContent.ProjectileType<WitnessJudgement>(),
                RitualArmamentRules.ScaledDamage(Projectile.damage, WitnessRules.JudgementShare), Projectile.knockBack, Projectile.owner,
                0, Projectile.ai[1], 0);
        }
        Projectile.netUpdate = resync = true;
    }

    private void StartReturn(int age)
    {
        phase = WitnessPhase.Return;
        phaseStart = age;
        // Tearing free is its own pass: the struck target and anything on the way home may be hit once more.
        Rearm();
        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = resync = true;
    }
}

// A testimony: a small seeking porcelain shard (x0.28), once per root, pierce 1. ai[0] age, ai[1] target, ai[2] shape.
public sealed class WitnessShard : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    internal float Age => Projectile.ai[0];
    internal int Shape => Math.Clamp((int)Projectile.ai[2], 0, 2);

    public override string Texture => WitnessArt.Root + "WitnessShards";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.DamageType = RitualArmamentItems.DamageClassFor(RitualArmamentKind.Rogue);
        Projectile.penetrate = 1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = 300;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(RitualTargeting.Root(target));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // The fast head sweeps; the drawn tail is harmless.
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, Projectile.width, ref collision);
    }
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] is < 0 or > 2) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner)) { Projectile.Kill(); return; }
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        NPC? target = RitualTargeting.Acquire(Projectile, owner, excluded: hitRoots);
        if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, WitnessRules.ShardSpeed);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
}

// The stealth Triangle Judgement: three swords stake the corners, the triangle locks at 16 and executes once per
// root inside its 185 px footprint during 28-31 (x0.60 of the blade). The 0.2.x clocks, footprint and ledger,
// with the shared RitualArmamentChoreography geometry read as is. ai[0] age (real ticks), ai[1] tracked NPC.
public sealed class WitnessJudgement : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    internal float Age => Projectile.ai[0];

    public override string Texture => WitnessArt.Root + "WitnessSword";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.DamageType = RitualArmamentItems.DamageClassFor(RitualArmamentKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.extraUpdates = 1; Projectile.timeLeft = 150;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanDamage() => RitualArmamentChoreography.VerdictLive(Age) ? null : false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(RitualTargeting.Root(target));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => RitualArmamentChoreography.VerdictLive(Age) && RitualArmamentChoreography.TriangleHits(
            new(targetHitbox.Center.X - Projectile.Center.X, targetHitbox.Center.Y - Projectile.Center.Y),
            new(targetHitbox.Width * .5f, targetHitbox.Height * .5f), RitualArmamentChoreography.VerdictRadius);
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || !RitualArmamentItems.Usable(Main.player[Projectile.owner])
            || Age >= RitualArmamentChoreography.VerdictDuration)
        { Projectile.Kill(); return; }
        float before = Age;
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        // The triangle follows its target until the swords stake the corners, then holds still.
        if (Age < RitualArmamentChoreography.VerdictLock)
        {
            NPC? target = RitualTargeting.Current(Projectile);
            if (target is not null)
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center,
                    RitualArmamentChoreography.Response(.23f, 1f / Projectile.MaxUpdates));
        }
        if (before < RitualArmamentChoreography.VerdictLock && Age >= RitualArmamentChoreography.VerdictLock
            && Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
    }
}
