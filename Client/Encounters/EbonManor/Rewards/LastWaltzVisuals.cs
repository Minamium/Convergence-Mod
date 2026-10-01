#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Presentation for The Last Waltz. Content never references this file: it enters as a client-only
// GlobalProjectile. Noirette is drawn from her Raid sheet (48x64 cells at 1x, point sampled) through the
// Manor BodyPass with a compact twin-tail mesh; EbonNoirette's own mesh owns static boss state, so it cannot
// be called without a boss. Threads, spokes, stars and debris are retained sources of the Ebon pixel layer.
// Everything is drawn between two accepted ticks (WeaponDrawClock.Fraction); sounds fire from the same
// event as the visual peak (yank, crash, spokes opening).
[Autoload(Side = ModSide.Client)]
internal sealed class LastWaltzVisuals : GlobalProjectile
{
    private const int Columns = 12, Rows = 16;
    private const float BodyScale = 1;
    // Same measurements as EbonNoirette (private there): logical pixel under the centre, tail masks, fingertips.
    private static readonly Vector2 Pivot = new(26, 33);
    private static readonly Vector3[] Tails =
    {
        new(20, 8, 40), new(20, 8, 40), new(20, 8, 40), new(20, 8, 40),
        new(16, 10, 38), new(13, 8, 40), new(18, 22, 44), new(20, 8, 40),
    };
    private static readonly Vector2[] Hands =
    {
        new(30, 36), new(30, 36), new(31, 36), new(30, 35),
        new(44, 9), new(15, 28), new(31, 27), new(44, 22),
    };
    private static readonly VertexPositionColorTexture[] grid = new VertexPositionColorTexture[(Columns + 1) * (Rows + 1)];
    private static readonly VertexPositionColorTexture[] skin = new VertexPositionColorTexture[Columns * Rows * 6];

