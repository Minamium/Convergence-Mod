#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
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
//
// Its attacks (design §2.3) answer only the plans' Born/Fire/End. Motion, for every viewer of an Act body: the
// torso heaves down with the slam (`heave`, effigy only), each wing opens with the stronger of its own side's
// arms, the flame fingers turn and reach with the striking hand, and the afterimage follows the hand's speed
// inside its strike windows. Light (`material`): the heart sends a blood front down the striking arm, inside the
// painted limb, that reaches the fingertips exactly on Fire, the fingertips ignite, the sleeves shrink and warm, then
// tear open, the heart's core beats the slam (its surge) over the painted torso and the blood returns. Reduced
// Effects keeps every vertex and the light's timing (ScarletChoir.fx quiets it). With no cue and no material every
// pass draws the accepted picture; armsOnly (the Final avatar) keeps it too.
internal static class CrimsonChoirRig
{
    private const int Columns = 48, Rows = 56, Segments = 28, Sparks = 18;
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[Columns * Rows * 6];
    private static readonly VertexPositionColorTexture[] strip = new VertexPositionColorTexture[Segments * 6];
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static readonly VertexPositionColorTexture[] sparks = new VertexPositionColorTexture[Sparks * 6];
    private static readonly VertexPositionColorTexture[] grid = new VertexPositionColorTexture[(Columns + 1) * (Rows + 1)];
    private static readonly CrimsonChoirArm[] arms = new CrimsonChoirArm[4];
    private static readonly ScarletChoirBlood[] blood = new ScarletChoirBlood[4];
    // Rest-skeleton coordinate of every grid vertex: its arm (the strongest skin weight) and how far along that
    // arm's heart -> shoulder -> elbow -> wrist -> fingertip path it lies (0..1). Measured once on Load.
    private static readonly float[] along = new float[(Columns + 1) * (Rows + 1)];
    private static readonly byte[] armOf = new byte[(Columns + 1) * (Rows + 1)];
    private static Texture2D? body;

    internal static void Load()
    {
        if (Main.dedServ) return;
        body = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/ThornChoir", AssetRequestMode.ImmediateLoad).Value;
        MeasureSkeleton();
    }
    internal static void Unload() => body = null; // ModContent owns the texture; no owned GPU surface.

    internal static void Draw(SpriteBatch batch, Vector2 center, float height, float age, float charge,
        float recoil, float alpha, bool flipped, float rotation = 0, float dissolve = 0,
        float melt = 0, bool armsOnly = false, ReadOnlySpan<CrimsonChoirCue> cues = default,
        Matrix? projection = null, Vector2? screenOrigin = null, float heave = 0, in Vfx.ScarletChoirState material = default)
    {
        if (Main.dedServ || body is not { } texture || alpha <= .001f || height <= 0 || dissolve >= 1) return;
        bool reduced = CrimsonVisuals.Reduced;
        float scale = height / CrimsonChoirMotion.Canvas;
        float bodyAngle = rotation + MathF.Sin(age * .014f) * .026f;
        float exposure = reduced ? .52f : 1;
        // The arms come first: the torso heaves down with the slam (heave is 0 outside the Act effigy).
        float power = 0, burst = 0;
        for (int i = 0; i < 4; i++)
        {
            arms[i] = CrimsonChoirMotion.Arm(i, age, charge, recoil, cues);
            power = Math.Max(power, arms[i].Power); burst = Math.Max(burst, arms[i].Burst);
        }
        Vector2 root = center - (screenOrigin ?? Main.screenPosition) + new Vector2(0, MathF.Sin(age * .027f) * 3 + melt * 150 * scale + heave * burst);
        // Motion the cues drive; light from the body's own notes (`material`).
        bool attacking = !armsOnly && !cues.IsEmpty;
        bool lit = material.Active && !armsOnly;
        for (int i = 0; i < 4; i++) blood[i] = lit ? ScarletChoirBlood.Of(cues, i, age, material.Heat) : default;
        float free = MathF.Pow(.5f + .5f * MathF.Sin(age * .105f), 5), pulse = free, throb = free;
        // The free heartbeat is handed to the attack clock inside a strike window (no pop at its edges). The heart's
        // light takes the strike as the material gives it (halved under Reduced Effects). The mesh's throb is motion:
        // it follows the attack clock whenever the notes give one (even with the light off, Active false) and takes the
        // full strike in both modes, so Reduced Effects never moves a vertex.
        if (lit) pulse = Math.Max(free * (1 - material.Engaged), material.Ignite);
        if (!armsOnly && (material.Engaged > 0 || material.Ignite > 0))
            throb = Math.Max(free * (1 - material.Engaged), material.Ignite / (reduced ? ScarletBodyMaterial.ReducedIgnite : 1));
        Vector2 heart = CrimsonChoirMotion.Heart + new Vector2(MathF.Sin(age * .022f) * 9, MathF.Sin(age * .034f) * 8);
        var shader = ShaderManager.GetShader("Convergence.ScarletChoir");
        using var scope = new WorldGraphicsScope(batch);
        shader.TrySetParameter("uWorldViewProjection", projection ?? ScarletMaterials.WorldMatrix);
        shader.TrySetParameter("clock", age / 60);
        shader.TrySetParameter("signal", new Vector4(power, burst, alpha, exposure));
        shader.TrySetParameter("ceremony", new Vector2(dissolve, melt));
        shader.TrySetParameter("armsOnly", armsOnly ? 1f : 0f);
        // Always written: the shader is shared with the Final avatar and the free apparitions.
        shader.TrySetParameter("attack", lit ? new Vector4(material.Heat, material.Ignite, material.Drain, material.Surge) : Vector4.Zero);
        shader.SetTexture(texture, 0, SamplerState.LinearClamp);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);

