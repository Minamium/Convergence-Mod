#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Luminance.Common.VerletIntergration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Presentation of the Ember Censer (REWARDS.md, "Summon - Ember Censer"). The minion's replicated state is the only
// input; nothing here decides a hit, a packet or a world change.
//  - Body: the unlit crown censer (CenserBody: SR05 art at the 2 px dot, or the Imp Staff placeholder hung head down)
//    turns about its bowl mouth, which sits exactly on the column's top. In a swing the pose is CenserRules' pendulum
//    and tip from the censer's own clock (eased in from the free pendulum during the wind-up, before any pour); idle
//    and seeking, a damped pendulum driven by the ring's motion swings when the owner runs or stops and settles when
//    they stand still. The Grand Pour's brace trembles the bowl (noise, no period).
//  - Bowl: an ember glow at the mouth that builds pour by pour toward the Grand Pour, and a warm crown overlay that
//    spikes ember-gold through the brace. Thin smoke and a few embers rise; embers thicken as it warms.
//  - Drapes: two short Verlet tails (5 points) from the drape anchors swing with the pendulum; off under Reduced Effects.
//  - Pours: CenserInk draws the live column; this class supplies it (EmitColumn), the ember splash at the floor and
//    the scar hand-off when a pour ends.
//  - Cues: CenserSummon on summon; CenserSwing 10 ticks before a pour -> CenserPour; CenserBrace -> CenserGrandPour,
//    each within its per-owner budget (7 / 14 / 20 ticks). Local shake 1.5 on the owner's own Grand Pours, at most once
//    per 20 ticks.
// Time is the per-tick sample plus WeaponDrawClock.Fraction; the ring is interpolated between the two latest samples.
[Autoload(Side = ModSide.Client)]
internal sealed class CenserVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private CenserLook? look;
    private static bool failed;

    internal CenserLook? Look => look;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is EmberCenserMinion;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || projectile.ModProjectile is not EmberCenserMinion minion) return;
        look ??= new CenserLook(projectile);
        look.Observe(projectile, minion);
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (!Main.dedServ && projectile.ModProjectile is EmberCenserMinion minion) look?.Release(projectile, minion);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (failed || look is null || projectile.ModProjectile is not EmberCenserMinion minion) return false;
        try { look.Draw(Main.spriteBatch, projectile, minion); }
        catch (Exception exception)
        {
            failed = true;
            ConvergenceMod.Instance.Logger.Warn($"Ember Censer body drawing disabled for this session: {exception}");
        }
        return false;
    }

    internal static void Reset()
    {
        failed = false;
        CenserBody.Reset();
        CenserLook.ResetBudgets();
    }
}

internal sealed class CenserLook
{
    private const int DrapePoints = 5;
    private const float DrapeLink = 6, Tremble = 2.5f * CenserRules.Degrees;
    private static readonly float Omega2 = MathF.Pow(MathHelper.TwoPi / CrimsonRewardRules.SwingPeriod, 2);
    private static readonly VerletSettings drapeSettings = new(TileCollision: false, SlowInWater: false, Gravity: .3f, MaxFallSpeed: 6);

    // Per-owner sound budgets and the local shake limiter (game ticks + 1; 0 = never).
    private static readonly ulong[] pourCue = new ulong[256], swingCue = new ulong[256], grandCue = new ulong[256], braceCue = new ulong[256];
    private static ulong lastShake;

    internal readonly int Owner, Identity;
    private readonly float seed;
    private readonly List<VerletSegment>[] drapes = { new(DrapePoints), new(DrapePoints) };
    private readonly Vector2[][] oldDrapes = { new Vector2[DrapePoints], new Vector2[DrapePoints] };
    private bool started, drapesReady;
    private Vector2 center, oldCenter, velocity;
    private int clock, oldClock, sinceEntry, entryClock, blendTicks;
    private CenserState state, oldState;
    private bool consecutive;
    // The free pendulum (Idle, Seek, and the tail of a swing) and a tip left over when a swing ended.
    private float phys, oldPhys, physRate, rest, oldRest, restRate;
    // The wind-up blend from the free pendulum into the clock's pose: offset and rate at the entry.
    private float entryOffset, entryRate;
    // The pour live on the last update (its start clock), for the scar hand-off.
    private int livePour = -1;
    private CenserPour lastPour;
    private float heat;

