#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.EbonManor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.EbonManor;

// Atlas cells (all face right): 0 idle, 1 idle sway, 2 float, 3 glide,
// 4 cast, 5 pull, 6 parasol, 7 command.
internal readonly record struct NoirettePose(int Frame, float Weave, float Lean, Vector2 Offset, float Flash, int Facing,
    float Opacity, Vector2 Root, bool Hidden);

// Noirette keeps her authored 48x64 pixel art at the 2x Terraria pixel size.
// A deforming mesh adds lagging twin tails and hem; the pose, lean, weave and
// every thread anchor come from the accepted clock. No hitbox is involved.
internal static class EbonNoirette
{
    internal const float Scale = 2;
    private const int Columns = 12, Rows = 16;
    // Logical pixel under NPC.Center, per the measured body (dress centre, mid torso).
    private static readonly Vector2 Pivot = new(26, 33);
    private static readonly VertexPositionColorTexture[] grid = new VertexPositionColorTexture[(Columns + 1) * (Rows + 1)];
    private static readonly VertexPositionColorTexture[] skin = new VertexPositionColorTexture[Columns * Rows * 6];
    // Per-frame tail masks (x edge, top, bottom): tails hang behind her left side.
    private static readonly Vector3[] Tails =
    {
        new(20, 8, 40), new(20, 8, 40), new(20, 8, 40), new(20, 8, 40),
        new(16, 10, 38), new(13, 8, 40), new(18, 22, 44), new(20, 8, 40),
    };
    // Measured fingertips (logical px) for thread anchors.
    private static readonly Vector2[] Hands =
    {
        new(30, 36), new(30, 36), new(31, 36), new(30, 35),
        new(44, 9), new(15, 28), new(31, 27), new(44, 22),
    };
    private static readonly Vector2 SecondCast = new(45, 13);

    // Secondary motion, advanced once per game update from the drawn root.
    private static Vector2 tail, tailVelocity, hem, hemVelocity, lastRoot;
    private static bool initialized;
    internal static void Reset() { tail = tailVelocity = hem = hemVelocity = Vector2.Zero; initialized = false; }
    internal static void Update(Vector2 root)
    {
        if (!initialized || Vector2.DistanceSquared(root, lastRoot) > 260 * 260) { Reset(); initialized = true; lastRoot = root; return; }
        Vector2 motion = Vector2.Clamp(root - lastRoot, new(-24), new(24)); lastRoot = root;
        // Damped springs lag behind the body: tails swing longer than the hem.
        tailVelocity += -tail * .045f - tailVelocity * .09f - motion * .42f;
        hemVelocity += -hem * .10f - hemVelocity * .18f - motion * .30f;
        tail = Vector2.Clamp(tail + tailVelocity, new(-9), new(9));
        hem = Vector2.Clamp(hem + hemVelocity, new(-3), new(3));
    }

    internal static Vector2 Hand(in NoirettePose pose, int index = 0)
    {
        var local = pose.Frame == 4 && index == 1 ? SecondCast : Hands[Math.Clamp(pose.Frame, 0, 7)];
        return Map(pose, local);
    }
    private static Vector2 Map(in NoirettePose pose, Vector2 local)
    {
        Vector2 p = (local - Pivot) * Scale;
        if (pose.Facing < 0) p.X = -p.X;
        float lean = pose.Lean * pose.Facing;
        return pose.Root + pose.Offset + new Vector2(p.X * MathF.Cos(lean) - p.Y * MathF.Sin(lean), p.X * MathF.Sin(lean) + p.Y * MathF.Cos(lean));
    }

    // Her position is a pure function of the accepted clock (the runtime's Move),
    // so every client glides smoothly between the six-tick NPC syncs.
    internal static Vector2 Root(EbonBoss boss, float age)
    {
        var s = boss.State;
        if (s.Fight == Guid.Empty || s.MaxLife <= 0) return boss.NPC.Center;
        float at = s.EndAt >= 0 ? Math.Min(age, s.EndAt - 1) : age;
        var p = EbonRules.BossPosition(s.Field, s.Stage >= EbonStage.Performance ? s.Epoch : int.MaxValue, at);
        return new(p.X, p.Y);
    }

