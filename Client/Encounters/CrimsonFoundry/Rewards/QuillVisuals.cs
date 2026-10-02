#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Num = System.Numerics.Vector2;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Bodies, cues and accents of the Bloodink Quill (REWARDS.md, "Rogue - Bloodink Quill", Presentation and Audio). Client
// only: it reads the carriers' replicated state on every client and never decides a hit. Each accent fires once from
// the named event that the carrier exposes (the throw, the stick, the unseal, the unroll, a burst), so sound, droplets,
// embers and shake stay together and never repeat on catch-up.
//   QuillThrow  each throw (the owner full, others -8 dB, one voice); the rolled score 2 dB softer
//   QuillStick  the nib enters flesh or stone; Toll(n - 1) for the n-th quill standing, owner only
//   ScoreUnseal the windup: crimson cracks over the seal, wax flakes as it splits, the score turns and unrolls
//   InkBlaze    the unroll: the claimed ink catches
//   Toll(k)     each quill's burst, by its height against the halted score (positional for everyone)
//   ScoreChord  the score's burst on the cadence voicing (2 dB softer without the Full Melody); with a Full Melody also
//               the owner's local shake 3.5
// Bodies use QuillArt (SR06 or the vanilla placeholder) and draw in the projectile layer, above the ink.
[Autoload(Side = ModSide.Client)]
internal sealed class QuillVisuals : GlobalProjectile
{
    private const int SealSplit = 4;
    private bool seen, flew, stuck, burst, windup, split, unrolled, scoreBurst;
    private long stuckAt = -1;
    private float shiverSign = 1;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is BloodinkQuill or BloodinkTrail or SealedScore;

    private static long Now => QuillCarriers.Now;
    private static Vector2 X(Num v) => new(v.X, v.Y);
    private static Num N(Vector2 v) => new(v.X, v.Y);
    // Ticks since this client first saw the quill stand (or predicted it), -1 before.
    internal float StuckFor => stuckAt < 0 ? -1 : Now - stuckAt;

    // ---- Events ---------------------------------------------------------------------------------------------------

    public override void PostAI(Projectile p)
    {
        if (Main.dedServ) return;
        switch (p.ModProjectile)
        {
            case BloodinkQuill q: Quill(p, q); break;
            case BloodinkTrail t: Burning(p, t); break;
            case SealedScore s: Score(p, s); break;
        }
    }

    private void Quill(Projectile p, BloodinkQuill q)
    {
        bool standing = !q.Flying || q.Held;
        if (!seen)
        {
            seen = true;
            flew = !standing;
            shiverSign = ScarletRewardParticles.Hash(QuillInk.Seed(p.owner, q.Serial), 5) > .5f ? 1 : -1;
            if (flew && q.Age <= 2) ScarletRewardAudio.Shot(ScarletRewardCues.QuillThrow, p.owner, p.Center);
        }
        if (standing && !stuck)
        {
            stuck = true;
            stuckAt = Now;
            if (flew) Stick(p, q);
        }
        if (q.BurstBegun && !burst)
        {
            burst = true;
            Burst(p, q);
        }
    }

    private static void Stick(Projectile p, BloodinkQuill q)
    {
        int owner = p.owner;
        Vector2 nib = p.Center, back = -p.velocity.SafeNormalize(Vector2.UnitX);
        float seed = QuillInk.Seed(owner, q.Serial);
        ScarletRewardAudio.Shot(ScarletRewardCues.QuillStick, owner, nib);
        if (owner == Main.myPlayer && q.Stuck != QuillRules.Flying)
        {
            // The n-th quill standing rings Toll(n - 1); the owner's trail for this quill already stands.
            int n = 0;
            foreach (Projectile other in Main.ActiveProjectiles)
                if (other.owner == owner && other.ModProjectile is BloodinkTrail { Standing: true }) n++;
            ScarletRewardAudio.BuildToll(Math.Clamp(n, 1, R.MaxQuills) - 1, owner, nib);
        }
        if (q.Stuck > 0)
            QuillInk.AddSplash(owner, nib, back.ToRotation(), 1.1f, 2.4f, ScarletRewardFx.Reduced ? 2 : 4, 2.2f, false, seed);
        else
            for (int i = 0; i < 2; i++)
                ScarletRewardFx.Particle(ScarletParticleKind.Smoke, owner, nib + back * 4,
                    back.RotatedBy((ScarletRewardParticles.Hash(seed, i) - .5f) * 1.2f) * .8f, 22, 7, seed + i);
    }

