#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.AzureCathedral;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.AzureCathedral;

// Native-size atlas skin and small attached refractions. Every pose and cue is
// reconstructed from the accepted clock; none of these strips has a hitbox.
internal static class AzureLioraPresentation
{
    private const int Columns = 12, Rows = 16, StripSegments = 12;
    private static readonly VertexPositionColorTexture[] grid = new VertexPositionColorTexture[(Columns + 1) * (Rows + 1)];
    private static readonly VertexPositionColorTexture[] skin = new VertexPositionColorTexture[Columns * Rows * 6];
    private static readonly VertexPositionColorTexture[] strip = new VertexPositionColorTexture[StripSegments * 6];

    internal static void Draw(SpriteBatch batch, Texture2D art, Vector2 root, Rectangle source, int frame,
        float age, float ceremony, float devouring, float alpha, float lean, bool sealedGirl,
        bool cutting, float charge, float release)
    {
        if (Main.dedServ || alpha <= .001f) return;
        bool reduced = AzureVisuals.Reduced;
        float awakening = AzureRules.Ease((ceremony - AzureRules.IceBreak) / 24);
        float swordCeremony = AzureRules.Ease((ceremony - AzureRules.SwordLight + 35) / 40)
            * (1 - AzureRules.Ease((ceremony - 660) / 110));
        float devourSword = devouring < 0 ? 0 : AzureRules.Ease((devouring - 50) / 30)
            * (1 - AzureRules.Ease((devouring - AzureRules.DevourContact + 18) / 35));
        // The authored hold starts with a quick intake, barely breathes, and
        // releases into a short recoil on the authoritative Fire tick.
        float intake = cutting && release < -22 ? AzureRules.Ease(charge * 3.4f) : 0;
        float hold = cutting && release is >= -22 and < 0
            ? .62f + .20f * AzureRules.Ease((release + 22) / 6) + .025f * MathF.Sin(age * .19f) : 0;
        float burst = cutting && release >= 0 ? MathF.Exp(-release / 5.5f) : 0;
        float strike = cutting && release >= 0 ? (.82f + burst * .5f)
            * (1 - AzureRules.Ease(release / 18)) : 0;
        float swordPower = Math.Max(Math.Max(swordCeremony, devourSword), Math.Max(intake * .62f, Math.Max(hold, strike)));
        swordPower *= awakening * alpha;
        float veil = (sealedGirl ? .32f : .47f) * alpha * (reduced ? .42f : 1);
        float motion = reduced ? .28f : 1;
        float clock = age / 60;
        Vector2 origin = new(24, frame >= 4 ? 40 : 35);
        Vector2 scaleRoot = root;

        using var scope = new WorldGraphicsScope(batch);
        var device = Main.instance.GraphicsDevice;
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        var shader = ShaderManager.GetShader("Convergence.AzureLiora");
        shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("clock", clock);
        shader.TrySetParameter("region", new Vector4(source.X / (float)art.Width, source.Y / (float)art.Height,
            source.Width / (float)art.Width, source.Height / (float)art.Height));
        shader.TrySetParameter("signal", new Vector4(alpha, swordPower, burst, reduced ? .45f : 1));
        shader.TrySetParameter("tone", sealedGirl ? new Vector3(181 / 255f, 220 / 255f, 239 / 255f) : Vector3.One);
        shader.SetTexture(art, 0, SamplerState.PointClamp);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);

        Vector2 Map(Vector2 p)
        {
            p -= origin;
            return scaleRoot + new Vector2(p.X * MathF.Cos(lean) - p.Y * MathF.Sin(lean),
                p.X * MathF.Sin(lean) + p.Y * MathF.Cos(lean));
        }

