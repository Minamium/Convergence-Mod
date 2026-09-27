using System;
using System.Diagnostics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

// Draw between two accepted simulation samples, never predict the next hit.
// Render frequency changes visual sampling, not attack speed or packet cadence.
[Autoload(Side = ModSide.Client)]
internal sealed class WeaponDrawClock : ModSystem
{
    private static long stamp;
    private static float drawFraction = 1f;
    internal static float Fraction => Main.gamePaused ? 1f : drawFraction;

    public override void PostUpdateEverything()
    {
        if (!Main.gamePaused) stamp = Stopwatch.GetTimestamp();
        drawFraction = 1f;
    }
    // Installed tML calls this once at the start of DoDraw, before player and
    // over-player projectile layers. Capture a shared time, without moving the
    // camera: evaluating wall time in each layer would detach the grip again.
    public override void ModifyScreenPosition()
        => drawFraction = Main.gamePaused || stamp == 0 ? 1f
            : (float)Math.Clamp((Stopwatch.GetTimestamp() - stamp) * 60d / Stopwatch.Frequency, 0d, 1d);
    public override void OnWorldUnload() { stamp = 0; drawFraction = 1; }
    public override void Unload() { stamp = 0; drawFraction = 1; }
}
