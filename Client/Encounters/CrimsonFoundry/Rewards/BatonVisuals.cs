#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;
using Rules = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Presentation of the Scarlet Baton (Content/Encounters/CrimsonFoundry/Rewards/ScarletBaton.cs), derived only from
// replicated projectile state, so every client sees the same conducting, writing, tutti and river; nothing here
// changes gameplay. The black blood goes through ScarletRewardInk (BatonInkSystem is an emitter) as the spans
// BatonRules computes for the projectiles' own collision; the baton itself is the SR04 sprite (or its Crimson Rod
// placeholder, through CrimsonRewardSprites) drawn about its grip over the player.
internal static class BatonArt
{
    // Grip and gem as fractions of the texture. The SR04 brief puts the grip at the lower left and the gem at the upper
    // right of the held sprite (36 logical px across the diagonal, never more than 40x40); these are its briefed
    // positions until the exporter records the measured anchors. The Crimson Rod placeholder has the same diagonal.
    private static readonly Vector2 GripFinal = new(.14f, .86f), GemFinal = new(.86f, .14f);
    private static readonly Vector2 GripPlaceholder = new(.2f, .8f), GemPlaceholder = new(.8f, .2f);

    internal static ScarletRewardArt.Sprite Sprite => ScarletRewardArt.Get(CrimsonRewardSprites.BatonHeld);
    internal static Vector2 Grip(in ScarletRewardArt.Sprite s) => (s.Pixel ? GripFinal : GripPlaceholder) * new Vector2(s.Source.Width, s.Source.Height);
    internal static Vector2 Gem(in ScarletRewardArt.Sprite s) => (s.Pixel ? GemFinal : GemPlaceholder) * new Vector2(s.Source.Width, s.Source.Height);
    // Drawn grip-to-gem length in world px, and the texture's own grip-to-gem direction.
    internal static float Reach(in ScarletRewardArt.Sprite s) => (Gem(s) - Grip(s)).Length() * s.Scale;
    internal static float Axis(in ScarletRewardArt.Sprite s) => (Gem(s) - Grip(s)).ToRotation();
}

// The held pose: the arm points at the conducting tip (BatonRules.Tip), the baton runs from the hand toward it.
internal readonly record struct BatonPose(Vector2 Hand, float Arm, float Angle, Vector2 Gem);

internal static class BatonPoses
{
    private static readonly int[] swings = new int[Main.maxPlayers + 1];
    private static readonly Vector2[] gems = new Vector2[Main.maxPlayers + 1];
    private static readonly ulong[] gemTicks = new ulong[Main.maxPlayers + 1];

    internal static void Clear()
    {
        Array.Fill(swings, -1);
        Array.Clear(gemTicks);
    }

    static BatonPoses() => Array.Fill(swings, -1);