    internal CenserLook(Projectile projectile)
    {
        Owner = projectile.owner; Identity = projectile.identity;
        seed = (projectile.identity * 7919 % 997) * .0137f + .31f;
    }

    internal static void ResetBudgets()
    {
        Array.Clear(pourCue); Array.Clear(swingCue); Array.Clear(grandCue); Array.Clear(braceCue);
        lastShake = 0;
    }

    // ---- Per-tick observation (PostAI, every client) ------------------------------------------------------------
    internal void Observe(Projectile p, EmberCenserMinion m)
    {
        oldCenter = started ? center : p.Center;
        center = p.Center;
        Vector2 moved = center - oldCenter, accel = started ? moved - velocity : Vector2.Zero;
        velocity = moved;
        oldState = started ? state : m.State;
        state = m.State;
        oldClock = started ? clock : m.Clock;
        clock = m.Clock;
        consecutive = started && state == CenserState.Swing && oldState == CenserState.Swing && clock == CenserRules.Advance(oldClock);
        if (Vector2.DistanceSquared(center, oldCenter) > 600f * 600f) { oldCenter = center; accel = Vector2.Zero; drapesReady = false; }
        bool first = !started;
        started = true;
        // Summoned: a censer first seen within its first updates and not yet swinging (a player joining mid-fight
        // sees the old ones already swinging).
        if (first && m.Life <= 2 && m.State != CenserState.Swing)
            ScarletRewardAudio.Shot(ScarletRewardCues.CenserSummon, p.owner, center);

        oldPhys = phys; oldRest = rest;
        if (state == CenserState.Swing)
        {
            if (first || oldState != CenserState.Swing) Enter(accel);
            else sinceEntry++;
            // The free pendulum shadows the shown pose, so a swing that ends continues from where it was.
            phys = SwingAngle(clock, sinceEntry);
            physRate = phys - SwingAngle(Math.Max(0, clock - 1), Math.Max(0, sinceEntry - 1));
            rest = 0; restRate = 0;
        }
        else
        {
            if (!first && oldState == CenserState.Swing)
            {
                // Leaving a swing: the bowl swings on freely from its last pose; the tip settles back upright.
                oldPhys = phys = SwingAngle(oldClock, sinceEntry);
                physRate = CenserRules.Angle(oldClock) - CenserRules.Angle(Math.Max(0, oldClock - 1));
                oldRest = rest = CenserRules.Tip(oldClock);
                restRate = 0;
            }
            Swing(accel, velocity);
            // Critically damped settle of a leftover tip.
            const float w = .25f;
            restRate += -w * w * rest - 2 * w * restRate;
            rest += restRate;
        }
        heat = MathHelper.Lerp(heat, Heat(m), .25f);

        var body = CenserBody.Get();
        if (body is not null) UpdateDrapes(body);
        Mouth(out Vector2 mouth, out float angle);
        Effects(p, m, mouth, angle);
        Hand(p, m);
    }

    private void Enter(Vector2 accel)
    {
        // Continue the free pendulum one more tick, then ease from it into the clock's pose before the first tip.
        Swing(accel, velocity);
        entryClock = clock;
        sinceEntry = 0;
        blendTicks = entryClock < CenserRules.WindUp - CrimsonRewardRules.PourTipTicks - 6
            ? Math.Clamp(CenserRules.WindUp - CrimsonRewardRules.PourTipTicks - entryClock, 6, 20) : 6;
        entryOffset = phys - CenserRules.Angle(clock);
        entryRate = physRate - (CenserRules.Angle(clock + 1) - CenserRules.Angle(clock));
    }

    // The swing's shown angle: the clock's pendulum, plus the wind-up blend (a Hermite offset that matches the free
    // pendulum's angle and rate at the entry and is gone well before the first pour, so the column's top is exact).
    private float SwingAngle(float swingClock, float since)
    {
        float angle = CenserRules.Angle(swingClock);
        if (since >= blendTicks) return angle;
        float s = Math.Clamp(since / blendTicks, 0, 1), s2 = s * s, s3 = s2 * s;
        return angle + entryOffset * (2 * s3 - 3 * s2 + 1) + entryRate * blendTicks * (s3 - 2 * s2 + s);
    }

