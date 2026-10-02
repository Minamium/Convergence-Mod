#nullable enable
using System;
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Num = System.Numerics.Vector2;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Rogue: Bloodink Quill / 血墨の羽根筆 (REWARDS.md, "Rogue - Bloodink Quill"). Calamity is a required dependency, so the
// weapon always exists; rogue damage and stealth come through CalamityRogueArmament.
//
//   Throw    BloodinkQuill flies the pure QuillFlight curve (straight 12 ticks, then gravity) and writes ink behind it;
//            it sticks in the first NPC it hits (x1.0, moving with it) or in a tile; timeout lets it fall away.
//   Build    on the stick the owner spawns BloodinkTrail: the world-fixed dormant ink from the throw point to the nib.
//            At most 8 stand; a ninth pulls the oldest out. A quill and its ink last 480 ticks (fading over 60).
//   Release  a stealth strike throws SealedScore, which claims every standing trail at the throw (rank = throw order).
//            At its unroll U every claimed stroke burns from the nib back (x1.75 per root per stroke), quill i bursts at
//            U + S(i) (x1.5 per root) and the score bursts at U + S(n) (x2.0, or the Full Melody x3.0 at eight).
//
// Authority and replication: the owner client alone throws, sticks, claims and deals damage. Everything others draw
// rides native replication (netImportant, netUpdate): quill ai = (stuck, serial, offset), trail ai = (stop, serial,
// state), score ai = (age, flight, tag). The release runs on a per-client stamp taken when a client first sees the
// unroll (the Severing Silk method). No packet, no Encounter state. Presentation is Client code reading these
// projectiles (Client/Encounters/CrimsonFoundry/Rewards/Quill*.cs); nothing here draws or plays sound.
// Cleanup: every carrier is a native projectile (death/Down rules below, world unload clears them natively).
public sealed class CrimsonBloodinkQuill : CalamityRogueArmament
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Quill);

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Rogue);
        Item.DamageType = RogueClass;
        Item.width = 28;
        Item.height = 32;
        // Shoot style: vanilla turns the thrower toward the cursor and replicates itemRotation, which the arm reads.
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shootSpeed = R.QuillSpeed;
        Item.shoot = ModContent.ProjectileType<BloodinkQuill>();
    }

    public override bool CanUseItem(Player player)
    {
        if (!CrimsonRewardItems.Usable(player)) return false;
        // Effect bounds: one Sealed Score at a time (a stealth throw waits for the last one to finish), and at most four
        // quills in the air (the score is thrown whatever is flying).
        if (player.whoAmI == Main.myPlayer && HasStealthStrike(player)) return !QuillCarriers.ScoreAlive(player.whoAmI);
        return QuillCarriers.InFlight(player.whoAmI) < R.MaxQuillsInFlight;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type,
        int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool stealth = HasStealthStrike(player);
        Vector2 aim = CrimsonRewardItems.Aim(velocity, player.direction);
        Vector2 center = player.MountedCenter, origin = center + aim * 16;
        if (!Collision.CanHitLine(center, 1, 1, origin, 1, 1)) origin = center;
        var book = player.GetModPlayer<BloodinkQuillPlayer>();
        int index = stealth
            ? SealedScore.Throw(source, player, origin, aim, damage, knockback, book.NextCast())
            : Projectile.NewProjectile(source, origin, aim * R.QuillSpeed, ModContent.ProjectileType<BloodinkQuill>(), damage, knockback,
                player.whoAmI, QuillRules.Flying, book.NextThrow(), 0);
        MarkStealthStrike(index, stealth);
        return false;
    }

    // The quill leaves the hand on the first tick; the arm follows through past the aim and returns along a curve
    // (QuillRules.ArmAngle). Runs for every player on every client from the replicated itemRotation and direction.
    public override void UseItemFrame(Player player)
    {
        if (player.itemAnimationMax <= 0 || player.itemAnimation <= 0) return;
        float progress = 1f - player.itemAnimation / (float)player.itemAnimationMax;
        float aim = player.itemRotation + (player.direction == -1 ? MathHelper.Pi : 0);
        float arm = QuillRules.ArmAngle(progress, aim, player.direction);
        var stretch = progress < QuillRules.ArmFollowEnd ? Player.CompositeArmStretchAmount.Full : Player.CompositeArmStretchAmount.ThreeQuarters;
        player.SetCompositeArmFront(true, stretch, arm - MathHelper.PiOver2);
    }
}

