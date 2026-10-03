// Offline frames of the Sable Scythe's crescents (刈り月; docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable
// Scythe", Crescents) through the production ScarletRewardInk path passes and ScarletCrescentInk at actual size. Not a
// playtest: the dummies are fixed boxes, line of sight is always clear and the reaper is the stand-in of the measure scene.
//
//   pwsh tools/preview-scarlet.ps1 -Rewards -Only scythe-crescents
//
// scythe-crescents: one held measure (Over, Under, Over, Under, Whip) at a stationary reaper facing right, the cursor on
//   a boss-sized dummy with a crowd of three small dummies below the aim. Each Over and Under throws its crescent at age
//   9 (ticks 9, 27, 45, 63), the lash arc sheds the volley of five at 92; they steer by SableCrescentFlight, hit the
//   boss, chain into the crowd, break on their last target and leave their scars and spatters.
// scythe-crescents-boss: the same measure at a lone boss drifting down 1.5 px/tick: every crescent leads it and breaks
//   on it (nothing to chain to).
// scythe-crescents-contract: the crowd scene with each live crescent's collision capsules outlined (draw equals collide).
// The simulation is SableCrescent's owner logic on the pure rules (throw, acquisition, steering, collision, chaining,
// breaking, the live cap) and ScytheCrescentVisuals' client events (flares, drips, cinders, embers, hit droplets).
#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using F = Convergence.Content.Encounters.CrimsonFoundry.Rewards.SableCrescentFlight;
using NVector = System.Numerics.Vector2;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

internal sealed class CrescentSimulation
{
    internal sealed class Crescent
    {
        internal int Kind, BreakAge, Target = -1, Born, Ended = -1, Index;
        internal NVector Center, Velocity, Heading;
        internal float Age, NextAcquire;
        internal readonly bool[] Hit = new bool[8];
        internal int Hits;
        internal Vector2 BreakApex, BreakHeading;
        internal float BreakSide;
        internal bool Broken => BreakAge > 0;
    }
    internal readonly record struct Drop(Vector2 From, Vector2 Velocity, int Born, float Seed, float Radius, int Life);
    internal readonly record struct Spark(int Tick, Vector2 At, Vector2 Velocity, float Life, float Size, float Seed,
        ScarletParticleKind Kind = ScarletParticleKind.Ember);

    private readonly Vector2 center;
    private readonly Vector2[] dummyCenter, dummyHalf;
    private readonly Vector2 dummyVelocity, cursorOffset;
    internal readonly List<Crescent> Crescents = new();
    internal readonly List<Drop> Drops = new();
    internal readonly List<Spark> Sparks = new();
    internal int Tick { get; private set; } = -1;

    internal CrescentSimulation(Vector2 center, Vector2[] dummyCenter, Vector2[] dummyHalf, Vector2 dummyVelocity, Vector2 cursorOffset)
    {
        this.center = center; this.dummyCenter = dummyCenter; this.dummyHalf = dummyHalf; this.dummyVelocity = dummyVelocity; this.cursorOffset = cursorOffset;
    }

    internal Vector2 Shoulder => center + new Vector2(-2, -6);
    internal int Dummies => dummyCenter.Length;
    // Only the first dummy (the boss) moves.
    internal Vector2 DummyCenter(int i, float tick) => center + dummyCenter[i] + (i == 0 ? dummyVelocity * Math.Max(0, tick) : Vector2.Zero);
    internal Vector2 DummyHalf(int i) => dummyHalf[i];
    internal Vector2 Cursor(float tick) => DummyCenter(0, tick) + cursorOffset;

    internal void AdvanceTo(int tick)
    {
        if (tick < Tick) { Crescents.Clear(); Drops.Clear(); Sparks.Clear(); Tick = -1; }
        while (Tick < tick) Step(++Tick);
    }

    private static NVector N(Vector2 v) => new(v.X, v.Y);
    private static Vector2 X(NVector v) => new(v.X, v.Y);

    private void Step(int t)
    {
        Throw(t);
        foreach (var c in Crescents)
        {
            if (c.Ended >= 0 || c.Born >= t) continue;
            for (int u = 0; u < 1 + R.CrescentExtraUpdates && c.Ended < 0; u++) Update(c, t, t - 1 + (u + 1) * F.Dt);
        }
    }