    internal static BatonPose Pose(BatonSwing swing, Player owner, float time)
    {
        NVector tip = BatonRules.Tip(swing.Gesture, swing.Previous, swing.PreviousAim, swing.Aim, Math.Clamp(time, 0, BatonRules.Duration(swing.Gesture)));
        Vector2 offset = new(tip.X, tip.Y * owner.gravDir);
        float arm = offset.ToRotation();
        Vector2 hand = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, arm - MathHelper.PiOver2);
        if (!float.IsFinite(hand.X) || !float.IsFinite(hand.Y)) hand = owner.MountedCenter;
        Vector2 axis = owner.MountedCenter + offset - hand;
        float angle = axis.LengthSquared() > 1 ? axis.ToRotation() : arm;
        Vector2 gem = hand + angle.ToRotationVector2() * BatonArt.Reach(BatonArt.Sprite);
        return new BatonPose(hand, arm, angle, gem);
    }

    // The owner's current gesture (kept by its visuals' PostAI).
    internal static bool TryCurrent(int owner, out BatonSwing swing)
    {
        swing = null!;
        if (owner < 0 || owner >= Main.maxPlayers) return false;
        int slot = swings[owner];
        if (slot < 0 || slot >= Main.maxProjectiles) return false;
        Projectile p = Main.projectile[slot];
        if (!p.active || p.owner != owner || p.ModProjectile is not BatonSwing found) return false;
        swing = found;
        return true;
    }
    internal static void Track(Projectile p, Vector2 gem)
    {
        if (p.owner < 0 || p.owner >= Main.maxPlayers) return;
        swings[p.owner] = p.whoAmI;
        gems[p.owner] = gem;
        gemTicks[p.owner] = Main.GameUpdateCount;
    }
    // Where the pen's ink leaves from: the gem if the baton is in the hand, otherwise the owner's hand height.
    internal static Vector2 GemOf(int owner)
    {
        if (owner < 0 || owner >= Main.maxPlayers) return Vector2.Zero;
        return Main.GameUpdateCount - gemTicks[owner] <= 2 ? gems[owner] : Main.player[owner].MountedCenter;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class BatonSwingVisuals : GlobalProjectile
{
    private int lastClock = int.MinValue;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is BatonSwing;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not BatonSwing swing || !projectile.active) return;
        Player owner = Main.player[projectile.owner];
        if (!owner.active || owner.HeldItem.ModItem is not CrimsonBaton) return;
        owner.ChangeDir(swing.Facing);
        var pose = BatonPoses.Pose(swing, owner, swing.Clock);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, pose.Arm - MathHelper.PiOver2);
        BatonPoses.Track(projectile, pose.Gem);
        int clock = swing.Clock, previous = lastClock;
        lastClock = clock;
        bool fresh = previous == int.MinValue;
        if (swing.Gesture == BatonRules.Tutti && (fresh ? clock <= 1 : previous < 0 && clock >= 0))
            ScarletRewardAudio.Play(ScarletRewardCues.BatonLift, owner.Center, .7f, 0, 0, 2);
        int ictus = BatonRules.IctusTick(swing.Gesture);
        if (!fresh && previous < ictus && clock >= ictus) Flare(projectile, pose.Gem, swing.Gesture == BatonRules.Tutti);
    }

    // The gem flares at each ictus: a breath of embers and a little light, no new light material.
    private static void Flare(Projectile p, Vector2 gem, bool downbeat)
    {
        float strength = downbeat ? 1f : .6f;
        Lighting.AddLight(gem, .9f * strength, .16f * strength, .1f * strength);
        int n = downbeat ? 5 : 3;
        for (int i = 0; i < n; i++)
        {
            float seed = p.identity * 7.31f + Main.GameUpdateCount * .17f + i;
            float h = ScarletRewardParticles.Hash(seed, 1), k = ScarletRewardParticles.Hash(seed, 2);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, p.owner, gem, new Vector2((h - .5f) * 2.4f, -.6f - 1.4f * k), 12 + 8 * k, 4 + 2 * h, seed);
        }
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not BatonSwing swing) return false;
        Player owner = Main.player[projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not CrimsonBaton) return false;
        var pose = BatonPoses.Pose(swing, owner, swing.Clock + WeaponDrawClock.Fraction);
        Draw(pose, owner, lightColor);
        return false;
    }

    // About the grip: the texture's grip-to-gem axis turned onto the pose; mirrored (flipped vertically) when it points
    // to the left so its lit edge stays on top. Final pixel art draws with point sampling, the placeholder as it is.
    private static void Draw(in BatonPose pose, Player owner, Color light)
    {
        var sprite = BatonArt.Sprite;
        bool left = MathF.Cos(pose.Angle) < 0;
        Vector2 grip = BatonArt.Grip(sprite);
        float axis = BatonArt.Axis(sprite);
        Vector2 origin = left ? new Vector2(grip.X, sprite.Source.Height - grip.Y) : grip;
        float rotation = left ? pose.Angle + axis : pose.Angle - axis;
        Color color = Lighting.GetColor((int)(pose.Hand.X / 16), (int)(pose.Hand.Y / 16));
        SpriteBatch batch = Main.spriteBatch;
        WorldBatchParameters saved = default;
        if (sprite.Pixel)
        {
            saved = WorldBatchParameters.Capture(batch);
            batch.End();
            batch.Begin(saved.Sort, saved.Blend, SamplerState.PointClamp, saved.Depth, saved.Raster, saved.Effect, saved.Transform);
        }
        try
        {
            batch.Draw(sprite.Texture, pose.Hand - Main.screenPosition + new Vector2(0, owner.gfxOffY), sprite.Source, color, rotation, origin,
                sprite.Scale, left ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        }
        finally
        {
            if (sprite.Pixel) { batch.End(); saved.Restore(batch); }
        }
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class BatonStrokeVisuals : GlobalProjectile
{
    private int lastClock = int.MinValue;
    private CrimsonStrokeKind lastKind;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is BatonStroke;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not BatonStroke stroke || !projectile.active || stroke.SampleCount < 2) return;
        int clock = stroke.Clock, previous = lastClock;
        var kind = stroke.Kind;
        bool fresh = previous == int.MinValue;
        var before = lastKind;
        lastClock = clock; lastKind = kind;
        int owner = projectile.owner;
        var points = stroke.Points;
        Vector2 start = Xna(points[0]), end = Xna(points[^1]);
        if (kind == CrimsonStrokeKind.Dormant)
        {
            // The pen's ink leaves the gem for the first ticks of the stroke.
            if (clock < Rules.WriteTicks && clock % 2 == 0 && (fresh || clock != previous))
                BatonInkSystem.Drop(owner, BatonPoses.GemOf(owner), start, projectile.identity * 3 + clock);
            if (Crossed(fresh, previous, clock, BatonRules.WriteStart))
                ScarletRewardAudio.Shot(ScarletRewardCues.BatonStroke, owner, start, .55f);
            // The stroke is written at the ictus: the next toll of the ladder, for its owner only.
            if (Crossed(fresh, previous, clock, BatonRules.WriteEnd))
                ScarletRewardAudio.BuildToll(Math.Clamp(BatonScore.Count(owner) - 1, 0, ScarletRewardCues.Tolls - 1), owner, end);
            return;
        }
        if (kind != CrimsonStrokeKind.Live) return;
        if (!fresh && before != CrimsonStrokeKind.Live) Ignited(projectile, stroke);
        else if (!fresh && previous < BatonRules.IgniteEnd && clock >= BatonRules.IgniteEnd) Dried(projectile, stroke);
        if (clock < BatonRules.IgniteEnd)
            Lighting.AddLight(Xna(points[points.Length / 2]), .55f, .06f, .04f);
    }

    private static bool Crossed(bool fresh, int previous, int clock, int tick)
        => fresh ? clock == tick : previous < tick && clock >= tick && clock - tick <= 2;

    private static void Ignited(Projectile p, BatonStroke stroke)
    {
        var points = stroke.Points;
        Vector2 center = p.Center;
        ScarletRewardAudio.Play(ScarletRewardCues.InkIgnite, center, .62f, 0, .03f, 3);
        for (int i = 0; i < 6; i++)
        {
            float seed = p.identity * 5.17f + i * 1.3f;
            Vector2 at = Xna(points[Math.Clamp((int)((i + .5f) / 6 * points.Length), 0, points.Length - 1)]);
            float h = ScarletRewardParticles.Hash(seed, 3), k = ScarletRewardParticles.Hash(seed, 4);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, p.owner, at, new Vector2((h - .5f) * 2f, -1.2f - 1.6f * k), 18 + 10 * k, 5, seed);
        }
        // A partial score closes on the cadence with its last ignition (a full score closes on the river instead).
        int owner = p.owner, cast = stroke.Cast, slot = -1;
        if (cast < 0 || BatonScore.River(owner, cast, ref slot) is not null) return;
        foreach (Projectile other in Main.ActiveProjectiles)
            if (other.owner == owner && other.ModProjectile is BatonStroke { Kind: CrimsonStrokeKind.Scheduled } waiting && waiting.State.Cast == cast) return;
        ScarletRewardAudio.Play(ScarletRewardCues.Cadence, center, .8f, 0, 0, 2);
    }

    private static void Dried(Projectile p, BatonStroke stroke)
    {
        var points = stroke.Points;
        for (int i = 0; i < 2; i++)
        {
            float seed = p.identity * 2.9f + i * 4.1f;
            Vector2 at = Xna(points[Math.Clamp((int)((i + .5f) / 2 * points.Length), 0, points.Length - 1)]);
            ScarletRewardFx.Particle(ScarletParticleKind.Smoke, p.owner, at, new Vector2(0, -.5f), 28, 12, seed);
        }
    }

    private static Vector2 Xna(NVector v) => new(v.X, v.Y);
}

