#nullable enable
using System;
using System.Collections.Generic;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor;

// Feature-owned material helpers. Callers hold a WorldGraphicsScope (the
// SpriteBatch is ended); the scope restores device, texture and sampler state.
// All positions are world coordinates; nothing here reports a hit.
internal static class EbonMaterials
{
    internal const string Path = "Convergence/Assets/Textures/EbonManor/";
    internal static readonly Vector3 Moon = new(.62f, .70f, .90f), Silk = new(.66f, .68f, .86f), Rose = new(.86f, .46f, .56f),
        Chalk = new(.84f, .80f, .72f), Gold = new(.90f, .72f, .44f), Candle = new(1f, .70f, .38f), Hot = new(1f, .84f, .88f),
        // Live silk: moon-silver bloom and a dusty-rose fringe around a white-hot core.
        Bloom = new(.74f, .80f, 1f), Dusty = new(.90f, .58f, .70f), Pale = new(.90f, .93f, 1f);
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static readonly List<Vector2> points = new(72);
    private static ManagedShader? silk;
    private static PrimitiveSettings? silkSettings;
    private static float silkHalfWidth = 4, completion = 1;
    private static bool taper;
    private static float Reduced => EbonVisuals.Reduced ? 1 : 0;
    // World px per screen px: hairlines, razor cores and glints keep at least
    // ~.75 screen px when the game view is zoomed out.
    private static float View => 1f / Math.Max(.1f, Main.GameViewMatrix.Zoom.X);

    internal static void Reset() { silk = null; silkSettings = null; points.Clear(); }
    internal static Texture2D Texture(string name) => ModContent.Request<Texture2D>(Path + name).Value;
    internal static Matrix Screen
    {
        get { var v = Main.instance.GraphicsDevice.Viewport; return Matrix.CreateOrthographicOffCenter(0, v.Width, v.Height, 0, -1, 1); }
    }
    internal static Matrix World => Main.GameViewMatrix.TransformationMatrix * Screen;

    internal static ManagedShader Manor(float age, Matrix? matrix = null)
    {
        var device = Main.instance.GraphicsDevice;
        device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        var shader = ShaderManager.GetShader("Convergence.EbonManor");
        shader.TrySetParameter("uWorldViewProjection", matrix ?? World);
        shader.TrySetParameter("clock", age / 60);
        shader.TrySetParameter("view", View);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
        return shader;
    }

