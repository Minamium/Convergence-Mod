using System;
using Microsoft.Xna.Framework;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// CONTRACT for the Ebon reward pixel layer (Soboro's half-resolution technique with the Ebon palette).
// Weapon presentation code depends only on this surface. Implementation notes for the layer:
//  - Client only; never touched on a dedicated server. Graphics resources are created and released on
//    the main thread; Mod unload disposes them; world unload drops every source.
//  - Each frame, before the world is drawn (after the camera is final), the layer renders all live
//    sources into a screen-aligned half-resolution target (1 dot = 2 world px), then composites it
//    once after projectiles with a one-dot navy outline and a soft glow, honouring zoom.
//  - Bounded: at most MaxSources live sources and fixed per-frame primitive budgets; no per-frame
//    allocation. A rendering exception disables the layer for the session with one warning.

internal enum EbonTone : byte { Outline, Charcoal, Rose, Silver, Ivory, Moon, Gold, Candle }
internal enum EbonBandMode : byte { Chalk, Tear }
internal enum EbonShardKind : byte { Glass, Wood, Silk, Spark, Lace }

// A retained presentation source. The layer calls Emit once per rendered frame; a source reads its
// own latest replicated/owner state plus canvas.Fraction (WeaponDrawClock.Fraction) and records
// primitives. Return false when finished and the layer drops it.
internal interface IEbonPixelSource
{
    bool Emit(EbonPixelCanvas canvas);
}

internal static partial class EbonPixelLayer
{
    internal const int MaxSources = 256;

    // Register a source (client only). Returns false when the layer is disabled or full.
    internal static bool Add(IEbonPixelSource source) => AddCore(source);

    static partial void AddSource(IEbonPixelSource source, ref bool accepted);
    private static bool AddCore(IEbonPixelSource source)
    {
        bool accepted = false;
        AddSource(source, ref accepted);
        return accepted;
    }
}

// Primitive recorder for one frame. World coordinates in pixels; angles in radians.
internal sealed partial class EbonPixelCanvas
{
    // WeaponDrawClock.Fraction for this frame (0..1 between game ticks).
    internal float Fraction { get; set; } = 1f;
    // Ebon Reduced Effects: sources should emit fewer secondary primitives when true.
    internal bool Reduced { get; set; }

    // A slash sweep: the annular sector around `root` swept from angle a0 to a1 (the sweep direction is
    // a0 -> a1), between innerRadius and outerRadius. fade 0..1 dissolves it from the tail; heat 0..1
    // warms the edge from ivory/silver toward dusty rose.
    internal void Crescent(Vector2 root, float a0, float a1, float innerRadius, float outerRadius, float fade, float heat, int seed)
        => CrescentCore(root, a0, a1, innerRadius, outerRadius, fade, heat, seed);

    // A straight thread or strand, thickness in dots (1-3). shimmer 0..1 adds travelling highlights.
    internal void Thread(Vector2 a, Vector2 b, EbonTone tone, int thickness = 1, float alpha = 1, float shimmer = 0)
        => ThreadCore(a, b, tone, thickness, alpha, shimmer);

    // A vibrating string between fixed ends a and b: one standing half-wave of `amplitude` px at `phase`.
    internal void String(Vector2 a, Vector2 b, EbonTone tone, float amplitude, float phase, int thickness = 1, float alpha = 1)
        => StringCore(a, b, tone, amplitude, phase, thickness, alpha);

    // A band along a -> b, `width` px wide. Chalk: a solid tailor's-chalk forecast stroke (never dashed),
    // drawn up to `progress` (0..1) of its length. Tear: a live rip with jagged torn edges up to `progress`.
    internal void Band(Vector2 a, Vector2 b, float width, EbonBandMode mode, float progress, float alpha, int seed)
        => BandCore(a, b, width, mode, progress, alpha, seed);

    internal void Dot(Vector2 at, EbonTone tone, int size = 1, float alpha = 1) => DotCore(at, tone, size, alpha);

    internal void Ring(Vector2 center, float radius, EbonTone tone, int thickness = 1, float alpha = 1)
        => RingCore(center, radius, tone, thickness, alpha);

    // An impact star (4-6 rays) of `radius` px, age01 0..1 over its short life.
    internal void Star(Vector2 at, float radius, float age01, int seed) => StarCore(at, radius, age01, seed);

    // Deterministic analytic debris (no retained particles): `count` pieces born at `origin`, evaluated at
    // `t` ticks of `life`, initial `speed` px/tick spread over `spread` radians around `heading`, `gravity` px/tick^2.
    internal void Burst(Vector2 origin, int seed, int count, float t, float life, float speed, float gravity, EbonShardKind kind, float spread = MathF.Tau, float heading = 0)
        => BurstCore(origin, seed, count, t, life, speed, gravity, kind, spread, heading);

    // A small stitched cross (a tailor's mark), `size` in dots.
    internal void Stitch(Vector2 at, int size, EbonTone tone, float alpha = 1) => StitchCore(at, size, tone, alpha);

    partial void CrescentCore(Vector2 root, float a0, float a1, float innerRadius, float outerRadius, float fade, float heat, int seed);
    partial void ThreadCore(Vector2 a, Vector2 b, EbonTone tone, int thickness, float alpha, float shimmer);
    partial void StringCore(Vector2 a, Vector2 b, EbonTone tone, float amplitude, float phase, int thickness, float alpha);
    partial void BandCore(Vector2 a, Vector2 b, float width, EbonBandMode mode, float progress, float alpha, int seed);
    partial void DotCore(Vector2 at, EbonTone tone, int size, float alpha);
    partial void RingCore(Vector2 center, float radius, EbonTone tone, int thickness, float alpha);
    partial void StarCore(Vector2 at, float radius, float age01, int seed);
    partial void BurstCore(Vector2 origin, int seed, int count, float t, float life, float speed, float gravity, EbonShardKind kind, float spread, float heading);
    partial void StitchCore(Vector2 at, int size, EbonTone tone, float alpha);
}