    private Vector2 before, now, lastVelocity, tail, tailVelocity, hem, hemVelocity;
    private bool initialized, entered, registered, layerOk, yanked, noted;
    private int frame, idle, previousTick = -1, enteredAt, hitDelay;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile p, bool lateInstantiation)
        => p.ModProjectile is EbonLastWaltzCompanion or EbonWaltzFling or EbonWaltzSpoke;

    internal Vector2 Center(Projectile p) => initialized ? Vector2.Lerp(before, now, WeaponDrawClock.Fraction) : p.Center;

    public override void PostAI(Projectile p)
    {
        // PostAI runs before the tick's movement, so the accepted end-of-tick centre is where it will be.
        Vector2 end = p.Center + (p.ModProjectile.ShouldUpdatePosition() ? p.velocity : Vector2.Zero);
        if (!initialized || Vector2.DistanceSquared(now, end) > 600 * 600) before = now = end;
        else { before = now; now = end; }
        initialized = true;
        hitDelay = Math.Max(0, hitDelay - 1);
        switch (p.ModProjectile)
        {
            case EbonLastWaltzCompanion: Companion(p); break;
            case EbonWaltzFling fling: Fling(p, fling); break;
            case EbonWaltzSpoke spoke: Spoke(p, spoke); break;
        }
        lastVelocity = p.velocity;
    }

    // --- Companion ---------------------------------------------------------------------------
    private void Companion(Projectile p)
    {
        int tick = (int)p.ai[0];
        if (!entered) { entered = true; Entrance(p); }
        else enteredAt++;
        idle = (idle + 1) % 2520;
        frame = Math.Clamp(EbonLastWaltzRules.Frame(tick, p.velocity.LengthSquared() > 3.5f * 3.5f, idle), 0, EbonLastWaltzRules.FrameCount - 1);
        // Damped springs lag behind the body: the tails swing longer than the hem.
        Vector2 motion = Vector2.Clamp(now - before, new(-24), new(24));
        tailVelocity += -tail * .045f - tailVelocity * .09f - motion * .42f;
        hemVelocity += -hem * .10f - hemVelocity * .18f - motion * .30f;
        tail = Vector2.Clamp(tail + tailVelocity, new(-9), new(9));
        hem = Vector2.Clamp(hem + hemVelocity, new(-3), new(3));
        // Anticipation: silk gathers at the target and converges where the six spokes will open.
        int gather = EbonLastWaltzRules.WaltzStart - EbonLastWaltzRules.Anticipation;
        if (previousTick >= 0 && previousTick < gather && tick >= gather && tick < gather + 3)
            EbonPixelLayer.Add(new GatherSource(Ref.Of(p), Main.GameUpdateCount));
        previousTick = tick;
    }

    private void Entrance(Projectile p)
    {
        // Woven in out of lace and silk, with a soft, low chord.
        EbonPixelLayer.Add(new BurstSource(p.Center, p.identity * 31, 16, 38, 3.2f, -.03f, EbonShardKind.Lace, EbonShardKind.Silk, 0, 56));
        EbonRewardAudio.Play("WaltzOpen", p.Center, .3f, -.2f);
    }

    // Pose offsets on the fractional clock: cast leans back, the yank recoils, the parasol sways.
    private void Pose(Projectile p, out Vector2 root, out float lean, out int facing, out float flash)
    {
        float fraction = WeaponDrawClock.Fraction, time = (float)(Main.GameUpdateCount % 1_000_000) + fraction;
        facing = p.spriteDirection < 0 ? -1 : 1;
        lean = Math.Clamp(p.rotation, -.2f, .2f);
        flash = 0;
        Vector2 offset = new(0, MathF.Sin(time * .05f + p.identity) * 2.5f);
        switch (frame)
        {
            case 4: lean -= .05f; offset.Y -= 3; break;
            case 5:
                float since = EbonLastWaltzRules.SinceYank((int)p.ai[0]) + fraction;
                float kick = EbonVisualsMath.Pulse(since, 7);
                lean -= .12f * kick; offset.X -= 7 * kick * facing; flash = .6f * EbonVisualsMath.Pulse(since, 4);
                break;
            case 6: lean += MathF.Sin(time * .11f) * .06f; break;
            case 7: lean -= .04f; break;
        }
        root = Center(p) + offset;
    }

    private static Vector2 Map(int facing, float lean, Vector2 root, Vector2 local)
    {
        Vector2 p = (local - Pivot) * BodyScale;
        if (facing < 0) p.X = -p.X;
        float angle = lean * facing;
        return root + new Vector2(p.X * MathF.Cos(angle) - p.Y * MathF.Sin(angle), p.X * MathF.Sin(angle) + p.Y * MathF.Cos(angle));
    }

    // Her real fingertip on the drawn pose: threads and the parasol gather start here.
    internal Vector2 Hand(Projectile p)
    {
        Pose(p, out Vector2 root, out float lean, out int facing, out _);
        return Map(facing, lean, root, Hands[Math.Clamp(frame, 0, Hands.Length - 1)]);
    }

    private void DrawBody(SpriteBatch batch, Projectile p)
    {
        Pose(p, out Vector2 root, out float lean, out int facing, out float flash);
        var art = EbonMaterials.Texture("Noirette");
        bool reduced = EbonVisuals.Reduced;
        float motion = reduced ? .3f : 1;
        float time = (float)(Main.GameUpdateCount % 1_000_000) + WeaponDrawClock.Fraction;
        float woven = EbonVisualsMath.Ease((enteredAt + WeaponDrawClock.Fraction) / 40f);
        using var scope = new WorldGraphicsScope(batch);
        var shader = EbonMaterials.Manor(time);
        var src = new Rectangle(frame % 4 * 48, frame / 4 * 64, 48, 64);
        shader.SetTexture(art, 0, SamplerState.PointClamp);
        shader.TrySetParameter("region", new Vector4(src.X / (float)art.Width, src.Y / (float)art.Height, src.Width / (float)art.Width, src.Height / (float)art.Height));
        shader.TrySetParameter("signal", new Vector4(1, woven, reduced ? 1 : 0, p.identity * .29f));
        shader.TrySetParameter("shape", new Vector4(flash, .30f, 48, 64));
        shader.TrySetParameter("weaveDensity", 9f);
        var mask = Tails[Math.Clamp(frame, 0, Tails.Length - 1)];
        float sway = MathF.Sin(time * .045f + p.identity) * 1.2f * motion;
        for (int y = 0; y <= Rows; y++)
            for (int x = 0; x <= Columns; x++)
            {
                Vector2 uv = new(x / (float)Columns, y / (float)Rows);
                Vector2 point = uv * new Vector2(48, 64);
                // Tails grow from the tie point and lag the body; spring X is world-space, the sheet faces right.
                float wt = EbonVisualsMath.Ease((mask.X - point.X) / 8) * EbonVisualsMath.Ease((point.Y - mask.Y) / 8) * EbonVisualsMath.Ease((mask.Z - point.Y) / 6);
                float reach = MathF.Pow(Math.Clamp((point.Y - mask.Y) / 30, 0, 1), 2);
                point += new Vector2((tail.X * motion + sway) * facing, tail.Y * .35f * motion) * reach * wt;
                // Hem: petticoat flutter with a short lag; the legs stay put.
                float wh = EbonVisualsMath.Ease((point.Y - 36) / 12) * EbonVisualsMath.Ease((56 - point.Y) / 6);
                point += new Vector2(MathF.Sin(time * .05f - point.Y * .2f + point.X * .1f) * .6f * motion + hem.X * facing * .5f * motion,
                    MathF.Sin(time * .06f + point.X * .3f) * .3f * motion) * wh;
                grid[y * (Columns + 1) + x] = new(new(Map(facing, lean, root, point) - Main.screenPosition, 0), Color.White, uv);
            }
        int n = 0;
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Columns; x++)
            {
                int a = y * (Columns + 1) + x, b = a + 1, c = a + Columns + 1, d = c + 1;
                skin[n++] = grid[a]; skin[n++] = grid[b]; skin[n++] = grid[c];
                skin[n++] = grid[b]; skin[n++] = grid[d]; skin[n++] = grid[c];
            }
        shader.Apply("BodyPass");
        Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, skin, 0, n / 3);
    }

    // --- Flung furniture -----------------------------------------------------------------------
    private void Fling(Projectile p, EbonWaltzFling fling)
    {
        if (!registered) { registered = true; EbonPixelLayer.Add(new FlingThread(Ref.Of(p))); }
        if (yanked || !fling.Flying) return;
        yanked = true;
        EbonRewardAudio.Play("FurnitureYank", p.Center, .5f, fling.Piece * .03f - .07f, .03f);
    }

    private static void FlingPose(EbonWaltzFling fling, out EbonRewardArt.Sprite sprite, out float rotation, out float scale)
    {
        sprite = EbonRewardArt.Furniture(fling.Piece);
        float age = Math.Max(0, fling.Age - 1 + WeaponDrawClock.Fraction);
        // Final art keeps its own dot (1x here, like Noirette); painted placeholders are scaled down.
        scale = (sprite.Pixel ? 1f : sprite.Scale * .7f) * (.55f + .45f * EbonVisualsMath.Ease(age / 6));
        rotation = fling.Flying ? (age - EbonLastWaltzRules.Lift) * .2f * (fling.Piece % 2 == 0 ? 1 : -1) : MathF.Sin(age * .5f) * .08f;
        if (sprite.Pixel) { const float step = MathF.Tau / 16; rotation = MathF.Round(rotation / step) * step; }
    }

    // The silk eyelet at the top centre of the piece, where the thread is tied.
    internal Vector2 Eyelet(Projectile p, EbonWaltzFling fling)
    {
        FlingPose(fling, out var sprite, out float rotation, out float scale);
        return Center(p) + new Vector2(0, -sprite.Source.Height * scale * .5f).RotatedBy(rotation);
    }

    private void DrawFling(SpriteBatch batch, Projectile p, EbonWaltzFling fling, Color light)
    {
        FlingPose(fling, out var sprite, out float rotation, out float scale);
        using var scope = new WorldGraphicsScope(batch);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sprite.Pixel ? SamplerState.PointClamp : SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        batch.Draw(sprite.Texture, Center(p) - Main.screenPosition, sprite.Source, Color.Lerp(light, Color.White, .5f),
            rotation, sprite.Origin, scale, SpriteEffects.None, 0);
        batch.End();
    }

    // Piece, shards: furniture breaks into its own materials.
    private static (EbonShardKind Primary, EbonShardKind Accent) Shards(int piece) => piece switch
    {
        1 => (EbonShardKind.Spark, EbonShardKind.Wood),
        2 => (EbonShardKind.Wood, EbonShardKind.Lace),
        3 => (EbonShardKind.Wood, EbonShardKind.Glass),
        4 => (EbonShardKind.Wood, EbonShardKind.Spark),
        5 => (EbonShardKind.Glass, EbonShardKind.Silk),
        _ => (EbonShardKind.Wood, EbonShardKind.Silk),
    };

    public override void OnKill(Projectile p, int timeLeft)
    {
        if (p.ModProjectile is not EbonWaltzFling { Flying: true } fling) return;
        // The crash: debris thrown back along the way it came, the next note of the arpeggio.
        var (primary, accent) = Shards(fling.Piece);
        float heading = (lastVelocity.LengthSquared() > .01f ? -lastVelocity : -Vector2.UnitY).ToRotation();
        EbonPixelLayer.Add(new BurstSource(p.Center, p.identity * 13 + fling.Piece, 11, 28, 5.5f, .35f, primary, accent, 26, 70, heading, MathF.PI * 1.1f));
        EbonRewardAudio.Play("FurnitureCrash", p.Center, .6f, fling.Piece * .02f - .05f, .04f);
        EbonRewardAudio.Note(fling.Piece, p.Center, .32f);
    }

    // --- Spokes --------------------------------------------------------------------------------
    private void Spoke(Projectile p, EbonWaltzSpoke spoke)
    {
        if (registered) return;
        registered = true;
        layerOk = EbonPixelLayer.Add(new SpokeSource(Ref.Of(p)));
        if (spoke.Index != 0) return;
        // The release: one chord as the parasol opens, lace and silk from the hub, a small local shake.
        EbonRewardAudio.Play("WaltzOpen", p.Center, .75f);
        EbonPixelLayer.Add(new BurstSource(p.Center, p.identity * 7, 12, 30, 4.2f, 0, EbonShardKind.Lace, EbonShardKind.Silk, 0, 0));
        if (p.owner == Main.myPlayer) Shake(p.Center, 1.8f);
    }

    public override void OnHitNPC(Projectile p, NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Contact feedback is the owner's: hits are decided there, and no packet carries them to peers.
        if (p.ModProjectile is not EbonWaltzSpoke spoke || hitDelay > 0) return;
        hitDelay = 4;
        Vector2 axis = spoke.Angle(spoke.Age).ToRotationVector2();
        float along = Math.Clamp(Vector2.Dot(target.Center - p.Center, axis), EbonLastWaltzRules.SpokeInner,
            Math.Max(EbonLastWaltzRules.SpokeInner, spoke.Length(spoke.Age)));
        Vector2 contact = Vector2.Clamp(p.Center + axis * along, target.position, target.position + new Vector2(target.width, target.height));
        EbonPixelLayer.Add(new BurstSource(contact, p.identity * 5 + spoke.Index, 4, 16, 3.6f, .1f, EbonShardKind.Spark, EbonShardKind.Silk, 14 + spoke.Index, 0));
        if (noted) return;
        noted = true;
        EbonRewardAudio.Note(2 + spoke.Index, contact, .3f);
    }

    private static void Shake(Vector2 at, float strength)
    {
        if (Main.dedServ || EbonVisuals.Reduced || !ModContent.GetInstance<EbonVisualConfig>().ScreenShake) return;
        ScreenShakeSystem.StartShakeAtPoint(Vector2.Lerp(Main.LocalPlayer.Center, at, .2f), strength,
            shakeDirection: Vector2.UnitY, angularVariance: .7f, shakeStrengthDissipationIncrement: .5f);
    }

    // --- Draw ----------------------------------------------------------------------------------
    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        SpriteBatch batch = Main.spriteBatch;
        switch (p.ModProjectile)
        {
            case EbonLastWaltzCompanion: if (initialized) DrawBody(batch, p); break;
            case EbonWaltzFling fling: if (initialized) DrawFling(batch, p, fling, lightColor); break;
            case EbonWaltzSpoke spoke: if (registered && !layerOk) DrawPlainSpoke(batch, p, spoke); break;
        }
        return false;
    }

    // Only if the pixel layer refused the source: the spokes are gameplay and must stay readable.
    private void DrawPlainSpoke(SpriteBatch batch, Projectile p, EbonWaltzSpoke spoke)
    {
        float age = Math.Max(0, spoke.Age - 1 + WeaponDrawClock.Fraction), length = spoke.Length(age);
        if (length < EbonLastWaltzRules.SpokeInner + 2) return;
        Vector2 axis = spoke.Angle(age).ToRotationVector2();
        batch.Draw(TextureAssets.MagicPixel.Value, Center(p) + axis * EbonLastWaltzRules.SpokeInner - Main.screenPosition,
            new Rectangle(0, 0, 1, 1), new Color(206, 210, 230) * .85f, axis.ToRotation(), Vector2.Zero,
            new Vector2(length - EbonLastWaltzRules.SpokeInner, 3), SpriteEffects.None, 0);
    }

    // --- Pixel-layer sources ---------------------------------------------------------------------
    // A projectile is found again by slot and exact identity; slots are reused.
    private readonly record struct Ref(int Slot, int Identity, int Owner, int Type)
    {
        internal static Ref Of(Projectile p) => new(p.whoAmI, p.identity, p.owner, p.type);
        internal bool TryGet(out Projectile p)
        {
            p = Main.projectile[Slot];
            return p.active && p.identity == Identity && p.owner == Owner && p.type == Type;
        }
    }

    // Ticks since `born` on the fractional clock (never negative).
    private static float Since(ulong born, float fraction) => Math.Max(0, (float)(long)(Main.GameUpdateCount - born) - 1 + fraction);

    // One silk strand per spoke from the hub clearing to the tip, with a trail and a flare on every beat step.
    private sealed class SpokeSource(Ref projectile) : IEbonPixelSource
    {
        public bool Emit(EbonPixelCanvas c)
        {
            if (!projectile.TryGet(out Projectile p) || p.ModProjectile is not EbonWaltzSpoke spoke) return false;
            float age = Math.Max(0, spoke.Age - 1 + c.Fraction), length = spoke.Length(age);
            if (length < 1) return true;
            Vector2 hub = p.GetGlobalProjectile<LastWaltzVisuals>().Center(p), axis = spoke.Angle(age).ToRotationVector2();
            Vector2 start = hub + axis * EbonLastWaltzRules.SpokeInner, tip = hub + axis * length;
            float alpha = Math.Clamp(length / 40f, 0, 1);
            // Beat steps: the turn is fastest on each beat, so the strand flares there.
            float since = age % EbonRewardRules.BeatTicks, flare = EbonVisualsMath.Pulse(since, 5);
            c.Thread(start, tip, EbonTone.Silver, flare > .35f ? 3 : 2, alpha, .35f);
            c.Thread(start + axis * (length * .12f), tip, EbonTone.Ivory, 1, alpha * .85f, .8f);
            c.Dot(tip, EbonTone.Moon, 2, alpha);
            if (!c.Reduced)
            {
                Vector2 back = spoke.Angle(age - 2.5f).ToRotationVector2();
                c.Thread(hub + back * EbonLastWaltzRules.SpokeInner, hub + back * (length * .92f), EbonTone.Rose, 1, alpha * .45f);
                if (since < 6) c.Star(tip, 10 + 8 * flare, since / 6f, spoke.Index * 3 + (int)(age / EbonRewardRules.BeatTicks));
            }
            if (spoke.Index == 0)
            {
                c.Ring(hub, 12, EbonTone.Silver, 1, alpha);
                if (since < 10) c.Ring(hub, 16 + 44 * EbonVisualsMath.OutExpo(since / 10f), EbonTone.Ivory, 1, 1 - since / 10f);
            }
            return true;
        }
    }

    // The silk that holds a piece up: tied at the eyelet, plucked taut at the yank, fading in flight.
    private sealed class FlingThread(Ref projectile) : IEbonPixelSource
    {
        public bool Emit(EbonPixelCanvas c)
        {
            if (!projectile.TryGet(out Projectile p) || p.ModProjectile is not EbonWaltzFling fling
                || !EbonLastWaltzCompanion.TryGetParent(p, out Projectile parent)) return false;
            Vector2 hand = parent.GetGlobalProjectile<LastWaltzVisuals>().Hand(parent);
            Vector2 eyelet = p.GetGlobalProjectile<LastWaltzVisuals>().Eyelet(p, fling);
            float age = Math.Max(0, fling.Age - 1 + c.Fraction), flight = age - EbonLastWaltzRules.Lift;
            float amplitude = flight < 0 ? 1.4f * MathF.Sin(age * .9f) : 9 * EbonVisualsMath.Pulse(flight, 4);
            float alpha = flight < 0 ? Math.Clamp(age / 4f, 0, 1) : 1 - .55f * EbonVisualsMath.Ease(flight / 18);
            c.String(hand, eyelet, EbonTone.Silver, amplitude, age * 1.7f, 1, alpha);
            if (!c.Reduced && flight is >= 0 and < 6) c.Dot(hand, EbonTone.Moon, 2, 1 - flight / 6);
            return true;
        }
    }

    // Build-up before the waltz: a thread to the target, a ring closing on it and six beads
    // converging on the angles where the spokes will open.
    private sealed class GatherSource(Ref parent, ulong born) : IEbonPixelSource
    {
        public bool Emit(EbonPixelCanvas c)
        {
            float t = Since(born, c.Fraction);
            if (t >= EbonLastWaltzRules.Anticipation + 1 || !parent.TryGet(out Projectile p)
                || EbonLastWaltzCompanion.Aimed(p, mustBeHittable: false) is not { } target) return false;
            float k = Math.Clamp(t / EbonLastWaltzRules.Anticipation, 0, 1), ease = EbonVisualsMath.Ease(k);
            Vector2 hand = p.GetGlobalProjectile<LastWaltzVisuals>().Hand(p), at = target.Center;
            c.Thread(hand, at, EbonTone.Silver, 1, .35f + .5f * k, .7f);
            c.Ring(at, 24 + 120 * (1 - ease), EbonTone.Ivory, 1, k);
            if (c.Reduced) return true;
            float phase = EbonLastWaltzRules.SpokePhase(p.identity);
            for (int i = 0; i < EbonRewardRules.Spokes; i++)
                c.Dot(at + (phase + i * MathF.Tau / EbonRewardRules.Spokes).ToRotationVector2() * (30 + 110 * (1 - ease)), EbonTone.Moon, 2, k);
            return true;
        }
    }

    // Analytic debris: lace and silk motes, or a crash of splinters, glass and sparks, with a star and a ring.
    private sealed class BurstSource(Vector2 origin, int seed, int count, float life, float speed, float gravity,
        EbonShardKind kind, EbonShardKind accent, float starRadius, float ringRadius, float heading = 0, float spread = MathF.Tau) : IEbonPixelSource
    {
        private readonly ulong born = Main.GameUpdateCount;
        public bool Emit(EbonPixelCanvas c)
        {
            float t = Since(born, c.Fraction);
            if (t >= life) return false;
            if (count > 0)
            {
                c.Burst(origin, seed, c.Reduced ? (count + 1) / 2 : count, t, life, speed, gravity, kind, spread, heading);
                if (!c.Reduced) c.Burst(origin, seed + 91, (count + 1) / 2, t, life, speed * .65f, gravity, accent, spread, heading);
            }
            if (starRadius > 0 && t < 9) c.Star(origin, starRadius, t / 9f, seed);
            if (ringRadius > 0 && !c.Reduced && t < 14) c.Ring(origin, ringRadius * EbonVisualsMath.OutExpo(t / 14f), EbonTone.Ivory, 1, 1 - t / 14f);
            return true;
        }
    }
}
