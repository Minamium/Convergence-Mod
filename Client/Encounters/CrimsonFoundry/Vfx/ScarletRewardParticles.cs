#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// The reward particles (REWARDS.md#black-blood-material): embers (soft additive sparks), smoke (alpha, broken up by
// noise), bone chips and wax flakes (pixel-dot fragments of the sprites; the only pixel effects allowed).
internal enum ScarletParticleKind : byte { Ember, Smoke, BoneChip, WaxFlake }

// One fixed pool for every reward weapon: at most 200 particles per owner and 600 in total, halved by Reduced
// Effects. Update once per game tick; Draw once per frame in the reward ink layer. Terraria-free, so the offline
// preview draws the same particles; allocates nothing per frame (the 1x1 white texture is created once).
internal sealed class ScarletRewardParticles : IDisposable
{
    private struct Particle
    {
        internal Vector2 Position, Velocity;
        internal float Age, Life, Size, Rotation, Spin, Seed;
        internal ScarletParticleKind Kind;
        internal byte Owner;
        internal bool Local;
    }

    // Smoke darkens what is behind it by the soft texture's intensity (the soft circle carries no alpha of its own).
    private static readonly BlendState Darken = new()
    {
        ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    private readonly Particle[] pool = new Particle[CrimsonRewardRules.MaxParticles];
    private readonly int[] byOwner = new int[256];
    private Texture2D? pixel;
    private int count;

    internal int Count => count;

    // False when a budget is full. owner: player slot (anything else shares one bucket). life in ticks; size in px.
    internal bool Spawn(ScarletParticleKind kind, int owner, bool local, Vector2 at, Vector2 velocity, float life, float size, bool reduced, float seed = 0)
    {
        int bucket = owner is >= 0 and < 255 ? owner : 255;
        if (count >= CrimsonRewardRules.ReducedCount(CrimsonRewardRules.MaxParticles, reduced)
            || byOwner[bucket] >= CrimsonRewardRules.ReducedCount(CrimsonRewardRules.MaxParticlesPerOwner, reduced)
            || !float.IsFinite(at.X + at.Y + velocity.X + velocity.Y + life + size) || life < 1) return false;
        pool[count++] = new Particle
        {
            Position = at, Velocity = velocity, Life = life, Size = Math.Max(1, size), Kind = kind, Owner = (byte)bucket, Local = local,
            Seed = seed, Rotation = seed * 6.2831855f, Spin = (Hash(seed, 3) - .5f) * .5f,
        };
        byOwner[bucket]++;
        return true;
    }

    // One game tick.
    internal void Update()
    {
        for (int i = count - 1; i >= 0; i--)
        {
            ref var p = ref pool[i];
            p.Age++;
            if (p.Age >= p.Life) { Remove(i); continue; }
            switch (p.Kind)
            {
                case ScarletParticleKind.Ember:
                    // Sparks rise on the heat, drift and slow; a little sideways wander from noise, not a period.
                    p.Velocity = p.Velocity * .955f + new Vector2((Hash(p.Seed, (int)p.Age >> 2) - .5f) * .12f, -.035f);
                    break;
                case ScarletParticleKind.Smoke:
                    p.Velocity = p.Velocity * .94f + new Vector2((Hash(p.Seed, (int)p.Age >> 3) - .5f) * .05f, -.025f);
                    break;
                default:
                    // Chips and flakes are bodies: gravity, a little drag, and spin.
                    p.Velocity = new Vector2(p.Velocity.X * .985f, Math.Min(p.Velocity.Y + .25f, 10));
                    p.Rotation += p.Spin;
                    break;
            }
            p.Position += p.Velocity;
        }
    }

    internal void Clear()
    {
        count = 0;
        Array.Clear(byOwner);
    }

    private void Remove(int i)
    {
        byOwner[pool[i].Owner]--;
        pool[i] = pool[--count];
    }

    // Draws through the given SpriteBatch (not begun): smoke, chips and flakes alpha-blended, then embers additive.
    internal void Draw(SpriteBatch batch, in ScarletView view, IScarletAssets assets)
    {
        if (count == 0) return;
        if (pixel is null || pixel.IsDisposed)
        {
            pixel = new Texture2D(view.Device, 1, 1);
            pixel.SetData(new[] { Color.White });
        }
        Texture2D soft = assets.GetTexture("Luminance/BloomCircleSmall");
        Matrix world = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
        float fraction = view.Fraction;
        Vector2 softOrigin = new(soft.Width * .5f, soft.Height * .5f);
        batch.Begin(SpriteSortMode.Deferred, Darken, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        for (int i = 0; i < count; i++)
        {
            ref readonly var p = ref pool[i];
            if (p.Kind != ScarletParticleKind.Smoke) continue;
            float t = (p.Age + fraction) / p.Life, opacity = p.Local ? 1 : CrimsonRewardRules.RemoteLiveOpacity;
            Vector2 at = p.Position + p.Velocity * fraction;
            // Three soft lobes whose offsets and sizes drift with hashed noise: a broken puff, never a clean disc.
            float alpha = .45f * MathF.Sin(MathF.PI * Math.Clamp(t, 0, 1)) * opacity, grow = 1 + 1.6f * t;
            for (int lobe = 0; lobe < 3; lobe++)
            {
                float h = Hash(p.Seed, lobe + 11), a = h * 6.2831855f + t * (h - .5f) * 3;
                Vector2 offset = new Vector2(MathF.Cos(a), MathF.Sin(a)) * p.Size * .35f * grow;
                float scale = p.Size * grow * (.55f + .45f * Hash(p.Seed, lobe + 23)) / soft.Width * 2;
                batch.Draw(soft, at + offset, null, Color.White * (alpha * (.6f + .4f * h)), 0, softOrigin, scale, SpriteEffects.None, 0);
            }
        }
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        for (int i = 0; i < count; i++)
        {
            ref readonly var p = ref pool[i];
            if (p.Kind is ScarletParticleKind.Ember or ScarletParticleKind.Smoke) continue;
            float t = (p.Age + fraction) / p.Life, opacity = p.Local ? 1 : CrimsonRewardRules.RemoteLiveOpacity;
            Vector2 at = p.Position + p.Velocity * fraction;
            // A crisp fragment of the sprite at the 2 px dot.
            Color tint = p.Kind == ScarletParticleKind.BoneChip ? new Color(232, 222, 196) : new Color(170, 18, 28);
            float alpha = (1 - Math.Clamp((t - .7f) / .3f, 0, 1)) * opacity;
            batch.Draw(pixel, at, null, tint * alpha, p.Rotation, new Vector2(.5f), new Vector2(p.Size, p.Size * (p.Kind == ScarletParticleKind.WaxFlake ? .6f : 1)), SpriteEffects.None, 0);
        }
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        for (int i = 0; i < count; i++)
        {
            ref readonly var p = ref pool[i];
            if (p.Kind != ScarletParticleKind.Ember) continue;
            float t = (p.Age + fraction) / p.Life, opacity = p.Local ? 1 : CrimsonRewardRules.RemoteLiveOpacity;
            float flicker = .7f + .3f * Hash(p.Seed, (int)p.Age);
            float heat = 1 - t;
            Color color = new Color(1f, .3f + .35f * heat, .08f + .1f * heat) * (heat * flicker * opacity);
            Vector2 at = p.Position + p.Velocity * fraction;
            batch.Draw(soft, at, null, color, 0, softOrigin, p.Size * (1 - .5f * t) / soft.Width * 2, SpriteEffects.None, 0);
            batch.Draw(soft, at, null, new Color(1f, .85f, .6f) * (heat * flicker * opacity * .6f), 0, softOrigin, p.Size * .35f / soft.Width * 2, SpriteEffects.None, 0);
        }
        batch.End();
    }

    internal static float Hash(float seed, int salt)
    {
        uint h = unchecked((uint)BitConverter.SingleToInt32Bits(seed) * 0x9E3779B1u ^ (uint)salt * 0x85EBCA77u);
        h ^= h >> 15; h = unchecked(h * 0x2C1B3C6Du); h ^= h >> 12; h = unchecked(h * 0x297A2D39u); h ^= h >> 15;
        return (h & 0xFFFFFF) / 16777216f;
    }

    public void Dispose()
    {
        pixel?.Dispose();
        pixel = null;
        Clear();
    }
}