    // One accepted-clock pose for this frame; shared by body and threads.
    internal static NoirettePose Resolve(EbonBoss boss, float age)
    {
        var s = boss.State;
        int facing = boss.NPC.spriteDirection < 0 ? -1 : 1;
        Vector2 root = Root(boss, age);
        if (s.Stage is EbonStage.Deployment or EbonStage.Ready || s.MusicStart < 0 || age < s.MusicStart)
            return new(2, 0, 0, Vector2.Zero, 0, facing, 0, root, true);
        if (s.Stage == EbonStage.Countdown || age < s.UnlockAt)
        {
            // Woven in through the intro's near-silent break, the parasol opens on the
            // second build, she draws the silk in as the bass returns and points
            // through the two held beats before the drop.
            float b = EbonIntro.Bars(s, age);
            if (b < EbonIntro.Break) return new(2, 0, 0, Vector2.Zero, 0, facing, 0, root, true);
            float weave = EbonVisualsMath.Ease(b - EbonIntro.Break);
            int frame = b < EbonIntro.Bloom ? 2 : b < EbonIntro.Gather ? 6 : b < EbonIntro.Hush ? 4 : 7;
            float draw = EbonVisualsMath.Ease((b - EbonIntro.Gather) * 2);
            float lean = frame == 6 ? MathF.Sin(age * .09f) * .05f : frame == 4 ? -.05f * draw : frame == 7 ? -.04f : 0;
            float flash = frame == 7 ? EbonVisualsMath.Pulse(age - EbonIntro.Tick(s, EbonIntro.Hush), 10) : 0;
            return new(frame, weave, lean, new(0, -10 * (1 - weave) - (frame == 4 ? 4 * draw : 0)), flash, facing, 1, root, false);
        }
        if (s.EndAt >= 0)
        {
            float t = age - s.EndAt;
            if (s.Stage == EbonStage.Victory)
            {
                // A held recoil, then the silk unravels upward.
                float unravel = EbonVisualsMath.Ease((t - 150) / 170);
                int frame = t < 24 ? 5 : 2;
                float lean = t < 24 ? -.12f * EbonVisualsMath.Pulse(t, 14) : MathF.Sin(t * .02f) * .03f;
                return new(frame, 1 - unravel, lean, new(0, -46 * unravel), t < 24 ? EbonVisualsMath.Pulse(t, 8) : 0,
                    facing, 1 - EbonVisualsMath.Ease((t - 300) / 60), root, false);
            }
            // Defeat: she settles her parasol and curtsies before fading.
            float bow = EbonVisualsMath.Ease((t - 40) / 50) * (1 - EbonVisualsMath.Ease((t - 150) / 60));
            return new(0, 1, .10f * bow, new(0, 4 * bow), 0, facing, 1 - EbonVisualsMath.Ease((t - 210) / 80), root, false);
        }
        if (s.Transition(age))
        {
            float t = age - s.PhaseAt, lead = EbonRules.Lead(s.Phase);
            float weave = EbonVisualsMath.Ease((t - 16) / 30);
            int frame = t < 46 ? 2 : t < lead - 58 ? 6 : 7;
            float lean = frame == 6 ? MathF.Sin(age * .09f) * .05f : frame == 7 ? -.04f : 0;
            float flash = frame == 7 ? EbonVisualsMath.Pulse(t - (lead - 58), 10) : 0;
            return new(frame, weave, lean, Vector2.Zero, flash, facing, t < 16 ? 0 : 1, root, false);
        }
        return Combat(boss, age, facing, root);
    }