// Owner bookkeeping: throw serials and cast ids (both 0..1023, compared modulo 1024). Only the owner client advances
// them. They are never reset, so a new quill can never alias a part of an earlier release that is still finishing;
// death clears the owner's carriers (each carrier's AI), which is the build this weapon keeps.
public sealed class BloodinkQuillPlayer : ModPlayer
{
    private int serial = -1, cast = -1;
    internal int NextThrow() => serial = R.NextSerial(serial);
    internal int NextCast() => cast = R.NextSerial(cast);
}

internal static class QuillCarriers
{
    internal static long Now => (long)Main.GameUpdateCount;
    internal static Num N(Vector2 v) => new(v.X, v.Y);
    internal static Vector2 X(Num v) => new(v.X, v.Y);
    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
    internal static bool Sane(Projectile p) => p.owner >= 0 && p.owner < Main.maxPlayers && Finite(p.position) && Finite(p.velocity);

    internal static int InFlight(int owner)
    {
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is BloodinkQuill { Flying: true }) count++;
        return count;
    }

    internal static bool ScoreAlive(int owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is SealedScore) return true;
        return false;
    }

    // The fraction of `step` a w x h box at `position` can move before it meets a solid tile, pushed `embed` px further
    // in (the nib enters stone). Sub-steps of 6 px so nothing tunnels through a thin wall, then a short bisection.
    internal static bool TileContact(Vector2 position, int width, int height, Vector2 step, float embed, out float fraction)
    {
        fraction = 1;
        float length = step.Length();
        if (!(length > 1e-3f)) return false;
        int pieces = Math.Max(1, (int)MathF.Ceiling(length / 6f));
        float free = 0;
        for (int i = 1; i <= pieces; i++)
        {
            float s = i / (float)pieces;
            if (!Collision.SolidCollision(position + step * s, width, height)) { free = s; continue; }
            float lo = free, hi = s;
            for (int k = 0; k < 6; k++)
            {
                float mid = (lo + hi) * .5f;
                if (Collision.SolidCollision(position + step * mid, width, height)) hi = mid; else lo = mid;
            }
            fraction = Math.Min(1, lo + embed / length);
            return true;
        }
        return false;
    }

    // Cached projectile slots, revalidated on every use (slots are reused). No closures: these run every tick.
    internal static BloodinkTrail? Trail(ref int cache, int owner, int serial)
    {
        if (cache >= 0 && cache < Main.maxProjectiles && Main.projectile[cache] is { active: true } c && c.owner == owner
            && c.ModProjectile is BloodinkTrail cached && cached.Serial == serial) return cached;
        cache = -1;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is BloodinkTrail found && found.Serial == serial) { cache = p.whoAmI; return found; }
        return null;
    }

    internal static SealedScore? Score(ref int cache, int owner, int cast)
    {
        if (cast < 0) return null;
        if (cache >= 0 && cache < Main.maxProjectiles && Main.projectile[cache] is { active: true } c && c.owner == owner
            && c.ModProjectile is SealedScore cached && cached.Cast == cast) return cached;
        cache = -1;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is SealedScore found && found.Cast == cast) { cache = p.whoAmI; return found; }
        return null;
    }
}

