#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;
using Rules = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Magic: Scarlet Baton (docs/encounters/crimson-foundry/REWARDS.md, "Magic - Scarlet Baton"). Hold to conduct the 4/4
// pattern (Down, In, Out, Up): every gesture writes a black-blood stroke centred on the cursor, steered by the cursor's
// sweep, live for a moment behind its ember-gold bead and then dormant. Up to eight wait as a score. Right click gives
// the Tutti: the strokes dormant at the cast ignite in writing order a sixteenth apart, and a full score of eight runs
// together into the Black-Blood River.
// Ownership: only the owner client samples the cursor, spends mana, keeps the combo and the cast ids, and spawns or
// changes projectiles; peers rebuild every shape and timeline from the replicated ai, position and velocity (native
// projectile replication with netImportant/netUpdate; no packet). Presentation lives in
// Client/Encounters/CrimsonFoundry/Rewards/BatonVisuals.cs.
public sealed class CrimsonBaton : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Baton);

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Magic);
        Item.width = Item.height = 40;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        // The baton is drawn about its grip by the held gesture (BatonVisuals), so its gem can lead the pen.
        Item.noUseGraphic = true;
        // Hold to keep conducting: one gesture (and one stroke) per use.
        Item.autoReuse = true;
        Item.mana = Rules.BatonMana;
        Item.shoot = ModContent.ProjectileType<BatonSwing>();
        Item.shootSpeed = 1;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (!CrimsonRewardItems.CanAct(player)) return false;
        bool tutti = player.altFunctionUse == 2;
        if (tutti && !BatonScore.CanTutti(player.whoAmI)) return false;
        // No automatic mana potion: a stroke starts only when its mana is already there, so the native payment that
        // follows never reaches for one.
        if (!tutti && player.statMana < player.GetManaCost(Item)) return false;
        Item.useTime = Item.useAnimation = tutti ? Rules.TuttiTicks : Rules.GestureTicks;
        return true;
    }

    // The Tutti costs no mana.
    public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
    {
        if (player.altFunctionUse == 2) mult = 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer || !CrimsonRewardItems.CanAct(player)) return false;
        var conductor = player.GetModPlayer<ScarletBatonPlayer>();
        if (player.altFunctionUse == 2) conductor.Tutti(source, damage, knockback);
        else conductor.Conduct(source, damage, knockback);
        return false;
    }
}

internal static class BatonSupport
{
    internal static Vector2 Xna(NVector v) => new(v.X, v.Y);
    internal static NVector Num(Vector2 v) => new(v.X, v.Y);
    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
    internal static bool InWorld(Vector2 v)
        => Finite(v) && v.X >= -1000 && v.Y >= -1000 && v.X <= Main.maxTilesX * 16f + 1000 && v.Y <= Main.maxTilesY * 16f + 1000;
    internal static bool ValidOwner(Projectile p) => p.owner >= 0 && p.owner < Main.maxPlayers;
}

// The owner's bookkeeping (owner client only): the combo, the previous gesture's aim (so the next one continues its
// pose), the cursor's sweep over the last SweepWindow ticks and the cast ids. Everything visible lives in the
// replicated projectiles; death and entering a world clear this.
public sealed class ScarletBatonPlayer : ModPlayer
{
    private const int History = 8;
    private readonly Vector2[] cursor = new Vector2[History];
    private int cursorCount, cursorHead;
    private int lastGesture = BatonRules.Rest, cast;
    private ulong lastEnd;
    private float lastAim;

    private void Reset()
    {
        lastGesture = BatonRules.Rest; lastEnd = 0; lastAim = 0;
        cursorCount = cursorHead = 0;
    }

    public override void UpdateDead() => Reset();
    public override void OnEnterWorld() { Reset(); cast = 0; }

