using ScarletGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Original artwork with species-specific articulation and accepted gesture paths.
internal static class CrimsonRig
{
    private static Texture2D? performer;
    private static readonly Texture2D?[] effigies = new Texture2D?[3];
    private static readonly int[] partOrder = { 3, 4, 0, 1, 2 };
    private static readonly int[] singlePart = { 0 };
    private const int Columns = 24, Rows = 32;
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[Columns * Rows * 6];
    private static readonly Rectangle[] poses = { new(20, 202, 335, 540), new(422, 202, 460, 540), new(890, 190, 487, 530), new(1398, 202, 355, 540) };
    private static readonly Vector2[] pivots = { new(190, 278), new(223, 278), new(276, 270), new(207, 278) };
    internal static void Load()
    {
        if (Main.dedServ) return;
        performer = LoadTexture("ScarletConjurer");
        string[] names = { "EmberCrown", "SableMantle", "ThornChoir" };
        for (int i = 0; i < 3; i++) effigies[i] = LoadTexture(names[i]);
        // Direct FNA effect construction belongs to drawing, not this loader hook.
    }
    private static Texture2D LoadTexture(string name) => ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/" + name, AssetRequestMode.ImmediateLoad).Value;
    internal static void Unload()
    {
        performer = null; Array.Clear(effigies);
        ScarletMaterials.Reset();
    }
    internal static (float Charge, float Recoil) Signal(CrimsonBoss boss, int source, float age)
    {
        float until = 60, since = 100;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is CrimsonGesture g && g.TryBoss(out var owner) && owner == boss
                && age >= g.Plan.Born && (source < 0 || g.Plan.Source == source))
            {
                float delta = g.Plan.Fire - age;
                if (delta >= 0) until = Math.Min(until, delta); else since = Math.Min(since, -delta);
            }
        return (CrimsonRigMotion.Charge(until), CrimsonRigMotion.Recoil(since));
    }
    internal static bool Draw(CrimsonBoss boss, SpriteBatch batch, Vector2 screen)
    {
        float age = CrimsonVisuals.RenderAge(boss);
        var signal = Signal(boss, -1, age);
        float ending = boss.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat ? .65f : 1;
        if (boss.State.PerformerDefeated) ending *= .18f;
        Vector2 at = boss.NPC.Center;
        if (CrimsonGesture.TryPose(boss, 3, age, out var pose)) at = CrimsonGestureVisuals.V(pose.Body(age));
        DrawPerformer(batch, screen, at, age, boss.NPC.velocity, boss.NPC.spriteDirection,
            boss.State.MusicStart >= 0, signal.Charge, signal.Recoil, ending);
        return false;
    }
    internal static void DrawPerformer(SpriteBatch batch, Vector2 screen, Vector2 center, float age, Vector2 velocity,
        int facing, bool floating, float charge, float recoil, float alpha = 1)
    {
        if (performer is not { } sprite) return;
        int idle = floating ? 2 : Math.Abs(velocity.X) > .7f && MathF.Sin(age * .22f) > 0 ? 3 : 0;
        float cast = CrimsonInvocation.Ease(charge * 2);
        // A tiny character does not benefit from a 24x32 apparition mesh.
        // Keep her authored pixels, a restrained silhouette rim, and no huge orb
        // over her face. Preserve the caller's batch including UI/sky transforms.
        using (var scope = new ScarletGraphicsScope(batch))
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawPose(idle, 1 - cast); DrawPose(1, cast);
            batch.End();
        }
        Vector2 orb = center + new Vector2(facing * (94 + charge * 12), -25).RotatedBy(velocity.X * .009f);
        CrimsonEnergy.Begin();
        CrimsonEnergy.AddCore(orb, 53 + charge * 24 + recoil * 18, age, charge, recoil, alpha, CrimsonVisuals.Reduced);
        CrimsonEnergy.Draw(batch);
        if (floating && !CrimsonVisuals.Reduced)
        {
            var bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
            for (int i = 0; i < 7; i++)
            {
                float life = (age * .017f + i * .143f) % 1;
                Vector2 at = center + new Vector2(MathF.Sin(i * 3.1f + life * 5) * 17, 16 + life * 36) - velocity * life * 1.4f;
                batch.Draw(bloom, at - screen, null, new Color(255, 76, 100, 0) * ((1 - life) * .42f * alpha), 0,
                    bloom.Size() * .5f, (2 + MathF.Sin(life * MathF.PI) * 3) / bloom.Width, SpriteEffects.None, 0);
            }
        }
        void DrawPose(int pose, float opacity)
        {
            if (opacity < .001f) return;
            Vector2 at = center - screen + new Vector2(0, floating ? MathF.Sin(age * .045f) * 1.4f : 0);
            float tilt = Math.Clamp(velocity.X * .009f, -.13f, .13f) - recoil * .035f;
            var flip = facing < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var origin = pivots[pose];
            if (facing < 0) origin.X = poses[pose].Width - origin.X;
            Color rim = new Color(243, 118, 136, 0) * (alpha * opacity * .26f);
            for (int k = 0; k < 4; k++)
                batch.Draw(sprite, at + new Vector2(k == 0 ? -1 : k == 1 ? 1 : 0, k == 2 ? -1 : k == 3 ? 1 : 0),
                    poses[pose], rim, tilt, origin, 56f / 540, flip, 0);
            batch.Draw(sprite, at, poses[pose], Color.White * (alpha * opacity), tilt, origin, 56f / 540, flip, 0);
        }
    }
    internal static bool DrawEffigy(CrimsonEffigy effigy, SpriteBatch batch, Vector2 screen)
    {
        if (effigy.State.Index >= 3 || effigies[effigy.State.Index] is not { } texture || !effigy.TryBoss(out var boss)) return false;
        float age = CrimsonVisuals.RenderAge(boss!), born = age - effigy.State.Born;
        float appear = CrimsonInvocation.Ease(born / 62), snap = MathF.Exp(-Math.Max(0, born - 12) / 14);
        var signal = Signal(boss!, effigy.State.Index, age);
        float size = effigy.State.Index == 1 ? 370 : 330;
        float alpha = boss!.State.Presence(effigy.State.Index, age);
        if (boss.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat) alpha *= .35f;
        if (alpha <= .001f) return false;
        Vector2 at = effigy.NPC.Center;
        if (CrimsonGesture.TryPose(boss, effigy.State.Index, age, out var pose))
        {
            at = CrimsonGestureVisuals.V(pose.Body(age));
            if (pose.Technique == CrimsonTechnique.MantleRush && pose.Live(age) && !CrimsonVisuals.Reduced)
                for (int k = 3; k >= 1; k--)
                    Mesh(batch, texture, texture.Bounds, CrimsonGestureVisuals.V(pose.Body(age - k * .35f)) - screen,
                        texture.Size() * .5f, size / texture.Height, age, 14, signal.Charge, signal.Recoil,
                        new Color(142, 98, 159) * (alpha * .09f), effigy.NPC.spriteDirection < 0, effigy.NPC.rotation, true, effigy.State.Index);
        }
        ScarletArticulation.DrawSecondary(batch, effigy.State.Index, age, alpha);
        Mesh(batch, texture, texture.Bounds, at - screen, texture.Size() * .5f,
            size / texture.Height * (.90f + appear * .1f), age + effigy.State.Index * 100,
            14, signal.Charge, signal.Recoil, Color.White * (appear * alpha), effigy.NPC.spriteDirection < 0,
            effigy.NPC.rotation + signal.Recoil * .045f, true, effigy.State.Index);
        if (effigy.State.Index == 0)
        {
            CrimsonEnergy.Begin();
            CrimsonEnergy.AddCore(at + new Vector2(0, -18), 18 + signal.Charge * 12 + snap * 22, age,
                signal.Charge, Math.Max(signal.Recoil, snap), appear * alpha * .66f, CrimsonVisuals.Reduced);
            CrimsonEnergy.Draw(batch);
        }
        return false;
    }
    internal static void DrawPressure(SpriteBatch batch, Vector2 center, int source, float age, float charge, float recoil)
    {
        // Broken converging filaments, not a UI target circle or opaque halo.
        var bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
        Color hue = ScarletMaterials.Palette(source); hue.A = 0;
        float pulse = .78f + .22f * MathF.Pow(Math.Max(0, MathF.Sin(age * .36f)), 3);
        int count = CrimsonVisuals.Reduced ? 4 : 11;
        for (int i = 0; i < count; i++)
        {
            float drift = (age * .022f + i * .618034f) % 1;
            float angle = i * 2.399963f + MathF.Sin(i * 2.1f) * .2f;
            float reach = (source == 3 ? 65 : 210) * (1 - drift * charge) + recoil * 85;
            Vector2 offset = new Vector2(reach, 0).RotatedBy(angle);
            float brightness = MathF.Sin(drift * MathF.PI) * charge * .52f;
            batch.Draw(bloom, center + offset - Main.screenPosition, null, hue * brightness, angle,
                bloom.Size() * .5f, new Vector2(17 + charge * 22, 2.8f) / bloom.Width, SpriteEffects.None, 0);
        }
        float radius = source == 3 ? 35 : 82;
        batch.Draw(bloom, center - Main.screenPosition, null, hue * (charge * pulse * .36f + recoil * .25f), 0,
            bloom.Size() * .5f, (radius + charge * 25 + recoil * 42) * 2 / bloom.Width, SpriteEffects.None, 0);
        if (recoil > .02f)
            batch.Draw(bloom, center - Main.screenPosition, null, new Color(255, 222, 214, 0) * (recoil * .58f), -.35f,
                bloom.Size() * .5f, new Vector2(150, 7) * (CrimsonVisuals.Reduced ? .45f : 1) / bloom.Width, SpriteEffects.None, 0);
    }
    private static void Mesh(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 center, Vector2 pivot,
        float scale, float time, float motion, float charge, float recoil, Color tint, bool flip, float rotation, bool apparition, int species = -1)
    {
        if (Main.dedServ || tint.A == 0) return;
        var material = ShaderManager.GetShader("Convergence.ScarletSurface");
        using var scope = new ScarletGraphicsScope(batch);
        material.TrySetParameter("uWorldViewProjection", ScarletMaterials.WorldMatrix);
        material.TrySetParameter("clock", time / 60);
        material.TrySetParameter("species", apparition ? (float)species : 3f);
        material.TrySetParameter("signal", new Vector4(charge, recoil, 1, CrimsonVisuals.Reduced ? 1 : 0));
        material.TrySetParameter("texel", new Vector2(1f / texture.Width, 1f / texture.Height));
        material.TrySetParameter("region", new Vector4(source.X / (float)texture.Width, source.Y / (float)texture.Height,
            source.Width / (float)texture.Width, source.Height / (float)texture.Height));
        material.SetTexture(texture, 0, apparition ? SamplerState.LinearClamp : SamplerState.PointClamp);
        material.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        material.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
        foreach (int part in apparition ? partOrder : singlePart)
        {
            var pose = ScarletArticulation.Part(apparition ? species : 3, part, time, charge, recoil);
            material.TrySetParameter("part", (float)part);
            int offset = 0;
            for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
            {
                var a = Vertex(x, y); var b = Vertex(x + 1, y); var c = Vertex(x, y + 1); var d = Vertex(x + 1, y + 1);
                mesh[offset++] = a; mesh[offset++] = b; mesh[offset++] = c;
                mesh[offset++] = b; mesh[offset++] = d; mesh[offset++] = c;
            }
            material.Apply(); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, mesh, 0, offset / 3);
            VertexPositionColorTexture Vertex(int x, int y)
            {
                float u = x / (float)Columns, v = y / (float)Rows;
                Vector2 local = (new Vector2(u * source.Width, v * source.Height) - pivot) * scale;
                float flexible = apparition && part != 0 ? MathF.Abs(u - .5f) * v : 0;
                float secondary = CrimsonVisuals.Reduced ? 0 : motion;
                local.X += MathF.Sin(time * .037f - v * 7 + u * 2) * secondary * flexible;
                local.Y += MathF.Sin(time * .028f + u * 8 - v * 3) * secondary * flexible * .5f;
                float side = part is 1 or 3 ? -1 : 1;
                Vector2 joint = new(side * source.Width * scale * .08f, source.Height * scale * (part >= 3 ? .02f : -.12f));
                local = ((local - joint) * pose.Scale).RotatedBy(pose.Rotation) + joint + pose.Offset;
                if (flip) local.X = -local.X;
                Vector2 pos = center + local.RotatedBy(rotation);
                return new(new Vector3(pos, 0), tint, new((source.X + u * source.Width) / texture.Width, (source.Y + v * source.Height) / texture.Height));
            }
        }
    }
}
