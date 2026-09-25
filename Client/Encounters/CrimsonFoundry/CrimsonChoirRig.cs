#nullable enable
using System;
using Convergence.Client.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// The original organic apparition, skinned at shoulders/elbows/wrists. Its
// emissive anatomy, living wing membranes and vascular heart are separate
// Luminance passes. No whole-image armor puppet or gameplay-side animation.
internal static class CrimsonChoirRig
{
    private const int Columns = 48, Rows = 56, Segments = 28;
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[Columns * Rows * 6];
    private static readonly VertexPositionColorTexture[] strip = new VertexPositionColorTexture[Segments * 6];
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static readonly VertexPositionColorTexture[] grid = new VertexPositionColorTexture[(Columns + 1) * (Rows + 1)];
    private static readonly CrimsonChoirArm[] arms = new CrimsonChoirArm[4];
    private static Texture2D? body;

    internal static void Load()
    {
        if (!Main.dedServ)
            body = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/ThornChoir", AssetRequestMode.ImmediateLoad).Value;
    }
    internal static void Unload() => body = null; // ModContent owns the texture; no owned GPU surface.

    internal static void Draw(SpriteBatch batch, Vector2 center, float height, float age, float charge,
        float recoil, float alpha, bool flipped, float rotation = 0, float dissolve = 0,
        float melt = 0, bool armsOnly = false, ReadOnlySpan<CrimsonChoirCue> cues = default)
    {
        if (Main.dedServ || body is not { } texture || alpha <= .001f || height <= 0 || dissolve >= 1) return;
        bool reduced = CrimsonVisuals.Reduced;
        float scale = height / CrimsonChoirMotion.Canvas;
        float bodyAngle = rotation + MathF.Sin(age * .014f) * .026f;
        float exposure = reduced ? .52f : 1;
        Vector2 root = center - Main.screenPosition + new Vector2(0, MathF.Sin(age * .027f) * 3 + melt * 150 * scale);
        float power = 0, burst = 0;
        for (int i = 0; i < 4; i++)
        {
            arms[i] = CrimsonChoirMotion.Arm(i, age, charge, recoil, cues);
            power = Math.Max(power, arms[i].Power); burst = Math.Max(burst, arms[i].Burst);
        }
        float pulse = MathF.Pow(.5f + .5f * MathF.Sin(age * .105f), 5);
        Vector2 heart = CrimsonChoirMotion.Heart + new Vector2(MathF.Sin(age * .022f) * 9, MathF.Sin(age * .034f) * 8);
        var shader = ShaderManager.GetShader("Convergence.ScarletChoir");
        using var scope = new WorldGraphicsScope(batch);
        shader.TrySetParameter("uWorldViewProjection", ScarletMaterials.WorldMatrix);
        shader.TrySetParameter("clock", age / 60);
        shader.TrySetParameter("signal", new Vector4(power, burst, alpha, exposure));
        shader.TrySetParameter("ceremony", new Vector2(dissolve, melt));
        shader.TrySetParameter("armsOnly", armsOnly ? 1f : 0f);
        shader.SetTexture(texture, 0, SamplerState.LinearClamp);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);

        // Wing roots sit behind the ribs, not in a circular HUD halo. The torn
        // energetic membranes lag behind the faster foreground hand action.
        if (!armsOnly)
            for (int side = -1; side <= 1; side += 2)
                for (int k = reduced ? 2 : 0; k < 6; k++)
                {
                    int arm = side < 0 ? 0 : 1;
                    float tension = arms[arm].Power, release = arms[arm].Burst;
                    float lag = MathF.Sin(age * (.018f + k * .002f) + k * 1.5f + side);
                    float open = 1 + tension * .28f + release * .24f;
                    Vector2 start = new(640 + side * 72, 376 + k * 30);
                    Vector2 tip = new(640 + side * (440 + k * 27 + side * 28) * open,
                        70 + k * 113 + lag * 39 + side * 37 - release * (80 - k * 18));
                    Vector2 a = start + new Vector2(side * (230 + k * 21), -260 + k * 42);
                    Vector2 b = tip + new Vector2(-side * 125, -140 + k * 24);
                    Ribbon(start, a, b, tip, 72 + k * 12 + tension * 42, k * 1.73f + side, .70f * exposure * (1 - dissolve), 0);
                    if (!reduced) Ribbon(start, a, b, tip, 5 + tension * 6, k * 1.73f + side, .7f * (1 - dissolve), 1);
                }