    // The cursor relative to the screen (camera motion is not a sweep), once per tick after the item update.
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || Main.dedServ) return;
        if (Player.HeldItem.type != ModContent.ItemType<CrimsonBaton>()) { cursorCount = 0; return; }
        cursor[cursorHead] = Main.MouseWorld - Main.screenPosition;
        cursorHead = (cursorHead + 1) % History;
        cursorCount = Math.Min(History, cursorCount + 1);
    }

    // The sweep over the last SweepWindow ticks, up to now.
    private NVector Sweep()
    {
        if (cursorCount == 0) return NVector.Zero;
        int back = Math.Min(cursorCount, (int)Rules.SweepWindow);
        Vector2 then = cursor[(cursorHead - back + History) % History];
        return BatonSupport.Num(Main.MouseWorld - Main.screenPosition - then);
    }

    // The next gesture: Down after BatonIdleReset idle ticks or after a tutti, otherwise the next beat of the bar.
    private (int Gesture, int Previous, float PreviousAim) Take(float aim, bool tutti)
    {
        ulong now = Main.GameUpdateCount;
        long idle = lastGesture < BatonRules.Down ? long.MaxValue : (long)now - (long)lastEnd;
        int gesture = tutti ? BatonRules.Tutti : BatonRules.NextGesture(lastGesture, idle);
        int previous = BatonRules.Continues(lastGesture, idle) ? lastGesture : BatonRules.Rest;
        float previousAim = lastAim;
        lastGesture = gesture; lastEnd = now + (ulong)BatonRules.Duration(gesture); lastAim = aim;
        return (gesture, previous, previousAim);
    }

    internal int NextCast() => cast = (cast + 1) & CrimsonStrokeState.MaxCast;

    // The aim at the (clamped) cursor; a cursor straight above or below keeps the current facing.
    private Vector2 Cursor(out float aim, out int facing)
    {
        Vector2 center = Player.MountedCenter;
        Vector2 target = BatonSupport.Xna(BatonRules.ClampCursor(BatonSupport.Num(center), BatonSupport.Num(Main.MouseWorld)));
        facing = target.X > center.X + .01f ? 1 : target.X < center.X - .01f ? -1 : Player.direction;
        Vector2 toward = target - center;
        if (MathF.Abs(toward.X) < .01f) toward.X = facing * .01f;
        aim = toward.ToRotation();
        return target;
    }

    private void Swing(IEntitySource source, int gesture, int previous, float previousAim, float aim)
    {
        Vector2 previousAxis = previous >= BatonRules.Down ? previousAim.ToRotationVector2() : Vector2.Zero;
        // The creation message carries every field; the age starts at -1 so the cast tick's own update reaches 0.
        Projectile.NewProjectile(source, Player.MountedCenter, previousAxis, ModContent.ProjectileType<BatonSwing>(), 0, 0,
            Player.whoAmI, BatonRules.EncodeGesture(gesture, previous), aim, -1);
    }

    // Left click: one gesture and the stroke it writes, fixed now (shape and all).
    internal void Conduct(IEntitySource source, int damage, float knockback)
    {
        Vector2 center = Cursor(out float aim, out int facing);
        var (gesture, previous, previousAim) = Take(aim, false);
        Swing(source, gesture, previous, previousAim, aim);
        var steering = BatonRules.Steer(gesture, facing, Sweep());
        BatonScore.MakeRoom(Player.whoAmI);
        Projectile.NewProjectile(source, center, BatonSupport.Xna(steering.Half), ModContent.ProjectileType<BatonStroke>(),
            CrimsonRewardItems.Hit(damage, Rules.WriteMultiplier), knockback, Player.whoAmI,
            BatonRules.PackShape(steering.Bend, steering.Skew), -1, CrimsonStrokeState.Dormant.Encode());
    }

    // Right click: the strokes dormant now are scheduled at once (every part is committed at the cast).
    internal void Tutti(IEntitySource source, int damage, float knockback)
    {
        Cursor(out float aim, out _);
        var (gesture, previous, previousAim) = Take(aim, true);
        Swing(source, gesture, previous, previousAim, aim);
        BatonScore.Cast(Player, source, damage, knockback, NextCast());
    }
}