        // The skirt's lower edge lags the torso by just a few native pixels.
        // Hair and face stay locked to the original eight pixel poses.
        int n = 0;
        for (int y = 0; y <= Rows; y++)
            for (int x = 0; x <= Columns; x++)
            {
                Vector2 uv = new((float)x / Columns, (float)y / Rows);
                float hem = AzureRules.Ease((uv.Y - .54f) / .36f);
                float side = (uv.X - .5f) * 2;
                Vector2 p = uv * new Vector2(48, 64);
                p.X += motion * hem * (MathF.Sin(age * .035f - uv.Y * 8 + side * 2) * 1.65f
                    + burst * side * 2.2f);
                p.Y += motion * hem * MathF.Sin(age * .045f + uv.X * 7) * .8f;
                grid[y * (Columns + 1) + x] = new(new(Map(p), 0), Color.White, uv);
            }
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Columns; x++)
            {
                int a = y * (Columns + 1) + x, b = a + 1, c = a + Columns + 1, d = c + 1;
                skin[n++] = grid[a]; skin[n++] = grid[b]; skin[n++] = grid[c];
                skin[n++] = grid[b]; skin[n++] = grid[d]; skin[n++] = grid[c];
            }

        if (veil > .001f)
        {
            shader.TrySetParameter("shape", new Vector4(veil, .12f, 0, 0));
            float drift = MathF.Sin(age * .038f) * 2 * motion;
            Ribbon(new(14, 52), new(10, 58), new(21 + drift, 62), new(25 + drift, 69), 2.7f * motion, .5f);
            shader.TrySetParameter("shape", new Vector4(veil, 1.83f, 0, 0));
            Ribbon(new(33, 52), new(39, 56), new(37 + drift, 63), new(44 + drift, 66), 2.2f * motion, .5f);
        }
        shader.Apply("AuraPass"); Submit(skin, n);
        shader.Apply("BodyPass"); Submit(skin, n);

        if (swordPower > .025f && frame >= 4)
        {
            Vector2 hilt, tip;
            // Measured native-cell blade endpoints (not the cell center).
            if (frame == 5) { hilt = new(21, 25); tip = new(19, 2); }
            else if (frame == 6) { hilt = new(37, 34); tip = new(47, 25); }
            else { hilt = new(33, 42); tip = new(47, 52); }
            Vector2 delta = tip - hilt;
            float leanBack = burst * 2.5f;
            shader.TrySetParameter("shape", new Vector4(swordPower * (reduced ? .60f : 1), 3.17f, 1, 0));
            Ribbon(hilt, hilt + delta * .32f + new Vector2(-leanBack, 0),
                hilt + delta * .76f + new Vector2(leanBack, -2), tip + delta * .12f,
                2.0f + 2.2f * swordPower, 1);
        }

        void Ribbon(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float width, float blade)
        {
            int written = 0;
            for (int k = 0; k < StripSegments; k++)
            {
                float t = (float)k / StripSegments, next = (float)(k + 1) / StripSegments;
                var l = Vertex(t, -1); var r = Vertex(t, 1);
                var nl = Vertex(next, -1); var nr = Vertex(next, 1);
                strip[written++] = l; strip[written++] = r; strip[written++] = nl;
                strip[written++] = r; strip[written++] = nr; strip[written++] = nl;
            }
            shader.Apply("RibbonPass"); Submit(strip, written);
            VertexPositionColorTexture Vertex(float t, float side)
            {
                float s = 1 - t;
                Vector2 p = s * s * s * a + 3 * s * s * t * b + 3 * s * t * t * c + t * t * t * d;
                Vector2 tangent = 3 * s * s * (b - a) + 6 * s * t * (c - b) + 3 * t * t * (d - c);
                if (tangent.LengthSquared() < .001f) tangent = Vector2.UnitY;
                tangent.Normalize();
                Vector2 normal = new(-tangent.Y, tangent.X);
                float taper = blade > .5f ? .45f + .55f * MathF.Sin(t * MathF.PI)
                    : MathF.Sin(t * MathF.PI);
                return new(new(Map(p + normal * side * width * taper), 0), Color.White,
                    new Vector2(t, (side + 1) * .5f));
            }
        }
    }

    private static void Submit(VertexPositionColorTexture[] vertices, int count)
        => Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count / 3);
}
