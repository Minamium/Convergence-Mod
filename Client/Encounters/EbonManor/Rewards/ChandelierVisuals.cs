#nullable enable
using System;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Presentation of the Ballroom Chandelier (REWARDS.md "Summon"). The minion's replicated state is the only
// input; nothing here decides a hit, a packet or a world change.
//  - The unlit body (EbonRewardArt.Chandelier) is drawn in the projectile layer, hanging from its ring and
//    swaying; on the shatter the sprite splits into a 4x4 grid of its own pieces that burst out and fly back
//    together while the chandelier is reeled up.
//  - The thread, candle flames and shatter debris are pixel-layer primitives from one retained source per
//    chandelier. The thread is drawn from the ring up past the top of the screen, tenses before the beat,
//    is snipped with a short gap animation, and a fresh line is lowered to hook the ring for the reel.
//  - Cues fire from the same event as their visual peak: ChandelierSnip at the cut, ChandelierShatter at the
//    impact (with one pluck of the arpeggio), ChandelierReel when the pieces turn back.
// Time is the per-tick sample count plus WeaponDrawClock.Fraction; motion is interpolated between the two
// latest accepted positions. Reduced Effects drops the streak, the footprint ring, sparks and shake.
[Autoload(Side = ModSide.Client)]
internal sealed class EbonChandelierVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private ChandelierLook? look;
    private static bool failed;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is EbonChandelierMinion;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not EbonChandelierMinion minion) return;
        look ??= new ChandelierLook(projectile);
        look.Observe(projectile, minion);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (failed || look is null || projectile.ModProjectile is not EbonChandelierMinion minion) return false;
        try
        {
            ChandelierPose pose = look.Pose(projectile, minion, WeaponDrawClock.Fraction);
            ChandelierArt.Body(Main.spriteBatch, pose, projectile.identity, lightColor);
        }
        catch (Exception exception)
        {
            failed = true;
            ConvergenceMod.Instance.Logger.Warn($"Ballroom Chandelier body drawing disabled for this session: {exception}");
        }
        return false;
    }
}

// Where and how one chandelier is drawn this frame; the body pass and the pixel source share it.
internal readonly record struct ChandelierPose(Vector2 Center, Vector2 Ring, float Angle, float Clock, float Anticipation,
    bool Small, EbonRewardArt.Sprite Sprite, ChandelierState State, float SinceSnip, float SinceImpact);

// Per-chandelier presentation state: the previous accepted sample, the lagging thread anchor, and the clocks
// of the snip and the impact. Cues are keyed by the minion's drop serial so each fires once per drop even
// when a correction packet rewinds the replicated age.
internal sealed class ChandelierLook
{
    private const byte Snipped = 1, Shattered = 2, Reeled = 4;
    private static ulong lastShake;

    internal readonly int Slot, Owner, Identity;
    private Vector2 seen, snipRing, impact;
    private float anchorX, snipAngle, spin, landAngle, lastSwing;
    private ulong snipTick, impactTick;
    private bool started, registered, hasSnip, hasImpact;
    private int serial;
    private byte cues;

    internal ChandelierLook(Projectile projectile)
    {
        Slot = projectile.whoAmI; Owner = projectile.owner; Identity = projectile.identity;
    }

