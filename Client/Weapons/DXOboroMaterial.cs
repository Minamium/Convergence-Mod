using System;
using Convergence.Client.Graphics;
using Convergence.Content.Items.DXOboro;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Weapons;

internal static class DXOboroMaterial
{
    internal static void Lightning(SpriteBatch batch, float age, int step, float aim, int facing,
        Vector2 grip, double tick, bool reduced)
    {
        if (age < DXOboroMotion.Release(step) || age >= DXOboroMotion.LiveEnd(step)) return;
        float angle = DXOboroMotion.Angle(step, age, aim, facing);
        float speed = MathF.Abs(DXOboroMotion.Offset(step, age + .02f)
            - DXOboroMotion.Offset(step, age - .02f)) / .04f;
        SwordLightning.Draw(batch, grip + angle.ToRotationVector2() * 24,
            grip + angle.ToRotationVector2() * DXOboroMotion.Reach,
            tick, Math.Clamp(speed / .5f, 0, 1), reduced, 11 + step);
    }
    // Two bounded ribbons and a short leading wedge. All use the cut's own
    // fractional angle; no stored draw-history can detach on teleport/cancel.
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[24 * 6 * 2];

    internal static void Draw(SpriteBatch batch, float age, int step, float aim, int facing,
        Vector2 handCenter, Vector2 handAlong, Vector2 handAcross, bool reduced)
    {
        int release = DXOboroMotion.Release(step), liveEnd = DXOboroMotion.LiveEnd(step);
        if (age < release || age >= DXOboroMotion.Duration(step)) return;
        float elapsed = age - release;
        float life = MathF.Min(1f, (DXOboroMotion.Duration(step) - age) / (step == 2 ? 5f : 3f));
        int sections = reduced ? 10 : 20;
        int used = 0;
        Vector2 RootAt(float sampleAge)
        {
            float arm = DXOboroMotion.ArmAngle(step, sampleAge, aim, facing);
            return handCenter + handAlong * MathF.Cos(arm) + handAcross * MathF.Sin(arm);
        }

        // The outer torn wing has real radial depth. Its tail is narrow at the
        // previous angle; the luminous leading edge tapers into the blade tip.
        for (int i = 0; i < sections; i++)
        {
            float u0 = i / (float)sections, u1 = (i + 1f) / sections;
            Wing(u0, 0, out var a, out var b);
            Wing(u1, 0, out var c, out var d);
            Quad(a, b, c, d);
            if (!reduced)
            {
                Wing(u0, 1, out a, out b);
                Wing(u1, 1, out c, out d);
                Quad(a, b, c, d);
            }
        }

        using var scope = new WorldGraphicsScope(batch);
        var device = Main.instance.GraphicsDevice;
        var shader = ShaderManager.GetShader("Convergence.DXOboroVeil");
        shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width,
                device.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("clock", Main.GameUpdateCount / 60f);
        shader.TrySetParameter("reduced", reduced ? 1f : 0f);
        shader.Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, used / 3);

        if (age >= liveEnd) return;
        used = 0;
        Vector2 root = RootAt(age);
        Vector2 axis = DXOboroMotion.Angle(step, age, aim, facing).ToRotationVector2();
        Vector2 normal = new(-axis.Y, axis.X);
        float blade = DXOboroMotion.BladeLength(step, age);
        // Front flare connects the physical tip to the spectral limit. The
        // short tapered wedge makes the present cutting direction unambiguous.
        for (int i = 0; i < 8; i++)
        {
            Flare(i / 8f, out var a, out var b);
            Flare((i + 1f) / 8f, out var c, out var d);
            Quad(a, b, c, d);
        }
        shader.Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, used / 3);

        void Quad(VertexPositionColorTexture a, VertexPositionColorTexture b,
            VertexPositionColorTexture c, VertexPositionColorTexture d)
        {
            vertices[used++] = a; vertices[used++] = b; vertices[used++] = c;
            vertices[used++] = c; vertices[used++] = b; vertices[used++] = d;
        }

        void Wing(float u, int layer, out VertexPositionColorTexture inner,
            out VertexPositionColorTexture outer)
        {
            float history = MathF.Min(elapsed, step == 2 ? 8f : 5f);
            float sampleAge = age - history * (1f - u);
            if (age >= liveEnd)
                sampleAge = liveEnd - history * (1f - u);
            Vector2 axisAt = DXOboroMotion.Angle(step, sampleAge, aim, facing).ToRotationVector2();
            float body = MathF.Pow(MathF.Max(0f, MathF.Sin(MathF.PI * u)), .56f);
            float tear = .72f + .17f * MathF.Sin(u * 31f + step * 4.2f)
                + .11f * MathF.Sin(u * 73f - elapsed * .35f);
            float radius = DXOboroMotion.Reach - (layer == 0 ? 0f : 24f);
            float depth = (layer == 0 ? (step == 2 ? 112f : 91f) : 53f) * body * tear;
            float innerRadius = radius - depth - (layer == 0 ? 4f : 8f);
            float recover = MathHelper.Clamp((age - liveEnd) / 1.8f, 0f, 1f);
            float opacity = life * (1f - .73f * recover * recover * (3f - 2f * recover))
                * (layer == 0 ? .93f : .38f) * MathF.Min(1f, elapsed / 2f);
            float tail = MathHelper.Clamp(u / .14f, 0f, 1f);
            opacity *= tail * tail * (3f - 2f * tail);
            var color = Color.White * opacity;
            Vector2 historicalRoot = RootAt(sampleAge);
            inner = new(new(historicalRoot + axisAt * innerRadius - Main.screenPosition, 0), color, new(u, 0));
            outer = new(new(historicalRoot + axisAt * radius - Main.screenPosition, 0), color, new(u, 1));
        }

        void Flare(float u, out VertexPositionColorTexture lower,
            out VertexPositionColorTexture upper)
        {
            float radius = MathHelper.Lerp(blade - 8f, DXOboroMotion.Reach, u);
            float width = (step == 2 ? 27f : 19f) * MathF.Sin(MathF.PI * u) + 2f;
            Vector2 center = root + axis * radius - Main.screenPosition;
            var color = Color.White * life;
            lower = new(new(center - normal * width, 0), color, new(u, 0));
            upper = new(new(center + normal * width, 0), color, new(u, 1));
        }
    }
}