    // A rotated quad about its centre (U runs along the rotation axis).
    internal static void Quad(ManagedShader shader, string pass, Vector2 center, Vector2 size, float rotation, bool world = true)
    {
        Vector2 at = world ? center - Main.screenPosition : center;
        Vector2 dx = rotation.ToRotationVector2() * size.X * .5f, dy = new Vector2(-MathF.Sin(rotation), MathF.Cos(rotation)) * size.Y * .5f;
        Submit(shader, pass, at - dx - dy, at + dx - dy, at - dx + dy, at + dx + dy);
    }
    private static void Submit(ManagedShader shader, string pass, Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
    {
        quad[0] = new(new(topLeft, 0), Color.White, new(0, 0)); quad[1] = new(new(bottomLeft, 0), Color.White, new(0, 1));
        quad[2] = new(new(topRight, 0), Color.White, new(1, 0)); quad[3] = quad[2]; quad[4] = quad[1];
        quad[5] = new(new(bottomRight, 0), Color.White, new(1, 1));
        shader.Apply(pass); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    // An atlas cell placed by a pivot (in source pixels), uniform scale and rotation.
    internal static void Sprite(ManagedShader shader, string pass, Texture2D texture, Rectangle source, Vector2 pivot,
        Vector2 origin, float scale, float rotation, bool flip, SamplerState sampler, float shade = 0, float rimless = 0, float weave = 12)
    {
        shader.SetTexture(texture, 0, sampler);
        shader.TrySetParameter("region", new Vector4(source.X / (float)texture.Width, source.Y / (float)texture.Height,
            source.Width / (float)texture.Width, source.Height / (float)texture.Height));
        shader.TrySetParameter("shape", new Vector4(2f / source.Width, 2f / source.Height, shade, rimless));
        shader.TrySetParameter("weaveDensity", weave);
        Vector2 Corner(float u, float v)
        {
            var local = (new Vector2(u * source.Width, v * source.Height) - origin) * scale;
            if (flip) local.X = -local.X;
            return pivot - Main.screenPosition + local.RotatedBy(rotation);
        }
        Submit(shader, pass, Corner(0, 0), Corner(1, 0), Corner(0, 1), Corner(1, 1));
    }

    // Broad warning footprint: a feathered silk veil exactly over a straight band,
    // with no boundary lines. live warms it while the furniture is in flight.
    // clip > 0 fades the veil out inside a disc of that radius centred on b, so a
    // fall corridor meets its burst disc without stacking a denser band.
    internal static void Veil(ManagedShader shader, Vector2 a, Vector2 b, float radius, float progress, float opacity, float seed, float live = 0,
        float clip = 0)
    {
        float length = Vector2.Distance(a, b);
        if (length < 1 || opacity <= .002f) return;
        shader.TrySetParameter("shape", new Vector4(length, radius, Math.Clamp(progress, 0, 1), Math.Clamp(live, 0, 1)));
        shader.TrySetParameter("signal", new Vector4(opacity, clip > 0 ? clip : -1e4f, Reduced, seed));
        Quad(shader, "VeilPass", (a + b) * .5f, new Vector2(length, radius * 2), (b - a).ToRotation());
    }

    // Thread-width warning footprint: one hairline strung from a toward b (the
    // quad is the honest band), trembling less as it tightens. The tremble is a
    // fixed three-half-wave shape, so it is capped by length: short strands
    // shiver instead of folding into an S.
    internal static void Hairline(ManagedShader shader, Vector2 a, Vector2 b, float radius, float progress, float opacity, float seed,
        float reveal = 1, float tremble = 0)
    {
        float length = Vector2.Distance(a, b);
        if (length < 1 || opacity <= .002f) return;
        tremble = Math.Clamp(tremble * (EbonVisuals.Reduced ? .4f : 1), 0, Math.Max(0, Math.Min(radius - 2, length * .006f)));
        shader.TrySetParameter("shape", new Vector4(length, radius, Math.Clamp(progress, 0, 1), Math.Clamp(reveal, 0, 1)));
        shader.TrySetParameter("signal", new Vector4(opacity, tremble, Reduced, seed));
        Quad(shader, "HairlinePass", (a + b) * .5f, new Vector2(length, radius * 2), (b - a).ToRotation());
    }

    // Circular warning footprint (the chandelier burst): the same veil on a disc.
    internal static void VeilDisc(ManagedShader shader, Vector2 center, float radius, float progress, float opacity, float seed)
    {
        if (opacity <= .002f || radius < 1) return;
        shader.TrySetParameter("shape", new Vector4(radius, Math.Clamp(progress, 0, 1), 0, 0));
        shader.TrySetParameter("signal", new Vector4(opacity, 0, Reduced, seed));
        Quad(shader, "DiscPass", center, new Vector2(radius * 2), 0);
    }

    // A small crisp four-point glint; rays lie along rotation and its normal.
    internal static void Glint(ManagedShader shader, Vector2 center, float size, Vector3 tint, float opacity, float rays = 1, float rotation = 0)
    {
        if (opacity <= .002f || size < 1) return;
        shader.TrySetParameter("signal", new Vector4(opacity, rays, size, 0));
        shader.TrySetParameter("tint", tint);
        Quad(shader, "GlintPass", center, new Vector2(size), rotation);
    }

    // blade: px along a->b where the shears are snipping (a glint rides there); < 0 for none.
    internal static void Tear(ManagedShader shader, Vector2 a, Vector2 b, float radius, float sinceFire, float live, float opacity, float seed,
        float blade = -1)
    {
        float length = Vector2.Distance(a, b);
        if (length < 1 || opacity <= .002f) return;
        shader.TrySetParameter("shape", new Vector4(length, radius, Math.Max(0, sinceFire), live));
        shader.TrySetParameter("signal", new Vector4(opacity, blade >= 0 ? blade : -1e4f, Reduced, seed));
        Quad(shader, "TearPass", (a + b) * .5f, new Vector2(length, radius * 2), (b - a).ToRotation());
    }

    private static readonly VertexPositionColorTexture[] fan = new VertexPositionColorTexture[16 * 6];
    // A dim angular wedge between two spoke angles (afterglow or turn preview),
    // clipped per angle to the field like the spokes themselves. outer > 0 keeps
    // only the part of the wedge beyond that fraction of the reach.
    internal static void Sweep(ManagedShader shader, Convergence.Common.Raids.Arena.RaidFieldGeometry field, Vector2 center,
        float from, float to, float inner, float reach, Vector3 tint, float opacity, float seed, float outer = 0)
    {
        if (opacity <= .002f || MathF.Abs(to - from) < .002f) return;
        const int steps = 16;
        int n = 0;
        Vector2 Outer(float a)
        {
            var d = a.ToRotationVector2();
            float r = reach;
            if (field.ClipAxis(center.X, center.Y, d.X, d.Y, out _, out float last)) r = Math.Min(r, last);
            return center + d * Math.Max(inner + 1, r) - Main.screenPosition;
        }
        for (int k = 0; k < steps; k++)
        {
            float u0 = k / (float)steps, u1 = (k + 1) / (float)steps;
            float a0 = MathHelper.Lerp(from, to, u0), a1 = MathHelper.Lerp(from, to, u1);
            Vector2 i0 = center + a0.ToRotationVector2() * inner - Main.screenPosition, i1 = center + a1.ToRotationVector2() * inner - Main.screenPosition;
            Vector2 o0 = Outer(a0), o1 = Outer(a1);
            fan[n++] = new(new(i0, 0), Color.White, new(0, u0)); fan[n++] = new(new(o0, 0), Color.White, new(1, u0)); fan[n++] = new(new(i1, 0), Color.White, new(0, u1));
            fan[n++] = new(new(o0, 0), Color.White, new(1, u0)); fan[n++] = new(new(o1, 0), Color.White, new(1, u1)); fan[n++] = new(new(i1, 0), Color.White, new(0, u1));
        }
        shader.TrySetParameter("signal", new Vector4(opacity * (EbonVisuals.Reduced ? .5f : 1), outer > 0 ? outer : -1, Reduced, seed));
        shader.TrySetParameter("tint", tint);
        shader.Apply("SweepPass");
        Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, fan, 0, n / 3);
    }

    // The true radius is the centre of the hoop's thin boundary line.
    internal static void Ring(ManagedShader shader, Vector2 center, float radius, bool spread, float progress, Vector3 tint,
        float opacity, float flash, bool failed, float seed)
    {
        float outer = radius + 2.4f;
        shader.TrySetParameter("shape", new Vector4(spread ? 1 : 0, progress, outer, EbonVisuals.Reduced ? 1 : 0));
        shader.TrySetParameter("signal", new Vector4(opacity, flash, failed ? 1 : 0, seed));
        shader.TrySetParameter("tint", tint);
        Quad(shader, "RingPass", center, new Vector2(outer * 2), 0);
    }

    internal static void Glow(ManagedShader shader, Vector2 center, float diameter, Vector3 tint, float opacity,
        float ring = 0, float ringWidth = .1f, float additive = 1, bool world = true)
    {
        if (opacity <= .002f || diameter < 1) return;
        shader.TrySetParameter("shape", new Vector4(ring, ringWidth, additive, 0));
        shader.TrySetParameter("signal", new Vector4(opacity, 0, 0, 0));
        shader.TrySetParameter("tint", tint);
        Quad(shader, "GlowPass", center, new Vector2(diameter), 0, world);
    }

    internal static void Shard(ManagedShader shader, Vector2 center, float length, float width, float rotation, int kind, float opacity, float glint)
    {
        if (opacity <= .002f) return;
        shader.TrySetParameter("shape", new Vector4(kind, 0, 0, 0));
        shader.TrySetParameter("signal", new Vector4(opacity, glint, 0, 0));
        Quad(shader, "ShardPass", center, new Vector2(length, width), rotation);
    }

    // Deterministic debris on analytic arcs from a hashed seed: no retained
    // particles, identical after pause, catch-up or a late join.
    internal static void Burst(ManagedShader shader, Vector2 origin, int seed, int count, float t, float life, int kind,
        float speed, float gravity, float size, float spread = MathHelper.TwoPi, float heading = 0)
    {
        if (t < 0 || t >= life) return;
        if (EbonVisuals.Reduced) count = Math.Max(2, count / 3);
        for (int i = 0; i < count; i++)
        {
            float h1 = Hash(seed, i, 1), h2 = Hash(seed, i, 2), h3 = Hash(seed, i, 3);
            float angle = heading + (h1 - .5f) * spread;
            float v = speed * (.35f + .65f * h2);
            float drag = 1 - MathF.Exp(-t / 14);
            Vector2 at = origin + angle.ToRotationVector2() * v * 14 * drag + new Vector2(0, .5f * gravity * t * t);
            float fade = 1 - EbonVisualsMath.Ease((t - life * .45f) / (life * .55f));
            float spin = (h3 - .5f) * .5f * t + angle;
            Shard(shader, at, size * (.6f + .8f * h3), size * (.22f + .2f * h2), spin, kind, fade, t * .3f + i);
        }
    }
    internal static float Hash(int seed, int i, int salt)
    {
        uint x = (uint)seed * 747796405u + (uint)i * 2891336453u + (uint)salt * 277803737u;
        x ^= x >> 16; x *= 2246822519; x ^= x >> 13; x *= 3266489917; x ^= x >> 16;
        return (x & 0xFFFFFF) / 16777216f;
    }

    // Silk threads through Luminance's primitive renderer (classic silk): every
    // harmless strand (hanging, control, flung, recoiling, ceremony).
    internal static void Thread(IReadOnlyList<Vector2> path, float halfWidth, Vector3 tint, float opacity, float tension,
        float heat, float seed, float age, bool tapered = false)
        => Render(path, halfWidth, tint, opacity, tension, heat, seed, age, tapered, Vector4.Zero, Vector2.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero);

    // Live razor silk on an honest straight band of the given hit radius: the
    // ribbon stays on the accepted line while the shader twangs only the core.
    // since: ticks since the strand snapped taut (snap bead, ignition, flash);
    // flash scales that snap (repeated beats use less); openStart/openEnd fade an
    // end that finishes in open space instead of at an anchor.
    internal static void Razor(Vector2 a, Vector2 b, float radius, float opacity, float since, float seed, float age,
        float twangAmplitude = 0, float twangRate = 0, float flash = 1, bool openStart = false, bool openEnd = false)
    {
        line.Clear(); line.Add(a); line.Add(b);
        RazorPath(line, radius, opacity, since, seed, age, twangAmplitude, twangRate, flash, openStart, openEnd);
    }
    internal static void RazorPath(IReadOnlyList<Vector2> path, float radius, float opacity, float since, float seed, float age,
        float twangAmplitude = 0, float twangRate = 0, float flash = 1, bool openStart = false, bool openEnd = false)
    {
        radius = Math.Max(1, radius);
        float length = Length(path), gain = Math.Max(0, flash), t = Math.Max(0, since);
        // The twang is a fixed three-half-wave shape: cap it by length so short strands never fold.
        float amplitude = Math.Min(twangAmplitude * (EbonVisuals.Reduced ? .4f : 1), Math.Max(0, Math.Min(radius - 1.5f, length * .006f)));
        // Snap: a bead races in from both anchors (ease-out over ~6 ticks) and
        // ignites the core behind it; ahead of it the core idles at 35%.
        float meet = 1 - MathF.Pow(1 - Math.Clamp(t / 6, 0, 1), 3);
        var snap = new Vector4(1 - .65f * gain, meet * (length * .5f + 16), .8f * (1 - EbonVisualsMath.Ease((meet - .86f) / .14f)) * gain,
            MathF.Pow(2, -t / 3.5f) * (1 - Reduced * .5f) * gain);
        Render(path, radius + Math.Max(8, radius * .7f), Bloom, opacity, 1, 1, seed, age, false,
            new Vector4(radius, t, -1, Reduced), new Vector2(amplitude, twangRate),
            new Vector4(openStart ? 1 : 0, openEnd ? 1 : 0, Math.Min(radius, 12), Math.Max(.6f, .75f * View)), snap, Vector4.Zero);
    }

    // A strand that has stopped dealing damage: cool silver, no band, no white
    // heat; it thins evenly, parts at one or two hashed points and frays into
    // motes (fray 0..1). freeStart/freeEnd mark the broken end(s), which recede.
    internal static void Frayed(IReadOnlyList<Vector2> path, float radius, float fray, int seed, float age, bool freeStart, bool freeEnd)
    {
        if (fray >= 1) return;
        float f = Math.Clamp(fray, 0, 1), length = Length(path), left = 1 - f;
        bool second = Hash(seed, 5, 3) >= .45f && length >= 260;
        // Gap half-widths start closed (-14) so a break opens smoothly instead of popping.
        var breaks = new Vector4((.3f + .4f * Hash(seed, 5, 1)) * length, -14 + 75 * f,
            (.12f + .2f * Hash(seed, 5, 2)) * length, second ? -14 + 54 * Math.Clamp(f * 1.6f - .5f, 0, 1) : -100);
        var snap = new Vector4(left * left, EbonVisualsMath.Ease((f - .15f) / .25f) * left, 0, 0);
        Render(path, Math.Max(1, radius) + 8, Bloom, 1, 1, 0, seed, age, false,
            new Vector4(Math.Max(1, radius), 0, f, Reduced), Vector2.Zero,
            new Vector4(freeStart ? 1 : 0, freeEnd ? 1 : 0, 10 + 170 * f, Math.Max(.75f * (1 - .3f * f), .75f * View)), snap, breaks);
    }

    private static float Length(IReadOnlyList<Vector2> path)
    {
        float length = 0;
        for (int i = 1; i < path.Count; i++) length += Vector2.Distance(path[i - 1], path[i]);
        return length;
    }

    private static void Render(IReadOnlyList<Vector2> path, float halfWidth, Vector3 tint, float opacity, float tension,
        float heat, float seed, float age, bool tapered, Vector4 razor, Vector2 twang, Vector4 razorEx, Vector4 snap, Vector4 breaks)
    {
        if (Main.dedServ || path.Count < 2 || opacity <= .003f) return;
        silk ??= ShaderManager.GetShader("Convergence.EbonSilk");
        silkSettings ??= new PrimitiveSettings(u => silkHalfWidth * (taper ? .2f + .8f * Math.Clamp(u * completion, 0, 1) : 1),
            _ => Color.White, Smoothen: false, Shader: silk);
        points.Clear();
        float length = 0;
        for (int i = 0; i < path.Count && points.Count < 64; i++)
        {
            if (points.Count > 0 && Vector2.DistanceSquared(points[^1], path[i]) < .01f) continue;
            if (points.Count > 0) length += Vector2.Distance(points[^1], path[i]);
            points.Add(path[i]);
        }
        if (points.Count < 2) return;
        if (points.Count == 2) points.Insert(1, (points[0] + points[1]) * .5f);
        // Installed Luminance 1.0.14 omits the final segment; extend by one step
        // so the visible end stays attached to its hand, hook or anchor.
        points.Add(points[^1] + (points[^1] - points[^2]));
        completion = (points.Count - 1f) / (points.Count - 2);
        silkHalfWidth = halfWidth; taper = tapered;
        silk.TrySetParameter("clock", age / 60);
        silk.TrySetParameter("completionScale", completion);
        silk.TrySetParameter("signal", new Vector4(opacity * (EbonVisuals.Reduced ? .85f : 1), tension, heat, seed));
        silk.TrySetParameter("tint", tint);
        silk.TrySetParameter("footprint", new Vector2(Math.Max(1, length), halfWidth));
        silk.TrySetParameter("razor", razor);
        silk.TrySetParameter("twang", twang);
        silk.TrySetParameter("razorEx", razorEx);
        silk.TrySetParameter("snap", snap);
        silk.TrySetParameter("breaks", breaks);
        silk.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        PrimitiveRenderer.RenderTrail(points, silkSettings, points.Count);
    }
    private static readonly List<Vector2> line = new(34);
    // A straight or vibrating thread between two points (standing-wave twang).
    internal static void Straight(Vector2 a, Vector2 b, float halfWidth, Vector3 tint, float opacity, float tension, float heat,
        float seed, float age, float amplitude = 0, float frequency = 0)
    {
        line.Clear();
        int steps = amplitude > .05f ? 24 : 2;
        Vector2 n = (b - a).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps;
            float wave = amplitude > .05f ? MathF.Sin(u * MathF.PI) * MathF.Sin(u * MathF.PI * 3 + age * frequency) * amplitude : 0;
            line.Add(Vector2.Lerp(a, b, u) + n * wave);
        }
        Thread(line, halfWidth, tint, opacity, tension, heat, seed, age);
    }
    // A hanging catenary-like sag (parabola) for slack silk.
    internal static void Slack(Vector2 a, Vector2 b, float sag, float halfWidth, Vector3 tint, float opacity, float tension,
        float heat, float seed, float age)
    {
        line.Clear();
        for (int i = 0; i <= 16; i++)
        {
            float u = i / 16f;
            line.Add(Vector2.Lerp(a, b, u) + new Vector2(0, 4 * sag * u * (1 - u)));
        }
        Thread(line, halfWidth, tint, opacity, tension, heat, seed, age);
    }
}