    // The free pendulum: gravity tuned to the swing period, driven by the ring's acceleration and a little air drag
    // (it trails when the owner runs, swings when they start or stop, settles when they stand still).
    private void Swing(Vector2 accel, Vector2 moved)
    {
        accel = Vector2.Clamp(accel, new Vector2(-6), new Vector2(6));
        for (int step = 0; step < 2; step++)
        {
            float sin = MathF.Sin(phys), cos = MathF.Cos(phys);
            float torque = -Omega2 * sin - .35f * (accel.X * cos - accel.Y * sin) / CrimsonRewardRules.BowlDrop
                - .02f * moved.X * cos / CrimsonRewardRules.BowlDrop - .05f * physRate;
            physRate = Math.Clamp(physRate + torque * .5f, -.2f, .2f);
            phys = Math.Clamp(phys + physRate * .5f, -1.2f, 1.2f);
        }
    }

    private static float Heat(EmberCenserMinion m)
    {
        float warmth = m.State == CenserState.Swing ? CenserRules.Warmth(m.Clock) : CenserRules.WarmthIdle;
        if (m.LivePour(out var pour)) warmth += (pour.Grand ? .5f : .35f) * (1 - pour.Age / pour.Live);
        return Math.Clamp(warmth, 0, 1.4f);
    }

    // ---- Pose ---------------------------------------------------------------------------------------------------
    private float DrawClock(float fraction) => consecutive ? oldClock + fraction : clock;

    // The pendulum angle the mouth hangs on and the sprite's extra turn (tip and tremble) at a draw fraction.
    private void Pose(float fraction, out Vector2 ring, out float angle, out float turn)
    {
        ring = Vector2.Lerp(oldCenter, center, fraction);
        if (state == CenserState.Swing)
        {
            float c = DrawClock(fraction);
            angle = SwingAngle(c, Math.Max(0, sinceEntry - 1 + fraction));
            turn = CenserRules.Tip(c);
            if (CenserRules.Bracing(c) || CenserRules.TryPour(c, out var pour) && pour.Grand && pour.Age < 3)
                turn += (Noise(seed * 9.1f, c * .9f) - .5f) * 2 * Tremble * (ScarletRewardFx.Reduced ? .4f : 1);
            return;
        }
        angle = MathHelper.Lerp(oldPhys, phys, fraction);
        turn = MathHelper.Lerp(oldRest, rest, fraction);
    }

    private void Mouth(out Vector2 mouth, out float angle)
    {
        Pose(1, out Vector2 ring, out angle, out _);
        mouth = ring + Xna(CenserRules.MouthAt(angle));
    }

    // A point of the sprite (source px) in the world for a pose: the sprite turns about its mouth anchor.
    private static Vector2 Anchor(CenserBody body, Vector2 local, Vector2 mouth, float rotation)
        => mouth + ((local - body.Mouth) * body.Scale).RotatedBy(rotation);

    private static float Rotation(CenserBody body, float angle, float turn) => body.Base - (angle + turn);

    // ---- Drapes -------------------------------------------------------------------------------------------------
    private void UpdateDrapes(CenserBody body)
    {
        if (ScarletRewardFx.Reduced) { drapesReady = false; return; }
        Pose(1, out Vector2 ring, out float angle, out float turn);
        Vector2 mouth = ring + Xna(CenserRules.MouthAt(angle));
        float rotation = Rotation(body, angle, turn);
        for (int side = 0; side < 2; side++)
        {
            var chain = drapes[side];
            Vector2 root = Anchor(body, side == 0 ? body.DrapeLeft : body.DrapeRight, mouth, rotation);
            if (!float.IsFinite(root.X + root.Y)) { drapesReady = false; return; }
            if (!drapesReady || chain.Count != DrapePoints)
            {
                chain.Clear();
                for (int j = 0; j < DrapePoints; j++) chain.Add(new VerletSegment(root + new Vector2(0, DrapeLink * j), Vector2.Zero, j == 0));
            }
            for (int j = 0; j < DrapePoints; j++) oldDrapes[side][j] = chain[j].Position;
            chain[0].Position = chain[0].OldPosition = root;
            // A faint, aperiodic flutter so cloth never hangs frozen.
            for (int j = 1; j < DrapePoints; j++)
                chain[j].Velocity = Vector2.Clamp(chain[j].Velocity * .9f
                    + new Vector2((Noise(seed + side * 3.7f + j, (float)Main.GameUpdateCount * .05f) - .5f) * .12f, 0), new Vector2(-6), new Vector2(6));
            VerletSimulations.VerletSimulation(chain, DrapeLink, drapeSettings, 6);
            for (int j = 1; j < DrapePoints; j++)
            {
                Vector2 offset = chain[j].Position - root;
                if (!float.IsFinite(offset.X + offset.Y)) chain[j].Position = root + new Vector2(0, DrapeLink * j);
                else if (offset.LengthSquared() > DrapeLink * DrapeLink * j * j * 1.44f) chain[j].Position = root + offset.SafeNormalize(Vector2.UnitY) * DrapeLink * j * 1.2f;
            }
            if (!drapesReady) for (int j = 0; j < DrapePoints; j++) oldDrapes[side][j] = chain[j].Position;
        }
        drapesReady = true;
    }

