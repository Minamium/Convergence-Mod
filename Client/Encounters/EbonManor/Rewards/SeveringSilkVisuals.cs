#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Num = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Presentation of Severing Silk (REWARDS.md "Rogue"). Client only: it reads the projectiles' replicated state
// and never decides a hit. Silk, knots, the cut, the placeholder spool/scissors and every spark are pixel-layer
// primitives from retained sources; final spool/scissors art is drawn in PreDraw at EbonRewardArt.PixelScale
// with point sampling: "Spool" (11x18, upright, tumbling about its centre) and "ScissorsClosed"/"ScissorsOpen"
// (35x18, beak pointing right, finger loops behind) on one canvas whose beak hinge sits on the projectile centre.
// Time is the per-client tick count plus WeaponDrawClock.Fraction (canvas.Fraction); the sever clock is the
// strand's own Phase, so the strands of one snip share a beat exactly.
[Autoload(Side = ModSide.Client)]
internal sealed class SeveringSilkVisuals : GlobalProjectile
{
    private const int KnotGlint = 24, PinGlint = 10;
    // Anchor from the art report (.local/ebon-reward-art/report.json, texture space with (0,0) at the top-left
    // corner): the beak hinge shared by both scissors canvases, 26 texels (52 px) in from the loops.
    private static readonly Vector2 ScissorsHinge = new(26f, 8f);
    // The two scissors frames swap where the beak has opened this far; it stays shut for the whole flight.
    private const float OpenFrame = .3f;
    private static readonly List<ScreenShakeSystem.ShakeInfo> shakes = new(4);
    private static long popTick;
    private static int pops;
    private static bool ScissorsArt => EbonRewardArt.HasFinal("ScissorsClosed") && EbonRewardArt.HasFinal("ScissorsOpen");

    private Vector2 previous, current;
    private float previousSpin, currentSpin, armedSettle = 1;
    private bool started, registered;
    private long born;
    private int lastPhase;
    private IEbonPixelSource? source;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is EbonSilkSpool or EbonSilkStrand or EbonSilkScissors;

    internal Vector2 At(Projectile p, float fraction) => started ? Vector2.Lerp(previous, current, fraction) : p.Center;
    internal float Spin(Projectile p, float fraction) => started ? MathHelper.Lerp(previousSpin, currentSpin, fraction) : p.rotation;
    // Ticks since this client first saw the projectile (the pin age of a strand).
    internal float Age(float fraction) => started ? (EbonSilkUtil.Now - born) + fraction : 0;

    // Resting bow of a strand: the settle after the pin, straightened away by the tightening, none once parted.
    internal float Bow(Projectile p, EbonSilkStrand s, float fraction)
    {
        int phase = s.Phase;
        if (phase > EbonRewardRules.TightenTicks) return 0;
        float sag = EbonSilkMath.Sag(Vector2.Distance(p.Center, s.End));
        if (phase >= 1) return sag * armedSettle * (1 - EbonRewardRules.Smooth((phase - 1 + fraction) / EbonSilkMath.StraightenTicks));
        return sag * EbonSilkMath.Settle(Age(fraction));
    }

    internal readonly record struct ScissorsPose(Vector2 Center, float Angle, float Gape, float Alpha, float Time);

    // The scissors on their own clock `t` (ticks since the arming tick, negative while braking in): they
    // tremble while the strands tighten, snap shut and kick back at the parting, then dissolve.
    internal ScissorsPose Pose(Projectile p, EbonSilkScissors s, float fraction)
    {
        int phase = s.Phase;
        float t = phase > 0 ? phase - 1 + fraction : s.Age - s.Flight + fraction;
        Vector2 heading = s.Heading, center = At(p, fraction);
        if (t > 0 && t < EbonRewardRules.TightenTicks)
            center += new Vector2(-heading.Y, heading.X) * (MathF.Sin(t * 9f) * 1.3f * t / EbonRewardRules.TightenTicks);
        float since = t - EbonRewardRules.TightenTicks;
        if (since > 0) center -= heading * (7f * EbonRewardRules.Smooth(since / 3f) * (1 - EbonRewardRules.Smooth((since - 3f) / 10f)));
        float alpha = 1 - EbonRewardRules.Smooth((t - 10f) / 11f);
        return new(center, heading.ToRotation(), EbonSilkMath.Gape(t), alpha, t);
    }