// The score lives in the owner's replicated strokes themselves, so there is no list to clear on death, item swaps or
// world unload.
internal static class BatonScore
{
    private const int Scan = 48;

    private static int StrokeType => ModContent.ProjectileType<BatonStroke>();

    // The owner's dormant strokes (written or being written), oldest first; `written` keeps only finished ones.
    private static int Dormant(int owner, bool written, Span<int> slots)
    {
        int type = StrokeType, n = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != owner || p.type != type || p.ModProjectile is not BatonStroke stroke || n >= slots.Length) continue;
            if (stroke.Kind != CrimsonStrokeKind.Dormant || (written && stroke.Clock < BatonRules.Written)) continue;
            slots[n++] = p.whoAmI;
        }
        // Writing order: the oldest (largest clock) first; identity breaks a tie.
        for (int i = 1; i < n; i++)
            for (int j = i; j > 0 && Older(slots[j], slots[j - 1]); j--) (slots[j], slots[j - 1]) = (slots[j - 1], slots[j]);
        return n;
    }
    private static bool Older(int a, int b)
    {
        Projectile pa = Main.projectile[a], pb = Main.projectile[b];
        return pa.ai[1] > pb.ai[1] || (pa.ai[1] == pb.ai[1] && pa.identity < pb.identity);
    }

    internal static int Count(int owner)
    {
        Span<int> slots = stackalloc int[Scan];
        return Dormant(owner, false, slots);
    }

    // A tutti needs a written stroke and nothing of an earlier cast still to come.
    internal static bool CanTutti(int owner)
    {
        bool any = false;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != owner) continue;
            if (p.ModProjectile is BatonStroke stroke)
            {
                if (stroke.Kind == CrimsonStrokeKind.Scheduled) return false;
                any |= stroke.Kind == CrimsonStrokeKind.Dormant && stroke.Clock >= BatonRules.Written;
            }
            else if (p.ModProjectile is BatonRiver river && river.Age < BatonRules.RiverDryStart) return false;
        }
        return any;
    }

    // Before a new stroke: a ninth dries the oldest away, and at most one stroke is drying at a time (8 + 1).
    internal static void MakeRoom(int owner)
    {
        Span<int> slots = stackalloc int[Scan];
        int n = Dormant(owner, false, slots);
        int evict = BatonRules.Evictions(n);
        int dryingSlot = -1;
        for (int i = 0; i < evict; i++)
            if (Main.projectile[slots[i]].ModProjectile is BatonStroke stroke) { stroke.DryAway(); dryingSlot = slots[i]; }
        if (dryingSlot < 0) return;
        // Older strokes still drying make way for the one that has just started.
        int type = StrokeType;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.type == type && p.whoAmI != dryingSlot && p.ModProjectile is BatonStroke { Kind: CrimsonStrokeKind.Residue })
                p.Kill();
    }

    // The Tutti: schedule every written dormant stroke by rank (writing order) and, for a full score, the river.
    internal static void Cast(Player player, IEntitySource source, int damage, float knockback, int cast)
    {
        Span<int> slots = stackalloc int[Scan];
        int n = Math.Min(Dormant(player.whoAmI, true, slots), Rules.MaxStrokes);
        int ignite = CrimsonRewardItems.Hit(damage, Rules.IgniteMultiplier);
        for (int rank = 0; rank < n; rank++)
            if (Main.projectile[slots[rank]].ModProjectile is BatonStroke stroke) stroke.Schedule(rank, cast, ignite);
        if (!BatonRules.RiverFor(n)) return;
        Projectile first = Main.projectile[slots[0]];
        Projectile.NewProjectile(source, first.Center, Vector2.Zero, ModContent.ProjectileType<BatonRiver>(),
            CrimsonRewardItems.Hit(damage, Rules.RiverMultiplier), knockback, player.whoAmI, BatonRules.RiverSpawnAge, cast, n);
    }

    // The owner's river for a cast, if it is still running.
    internal static BatonRiver? River(int owner, int cast, ref int cached)
    {
        if (cached >= 0 && cached < Main.maxProjectiles && Main.projectile[cached] is { active: true } known
            && known.owner == owner && known.ModProjectile is BatonRiver r && r.Cast == cast) return r;
        cached = -1;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is BatonRiver river && river.Cast == cast) { cached = p.whoAmI; return river; }
        return null;
    }
}