[Autoload(Side = ModSide.Client)]
internal sealed class BatonRiverVisuals : GlobalProjectile
{
    private int lastAge = int.MinValue;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is BatonRiver;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not BatonRiver river || !projectile.active || !river.Built || river.SampleCount < 1) return;
        int age = river.Age, previous = lastAge;
        lastAge = age;
        if (previous != int.MinValue && previous < 0 && age >= 0)
        {
            Vector2 start = projectile.Center;
            ScarletRewardAudio.Play(ScarletRewardCues.RiverRelease, start, .9f, 0, 0, 2);
            ScarletRewardFx.Shake(projectile.owner, start, Rules.RiverShake);
        }
        if (age < 0 || age >= BatonRules.RiverDryStart) return;
        float head = river.Length * Math.Clamp(age / (float)Rules.RiverHeadTicks, 0, 1);
        Vector2 at = BatonInkSystem.PointAt(river.Points, river.Along, river.SampleCount, head);
        Lighting.AddLight(at, .8f, .1f, .06f);
        if (age <= Rules.RiverHeadTicks)
        {
            float seed = projectile.identity * 3.7f + age;
            float h = ScarletRewardParticles.Hash(seed, 5), k = ScarletRewardParticles.Hash(seed, 6);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, projectile.owner, at, new Vector2((h - .5f) * 2.4f, -1 - 1.5f * k), 20 + 10 * k, 5 + 2 * h, seed);
            if (age % 3 == 0) ScarletRewardFx.Particle(ScarletParticleKind.Smoke, projectile.owner, at, new Vector2(0, -.6f), 30, 14, seed + .5f);
        }
    }
}