    // ---- Smoke, embers, light, cues and shake -------------------------------------------------------------------
    private void Effects(Projectile p, EmberCenserMinion m, Vector2 mouth, float angle)
    {
        uint now = (uint)Main.GameUpdateCount;
        float h = ScarletRewardParticles.Hash(seed, (int)now), h2 = ScarletRewardParticles.Hash(seed, (int)now + 101);
        Vector2 up = new Vector2(-MathF.Sin(angle), -MathF.Cos(angle)); // the bowl's opening faces back up the chain
        // Thin smoke and a few embers; embers thicken as the bowl warms.
        if (h < .07f) ScarletRewardFx.Particle(ScarletParticleKind.Smoke, Owner, mouth + up * 4, new Vector2((h2 - .5f) * .4f, -.5f), 34, 10, seed + now);
        if (h2 < .04f + .22f * Math.Clamp(heat, 0, 1))
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, Owner, mouth + up * 3 + new Vector2((h - .5f) * 10, 0),
                new Vector2((h - .5f) * 1.2f, -.6f - h2), 18 + 10 * h, 4, seed * 3 + now);
        float glow = .25f + .45f * Math.Clamp(heat, 0, 1.4f);
        Lighting.AddLight(mouth, .55f * glow, .12f * glow, .06f * glow);

        bool live = m.LivePour(out var pour);
        if (live) Splash(p, m, pour, mouth, now);

