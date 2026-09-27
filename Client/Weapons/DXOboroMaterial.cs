using System;
using Convergence.Client.Graphics;
using Convergence.Content.Items.DXOboro;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Weapons;

// Original authored sweep surfaces, not a uniformly rotating circular fan.
// Shape, torn edges and discharge all use the accepted cut's fractional clock.
// This class owns no hitboxes, particles, render targets or persistent history.
internal static class DXOboroMaterial
{
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6144];
    private static int used;
    private static readonly float[][] reach = {
        new[] { .62f, .76f, .94f, .99f, .98f, 1f, 1f },
        new[] { .72f, .80f, .87f, .91f, .95f, 1f, 1f },
        new[] { .51f, .66f, .94f, .98f, .89f, 1f, 1f }
    };
    private static readonly float[][] depth = {
        new[] { .01f, .24f, .76f, 1f, .81f, .54f, .015f },
        new[] { .01f, .30f, .54f, .64f, 1f, .72f, .015f },
        new[] { .01f, .24f, .90f, .84f, 1f, .68f, .015f }
    };
    private static readonly float[] fractureRadius = { .73f, .98f, .74f, .96f, .64f, .92f, .77f, .99f, .82f };

    internal static void Draw(SpriteBatch batch, float age, int step, float aim, int facing,
        Vector2 handCenter, Vector2 handAlong, Vector2 handAcross, bool reduced)
    {
        int release = DXOboroMotion.Release(step), end = DXOboroMotion.LiveEnd(step);
        // A real quiet gap before the next cut, not a fan throughout recovery.
        if (age <= release || age >= end + 1.8f) return;
        float elapsed = age - release;
        float progress = Math.Clamp(elapsed / (end - release), 0, 1);
        float recovery = Smooth((age - end) / 1.8f);
        float enter = Smooth(elapsed / .8f);
        float brightness = enter * (1f - recovery);
        float sampleHead = Math.Min(age, end);
        float memory = Math.Min(elapsed, step == 2 ? 6.2f : step == 1 ? 3.9f : 4.6f);
        int sections = reduced ? 24 : 48;
        used = 0;

        Vector2 Root(float t)
        {
            float a = DXOboroMotion.ArmAngle(step, t, aim, facing);
            return handCenter + handAlong * MathF.Cos(a) + handAcross * MathF.Sin(a);
        }

        // Downstroke: hooked wing. Reverse: compressed scimitar. Finisher:
        // split broad shoulder. Depth really changes: pressure -> open -> tear.
        float pressure = .44f + .56f * MathF.Sin(MathF.PI * MathF.Pow(progress, .76f));
        float width = (step == 2 ? 139f : step == 1 ? 78f : 106f) * pressure;
        for (int lane = 0; lane < (reduced ? 1 : 3); lane++)
        for (int i = 0; i < sections; i++)
        {
            Surface(i / (float)sections, lane, out var a, out var b);
            Surface((i + 1f) / sections, lane, out var c, out var d);
            Quad(a, b, c, d);
        }
        using var scope = new WorldGraphicsScope(batch);
        Apply(0, age, step, progress, recovery, reduced);
        Flush();

        void Surface(float u, int lane, out VertexPositionColorTexture inner,
            out VertexPositionColorTexture outer)
        {
            float t = sampleHead - memory * (1f - u);
            float axis = DXOboroMotion.Angle(step, t, aim, facing);
            Vector2 radial = axis.ToRotationVector2();
            Vector2 tangent = new(-radial.Y * facing, radial.X * facing);
            Vector2 anchor = Root(t);
            float station = Station(reach[step], u);
            float profile = Station(depth[step], u);
            float fold = MathF.Sin(u * 15f + step * 2.8f - progress * 4f);
            float torn = .84f + .16f * fold;
            float radius = DXOboroMotion.Reach * station;
            float thickness = width * profile * torn;
            float alpha = brightness * Smooth(u / .12f);
            if (lane != 0)
            {
                // Detached long splinters leave negative space inside the wing.
                float fracture = Smooth((progress - .13f * lane) / .36f);
                radius -= (28f + lane * 20f) * (1f - u) + lane * 7f;
                thickness *= (lane == 1 ? .14f : .085f) * fracture;
                anchor -= tangent * (lane * 11f * (1f - u) * fracture);
                alpha *= .54f * fracture * Smooth((.97f - u) / .15f);
            }
            thickness *= 1f - .88f * recovery;
            radius -= recovery * 14f * (1f - u);
            Color color = Color.White * alpha;
            inner = Vertex(anchor + radial * MathF.Max(18f, radius - thickness), color, new(u, 0));
            outer = Vertex(anchor + radial * radius, color, new(u, 1));
        }
    }

    internal static void BladeAndFracture(SpriteBatch batch, float age, int step, float aim, int facing,
        Vector2 grip, bool reduced)
    {
        int release = DXOboroMotion.Release(step), end = DXOboroMotion.LiveEnd(step);
        float elapsed = age - release;
        if (elapsed < 0 || age >= end) return;
        float progress = elapsed / (end - release);
        float angle = DXOboroMotion.Angle(step, age, aim, facing);
        Vector2 axis = angle.ToRotationVector2();
        Vector2 side = new(-axis.Y * facing, axis.X * facing);
        // The first live tick already reaches the full hit radius; keep a
        // narrow visible spine even before the broad smear has opened.
        float bloom = (.28f + .72f * Smooth(elapsed / .65f))
            * (1f - .68f * Smooth((progress - .77f) / .23f));

        using var scope = new WorldGraphicsScope(batch);
        used = 0;
        for (int i = 0; i < 18; i++)
        {
            Smear(i / 18f, out var a, out var b);
            Smear((i + 1f) / 18f, out var c, out var d);
            Quad(a, b, c, d);
        }
        Apply(2, age, step, progress, 0, reduced);
        Flush();

        // Large drawn fractures punctuate the silhouette. One evolving discharge
        // per cut; no repeated flashing/RNG by rendered frame.
        float flash = Smooth((progress - .23f) / .11f) * (1f - Smooth((progress - .66f) / .23f));
        if (flash <= .001f) return;
        used = 0;
        int arcs = step == 2 ? 2 : 1;
        if (reduced) arcs = 1;
        for (int branch = 0; branch < arcs; branch++)
        for (int i = 0; i < 16; i++)
        {
            float u0 = i / 16f, u1 = (i + 1f) / 16f;
            Vector2 a = Bolt(u0, branch), b = Bolt(u1, branch);
            Vector2 d = b - a;
            if (d.LengthSquared() < .0001f) continue;
            Vector2 normal0 = JointNormal(u0, branch);
            Vector2 normal1 = JointNormal(u1, branch);
            float width0 = BoltWidth(u0, branch), width1 = BoltWidth(u1, branch);
            Color color = Color.White * (flash * (reduced ? .64f : 1f));
            Quad(Vertex(a - normal0 * width0, color, new(u0, 0)),
                Vertex(a + normal0 * width0, color, new(u0, 1)),
                Vertex(b - normal1 * width1, color, new(u1, 0)),
                Vertex(b + normal1 * width1, color, new(u1, 1)));
        }
        Apply(1, age, step, progress, 0, reduced);
        Flush();

        void Smear(float u, out VertexPositionColorTexture lower, out VertexPositionColorTexture upper)
        {
            float radius = MathHelper.Lerp(37f, DXOboroMotion.Reach, u);
            float belly = MathF.Pow(MathF.Max(0, MathF.Sin(u * MathF.PI)), .8f);
            float width = (step == 2 ? 32f : step == 1 ? 17f : 24f) * belly * bloom;
            Vector2 at = grip + axis * radius;
            Color color = Color.White * bloom;
            lower = Vertex(at - side * width * .24f, color, new(u, 0));
            upper = Vertex(at + side * width, color, new(u, 1));
        }

        Vector2 Bolt(float u, int branch)
        {
            float x = u * (fractureRadius.Length - 1);
            int n = Math.Min(fractureRadius.Length - 2, (int)x);
            // Linear authored corners, with sub-frame deformation of their
            // support curve. Large readable teeth instead of tiny noise forks.
            float radius = MathHelper.Lerp(fractureRadius[n], fractureRadius[n + 1], x - n);
            float sign = step == 1 ? -1f : 1f;
            float sweep = (step == 2 ? 2.13f : 1.65f) * (.66f + .34f * flash);
            float a = angle - facing * sign * ((1f - u) * sweep + branch * .18f);
            radius = 46f + radius * (DXOboroMotion.Reach - 58f - branch * 46f);
            radius += MathF.Sin(u * 7f + progress * 4f + step) * 6f;
            return grip + a.ToRotationVector2() * radius;
        }
        float BoltWidth(float u, int branch)
            => (step == 2 ? 17f : 11f) * MathF.Pow(MathF.Max(0, MathF.Sin(MathF.PI * u)), .65f)
                * (branch == 0 ? 1f : .67f) * (.48f + .52f * flash);

        Vector2 JointNormal(float u, int branch)
        {
            Vector2 at = Bolt(u, branch);
            Vector2 before = at - Bolt(MathF.Max(0, u - 1f / 16f), branch);
            Vector2 after = Bolt(MathF.Min(1, u + 1f / 16f), branch) - at;
            if (before.LengthSquared() < .0001f) before = after;
            if (after.LengthSquared() < .0001f) after = before;
            before.Normalize(); after.Normalize();
            Vector2 tangent = before + after;
            if (tangent.LengthSquared() < .0001f) tangent = after;
            tangent.Normalize();
            Vector2 normal = new(-tangent.Y, tangent.X);
            float miter = Math.Clamp(1f / MathF.Max(.01f,
                MathF.Abs(Vector2.Dot(normal, new Vector2(-after.Y, after.X)))), 1, 1.75f);
            return normal * miter;
        }
    }

    internal static void Impact(SpriteBatch batch, Vector2 at, float angle, float age, int step, bool reduced)
    {
        if (age < 0 || age >= 7) return;
        float p = age / 7f, fade = (1f - p) * (1f - p);
        int shards = reduced ? 3 : step == 2 ? 9 : 6;
        used = 0;
        for (int i = 0; i < shards; i++)
        {
            float a = angle + i * MathHelper.TwoPi / shards + .23f * MathF.Sin(i * 7f);
            Vector2 axis = a.ToRotationVector2(), normal = new(-axis.Y, axis.X);
            float length = (step == 2 ? 83f : 55f) * (1 + .28f * MathF.Sin(i * 5.1f)) * (.35f + p);
            Vector2 start = at + axis * (p * 32f), tip = start + axis * length;
            Vector2 bend = Vector2.Lerp(start, tip, .37f) + normal * MathF.Sin(i * 3f) * 7f * fade;
            Color color = Color.White * (fade * (reduced ? .6f : 1f));
            float width = (step == 2 ? 6f : 4f) * fade;
            Quad(Vertex(start, color, new(0, .5f)), Vertex(bend - normal * width, color, new(.37f, 0)),
                Vertex(bend + normal * width, color, new(.37f, 1)), Vertex(tip, color, new(1, .5f)));
        }
        using var scope = new WorldGraphicsScope(batch);
        Apply(1, age, step, p, 0, reduced);
        Flush();
    }

    private static float Station(float[] values, float u)
    {
        float x = Math.Clamp(u, 0, 1) * (values.Length - 1);
        int i = Math.Min(values.Length - 2, (int)x);
        return MathHelper.Lerp(values[i], values[i + 1], Smooth(x - i));
    }
    private static float Smooth(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    private static VertexPositionColorTexture Vertex(Vector2 point, Color color, Vector2 uv)
        => new(new Vector3(point - Main.screenPosition, 0), color, uv);
    private static void Quad(VertexPositionColorTexture a, VertexPositionColorTexture b,
        VertexPositionColorTexture c, VertexPositionColorTexture d)
    {
        vertices[used++] = a; vertices[used++] = b; vertices[used++] = c;
        vertices[used++] = c; vertices[used++] = b; vertices[used++] = d;
    }
    private static void Apply(float mode, float age, int step, float progress, float recovery, bool reduced)
    {
        var device = Main.instance.GraphicsDevice;
        var shader = ShaderManager.GetShader("Convergence.DXOboroVeil");
        shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("clock", age / 60f);
        shader.TrySetParameter("cut", (float)step);
        shader.TrySetParameter("progress", progress);
        shader.TrySetParameter("recovery", recovery);
        shader.TrySetParameter("mode", mode);
        shader.TrySetParameter("reduced", reduced ? 1f : 0f);
        shader.Apply();
    }
    private static void Flush()
    {
        if (used > 0) Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, used / 3);
    }
}
