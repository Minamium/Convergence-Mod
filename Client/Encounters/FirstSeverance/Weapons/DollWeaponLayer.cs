#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// CONTRACT for the shared Doll weapon layer. Every refreshed reward weapon (claws, Pale Meridian,
// Lacuna Testament, Choir of the Unmade, Last Witness) draws only through this surface: no per-weapon
// hooks, render targets, DrawBehind/overPlayers or private device-state code. Notes for the layer:
//  - Client only; never touched on a dedicated server. Graphics resources are created and released on
//    the main thread; Mod unload disposes them; world unload drops every source.
//  - Each frame, once the camera is final, sources record into the canvas. Front pixel sprites render
//    into a half-resolution Art target and light (lines, rings, forecasts, debris, energy materials)
//    into a half-resolution Light target, both on a world-aligned grid (1 dot = 2 world px). After the
//    players are drawn the layer composites Art, then Light (premultiplied, so a dark void occludes),
//    with a one-dot ink outline around light and a bounded glow on tones at or above lilac. Back
//    sprites (the organ and its gallery only) draw directly after projectiles, behind the players.
//  - Bounded: at most MaxSources live sources, fixed command arrays and no per-frame allocation; over
//    budget, commands are dropped and counted, never grown. A throwing source is dropped with one
//    warning; a rendering failure degrades the layer to direct point-sampled sprite draws (no light).
// Nothing here touches hits, input, packets or saved state.

// Where a sprite draws: Front is in front of every player (held bodies, projectiles, forecasts,
// damaging light, residue); Back is behind the players (only the Choir organ and its gallery row).
internal enum DollStratum : byte { Front, Back }

// The Doll palette: ink, black iron, porcelain, pearl, dull brass, the light ramp
// (Plum -> PlumLight -> Violet -> Lilac -> PearlViolet -> Bone -> White) and a ruby accent.
// Must match DollPixelArt.Palette and the palette block of every Doll*.fx.
internal enum DollTone : byte
{
    Ink, IronDark, Iron, IronLight, PorcelainShade, Porcelain, PorcelainLight, Bone, PearlGrey, Pearl,
    BrassShade, Brass, BrassLight, Plum, PlumLight, Violet, Lilac, PearlViolet, White, Ruby,
}

internal enum DollShardKind : byte { Porcelain, Brass, Pearl, Spark }

// A pixel sprite: a source rectangle of an already loaded texture (read through Asset.Value on the
// frame it draws, never cached from an AsyncLoad request) and its pivot in texel coordinates
// (top-left origin, texel i spans [i, i + 1)). One texel always draws as one dot (2 world px).
internal readonly record struct DollSprite(Texture2D Texture, Rectangle Source, Vector2 PivotTexel);

// Per-sprite effects; default is none. Ink texels (the art's outline) survive flash and silhouette.
internal readonly record struct DollSpriteFx
{
    private readonly byte silhouette;

    // 0..1: steps the body toward pearl and white in palette steps.
    public float Flash { get; init; }
    // 0..1 with Seed: porcelain-crumble removal of texels, shaded at the crumbling edge.
    public float Dissolve { get; init; }
    public int Seed { get; init; }
    // 0..1 of the rows hidden, from the top (or the bottom); a rising reveal animates it toward 0.
    public float Hide { get; init; }
    public bool HideFromBottom { get; init; }
    // 0..1, applied in four dithered steps locked to the texels.
    public float Fade { get; init; }
    // One tone for every non-ink texel, or null.
    public DollTone? Silhouette
    {
        get => silhouette == 0 ? null : (DollTone)(silhouette - 1);
        init => silhouette = value is { } tone ? (byte)(tone + 1) : (byte)0;
    }
}

// A retained presentation source. The layer calls Emit once per rendered frame; a source reads its own
// latest replicated/owner state plus canvas.Fraction (WeaponDrawClock.Fraction) and records commands.
// Return false when finished and the layer drops it.
internal interface IDollWeaponSource
{
    bool Emit(DollWeaponCanvas canvas);
}

// A weapon material for canvas.Energy batches: binds its effect for `pass` while the Light target is
// bound (premultiplied AlphaBlend, dot-space projection in the context). Return false to skip the batch.
internal interface IDollEnergyMaterial
{
    bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass);
}

// Projection: dot space -> Light target. DotOrigin: absolute world dot of target cell (0, 0), for
// world-stable noise and dither. Pixel: the shared DollPixel effect (the built-in RampPass lives there).
internal readonly record struct DollEnergyContext(Matrix Projection, Vector2 DotOrigin, double Clock, float Fraction,
    bool Reduced, Effect Pixel);

internal static partial class DollWeaponLayer
{
    internal const int MaxSources = 192;

    // Register a source (client only). Re-adding a registered source is accepted once. Returns false on a
    // dedicated server, while the layer is disabled, or when MaxSources sources are live (counted).
    internal static bool Add(IDollWeaponSource source) => AddCore(source);

    static partial void AddSource(IDollWeaponSource source, ref bool accepted);

    private static bool AddCore(IDollWeaponSource source)
    {
        bool accepted = false;
        AddSource(source, ref accepted);
        return accepted;
    }
}