        // Cues fire on a plain one-tick advance only: never replayed by a correction, never caught up.
        if (state != CenserState.Swing || !consecutive) return;
        if (CenserRules.SwingCue(clock) && Budget(swingCue, CrimsonRewardRules.SwingCueInterval))
            ScarletRewardAudio.Play(ScarletRewardCues.CenserSwing, mouth, .5f, 0, .03f, 3);
        if (CenserRules.BraceStarts(clock) && Budget(braceCue, CrimsonRewardRules.SwingCueInterval))
            ScarletRewardAudio.Play(ScarletRewardCues.CenserBrace, mouth, .55f, 0, .02f, 3);
        if (!CenserRules.PourStarts(clock, out int index)) return;
        if (index == CrimsonRewardRules.GrandEvery - 1)
        {
            if (Budget(grandCue, CrimsonRewardRules.GrandCueInterval))
                ScarletRewardAudio.Play(ScarletRewardCues.CenserGrandPour, mouth, .75f, 0, .02f, 3);
            if (p.owner == Main.myPlayer && Main.GameUpdateCount + 1 - lastShake > CrimsonRewardRules.GrandShakeInterval)
            {
                lastShake = Main.GameUpdateCount + 1;
                ScarletRewardFx.Shake(p.owner, mouth, CrimsonRewardRules.GrandShake, Vector2.UnitY);
            }
        }
        else if (Budget(pourCue, CrimsonRewardRules.PourCueInterval))
            ScarletRewardAudio.Play(ScarletRewardCues.CenserPour, mouth, .6f, 0, .03f, 3);
    }

    private bool Budget(ulong[] stamps, int interval)
    {
        int owner = Math.Clamp(Owner, 0, stamps.Length - 1);
        ulong now = Main.GameUpdateCount + 1;
        if (stamps[owner] != 0 && now - stamps[owner] < (ulong)interval) return false;
        stamps[owner] = now;
        return true;
    }

    // Embers and smoke where the pour lands; a spill of sparks at the lip; light down the column.
    private void Splash(Projectile p, EmberCenserMinion m, in CenserPour pour, Vector2 mouth, uint now)
    {
        float h = ScarletRewardParticles.Hash(seed + 5, (int)now);
        ScarletRewardFx.Particle(ScarletParticleKind.Ember, Owner, mouth + new Vector2((h - .5f) * pour.Radius * .6f, 4),
            new Vector2((h - .5f) * 1.6f, -.4f - h), 14 + 8 * h, 4, seed * 7 + now);
        if (!Landing(p, m, pour, pour.Age, out Vector2 land)) { Lighting.AddLight(mouth + new Vector2(0, 80), .7f, .18f, .06f); return; }
        Lighting.AddLight(land, .9f, .25f, .08f);
        Lighting.AddLight(Vector2.Lerp(mouth, land, .5f), .6f, .14f, .05f);
        int embers = pour.Grand ? 3 : 2;
        for (int i = 0; i < embers; i++)
        {
            float a = ScarletRewardParticles.Hash(seed + i, (int)now * 3 + 1), b = ScarletRewardParticles.Hash(seed + i, (int)now * 3 + 2);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, Owner, land + new Vector2((a - .5f) * pour.Radius * 1.4f, -4),
                new Vector2((a - .5f) * 3.2f, -1.2f - 2.2f * b), 16 + 12 * b, pour.Grand ? 6 : 5, seed * 11 + now + i);
        }
        if (h < .35f) ScarletRewardFx.Particle(ScarletParticleKind.Smoke, Owner, land + new Vector2((h - .5f) * pour.Radius, -10),
            new Vector2(0, -.7f), 32, pour.Grand ? 18 : 13, seed * 13 + now);
    }

    // The column's foot when it rests on the floor (world, relative to the censer's current ring).
    private static bool Landing(Projectile p, EmberCenserMinion m, in CenserPour pour, float age, out Vector2 land)
    {
        land = default;
        Span<NVector2> points = stackalloc NVector2[CenserRules.MaxColumnPoints];
        Span<float> radii = stackalloc float[CenserRules.MaxColumnPoints], times = stackalloc float[CenserRules.MaxColumnPoints];
        int n = CenserRules.Column(pour, pour.Start + age, m.Floors(pour), points, radii, times, out bool rests);
        if (!rests || n == 0) return false;
        land = p.Center + Xna(points[n - 1]);
        return true;
    }

    // The scar hand-off: when a pour's window ends, its column dries where it fell.
    private void Hand(Projectile p, EmberCenserMinion m)
    {
        bool live = m.LivePour(out var pour);
        if (livePour >= 0 && (!live || pour.Start != livePour)) Keep(p, m, lastPour with { Age = lastPour.Live }, p.Center);
        livePour = live ? pour.Start : -1;
        if (live) lastPour = pour;
    }

    // The censer is gone (dismissed, sacrificed, its owner gone): a pour still live, or one that ended on the update
    // that removed it (no PostAI follows a kill), dries where it was.
    internal void Release(Projectile p, EmberCenserMinion m)
    {
        if (livePour < 0) return;
        if (m.LivePour(out var pour) && pour.Start == livePour) Keep(p, m, pour, p.Center);
        else Keep(p, m, lastPour with { Age = lastPour.Live }, p.Center);
        livePour = -1;
    }

    private void Keep(Projectile p, EmberCenserMinion m, in CenserPour pour, Vector2 ring)
    {
        Span<NVector2> column = stackalloc NVector2[CenserRules.MaxColumnPoints];
        Span<float> radii = stackalloc float[CenserRules.MaxColumnPoints], times = stackalloc float[CenserRules.MaxColumnPoints];
        Span<Vector2> points = stackalloc Vector2[CenserRules.MaxColumnPoints];
        int n = CenserRules.Column(pour, pour.Start + pour.Age, m.Floors(pour), column, radii, times, out _);
        for (int i = 0; i < n; i++) points[i] = ring + Xna(column[i]); // top first
        CenserInk.Keep(Owner, InkSeed(pour), pour, points[..n]);
    }

    private float InkSeed(in CenserPour pour) => seed + pour.Start * .37f;

    // ---- Ink ----------------------------------------------------------------------------------------------------
    // The live column at the draw fraction (CenserRules.Column): from the mouth down to its foot, so the material's
    // flow runs down the stream. Each point's radius opens with its ignition exactly as it collides.
    internal void EmitColumn(ScarletInkCanvas canvas, Projectile p, EmberCenserMinion m, float fraction)
    {
        if (state != CenserState.Swing) return;
        float c = DrawClock(fraction);
        if (!CenserRules.TryPour(c, out var pour)) return;
        Vector2 ring = Vector2.Lerp(oldCenter, center, fraction);
        Span<NVector2> column = stackalloc NVector2[CenserRules.MaxColumnPoints];
        Span<float> radii = stackalloc float[CenserRules.MaxColumnPoints], times = stackalloc float[CenserRules.MaxColumnPoints];
        int n = CenserRules.Column(pour, c, m.Floors(pour), column, radii, times, out _);
        if (n == 0 || !canvas.Begin(ScarletRewardFx.Ink(Owner, ScarletInkLook.Live, InkSeed(pour), 1, fire: true))) return;
        Vector2 previous = default;
        for (int i = 0; i < n; i++)
        {
            // The radius passed is the pour's; the shader opens it with the time exactly as CenserRules collides.
            Vector2 at = ring + Xna(column[i]);
            if (i > 0) CenserInk.Subdivide(canvas, previous, at, pour.Radius, times[i], times[i - 1]);
            canvas.Point(at, pour.Radius, times[i]);
            previous = at;
        }
        canvas.End();
        // Droplets leap from where the stream lands and fall back: one launched every 2.3 ticks from the moment the
        // head first meets the floor, each on its own 9-tick arc (at most four at once, two under Reduced Effects).
        if (!Landing(p, m, pour, pour.Age, out Vector2 land)) return;
        float splash = pour.Age - CenserRules.Rest(pour, m.Floors(pour)[0]) / pour.Fall;
        if (splash < 0) return;
        land += ring - p.Center;
        var drop = ScarletRewardFx.Ink(Owner, ScarletInkLook.Live, InkSeed(pour) + 2.3f, 1, fire: true);
        const float every = 2.3f, arc = 9;
        int newest = (int)(splash / every), alive = canvas.Reduced ? 2 : 4;
        for (int i = Math.Max(0, newest - alive + 1); i <= newest; i++)
        {
            float t = splash - i * every;
            if (t < 1.5f || t >= arc) continue;
            float a = ScarletRewardParticles.Hash(InkSeed(pour) + i, i * 31 + 7), side = (i & 1) == 0 ? -1 : 1;
            Vector2 v = new(side * (1.4f + 1.8f * a), -2.6f - 1.6f * a);
            Vector2 now = land + v * t + new Vector2(0, .32f * t * t), before = land + v * (t - 1.5f) + new Vector2(0, .32f * (t - 1.5f) * (t - 1.5f));
            if (now.Y > land.Y + 2) continue;
            canvas.Droplet(drop, before, now, pour.Grand ? 3.4f : 2.6f, t + 3);
        }
    }

    // ---- Body -----------------------------------------------------------------------------------------------------
    internal void Draw(SpriteBatch batch, Projectile p, EmberCenserMinion m)
    {
        var body = CenserBody.Get();
        if (body is null) return;
        float fraction = ScarletRewardFx.Fraction;
        Pose(fraction, out Vector2 ring, out float angle, out float turn);
        Vector2 mouth = ring + Xna(CenserRules.MouthAt(angle));
        float rotation = Rotation(body, angle, turn);
        Color light = Lighting.GetColor((int)(mouth.X / 16f), (int)(mouth.Y / 16f));
        Color tint = Color.Lerp(light, Color.White, .55f);
        bool pixel = body.Pixel;
        float warm = Math.Clamp(heat, 0, 1.4f);
        var saved = WorldBatchParameters.Capture(batch);
        batch.End();
        bool begun = false;
        try
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, body.Sampler, saved.Depth, saved.Raster, null, saved.Transform);
            begun = true;
            if (drapesReady && !ScarletRewardFx.Reduced) DrawDrapes(batch, fraction, tint, pixel);
            batch.Draw(body.Texture, Screen(mouth, pixel), body.Source, tint, rotation, body.Mouth, body.Scale, SpriteEffects.None, 0);
            batch.End();
            begun = false;
            batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, saved.Depth, saved.Raster, null, saved.Transform);
            begun = true;
            // The crown warms with the build and spikes ember-gold through the brace.
            bool brace = state == CenserState.Swing && CenserRules.Bracing(DrawClock(fraction));
            Color crown = brace ? new Color(1f, .74f, .32f) * .55f : new Color(1f, .4f, .14f) * (.28f * Math.Clamp(warm - .15f, 0, 1));
            if (crown.A > 2 || crown.R > 2) batch.Draw(body.Texture, Screen(mouth, pixel), body.Source, crown, rotation, body.Mouth, body.Scale, SpriteEffects.None, 0);
            // The ember glow at the bowl mouth (no period: value noise drifts it).
            Texture2D soft = ScarletVfxHost.Assets.GetTexture("Luminance/BloomCircleSmall");
            Vector2 up = new(-MathF.Sin(angle + turn), -MathF.Cos(angle + turn));
            float flicker = .8f + .2f * Noise(seed, (Main.GameUpdateCount + fraction) * .11f);
            float glow = (.2f + .6f * Math.Clamp(warm, 0, 1.4f)) * flicker;
            Vector2 origin = new(soft.Width * .5f, soft.Height * .5f);
            Vector2 lip = mouth - up * 2;
            float size = 26f / soft.Width;
            batch.Draw(soft, lip - Main.screenPosition, null, new Color(1f, .14f, .07f) * (.65f * glow), MathF.Atan2(up.Y, up.X),
                origin, new Vector2(size * .65f, size), SpriteEffects.None, 0);
            batch.Draw(soft, lip - Main.screenPosition, null, new Color(1f, .62f, .3f) * (.45f * glow * Math.Clamp(warm, 0, 1)), 0,
                origin, size * .35f, SpriteEffects.None, 0);
        }
        finally
        {
            if (begun) batch.End();
            saved.Restore(batch);
        }
    }

    private void DrawDrapes(SpriteBatch batch, float fraction, Color tint, bool pixel)
    {
        Texture2D white = TextureAssets.MagicPixel.Value;
        var source = new Rectangle(0, 0, 1, 1);
        Color cloth = new Color(122, 14, 28).MultiplyRGB(tint), edge = new Color(30, 4, 9).MultiplyRGB(tint);
        for (int side = 0; side < 2; side++)
        {
            var chain = drapes[side];
            if (chain.Count != DrapePoints) continue;
            for (int pass = 0; pass < 2; pass++)
                for (int j = 0; j < DrapePoints - 1; j++)
                {
                    Vector2 a = Vector2.Lerp(oldDrapes[side][j], chain[j].Position, fraction), b = Vector2.Lerp(oldDrapes[side][j + 1], chain[j + 1].Position, fraction);
                    Vector2 d = b - a;
                    float length = d.Length();
                    if (length < .5f) continue;
                    float width = MathHelper.Lerp(5, 2, j / (DrapePoints - 2f)) + (pass == 0 ? 2 : 0);
                    if (pixel) width = MathF.Max(2, MathF.Round(width * .5f) * 2);
                    batch.Draw(white, Screen(a, pixel), source, pass == 0 ? edge : cloth, MathF.Atan2(d.Y, d.X), new Vector2(0, .5f),
                        new Vector2(length + (pass == 0 ? 1 : 0), width), SpriteEffects.None, 0);
                }
        }
    }

    // World to the projectile layer's screen space; final pixel art sits on the 2 px dot grid.
    private static Vector2 Screen(Vector2 world, bool pixel)
    {
        Vector2 s = world - Main.screenPosition;
        return pixel ? new Vector2(MathF.Floor(s.X * .5f) * 2f, MathF.Floor(s.Y * .5f) * 2f) : s;
    }

    private static Vector2 Xna(NVector2 v) => new(v.X, v.Y);

    // Smooth value noise in time: aperiodic, so nothing pulses.
    private static float Noise(float noiseSeed, float x)
    {
        int i = (int)MathF.Floor(x);
        float f = x - i, u = f * f * (3 - 2 * f);
        return MathHelper.Lerp(ScarletRewardParticles.Hash(noiseSeed, i), ScarletRewardParticles.Hash(noiseSeed, i + 1), u);
    }
}