    // SableStroke.Throw at the stroke ages of one held measure (strokes back to back from tick 0).
    private void Throw(int t)
    {
        if (t >= R.MeasureTicks) return;
        var (stroke, age) = ScythePreview.At(t);
        int kind = SableScytheMotion.Kind(stroke);
        if (kind != SableScytheMotion.Whip && age == R.CrescentThrowAge)
        {
            int crescent = kind == SableScytheMotion.Under ? F.Under : F.Over;
            Vector2 tip = Shoulder + ScythePreview.X(SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, age)));
            Spawn(t, crescent, tip, F.ThrowHeading(0, 1, crescent));
        }
        else if (kind == SableScytheMotion.Whip && age == R.VolleyAge)
        {
            Span<NVector> arc = stackalloc NVector[(R.WhipLiveEnd - R.WhipLiveStart) * 4 + 1];
            for (int i = 0; i < arc.Length; i++) arc[i] = N(Shoulder) + SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, R.WhipLiveStart + i / 4f));
            for (int k = 0; k < R.VolleyCrescents; k++)
                Spawn(t, F.FirstVolley + k, X(F.ArcPoint(arc, F.VolleyWritten(k), 4)), F.VolleyHeading(0, k, arc[0], arc[^1]));
        }
    }

    private void Spawn(int t, int kind, Vector2 apex, float heading)
    {
        int flying = 0; Crescent? oldest = null;
        foreach (var c in Crescents)
            if (c.Ended < 0 && !c.Broken) { flying++; if (oldest is null || c.Age > oldest.Age) oldest = c; }
        if (F.MustBreakOldest(flying) && oldest is not null) Break(oldest, t);
        NVector f = F.Unit(heading);
        var n = new Crescent { Kind = kind, Born = t, Heading = f, Index = Crescents.Count };
        n.Center = F.CenterFromApex(N(apex), f, kind);
        n.Velocity = f * F.LaunchSpeed(kind);
        n.Target = Find(n, t, false);
        Crescents.Add(n);
        float seed = n.Index * 31 + kind;
        int flares = F.IsVolley(kind) ? 1 : 3;
        float[] sizes = { 16, 9, 6 };
        for (int i = 0; i < flares; i++)
            Sparks.Add(new Spark(t, apex, X(f) * (1.2f + i * .4f), 8, F.IsVolley(kind) ? 9 : sizes[i], seed + i));
    }

    private int Find(Crescent c, float tick, bool chain)
    {
        Span<NVector> mins = stackalloc NVector[Dummies], maxs = stackalloc NVector[Dummies];
        Span<bool> skip = stackalloc bool[Dummies];
        for (int i = 0; i < Dummies; i++)
        {
            Vector2 d = DummyCenter(i, tick), h = DummyHalf(i);
            mins[i] = N(d - h); maxs[i] = N(d + h); skip[i] = c.Hit[i];
        }
        NVector cursor = F.ClampCursor(N(center), N(Cursor(tick)));
        return chain ? F.Chain(c.Center, mins, maxs, skip) : F.Acquire(cursor, c.Center, c.Heading, mins, maxs, skip);
    }

    private void Break(Crescent c, int t)
    {
        if (c.Broken) return;
        c.BreakAge = F.BreakAgeAt(c.Age);
        c.BreakApex = X(F.Apex(c.Center, c.Heading, c.Kind));
        c.BreakHeading = X(c.Heading);
        c.BreakSide = (c.Index & 1) == 0 ? 1 : -1;
    }

    // One update of SableCrescent.AI (owner), then the move and the collision, as Projectile.Update orders them.
    private void Update(Crescent c, int t, float tick)
    {
        float dt = F.Dt;
        c.Age = MathF.Min(c.Age + dt, R.CrescentLife + 1);
        if (F.Done(c.Age, c.BreakAge)) { c.Ended = t; return; }
        if (c.Broken) c.Velocity = F.Brake(c.Velocity, dt);
        else
        {
            if (c.Target >= 0 && c.Hit[c.Target]) c.Target = -1;
            if (c.Target < 0 && c.Age >= c.NextAcquire) { c.NextAcquire = c.Age + R.CrescentAcquireInterval; c.Target = Find(c, tick, false); }
            NVector? aim = null;
            if (c.Target >= 0)
                aim = F.AimPoint(c.Center, N(DummyCenter(c.Target, tick)), c.Target == 0 ? N(dummyVelocity) : NVector.Zero, c.Velocity.Length());
            c.Velocity = F.Steer(c.Velocity, c.Center, c.Age, c.Kind, aim, dt);
        }
        c.Center += c.Velocity * dt;
        if (c.Velocity.LengthSquared() > 1e-6f) c.Heading = NVector.Normalize(c.Velocity);
        Vector2 apex = X(F.Apex(c.Center, c.Heading, c.Kind)), heading = X(c.Heading), normal = new(-heading.Y, heading.X);
        float seed = c.Index * 31 + c.Kind;

        // In flight (ScytheCrescentVisuals): the wake's cinder every 2 ticks, a drip every 4 ticks from alternating tips,
        // a cinder every 3 off the apex.
        if (c.Age == MathF.Floor(c.Age) && !c.Broken)
        {
            int whole = (int)c.Age;
            if (whole % ScarletCrescentInk.WakeEvery == 0)
                Sparks.Add(new Spark(t, ScarletCrescentInk.WakeFrom(X(c.Center), heading, c.Kind, ScarletRewardParticles.Hash(seed, whole + 101)),
                    ScarletCrescentInk.WakeVelocity(heading), ScarletCrescentInk.WakeLife, ScarletCrescentInk.WakeSize(ScarletRewardParticles.Hash(seed, whole + 211)),
                    seed + whole * .53f, ScarletParticleKind.Cinder));
            if (whole % 4 == 0)
            {
                float side = (whole / 4 & 1) == 0 ? 1 : -1;
                Drops.Add(new Drop(ScarletCrescentInk.DripFrom(X(c.Center), heading, c.Kind, side), ScarletCrescentInk.DripVelocity(X(c.Velocity), heading, side), t, seed + whole,
                    ScarletCrescentInk.DripRadius, ScarletCrescentInk.DripLife));
            }
            if (whole % 3 == 0)
                Sparks.Add(new Spark(t, apex, -heading * 1.4f + normal * ((ScarletRewardParticles.Hash(seed, whole) - .5f) * 1.2f) + new Vector2(0, -.4f), 14, 5,
                    seed + whole * .37f, ScarletParticleKind.Cinder));
        }

        // Collision: the body as it collides now; once per dummy, at most 3; a hit on its target (or with none) chains.
        if (c.Hits >= R.CrescentRoots) return;
        Span<NVector> at = stackalloc NVector[R.CrescentSamples];
        Span<float> radius = stackalloc float[R.CrescentSamples];
        F.Body(c.Center, c.Heading, c.Kind, R.InkOpen(c.Age), at, radius);
        for (int i = 0; i < Dummies && c.Hits < R.CrescentRoots; i++)
        {
            if (c.Hit[i]) continue;
            Vector2 d = DummyCenter(i, tick), h = DummyHalf(i);
            if (!F.Touches(c.Center, c.Kind, at, radius, N(d - h), N(d + h))) continue;
            c.Hit[i] = true; c.Hits++;
            Vector2 contact = Vector2.Clamp(apex, d - h, d + h);
            for (int k = 0; k < 3; k++)
            {
                float turn = (ScarletRewardParticles.Hash(seed + i, k) * 2 - 1) * .6f, speed = 4 + 3 * ScarletRewardParticles.Hash(seed + i, k + 7);
                Drops.Add(new Drop(contact, Rotate(heading, turn) * speed + new Vector2(0, -1.2f), t, seed + i * 3 + k, ScarletCrescentInk.HitDropRadius, ScarletCrescentInk.HitDropLife));
            }
            for (int k = 0; k < 2; k++)
                Sparks.Add(new Spark(t, contact, Rotate(heading, (k - .5f) * 1.1f) * 1.8f + new Vector2(0, -.6f), 12, 7, seed + 11 + k));
            if (c.Broken) continue;
            if (c.Hits >= R.CrescentRoots) { Break(c, t); continue; }
            if (c.Target >= 0 && c.Target != i) continue;
            int next = Find(c, tick, true);
            if (next >= 0) c.Target = next; else Break(c, t);
        }
    }

    internal static Vector2 Rotate(Vector2 v, float angle)
    {
        float cs = MathF.Cos(angle), sn = MathF.Sin(angle);
        return new Vector2(v.X * cs - v.Y * sn, v.X * sn + v.Y * cs);
    }
}

