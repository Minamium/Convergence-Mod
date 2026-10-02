#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Num = System.Numerics.Vector2;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The Bloodink Quill's black blood (REWARDS.md, "Rogue - Bloodink Quill", Presentation). Client only; it reads the
// replicated carriers and decides nothing. Every path is sampled by QuillRules from the same QuillFlight the quill flew,
// so the ink is the trajectory (draw equals collide for the burn: the same samples, radius 18 opened by the shader).
//   flying quill   wet ink written behind it: dormant, with the ember-gold bead on the nib
//   BloodinkTrail  dried ink (dormant), a blot under 48 px; fades over its last 60 ticks; warm at a full build of eight;
//                  after the unroll it burns from the nib back (live), then dries (residue)
//   quill burst    a 56 px live disc at the nib, then its scar; the Sealed Score's 120/160 px burst likewise
//   flourish       five faint dormant staff lines flashing out of the unsealing score (harmless)
//   ghosts         a pulled-out or fallen-away quill tumbling down, and its ink fading (no hit, nothing replicated)
//   splashes       black-blood droplets: a few on a stick into flesh, a ring at each burst
// Time: the per-client tick count plus the draw fraction; a drawn state lags its update by up to one tick, like every
// other interpolated weapon body. Nothing reads the music or a world beat clock.
[Autoload(Side = ModSide.Client)]
internal sealed class QuillInk : ModSystem, IScarletInkEmitter
{
    private const int MaxSplashes = 32, MaxInkGhosts = 16, MaxBodyGhosts = 16;
    internal const int InkGhostLife = 20, BodyGhostLife = 26, SplashLife = 22;

    private struct Splash
    {
        internal int Owner, Count;
        internal Vector2 At;
        internal float Angle, Spread, Speed, Radius, Seed;
        internal long Start;
        internal bool Live;
    }

    private struct InkGhost
    {
        internal int Owner;
        internal Vector2 Origin, Launch;
        internal float Stop, Opacity, Seed;
        internal long Start;
        internal bool Blot;
    }

    private struct BodyGhost
    {
        internal int Owner;
        internal Vector2 At, Velocity;
        internal float Rotation, Spin, Opacity;
        internal long Start;
        internal bool Score;
    }

    private static readonly Splash[] splashes = new Splash[MaxSplashes];
    private static readonly InkGhost[] inkGhosts = new InkGhost[MaxInkGhosts];
    private static readonly BodyGhost[] bodyGhosts = new BodyGhost[MaxBodyGhosts];
    private static readonly QuillInkSample[] wet = new QuillInkSample[QuillRules.MaxStrokeSamples];
    private static readonly int[] standing = new int[256];
    private static int nextSplash, nextInkGhost, nextBodyGhost;

    private static long Now => QuillCarriers.Now;
    private static Vector2 X(Num v) => new(v.X, v.Y);
    private static Num N(Vector2 v) => new(v.X, v.Y);
    internal static float Seed(int owner, int serial) => (serial * .618034f + owner * 3.7f) % 89f;

    public override void Load() => ScarletRewardInk.Register(this);

    public override void Unload()
    {
        ScarletRewardInk.Unregister(this);
        Clear();
        QuillArt.Reset();
    }

    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();

    private static void Clear()
    {
        Array.Clear(splashes); Array.Clear(inkGhosts); Array.Clear(bodyGhosts);
        for (int i = 0; i < MaxSplashes; i++) splashes[i].Start = long.MinValue;
        for (int i = 0; i < MaxInkGhosts; i++) inkGhosts[i].Start = long.MinValue;
        for (int i = 0; i < MaxBodyGhosts; i++) bodyGhosts[i].Start = long.MinValue;
        nextSplash = nextInkGhost = nextBodyGhost = 0;
    }

    // ---- Events from QuillVisuals (bounded rings: the oldest is overwritten) ---------------------------------------

    internal static void AddSplash(int owner, Vector2 at, float angle, float spread, float speed, int count, float radius, bool live, float seed)
    {
        splashes[nextSplash] = new Splash
        {
            Owner = owner, At = at, Angle = angle, Spread = spread, Speed = speed, Count = count, Radius = radius, Live = live, Seed = seed, Start = Now,
        };
        nextSplash = (nextSplash + 1) % MaxSplashes;
    }