    // --- Per-tick observation (PostAI, every client) ---------------------------------------------
    internal void Observe(Projectile p, EbonChandelierMinion m)
    {
        if (!started)
        {
            started = true; seen = p.Center; anchorX = p.Center.X; serial = m.Serial;
            // A replica first seen mid-drop must not replay what already happened.
            if (m.State >= ChandelierState.Fall) cues |= Snipped;
            if (m.State >= ChandelierState.Reweave) cues |= Shattered | Reeled;
            // Summoning builds the row: each new chandelier plucks the next note of the arpeggio.
            if (p.owner == Main.myPlayer && m.State == ChandelierState.Idle && m.Age <= 2)
                EbonRewardAudio.Note(Math.Max(m.Count, 1) - 1, p.Center, .38f);
        }
        if (!registered) registered = EbonPixelLayer.Add(new ChandelierSource(this));
        if (serial != m.Serial) { serial = m.Serial; cues = 0; }

        ChandelierState state = m.State;
        if (state == ChandelierState.Fall && (cues & Snipped) == 0) Snip(p, m);
        else if (state == ChandelierState.Reweave)
        {
            cues |= Snipped;
            if ((cues & Shattered) == 0) Shatter(p, m);
            if ((cues & Reeled) == 0 && hasImpact && Main.GameUpdateCount - impactTick >= EbonChandelierRules.ReelCueTicks)
            {
                cues |= Reeled;
                EbonRewardAudio.Play("ChandelierReel", impact + new Vector2(0, -60), .55f, Detune(m), .03f, 3);
            }
        }

        float sinceSnip = hasSnip ? (float)(Main.GameUpdateCount - snipTick) : 999f;
        float sinceImpact = hasImpact ? (float)(Main.GameUpdateCount - impactTick) : 999f;
        float lit = LitAmount(state, sinceSnip, sinceImpact, 1, 3);
        if (lit > .02f) Lighting.AddLight(p.Center + new Vector2(0, -8), .62f * lit, .42f * lit, .22f * lit);

        // The thread's top end trails the body, so a gliding chandelier drags a slanted line behind it.
        anchorX += (p.Center.X - anchorX) * .07f;
        seen = p.Center;
        if (state is not (ChandelierState.Fall or ChandelierState.Reweave)) lastSwing = Swing(p, (float)Main.GameUpdateCount);
    }

    private void Snip(Projectile p, EbonChandelierMinion m)
    {
        cues |= Snipped; hasSnip = true; snipTick = Main.GameUpdateCount;
        snipAngle = lastSwing; // continue from the pose it was hanging in
        spin = (ChandelierArt.Hash(Identity, m.Serial) - .5f) * .016f;
        snipRing = p.Center + new Vector2(0, EbonChandelierRules.BodyHalfHeight - EbonRewardArt.Chandelier(m.Ordinal % 2 == 1).Size.Y);
        EbonRewardAudio.Play("ChandelierSnip", snipRing, .62f, Detune(m), .03f, 4);
    }

    private void Shatter(Projectile p, EbonChandelierMinion m)
    {
        cues |= Shattered; hasImpact = true; impactTick = Main.GameUpdateCount;
        impact = m.Impact == Vector2.Zero ? p.Center + new Vector2(0, EbonChandelierRules.BodyHalfHeight) : m.Impact;
        landAngle = hasSnip ? Math.Clamp(snipAngle + spin * (float)(impactTick - snipTick), -.35f, .35f) : 0f;
        EbonRewardAudio.Play("ChandelierShatter", impact, .82f, Detune(m), .03f, 4);
        // Each chandelier of the cascade rings its own note of the B-minor arpeggio.
        EbonRewardAudio.Note(m.Ordinal % 4 * 2 + m.Ordinal / 4 % 2, impact, .34f);
        if (p.owner == Main.myPlayer) Shake(m.Ordinal % 2 == 1);
    }

    // Local feedback only: nothing to cancel, and the config can switch it off.
    private void Shake(bool small)
    {
        var config = ModContent.GetInstance<EbonVisualConfig>();
        if (config.ReducedEffects || !config.ScreenShake || Main.GameUpdateCount - lastShake < 6) return;
        lastShake = Main.GameUpdateCount;
        ScreenShakeSystem.StartShakeAtPoint(impact, small ? 1.6f : 2.2f, shakeDirection: Vector2.UnitY, angularVariance: .4f,
            shakeStrengthDissipationIncrement: .6f);
    }

    // Slightly different pitch down the cascade so four drops do not machine-gun one sample.
    private static float Detune(EbonChandelierMinion m) => (m.Ordinal % 4 - 1.5f) * .04f;

    private static float Swing(Projectile p, float clock)
        => EbonChandelierRules.Sway(clock, p.identity * 1.7f) + Math.Clamp(p.velocity.X * .012f, -.2f, .2f);

    private static float LitAmount(ChandelierState state, float sinceSnip, float sinceImpact, int candle, int count) => state switch
    {
        ChandelierState.Fall => 1f - EbonRewardRules.Smooth(sinceSnip / 3f), // the cut blows the candles out
        ChandelierState.Reweave => EbonChandelierRules.Relit(sinceImpact, candle, count),
        _ => 1f,
    };