internal abstract class ScytheCrescentSceneBase : IRewardsPreviewScene
{
    public abstract string Name { get; }
    public int[] Ticks { get; } = { 9, 11, 14, 18, 22, 27, 31, 36, 45, 54, 63, 76, 92, 94, 97, 100, 104, 110, 118, 128, 140, 160 };
    protected abstract CrescentSimulation Create(Vector2 center);
    protected virtual bool Contract => false;
    private CrescentSimulation? simulation;

    private CrescentSimulation Sim(Vector2 center) => simulation ??= Create(center);

    public void Emit(ScarletInkCanvas canvas, Vector2 center, int tick)
    {
        var sim = Sim(center);
        sim.AdvanceTo(tick);
        if (tick <= R.MeasureTicks) ScythePreview.EmitWake(canvas, sim.Shoulder, tick);
        ScythePreview.EmitLash(canvas, sim.Shoulder, tick);
        foreach (var c in sim.Crescents)
        {
            var style = new ScarletInkStyle(ScarletInkLook.Live, true, 40 + c.Index * .37f, Owner: 0);
            Vector2 heading = new(c.Heading.X, c.Heading.Y), at = new(c.Center.X, c.Center.Y);
            if (c.Ended < 0)
            {
                ScarletCrescentInk.Body(canvas, style, at, heading, c.Kind, c.Age, F.Remaining(c.Age, c.BreakAge), !c.Broken);
                if (c.Broken) ScarletCrescentInk.Spatter(canvas, style, c.BreakApex, c.BreakHeading, c.Kind, c.BreakSide, 1);
                continue;
            }
            float t = tick - c.Ended;
            if (t < 0 || t > R.CrescentScar) continue;
            float fade = 1 - t / R.CrescentScar;
            var scar = style with { Look = ScarletInkLook.Residue };
            ScarletCrescentInk.Scar(canvas, scar, at, heading, c.Kind, fade);
            if (c.Broken) ScarletCrescentInk.Spatter(canvas, scar, c.BreakApex, c.BreakHeading, c.Kind, c.BreakSide, fade);
        }
        foreach (var d in sim.Drops)
            ScarletCrescentInk.Drop(canvas, new ScarletInkStyle(ScarletInkLook.Live, true, d.Seed, 1, false, 0, 0), d.From, d.Velocity, tick - d.Born, d.Radius, d.Life);
    }

