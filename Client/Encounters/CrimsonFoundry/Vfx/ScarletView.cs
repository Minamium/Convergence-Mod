#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// Everything a Scarlet effect needs to know about one frame, with no Terraria,
// tModLoader or Luminance types. Production fills it once per frame from
// Main.* (screenPosition, GameViewMatrix, the real device); the offline preview
// fills it from a hidden FNA device. Positions are world pixels, times are ticks.
internal readonly record struct ScarletView(
    GraphicsDevice Device, Vector2 ScreenPosition, Matrix GameView, float Zoom,
    int Width, int Height, int Tick, float Fraction, bool Reduced)
{
    // Fractional action clock in ticks; shaders take Clock / 60 seconds.
    internal float Clock => Tick + Fraction;

    // The existing shader convention: Reduced Effects drops the detail term to a quarter.
    internal float Detail => Reduced ? .25f : 1f;

    internal Matrix Projection => Matrix.CreateOrthographicOffCenter(0, Width, Height, 0, -1, 1);

    // (world - ScreenPosition) -> clip space; the zoom lives in GameView exactly like
    // Main.GameViewMatrix.TransformationMatrix * ortho(0, W, H, 0) in CrimsonEnergy.
    internal Matrix ScreenClip => GameView * Projection;

    // World pixels straight to clip space (what a world-space mesh needs as its WVP).
    internal Matrix WorldClip => Matrix.CreateTranslation(-ScreenPosition.X, -ScreenPosition.Y, 0) * ScreenClip;

    // World position -> pixel on the render target (zoom applied).
    internal Vector2 ToTarget(Vector2 world) => Vector2.Transform(world - ScreenPosition, GameView);

    // Same shape as Terraria's zoom: scale about the centre of the screen.
    internal static Matrix ZoomMatrix(float zoom, int width, int height)
        => Matrix.CreateTranslation(-width * .5f, -height * .5f, 0) * Matrix.CreateScale(zoom, zoom, 1)
            * Matrix.CreateTranslation(width * .5f, height * .5f, 0);

    internal static ScarletView Create(GraphicsDevice device, int width, int height, Vector2 screenPosition,
        float zoom, int tick, float fraction = 0, bool reduced = false)
        => new(device, screenPosition, ZoomMatrix(zoom, width, height), zoom, width, height, tick, fraction, reduced);

    internal ScarletView At(int tick, float fraction = 0) => this with { Tick = tick, Fraction = fraction };
}