// Sub-tick arm pose for every player conducting (the WeaponArmDraw technique).
[Autoload(Side = ModSide.Client)]
internal sealed class BatonClientPlayer : ModPlayer
{
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender || Player.dead || !Player.active || Player.HeldItem.ModItem is not CrimsonBaton) return;
        if (!BatonPoses.TryCurrent(Player.whoAmI, out var swing)) return;
        var pose = BatonPoses.Pose(swing, Player, swing.Clock + WeaponDrawClock.Fraction);
        float rotation = pose.Arm - MathHelper.PiOver2;
        drawInfo.compositeFrontArmRotation = Player.gravDir == -1f ? -rotation : rotation;
    }
}

// The baton's black blood: every stroke and river as BatonRules' spans (the same footprint the projectiles collide
// with), plus the pen's droplets. Registered with ScarletRewardInk, which draws once per frame beneath the Raid's
// forecasts. Fixed arrays only; nothing is allocated per frame.
[Autoload(Side = ModSide.Client)]
internal sealed class BatonInkSystem : ModSystem, IScarletInkEmitter
{
    private const int MaxDrops = 48;
    private const float DropGravity = .22f;
    private struct PenDrop
    {
        internal Vector2 Position, Previous, Velocity;
        internal float Age, Life, Radius, Seed;
        internal int Owner;
    }
    private static readonly PenDrop[] drops = new PenDrop[MaxDrops];
    private static int dropCount;
    private static readonly int[] dormant = new int[Main.maxPlayers + 1];