    // --- Frame pose -----------------------------------------------------------------------------
    internal ChandelierPose Pose(Projectile p, EbonChandelierMinion m, float fraction)
    {
        ulong tick = Main.GameUpdateCount;
        Vector2 center = Vector2.DistanceSquared(seen, p.Center) < 400f * 400f ? Vector2.Lerp(seen, p.Center, fraction) : p.Center;
        float clock = (float)tick + fraction;
        bool small = m.Ordinal % 2 == 1;
        EbonRewardArt.Sprite sprite = EbonRewardArt.Chandelier(small);
        ChandelierState state = m.State;
        float sinceSnip = hasSnip ? (float)(tick - snipTick) + fraction : 999f;
        float sinceImpact = hasImpact ? (float)(tick - impactTick) + fraction : 999f;
        float anticipation = state == ChandelierState.Settle ? EbonChandelierRules.Anticipation(m.Wait) : 0f;
        float angle = state switch
        {
            ChandelierState.Fall => hasSnip ? Math.Clamp(snipAngle + spin * sinceSnip, -.35f, .35f) : Swing(p, clock),
            ChandelierState.Reweave => hasImpact ? landAngle * (1f - EbonRewardRules.Smooth(sinceImpact / EbonRewardRules.ReweaveTicks)) : 0f,
            _ => Swing(p, clock),
        };
        // The chandelier hangs from its ring: the projectile centre is the body, the ring is the sprite's top.
        Vector2 ring = center + new Vector2(0, EbonChandelierRules.BodyHalfHeight - sprite.Size.Y);
        if (state is not (ChandelierState.Fall or ChandelierState.Reweave))
        {
            float phase = Identity * 1.7f;
            ring += new Vector2(MathF.Sin(clock * .05f + phase) * 2f, MathF.Sin(clock * .07f + phase * 1.3f) * 2.5f - anticipation * 5f);
        }
        return new ChandelierPose(center, ring, angle, clock, anticipation, small, sprite, state, sinceSnip, sinceImpact);
    }

    // --- Pixel layer ----------------------------------------------------------------------------
    internal void Emit(EbonPixelCanvas c, Projectile p, EbonChandelierMinion m)
    {
        ChandelierPose pose = Pose(p, m, c.Fraction);
        Vector2 s = pose.Ring - Main.screenPosition;
        if (s.X < -420 || s.X > Main.screenWidth + 420 || s.Y < -200 || s.Y > Main.screenHeight + 420) return;
        int seed = Identity * 977 + Owner * 131 + m.Serial * 7;
        DrawThread(c, pose);
        Flames(c, pose, seed);
        Debris(c, pose, seed);
    }

    private static float VisibleTop()
    {
        float zoom = Math.Max(.25f, Main.GameViewMatrix.Zoom.Y);
        return Main.screenPosition.Y + Main.screenHeight * .5f * (1f - 1f / zoom);
    }

    private void DrawThread(EbonPixelCanvas c, in ChandelierPose pose)
    {
        float topY = VisibleTop() - 40f;
        Vector2 top = new(pose.Ring.X + Math.Clamp(anchorX - pose.Center.X, -120f, 120f), topY);
        switch (pose.State)
        {
            case ChandelierState.Fall:
                DrawSnip(c, pose, top);
                break;
            case ChandelierState.Reweave:
                // A fresh line is lowered to hook the ring (first 30%), then pulls taut with travelling highlights.
                if (pose.Ring.Y <= topY + 4f) break;
                float hook = EbonRewardRules.Smooth(pose.SinceImpact / (EbonRewardRules.ReweaveTicks * .3f));
                Hanging(c, top, Vector2.Lerp(top, pose.Ring, hook), .55f + .3f * hook, hook < 1f ? 0f : .8f);
                break;
            default:
                if (pose.Ring.Y <= topY + 4f) break;
                // Over the last ticks before the beat the line tenses and trembles.
                if (pose.Anticipation > .02f)
                    c.String(top, pose.Ring, EbonTone.Silver, 3.2f * pose.Anticipation, pose.Clock * .8f);
                else
                    Hanging(c, top, pose.Ring, .85f, .2f);
                break;
        }
    }

