#nullable enable
using System;
using Convergence.Content.Items.DXOboro;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

// One accepted Soboro cut as drawn: its step/aim/facing and the same native
// hand basis the production collision uses. Pure presentation data.
internal readonly record struct SoboroCutPose(int Step, float Aim, int Facing,
    Vector2 HandCenter, Vector2 HandAlong, Vector2 HandAcross, int Seed)
{
    internal Vector2 Root(float age)
    {
        float angle = DXOboroMotion.ArmAngle(Step, age, Aim, Facing);
        return HandCenter + HandAlong * MathF.Cos(angle) + HandAcross * MathF.Sin(angle);
    }

    internal Vector2 Direction(float age) => DXOboroMotion.Angle(Step, age, Aim, Facing).ToRotationVector2();
}

internal struct SoboroSlashVertex : IVertexType
{
    public Vector3 Position;
    public Vector2 Uv;
    public Vector4 Data;

    internal static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(20, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1));

    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}

// Pixel-art Soboro cuts. Everything is drawn into a half-resolution target
// (one art pixel = two world pixels) and point-upscaled by Composite, so the
// slash reads like Terraria's sprite art instead of a smooth vector ribbon.
// The crescent follows the accepted swept blade path; lightning, sparks, the
// crest glint and the hit star are deterministic per cut and never gameplay.
internal static class SoboroSlashArt
{
    internal const float ArtScale = .5f;
    internal const int ResidueTicks = 7;
    private const float MinimumRadius = 40f;
    private const float OuterRadius = 1.10f;
    private const string NoisePath = "Convergence/Assets/Textures/Items/DXOboro/SlashNoise";

    internal static readonly Color Tone1 = new(22, 5, 48), Tone2 = new(62, 16, 138), Tone3 = new(112, 40, 222),
        Tone4 = new(170, 108, 255), Tone5 = new(220, 196, 255), Tone6 = Color.White;

    private static readonly SoboroSlashVertex[] crescent = new SoboroSlashVertex[64 * 6];
    private static readonly VertexPositionTexture[] quad = new VertexPositionTexture[6];
    private static readonly Vector2[] path = new Vector2[48], scratch = new Vector2[48];
    private static readonly Point[] plotted = new Point[1536];
    private static int plottedCount;

    private static Texture2D Noise => ModContent.Request<Texture2D>(NoisePath, AssetRequestMode.ImmediateLoad).Value;

    private static float Thickness(int step) => step switch { 1 => .34f, 2 => .72f, _ => .52f };
    private static float Cooling(int step) => step switch { 1 => 1.7f, 2 => 3.3f, _ => 2.4f };

    internal static bool Visible(int step, float age)
        => age >= DXOboroMotion.Release(step) - 3f && age < DXOboroMotion.LiveEnd(step) + ResidueTicks;

    // Same sub-tick policy as the blade: a live tick never shows the harmless
    // wind-up and a recovery tick never shows a live flare. Residue keeps
    // counting after the projectile is gone.
    internal static float DrawAge(int step, int ageTick, float fraction, int ticksSinceUpdate, bool alive)
    {
        float age = ageTick - 1f + ticksSinceUpdate + fraction;
        if (!alive) return age;
        age = Math.Clamp(age, 0f, DXOboroMotion.Duration(step));
        if (ageTick >= DXOboroMotion.Release(step)) age = MathF.Max(age, DXOboroMotion.Release(step));
        if (ageTick >= DXOboroMotion.LiveEnd(step)) age = MathF.Max(age, DXOboroMotion.LiveEnd(step));
        return age;
    }

    // World-space radius that encloses everything a cut can draw.
    internal static float Extent(int step) => DXOboroMotion.Reach * OuterRadius + (step == 2 ? 200f : 150f);