// A thrown quill. ai[0] stuck: 0 flying, NPC slot + 1, -1 fixed in the world; ai[1] throw serial; ai[2] the stick
// offset from the NPC's centre (PackPair, +-512 px). Position = nib. In flight position and velocity are native and
// follow QuillFlight exactly; on the stick the owner sends netUpdate and every client then holds the nib at the NPC's
// centre plus the offset (or where it is). After the stick the velocity keeps the impact direction (its rotation)
// and native movement is off. A claimed quill bursts at U + S(rank): a 56 px disc, x1.5 once per root.
public sealed class BloodinkQuill : ModProjectile
{
    private readonly CrimsonRootLedger roots = new();
    private Vector2 origin, launch, tickStart;
    private bool started, held, stuckSeen, resync, burstBegun;
    private int age, heldFor, trailCache = -1, scoreCache = -1, rank = -1, cast = -1, waited;
    private float stop = -1;
    private ulong incarnation;
    private long unroll = -1;
    private Vector2 unrollAt;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.QuillProjectile);

    internal int Stuck => (int)Projectile.ai[0];
    internal int Serial => (int)Projectile.ai[1];
    internal bool Flying => Projectile.ai[0] == QuillRules.Flying;
    internal bool Held => held;
    // The throw point and initial velocity (QuillFlight's inputs), captured by every client from the spawn state.
    internal Vector2 Origin => origin;
    internal Vector2 Launch => launch;
    internal bool Started => started;
    // AI ticks since the throw on this client; the body sits at QuillFlight(Age) after a tick.
    internal int Age => age;
    // Fractional flight time where it stopped (this client's estimate), -1 while flying.
    internal float StopTime => stop;
    internal bool Claimed => rank >= 0;
    internal int Rank => rank;
    internal bool Unrolled => unroll >= 0;
    internal float Tau => unroll < 0 ? float.NegativeInfinity : QuillCarriers.Now - unroll;
    internal Vector2 ScoreAt => unrollAt;
    internal bool BurstBegun => burstBegun;
    internal float BurstSince => Tau - QuillRules.BurstStart(Math.Max(rank, 0));
    private bool IsOwner => Projectile.owner == Main.myPlayer;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)QuillRules.QuillHitbox;
        Projectile.friendly = true;
        Projectile.DamageType = CrimsonRewardItems.DamageClassFor(CrimsonRewardKind.Rogue);
        Projectile.penetrate = -1;
        Projectile.tileCollide = false; // tiles are found along QuillFlight by TileContact, so the stick lands on the curve
        Projectile.ignoreWater = true;
        Projectile.timeLeft = R.QuillFlight + 2;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => Flying && !held;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false; // the body is drawn by QuillVisuals (Client)

    public override bool? CanDamage()
    {
        if (Flying) return started && !held ? null : false;
        return burstBegun && QuillRules.Live(BurstSince, QuillRules.QuillBurstLive) ? null : false;
    }
    public override bool? CanHitNPC(NPC target) => roots.Contains(CrimsonRewardItems.Root(target)) ? false : null;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Num min = new(targetHitbox.Left, targetHitbox.Top), max = new(targetHitbox.Right, targetHitbox.Bottom);
        if (Flying)
            return started && R.CapsuleTouchesBox(QuillCarriers.N(tickStart), QuillCarriers.N(Projectile.Center), QuillRules.QuillBody, min, max);
        float reach = QuillRules.BurstReach(BurstSince, R.QuillBurstRadius, QuillRules.QuillBurstLive);
        return reach > 0 && R.DiscTouchesBox(QuillCarriers.N(Projectile.Center), reach, min, max);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        roots.TryAdd(CrimsonRewardItems.Root(target));
        if (!IsOwner || !Flying || held) return;
        // The first NPC hit: the nib enters it and the quill moves with it from now on.
        Vector2 nib = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * QuillRules.Embed;
        Vector2 inset = new(Math.Min(4, target.width * .5f), Math.Min(4, target.height * .5f));
        nib = Vector2.Clamp(nib, target.position + inset, target.position + target.Size - inset);
        Projectile.Center = nib;
        stop = QuillRules.StopNear(QuillCarriers.N(origin), QuillCarriers.N(launch), age, QuillCarriers.N(nib));
        Projectile.ai[0] = QuillRules.StuckOn(target.whoAmI);
        Projectile.ai[2] = QuillRules.PackOffset(QuillCarriers.N(nib - target.Center));
        incarnation = CrimsonRewardItems.Incarnation(target);
        Stick();
    }

    public override void AI()
    {
        if (!QuillCarriers.Sane(Projectile) || !CrimsonRewardItems.ValidAi(Projectile, QuillRules.InWorld, Main.maxNPCs, 0, R.QuillSerials - 1, 0, QuillRules.MaxOffsetCode)
            || !QuillRules.ValidQuill(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2], Main.maxNPCs))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        if (!started)
        {
            started = true;
            origin = tickStart = Projectile.Center;
            launch = Projectile.velocity;
        }
        // State set from another projectile's code (a score's claim, a pull) may land after native Update cleared
        // netUpdate: publish again from our own AI so the flag survives.
        if (resync && IsOwner) { resync = false; Projectile.netUpdate = true; }
        if (Flying && !held) Fly(owner);
        else Hold(owner);
    }

    private void Fly(Player owner)
    {
        // Death clears the owner's build, the quills in the air included; timeout lets the quill fall away (client).
        if (owner.dead || age >= R.QuillFlight) { Projectile.Kill(); return; }
        tickStart = Projectile.Center;
        Projectile.velocity = QuillCarriers.X(QuillRules.NextVelocity(QuillCarriers.N(Projectile.velocity), age));
        Projectile.rotation = Projectile.velocity.ToRotation();
        age++;
        if (!QuillCarriers.TileContact(Projectile.position, Projectile.width, Projectile.height, Projectile.velocity, QuillRules.TileEmbed, out float f))
            return;
        // The nib meets stone on this step: move it there now; native movement is off from here on.
        stop = age - 1 + f;
        Projectile.position += Projectile.velocity * f;
        if (IsOwner)
        {
            Projectile.ai[0] = QuillRules.InWorld;
            Projectile.ai[2] = 0;
            Stick();
        }
        else held = true; // predicted; the owner's update confirms (or moves it to the NPC it hit first)
    }

    // Owner only: the quill stands; the ink it wrote stays as a world-fixed carrier from the throw point to the nib.
    private void Stick()
    {
        Projectile.timeLeft = R.QuillLife;
        Projectile.netUpdate = resync = true;
        if (stop < 0) stop = age;
        BloodinkTrail.Write(Projectile.GetSource_FromThis(), Projectile.owner, origin, launch, stop, Serial, Projectile.damage, Projectile.knockBack);
    }

    private void Hold(Player owner)
    {
        if (Flying)
        {
            // A remote copy holding a predicted tile stop while the owner's update is on its way.
            if (++heldFor > 30) Projectile.Kill();
            return;
        }
        if (!stuckSeen)
        {
            stuckSeen = true;
            if (!IsOwner) Projectile.timeLeft = R.QuillLife;
            if (stop < 0) stop = QuillRules.StopNear(QuillCarriers.N(origin), QuillCarriers.N(launch), age, QuillCarriers.N(Projectile.Center));
        }
        int stuck = Stuck;
        if (stuck > 0)
        {
            if (CrimsonRewardItems.TryNpc(stuck - 1, IsOwner ? incarnation : 0, out NPC npc) && QuillRules.TryUnpackOffset(Projectile.ai[2], out Num offset))
                Projectile.Center = npc.Center + QuillCarriers.X(offset);
            else if (IsOwner)
            {
                // Its NPC died (or the slot now holds another): the quill stays where it was.
                Projectile.ai[0] = QuillRules.InWorld;
                Projectile.netUpdate = true;
            }
        }
        Release(owner);
    }

    private void Release(Player owner)
    {
        if (rank < 0)
        {
            // Standing: the build. It keeps its lifetime through Down and item swaps; death clears it.
            if (owner.dead) { Projectile.Kill(); return; }
            BloodinkTrail? trail = Trail();
            if (trail is null || !QuillRules.TryClaim(trail.Projectile.ai[2], out cast, out rank)) { rank = -1; return; }
        }
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, 3); // the release decides when it ends
        bool usable = CrimsonRewardItems.Usable(owner);
        if (unroll < 0)
        {
            SealedScore? score = QuillCarriers.Score(ref scoreCache, Projectile.owner, cast);
            if (score is { Unrolled: true })
            {
                unroll = score.UnrollStamp;
                unrollAt = score.Projectile.Center;
            }
            else
            {
                if (!usable || ++waited > QuillRules.ClaimWait) Projectile.Kill();
                return;
            }
        }
        float tau = Tau;
        int start = QuillRules.BurstStart(rank);
        if (!QuillRules.PartSurvives(tau, start, usable)) { Projectile.Kill(); return; }
        if (tau >= start && !burstBegun)
        {
            burstBegun = true;
            Array.Clear(Projectile.localNPCImmunity);
            roots.Clear();
        }
        if (tau >= QuillRules.QuillDone(rank)) Projectile.Kill();
    }

    internal BloodinkTrail? Trail() => QuillCarriers.Trail(ref trailCache, Projectile.owner, Serial);

    // Owner only, at the score's throw: the burst's damage from the live weapon damage.
    internal void Arm(int damage) => Projectile.damage = damage;

    // Owner only: a ninth standing quill pulls this one out (with its ink).
    internal void Pull() => Projectile.Kill();
}