    public override void Load()
    {
        ScarletRewardInk.Register(this);
        BatonPoses.Clear();
    }

    public override void Unload()
    {
        ScarletRewardInk.Unregister(this);
        Clear();
    }

    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();

    private static void Clear()
    {
        dropCount = 0;
        BatonPoses.Clear();
    }

    // A droplet from the gem toward the pen: it flies at the pen and falls a little, whether or not it gets there.
    internal static void Drop(int owner, Vector2 from, Vector2 toward, float seed)
    {
        if (Main.dedServ || !float.IsFinite(from.X + from.Y + toward.X + toward.Y)) return;
        if (dropCount >= MaxDrops) { Array.Copy(drops, 1, drops, 0, MaxDrops - 1); dropCount--; }
        Vector2 aim = toward - from;
        float distance = aim.Length();
        if (distance < 1) return;
        float h = ScarletRewardParticles.Hash(seed, 7), k = ScarletRewardParticles.Hash(seed, 8);
        float speed = Math.Clamp(distance / 9f, 6, 20) * (.85f + .3f * h);
        Vector2 velocity = aim / distance * speed + new Vector2((k - .5f) * 1.5f, -1.2f - k);
        drops[dropCount++] = new PenDrop
        {
            Position = from, Previous = from, Velocity = velocity, Age = 0, Life = 9 + 4 * k, Radius = 2.2f + .9f * h, Seed = seed, Owner = owner,
        };
    }

    public override void PostUpdateEverything()
    {
        if (Main.gamePaused) return;
        for (int i = dropCount - 1; i >= 0; i--)
        {
            ref PenDrop d = ref drops[i];
            d.Previous = d.Position;
            d.Velocity.Y += DropGravity;
            d.Position += d.Velocity;
            d.Age++;
            if (d.Age >= d.Life) { drops[i] = drops[--dropCount]; }
        }
    }