    // A thread bright near the body and faint toward the ceiling, so a row of chandeliers is not a curtain.
    private static void Hanging(EbonPixelCanvas c, Vector2 top, Vector2 end, float alpha, float shimmer)
    {
        Vector2 up = top - end;
        float length = up.Length();
        if (length < 1f) return;
        Vector2 join = end + up / length * Math.Min(length, 220f);
        c.Thread(join, end, EbonTone.Silver, 1, alpha, shimmer);
        if (length > 220f) c.Thread(top, join, EbonTone.Silver, 1, alpha * .4f);
    }

    // The cut: a glint, the upper line whipping up and fading, and a short stub fluttering on the ring.
    private void DrawSnip(EbonPixelCanvas c, in ChandelierPose pose, Vector2 top)
    {
        float t = pose.SinceSnip;
        if (!hasSnip || t > EbonChandelierRules.SnipTicks) return;
        Vector2 cut = snipRing + new Vector2(0, -14);
        float fade = 1f - EbonRewardRules.Smooth(t / 14f);
        Vector2 upper = cut - new Vector2(0, EbonChandelierRules.Retract(t));
        if (fade > .01f && upper.Y > top.Y + 2f) c.Thread(new Vector2(top.X, top.Y), upper, EbonTone.Silver, 1, fade);
        float stubFade = 1f - EbonRewardRules.Smooth((t - 10f) / 12f);
        if (stubFade > .01f)
        {
            Vector2 end = pose.Ring + new Vector2(MathF.Sin(t * .6f) * 3f * (1f - t / EbonChandelierRules.SnipTicks), -14f * (1f - .5f * EbonRewardRules.Smooth(t / 10f)));
            c.Thread(pose.Ring, end, EbonTone.Silver, 1, stubFade);
        }
        if (t < 7f) c.Star(cut, 8, t / 7f, Identity * 31 + serial);
    }

    private void Flames(EbonPixelCanvas c, in ChandelierPose pose, int seed)
    {
        Vector2[] candles = ChandelierArt.Candles(pose.Sprite.Pixel, pose.Small);
        float u = pose.State == ChandelierState.Reweave ? pose.SinceImpact / EbonRewardRules.ReweaveTicks : 1f;
        for (int k = 0; k < candles.Length; k++)
        {
            float lit = LitAmount(pose.State, pose.SinceSnip, pose.SinceImpact, k, candles.Length);
            if (lit <= .02f) continue;
            Vector2 local = new(candles[k].X * pose.Sprite.Size.X, candles[k].Y * pose.Sprite.Size.Y);
            // While the pieces are scattered a candle rides on its own piece.
            if (u < 1f) local += ChandelierArt.CandleOffset(pose.Sprite, candles[k], Identity, u);
            Vector2 wick = pose.Ring + local.RotatedBy(pose.Angle);
            float flicker = MathF.Sin(pose.Clock * .9f + k * 2.3f + Identity) * MathF.Sin(pose.Clock * .37f + k * 1.1f);
            float a = lit * (.8f + .2f * flicker), boost = pose.Anticipation;
            if (!c.Reduced) c.Dot(wick + new Vector2(0, -4), EbonTone.Candle, 2, .3f * a);
            c.Dot(wick + new Vector2(0, -2), EbonTone.Ivory, 1, a);
            c.Dot(wick + new Vector2(0, -4), EbonTone.Candle, 1, a);
            c.Dot(wick + new Vector2(flicker > .3f ? 2 : flicker < -.3f ? -2 : 0, -6), EbonTone.Candle, 1, .85f * a);
            if (boost > .35f) c.Dot(wick + new Vector2(0, -8), EbonTone.Gold, 1, boost * a);
            // A candle catching during the reweave throws a few sparks.
            float since = pose.SinceImpact - EbonChandelierRules.RelightStart(k, candles.Length) - EbonChandelierRules.CatchTicks;
            if (!c.Reduced && pose.State == ChandelierState.Reweave && since >= 0f && since < 8f)
                c.Burst(wick + new Vector2(0, -4), seed + k * 17, 3, since, 8, 1.3f, .02f, EbonShardKind.Spark, MathF.PI, -MathF.PI / 2);
        }
    }

