#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

internal static class CrimsonRig
{
    private static Texture2D? atlas;
    // Tight UVs measured from the twelve connected alpha silhouettes. The
    // generated sheet is not an exact grid: a few toes/pauldrons cross a cell.
    // Explicit gutters avoid clipping those or sampling the neighbouring limb.
    private static readonly Rectangle[] parts = {
        new(98, 8, 221, 350), new(461, 8, 162, 309),
        new(856, 12, 96, 341), new(1220, 3, 111, 355),
        new(78, 425, 263, 228), new(504, 359, 97, 343),
        new(853, 360, 99, 349), new(1120, 361, 274, 348),
        new(53, 755, 332, 257), new(492, 715, 116, 347),
        new(809, 715, 190, 343), new(1079, 715, 342, 344)
    };
    internal static void Load()
    {
        atlas = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/FoundryRig", AssetRequestMode.ImmediateLoad).Value;
        if (atlas.Width != 1448 || atlas.Height != 1086)
            throw new InvalidOperationException("FoundryRig atlas dimensions no longer match its UV map.");
    }
    internal static void Unload() => atlas = null;
    internal static bool Draw(CrimsonBoss boss, SpriteBatch batch, Vector2 screen)
    {
        if (atlas is null) return false;
        float age = CrimsonVisuals.RenderAge(boss), until = 60, since = 100;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not CrimsonAttack a || a.Hazard.Fight != boss.State.Fight) continue;
            float dt = a.Hazard.Fire - age;
            if (dt >= 0) until = Math.Min(until, dt);
            else since = Math.Min(since, -dt);
        }
        float charge = CrimsonRigMotion.Charge(until), recoil = CrimsonRigMotion.Recoil(since);
        float pulse = boss.State.MusicStart < 0 ? 0 : CrimsonRegistration.Score.Pulse(age - boss.State.MusicStart);
        float purgeAge = boss.State.PurgeTick < 0 ? -1 : age - boss.State.PurgeTick;
        bool fast = purgeAge >= 90;
        float assemble = CrimsonRigMotion.Assemble(age);
        Vector2 root = boss.NPC.Center - new Vector2(0, 43 + (1 - assemble) * 130);
        float lean = Math.Clamp(boss.NPC.velocity.X * .010f, -.48f, .48f) + MathF.Sin(age * .025f) * .028f;
        float alpha = assemble;
        if (boss.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat) alpha *= .72f;
        if (fast && !CrimsonVisuals.Reduced)
            for (int i = 3; i > 0; i--) Skeleton(root - boss.NPC.velocity * i * .85f, new Color(210, 32, 51, 0) * (.09f / i));
        Skeleton(root, Color.White * alpha);

        // The operator has her own small command platform, not a giant face
        // pasted into the robot's torso. It remains stationary relative to stage.
        Vector2 operatorPos = new(boss.State.Field.Left + 190, boss.State.Field.Top + 205 + MathF.Sin(age * .022f) * 7);
        Part(11, operatorPos, 105, MathF.Sin(age * .012f) * .025f, Color.White * alpha, new(.5f));
        Bloom(operatorPos + new Vector2(0, 52), new(40, 11), new Color(242, 18, 45, 0) * (.2f + pulse * .14f));

        CrimsonEnergy.Begin();
        float reveal = purgeAge < 0 ? 0 : MathF.Exp(-Math.Max(0, purgeAge - 12) / 24);
        CrimsonEnergy.AddCore(root, 53 + charge * 26 + pulse * 9 + reveal * 34, age, charge,
            Math.Max(MathF.Exp(-since / 5), reveal), alpha, CrimsonVisuals.Reduced);
        // Connected exhaust starts at the same animated engine joints.
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 jet = Joint(root, new(side * 31, 24));
            Vector2 direction = new Vector2(side * .12f, 1).RotatedBy(lean);
            CrimsonEnergy.Add(jet, direction, (fast ? 125 : 45) + pulse * 30, fast ? 10 : 6,
                age, age - 16, age + 1, .38f * alpha, CrimsonVisuals.Reduced);
        }
        CrimsonEnergy.Draw(batch);
        return false;

        Vector2 Joint(Vector2 origin, Vector2 local) => origin + local.RotatedBy(lean);
        void Skeleton(Vector2 center, Color color)
        {
            float tension = charge * .25f - recoil * .32f;
            float spread = .23f + tension + (fast ? .22f : 0);
            for (int side = -1; side <= 1; side += 2)
            {
                float walk = MathF.Sin(age * (fast ? .095f : .038f) + side * 1.4f);
                Vector2 hip = Joint(center, new(side * 18, 49));
                Vector2 knee = hip + new Vector2(side * (16 + walk * 9), 75).RotatedBy(lean);
                Vector2 foot = knee + new Vector2(side * (15 - walk * 9), 83).RotatedBy(lean + (fast ? -.10f : .04f));
                Bone(5, hip, knee, color, side < 0);
                Bone(6, knee, foot, color, side < 0);
                Vector2 shoulder = Joint(center, new(side * 45, -28));
                Vector2 elbow = shoulder + new Vector2(side * 36, 60).RotatedBy(lean - side * spread);
                Vector2 wrist = elbow + new Vector2(side * (28 + charge * 30), 65 - charge * 60 + recoil * 35).RotatedBy(lean - side * spread);
                Bone(2, shoulder, elbow, color, side < 0);
                Bone(3, elbow, wrist, color, side < 0);
                Part(10, Joint(center, new(side * 42, -5)), 92, lean + side * .3f, color, new(.5f));
                Armor(8, shoulder, side * 1f, 85, side < 0);
                Armor(9, (hip + knee) * .5f + new Vector2(side * 13, 0), side * .25f, 105, side < 0);
            }
            Part(4, Joint(center, new(0, 48)), 58, lean, color, new(.5f));
            Part(0, center, 113, lean, color, new(.5f, .45f));
            Part(1, Joint(center, new(-2, -67)), 55, lean - boss.NPC.velocity.X * .004f, color, new(.5f));
            Armor(7, center, 0, 134, false);

            void Armor(int part, Vector2 at, float twist, float height, bool flip)
            {
                float travel = CrimsonRigMotion.ArmorTravel(purgeAge);
                float opacity = purgeAge < 0 ? 1 : 1 - CrimsonRigMotion.Ease((purgeAge - 28) / 60);
                if (opacity <= 0) return;
                Vector2 outward = (at - center).SafeNormalize(new Vector2(0, -1));
                Part(part, at + outward * travel * 270 + new Vector2(0, travel * travel * 70), height,
                    lean + twist * .18f + travel * (twist == 0 ? -.9f : twist), color * opacity, new(.5f), flip);
            }
        }
        void Bone(int index, Vector2 from, Vector2 to, Color color, bool flip)
        {
            Vector2 d = to - from;
            Part(index, from, d.Length() / .88f, d.ToRotation() - MathHelper.PiOver2, color, new(.5f, .06f), flip);
        }
        void Part(int index, Vector2 at, float height, float rotation, Color color, Vector2 pivot, bool flip = false)
        {
            Rectangle r = parts[index];
            batch.Draw(atlas, at - screen, r, color, rotation, r.Size() * pivot, height / r.Height,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }
        void Bloom(Vector2 at, Vector2 halfSize, Color color)
        {
            var tex = MiscTexturesRegistry.BloomCircleSmall.Value;
            batch.Draw(tex, at - screen, null, color, 0, tex.Size() * .5f, halfSize * 2 / tex.Size(), SpriteEffects.None, 0);
        }
    }
}