    internal static void DrawCrescent(GraphicsDevice device, in SoboroCutPose pose, float age, Vector2 origin,
        Matrix projection, bool reduced)
    {
        int step = pose.Step, release = DXOboroMotion.Release(step), end = DXOboroMotion.LiveEnd(step);
        if (age <= release) return;
        float head = MathF.Min(age, end), tail = release - .2f;
        if (head - tail < .05f) return;
        float fade = Math.Clamp((age - end) / 6f, 0f, 1f);
        int sections = reduced ? 28 : 56, used = 0;
        SoboroSlashVertex lastInner = default, lastOuter = default;
        for (int i = 0; i <= sections; i++)
        {
            float u = i / (float)sections, t = tail + (head - tail) * u;
            Vector2 radial = pose.Direction(t), root = pose.Root(t);
            // x: ticks since the tip crossed (cooling), y: when it crossed (stable noise)
            Vector4 data = new(age - t, t - release, 1f, 0f);
            SoboroSlashVertex inner = Vertex(root + radial * MinimumRadius, origin, u, MinimumRadius / DXOboroMotion.Reach, data);
            SoboroSlashVertex outer = Vertex(root + radial * (DXOboroMotion.Reach * OuterRadius), origin, u, OuterRadius, data);
            if (i > 0)
            {
                crescent[used++] = lastInner; crescent[used++] = lastOuter; crescent[used++] = inner;
                crescent[used++] = inner; crescent[used++] = lastOuter; crescent[used++] = outer;
            }
            lastInner = inner;
            lastOuter = outer;
        }
        ManagedShader shader = ShaderManager.GetShader("Convergence.SoboroPixelSlash");
        shader.TrySetParameter("uWorldViewProjection", projection);
        shader.TrySetParameter("cutStep", (float)step);
        shader.TrySetParameter("fade", fade);
        shader.TrySetParameter("thick", Thickness(step) * (reduced ? .88f : 1f));
        shader.TrySetParameter("tau", Cooling(step));
        shader.TrySetParameter("echo", step == 2 ? 1f : 0f);
        shader.TrySetParameter("reduced", reduced ? 1f : 0f);
        shader.TrySetParameter("noiseShift", new Vector2(pose.Seed * .1379f % 1f, pose.Seed * .2917f % 1f));
        shader.SetTexture(Noise, 1, SamplerState.LinearWrap);
        shader.Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, crescent, 0, used / 3);
    }

    // Accents are plotted pixel by pixel into the same half-resolution target.
    internal static void DrawAccents(SpriteBatch batch, Texture2D pixel, in SoboroCutPose pose, float age,
        Vector2 origin, bool reduced, bool hasImpact, Vector2 impactAt, float impactAge)
    {
        Glint(batch, pixel, pose, age, origin);
        Lightning(batch, pixel, pose, age, origin, reduced);
        Sparks(batch, pixel, pose, age, origin, reduced);
        if (hasImpact) ImpactStar(batch, pixel, (impactAt - origin) * ArtScale, impactAge, pose.Step == 2, reduced);
    }

    // Point-upscales the half-resolution layer over the given screen rectangle,
    // with the one-pixel dark outline and a restrained glow (none when reduced).
    internal static void Composite(GraphicsDevice device, Texture2D scene, Rectangle screenBounds, Matrix view, bool reduced)
    {
        float width = scene.Width, height = scene.Height;
        VertexPositionTexture Corner(int x, int y)
            => new(new Vector3(x, y, 0), new Vector2(x * ArtScale / width, y * ArtScale / height));
        quad[0] = Corner(screenBounds.Left, screenBounds.Top);
        quad[1] = Corner(screenBounds.Right, screenBounds.Top);
        quad[2] = Corner(screenBounds.Left, screenBounds.Bottom);
        quad[3] = quad[2];
        quad[4] = quad[1];
        quad[5] = Corner(screenBounds.Right, screenBounds.Bottom);
        ManagedShader shader = ShaderManager.GetShader("Convergence.SoboroPixelSlash");
        shader.TrySetParameter("uWorldViewProjection", view * Matrix.CreateOrthographicOffCenter(0,
            device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("texel", new Vector2(1f / width, 1f / height));
        shader.TrySetParameter("glowStrength", reduced ? 0f : .55f);
        shader.SetTexture(scene, 0, SamplerState.PointClamp);
        shader.Apply("CompositePass");
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    // A short "shing" on the physical blade tip while the blade crests the swing.
    private static void Glint(SpriteBatch batch, Texture2D pixel, in SoboroCutPose pose, float age, Vector2 origin)
    {
        float start = DXOboroMotion.Release(pose.Step) - 2.6f, p = (age - start) / 3.2f;
        if (p <= 0 || p >= 1) return;
        float strength = MathF.Sin(MathF.PI * p);
        Vector2 tip = pose.Root(age) + pose.Direction(age) * DXOboroMotion.BladeLength(pose.Step, age);
        Point c = ToPixel((tip - origin) * ArtScale);
        int arm = (int)MathF.Round(1 + 5 * strength), diagonal = (int)MathF.Round(3 * strength);
        for (int k = 1; k <= arm; k++)
        {
            Color color = k <= arm / 2 ? Tone5 : Tone4;
            Plot(batch, pixel, c.X + k, c.Y, color); Plot(batch, pixel, c.X - k, c.Y, color);
            Plot(batch, pixel, c.X, c.Y + k, color); Plot(batch, pixel, c.X, c.Y - k, color);
        }
        for (int k = 1; k <= diagonal; k++)
        {
            Plot(batch, pixel, c.X + k, c.Y + k, Tone4); Plot(batch, pixel, c.X - k, c.Y - k, Tone4);
            Plot(batch, pixel, c.X + k, c.Y - k, Tone4); Plot(batch, pixel, c.X - k, c.Y + k, Tone4);
        }
        Plot(batch, pixel, c.X, c.Y, Tone6);
    }

    // Branching bolts thrown from the rim; each shape holds for two ticks.
    private static void Lightning(SpriteBatch batch, Texture2D pixel, in SoboroCutPose pose, float age, Vector2 origin, bool reduced)
    {
        int step = pose.Step, release = DXOboroMotion.Release(step), end = DXOboroMotion.LiveEnd(step);
        float start = release + (step == 1 ? .6f : 1f), stop = end - .5f;
        if (age < start || age >= stop + 2.5f) return;
        int epoch = (int)((age - start) / 2f);
        float strength = age < stop ? 1f : 1f - (age - stop) / 2.5f;
        int bolts = reduced ? (step == 2 ? 2 : 1) : step switch { 1 => 1, 2 => 4, _ => 2 };
        float length = step switch { 1 => 85f, 2 => 175f, _ => 120f };
        float turn = pose.Facing * (step == 1 ? -1f : 1f);
        plottedCount = 0;
        for (int b = 0; b < bolts; b++)
        {
            var random = new Random32(pose.Seed * 131 + b * 17 + epoch * 7919);
            float emit = MathF.Min(age, end) - random.Range(.3f, 2.2f);
            Vector2 radial = pose.Direction(emit), tangent = new Vector2(-radial.Y, radial.X) * turn;
            Vector2 root = pose.Root(emit) + radial * (DXOboroMotion.Reach * random.Range(.78f, .98f));
            Vector2 direction = Vector2.Normalize(-tangent * random.Range(.3f, .9f) + radial * random.Range(.5f, 1f));
            float boltLength = length * random.Range(.6f, 1f) * MathF.Max(strength, .35f) * ArtScale;
            Vector2 from = (root - origin) * ArtScale;
            int count = Subdivide(from, from + direction * boltLength, 5, .34f, ref random);
            for (int i = 1; i < count; i++) Collect(path[i - 1], path[i]);
            if (random.Next() < .7f)
            {
                int split = count / 2 + (int)random.Range(-3, 3);
                Vector2 fork = Vector2.Normalize(direction + new Vector2(-direction.Y, direction.X) * random.Range(-.9f, .9f));
                Vector2 at = path[Math.Clamp(split, 0, count - 1)];
                int branchCount = Subdivide(at, at + fork * boltLength * .22f, 4, .34f, ref random);
                for (int i = 1; i < branchCount; i++) Collect(path[i - 1], path[i]);
            }
        }
        if (strength < .5f)
        {
            for (int i = 0; i < plottedCount; i++) Plot(batch, pixel, plotted[i].X, plotted[i].Y, Tone4);
            return;
        }
        for (int i = 0; i < plottedCount; i++)
        {
            Point p = plotted[i];
            Plot(batch, pixel, p.X + 1, p.Y, Tone3); Plot(batch, pixel, p.X - 1, p.Y, Tone3);
            Plot(batch, pixel, p.X, p.Y + 1, Tone3); Plot(batch, pixel, p.X, p.Y - 1, Tone3);
        }
        for (int i = 0; i < plottedCount; i++) Plot(batch, pixel, plotted[i].X, plotted[i].Y, Tone6);
    }

    // Short-lived pixel sparks thrown along the tip path while the cut is live.
    private static void Sparks(SpriteBatch batch, Texture2D pixel, in SoboroCutPose pose, float age, Vector2 origin, bool reduced)
    {
        int step = pose.Step, release = DXOboroMotion.Release(step), end = DXOboroMotion.LiveEnd(step);
        int count = step switch { 1 => 5, 2 => 12, _ => 7 };
        if (reduced) count /= 2;
        float turn = pose.Facing * (step == 1 ? -1f : 1f);
        for (int k = 0; k < count; k++)
        {
            var random = new Random32(pose.Seed * 977 + k * 31);
            float born = release + random.Range(.5f, MathF.Max(end - release - .5f, 1f));
            float life = random.Range(4f, 7.5f), a = age - born;
            if (a < 0 || a > life) continue;
            Vector2 radial = pose.Direction(born), tangent = new Vector2(-radial.Y, radial.X) * turn;
            Vector2 start = pose.Root(born) + radial * (DXOboroMotion.Reach * random.Range(.82f, 1f));
            Vector2 velocity = tangent * random.Range(3f, 7f) + radial * random.Range(1.5f, 4.5f);
            Vector2 at = start + velocity * a + new Vector2(0, .08f) * a * a;
            float heat = 1f - a / life;
            plottedCount = 0;
            Collect((at - velocity * 1.2f - origin) * ArtScale, (at - origin) * ArtScale);
            for (int i = 0; i < plottedCount; i++)
                Plot(batch, pixel, plotted[i].X, plotted[i].Y, heat < .4f ? Tone3 : Tone4);
            Point head = ToPixel((at - origin) * ArtScale);
            Plot(batch, pixel, head.X, head.Y, heat > .5f ? Tone6 : Tone5);
        }
    }

    // A bounded star at the owner's local hit point; heavy hits get longer rays.
    private static void ImpactStar(SpriteBatch batch, Texture2D pixel, Vector2 center, float age, bool heavy, bool reduced)
    {
        if (age < 0 || age >= 6) return;
        float p = age / 6f;
        int rays = reduced ? 4 : 8;
        float reach = (heavy ? 14f : 9f) * (1f - p * .6f), skip = reach * p * .7f;
        Point c = ToPixel(center);
        plottedCount = 0;
        for (int r = 0; r < rays; r++)
        {
            float angle = r * MathHelper.TwoPi / rays + .39f;
            Vector2 d = angle.ToRotationVector2();
            Collect(center + d * skip, center + d * reach * (r % 2 == 0 ? 1f : .6f));
        }
        // A dark rim first so the star reads even on the white crescent.
        for (int i = 0; i < plottedCount; i++)
        {
            Point q = plotted[i];
            Plot(batch, pixel, q.X + 1, q.Y, Tone1); Plot(batch, pixel, q.X - 1, q.Y, Tone1);
            Plot(batch, pixel, q.X, q.Y + 1, Tone1); Plot(batch, pixel, q.X, q.Y - 1, Tone1);
        }
        for (int i = 0; i < plottedCount; i++)
            Plot(batch, pixel, plotted[i].X, plotted[i].Y, p < .45f ? Tone6 : Tone5);
        if (p < .5f)
        {
            Plot(batch, pixel, c.X, c.Y, Tone6); Plot(batch, pixel, c.X + 1, c.Y, Tone6);
            Plot(batch, pixel, c.X, c.Y + 1, Tone6); Plot(batch, pixel, c.X + 1, c.Y + 1, Tone6);
        }
    }

    // Midpoint displacement into the shared path buffer; returns the point count.
    private static int Subdivide(Vector2 from, Vector2 to, int levels, float jag, ref Random32 random)
    {
        path[0] = from;
        path[1] = to;
        int count = 2;
        float displacement = Vector2.Distance(from, to) * jag;
        for (int level = 0; level < levels && count * 2 - 1 <= path.Length; level++)
        {
            int written = 0;
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 a = path[i], b = path[i + 1], d = b - a;
                float length = d.Length();
                Vector2 normal = length > .001f ? new Vector2(-d.Y, d.X) / length : Vector2.Zero;
                scratch[written++] = a;
                scratch[written++] = (a + b) * .5f + normal * random.Range(-displacement, displacement);
            }
            scratch[written++] = path[count - 1];
            Array.Copy(scratch, path, written);
            count = written;
            displacement *= .55f;
        }
        return count;
    }

    // Bresenham between two target-space points into the plotted buffer.
    private static void Collect(Vector2 a, Vector2 b)
    {
        Point p0 = ToPixel(a), p1 = ToPixel(b);
        int dx = Math.Abs(p1.X - p0.X), dy = -Math.Abs(p1.Y - p0.Y);
        int sx = p0.X < p1.X ? 1 : -1, sy = p0.Y < p1.Y ? 1 : -1, error = dx + dy;
        for (int guard = 0; guard < 512 && plottedCount < plotted.Length; guard++)
        {
            plotted[plottedCount++] = p0;
            if (p0 == p1) break;
            int e2 = 2 * error;
            if (e2 >= dy) { error += dy; p0.X += sx; }
            if (e2 <= dx) { error += dx; p0.Y += sy; }
        }
    }

    private static Point ToPixel(Vector2 v) => new((int)MathF.Floor(v.X), (int)MathF.Floor(v.Y));

    private static void Plot(SpriteBatch batch, Texture2D pixel, int x, int y, Color color)
        => batch.Draw(pixel, new Rectangle(x, y, 1, 1), new Rectangle(0, 0, 1, 1), color);

    private static SoboroSlashVertex Vertex(Vector2 world, Vector2 origin, float u, float rho, Vector4 data)
        => new() { Position = new Vector3((world - origin) * ArtScale, 0), Uv = new Vector2(u, rho), Data = data };

    // Allocation-free deterministic sequence (xorshift32); drawing never uses gameplay RNG.
    private struct Random32
    {
        private uint state;

        internal Random32(int seed)
        {
            state = (uint)seed * 747796405u + 2891336453u;
            if (state == 0) state = 1;
            Next();
        }

        internal float Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / 16777216f;
        }

        internal float Range(float low, float high) => low + (high - low) * Next();
    }
}
