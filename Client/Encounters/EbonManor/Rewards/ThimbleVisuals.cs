#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Score = Convergence.Content.Encounters.EbonManor.Rewards.EbonThimbleScore;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Presentation of the Ebon Thimble (Content/Encounters/EbonManor/Rewards/EbonThimble.cs). Everything here is
// derived from replicated projectile state, so a second peer sees the same lift, yank, crash and piano; nothing
// here changes gameplay. Furniture sprites are drawn here (point sampling for final art, snapped to the 2 px dot,
// hung from the measured eyelets of Furniture.png and Piano.png); silk threads, the fingertip glint, the landing
// circle and the debris are Ebon pixel-layer sources.
[Autoload(Side = ModSide.Client)]
internal sealed class ThimbleVisuals : GlobalProjectile
{
    private const float RotationStep = MathF.PI / 24f;
    private static readonly Projectile?[] controllers = new Projectile?[Main.maxPlayers];

    private static readonly EbonRewardArt.Sprite?[] furnitureSprites = new EbonRewardArt.Sprite?[EbonRewardArt.FurnitureCount];
    private static EbonRewardArt.Sprite? pianoSprite;

    private Vector2 before, now, handBefore, handNow, flightDirection = Vector2.UnitX;
    private bool seen, registered, mirrored;
    private int state = -1;
    private ulong retryAt;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile p, bool lateInstantiation)
        => p.ModProjectile is EbonThimbleChannel or EbonThimblePiece or EbonThimblePiano;

    internal static void Clear()
    {
        Array.Clear(controllers);
        Array.Clear(furnitureSprites);
        pianoSprite = null;
    }

    // Sprites are resolved once and reused every frame (a lookup builds strings and walks the asset repository);
    // a disposed texture (a graphics or resource reload) is resolved again.
    private static EbonRewardArt.Sprite FurnitureSprite(int index)
    {
        index = Math.Clamp(index, 0, EbonRewardArt.FurnitureCount - 1);
        if (furnitureSprites[index] is { } known && !known.Texture.IsDisposed) return known;
        EbonRewardArt.Sprite sprite = EbonRewardArt.Furniture(index);
        furnitureSprites[index] = sprite;
        return sprite;
    }

    private static EbonRewardArt.Sprite PianoSprite()
    {
        if (pianoSprite is { } known && !known.Texture.IsDisposed) return known;
        EbonRewardArt.Sprite sprite = EbonRewardArt.Piano();
        pianoSprite = sprite;
        return sprite;
    }

    // Where the silk ties on, in texels from the sprite's top-left: the measured eyelet of final art, the top edge
    // of a painted placeholder. EyeletOffset is the same point relative to the sprite's centre, in world px.
    private static Vector2 Eyelet(EbonRewardArt.Sprite sprite, int index)
        => sprite.Pixel ? ThimbleSupport.Xna(Score.FurnitureEyelet(index)) : new Vector2(sprite.Source.Width * .5f, 0);
    private static Vector2 EyeletOffset(EbonRewardArt.Sprite sprite, int index) => (Eyelet(sprite, index) - sprite.Origin) * sprite.Scale;

    // Two accepted simulation samples, taken once per tick after everything has moved (see ThimbleSystem).
    internal void Sample(Projectile p)
    {
        bool first = !seen || Vector2.DistanceSquared(now, p.Center) > 800f * 800f;
        if (first) before = now = p.Center; else { before = now; now = p.Center; }
        if (p.ModProjectile is EbonThimbleChannel)
        {
            Vector2 hand = EbonThimbleChannel.Hand(p);
            if (first) handBefore = handNow = hand; else { handBefore = handNow; handNow = hand; }
        }
        seen = true;
    }
    internal Vector2 Center(Projectile p) => seen ? Vector2.Lerp(before, now, WeaponDrawClock.Fraction) : p.Center;
    private Vector2 Hand(Projectile p) => seen ? Vector2.Lerp(handBefore, handNow, WeaponDrawClock.Fraction) : EbonThimbleChannel.Hand(p);

    // The silk's anchor: the owner's fingertip, just past the hand along the raised arm.
    internal static Vector2 Tip(int owner)
    {
        if (owner < 0 || owner >= controllers.Length) return Vector2.Zero;
        if (controllers[owner] is { active: true } p && p.owner == owner && p.ModProjectile is EbonThimbleChannel)
            return p.GetGlobalProjectile<ThimbleVisuals>().Hand(p) + p.rotation.ToRotationVector2() * 5f;
        return Main.player[owner].MountedCenter;
    }

    // --- Events: one per replicated state change, fired where the visual peak is -------------------------
    public override void PostAI(Projectile p)
    {
        bool first = state < 0;
        switch (p.ModProjectile)
        {
            case EbonThimbleChannel: Channel(p); break;
            case EbonThimblePiece piece: Piece(p, piece, first); break;
            case EbonThimblePiano piano: Piano(p, piano, first); break;
        }
    }

    // One retained pixel source per projectile; a full or disabled layer is retried twice a second.
    private bool WantsSource => !registered && Main.GameUpdateCount >= retryAt;
    private void Registered(bool accepted)
    {
        registered = accepted;
        if (!accepted) retryAt = Main.GameUpdateCount + 30;
    }

    private void Channel(Projectile p)
    {
        if (p.owner >= 0 && p.owner < controllers.Length) controllers[p.owner] = p;
        if (WantsSource) Registered(EbonPixelLayer.Add(new Glint(p)));
    }

    private void Piece(Projectile p, EbonThimblePiece piece, bool first)
    {
        int s = piece.State;
        if (s == Score.Flying && p.velocity.LengthSquared() > 1) flightDirection = Vector2.Normalize(p.velocity);
        if (first)
        {
            // Rising from the fingertip is the peak of a lift; a piece first seen mid-flight (late join) is silent.
            if (s == Score.Hanging && piece.Age <= 2) Lifted(p, piece);
        }
        else if (s != state)
        {
            if (s == Score.Flying) Yanked(p, piece);
            else if (s == Score.Crashed) Crashed(p, piece);
            else if (s == Score.Dropped) EbonPixelLayer.Add(new Fx(FxKind.Cut, p.Center, Score.Seed(p.identity, piece.Index), piece.Index, p.owner, Vector2.Zero));
        }
        state = s;
        if (WantsSource && (s is Score.Hanging or Score.Armed or Score.Flying)) Registered(EbonPixelLayer.Add(new PieceThread(p)));
    }

    private static void Lifted(Projectile p, EbonThimblePiece piece)
    {
        EbonRewardAudio.Play("ThimbleLift", p.Center, .5f, 0, .03f, 4);
        EbonRewardAudio.Note(piece.Index, p.Center, .5f);
        EbonPixelLayer.Add(new Fx(FxKind.Lift, p.Center, Score.Seed(p.identity, piece.Index), piece.Index, p.owner, Vector2.Zero));
    }

    private static void Yanked(Projectile p, EbonThimblePiece piece)
    {
        Vector2 direction = p.velocity.SafeNormalize(Vector2.UnitX);
        EbonRewardAudio.Play("FurnitureYank", p.Center, .62f, (piece.Index - 3.5f) * .02f, .04f, 4);
        EbonPixelLayer.Add(new Fx(FxKind.Yank, p.Center, Score.Seed(p.identity, piece.Index), piece.Index, p.owner, direction));
    }

    private void Crashed(Projectile p, EbonThimblePiece piece)
    {
        EbonRewardAudio.Play("FurnitureCrash", p.Center, .78f, (piece.Index - 3.5f) * .015f, .06f, 4);
        EbonPixelLayer.Add(new Fx(FxKind.Crash, p.Center, Score.Seed(p.identity, piece.Index), piece.Index, p.owner, flightDirection));
    }

    private void Piano(Projectile p, EbonThimblePiano piano, bool first)
    {
        // 0 unseen and waiting, 1 falling, 2 crashed, 3 dropped.
        int phase = piano.Dropped ? 3 : piano.Crashed ? 2 : piano.Pending ? 0 : 1;
        // Fixed when first seen, so it cannot flip while the owner walks past it.
        if (first && ThimbleSupport.ValidOwner(p)) mirrored = Score.PianoMirrored(p.Center.X, Main.player[p.owner].Center.X);
        if (!first && phase != state)
        {
            if (phase == 1) EbonRewardAudio.Play("ThimbleLift", p.Center, .55f, -.45f, .02f, 2);
            else if (phase == 2)
            {
                // The impact point is the bottom edge: the centre of the true damage circle and of the landing ring.
                EbonRewardAudio.Play("PianoCrash", p.Bottom, 1f, 0, .02f, 2);
                Shake(p.Bottom, 4f);
                EbonPixelLayer.Add(new Fx(FxKind.Piano, p.Bottom, Score.Seed(p.identity, 9), 0, p.owner, Vector2.Zero));
            }
        }
        state = phase;
        if (WantsSource && phase < 2) Registered(EbonPixelLayer.Add(new PianoRite(p)));
    }

    // Local feel only: honours Reduced Effects and the shake switch of the Ebon client config.
    private static void Shake(Vector2 at, float strength)
    {
        var config = ModContent.GetInstance<EbonVisualConfig>();
        if (Main.dedServ || config.ReducedEffects || !config.ScreenShake) return;
        ScreenShakeSystem.StartShakeAtPoint(at, strength, shakeDirection: Vector2.UnitY, angularVariance: .45f,
            shakeStrengthDissipationIncrement: .55f);
    }

    // --- Furniture and piano sprites -------------------------------------------------------------------
    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        if (p.ModProjectile is EbonThimblePiece piece) DrawPiece(p, piece);
        else if (p.ModProjectile is EbonThimblePiano piano) DrawPiano(p, piano);
        return false;
    }

    private void DrawPiece(Projectile p, EbonThimblePiece piece)
    {
        int s = piece.State;
        if (s == Score.Crashed && piece.Age > 0) return; // the debris source takes over after the impact frame
        EbonRewardArt.Sprite sprite = FurnitureSprite(piece.Index);
        float fraction = WeaponDrawClock.Fraction;
        float alpha = s switch
        {
            Score.Hanging => Math.Clamp((piece.Age + fraction + 1) / 6f, 0, 1),
            Score.Dropped => 1 - Math.Clamp((piece.Age + fraction) / Score.DropLife, 0, 1),
            _ => 1f,
        };
        // Painted placeholders may pop in and squash on impact; pixel art keeps its exact scale and only fades.
        float pop = sprite.Pixel ? 1f : .62f + .38f * Math.Min(1, Score.LiftEase(s == Score.Hanging ? piece.Age : Score.LiftEaseTicks));
        Vector2 scale = s == Score.Crashed && !sprite.Pixel ? new Vector2(1.18f, .8f) : new Vector2(pop);
        // Hanging pieces swing from their eyelet; thrown ones tumble about their centre. The props are front-on and
        // left-right symmetric, so they are never mirrored with the owner's facing.
        bool hung = s is Score.Hanging or Score.Armed;
        Vector2 center = Center(p);
        if (hung) Draw(sprite, center + EyeletOffset(sprite, piece.Index), Eyelet(sprite, piece.Index), p.rotation, scale, alpha);
        else Draw(sprite, center, sprite.Origin, p.rotation, scale, alpha);
    }

    private void DrawPiano(Projectile p, EbonThimblePiano piano)
    {
        if (piano.Pending || (piano.Crashed && piano.CrashAge > 1)) return;
        EbonRewardArt.Sprite sprite = PianoSprite();
        float fraction = WeaponDrawClock.Fraction;
        float alpha = piano.Dropped ? 1 - Math.Clamp((piano.DropAge + fraction) / Score.PianoFade, 0, 1)
            : Math.Clamp((piano.FallAge + fraction + 1) / 6f, 0, 1);
        // Anchored on the bottom edge (the feet of the final art) so a placeholder's crash squash settles onto the floor.
        // The final piano keeps its exact scale and is mirrored so that its keyboard end faces the owner.
        Vector2 bottom = Center(p) + new Vector2(0, p.height * .5f);
        Draw(sprite, bottom, new Vector2(sprite.Source.Width * .5f, sprite.Source.Height), 0,
            piano.Crashed && !sprite.Pixel ? new Vector2(1.2f, .76f) : Vector2.One, alpha, sprite.Pixel && mirrored);
    }

    // The texture's own corner is snapped to the dot grid (not the origin: eyelets sit on half texels), so an
    // unrotated sprite always lies on whole dots. A mirrored sprite keeps the same origin and corner: the flip only
    // reverses the texels inside the quad.
    private static Vector2 Snap(Vector2 v)
    {
        const float dot = EbonRewardArt.PixelScale;
        return new Vector2(MathF.Round(v.X / dot) * dot, MathF.Round(v.Y / dot) * dot);
    }
    private static float Step(float rotation) => MathF.Round(rotation / RotationStep) * RotationStep;

    private static void Draw(EbonRewardArt.Sprite sprite, Vector2 world, Vector2 origin, float rotation, Vector2 scale, float alpha, bool mirror = false)
    {
        if (alpha <= .01f) return;
        SpriteBatch batch = Main.spriteBatch;
        Vector2 at = world - Main.screenPosition;
        WorldBatchParameters saved = default;
        if (sprite.Pixel)
        {
            // Final art: whole dots, stepped rotation, nearest-neighbour sampling, in the caller's own batch state.
            Vector2 corner = origin * scale * sprite.Scale;
            at = Snap(at - corner) + corner;
            rotation = Step(rotation);
            saved = WorldBatchParameters.Capture(batch);
            batch.End();
            batch.Begin(saved.Sort, saved.Blend, SamplerState.PointClamp, saved.Depth, saved.Raster, saved.Effect, saved.Transform);
        }
        try
        {
            batch.Draw(sprite.Texture, at, sprite.Source, Color.White * alpha, rotation, origin, scale * sprite.Scale,
                mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }
        finally
        {
            if (sprite.Pixel) { batch.End(); saved.Restore(batch); }
        }
    }

    // --- Pixel-layer sources ---------------------------------------------------------------------------
    private enum FxKind : byte { Lift, Yank, Crash, Cut, Piano }

    // A short event effect evaluated analytically from its age; it owns no projectile.
    private sealed class Fx(FxKind kind, Vector2 at, int seed, int index, int owner, Vector2 direction) : IEbonPixelSource
    {
        private readonly ulong start = Main.GameUpdateCount;

        public bool Emit(EbonPixelCanvas canvas)
        {
            float t = Main.GameUpdateCount - start + canvas.Fraction;
            bool reduced = canvas.Reduced;
            switch (kind)
            {
                case FxKind.Lift:
                {
                    if (t >= 18) return false;
                    Vector2 tip = Tip(owner);
                    canvas.Star(tip, 16, Math.Clamp(t / 12f, 0, 1), seed);
                    canvas.Burst(tip, seed, reduced ? 3 : 6, t, 18, 2.4f, -.02f, EbonShardKind.Silk, 2.4f, -MathF.PI / 2);
                    return true;
                }
                case FxKind.Yank:
                {
                    if (t >= 12) return false;
                    canvas.Star(at, 18, Math.Clamp(t / 10f, 0, 1), seed);
                    canvas.Burst(at, seed, reduced ? 2 : 5, t, 12, 3f, 0, EbonShardKind.Silk, 2.2f, direction.ToRotation() + MathF.PI);
                    return true;
                }
                case FxKind.Cut:
                    if (t >= 14) return false;
                    canvas.Burst(at, seed, reduced ? 2 : 5, t, 14, 2.6f, .08f, EbonShardKind.Silk);
                    return true;
                case FxKind.Crash:
                {
                    if (t >= 30) return false;
                    Score.Material material = Score.MaterialOf(index);
                    EbonShardKind main = material switch
                    {
                        Score.Material.Glass => EbonShardKind.Glass,
                        Score.Material.Wire => EbonShardKind.Spark,
                        _ => EbonShardKind.Wood,
                    };
                    // Splinters kick back and sideways off whatever stopped the piece.
                    float back = direction.ToRotation() + MathF.PI;
                    canvas.Star(at, 44, Math.Clamp(t / 10f, 0, 1), seed);
                    if (t < 9) canvas.Ring(at, 6 + t * 5, EbonTone.Ivory, 1, 1 - t / 9f);
                    canvas.Burst(at, seed, reduced ? 7 : 14, t, 28, 6.4f, .3f, main, 3.4f, back);
                    canvas.Burst(at, seed ^ 0x5bd1, reduced ? 3 : 7, t, 26, 3.4f, .05f, EbonShardKind.Silk);
                    if (material == Score.Material.Glass && !reduced)
                        canvas.Burst(at, seed ^ 0x2c9, 5, t, 20, 7.5f, .2f, EbonShardKind.Spark, 3.4f, back);
                    return true;
                }
                default:
                {
                    if (t >= 56) return false;
                    float radius = EbonRewardRules.PianoRadius;
                    // The shock front reaches the true crash radius, so the ring tells the truth about the hit.
                    float front = radius * EbonRewardRules.Smooth(t / 9f);
                    canvas.Star(at, 130, Math.Clamp(t / 14f, 0, 1), seed);
                    if (t < 16) canvas.Ring(at, front, EbonTone.Ivory, 2, 1 - t / 16f);
                    if (!reduced && t < 24) canvas.Ring(at, front * .72f, EbonTone.Silver, 1, 1 - t / 24f);
                    float up = -MathF.PI / 2;
                    canvas.Burst(at, seed, reduced ? 11 : 22, t, 46, 10f, .42f, EbonShardKind.Wood, 3.2f, up);
                    canvas.Burst(at, seed ^ 0x2f1, reduced ? 8 : 16, t, 42, 11f, .44f, EbonShardKind.Glass, 3.4f, up);
                    canvas.Burst(at, seed ^ 0x7c3, reduced ? 8 : 18, t, 30, 13f, .22f, EbonShardKind.Spark, 3.6f, up);
                    canvas.Burst(at, seed ^ 0x1d9, reduced ? 5 : 10, t, 54, 5.5f, .07f, EbonShardKind.Lace);
                    return true;
                }
            }
        }
    }

    // The silk of one piece: from the fingertip to its eyelet while it hangs, then whipped free as it is yanked.
    private sealed class PieceThread(Projectile projectile) : IEbonPixelSource
    {
        private readonly int slot = projectile.whoAmI, identity = projectile.identity, owner = projectile.owner;

        public bool Emit(EbonPixelCanvas canvas)
        {
            Projectile p = Main.projectile[slot];
            if (!p.active || p.identity != identity || p.owner != owner || p.ModProjectile is not EbonThimblePiece piece) return false;
            int s = piece.State;
            if (s is not (Score.Hanging or Score.Armed or Score.Flying)) return false;
            EbonRewardArt.Sprite sprite = FurnitureSprite(piece.Index);
            Vector2 center = p.GetGlobalProjectile<ThimbleVisuals>().Center(p);
            // The silk ends on the eyelet. A hanging piece swings from it; a thrown one tumbles about its centre, so
            // its eyelet turns with it (by the angle the sprite is actually drawn at).
            Vector2 tie = EyeletOffset(sprite, piece.Index);
            Vector2 eyelet = center + (s == Score.Flying ? tie.RotatedBy(sprite.Pixel ? Step(p.rotation) : p.rotation) : tie);
            Vector2 tip = Tip(owner);
            float age = piece.Age + canvas.Fraction;
            if (s == Score.Flying)
            {
                // The thread snaps: its far end races along to the flying piece and the strand is gone.
                float snap = Math.Clamp(age / 9f, 0, 1);
                if (snap >= 1) return false;
                canvas.Thread(Vector2.Lerp(tip, eyelet, EbonRewardRules.Smooth(snap)), eyelet, EbonTone.Moon, 1, 1 - snap, 1);
                return true;
            }
            float wind = s == Score.Armed ? Score.Wind(piece.Age, piece.Index) : 0;
            float slack = (1 - wind) * 8f * MathF.Sin(ThimbleSupport.Clock * .07f + piece.Index * 1.7f);
            Strand(canvas, tip, eyelet, slack, wind > 0 ? EbonTone.Moon : EbonTone.Silver,
                s == Score.Hanging ? Math.Clamp((age + 1) / 6f, 0, 1) : 1, wind > 0 ? 1 : .45f);
            return true;
        }

        // Slack silk bows sideways a little; taut silk (and Reduced Effects) is a straight thread.
        private static void Strand(EbonPixelCanvas canvas, Vector2 a, Vector2 b, float bow, EbonTone tone, float alpha, float shimmer)
        {
            Vector2 d = b - a;
            float length = d.Length();
            if (canvas.Reduced || MathF.Abs(bow) < 1.5f || length < 24)
            {
                canvas.Thread(a, b, tone, 1, alpha, shimmer);
                return;
            }
            Vector2 mid = (a + b) * .5f + new Vector2(-d.Y, d.X) / length * bow;
            canvas.Thread(a, mid, tone, 1, alpha, shimmer);
            canvas.Thread(mid, b, tone, 1, alpha, shimmer);
        }
    }

    // The fingertip glint while channeling (pulsing on the beat, flaring on each lift or yank, dim when the mana
    // is spent) and, for the owner with all eight raised, the landing circle that follows the cursor.
    private sealed class Glint(Projectile projectile) : IEbonPixelSource
    {
        private readonly int slot = projectile.whoAmI, identity = projectile.identity, owner = projectile.owner;

        public bool Emit(EbonPixelCanvas canvas)
        {
            Projectile p = Main.projectile[slot];
            if (!p.active || p.identity != identity || p.owner != owner || p.ModProjectile is not EbonThimbleChannel c) return false;
            Vector2 tip = Tip(owner);
            float clock = c.Age + canvas.Fraction;
            if (c.Phase == Score.Releasing)
            {
                float kick = Score.LaunchKick(c.Age, c.Lifted);
                if (kick > .03f)
                {
                    canvas.Star(tip, 8 + 14 * kick, 1 - kick, owner * 31 + c.Age);
                    canvas.Dot(tip, EbonTone.Moon, 2, kick);
                }
                return true;
            }
            bool spent = c.Phase == Score.Spent, ready = Score.Finale(c.Lifted);
            // Phased on the lift schedule so the pulse peaks on each lift instead of flashing once just before it.
            float beat = Score.BeatPhase(clock);
            float pulse = MathF.Pow(1 - beat, 3), lift = Score.LiftKick(c.Age, c.Lifted);
            canvas.Dot(tip, spent ? EbonTone.Rose : ready ? EbonTone.Moon : EbonTone.Silver, 1, spent ? .6f : 1);
            float glow = Math.Max(pulse, lift);
            if (!spent && glow > .08f)
                canvas.Star(tip, 4 + 7 * pulse + 9 * lift + (ready ? 3 : 0), Math.Clamp(1 - glow, 0, .9f),
                    owner * 17 + Score.BeatIndex(clock));
            if (ready && owner == Main.myPlayer)
            {
                // Where the piano will come down: the true crash radius, and the silk it falls along.
                Vector2 cursor = Main.MouseWorld;
                float breathe = .5f + .5f * MathF.Sin(clock * .16f);
                canvas.Ring(cursor, EbonRewardRules.PianoRadius, EbonTone.Silver, 1, .16f + .14f * breathe);
                if (!canvas.Reduced)
                    canvas.Thread(cursor, cursor - new Vector2(0, EbonRewardRules.PianoHeight), EbonTone.Silver, 1, .22f, .6f);
            }
            return true;
        }
    }

    // The finale piano: the landing circle from the release, the cut silk it drops from, and its appearing glint.
    private sealed class PianoRite(Projectile projectile) : IEbonPixelSource
    {
        private readonly int slot = projectile.whoAmI, identity = projectile.identity, owner = projectile.owner;
        private readonly float startY = projectile.Center.Y;

        public bool Emit(EbonPixelCanvas canvas)
        {
            Projectile p = Main.projectile[slot];
            if (!p.active || p.identity != identity || p.owner != owner || p.ModProjectile is not EbonThimblePiano piano
                || piano.Crashed || piano.Dropped) return false;
            float age = piano.Age + canvas.Fraction;
            Vector2 target = new(p.Center.X, piano.TargetY);
            // The circle is the real crash radius and sharpens as the piano nears.
            float warn = EbonRewardRules.Smooth(age / Score.PianoAppears);
            canvas.Ring(target, EbonRewardRules.PianoRadius, EbonTone.Silver, 1, .18f + .32f * warn);
            if (!canvas.Reduced)
                for (int k = 0; k < 6; k++)
                {
                    float angle = age * .03f + k * MathF.Tau / 6;
                    canvas.Dot(target + angle.ToRotationVector2() * EbonRewardRules.PianoRadius, EbonTone.Ivory, 1, .25f + .4f * warn);
                }
            if (piano.Pending) return true;
            float fall = piano.FallAge + canvas.Fraction;
            if (fall >= 12) return true;
            // It was woven in and is cut as it drops: the silk trails upward and fades.
            EbonRewardArt.Sprite sprite = PianoSprite();
            Vector2 center = p.GetGlobalProjectile<ThimbleVisuals>().Center(p);
            float bottomY = center.Y + p.height * .5f, topY = bottomY - sprite.Size.Y, fade = 1 - fall / 12f;
            if (sprite.Pixel)
            {
                // Final art: one strand, tied to the ring on the tip of the lid (on the far side when mirrored).
                Vector2 ring = ThimbleSupport.Xna(Score.PianoEyeletFor(sprite.Source.Width, p.GetGlobalProjectile<ThimbleVisuals>().mirrored));
                Vector2 eyelet = new(center.X + (ring.X - sprite.Source.Width * .5f) * sprite.Scale, bottomY - (sprite.Source.Height - ring.Y) * sprite.Scale);
                canvas.Thread(new Vector2(eyelet.X, startY - 480), eyelet, EbonTone.Silver, 1, fade, 1);
                if (fall < 8) canvas.Star(eyelet, 40, fall / 8f, Score.Seed(identity, 3));
                return true;
            }
            // The painted stand-in hangs on three strands.
            for (int k = -1; k <= 1; k++)
                canvas.Thread(new Vector2(center.X + k * 44, startY - 480), new Vector2(center.X + k * 44, topY), EbonTone.Silver, 1, fade, 1);
            if (fall < 8) canvas.Star(new Vector2(center.X, topY), 40, fall / 8f, Score.Seed(identity, 3));
            return true;
        }
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ThimbleSystem : ModSystem
{
    // After everything has moved: the two samples the sprites and silk interpolate between.
    public override void PostUpdateEverything()
    {
        if (Main.gamePaused) return;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EbonThimbleChannel or EbonThimblePiece or EbonThimblePiano)
                p.GetGlobalProjectile<ThimbleVisuals>().Sample(p);
    }
    public override void OnWorldUnload() => ThimbleVisuals.Clear();
    public override void Unload() => ThimbleVisuals.Clear();
}
