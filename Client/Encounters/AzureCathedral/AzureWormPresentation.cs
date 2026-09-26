using System;
using Convergence.Content.Encounters.AzureCathedral;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.AzureCathedral;

// Material-only geometry attached to the accepted native chain. No particle
// lifetime, gameplay RNG, extra actor, hitbox or second movement simulation.
internal static class AzureWormPresentation
{
    private const int Steps = 20;
    private static readonly VertexPositionColorTexture[] ribbon = new VertexPositionColorTexture[Steps * 6];

    // All spawned joints share the same reveal. The old index*6 delay left
    // most of the already-moving chain invisible for 264 additional ticks.
    internal static float Presence(float age, int musicStart) => musicStart < 0 ? 0
        : AzureRules.Ease((age - musicStart - AzureRules.WormArrival) / 24);

    internal static float ArrivalLight(float age, int musicStart, int index)
    {
        float distance = (age - musicStart - AzureRules.WormArrival) - index * 4;
        return MathF.Exp(-distance * distance / 1500);
    }

    internal static void Wakes(ManagedShader shader, AzureWorm[] parts, AzureBoss girl,
        float age, Vector2 screen, float presence, bool silhouette)
    {
        if (Main.dedServ || silhouette || presence < .001f) return;
        bool reduced = AzureVisuals.Reduced, melting = girl.State.Phase == AzurePhase.Melting;
        // Two attached frost fins per three joints (six in Reduced Effects).
        // Body illumination remains independently readable in the reduced path.
        int stride = reduced ? 6 : 3;
        for (int i = 0; i <= AzureRules.Segments; i += stride)
        {
            var part = parts[i];
            if (part is null) continue;
            float melt = melting ? AzureRules.Melt(age - girl.State.EndAt, i) : 0;
            float alpha = presence * (1 - melt) * (reduced ? .27f : .48f);
            if (alpha < .001f) continue;
            Vector2 axis = part.NPC.rotation.ToRotationVector2(), normal = new(-axis.Y, axis.X);
            Vector2 center = part.NPC.Center - screen + new Vector2(0, melt * melt * (60 + i * 2));
            Vector2 behind = parts[Math.Min(AzureRules.Segments, i + 2)] is { } tail
                ? tail.NPC.Center - screen : center - axis * 150;
            // Replication can briefly leave the next native joint far away.
            // Clamp only this decorative attachment, never native positions.
            Vector2 drift = behind - center;
            if (drift.LengthSquared() > 320 * 320) behind = center + Vector2.Normalize(drift) * 320;
            float charge = Math.Clamp(parts[0]?.NPC.ai[2] ?? 0, 0, 1);
            float arrival = ArrivalLight(age, girl.State.MusicStart, i);
            shader.TrySetParameter("signal", new Vector4(alpha, charge + arrival * .6f,
                reduced ? 1 : 0, i));
            for (int side = -1; side <= 1; side += 2)
            {
                float lag = MathF.Sin(age * .033f - i * .36f) * (reduced ? 2 : 10);
                Vector2 root = center + axis * (i == 0 ? 18 : 5) + normal * (side * (i == 0 ? 63 : 44));
                Vector2 shoulder = root - axis * 32 + normal * (side * (26 + charge * 12));
                Vector2 bend = behind + normal * (side * (65 + lag));
                Vector2 tip = behind - axis * (52 + charge * 32) + normal * (side * (26 + lag));
                float width = (i == 0 ? 30 : 23) * (1 + charge * .3f);
                DrawRibbon(shader, root, shoulder, bend, tip, width, age, i, side, reduced);
            }
        }
    }

    private static void DrawRibbon(ManagedShader shader, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
        float width, float age, int index, int side, bool reduced)
    {
        Vector2 Point(float t)
        {
            float v = 1 - t;
            Vector2 p = a * (v * v * v) + b * (3 * v * v * t) + c * (3 * v * t * t) + d * (t * t * t);
            Vector2 wind = new(-MathF.Sin(index * .13f), MathF.Cos(index * .13f));
            return p + wind * (MathF.Sin(t * 9 - age * .09f + index * .7f) * t * t * (reduced ? 1 : 6) * side);
        }
        Vector2 Edge(float t, float sign)
        {
            Vector2 tangent = Point(Math.Min(1, t + .008f)) - Point(Math.Max(0, t - .008f));
            if (tangent.LengthSquared() < .001f) tangent = Vector2.UnitX;
            tangent.Normalize();
            return Point(t) + new Vector2(-tangent.Y, tangent.X) * (sign * width * (1 - t) * (.55f + .65f * MathF.Sin(t * MathHelper.Pi)));
        }
        for (int n = 0; n < Steps; n++)
        {
            float t = n / (float)Steps, u = (n + 1f) / Steps;
            int k = n * 6;
            ribbon[k] = new(new(Edge(t, -1), 0), Color.White, new(t, 0));
            ribbon[k + 1] = new(new(Edge(t, 1), 0), Color.White, new(t, 1));
            ribbon[k + 2] = new(new(Edge(u, -1), 0), Color.White, new(u, 0));
            ribbon[k + 3] = ribbon[k + 2]; ribbon[k + 4] = ribbon[k + 1];
            ribbon[k + 5] = new(new(Edge(u, 1), 0), Color.White, new(u, 1));
        }
        shader.Apply("WormRibbonPass");
        Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, ribbon, 0, Steps * 2);
    }
}