    public void Emit(ScarletInkCanvas canvas, in ScarletView view)
    {
        float fraction = view.Fraction;
        Array.Clear(dormant);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is BatonStroke { Kind: CrimsonStrokeKind.Dormant } && p.owner is >= 0 and < Main.maxPlayers) dormant[p.owner]++;
        Span<BatonInkSpan> spans = stackalloc BatonInkSpan[2];
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is BatonStroke stroke) EmitStroke(canvas, p, stroke, fraction, spans);
            else if (p.ModProjectile is BatonRiver river) EmitRiver(canvas, p, river, fraction, spans);
        }
        for (int i = 0; i < dropCount; i++)
        {
            ref readonly PenDrop d = ref drops[i];
            Vector2 at = Vector2.Lerp(d.Previous, d.Position, fraction);
            float fade = 1 - Math.Clamp((d.Age + fraction) / d.Life, 0, 1);
            canvas.Droplet(ScarletRewardFx.Ink(d.Owner, ScarletInkLook.Live, d.Seed, .6f + .4f * fade), at - d.Velocity * .6f, at, d.Radius * (.5f + .5f * fade), d.Age + fraction);
        }
    }

    private static void EmitStroke(ScarletInkCanvas canvas, Projectile p, BatonStroke stroke, float fraction, Span<BatonInkSpan> spans)
    {
        int count = stroke.SampleCount;
        if (count < 2) return;
        float length = stroke.Length, seed = p.identity % 997 * .37f;
        int owner = p.owner, n;
        float warmth = owner is >= 0 and < Main.maxPlayers && dormant[owner] >= Rules.MaxStrokes ? 1 : 0;
        var state = stroke.State;
        switch (state.Kind)
        {
            case CrimsonStrokeKind.Dormant:
                n = BatonRules.DormantSpans(Math.Min(stroke.Clock + fraction, Rules.StrokeLife), length, warmth, spans);
                break;
            case CrimsonStrokeKind.Scheduled:
                // Ticks since the cast: the cast tick's own update already counted the countdown once.
                spans[0] = BatonRules.ScheduledSpan(length, BatonRules.Countdown(state.Rank) - 1 - state.Countdown + fraction, warmth);
                n = 1;
                break;
            case CrimsonStrokeKind.Live:
                spans[0] = BatonRules.IgnitedSpan(stroke.Clock + fraction, length, stroke.Rank, stroke.FullScore);
                n = 1;
                break;
            default:
                spans[0] = BatonRules.DryingSpan(stroke.Clock + fraction, length);
                n = 1;
                break;
        }
        for (int i = 0; i < n; i++)
        {
            ref readonly var span = ref spans[i];
            var style = ScarletRewardFx.Ink(owner, (ScarletInkLook)span.Look, seed, span.Opacity, false, span.Warmth);
            EmitSpan(canvas, style, stroke.Points, stroke.Along, ReadOnlySpan<bool>.Empty, count, span);
        }
    }

    private static void EmitRiver(ScarletInkCanvas canvas, Projectile p, BatonRiver river, float fraction, Span<BatonInkSpan> spans)
    {
        if (!river.Built || river.SampleCount < 2) return;
        int n = BatonRules.RiverSpans(river.Age + fraction, river.Length, spans);
        float seed = p.identity % 997 * .53f + 11;
        for (int i = 0; i < n; i++)
        {
            ref readonly var span = ref spans[i];
            var style = ScarletRewardFx.Ink(p.owner, (ScarletInkLook)span.Look, seed, span.Opacity);
            EmitSpan(canvas, style, river.Points, river.Along, river.Breaks, river.SampleCount, span);
        }
    }

    // The polyline clipped to the span, one path per unbroken stretch; the bead (if any) sits on the span's end.
    internal static void EmitSpan(ScarletInkCanvas canvas, in ScarletInkStyle style, ReadOnlySpan<NVector> points, ReadOnlySpan<float> along,
        ReadOnlySpan<bool> breaks, int count, in BatonInkSpan span)
    {
        if (count < 2 || span.To <= span.From) return;
        bool open = false;
        for (int i = 1; i < count; i++)
        {
            if (!breaks.IsEmpty && breaks[i])
            {
                if (open) { canvas.End(); open = false; }
                continue;
            }
            float u0 = along[i - 1], u1 = along[i];
            if (u1 < span.From) continue;
            if (u0 >= span.To) break;
            float a = Math.Max(u0, span.From), b = Math.Min(u1, span.To), length = u1 - u0;
            if (!open)
            {
                if (!canvas.Begin(style)) return;
                open = true;
                canvas.Point(At(points, i, a, u0, length), span.Radius, span.TimeAt(a));
            }
            canvas.Point(At(points, i, b, u0, length), span.Radius, span.TimeAt(b));
            if (b < u1) break;
        }
        if (open) canvas.End(span.Bead);
    }

    private static Vector2 At(ReadOnlySpan<NVector> points, int i, float u, float u0, float length)
    {
        NVector a = points[i - 1], b = points[i];
        NVector at = length > 1e-5f ? NVector.Lerp(a, b, Math.Clamp((u - u0) / length, 0, 1)) : b;
        return new Vector2(at.X, at.Y);
    }

    // The point `u` px along a sampled path.
    internal static Vector2 PointAt(ReadOnlySpan<NVector> points, ReadOnlySpan<float> along, int count, float u)
    {
        if (count < 1) return Vector2.Zero;
        for (int i = 1; i < count; i++)
            if (along[i] >= u) return At(points, i, u, along[i - 1], along[i] - along[i - 1]);
        return new Vector2(points[count - 1].X, points[count - 1].Y);
    }
}
