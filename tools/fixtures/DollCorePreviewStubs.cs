// Only the host graphics surface/unused projection types are stubbed. The mesh,
// rupture envelopes, side-crater geometry and .fxc are production sources.
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria
{
    internal static class Main
    {
        internal static bool dedServ = false;
        internal static Vector2 screenPosition = Vector2.Zero;
        internal static Host instance = new();
        internal static Camera GameViewMatrix = new();
        internal static RasterizerState Rasterizer => RasterizerState.CullNone;
        internal static void QueueMainThreadAction(Action action) => action();
        internal sealed class Host { internal GraphicsDevice GraphicsDevice = null!; }
        internal sealed class Camera { internal Matrix TransformationMatrix = Matrix.Identity; }
    }
    internal static class VectorExtensions
    {
        internal static Vector2 SafeNormalize(this Vector2 v, Vector2 fallback)
            => v.LengthSquared() < .00001f ? fallback : Vector2.Normalize(v);
    }
}

namespace Convergence.Content.Encounters.FirstSeverance
{
    // At(phase, action, cycles, age) is checked against the real score by domain
    // tests. GPU preview constructs the already-projected (Struck, Age) envelope.
    internal enum FirstSeveranceBossPhase { Sealed, Unbound, Distant, Final }
    internal enum FirstSeveranceSubstate { None, RemoteCrush }
    internal static class FirstSeveranceScoreGeometry { internal const int CrushImpactTick = 162; }
}