    private static void Burst(Projectile p, BloodinkQuill q)
    {
        int owner = p.owner;
        Vector2 nib = p.Center;
        float seed = QuillInk.Seed(owner, q.Serial);
        // The melody plays back: the higher the quill against the halted score, the higher the toll.
        ScarletRewardAudio.Toll(QuillRules.TollForHeight(nib.Y, q.ScoreAt.Y), nib);
        QuillInk.AddSplash(owner, nib + new Vector2(0, -6), -MathHelper.PiOver2, MathHelper.TwoPi * .8f, 3.6f, ScarletRewardFx.Reduced ? 4 : 8, 2.6f, true, seed);
        for (int i = 0; i < 6; i++)
        {
            float h = ScarletRewardParticles.Hash(seed, i + 40), a = h * MathHelper.TwoPi;
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, owner, nib + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 20,
                new Vector2(MathF.Cos(a) * 1.4f, -1.2f - h), 20 + 10 * h, 5, seed + i);
        }
        ScarletRewardFx.Particle(ScarletParticleKind.Smoke, owner, nib, new Vector2(0, -.7f), 30, 16, seed + 9);
        Lighting.AddLight(nib, .9f, .12f, .08f);
    }

    // Embers lift off ink while it burns, smoke from where it has just dried; light along the blaze.
    private static void Burning(Projectile p, BloodinkTrail t)
    {
        if (!t.Unrolled) return;
        float tau = t.Tau;
        if (tau < 0 || tau >= QuillRules.StrokeDone(t.Length)) return;
        var s = t.Samples;
        if (s.Length == 0) return;
        int owner = p.owner, tick = (int)(Now & 0xFFFFFF);
        float seed = QuillInk.Seed(owner, t.Serial);
        const int embers = 2; // Reduced Effects halves particles where they are spawned (ScarletRewardParticles)
        for (int k = 0; k < 3; k++)
        {
            int i = (int)(ScarletRewardParticles.Hash(seed, tick * 5 + k) * s.Length) % s.Length;
            float age = QuillRules.BurnAge(tau, s[i].FromNib);
            Vector2 at = X(s[i].At);
            if (QuillRules.Burning(age))
            {
                if (k < embers)
                {
                    float h = ScarletRewardParticles.Hash(seed, tick * 7 + k);
                    ScarletRewardFx.Particle(ScarletParticleKind.Ember, owner, at + new Vector2((h - .5f) * 20, -6),
                        new Vector2((h - .5f) * 1.2f, -.8f - .8f * h), 16 + 12 * h, 4 + 2 * h, seed + tick + k);
                }
                if (k == 0) Lighting.AddLight(at, .7f, .08f, .05f);
            }
            else if (k == 2 && age >= R.BurnLive && age < R.BurnLive + 6 && ScarletRewardParticles.Hash(seed, tick * 11) > .55f)
                ScarletRewardFx.Particle(ScarletParticleKind.Smoke, owner, at, new Vector2(0, -.5f), 28, 11, seed + tick * .5f);
        }
    }

    private void Score(Projectile p, SealedScore s)
    {
        int owner = p.owner;
        Vector2 at = p.Center;
        float seed = QuillInk.Seed(owner, 700 + Math.Max(0, s.Cast));
        if (!seen)
        {
            seen = true;
            if (s.Age <= 2) ScarletRewardAudio.Shot(ScarletRewardCues.QuillThrow, owner, at, ScarletRewardCues.ScoreThrowDecibels);
        }
        if (!windup && s.Age > s.Flight)
        {
            windup = true;
            ScarletRewardAudio.Play(ScarletRewardCues.ScoreUnseal, at);
        }
        if (!split && s.Age > s.Flight + SealSplit)
        {
            // The seal splits: its halves fall away as wax flakes in the seal's red.
            split = true;
            var score = QuillArt.Score();
            Vector2 seal = score.Point(at, 0, QuillArt.Seal, Vector2.One);
            for (int i = 0; i < 8; i++)
            {
                float h = ScarletRewardParticles.Hash(seed, i + 60);
                ScarletRewardFx.Particle(ScarletParticleKind.WaxFlake, owner, seal + new Vector2((i % 2 == 0 ? -1 : 1) * 3, 0),
                    new Vector2((i % 2 == 0 ? -1 : 1) * (.6f + 1.4f * h), -1.6f - 1.6f * h), 26, CrimsonRewardSprites.PixelScale, seed + i);
            }
        }
        if (!unrolled && s.Unrolled)
        {
            unrolled = true;
            ScarletRewardAudio.Play(ScarletRewardCues.InkBlaze, at);
        }
        if (!scoreBurst && s.BurstSince >= 0)
        {
            scoreBurst = true;
            bool full = QuillRules.FullMelody(s.Quills);
            ScarletRewardAudio.Play(ScarletRewardCues.ScoreChord, at, full ? 0 : ScarletRewardCues.PartialScoreDecibels);
            if (full) ScarletRewardFx.Shake(owner, at, R.MelodyShake);
            int droplets = (full ? 14 : 10) / (ScarletRewardFx.Reduced ? 2 : 1);
            QuillInk.AddSplash(owner, at, -MathHelper.PiOver2, MathHelper.TwoPi * .9f, full ? 5.4f : 4.4f, droplets, 3f, true, seed);
            for (int i = 0; i < (full ? 18 : 12); i++)
            {
                float h = ScarletRewardParticles.Hash(seed, i + 80), a = h * MathHelper.TwoPi, reach = QuillRules.ScoreBurstRadius(s.Quills) * (.3f + .6f * h);
                ScarletRewardFx.Particle(ScarletParticleKind.Ember, owner, at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * reach,
                    new Vector2(MathF.Cos(a) * 1.2f, -1 - 1.4f * h), 22 + 12 * h, 6, seed + i);
            }
            for (int i = 0; i < 4; i++)
                ScarletRewardFx.Particle(ScarletParticleKind.Smoke, owner, at + new Vector2((i - 1.5f) * 30, 0), new Vector2(0, -.6f), 36, 22, seed + 30 + i);
            Lighting.AddLight(at, 1.2f, .16f, .1f);
        }
    }

    // A quill pulled out (a ninth stood), fallen away (timeout) or cleared (death), or a score that never released,
    // tumbles down; its ink fades. Natural expiry has already faded, so it leaves nothing.
    public override void OnKill(Projectile p, int timeLeft)
    {
        if (Main.dedServ || timeLeft <= 1) return;
        int owner = p.owner;
        switch (p.ModProjectile)
        {
            case BloodinkQuill q when !q.BurstBegun:
            {
                bool flying = q.Flying && !q.Held;
                Vector2 dir = p.velocity.SafeNormalize(Vector2.UnitX);
                float opacity = flying || q.Claimed ? 1 : QuillRules.InkFade(timeLeft);
                Vector2 velocity = flying ? p.velocity * .55f : -dir * 1.4f + new Vector2(0, -2.2f);
                QuillInk.AddBodyGhost(owner, p.Center, velocity, p.velocity.ToRotation(), (dir.X >= 0 ? 1 : -1) * .13f, opacity, false);
                if (flying && q.Started) QuillInk.AddInkGhost(owner, q.Origin, q.Launch, q.Age, 1, QuillInk.Seed(owner, q.Serial), false);
                break;
            }
            case BloodinkTrail t when !t.Unrolled:
                QuillInk.AddInkGhost(owner, p.Center, p.velocity, t.Stop, t.Standing ? QuillRules.InkFade(timeLeft) : 1, QuillInk.Seed(owner, t.Serial), t.Blot);
                break;
            case SealedScore s when s.BurstSince < 0 && !s.Unrolled:
                QuillInk.AddBodyGhost(owner, p.Center, p.velocity * .4f + new Vector2(0, -1.5f), p.rotation, .1f, 1, true);
                break;
        }
    }

    // ---- Bodies ---------------------------------------------------------------------------------------------------

    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        switch (p.ModProjectile)
        {
            case BloodinkQuill q: DrawQuill(p, q, lightColor); break;
            case SealedScore s: DrawScore(p, s, lightColor); break;
        }
        return false;
    }

    private void DrawQuill(Projectile p, BloodinkQuill q, Color light)
    {
        float f = ScarletRewardFx.Fraction;
        if (q.BurstBegun && q.BurstSince - 1 + f >= 0) return; // spent into its burst
        Vector2 at = p.Center;
        float rotation = p.velocity.ToRotation();
        if (q.Flying && !q.Held && q.Started)
        {
            // The same QuillFlight the ink is written from, at the draw time.
            float t = Math.Max(0, q.Age - 1 + f);
            at = X(QuillRules.At(N(q.Origin), N(q.Launch), t));
            rotation = X(QuillRules.VelocityAt(N(q.Launch), Math.Max(t, 1e-3f))).ToRotation();
        }
        else if (stuckAt >= 0)
        {
            // The impact shivers out of the shaft like a damped spring.
            float since = Now - stuckAt - 1 + f;
            if (since >= 0 && since < 16) rotation += shiverSign * .15f * MathF.Exp(-since / 2.6f) * MathF.Cos(since * 2.3f);
        }
        float opacity = !q.Flying && !q.Claimed ? QuillRules.InkFade(p.timeLeft) : 1;
        Color color = Color.Lerp(light, Color.White, .45f) * opacity;
        if (q.Claimed && q.Unrolled)
        {
            // The nib heats over the sixteenth before its burst.
            float heat = R.Smooth((q.BurstSince - 1 + f + R.S(1)) / R.S(1));
            color = Color.Lerp(color, new Color(255, 128, 96), .6f * heat);
        }
        var art = QuillArt.Quill();
        var saved = WorldBatchParameters.Capture(Main.spriteBatch);
        bool restarted = false;
        try
        {
            restarted = ScarletRewardFx.EnsureBatch(art.Pixel ? art.Sampler : null, saved);
            Draw(Main.spriteBatch, art, at - Main.screenPosition, rotation, color, Vector2.One);
        }
        finally { ScarletRewardFx.RestoreBatch(restarted, saved); }
    }

    private static void DrawScore(Projectile p, SealedScore s, Color light)
    {
        float f = ScarletRewardFx.Fraction;
        if (s.BurstSince - 1 + f >= 0) return; // consumed by its burst
        Vector2 at = p.Center - p.velocity * (1 - f);
        float age = Math.Max(0, s.Age - 1 + f), windup = age - s.Flight;
        float spin = s.Heading.X >= 0 ? 1 : -1;
        // Tumbling end over end along its travel, the tumble slowing with the glide; the windup turns it level without
        // a stop: the remaining offset to level is blended out from zero rate.
        float tumble = s.Heading.ToRotation() + spin * .3f * Travelled(age, s.Flight);
        float settled = s.Heading.ToRotation() + spin * .3f * Travelled(s.Flight + QuillRules.ScoreGlide, s.Flight);
        float rotation = tumble - MathF.IEEERemainder(settled, MathHelper.TwoPi) * R.Smooth(windup / 6);
        float open = R.Smooth((windup - 3) / 5);
        Vector2 stretch = new(1 + .45f * open, 1 - .15f * open);
        var art = QuillArt.Score();
        Color color = Color.Lerp(light, Color.White, .5f);
        var batch = Main.spriteBatch;
        // Crimson cracks run over the wax seal through the first ticks of the windup, until it splits.
        float seed = QuillInk.Seed(p.owner, 700 + Math.Max(0, s.Cast));
        float swell = R.Smooth((s.BurstSince - 1 + f + R.S(1)) / R.S(1));
        bool cracks = windup >= 0 && windup < SealSplit;
        // One AlphaBlend batch for the body and its additive accents (alpha-0 colours); restarted only for a sampler the
        // layer's batch lacks (delivered pixel art, then linear for the soft glow).
        var saved = WorldBatchParameters.Capture(batch);
        bool restarted = false;
        try
        {
            restarted |= ScarletRewardFx.EnsureBatch(art.Pixel ? art.Sampler : null, saved);
            Draw(batch, art, at - Main.screenPosition, rotation, color, stretch);
            if (cracks) Cracks(batch, art.Point(at, rotation, QuillArt.Seal, stretch) - Main.screenPosition, R.Smooth((windup + .5f) / SealSplit), seed);
            // A faint ember glow welling in the open score through the sixteenth before it bursts.
            if (swell > .01f)
            {
                restarted |= ScarletRewardFx.EnsureBatch(SamplerState.LinearClamp, saved);
                Texture2D soft = ScarletVfxHost.Assets.GetTexture("Luminance/BloomCircleSmall");
                batch.Draw(soft, at - Main.screenPosition, null, ScarletRewardFx.Additive(new Color(1f, .16f, .08f) * (.55f * swell)), 0, soft.Size() * .5f,
                    70f / soft.Width * (1 + .3f * swell), SpriteEffects.None, 0);
            }
        }
        finally { ScarletRewardFx.RestoreBatch(restarted, saved); }
    }

    // Ticks of travel at cruise speed covered by drawn age `age` (the glide counts for less).
    private static float Travelled(float age, int flight)
    {
        float distance = 0;
        int whole = (int)MathF.Floor(age);
        for (int k = 0; k < whole && k < flight + QuillRules.ScoreGlide; k++) distance += QuillRules.ScoreSpeedAt(k, flight);
        distance += QuillRules.ScoreSpeedAt(whole, flight) * (age - whole);
        return distance / R.ScoreSpeed;
    }

    private static void Cracks(SpriteBatch batch, Vector2 seal, float run, float seed)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        for (int k = 0; k < 3; k++)
        {
            float angle = k * MathHelper.TwoPi / 3 + ScarletRewardParticles.Hash(seed, k + 71) * 1.2f;
            Vector2 from = seal;
            for (int seg = 0; seg < 3; seg++)
            {
                float bend = angle + (ScarletRewardParticles.Hash(seed, k * 7 + seg + 81) - .5f) * .9f;
                Vector2 to = from + new Vector2(MathF.Cos(bend), MathF.Sin(bend)) * 3.4f * run;
                Vector2 d = to - from;
                batch.Draw(pixel, from, new Rectangle(0, 0, 1, 1), ScarletRewardFx.Additive(new Color(1f, .22f, .1f) * .95f), MathF.Atan2(d.Y, d.X), new Vector2(0, .5f),
                    new Vector2(d.Length(), 1.4f), SpriteEffects.None, 0);
                from = to;
            }
        }
    }

    // Draws one body into a begun batch. Final pixel art mirrors vertically when it points left, so it stays right way
    // up (the anchor mirrors with it); the diagonal placeholder is drawn as it is.
    internal static void Draw(SpriteBatch batch, in QuillArt.Body art, Vector2 position, float rotation, Color color, Vector2 stretch)
    {
        Vector2 origin = art.Origin;
        var effects = SpriteEffects.None;
        if (art.Pixel && MathF.Cos(rotation) < 0)
        {
            effects = SpriteEffects.FlipVertically;
            origin.Y = art.Source.Height - origin.Y;
        }
        batch.Draw(art.Texture, position, art.Source, color, rotation + art.Turn, origin, art.Scale * stretch, effects, 0);
    }

}