    public override void PostAI(Projectile p)
    {
        if (Main.dedServ) return;
        if (!started)
        {
            started = true; born = EbonSilkUtil.Now;
            previous = current = p.Center; previousSpin = currentSpin = p.rotation;
            Begin(p);
        }
        else
        {
            previous = current; current = p.Center;
            previousSpin = currentSpin; currentSpin = p.rotation;
        }
        // A full or disabled layer is retried; the sound above already played once.
        if (!registered && source is not null) registered = EbonPixelLayer.Add(source);
        switch (p.ModProjectile)
        {
            case EbonSilkStrand strand: Tick(p, strand); break;
            case EbonSilkScissors scissors: Tick(p, scissors); break;
        }
    }

    private void Begin(Projectile p)
    {
        int seed = p.identity * 31 + p.owner * 7 + p.type;
        switch (p.ModProjectile)
        {
            case EbonSilkSpool spool:
                EbonRewardAudio.Play("SpoolThrow", p.Center, .62f, 0, .06f, 3);
                source = new SpoolSource(p, spool, spool.Origin, seed);
                break;
            case EbonSilkStrand strand:
                EbonRewardAudio.Play("SilkPin", strand.End, .55f, 0, .05f, 3);
                source = new StrandSource(p, strand, seed);
                break;
            case EbonSilkScissors scissors:
                EbonRewardAudio.Play("SpoolThrow", p.Center, .66f, .35f, .02f, 2);
                source = new ScissorsSource(p, scissors, seed);
                break;
        }
    }

    // Sounds fire from the same tick as the visual they belong to: the scissors' bite as the silk tightens, the
    // chord with the parting, each note with its strand's curls.
    private void Tick(Projectile p, EbonSilkStrand s)
    {
        int phase = s.Phase;
        if (phase >= 1 && lastPhase < 1) armedSettle = EbonSilkMath.Settle(Age(0));
        Vector2 cutAt = Vector2.Lerp(p.Center, s.End, s.Cut);
        if (lastPhase < EbonSilkMath.SeverPhase && phase >= EbonSilkMath.SeverPhase)
        {
            Lighting.AddLight(cutAt, .5f, .55f, .8f);
            if (s.Rank == 0) EbonRewardAudio.Play("SeverAll", cutAt, .8f, 0, 0, 2);
        }
        int at = EbonSilkMath.CascadeAt(s.Rank);
        if (s.Rank > 0 && lastPhase < at && phase >= at)
            EbonRewardAudio.Note(EbonSilkMath.NoteFor(s.Rank, s.Count), cutAt, .5f);
        lastPhase = phase;
    }

    private void Tick(Projectile p, EbonSilkScissors s)
    {
        int phase = s.Phase;
        // The bite comes on the arming tick, six ticks ahead of the chord (the audition combo's order), so the
        // build-up is heard and the parting is left to SeverAll alone; it is also the quieter cue.
        if (lastPhase < 1 && phase >= 1)
            EbonRewardAudio.Play("ScissorsSnip", p.Center, .65f, 0, .02f, 2);
        if (lastPhase < EbonSilkMath.SeverPhase && phase >= EbonSilkMath.SeverPhase)
        {
            Lighting.AddLight(p.Center, .55f, .6f, .85f);
            if (p.owner == Main.myPlayer)
            {
                int armed = 0, type = ModContent.ProjectileType<EbonSilkStrand>();
                foreach (Projectile q in Main.ActiveProjectiles)
                    if (q.owner == p.owner && q.type == type && q.ModProjectile is EbonSilkStrand { Phase: > 0 }) armed++;
                Shake(p.Center, 2.6f + .25f * Math.Min(armed, 8), s.Heading);
            }
        }
        lastPhase = phase;
    }

