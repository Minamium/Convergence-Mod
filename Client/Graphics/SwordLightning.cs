using System;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Graphics;

// Original, bounded discharge geometry. The caller supplies the actual blade
// anchors and accepted draw time; repeated draws cannot spawn or advance it.
internal static class SwordLightning
{
    private const int Segments = 32;
    private static readonly Vector2[] path = new Vector2[Segments + 1];
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[Segments * 6 * 8];
    private static int used;

    internal static void Draw(SpriteBatch batch, Vector2 start, Vector2 end,
        double drawTick, float power, bool reduced, float seed)
    {
        power = Math.Clamp(power, 0, 1);
        float length = Vector2.Distance(start, end);
        if (power <= .005f || length < 8 || !float.IsFinite(length)) return;
        Vector2 axis = (end - start) / length, normal = new(-axis.Y, axis.X);
        float time = (float)(drawTick % 36000) / 4.5f;
        int epoch = (int)MathF.Floor(time);
        used = 0;
        // Cross-faded successive strokes: coherent forks during a discharge,
        // not a different random polyline each displayed frame.
        for (int echo = 0; echo < (reduced ? 1 : 2); echo++)
        {
            float age = time - epoch + echo;
            float light = Math.Clamp(age / .11f, 0, 1)
                * MathF.Pow(Math.Clamp(1 - age / 1.55f, 0, 1), 1.5f) * power;
            if (light < .01f) continue;
            float key = seed * 19.7f + (epoch - echo) * 3.17f;
            float amplitude = Math.Clamp(length * .042f, 7, 24);
            for (int i = 0; i <= Segments; i++)
            {
                float u = i / (float)Segments;
                float envelope = MathF.Sin(MathF.PI * u);
                float jag = Noise(i, key) * .7f + Noise(i / 3, key + 13) * .7f;
                path[i] = start + axis * (u * length) + normal * (jag * amplitude * envelope);
            }
            float width = (reduced ? 6 : 10) * (.6f + .4f * power);
            Ribbon(Segments, width, light, age, false);
            int branches = reduced ? 1 : 3;
            // Save branch attachment points before reusing the bounded buffer.
            Vector2 b0 = path[9], b1 = path[17], b2 = path[25];
            for (int b = 0; b < branches; b++)
            {
                Vector2 origin = b == 0 ? b0 : b == 1 ? b1 : b2;
                float side = Noise(b + 71, key) < 0 ? -1 : 1;
                Vector2 fork = axis * (.14f + .055f * b) * length
                    + normal * side * amplitude * (2.5f + .5f * b);
                for (int i = 0; i <= 10; i++)
                {
                    float u = i / 10f;
                    path[i] = origin + fork * u + normal * Noise(i + b * 17, key + 31)
                        * amplitude * .4f * MathF.Sin(u * MathF.PI);
                }
                Ribbon(10, width * .6f, light * .76f, age + b * .035f, true);
            }
        }
        if (used == 0) return;
        using var scope = new WorldGraphicsScope(batch);
        var device = Main.instance.GraphicsDevice;
        var material = ShaderManager.GetShader("Convergence.SwordLightning");
        material.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        material.Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, used / 3);
    }

    private static void Ribbon(int count, float width, float opacity, float age, bool fork)
    {
        for (int i = 0; i < count; i++)
        {
            Edge(i, out var a, out var b); Edge(i + 1, out var c, out var d);
            vertices[used++] = a; vertices[used++] = b; vertices[used++] = c;
            vertices[used++] = c; vertices[used++] = b; vertices[used++] = d;
        }
        void Edge(int i, out VertexPositionColorTexture a, out VertexPositionColorTexture b)
        {
            float u = i / (float)count;
            Vector2 tangent = path[Math.Min(count, i + 1)] - path[Math.Max(0, i - 1)];
            float len = MathF.Max(.001f, tangent.Length());
            Vector2 normal = new(-tangent.Y / len, tangent.X / len);
            float taper = fork ? 1 - u * .9f : .4f + .6f * MathF.Sin(MathF.PI * u);
            float head = Math.Clamp((age * 8 - u) * 5, 0, 1);
            var color = Color.White * (opacity * head * taper);
            Vector2 p = path[i] - Main.screenPosition;
            a = new(new(p - normal * width * taper, 0), color, new(u, 0));
            b = new(new(p + normal * width * taper, 0), color, new(u, 1));
        }
    }

    private static float Noise(int i, float seed)
    {
        float n = MathF.Sin(i * 127.1f + seed * 31.73f) * 43758.5453f;
        return (n - MathF.Floor(n)) * 2 - 1;
    }
}