    // Shatter debris at the impact, the honest damage footprint, and a streak behind the falling body.
    private void Debris(EbonPixelCanvas c, in ChandelierPose pose, int seed)
    {
        if (pose.State == ChandelierState.Fall && hasSnip && !c.Reduced)
        {
            float length = EbonChandelierRules.FallSpeed((int)pose.SinceSnip) * 1.6f, half = pose.Sprite.Size.X * .3f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = pose.Ring.X + side * half;
                c.Thread(new Vector2(x, pose.Ring.Y - length), new Vector2(x, pose.Ring.Y + 6f), EbonTone.Silver, 1, .35f);
            }
        }
        if (!hasImpact || pose.SinceImpact >= EbonChandelierRules.ShatterTicks) return;
        float s = pose.SinceImpact;
        bool reduced = c.Reduced;
        c.Burst(impact, seed, pose.Small ? (reduced ? 6 : 11) : (reduced ? 8 : 16), s, 28, 5.4f, .30f, EbonShardKind.Glass, MathF.PI * 1.15f, -MathF.PI / 2);
        c.Burst(impact, seed + 1, reduced ? 5 : 11, s, 18, 7.2f, .14f, EbonShardKind.Spark, MathF.PI * 1.3f, -MathF.PI / 2);
        if (s < 8f) c.Star(impact + new Vector2(0, -8), pose.Small ? 34 : 42, s / 8f, seed + 2);
        if (!reduced && s < 9f)
            c.Ring(impact, EbonRewardRules.ShatterRadius * EbonRewardRules.Smooth(s / 9f), EbonTone.Moon, 1, 1f - s / 9f);
    }
}

// One retained pixel-layer source per chandelier; it ends when the projectile does.
internal sealed class ChandelierSource : IEbonPixelSource
{
    private readonly ChandelierLook look;
    internal ChandelierSource(ChandelierLook look) => this.look = look;

    public bool Emit(EbonPixelCanvas canvas)
    {
        if (look.Slot < 0 || look.Slot >= Main.maxProjectiles) return false;
        Projectile p = Main.projectile[look.Slot];
        if (!p.active || p.identity != look.Identity || p.owner != look.Owner || p.ModProjectile is not EbonChandelierMinion minion)
            return false;
        look.Emit(canvas, p, minion);
        return true;
    }
}

// Sprite geometry shared by the body pass and the flames: candle anchors and the 4x4 shatter grid.
internal static class ChandelierArt
{
    private const int Cols = 4, Rows = 4, Cells = Cols * Rows;

    // Wick tops as fractions of the sprite: x from -.5 (left) to .5, y from 0 (ring) to 1 (bottom). The final
    // ER07 art is two tiers with four candles (one tier and three for the small one); the painted Raid
    // chandelier is taller with more tiers. Tune the first two tables to the delivered sprites.
    private static readonly Vector2[] finalLarge = { new(-.36f, .34f), new(-.14f, .26f), new(.14f, .26f), new(.36f, .34f) };
    private static readonly Vector2[] finalSmall = { new(-.28f, .36f), new(0f, .28f), new(.28f, .36f) };
    private static readonly Vector2[] rawLarge = { new(-.22f, .27f), new(.22f, .27f), new(-.30f, .49f), new(.30f, .49f), new(-.41f, .68f), new(.41f, .68f) };
    private static readonly Vector2[] rawSmall = { new(-.22f, .27f), new(.22f, .27f), new(-.30f, .49f), new(.30f, .49f) };

    internal static Vector2[] Candles(bool pixel, bool small)
        => pixel ? small ? finalSmall : finalLarge : small ? rawSmall : rawLarge;

    internal static float Hash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    private static Rectangle Cell(Rectangle source, int n)
    {
        int col = n % Cols, row = n / Cols;
        int x0 = source.X + col * source.Width / Cols, x1 = source.X + (col + 1) * source.Width / Cols;
        int y0 = source.Y + row * source.Height / Rows, y1 = source.Y + (row + 1) * source.Height / Rows;
        return new Rectangle(x0, y0, x1 - x0, y1 - y0);
    }