        // Wing roots sit behind the ribs, not in a circular HUD halo. The torn
        // energetic membranes lag behind the faster foreground hand action. Each
        // side's membrane answers the stronger of that side's upper and lower arm.
        if (!armsOnly)
            for (int side = -1; side <= 1; side += 2)
                for (int k = reduced ? 2 : 0; k < 6; k++)
                {
                    int upper = side < 0 ? 0 : 1, lower = upper + 2;
                    float tension = Math.Max(arms[upper].Power, arms[lower].Power), release = Math.Max(arms[upper].Burst, arms[lower].Burst);
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
        // a colored outline. These sleeves articulate with the same wrist. A
        // striking arm's sleeve tears open down its middle on the slam so the
        // bone shows through; its outline and colour stay the accepted ones,
        // so nothing changes past the body.
        for (int arm = 0; arm < 4; arm++)
        {
            var pose = arms[arm];
            var limb = lit ? material.Limb(arm) : default;
            Vector2 elbow = CrimsonChoirMotion.Elbow(arm, pose), wrist = CrimsonChoirMotion.Wrist(arm, pose);
            Vector2 tip = CrimsonChoirMotion.Tip(arm, pose);
            Ribbon(elbow, Vector2.Lerp(elbow, wrist, .55f), wrist, tip + (tip - wrist) * .85f,
                58 + pose.Power * 66 + pose.Burst * 48, arm * 3.7f + 4,
                (.5f + pose.Power * .55f + pose.Burst * .4f) * exposure * (1 - dissolve), 0, lit ? 1 + limb.Tear : 0);
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
                deform += (p - CrimsonChoirMotion.Heart) * heartWeight * (.09f * throb + .13f * power);
                deform.X *= 1 - melt * .65f;
                Vector2 position = Map(p + delta + deform);
                int index = y * (Columns + 1) + x;
                // R = anatomy, G/B = this arm's accepted excitation/discharge, A = the blood front (0 = none).
                float front = lit ? blood[armOf[index]].At(along[index]) : 0;
                grid[index] = new(new(position, 0),
                    new Color(Math.Min(1, total), Math.Clamp(localPower, 0, 1), Math.Clamp(localBurst, 0, 1), front), uv);
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
            // Flow follows the actual joint chain, from the heart to the hand. It stays the accepted flow: the sent
            // blood runs inside the painted limb (vertex alpha), never as a bead along this thin line.
            Ribbon(heart, shoulder, elbow, wrist, 9 + pose.Power * 13 + pose.Burst * 18,
                arm * 2.31f, (.30f + pose.Power * .48f + pose.Burst * .45f) * exposure * (1 - dissolve), 1);
            if (!reduced)
            {
                var past = CrimsonChoirMotion.Arm(arm, age - 6, charge, recoil, cues);
                var older = CrimsonChoirMotion.Arm(arm, age - 13, charge, recoil, cues);
                Vector2 end = CrimsonChoirMotion.Tip(arm, older), previous = CrimsonChoirMotion.Tip(arm, past);
                // The afterimage shows how fast the hand travels, only inside its own strike windows.
                float trace = attacking
                    ? ScarletChoirBlood.Window(cues, arm, age) * .74f * CrimsonChoirMotion.Ease((Vector2.Distance(tip, previous) / 6 - 4) / 22)
                    : .14f + pose.Burst * .6f;
                if (Vector2.DistanceSquared(tip, end) > 80)
                    Ribbon(tip, previous, end, end + new Vector2((arm % 2 == 0 ? -1 : 1) * 30, 38),
                        24 + pose.Burst * 28, arm + 8, trace * (1 - dissolve), 0);
            }
            // The flame fingers turn with the hand's strike (its rotation away from
            // the idle sway) and reach 30% further on the slam.
            float turn = 0, reach = 1;
            if (attacking)
            {
                var rest = CrimsonChoirMotion.Arm(arm, age, 0, 0, default);
                turn = pose.Shoulder + pose.Elbow + pose.Wrist - (rest.Shoulder + rest.Elbow + rest.Wrist);
                reach = 1 + .3f * pose.Burst;
            }
            for (int finger = 0; finger < (reduced ? 1 : 3); finger++)
            {
                float side = arm % 2 == 0 ? -1 : 1;
                Vector2 start = Vector2.Lerp(wrist, tip, .65f + finger * .15f);
                Vector2 offset = new(side * (24 + finger * 27) + MathF.Sin(age * .037f + arm + finger) * 23,
                    (arm < 2 ? -1 : 1) * (65 + finger * 26 + pose.Power * 42));
                Vector2 bend = new(side * 42, -15), hook = new(0, 25);
                if (attacking)
                {
                    offset = CrimsonChoirMotion.Rotate(offset, turn) * reach;
                    bend = CrimsonChoirMotion.Rotate(bend, turn) * reach;
                    hook = CrimsonChoirMotion.Rotate(hook, turn) * reach;
                }
                Vector2 end = start + offset;
                Ribbon(start, start + bend, end - hook, end,
                    12 + pose.Power * 16 + pose.Burst * 18, arm * 3 + finger,
                    (.33f + pose.Power * .30f + pose.Burst * .25f) * exposure * (1 - dissolve), 0);
            }
        }
        if (!armsOnly)
        {
            // The heart's core draws in through the warning, beats on the attack clock and throbs with each slam
            // (both arms' discharge, its surge), over the painted torso only (heartArea); the outer glow and the patch
            // keep their accepted values. Slams on the two inner quarters (the lower arms alone) neither swell nor
            // contract the core (`sized`): it sits over the central gap between their claws, so it only lights.
            float sized = 1, lift = 0;
            if (lit)
            {
                float outer = 0, inner = 0;
                for (int arm = 0; arm < 4; arm++)
                {
                    var limb = material.Limb(arm);
                    float drive = Math.Max(limb.Lift, limb.Burst);
                    if (arm < 2) outer = Math.Max(outer, drive); else inner = Math.Max(inner, drive);
                    lift = Math.Max(lift, limb.Lift);
                }
                sized = outer >= inner ? 1 : outer / inner;
            }
            shader.TrySetParameter("shape", new Vector4(free, power, burst, 0));
            // The core draws in by up to 8% through the warning and swells with the slam, more for a pair of arms
            // (the surge): a throb in size, never a brighter flash than the accepted one.
            float swell = sized * Math.Min(1, material.Surge / ScarletBodyMaterial.SurgeLimit);
            shader.TrySetParameter("heart", lit ? new Vector4(1, pulse, burst * sized, .08f * sized * lift - .05f * swell) : Vector4.Zero);
            Vector2 halfSize = new(235 + power * 60 + burst * 90, 260 + power * 70);
            shader.TrySetParameter("heartArea", new Vector4(heart.X, heart.Y, halfSize.X, halfSize.Y) / CrimsonChoirMotion.Canvas);
            Patch(heart, halfSize, "HeartPass");
            if (!reduced)
            {
                // Free sparks run heart -> wrist in one draw; a participant's strike window hands them to the attack.
                float hand = lit ? 1 - material.Engaged : 1;
                Vector2 half = new(6 + power * 8, 16 + power * 14);
                int count = 0;
                for (int i = 0; i < Sparks; i++)
                {
                    float life = (age * (.0035f + i % 3 * .0006f) + i * .618034f) % 1;
                    int arm = i % 4;
                    Vector2 wrist = CrimsonChoirMotion.Wrist(arm, arms[arm]);
                    Vector2 origin = Vector2.Lerp(heart, wrist, life);
                    float pathWave = MathF.Sin(life * MathF.PI);
                    origin += new Vector2(MathF.Sin(i * 4.7f + life * 6) * 35, MathF.Cos(i * 3.1f + life * 4) * 45) * pathWave;
                    float opacity = pathWave * (.28f + arms[arm].Power * .5f) * (1 - dissolve) * hand;
                    if (opacity <= 0) continue;
                    var tint = new Color(1f, 1f, 1f, Math.Min(1, opacity));
                    var a = new VertexPositionColorTexture(new(Map(origin - half), 0), tint, new(0, 0));
                    var b = new VertexPositionColorTexture(new(Map(origin + new Vector2(half.X, -half.Y)), 0), tint, new(1, 0));
                    var c = new VertexPositionColorTexture(new(Map(origin + new Vector2(-half.X, half.Y)), 0), tint, new(0, 1));
                    var d = new VertexPositionColorTexture(new(Map(origin + half), 0), tint, new(1, 1));
                    sparks[count++] = a; sparks[count++] = b; sparks[count++] = c;
                    sparks[count++] = b; sparks[count++] = d; sparks[count++] = c;
                }
                if (count > 0)
                {
                    shader.TrySetParameter("shape", new Vector4(1, 0, 0, 0));
                    shader.Apply("SparkPass"); DrawMesh(sparks, count);
                }
            }
        }

        Vector2 Map(Vector2 p)
        {
            p -= new Vector2(640);
            if (flipped) p.X = -p.X;
            return root + CrimsonChoirMotion.Rotate(p, bodyAngle) * scale;
        }
        // w: a sleeve's tear w - 1 (0 = none).
        void Ribbon(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float radius, float seed, float opacity, float filament, float w = 0)
        {
            if (opacity <= .001f) return;
            shader.TrySetParameter("shape", new Vector4(seed, opacity, filament, w));
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

    // Each grid vertex's arm (strongest skin weight) and its position along that arm's rest skeleton, heart ->
    // shoulder -> elbow -> wrist -> fingertip (0..1), so the blood front is a vertex lookup per frame.
    private static void MeasureSkeleton()
    {
        Span<Vector2> chain = stackalloc Vector2[5];
        Span<float> length = stackalloc float[5];
        for (int y = 0; y <= Rows; y++)
            for (int x = 0; x <= Columns; x++)
            {
                Vector2 p = new Vector2((float)x / Columns, (float)y / Rows) * CrimsonChoirMotion.Canvas;
                int best = 0; float strongest = -1;
                for (int arm = 0; arm < 4; arm++)
                {
                    float weight = CrimsonChoirMotion.Weight(p, arm);
                    if (weight > strongest) { strongest = weight; best = arm; }
                }
                Chain(best, chain, length);
                float nearest = float.MaxValue, at = 0;
                for (int k = 0; k < 4; k++)
                {
                    Vector2 segment = chain[k + 1] - chain[k];
                    float t = Math.Clamp(Vector2.Dot(p - chain[k], segment) / segment.LengthSquared(), 0, 1);
                    float distance = Vector2.DistanceSquared(p, chain[k] + segment * t);
                    if (distance < nearest) { nearest = distance; at = length[k] + (length[k + 1] - length[k]) * t; }
                }
                int index = y * (Columns + 1) + x;
                armOf[index] = (byte)best;
                along[index] = at / length[4];
            }

        static void Chain(int arm, Span<Vector2> chain, Span<float> length)
        {
            chain[0] = CrimsonChoirMotion.Heart; chain[1] = CrimsonChoirMotion.Shoulders[arm];
            chain[2] = CrimsonChoirMotion.Elbows[arm]; chain[3] = CrimsonChoirMotion.Wrists[arm]; chain[4] = CrimsonChoirMotion.Tips[arm];
            length[0] = 0;
            for (int k = 1; k < 5; k++) length[k] = length[k - 1] + Vector2.Distance(chain[k - 1], chain[k]);
        }
    }

    private static void DrawMesh(VertexPositionColorTexture[] vertices, int count)
        => Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count / 3);
}