    public void Particles(ScarletRewardParticles particles, Vector2 center, int tick, bool reduced)
    {
        var sim = Sim(center);
        sim.AdvanceTo(tick);
        foreach (var s in sim.Sparks)
            if (s.Tick == tick) particles.Spawn(s.Kind, 0, true, s.At, s.Velocity, s.Life, s.Size, reduced, s.Seed);
        if (tick > R.MeasureTicks) return;
        // The swing's edge embers and glints, as the measure scene and ScytheStrokeVisuals spawn them.
        var (stroke, age) = ScythePreview.At(tick);
        var (s0, a0) = ScythePreview.At(Math.Max(0, tick - 1));
        NVector now = SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, age)), before = SableScytheMotion.Tip(SableScytheMotion.Pose(s0, a0));
        Vector2 tip = sim.Shoulder + ScythePreview.X(now);
        if (s0 == stroke && SableScytheMotion.Live(stroke, (int)age) && MathF.Sign(now.Y) != MathF.Sign(before.Y) && now.X > 0)
        {
            particles.Spawn(ScarletParticleKind.Ember, 0, true, tip, Vector2.Zero, 7, 18, reduced, tick);
            particles.Spawn(ScarletParticleKind.Ember, 0, true, tip, Vector2.Zero, 5, 9, reduced, tick + .5f);
        }
        if (SableScytheMotion.Live(stroke, (int)age))
        {
            Vector2 back = sim.Shoulder + ScythePreview.X(SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, age - .25f)));
            Vector2 along = tip - back;
            along = along.LengthSquared() > 1e-3f ? Vector2.Normalize(along) : Vector2.UnitX;
            float seed = age * 13 + 7;
            particles.Spawn(ScarletParticleKind.Ember, 0, true, tip, -along * 1.6f + new Vector2((ScarletRewardParticles.Hash(seed, 1) - .5f) * .6f, -.5f), 10, 6, reduced, seed);
        }
    }

    public void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 center, int tick)
    {
        var sim = Sim(center);
        var batch = renderer.Batch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        for (int i = 0; i < sim.Dummies; i++)
        {
            Vector2 d = sim.DummyCenter(i, tick), h = sim.DummyHalf(i);
            ScythePreview.Box(batch, renderer.Pixel, view, d - h, d + h, new Color(120, 120, 140));
        }
        Vector2 cursor = sim.Cursor(tick);
        ScythePreview.Line(batch, renderer.Pixel, view, cursor - new Vector2(6, 0), cursor + new Vector2(6, 0), new Color(200, 200, 210) * .6f, 1);
        ScythePreview.Line(batch, renderer.Pixel, view, cursor - new Vector2(0, 6), cursor + new Vector2(0, 6), new Color(200, 200, 210) * .6f, 1);
        ScythePreview.Reaper(batch, renderer.Pixel, view, center, sim.Shoulder, Math.Min(tick, R.MeasureTicks - .01f));
        if (Contract)
            foreach (var c in sim.Crescents)
            {
                if (c.Ended >= 0) continue;
                Span<NVector> at = stackalloc NVector[R.CrescentSamples];
                Span<float> radius = stackalloc float[R.CrescentSamples];
                F.Body(c.Center, c.Heading, c.Kind, R.InkOpen(c.Age), at, radius);
                for (int i = 0; i + 1 < at.Length; i++)
                {
                    Vector2 a = new(at[i].X, at[i].Y), b = new(at[i + 1].X, at[i + 1].Y), d = b - a;
                    float r = MathF.Min(radius[i], radius[i + 1]);
                    if (d.LengthSquared() < 1e-4f || r <= 0) continue;
                    Vector2 n = new Vector2(-d.Y, d.X); n.Normalize(); n *= r;
                    ScythePreview.Line(batch, renderer.Pixel, view, a + n, b + n, new Color(255, 255, 255, 220), 1);
                    ScythePreview.Line(batch, renderer.Pixel, view, a - n, b - n, new Color(255, 255, 255, 220), 1);
                }
            }
        batch.End();
    }
}

internal sealed class ScytheCrescentsScene : ScytheCrescentSceneBase
{
    public override string Name => "scythe-crescents";
    internal static CrescentSimulation Crowd(Vector2 center) => new(center,
        new[] { new Vector2(560, -60), new Vector2(330, 150), new Vector2(400, 215), new Vector2(470, 165) },
        new[] { new Vector2(60, 85), new Vector2(16, 22), new Vector2(16, 22), new Vector2(16, 22) }, Vector2.Zero, Vector2.Zero);
    protected override CrescentSimulation Create(Vector2 center) => Crowd(center);
}

internal sealed class ScytheCrescentsContractScene : ScytheCrescentSceneBase
{
    public override string Name => "scythe-crescents-contract";
    protected override bool Contract => true;
    protected override CrescentSimulation Create(Vector2 center) => ScytheCrescentsScene.Crowd(center);
}

internal sealed class ScytheCrescentsBossScene : ScytheCrescentSceneBase
{
    public override string Name => "scythe-crescents-boss";
    protected override CrescentSimulation Create(Vector2 center) => new(center, new[] { new Vector2(620, -170) }, new[] { new Vector2(70, 95) },
        new Vector2(0, 1.5f), Vector2.Zero);
}