    private static NoirettePose Combat(EbonBoss boss, float age, int facing, Vector2 root)
    {
        var s = boss.State;
        // The newest accepted hazard drives the action beat; the waltz owns the pose while it spins.
        EbonAttackPlan? action = null;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not EbonAttack a || a.Plan.Fight != s.Fight || !a.TryBoss(out _)) continue;
            var plan = a.Plan;
            bool waltz = plan.Kind == EbonAttackKind.Waltz && age >= plan.Born && age < plan.End;
            bool beat = age >= plan.Born && age < plan.Fire + 20;
            if (!waltz && !beat) continue;
            if (action is not { } current || waltz && current.Kind != EbonAttackKind.Waltz
                || current.Kind != EbonAttackKind.Waltz && plan.Born > current.Born) action = plan;
        }
        float bob = 0;
        if (action is { } h)
        {
            if (h.Kind == EbonAttackKind.Waltz)
            {
                float twirl = age < h.Fire ? EbonVisualsMath.Ease((age - h.Born) / 30) : 1;
                return new(6, 1, MathF.Sin(age * .11f) * .06f * twirl, new(0, bob), 0, facing, 1, root, false);
            }
            float release = age - h.Fire;
            bool command = h.Kind is EbonAttackKind.Chandelier or EbonAttackKind.Shears;
            if (release < 0)
            {
                // Load: a small lift and backward lean; the last beat tightens forward.
                float load = EbonVisualsMath.Ease((age - h.Born) / 14);
                float tighten = EbonVisualsMath.Ease((age - (h.Fire - 8)) / 8);
                float lean = -.05f * load + .07f * tighten;
                return new(command ? 7 : 4, 1, lean, new(0, -3 * load), 0, facing, 1, root, false);
            }
            // Release: arms yanked back with a braked recoil.
            float kick = EbonVisualsMath.Pulse(release, 7);
            return new(5, 1, -.12f * kick, new(-7 * kick * facing, 0), .6f * EbonVisualsMath.Pulse(release, 4), facing, 1, root, false);
        }
        // Between beats: float, with a sway cell each bar; glide while changing station.
        // Between beats she floats; while gliding to the next station she faces her way.
        float bars = (age - s.Epoch) / EbonRules.BarTicks;
        int block = (int)(bars / 4) % EbonRules.Stations.Length;
        float step = EbonRules.Stations[block] - EbonRules.Stations[(block + EbonRules.Stations.Length - 1) % EbonRules.Stations.Length];
        bool moving = bars >= 4 && bars % 4 < 1 && step != 0;
        if (moving) facing = step < 0 ? -1 : 1;
        return new(moving ? 3 : 2, 1, MathF.Sin(age * .03f) * .02f + (moving ? .05f : 0), Vector2.Zero, 0, facing, 1, root, false);
    }

    internal static void Draw(SpriteBatch batch, EbonBoss boss, in NoirettePose pose, float age)
    {
        if (Main.dedServ || pose.Hidden || pose.Opacity <= .003f) return;
        var art = EbonMaterials.Texture("Noirette");
        bool reduced = EbonVisuals.Reduced;
        float motion = reduced ? .3f : 1;
        using var scope = new WorldGraphicsScope(batch);
        var shader = EbonMaterials.Manor(age);
        // Contrast haze and moon ring behind the figure.
        shader.TrySetParameter("signal", new Vector4(pose.Opacity * (.55f + .45f * pose.Weave), .55f + .45f * pose.Flash, reduced ? 1 : 0, boss.NPC.whoAmI * .13f));
        EbonMaterials.Quad(shader, "AuraPass", pose.Root + pose.Offset + new Vector2(0, -6), new Vector2(210, 230), 0);
        var src = new Rectangle(pose.Frame % 4 * 48, pose.Frame / 4 * 64, 48, 64);
        shader.SetTexture(art, 0, SamplerState.PointClamp);
        shader.TrySetParameter("region", new Vector4(src.X / (float)art.Width, src.Y / (float)art.Height, src.Width / (float)art.Width, src.Height / (float)art.Height));
        shader.TrySetParameter("signal", new Vector4(pose.Opacity, pose.Weave, reduced ? 1 : 0, boss.NPC.whoAmI * .29f));
        shader.TrySetParameter("shape", new Vector4(pose.Flash, .30f, 48, 64));
        shader.TrySetParameter("weaveDensity", 9f);
        var mask = Tails[Math.Clamp(pose.Frame, 0, 7)];
        float sway = MathF.Sin(age * .045f) * 1.2f * motion;
        for (int y = 0; y <= Rows; y++)
            for (int x = 0; x <= Columns; x++)
            {
                Vector2 uv = new(x / (float)Columns, y / (float)Rows);
                Vector2 p = uv * new Vector2(48, 64);
                // Tails: grow from the tie point, lag the body, keep the cast arm rigid.
                float wt = EbonVisualsMath.Ease((mask.X - p.X) / 8) * EbonVisualsMath.Ease((p.Y - mask.Y) / 8) * EbonVisualsMath.Ease((mask.Z - p.Y) / 6);
                float reach = MathF.Pow(Math.Clamp((p.Y - mask.Y) / 30, 0, 1), 2);
                p += (new Vector2(tail.X * motion + sway, tail.Y * .35f * motion) * reach) * wt;
                // Hem: petticoat flutter with a short lag; legs stay planted.
                float wh = EbonVisualsMath.Ease((p.Y - 36) / 12) * EbonVisualsMath.Ease((56 - p.Y) / 6);
                p += new Vector2(MathF.Sin(age * .05f - p.Y * .2f + p.X * .1f) * .6f * motion + hem.X * .5f * motion,
                    MathF.Sin(age * .06f + p.X * .3f) * .3f * motion) * wh;
                var world = Map(pose, p) - Main.screenPosition;
                grid[y * (Columns + 1) + x] = new(new(world, 0), Color.White, uv);
            }
        int n = 0;
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Columns; x++)
            {
                int a = y * (Columns + 1) + x, b = a + 1, c = a + Columns + 1, d = c + 1;
                skin[n++] = grid[a]; skin[n++] = grid[b]; skin[n++] = grid[c];
                skin[n++] = grid[b]; skin[n++] = grid[d]; skin[n++] = grid[c];
            }
        shader.Apply("BodyPass");
        Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, skin, 0, n / 3);
    }
}