// The held gesture. ai[0] gesture code (BatonRules.EncodeGesture: the gesture and the one it continues from), ai[1]
// the aim, ai[2] the clock (ticks since the gesture's first tick); velocity is the previous gesture's aim as a unit
// vector (zero from rest), so every client continues the pose exactly. Harmless; drawn by BatonVisuals.
public sealed class BatonSwing : ModProjectile
{
    internal int Gesture => BatonRules.TryDecodeGesture(Projectile.ai[0], out int g, out _) ? g : BatonRules.Down;
    internal int Previous => BatonRules.TryDecodeGesture(Projectile.ai[0], out _, out int p) ? p : BatonRules.Rest;
    internal float Aim => Projectile.ai[1];
    internal float PreviousAim => Projectile.velocity.LengthSquared() > .25f ? Projectile.velocity.ToRotation() : Aim;
    internal int Clock => (int)Projectile.ai[2];
    internal int Facing => MathF.Cos(Aim) < 0 ? -1 : 1;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.BatonHeld);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = Rules.TuttiTicks + 4;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles,
        List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

    private bool Valid()
    {
        float speed = Projectile.velocity.Length();
        return BatonSupport.ValidOwner(Projectile) && BatonRules.TryDecodeGesture(Projectile.ai[0], out int gesture, out _)
            && Rules.Valid(Projectile.ai[1], -MathF.PI - .01f, MathF.PI + .01f)
            && Rules.ValidInteger(Projectile.ai[2], -1, BatonRules.Duration(gesture))
            && float.IsFinite(speed) && (speed < .001f || MathF.Abs(speed - 1) < .01f);
    }

    public override void AI()
    {
        if (!Valid()) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Held projectiles stop while the owner cannot act (dead, Down in a Raid, stunned or item-locked).
        if (!owner.active || !CrimsonRewardItems.CanAct(owner)) { Projectile.Kill(); return; }
        if (owner.HeldItem.type != ModContent.ItemType<CrimsonBaton>())
        {
            // Native held-item sync may trail the projectile on peers; the owner stops at once.
            if (Projectile.owner == Main.myPlayer || ++Projectile.localAI[0] > Rules.HeldLagTicks) Projectile.Kill();
            return;
        }
        Projectile.localAI[0] = 0;
        Projectile.ai[2]++;
        // The next gesture starts on the tick this one ends; it takes over the pose from here.
        if (Clock >= BatonRules.Duration(Gesture)) { Projectile.Kill(); return; }
        Projectile.Center = owner.MountedCenter;
        Projectile.timeLeft = 2;
    }
}

// One written stroke: position = the chord's midpoint, velocity = the half-chord (ShouldUpdatePosition false, as the
// Moonshear, Moonloom Harp and Last Waltz projectiles use velocity as data), ai[0] the packed bend/skew, ai[1] the
// clock (since the gesture's first tick while dormant; since its ignition once live; since it began drying while
// residue), ai[2] the CrimsonStrokeState. The owner sets the schedule once, at the cast, and every client counts it
// down on its own. Draws nothing itself (BatonVisuals emits its ink); only the owner deals damage.
public sealed class BatonStroke : ModProjectile
{
    private NVector[]? points;
    private float[]? along;
    private int count;
    private float length;
    private CrimsonRootLedger? ledger;
    private bool resync;
    private int riverSlot = -1;

