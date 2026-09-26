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
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[18 * 6];

    internal static void Draw(SpriteBatch batch, int age, int step, float aim, int facing, Vector2 root, bool reduced)
    {
        if (age < DXOboroMotion.ReleaseFrame || age > DXOboroMotion.Duration) return;
        float tail = MathF.Min(age - DXOboroMotion.ReleaseFrame, reduced ? 4f : 8f);
        if (tail <= 0) return;
        int sections = reduced ? 8 : 16;
        int used = 0;
        for (int i = 0; i < sections; i++)
        {
            float u0 = i / (float)sections, u1 = (i + 1f) / sections;
            Edge(u0, out var a, out var b);
            Edge(u1, out var c, out var d);
            vertices[used++] = a; vertices[used++] = b; vertices[used++] = c;
            vertices[used++] = c; vertices[used++] = b; vertices[used++] = d;
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

        if (!DXOboroMotion.Live(age)) return;
        // The narrow live spine grows directly from the physical blade into the
        // moon rim. It is sampled from the same angle and never exceeds Reach.
        Vector2 axis = DXOboroMotion.Angle(step, age, aim, facing).ToRotationVector2();
        Vector2 normal = new(-axis.Y, axis.X);
        used = 0;
        const int spineSections = 8;
        for (int i = 0; i < spineSections; i++)
        {
            Spine(i / (float)spineSections, out var a, out var b);
            Spine((i + 1f) / spineSections, out var c, out var d);
            vertices[used++] = a; vertices[used++] = b; vertices[used++] = c;
            vertices[used++] = c; vertices[used++] = b; vertices[used++] = d;
        }
        shader.Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, used / 3);

        void Edge(float u, out VertexPositionColorTexture inner, out VertexPositionColorTexture outer)
        {
            float sampleAge = age - tail * (1 - u);
            float angle = DXOboroMotion.Angle(step, sampleAge, aim, facing);
            Vector2 direction = angle.ToRotationVector2();
            float fade = MathF.Min(1f, (DXOboroMotion.Duration - age) / 7f) *
                MathF.Pow(u, .72f);
            float envelope = MathF.Pow(MathF.Max(0f, MathF.Sin(u * MathF.PI)), .45f);
            float depth = 4f + (52f + 26f * MathF.Sin(u * MathF.PI)) * envelope;
            var color = Color.White * fade;
            inner = new(new(root + direction * (DXOboroMotion.Reach - depth) - Main.screenPosition, 0),
                color, new(u, 0));
            outer = new(new(root + direction * DXOboroMotion.Reach - Main.screenPosition, 0),
                color, new(u, 1));
        }

        void Spine(float u, out VertexPositionColorTexture lower, out VertexPositionColorTexture upper)
        {
            float radius = MathHelper.Lerp(112f, DXOboroMotion.Reach, u);
            float width = 3f + 9f * MathF.Sin(u * MathF.PI);
            float fade = 1f - .4f * (age - DXOboroMotion.ReleaseFrame) /
                (DXOboroMotion.LiveEndFrame - DXOboroMotion.ReleaseFrame);
            Vector2 center = root + axis * radius - Main.screenPosition;
            var color = Color.White * fade;
            lower = new(new(center - normal * width, 0), color, new(u, 0));
            upper = new(new(center + normal * width, 0), color, new(u, 1));
        }
    }
}
