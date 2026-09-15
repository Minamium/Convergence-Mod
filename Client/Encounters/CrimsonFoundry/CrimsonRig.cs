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

// A human-sized performer and large deforming apparitions, not one scaled
// machine portrait. All motion samples one fractional render clock.
internal static class CrimsonRig
{
    private static Texture2D? performer;
    private static readonly Texture2D?[] effigies = new Texture2D?[3];
    private static BasicEffect? material;
    private const int Columns = 24, Rows = 32;
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[Columns * Rows * 6];
    private static readonly Rectangle[] poses = { new(20, 202, 335, 540), new(422, 202, 460, 540), new(890, 190, 487, 530), new(1398, 202, 355, 540) };
    private static readonly Vector2[] pivots = { new(190, 278), new(223, 278), new(276, 270), new(207, 278) };
    internal static void Load()
    {
        performer = LoadTexture("ScarletConjurer");
        string[] names = { "EmberCrown", "SableMantle", "ThornChoir" };
        for (int i = 0; i < 3; i++) effigies[i] = LoadTexture(names[i]);
        material = new BasicEffect(Main.instance.GraphicsDevice) { TextureEnabled = true, VertexColorEnabled = true };
    }
    private static Texture2D LoadTexture(string name) => ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/" + name, AssetRequestMode.ImmediateLoad).Value;
    internal static void Unload()
    { performer = null; Array.Clear(effigies); material?.Dispose(); material = null; }
    internal static (float Charge, float Recoil) Signal(CrimsonBoss boss, int source, float age)
    {
        float until = 60, since = 100;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is CrimsonAttack a && a.Hazard.Fight == boss.State.Fight && (source < 0 || a.Hazard.Source == source))
            {
                float delta = a.Hazard.Fire - age;
                if (delta >= 0) until = Math.Min(until, delta); else since = Math.Min(since, -delta);
            }
        return (CrimsonRigMotion.Charge(until), CrimsonRigMotion.Recoil(since));
    }
    internal static bool Draw(CrimsonBoss boss, SpriteBatch batch, Vector2 screen)
    {
        float age = CrimsonVisuals.RenderAge(boss);
        var signal = Signal(boss, -1, age);
        float ending = boss.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat ? .65f : 1;
        // Final changes location/exposure, NEVER the performer's 56px stature.
        DrawPerformer(batch, screen, boss.NPC.Center, age, boss.NPC.velocity, 1,
            boss.State.MusicStart >= 0, signal.Charge, signal.Recoil, ending);
        return false;
    }
    internal static void DrawPerformer(SpriteBatch batch, Vector2 screen, Vector2 center, float age, Vector2 velocity,
        int facing, bool floating, float charge, float recoil, float alpha = 1)
    {
        if (performer is not { } sprite) return;
        int idle = floating ? 2 : Math.Abs(velocity.X) > .7f && MathF.Sin(age * .22f) > 0 ? 3 : 0;
        float cast = CrimsonInvocation.Ease(charge * 2);
        if (floating) DrawPose(2, 1);
        else { DrawPose(idle, 1 - cast); DrawPose(1, cast); }
        Vector2 orb = center + new Vector2(facing * 28, -13).RotatedBy(velocity.X * .009f);
        CrimsonEnergy.Begin();
        CrimsonEnergy.AddCore(orb, 13 + charge * 10 + recoil * 7, age, charge, recoil, alpha, CrimsonVisuals.Reduced);
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
            Mesh(batch, sprite, poses[pose], center - screen, pivots[pose], 56f / 540,
                age, .8f, charge, recoil, Color.White * (alpha * opacity), facing < 0, velocity.X * .009f, false);
        }
    }
    internal static bool DrawEffigy(CrimsonEffigy effigy, SpriteBatch batch, Vector2 screen)
    {
        if (effigy.State.Index >= 3 || effigies[effigy.State.Index] is not { } texture || !effigy.TryBoss(out var boss)) return false;
        float age = CrimsonVisuals.RenderAge(boss!), born = age - effigy.State.Born;
        float appear = CrimsonInvocation.Ease(born / 62), snap = MathF.Exp(-Math.Max(0, born - 12) / 14);
        var signal = Signal(boss!, effigy.State.Index, age);
        float size = effigy.State.Index == 1 ? 370 : 330;
        float alpha = boss!.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat ? .35f : 1;
        Mesh(batch, texture, texture.Bounds, effigy.NPC.Center - screen, texture.Size() * .5f,
            size / texture.Height * (.90f + appear * .1f), age + effigy.State.Index * 100,
            14, signal.Charge, signal.Recoil, Color.White * (appear * alpha), false, MathF.Sin(age * .015f + effigy.State.Index) * .06f, true);
        CrimsonEnergy.Begin();
        CrimsonEnergy.AddCore(effigy.NPC.Center, 23 + signal.Charge * 20 + snap * 55, age,
            signal.Charge, Math.Max(signal.Recoil, snap), appear * alpha, CrimsonVisuals.Reduced);
        CrimsonEnergy.Draw(batch);
        return false;
    }
    private static void Mesh(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 center, Vector2 pivot,
        float scale, float time, float motion, float charge, float recoil, Color tint, bool flip, float rotation, bool apparition)
    {
        if (material is null || tint.A == 0) return;
        int offset = 0;
        for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
        {
            var a = Vertex(x, y); var b = Vertex(x + 1, y); var c = Vertex(x, y + 1); var d = Vertex(x + 1, y + 1);
            mesh[offset++] = a; mesh[offset++] = b; mesh[offset++] = c;
            mesh[offset++] = b; mesh[offset++] = d; mesh[offset++] = c;
        }
        batch.End();
        var device = Main.instance.GraphicsDevice;
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState;
        var texture0 = device.Textures[0]; var sampler0 = device.SamplerStates[0];
        try
        {
            device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone; device.SamplerStates[0] = SamplerState.LinearClamp;
            material.Texture = texture; material.World = Matrix.Identity; material.View = Main.GameViewMatrix.TransformationMatrix;
            material.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1);
            foreach (var pass in material.CurrentTechnique.Passes) { pass.Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleList, mesh, 0, offset / 3); }
        }
        finally
        {
            device.Textures[0] = texture0; device.SamplerStates[0] = sampler0; device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        VertexPositionColorTexture Vertex(int x, int y)
        {
            float u = x / (float)Columns, v = y / (float)Rows;
            Vector2 local = (new Vector2(u * source.Width, v * source.Height) - pivot) * scale;
            float loose = apparition ? .25f + MathF.Abs(u - .5f) + v * v : .12f + MathF.Abs(u - .5f) * v;
            local.X += MathF.Sin(time * .037f - v * 7 + u * 2) * motion * loose;
            local.Y += MathF.Sin(time * .028f + u * 8 - v * 3) * motion * loose * .65f;
            local *= new Vector2(1 - charge * .055f + recoil * .12f, 1 + charge * .022f - recoil * .04f);
            if (flip) local.X = -local.X;
            Vector2 pos = center + local.RotatedBy(rotation);
            return new(new Vector3(pos, 0), tint, new((source.X + u * source.Width) / texture.Width, (source.Y + v * source.Height) / texture.Height));
        }
    }
}