    internal CrimsonStrokeKind Kind => CrimsonStrokeState.TryDecode(Projectile.ai[2], out var s) ? s.Kind : CrimsonStrokeKind.Dormant;
    internal CrimsonStrokeState State => CrimsonStrokeState.TryDecode(Projectile.ai[2], out var s) ? s : CrimsonStrokeState.Dormant;
    internal int Clock => (int)Projectile.ai[1];
    // The cast and rank this stroke was scheduled with, kept on every client that saw the schedule (localAI is never sent).
    internal int Cast => (int)Projectile.localAI[0] - 1;
    internal int Rank => (int)Projectile.localAI[1];
    internal bool FullScore => Projectile.localAI[2] >= 2;
    internal float Length => length;
    internal int SampleCount => count;
    internal ReadOnlySpan<NVector> Points => points is null ? ReadOnlySpan<NVector>.Empty : points.AsSpan(0, count);
    internal ReadOnlySpan<float> Along => along is null ? ReadOnlySpan<float>.Empty : along.AsSpan(0, count);
    internal BatonCurve Curve
    {
        get
        {
            BatonRules.TryUnpackShape(Projectile.ai[0], out int bend, out int skew);
            return BatonRules.Curve(BatonSupport.Num(Projectile.Center), BatonSupport.Num(Projectile.velocity), bend, skew);
        }
    }

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.BatonHeld);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        // Each part (the writing, the ignition) strikes an NPC once; the root ledger covers segmented NPCs.
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;

    // The shape is fixed at the spawn: sampled once into the polyline that is both drawn and collided.
    internal bool EnsurePoints()
    {
        if (points is not null) return count > 1;
        points = new NVector[BatonRules.MaxStrokeSamples];
        along = new float[BatonRules.MaxStrokeSamples];
        var curve = Curve;
        count = BatonRules.SampleStroke(curve, points, along);
        length = count > 0 ? along[count - 1] : 0;
        return count > 1;
    }

    private bool Valid()
        => BatonSupport.ValidOwner(Projectile) && CrimsonStrokeState.TryDecode(Projectile.ai[2], out _)
            && BatonRules.TryUnpackShape(Projectile.ai[0], out _, out _)
            && Rules.ValidInteger(Projectile.ai[1], -1, Rules.StrokeLife)
            && BatonRules.ValidHalf(BatonSupport.Num(Projectile.velocity)) && BatonSupport.InWorld(Projectile.Center);

    // Owner only, from the tutti (inside the player's update): the stroke's ignition is set once, with its damage.
    internal void Schedule(int rank, int cast, int damage)
    {
        Projectile.ai[2] = CrimsonStrokeState.Scheduled(BatonRules.Countdown(rank), rank, cast).Encode();
        Projectile.damage = damage;
        resync = true;
    }

    // Owner only, when a ninth stroke is written: dry away from the opacity it has now.
    internal void DryAway()
    {
        Projectile.ai[1] = BatonRules.DryAwayStart(BatonRules.NaturalOpacity(Clock));
        Projectile.ai[2] = CrimsonStrokeState.Residue.Encode();
        resync = true;
    }

    public override void AI()
    {
        if (!Valid()) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (!EnsurePoints()) { Projectile.Kill(); return; }
        var state = State;
        bool local = Projectile.owner == Main.myPlayer;
        // Commitment: death clears the build and the parts not started; Down kills only the parts not started.
        if (local && !BatonRules.Survives(state.Kind, owner.dead, !CrimsonRewardItems.Usable(owner))) { Projectile.Kill(); return; }
        // The schedule or the dry-away was written in the player's update and the native update clears netUpdate before
        // AI: publish it from here, so peers learn it.
        if (resync && local) { resync = false; Projectile.netUpdate = true; }
        switch (state.Kind)
        {
            case CrimsonStrokeKind.Dormant:
                Projectile.ai[1]++;
                if (Clock >= Rules.StrokeLife) Projectile.Kill();
                break;
            case CrimsonStrokeKind.Scheduled:
                Projectile.localAI[0] = state.Cast + 1;
                Projectile.localAI[1] = state.Rank;
                var next = state.Tick();
                Projectile.ai[2] = next.Encode();
                if (next.Kind == CrimsonStrokeKind.Live) Ignite(owner);
                break;
            case CrimsonStrokeKind.Live:
                Projectile.ai[1]++;
                Hold(owner);
                if (Clock >= BatonRules.StrokeEnd(Rank, FullScore)) Projectile.Kill();
                break;
            default:
                Projectile.ai[1]++;
                if (Clock >= Rules.StrokeDryAway) Projectile.Kill();
                break;
        }
    }

    // The ignition tick: a new part, so the ledger and the local immunity start over.
    private void Ignite(Player owner)
    {
        Projectile.ai[1] = 0;
        ledger?.Clear();
        Array.Clear(Projectile.localNPCImmunity);
        // A full score's scars wait for the river of the same cast.
        Projectile.localAI[2] = Cast >= 0 && BatonScore.River(owner.whoAmI, Cast, ref riverSlot) is not null ? 2 : 1;
    }

    // A full score holds its scars only while its river still runs (the owner's Down can stop a river that had not set off).
    private void Hold(Player owner)
    {
        if (FullScore && Clock >= BatonRules.IgniteEnd && BatonScore.River(owner.whoAmI, Cast, ref riverSlot) is null)
            Projectile.localAI[2] = 1;
    }

    private BatonInkSpan LiveSpan(out bool any)
    {
        any = false;
        var state = State;
        if (state.Kind == CrimsonStrokeKind.Live)
        {
            var span = BatonRules.IgnitedSpan(Clock, length, Rank, FullScore);
            any = span.Look == BatonLook.Live;
            return span;
        }
        if (state.Kind != CrimsonStrokeKind.Dormant) return default;
        Span<BatonInkSpan> spans = stackalloc BatonInkSpan[2];
        int n = BatonRules.DormantSpans(Clock, length, 0, spans);
        for (int i = 0; i < n; i++)
            if (spans[i].Look == BatonLook.Live) { any = true; return spans[i]; }
        return default;
    }

    public override bool? CanDamage()
    {
        LiveSpan(out bool any);
        return any && count > 1 ? null : false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (count < 2) return false;
        var span = LiveSpan(out bool any);
        return any && BatonRules.SpanTouchesBox(Points, Along, ReadOnlySpan<bool>.Empty, count, span,
            new NVector(targetHitbox.Left, targetHitbox.Top), new NVector(targetHitbox.Right, targetHitbox.Bottom));
    }

    public override bool? CanHitNPC(NPC target) => ledger?.Contains(CrimsonRewardItems.Root(target)) == true ? false : null;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => (ledger ??= new CrimsonRootLedger()).TryAdd(CrimsonRewardItems.Root(target));
}

