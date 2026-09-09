#nullable enable
using System;
using System.Diagnostics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// One clock for all apparatus, hands, traces and impact tails. No per-projectile
// stopwatch phase skew. Render interpolation never mutates a gameplay timer.
[Autoload(Side = ModSide.Client)]
public sealed class RitualRenderClock : ModSystem
{
    private static long stamp;
    internal static float Fraction => Main.gamePaused || stamp == 0 ? 0 : (float)Math.Clamp(
        (Stopwatch.GetTimestamp() - stamp) * 60d / Stopwatch.Frequency, 0, 1);
    internal static float Time => (float)(Main.GameUpdateCount % 36000) + Fraction;
    internal static float Sample(float current) => Math.Max(0, current - 1 + Fraction);
    public override void PostUpdateEverything() => stamp = Stopwatch.GetTimestamp();
    public override void OnWorldUnload() => stamp = 0;
    public override void Unload() => stamp = 0;
}