        // Bone hands emerge from broad translucent flame sleeves, not merely
        // a colored outline. These sleeves articulate with the same wrist.
        for (int arm = 0; arm < 4; arm++)
        {
            var pose = arms[arm];
            Vector2 elbow = CrimsonChoirMotion.Elbow(arm, pose), wrist = CrimsonChoirMotion.Wrist(arm, pose);
            Vector2 tip = CrimsonChoirMotion.Tip(arm, pose);
            Ribbon(elbow, Vector2.Lerp(elbow, wrist, .55f), wrist, tip + (tip - wrist) * .85f,
                58 + pose.Power * 66 + pose.Burst * 48, arm * 3.7f + 4,
                (.5f + pose.Power * .55f + pose.Burst * .4f) * exposure * (1 - dissolve), 0);
        }

        // Source-space skin preserves authored negative space, without rigid
        // rectangular cutout seams. Cloth follows slower than the bones.
        for (int y = 0; y <= Rows; y++)
            for (int x = 0; x <= Columns; x++)
            {
                Vector2 uv = new((float)x / Columns, (float)y / Rows), p = uv * CrimsonChoirMotion.Canvas;
                Vector2 delta = Vector2.Zero;
                float total = 0, localPower = 0, localBurst = 0;
                for (int arm = 0; arm < 4; arm++)
                {
                    float weight = CrimsonChoirMotion.Weight(p, arm);
                    if (weight <= 0) continue;
                    delta += (CrimsonChoirMotion.Skin(p, arm, arms[arm]) - p) * weight;
                    total += weight; localPower += arms[arm].Power * weight; localBurst += arms[arm].Burst * weight;
                }
                if (total > 1) delta /= total;
                float cloth = CrimsonChoirMotion.Ease((uv.Y - .43f) / .43f) * (1 - Math.Min(1, total));
                Vector2 deform = new(MathF.Sin(age * .026f - uv.Y * 12 + uv.X * 7) * 43 * cloth,
                    MathF.Sin(age * .038f + uv.X * 14) * 15 * cloth);
                float heartWeight = MathF.Exp(-Vector2.DistanceSquared(p, CrimsonChoirMotion.Heart) / 4800);
                deform += (p - CrimsonChoirMotion.Heart) * heartWeight * (.09f * pulse + .13f * power);
                deform.X *= 1 - melt * .65f;
                Vector2 position = Map(p + delta + deform);
                // R = anatomy, G/B = this arm's accepted excitation/discharge.
                grid[y * (Columns + 1) + x] = new(new(position, 0),
                    new Color(Math.Min(1, total), Math.Clamp(localPower, 0, 1), Math.Clamp(localBurst, 0, 1), 1f), uv);
            }
        int written = 0;
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Columns; x++)
            {
                int a = y * (Columns + 1) + x, b = a + 1, c = a + Columns + 1, d = c + 1;
                mesh[written++] = grid[a]; mesh[written++] = grid[b]; mesh[written++] = grid[c];
                mesh[written++] = grid[b]; mesh[written++] = grid[d]; mesh[written++] = grid[c];
            }
        shader.Apply("AuraPass"); DrawMesh(mesh, written);
        shader.Apply("AutoloadPass"); DrawMesh(mesh, written);

        for (int arm = 0; arm < 4; arm++)
        {
            var pose = arms[arm];
            Vector2 elbow = CrimsonChoirMotion.Elbow(arm, pose), wrist = CrimsonChoirMotion.Wrist(arm, pose), tip = CrimsonChoirMotion.Tip(arm, pose);
            Vector2 shoulder = CrimsonChoirMotion.Shoulders[arm];
            // Flow follows the actual joint chain, from the heart to the hand.
            Ribbon(heart, shoulder, elbow, wrist, 9 + pose.Power * 13 + pose.Burst * 18,
                arm * 2.31f, (.30f + pose.Power * .48f + pose.Burst * .45f) * exposure * (1 - dissolve), 1);
            if (!reduced)
            {
                var past = CrimsonChoirMotion.Arm(arm, age - 6, charge, recoil, cues);
                var older = CrimsonChoirMotion.Arm(arm, age - 13, charge, recoil, cues);
                Vector2 end = CrimsonChoirMotion.Tip(arm, older);
                if (Vector2.DistanceSquared(tip, end) > 80)
                    Ribbon(tip, CrimsonChoirMotion.Tip(arm, past), end, end + new Vector2((arm % 2 == 0 ? -1 : 1) * 30, 38),
                        24 + pose.Burst * 28, arm + 8, (.14f + pose.Burst * .6f) * (1 - dissolve), 0);
            }
            for (int finger = 0; finger < (reduced ? 1 : 3); finger++)
            {
                float side = arm % 2 == 0 ? -1 : 1;
                Vector2 start = Vector2.Lerp(wrist, tip, .65f + finger * .15f);
                Vector2 end = start + new Vector2(side * (24 + finger * 27) + MathF.Sin(age * .037f + arm + finger) * 23,
                    (arm < 2 ? -1 : 1) * (65 + finger * 26 + pose.Power * 42));
                Ribbon(start, start + new Vector2(side * 42, -15), end - new Vector2(0, 25), end,
                    12 + pose.Power * 16 + pose.Burst * 18, arm * 3 + finger,
                    (.33f + pose.Power * .30f + pose.Burst * .25f) * exposure * (1 - dissolve), 0);
            }
        }
        if (!armsOnly)
        {
            shader.TrySetParameter("shape", new Vector4(pulse, power, burst, 0));
            Patch(heart, new(235 + power * 60 + burst * 90, 260 + power * 70), "HeartPass");
            if (!reduced)
                for (int i = 0; i < 18; i++)
                {
                    float life = (age * (.0035f + i % 3 * .0006f) + i * .618034f) % 1;
                    int arm = i % 4;
                    Vector2 wrist = CrimsonChoirMotion.Wrist(arm, arms[arm]);
                    Vector2 origin = Vector2.Lerp(heart, wrist, life);
                    float pathWave = MathF.Sin(life * MathF.PI);
                    origin += new Vector2(MathF.Sin(i * 4.7f + life * 6) * 35, MathF.Cos(i * 3.1f + life * 4) * 45) * pathWave;
                    float opacity = pathWave * (.28f + arms[arm].Power * .5f) * (1 - dissolve);
                    shader.TrySetParameter("shape", new Vector4(opacity, i, 0, 0));
                    Patch(origin, new(6 + power * 8, 16 + power * 14), "SparkPass");
                }
        }

        Vector2 Map(Vector2 p)
        {
            p -= new Vector2(640);
            if (flipped) p.X = -p.X;
            return root + CrimsonChoirMotion.Rotate(p, bodyAngle) * scale;
        }
        void Ribbon(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float radius, float seed, float opacity, float filament)
        {
            if (opacity <= .001f) return;
            shader.TrySetParameter("shape", new Vector4(seed, opacity, filament, 0));
            int n = 0;
            for (int k = 0; k < Segments; k++)
            {
                float t = (float)k / Segments, next = (float)(k + 1) / Segments;
                var l = Vertex(t, -1); var r = Vertex(t, 1);
                var nl = Vertex(next, -1); var nr = Vertex(next, 1);
                strip[n++] = l; strip[n++] = r; strip[n++] = nl;
                strip[n++] = r; strip[n++] = nr; strip[n++] = nl;
            }
            shader.Apply("RibbonPass"); DrawMesh(strip, n);
            VertexPositionColorTexture Vertex(float t, float side)
            {
                float s = 1 - t;
                Vector2 p = s * s * s * a + 3 * s * s * t * b + 3 * s * t * t * c + t * t * t * d;
                Vector2 tangent = 3 * s * s * (b - a) + 6 * s * t * (c - b) + 3 * t * t * (d - c);
                if (tangent.LengthSquared() < .01f) tangent = new(0, 1);
                tangent.Normalize();
                Vector2 normal = new(-tangent.Y, tangent.X);
                float taper = (.16f + .84f * MathF.Sin(t * MathF.PI)) * (1 - CrimsonChoirMotion.Ease((t - .85f) / .15f));
                return new(new(Map(p + normal * side * radius * taper), 0), Color.White, new(t, (side + 1) * .5f));
            }
        }
        void Patch(Vector2 at, Vector2 halfSize, string pass)
        {
            var a = new VertexPositionColorTexture(new(Map(at - halfSize), 0), Color.White, new(0, 0));
            var b = new VertexPositionColorTexture(new(Map(at + new Vector2(halfSize.X, -halfSize.Y)), 0), Color.White, new(1, 0));
            var c = new VertexPositionColorTexture(new(Map(at + new Vector2(-halfSize.X, halfSize.Y)), 0), Color.White, new(0, 1));
            var d = new VertexPositionColorTexture(new(Map(at + halfSize), 0), Color.White, new(1, 1));
            quad[0] = a; quad[1] = b; quad[2] = c; quad[3] = b; quad[4] = d; quad[5] = c;
            shader.Apply(pass); DrawMesh(quad, 6);
        }
    }
    private static void DrawMesh(VertexPositionColorTexture[] vertices, int count)
        => Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count / 3);
}