// The Black-Blood River (a full score of eight at the cast). ai[0] age (negative while waiting for 8 + S(9)), ai[1] the
// cast id, ai[2] the stroke count (exactly eight). Every client collects the owner's strokes scheduled with that cast,
// ordered by rank, and builds the same path from their replicated shapes (BatonRules.BuildRiver). Only the owner deals
// damage: x5.0 once per root.
public sealed class BatonRiver : ModProjectile
{
    private readonly BatonCurve[] curves = new BatonCurve[Rules.MaxStrokes];
    private readonly bool[] have = new bool[Rules.MaxStrokes];
    private NVector[]? points;
    private float[]? along;
    private bool[]? breaks;
    private int collected, count;
    private float length;
    private bool built;
    private CrimsonRootLedger? ledger;

    internal int Age => (int)Projectile.ai[0];
    internal int Cast => (int)Projectile.ai[1];
    internal int Strokes => (int)Projectile.ai[2];
    internal bool Built => built;
    internal float Length => length;
    internal int SampleCount => count;
    internal ReadOnlySpan<NVector> Points => points is null ? ReadOnlySpan<NVector>.Empty : points.AsSpan(0, count);
    internal ReadOnlySpan<float> Along => along is null ? ReadOnlySpan<float>.Empty : along.AsSpan(0, count);
    internal ReadOnlySpan<bool> Breaks => breaks is null ? ReadOnlySpan<bool>.Empty : breaks.AsSpan(0, count);

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.BatonHeld);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;

    private bool Valid()
        => BatonSupport.ValidOwner(Projectile) && Rules.ValidInteger(Projectile.ai[0], BatonRules.RiverSpawnAge, BatonRules.RiverEnd)
            && Rules.ValidInteger(Projectile.ai[1], 0, CrimsonStrokeState.MaxCast)
            && Rules.ValidInteger(Projectile.ai[2], Rules.MaxStrokes, Rules.MaxStrokes) && BatonSupport.InWorld(Projectile.Center);

    public override void AI()
    {
        if (!Valid()) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        // A river that has not set off dies with its owner's death or Down; once running it finishes.
        if (Projectile.owner == Main.myPlayer && !BatonRules.RiverSurvives(Age, owner.dead, !CrimsonRewardItems.Usable(owner)))
        { Projectile.Kill(); return; }
        if (!built) Collect();
        Projectile.ai[0]++;
        if (!built && (collected == Strokes || Age >= 0)) Build();
        if (Age >= BatonRules.RiverEnd) Projectile.Kill();
    }

    // Peers may see the river a moment before the strokes' schedules arrive: keep looking until all are found.
    private void Collect()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Projectile.owner || p.ModProjectile is not BatonStroke stroke) continue;
            int rank, cast;
            var state = stroke.State;
            if (state.Kind == CrimsonStrokeKind.Scheduled) { rank = state.Rank; cast = state.Cast; }
            else if (state.Kind == CrimsonStrokeKind.Live && stroke.Cast >= 0) { rank = stroke.Rank; cast = stroke.Cast; }
            else continue;
            if (cast != Cast || rank < 0 || rank >= Rules.MaxStrokes || have[rank]) continue;
            curves[rank] = stroke.Curve;
            have[rank] = true;
            collected++;
        }
    }

    private void Build()
    {
        built = true;
        Span<BatonCurve> ordered = stackalloc BatonCurve[Rules.MaxStrokes];
        int n = 0;
        for (int i = 0; i < Rules.MaxStrokes; i++) if (have[i]) ordered[n++] = curves[i];
        points = new NVector[Rules.RiverMaxVertices];
        along = new float[Rules.RiverMaxVertices];
        breaks = new bool[Rules.RiverMaxVertices];
        count = n > 0 ? BatonRules.BuildRiver(ordered[..n], points, along, breaks, out length) : 0;
        if (count > 0) Projectile.Center = BatonSupport.Xna(points[0]);
    }

    private BatonInkSpan LiveSpan(out bool any)
    {
        any = false;
        Span<BatonInkSpan> spans = stackalloc BatonInkSpan[2];
        int n = BatonRules.RiverSpans(Age, length, spans);
        for (int i = 0; i < n; i++)
            if (spans[i].Look == BatonLook.Live) { any = true; return spans[i]; }
        return default;
    }

    public override bool? CanDamage()
    {
        if (!built || count < 1 || Age < 0 || Age >= BatonRules.RiverDryStart) return false;
        LiveSpan(out bool any);
        return any ? null : false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!built || count < 1) return false;
        var span = LiveSpan(out bool any);
        return any && BatonRules.SpanTouchesBox(Points, Along, Breaks, count, span,
            new NVector(targetHitbox.Left, targetHitbox.Top), new NVector(targetHitbox.Right, targetHitbox.Bottom));
    }

    public override bool? CanHitNPC(NPC target) => ledger?.Contains(CrimsonRewardItems.Root(target)) == true ? false : null;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => (ledger ??= new CrimsonRootLedger()).TryAdd(CrimsonRewardItems.Root(target));
}