    // Shake is owner-only and honours both Reduced Effects and the shake switch.
    private static void Shake(Vector2 at, float strength, Vector2 direction)
    {
        shakes.RemoveAll(info => info.ShakeStrength <= .01f);
        if (EbonVisuals.Reduced || !ModContent.GetInstance<EbonVisualConfig>().ScreenShake || shakes.Count >= 4) return;
        shakes.Add(ScreenShakeSystem.StartShakeAtPoint(at, strength, shakeDirection: direction.SafeNormalize(Vector2.UnitX),
            angularVariance: .35f, shakeStrengthDissipationIncrement: .55f));
    }
    internal static void ClearShakes()
    {
        foreach (var info in shakes) info.ShakeStrength = 0;
        shakes.Clear();
        popTick = 0; pops = 0;
    }

    // Local feedback for the native hit callback (owner only): a small star where the silk met the enemy.
    public override void OnHitNPC(Projectile p, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ || damageDone <= 0) return;
        Vector2 at = p.Center;
        float size = 1;
        switch (p.ModProjectile)
        {
            case EbonSilkStrand strand:
            {
                Vector2 a = p.Center, ab = strand.End - a;
                float length = ab.LengthSquared();
                float t = length < 1e-4f ? 0 : Math.Clamp(Vector2.Dot(target.Center - a, ab) / length, 0, 1);
                at = a + ab * t; size = 1.3f;
                break;
            }
            case EbonSilkScissors: size = 1.6f; break;
        }
        at = new(Math.Clamp(at.X, target.position.X, target.position.X + target.width),
            Math.Clamp(at.Y, target.position.Y, target.position.Y + target.height));
        long now = EbonSilkUtil.Now;
        if (popTick != now) { popTick = now; pops = 0; }
        if (++pops > 6) return;
        EbonPixelLayer.Add(new Pop(at, p.identity * 17 + target.whoAmI, size));
    }

    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        float f = WeaponDrawClock.Fraction;
        switch (p.ModProjectile)
        {
            case EbonSilkSpool when EbonRewardArt.HasFinal("Spool"):
            {
                Texture2D spool = EbonRewardArt.Final("Spool");
                DrawSprite(spool, spool.Size() * .5f, At(p, f), Spin(p, f), Color.White, SpriteEffects.None);
                break;
            }
            case EbonSilkScissors s when ScissorsArt:
            {
                ScissorsPose pose = Pose(p, s, f);
                if (pose.Alpha <= .01f) break;
                // Shut in flight, open from the arming tick until the parting, shut again from the parting on.
                bool open = pose.Gape > OpenFrame && pose.Time < EbonRewardRules.TightenTicks;
                // Beak along the heading; flying leftward the sprite is mirrored so the scissors stay right way up.
                DrawSprite(EbonRewardArt.Final(open ? "ScissorsOpen" : "ScissorsClosed"), ScissorsHinge, pose.Center, pose.Angle,
                    Color.White * pose.Alpha, MathF.Cos(pose.Angle) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
                break;
            }
        }
        return false; // everything else is drawn by the pixel layer
    }

    // Final art is pixel art: one texel per logical pixel at the 2 px dot, point sampling. `anchor` is the texel
    // position (texture space) that lands on `center`; a vertical flip mirrors the texture but not the origin,
    // so the anchor is mirrored with it. The caller's batch state is captured and restored around the swap.
    private static void DrawSprite(Texture2D texture, Vector2 anchor, Vector2 center, float rotation, Color color, SpriteEffects effects)
    {
        if ((effects & SpriteEffects.FlipVertically) != 0) anchor.Y = texture.Height - anchor.Y;
        SpriteBatch batch = Main.spriteBatch;
        WorldBatchParameters saved = WorldBatchParameters.Capture(batch);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
            saved.Raster, null, saved.Transform);
        batch.Draw(texture, center - Main.screenPosition, null, color, rotation, anchor,
            EbonRewardArt.PixelScale, effects, 0);
        batch.End();
        saved.Restore(batch);
    }

    // ---- Pixel-layer sources --------------------------------------------------------------------------

    private abstract class Source<T> : IEbonPixelSource where T : ModProjectile
    {
        protected readonly Projectile P;
        protected readonly T Model;
        protected readonly int Seed;
        protected Source(Projectile p, T model, int seed) { P = p; Model = model; Seed = seed; }
        // The slot may be reused by a new projectile; the ModProjectile instance never is.
        protected bool Alive => P.active && ReferenceEquals(P.ModProjectile, Model);
        protected SeveringSilkVisuals Visuals => P.GetGlobalProjectile<SeveringSilkVisuals>();
        public abstract bool Emit(EbonPixelCanvas canvas);
    }

    // The thrown spool unspooling its strand from the throw origin; placeholder body when no final art.
    private sealed class SpoolSource : Source<EbonSilkSpool>
    {
        private readonly Vector2 origin;
        internal SpoolSource(Projectile p, EbonSilkSpool model, Vector2 origin, int seed) : base(p, model, seed) => this.origin = origin;

        public override bool Emit(EbonPixelCanvas c)
        {
            if (!Alive) return false;
            float f = c.Fraction;
            SeveringSilkVisuals g = Visuals;
            Vector2 tip = g.At(P, f);
            if (Vector2.DistanceSquared(origin, tip) > 100)
            {
                c.Thread(origin, tip, EbonTone.Charcoal, 2);
                c.Thread(origin, tip, EbonTone.Silver, 1, .9f, .6f);
                c.Dot(origin, EbonTone.Ivory, 2);
            }
            if (!EbonRewardArt.HasFinal("Spool"))
            {
                Vector2 axis = g.Spin(P, f).ToRotationVector2(), side = new(-axis.Y, axis.X);
                c.Thread(tip - axis * 5, tip + axis * 5, EbonTone.Charcoal, 3);
                c.Thread(tip - axis * 4 + side * 1.5f, tip + axis * 4 + side * 1.5f, EbonTone.Silver, 1, .8f);
                c.Thread(tip + axis * 6 - side * 6, tip + axis * 6 + side * 6, EbonTone.Silver, 2);
                c.Thread(tip - axis * 6 - side * 6, tip - axis * 6 + side * 6, EbonTone.Silver, 2);
            }
            return true;
        }
    }

    // A pinned strand through its whole life: pin glint, settling bow, knots, tightening, parting and recoil.
    private sealed class StrandSource : Source<EbonSilkStrand>
    {
        internal StrandSource(Projectile p, EbonSilkStrand model, int seed) : base(p, model, seed) { }

        public override bool Emit(EbonPixelCanvas c)
        {
            if (!Alive) return false;
            float f = c.Fraction;
            SeveringSilkVisuals g = Visuals;
            EbonSilkStrand s = Model;
            Vector2 a = P.Center, b = s.End;
            int phase = s.Phase;
            if (phase > 0)
            {
                Armed(c, g, s, a, b, phase - 1 + f, f);
                return true;
            }
            float fade = Math.Clamp((P.timeLeft - f) / EbonSilkMath.FadeTicks, 0, 1);
            float age = g.Age(f);
            if (s.State < 0)
            {
                // Retired for a newer strand: the silk quivers loose and frays away.
                float k = Math.Clamp(s.RetiredFor + f, 0, EbonSilkMath.RetireTicks);
                float alpha = fade * (1 - k / EbonSilkMath.RetireTicks);
                c.String(a, b, EbonTone.Charcoal, 6 * k / EbonSilkMath.RetireTicks, k * 2.1f, 2, alpha);
                c.String(a, b, EbonTone.Silver, 6 * k / EbonSilkMath.RetireTicks, k * 2.1f, 1, alpha * .8f);
                c.Burst(Vector2.Lerp(a, b, .5f), Seed, c.Reduced ? 2 : 4, k, EbonSilkMath.RetireTicks, 1.4f, .03f, EbonShardKind.Silk);
                return true;
            }
            float bow = g.Bow(P, s, f);
            Silk(c, a, b, bow, fade, 0);
            c.Dot(a, EbonTone.Silver, 2, fade);
            c.Dot(b, EbonTone.Silver, 2, fade);
            if (age < PinGlint)
            {
                c.Star(b, 9, age / PinGlint, Seed);
                if (!c.Reduced) c.Ring(b, 2 + age * 1.6f, EbonTone.Ivory, 1, 1 - age / PinGlint);
            }
            Knots(c, a, b, bow, fade, age, f);
            return true;
        }

        // Black silk with a silver sheen; tension 0..1 brightens it toward moonlight as it is drawn tight. A bowed
        // strand is one standing half-wave (the same curve EbonSilkMath.Bow gives the knots); a straight one is
        // a thread, which can carry the travelling shimmer.
        private static void Silk(EbonPixelCanvas c, Vector2 a, Vector2 b, float bow, float alpha, float tension)
        {
            EbonTone core = tension > .55f ? EbonTone.Moon : EbonTone.Silver;
            int body = tension > .8f ? 3 : 2;
            if (bow > .3f)
            {
                if (EbonSilkMath.Flipped(EbonSilkUtil.N(a), EbonSilkUtil.N(b))) (a, b) = (b, a);
                c.String(a, b, EbonTone.Charcoal, bow, 0, body, alpha);
                c.String(a, b, core, bow, 0, 1, alpha * .85f);
                return;
            }
            c.Thread(a, b, EbonTone.Charcoal, body, alpha);
            c.Thread(a, b, core, 1, alpha * (.8f + .2f * tension), .1f + .9f * tension);
        }

        // The newer strand of every crossing pair draws the knot: a dot that stays, a star while it is fresh.
        private void Knots(EbonPixelCanvas c, Vector2 a, Vector2 b, float bow, float alpha, float age, float f)
        {
            int n = EbonSilkMath.Segments(Vector2.Distance(a, b));
            Span<Num> mine = stackalloc Num[n + 1];
            Span<Num> theirs = stackalloc Num[EbonSilkMath.Segments(EbonSilkMath.MaxStrand) + 1];
            EbonSilkMath.Polyline(EbonSilkUtil.N(a), EbonSilkUtil.N(b), bow, mine);
            foreach (Projectile q in Main.ActiveProjectiles)
            {
                if (q.whoAmI == P.whoAmI || q.owner != P.owner || q.type != P.type || q.ModProjectile is not EbonSilkStrand o
                    || o.State < 0 || o.Phase > EbonRewardRules.TightenTicks) continue;
                if (q.timeLeft > P.timeLeft || q.timeLeft == P.timeLeft && q.whoAmI > P.whoAmI) continue;
                Vector2 qa = q.Center, qb = o.End;
                if (!EbonSilkMath.Worthy(EbonSilkUtil.N(qa), EbonSilkUtil.N(qb))) continue;
                Span<Num> other = theirs[..(EbonSilkMath.Segments(Vector2.Distance(qa, qb)) + 1)];
                EbonSilkMath.Polyline(EbonSilkUtil.N(qa), EbonSilkUtil.N(qb), q.GetGlobalProjectile<SeveringSilkVisuals>().Bow(q, o, f), other);
                if (!EbonSilkMath.Knot(mine, other, out Num at)) continue;
                Vector2 knot = new(at.X, at.Y);
                c.Dot(knot, EbonTone.Ivory, 2, alpha);
                c.Dot(knot, EbonTone.Rose, 1, alpha);
                if (age < KnotGlint && !c.Reduced) c.Star(knot, 7 + 4 * (1 - age / KnotGlint), age / KnotGlint, Seed ^ q.identity);
            }
        }

        // t = ticks since the arming tick. 0..TightenTicks the silk straightens and brightens, then it parts:
        // a full-length flash over the two live ticks (the honest 24 px footprint), two recoiling halves, and
        // curls and a star at the cut that ripple out by rank with their notes.
        private void Armed(EbonPixelCanvas c, SeveringSilkVisuals g, EbonSilkStrand s, Vector2 a, Vector2 b, float t, float f)
        {
            if (t < EbonRewardRules.TightenTicks)
            {
                float tension = EbonRewardRules.Smooth(t / EbonRewardRules.TightenTicks);
                Silk(c, a, b, g.Bow(P, s, f), 1, tension);
                c.Dot(a, EbonTone.Moon, 2, .5f + .5f * tension);
                c.Dot(b, EbonTone.Moon, 2, .5f + .5f * tension);
                return;
            }
            float since = t - EbonRewardRules.TightenTicks;
            Vector2 cut = Vector2.Lerp(a, b, s.Cut);
            if (since < EbonSilkMath.LiveTicks)
            {
                float k = 1 - since / EbonSilkMath.LiveTicks;
                c.Thread(a, b, EbonTone.Moon, 3, k);
                c.Thread(a, b, EbonTone.Ivory, 1, k);
                if (!c.Reduced) c.Band(a, b, EbonRewardRules.StrandWidth, EbonBandMode.Tear, 1, .4f * k, Seed);
            }
            float r = EbonSilkMath.Retract(since);
            if (r < .985f)
            {
                Vector2 ea = Vector2.Lerp(cut, a, r), eb = Vector2.Lerp(cut, b, r);
                float amp = 12 * (1 - r), alpha = 1 - r * r;
                c.String(a, ea, EbonTone.Charcoal, amp, since * 1.7f, 2, alpha);
                c.String(a, ea, EbonTone.Silver, amp, since * 1.7f, 1, alpha * .8f);
                c.String(b, eb, EbonTone.Charcoal, -amp, since * 1.7f + 1.3f, 2, alpha);
                c.String(b, eb, EbonTone.Silver, -amp, since * 1.7f + 1.3f, 1, alpha * .8f);
            }
            float curl = since - EbonSilkMath.CascadeStep * s.Rank;
            if (curl >= 0 && curl < EbonSilkMath.CurlLife)
            {
                c.Burst(cut, Seed, c.Reduced ? 3 : 6, curl, EbonSilkMath.CurlLife, 2.6f, .02f, EbonShardKind.Silk);
                if (curl < 9) c.Star(cut, s.Rank == 0 ? 14 : 10, curl / 9, Seed);
                if (!c.Reduced && curl < 12) c.Ring(cut, 3 + curl * 1.8f, EbonTone.Ivory, 1, 1 - curl / 12);
            }
        }
    }

    // The stealth scissors: placeholder body (canvas lines) unless final art exists, plus every snip effect.
    private sealed class ScissorsSource : Source<EbonSilkScissors>
    {
        internal ScissorsSource(Projectile p, EbonSilkScissors model, int seed) : base(p, model, seed) { }

        public override bool Emit(EbonPixelCanvas c)
        {
            if (!Alive) return false;
            float f = c.Fraction;
            SeveringSilkVisuals g = Visuals;
            ScissorsPose pose = g.Pose(P, Model, f);
            Vector2 heading = pose.Angle.ToRotationVector2();
            if (Model.Phase == 0)
            {
                // Streak of the flight, longest at cruise speed. It trails from behind the finger loops of the
                // final art (the layer is composited over the sprite) or from the centre of the placeholder.
                float length = P.velocity.Length() * 1.7f, back = ScissorsArt ? ScissorsHinge.X * EbonRewardArt.PixelScale : 0;
                if (length > 4)
                {
                    c.Thread(pose.Center - heading * back, pose.Center - heading * (back + length), EbonTone.Ivory, 1, .55f);
                    c.Thread(pose.Center - heading * (back + length * .5f), pose.Center - heading * (back + length * 1.3f), EbonTone.Silver, 1, .3f);
                }
            }
            if (pose.Alpha > .01f && !ScissorsArt)
                Body(c, pose, heading);
            float t = pose.Time;
            if (t > 0 && t < EbonRewardRules.TightenTicks && !c.Reduced)
            {
                // The build-up: a ring draws in on the beak as the silk tightens.
                float k = t / EbonRewardRules.TightenTicks;
                c.Ring(pose.Center, 34 - 24 * EbonRewardRules.Smooth(k), EbonTone.Silver, 1, .35f + .4f * k);
            }
            float since = t - EbonRewardRules.TightenTicks;
            if (since >= 0)
            {
                // The release: the footprint ring reaches ScissorsRadius within the two live ticks.
                if (since < 9) c.Star(pose.Center, 20, since / 9, Seed);
                if (since < 14) c.Ring(pose.Center, EbonRewardRules.ScissorsRadius * EbonVisualsMath.OutExpo(Math.Min(since / 2.5f, 1)), EbonTone.Ivory, 1, 1 - since / 14);
                if (since < 20)
                {
                    c.Burst(pose.Center, Seed + 1, c.Reduced ? 4 : 8, since, 20, 3.6f, .05f, EbonShardKind.Silk);
                    if (!c.Reduced) c.Burst(pose.Center, Seed + 2, 6, since, 16, 4.4f, .08f, EbonShardKind.Spark);
                }
            }
            return true;
        }

        // Bird scissors: two long-billed blades crossing at a gold pivot, gold finger loops behind it.
        private static void Body(EbonPixelCanvas c, in ScissorsPose pose, Vector2 heading)
        {
            float half = pose.Gape * .3f, a = pose.Alpha;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 d = heading.RotatedBy(sign * half);
                c.Thread(pose.Center - d * 8, pose.Center + d * 22, EbonTone.Charcoal, 2, a);
                c.Thread(pose.Center - d * 6, pose.Center + d * 22, sign < 0 ? EbonTone.Ivory : EbonTone.Silver, 1, a * .9f);
                c.Ring(pose.Center - d * 12, 4, EbonTone.Gold, 1, a);
            }
            c.Dot(pose.Center, EbonTone.Gold, 2, a);
        }
    }

    // A short hit star where a strand or the snip met an enemy.
    private sealed class Pop : IEbonPixelSource
    {
        private readonly Vector2 at;
        private readonly int seed;
        private readonly float size;
        private readonly long born = EbonSilkUtil.Now;
        internal Pop(Vector2 at, int seed, float size) { this.at = at; this.seed = seed; this.size = size; }

        public bool Emit(EbonPixelCanvas c)
        {
            float age = EbonSilkUtil.Now - born + c.Fraction;
            if (age >= 16) return false;
            if (age < 10) c.Star(at, 7 * size, age / 10, seed);
            c.Burst(at, seed, c.Reduced ? 2 : 4, age, 16, 2.2f, .04f, EbonShardKind.Silk);
            return true;
        }
    }
}

// World and Mod teardown for the retained shake handles.
[Autoload(Side = ModSide.Client)]
internal sealed class SeveringSilkSystem : ModSystem
{
    public override void OnWorldUnload() => SeveringSilkVisuals.ClearShakes();
    public override void Unload() => SeveringSilkVisuals.ClearShakes();
}
