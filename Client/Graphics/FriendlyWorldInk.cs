#nullable enable
using System;

namespace Convergence.Client.Graphics;

// Friendly world ink (weapon effects painted on the world, such as the Scarlet rewards' black blood; the weapons work
// in every Raid) always lies beneath every Raid's forecasts. tML runs ModSystem.PostDrawTiles in load order, which
// follows type names, so a forecast drawer that sorts before the ink's own system would be painted over: every Raid
// drawer that paints forecasts in PostDrawTiles calls BeneathForecasts() before it begins drawing. The ink sets Draw;
// its draw is stamped per frame and idempotent, so whichever caller runs first draws it. Client only.
internal static class FriendlyWorldInk
{
    internal static Action? Draw;
    internal static void BeneathForecasts() => Draw?.Invoke();
}