    internal static void AddInkGhost(int owner, Vector2 origin, Vector2 launch, float stop, float opacity, float seed, bool blot)
    {
        if (opacity <= .01f) return;
        inkGhosts[nextInkGhost] = new InkGhost
        {
            Owner = owner, Origin = origin, Launch = launch, Stop = stop, Opacity = opacity, Seed = seed, Blot = blot, Start = Now,
        };
        nextInkGhost = (nextInkGhost + 1) % MaxInkGhosts;
    }

    internal static void AddBodyGhost(int owner, Vector2 at, Vector2 velocity, float rotation, float spin, float opacity, bool score)
    {
        if (opacity <= .01f) return;
        bodyGhosts[nextBodyGhost] = new BodyGhost
        {
            Owner = owner, At = at, Velocity = velocity, Rotation = rotation, Spin = spin, Opacity = opacity, Score = score, Start = Now,
        };
        nextBodyGhost = (nextBodyGhost + 1) % MaxBodyGhosts;
    }

    // ---- Ink ------------------------------------------------------------------------------------------------------

    public void Emit(ScarletInkCanvas canvas, in ScarletView view)
    {
        float f = view.Fraction;
        Array.Clear(standing);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is BloodinkTrail { Standing: true } && p.owner is >= 0 and < 256) standing[p.owner]++;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            switch (p.ModProjectile)
            {
                case BloodinkTrail t: Trail(canvas, p, t, f); break;
                case BloodinkQuill q: Quill(canvas, p, q, f); break;
                case SealedScore s: Score(canvas, p, s, f); break;
            }
        }
        Ghosts(canvas, f);
        Splashes(canvas, f);
    }

    private static void Trail(ScarletInkCanvas canvas, Projectile p, BloodinkTrail t, float f)
    {
        var s = t.Samples;
        if (s.Length == 0) return;
        int owner = p.owner;
        float seed = Seed(owner, t.Serial);
        bool claimed = !t.Standing;
        float tau = t.Unrolled ? t.Tau - 1 + f : float.NegativeInfinity;
        Vector2 nib = X(s[^1].At);
        if (tau < 0)
        {
            // The build: dried ink. A claimed stroke waiting for its unroll, or a full build of eight, warms its lip.
            float opacity = claimed ? 1 : QuillRules.InkFade(p.timeLeft);
            float warmth = claimed || standing[owner] >= R.MaxQuills ? 1 : 0;
            var dormant = ScarletRewardFx.Ink(owner, ScarletInkLook.Dormant, seed, opacity, false, warmth);
            if (t.Blot) { canvas.Disc(dormant, nib, QuillRules.BlotRadius, 0); return; }
            if (!canvas.Begin(dormant)) return;
            for (int i = 0; i < s.Length; i++) canvas.Point(X(s[i].At), s[i].Radius);
            canvas.End();
            return;
        }
        if (t.Blot) { Burst(canvas, owner, seed, nib, R.BurnRadius, tau, R.BurnLive); return; }
        // Ages rise toward the nib: [0, a) waits dormant, [a, b) burns, [b, c) dries, the rest is gone.
        int a = 0, b, c, n = s.Length;
        while (a < n && QuillRules.BurnAge(tau, s[a].FromNib) < 0) a++;
        for (b = a; b < n && QuillRules.BurnAge(tau, s[b].FromNib) < R.BurnLive; b++) { }
        for (c = b; c < n && QuillRules.BurnAge(tau, s[c].FromNib) < R.BurnLive + QuillRules.Residue; c++) { }
        if (a > 0 && canvas.Begin(ScarletRewardFx.Ink(owner, ScarletInkLook.Dormant, seed, 1, false, 1)))
        {
            for (int i = 0; i <= Math.Min(a, n - 1); i++) canvas.Point(X(s[i].At), s[i].Radius);
            canvas.End();
        }
        if (c > b && canvas.Begin(ScarletRewardFx.Ink(owner, ScarletInkLook.Residue, seed)))
        {
            // The scar starts under the last burning sample, so live and dried ink join without a gap.
            for (int i = Math.Max(0, b - 1); i < c; i++)
                canvas.Point(X(s[i].At), R.BurnRadius, Math.Clamp(1 - (QuillRules.BurnAge(tau, s[i].FromNib) - R.BurnLive) / QuillRules.Residue, 0, 1));
            canvas.End();
        }
        if (b > a && canvas.Begin(ScarletRewardFx.Ink(owner, ScarletInkLook.Live, seed) with { Window = R.BurnLive }))
        {
            // Exactly the samples that collide: radius 18, opened by the shader over 3 ticks since each ignited.
            for (int i = a; i < b; i++) canvas.Point(X(s[i].At), R.BurnRadius, Math.Max(0, QuillRules.BurnAge(tau, s[i].FromNib)));
            canvas.End();
        }
    }

    private static void Quill(ScarletInkCanvas canvas, Projectile p, BloodinkQuill q, float f)
    {
        int owner = p.owner;
        float seed = Seed(owner, q.Serial);
        if (q.Flying && !q.Held)
        {
            if (q.Started) Wet(canvas, owner, seed, q.Origin, q.Launch, Math.Max(0, q.Age - 1 + f), 1, true, false);
            return;
        }
        // Just stuck: until this client has the trail, the quill keeps showing the ink it wrote.
        var visuals = p.GetGlobalProjectile<QuillVisuals>();
        if (q.Started && q.StopTime >= 0 && visuals.StuckFor is >= 0 and < 20 && q.Trail() is null)
            Wet(canvas, owner, seed, q.Origin, q.Launch, q.StopTime, 1, false, false);
        if (q.BurstBegun) Burst(canvas, owner, seed, p.Center, R.QuillBurstRadius, q.BurstSince - 1 + f, QuillRules.QuillBurstLive);
    }

    private static void Score(ScarletInkCanvas canvas, Projectile p, SealedScore s, float f)
    {
        int owner = p.owner;
        float seed = Seed(owner, 700 + Math.Max(0, s.Cast));
        float windup = s.Age - 1 + f - s.Flight;
        Vector2 at = p.Center;
        if (windup >= 0 && windup < R.ScoreWindup)
        {
            // The flourish: five faint staff lines flash outward from the unsealing score, each half with a writing bead.
            float half = QuillRules.FlourishHalfLength(windup), opacity = QuillRules.FlourishOpacity(windup);
            if (opacity > .01f)
                for (int line = 0; line < 5; line++)
                {
                    float y = QuillRules.FlourishRow(line);
                    var style = ScarletRewardFx.Ink(owner, ScarletInkLook.Dormant, seed + line * 1.3f, opacity);
                    canvas.Line(style, at + new Vector2(-6, y), at + new Vector2(-half, y), 2.6f, 2f, 0, 0, true);
                    canvas.Line(style, at + new Vector2(6, y), at + new Vector2(half, y), 2.6f, 2f, 0, 0, true);
                }
        }
        Burst(canvas, owner, seed, at, QuillRules.ScoreBurstRadius(s.Quills), s.BurstSince - 1 + f, QuillRules.ScoreBurstLive);
    }

    // A disc of live black blood for `live` ticks (opened by the shader over 3), then its scar.
    private static void Burst(ScarletInkCanvas canvas, int owner, float seed, Vector2 at, float radius, float since, int live)
    {
        if (since < 0) return;
        if (since < live) canvas.Disc(ScarletRewardFx.Ink(owner, ScarletInkLook.Live, seed) with { Window = live }, at, radius, since);
        else if (since < live + QuillRules.Residue)
            canvas.Disc(ScarletRewardFx.Ink(owner, ScarletInkLook.Residue, seed), at, radius, 1 - (since - live) / QuillRules.Residue);
    }

    // Ink as QuillFlight wrote it up to `stop`: dormant, the bead on the nib while it is still being written.
    private static void Wet(ScarletInkCanvas canvas, int owner, float seed, Vector2 origin, Vector2 launch, float stop, float opacity, bool bead, bool blot)
    {
        int n = QuillRules.SampleStroke(N(origin), N(launch), stop, wet, out float length);
        if (n < 1) return;
        var style = ScarletRewardFx.Ink(owner, ScarletInkLook.Dormant, seed, opacity);
        if (blot || n < 2 || (!bead && QuillRules.IsBlot(length)))
        {
            if (!bead) canvas.Disc(style, X(wet[n - 1].At), QuillRules.BlotRadius, 0);
            return;
        }
        if (!canvas.Begin(style)) return;
        for (int i = 0; i < n; i++) canvas.Point(X(wet[i].At), wet[i].Radius);
        canvas.End(bead);
    }

    private static void Ghosts(ScarletInkCanvas canvas, float f)
    {
        for (int i = 0; i < MaxInkGhosts; i++)
        {
            ref readonly var g = ref inkGhosts[i];
            float age = Now - g.Start - 1 + f;
            if (g.Start == long.MinValue || age < 0 || age >= InkGhostLife) continue;
            float fade = 1 - R.Smooth(age / InkGhostLife);
            Wet(canvas, g.Owner, g.Seed, g.Origin, g.Launch, g.Stop, g.Opacity * fade, false, g.Blot);
        }
    }

    private static void Splashes(ScarletInkCanvas canvas, float f)
    {
        for (int i = 0; i < MaxSplashes; i++)
        {
            ref readonly var s = ref splashes[i];
            float age = Now - s.Start - 1 + f;
            if (s.Start == long.MinValue || age < 0 || age >= SplashLife) continue;
            var style = ScarletRewardFx.Ink(s.Owner, s.Live ? ScarletInkLook.Live : ScarletInkLook.Dormant, s.Seed);
            for (int k = 0; k < s.Count; k++)
            {
                float h = ScarletRewardParticles.Hash(s.Seed, k * 3 + 1), g = ScarletRewardParticles.Hash(s.Seed, k * 3 + 2);
                float angle = s.Angle + (h - .5f) * s.Spread, speed = s.Speed * (.55f + .75f * g);
                Vector2 v = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
                Vector2 now = Droplet(s.At, v, age), before = Droplet(s.At, v, Math.Max(0, age - 1.5f));
                float shrink = 1 - R.Smooth(age / SplashLife);
                if (!canvas.Droplet(style, before, now, s.Radius * (.6f + .4f * g) * shrink + .4f, s.Live ? age : 0)) return;
            }
        }
    }

    private static Vector2 Droplet(Vector2 at, Vector2 v, float t) => at + v * t + new Vector2(0, .3f * t * t);

    // ---- Ghost bodies: pixel bodies above the ink layer, beneath NPCs ----------------------------------------------

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        ScarletRewardInk.DrawWorld(); // idempotent per frame: the ink lies under these bodies
        bool any = false;
        for (int i = 0; i < MaxBodyGhosts && !any; i++) any = Now - bodyGhosts[i].Start < BodyGhostLife + 1 && bodyGhosts[i].Start != long.MinValue;
        if (!any) return;
        var view = ScarletRewardInk.View();
        var batch = Main.spriteBatch;
        Matrix world = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
        var quill = QuillArt.Quill();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, quill.Sampler, DepthStencilState.None, RasterizerState.CullNone, null, world);
        try
        {
            for (int i = 0; i < MaxBodyGhosts; i++)
            {
                ref readonly var g = ref bodyGhosts[i];
                float age = Now - g.Start - 1 + view.Fraction;
                if (g.Start == long.MinValue || age < 0 || age >= BodyGhostLife) continue;
                Vector2 at = g.At + g.Velocity * age + new Vector2(0, .2f * age * age);
                float alpha = g.Opacity * (1 - R.Smooth(age / BodyGhostLife));
                Color light = Lighting.GetColor((int)(at.X / 16), (int)(at.Y / 16));
                var art = g.Score ? QuillArt.Score() : quill;
                QuillVisuals.Draw(batch, art, at, g.Rotation + g.Spin * age, Color.Lerp(light, Color.White, .45f) * alpha, Vector2.One);
            }
        }
        finally { batch.End(); }
    }
}