// The ink a stuck quill wrote. Position = the throw point, velocity = the throw's initial velocity, ai[0] = the
// fractional flight time where it stopped, ai[1] = the quill's serial, ai[2] = state (0 standing, else the claim of
// Sealed Score `cast` at rank `rank`). Every client rebuilds the stroke from QuillFlight; it never moves, never joins
// quills to each other and never follows a target (owner decision 2). Dormant and harmless until its score unrolls;
// then it burns from the nib back at 180 px/tick, radius 18, x1.75 once per root.
public sealed class BloodinkTrail : ModProjectile
{
    private readonly CrimsonRootLedger roots = new();
    private QuillInkSample[]? samples;
    private int count, scoreCache = -1, waited;
    private float length;
    private Vector2 keyOrigin, keyVelocity, boundsMin, boundsMax;
    private float keyStop = -1;
    private bool resync;
    private long unroll = -1;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.QuillProjectile);

    internal float Stop => Projectile.ai[0];
    internal int Serial => (int)Projectile.ai[1];
    internal bool Standing => Projectile.ai[2] == QuillRules.Standing;
    internal bool Unrolled => unroll >= 0;
    internal float Tau => unroll < 0 ? float.NegativeInfinity : QuillCarriers.Now - unroll;
    internal bool TryClaim(out int cast, out int rank) => QuillRules.TryClaim(Projectile.ai[2], out cast, out rank);

    // The stroke's samples (throw point to nib), rebuilt only when the replicated inputs change.
    internal ReadOnlySpan<QuillInkSample> Samples
    {
        get
        {
            Ensure();
            return samples!.AsSpan(0, count);
        }
    }
    internal float Length { get { Ensure(); return length; } }
    internal bool Blot => QuillRules.IsBlot(Length);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.DamageType = CrimsonRewardItems.DamageClassFor(CrimsonRewardKind.Rogue);
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = R.QuillLife;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false; // the ink is emitted by QuillInk (Client)
    public override bool? CanDamage() => unroll >= 0 && Tau >= 0 && Tau < QuillRules.BurnEnd(Length) ? null : false;
    public override bool? CanHitNPC(NPC target) => roots.Contains(CrimsonRewardItems.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => roots.TryAdd(CrimsonRewardItems.Root(target));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Ensure();
        // Cheap reject first: the stroke's bounds grown by the burn radius.
        if (targetHitbox.Right < boundsMin.X || targetHitbox.Left > boundsMax.X || targetHitbox.Bottom < boundsMin.Y || targetHitbox.Top > boundsMax.Y)
            return false;
        return QuillRules.BurnTouches(Samples, Blot, Tau, new Num(targetHitbox.Left, targetHitbox.Top), new Num(targetHitbox.Right, targetHitbox.Bottom));
    }

    private void Ensure()
    {
        Vector2 at = Projectile.Center, velocity = Projectile.velocity;
        float stop = Projectile.ai[0];
        if (samples is not null && at == keyOrigin && velocity == keyVelocity && stop == keyStop) return;
        samples ??= new QuillInkSample[QuillRules.MaxStrokeSamples];
        keyOrigin = at; keyVelocity = velocity; keyStop = stop;
        count = QuillRules.SampleStroke(QuillCarriers.N(at), QuillCarriers.N(velocity), stop, samples, out length);
        boundsMin = new Vector2(float.MaxValue); boundsMax = new Vector2(float.MinValue);
        for (int i = 0; i < count; i++)
        {
            boundsMin = Vector2.Min(boundsMin, QuillCarriers.X(samples[i].At));
            boundsMax = Vector2.Max(boundsMax, QuillCarriers.X(samples[i].At));
        }
        boundsMin -= new Vector2(R.BurnRadius + 1); boundsMax += new Vector2(R.BurnRadius + 1);
    }

    public override void AI()
    {
        if (!QuillCarriers.Sane(Projectile) || !CrimsonRewardItems.ValidAi(Projectile, 0, R.QuillFlight, 0, R.QuillSerials - 1, 0, QuillRules.MaxClaim)
            || !QuillRules.ValidTrail(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2]))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        Ensure(); // sampled here, in the update, so drawing never allocates
        if (resync && Projectile.owner == Main.myPlayer) { resync = false; Projectile.netUpdate = true; }
        if (!TryClaim(out int cast, out _))
        {
            if (owner.dead) Projectile.Kill(); // death clears the build; Down and item swaps do not
            return;
        }
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, 3);
        bool usable = CrimsonRewardItems.Usable(owner);
        if (unroll < 0)
        {
            if (QuillCarriers.Score(ref scoreCache, Projectile.owner, cast) is { Unrolled: true } score) unroll = score.UnrollStamp;
            else
            {
                if (!usable || ++waited > QuillRules.ClaimWait) Projectile.Kill();
                return;
            }
        }
        float tau = Tau;
        if (!QuillRules.PartSurvives(tau, 0, usable)) { Projectile.Kill(); return; }
        if (tau >= QuillRules.StrokeDone(Length)) Projectile.Kill();
    }

    // Owner only, at the score's throw: this stroke now belongs to Sealed Score `cast`, burning at the live damage.
    internal void Claim(int cast, int rank, int damage)
    {
        Projectile.ai[2] = QuillRules.Claim(cast, rank);
        Projectile.damage = damage;
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, 3);
        Projectile.netUpdate = resync = true;
    }

    // Owner only: write the ink a quill left as it stuck. A ninth standing stroke pulls the oldest quill out with its ink.
    internal static void Write(IEntitySource source, int owner, Vector2 origin, Vector2 launch, float stop, int serial, int damage, float knockback)
    {
        Span<int> serials = stackalloc int[R.MaxQuills * 2];
        Span<int> slots = stackalloc int[R.MaxQuills * 2];
        for (int guard = 0; guard < R.MaxQuills * 2; guard++)
        {
            int standing = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == owner && p.ModProjectile is BloodinkTrail { Standing: true } t && standing < serials.Length)
                { serials[standing] = t.Serial; slots[standing] = p.whoAmI; standing++; }
            if (!QuillRules.MustPull(standing)) break;
            int oldest = QuillRules.Oldest(serials[..standing]);
            Projectile trail = Main.projectile[slots[oldest]];
            int pulled = serials[oldest];
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == owner && p.ModProjectile is BloodinkQuill q && q.Serial == pulled && !q.Flying) q.Pull();
            trail.Kill();
        }
        float at = Math.Clamp(float.IsFinite(stop) ? stop : 0, 0, R.QuillFlight);
        Projectile.NewProjectile(source, origin, launch, ModContent.ProjectileType<BloodinkTrail>(), damage, knockback, owner, at, serial, QuillRules.Standing);
    }
}