    // Piece centre relative to the ring, in world pixels.
    private static Vector2 Home(EbonRewardArt.Sprite sprite, Rectangle cell)
        => new((cell.X + cell.Width * .5f - sprite.Source.X - sprite.Source.Width * .5f) * sprite.Scale,
            (cell.Y + cell.Height * .5f - sprite.Source.Y) * sprite.Scale);

    // Where a piece is during the reweave (u 0..1): thrown out and up from the body's centre, then home.
    private static Vector2 Burst(int n, int identity, Vector2 home, Vector2 size, float u, out float spin)
    {
        float s = EbonChandelierRules.Scatter(u);
        Vector2 away = home - new Vector2(0, size.Y * .5f);
        away = away.LengthSquared() < 1f ? new Vector2(0, -1) : Vector2.Normalize(away);
        float h1 = Hash(identity, n * 3 + 1), h2 = Hash(identity, n * 3 + 2), h3 = Hash(identity, n * 3 + 3);
        float k = size.Y / 72f;
        Vector2 dir = new(away.X + (h1 - .5f) * .8f, away.Y * .7f - .45f - h2 * .5f);
        spin = (h3 - .5f) * 3.4f * s;
        return dir * ((34f + 36f * h1) * k * s) + new Vector2(0, 14f * k * s);
    }

    // A candle rides with the piece it sits on.
    internal static Vector2 CandleOffset(EbonRewardArt.Sprite sprite, Vector2 candle, int identity, float u)
    {
        int col = Math.Clamp((int)((candle.X + .5f) * Cols), 0, Cols - 1), row = Math.Clamp((int)(candle.Y * Rows), 0, Rows - 1);
        int n = row * Cols + col;
        return Burst(n, identity, Home(sprite, Cell(sprite.Source, n)), sprite.Size, u, out _);
    }

    private static Vector2 Screen(Vector2 world, bool snap)
    {
        Vector2 s = world - Main.screenPosition;
        // Final pixel art sits on the same 2 px dot grid as the pixel layer (which floors to a dot).
        return snap ? new Vector2(MathF.Floor(s.X * .5f) * 2f, MathF.Floor(s.Y * .5f) * 2f) : s;
    }

    private static void Restart(SpriteBatch batch, SamplerState sampler)
    {
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sampler, DepthStencilState.None, Main.Rasterizer, null,
            Main.GameViewMatrix.TransformationMatrix);
    }

    // The unlit body, whole or as its shatter pieces. Final art is pixel art (point sampling).
    internal static void Body(SpriteBatch batch, in ChandelierPose pose, int identity, Color light)
    {
        EbonRewardArt.Sprite sprite = pose.Sprite;
        bool pixel = sprite.Pixel;
        Color tint = Color.Lerp(light, Color.White, .55f);
        float u = pose.State == ChandelierState.Reweave ? pose.SinceImpact / EbonRewardRules.ReweaveTicks : 1f;
        if (pixel) Restart(batch, SamplerState.PointClamp);
        try
        {
            if (u >= 1f)
                batch.Draw(sprite.Texture, Screen(pose.Ring, pixel), sprite.Source, tint, pose.Angle,
                    new Vector2(sprite.Source.Width * .5f, 0f), sprite.Scale, SpriteEffects.None, 0f);
            else
                for (int n = 0; n < Cells; n++)
                {
                    Rectangle cell = Cell(sprite.Source, n);
                    Vector2 home = Home(sprite, cell);
                    Vector2 burst = Burst(n, identity, home, sprite.Size, u, out float spin);
                    Vector2 at = pose.Ring + (home + burst).RotatedBy(pose.Angle);
                    batch.Draw(sprite.Texture, Screen(at, pixel), cell, tint, pose.Angle + spin,
                        new Vector2(cell.Width, cell.Height) * .5f, sprite.Scale, SpriteEffects.None, 0f);
                }
        }
        finally
        {
            if (pixel) Restart(batch, SamplerState.LinearClamp);
        }
    }
}