// The stealth strike: a rolled score. ai[0] = age (ticks), ai[1] = flight ticks (shortened by the owner when a tile
// stops it), ai[2] = tag (the claiming cast and how many quills it took). It flies toward the cursor at 22 px/tick, glides
// to a halt, unseals over 8 ticks and unrolls at U = flight + 8; it bursts at U + S(n): 120 px x2.0, or 160 px x3.0 with
// a Full Melody of eight. Harmless until then.
public sealed class SealedScore : ModProjectile
{
    private readonly CrimsonRootLedger roots = new();
    private Vector2 heading;
    private bool started, held;
    private long unroll = -1;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.SealedScore);

    internal int Age => (int)Projectile.ai[0];
    internal int Flight => (int)Projectile.ai[1];
    internal int Cast => QuillRules.TryScoreTag(Projectile.ai[2], out int cast, out _) ? cast : -1;
    internal int Quills => QuillRules.TryScoreTag(Projectile.ai[2], out _, out int quills) ? quills : 0;
    internal int UnrollAge => QuillRules.UnrollAge(Flight);
    internal bool Unrolled => unroll >= 0;
    internal long UnrollStamp => unroll;
    internal float Tau => unroll < 0 ? float.NegativeInfinity : QuillCarriers.Now - unroll;
    internal Vector2 Heading => heading == Vector2.Zero ? Vector2.UnitX : heading;
    internal float BurstSince => Tau - QuillRules.ScoreBurstStart(Quills);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = CrimsonRewardItems.DamageClassFor(CrimsonRewardKind.Rogue);
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = QuillRules.MaxScoreAge + 30;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => !held;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false; // drawn by QuillVisuals (Client)
    public override bool? CanDamage() => QuillRules.Live(BurstSince, QuillRules.ScoreBurstLive) ? null : false;
    public override bool? CanHitNPC(NPC target) => roots.Contains(CrimsonRewardItems.Root(target)) ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => roots.TryAdd(CrimsonRewardItems.Root(target));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float reach = QuillRules.BurstReach(BurstSince, QuillRules.ScoreBurstRadius(Quills), QuillRules.ScoreBurstLive);
        return reach > 0 && R.DiscTouchesBox(QuillCarriers.N(Projectile.Center), reach,
            new Num(targetHitbox.Left, targetHitbox.Top), new Num(targetHitbox.Right, targetHitbox.Bottom));
    }

    public override void AI()
    {
        if (!QuillCarriers.Sane(Projectile) || !CrimsonRewardItems.ValidAi(Projectile, 0, QuillRules.MaxScoreAge, 0, R.ScoreFlight, 0, float.MaxValue)
            || !QuillRules.ValidScore(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2]))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        if (!started)
        {
            started = true;
            heading = Projectile.velocity.LengthSquared() > .001f ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
        }
        // Commitment: unstarted, the score (and with it every claimed part still waiting) dies on death or Down; its
        // burst, once live, finishes.
        if (!QuillRules.PartSurvives(Tau, QuillRules.ScoreBurstStart(Quills), CrimsonRewardItems.Usable(owner))) { Projectile.Kill(); return; }
        int age = Age;
        Projectile.velocity = held ? Vector2.Zero : Heading * QuillRules.ScoreSpeedAt(age, Flight);
        if (!held && Projectile.velocity != Vector2.Zero
            && QuillCarriers.TileContact(Projectile.position, Projectile.width, Projectile.height, Projectile.velocity, 0, out float f))
        {
            // A tile stops it early: it halts against the tile and the windup starts now.
            Projectile.position += Projectile.velocity * f;
            Projectile.velocity = Vector2.Zero;
            held = true;
            if (Projectile.owner == Main.myPlayer)
            {
                if (age < Flight) Projectile.ai[1] = Math.Max(0, age);
                Projectile.netUpdate = true;
            }
        }
        Projectile.rotation = Heading.ToRotation();
        if (unroll < 0 && age >= UnrollAge) unroll = QuillCarriers.Now - (age - UnrollAge);
        Projectile.ai[0] = Math.Min(age + 1, QuillRules.MaxScoreAge);
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, 3);
        if (unroll >= 0 && Tau >= QuillRules.ScoreDone(Quills)) Projectile.Kill();
    }

    // Owner only: claim the quills and ink standing now (quills thrown afterwards belong to the next build), arm them
    // from the live weapon damage and throw the score. Returns the score's slot.
    internal static int Throw(IEntitySource source, Player player, Vector2 origin, Vector2 aim, int damage, float knockback, int cast)
    {
        int owner = player.whoAmI;
        Span<int> serials = stackalloc int[R.MaxQuills * 2];
        Span<int> slots = stackalloc int[R.MaxQuills * 2];
        Span<int> order = stackalloc int[R.MaxQuills * 2];
        int standing = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is BloodinkTrail { Standing: true } t && standing < serials.Length)
            { serials[standing] = t.Serial; slots[standing] = p.whoAmI; standing++; }
        QuillRules.ThrowOrder(serials[..standing], order);
        int skip = Math.Max(0, standing - R.MaxQuills), quills = standing - skip;
        int burn = CrimsonRewardItems.Hit(damage, R.BurnMultiplier), burst = CrimsonRewardItems.Hit(damage, R.QuillBurstMultiplier);
        for (int rank = 0; rank < quills; rank++)
        {
            int i = order[skip + rank], serial = serials[i];
            ((BloodinkTrail)Main.projectile[slots[i]].ModProjectile!).Claim(cast, rank, burn);
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == owner && p.ModProjectile is BloodinkQuill q && q.Serial == serial && !q.Flying) q.Arm(burst);
        }
        int flight = QuillRules.ScoreFlightTicks(Vector2.Distance(origin, Main.MouseWorld));
        return Projectile.NewProjectile(source, origin, aim * R.ScoreSpeed, ModContent.ProjectileType<SealedScore>(),
            CrimsonRewardItems.Hit(damage, QuillRules.ScoreBurstMultiplier(quills)), knockback, owner, 0, flight, QuillRules.ScoreTag(cast, quills));
    }
}
