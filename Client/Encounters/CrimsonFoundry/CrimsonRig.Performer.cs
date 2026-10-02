using ScarletGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Vespera and the free-standing apparitions: the part of the rig that needs no boss, effigy or projectile, so the
// offline preview can link it unchanged. The boss/effigy entry points stay in CrimsonRig.cs.
internal static partial class CrimsonRig
{
    // The accepted charge/recoil from plan lists (the boss form passes ScarletCueFrame's one scan per frame).
    internal static (float Charge, float Recoil) Signal(ReadOnlySpan<CrimsonGesturePlan> gestures,
        ReadOnlySpan<CrimsonChorusPlan> choruses, int source, float age)
    {
        var (until, since) = ScarletNotes.SignalTimes(gestures, choruses, source, age);
        return (CrimsonRigMotion.Charge(until), CrimsonRigMotion.Recoil(since));
    }
    private static Texture2D? performer;
    private static readonly Rectangle[] poses = { new(20, 202, 335, 540), new(422, 202, 460, 540), new(890, 190, 487, 530), new(1398, 202, 355, 540) };
    private static readonly Vector2[] pivots = { new(190, 278), new(223, 278), new(276, 270), new(207, 278) };
    private static void LoadPerformer() => performer = LoadTexture("ScarletConjurer");
    private static Texture2D LoadTexture(string name) => ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/" + name, AssetRequestMode.ImmediateLoad).Value;
    internal static void DrawPerformer(SpriteBatch batch, Vector2 screen, Vector2 center, float age, Vector2 velocity,
        int facing, bool floating, float charge, float recoil, float alpha = 1, bool showOrb = true, float materialized = 1)
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
        if(showOrb) CrimsonEnergy.AddCore(orb, 53 + charge * 24 + recoil * 18, age, charge, recoil, alpha, CrimsonVisuals.Reduced);
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
            if(materialized>=.999f) batch.Draw(sprite, at, poses[pose], Color.White * (alpha * opacity), tilt, origin, 56f / 540, flip, 0);
            else for(int strip=0;strip<18;strip++) {
                var source=poses[pose];int h=source.Height/18;int y=strip*h;source.Y+=y;source.Height=Math.Min(h,poses[pose].Height-y);
                float settle=CrimsonInvocation.Ease((materialized-strip*.025f)*2.1f);
                Vector2 drift=new Vector2(MathF.Sin(strip*4.3f)*55*(1-settle),0);
                batch.Draw(sprite,at+drift,source,Color.Lerp(new Color(255,37,86),Color.White,settle)*(alpha*opacity*settle),
                    tilt,origin-new Vector2(0,y),56f/540,flip,0);
            }
        }
    }
    internal static void DrawApparition(SpriteBatch batch, int species, Vector2 center, float age, float reveal, float dissolve, float scale = .70f)
    {
        if (reveal <= 0 || dissolve >= 1) return;
        float height = species == 0 ? 350 : species == 2 ? 490 : 430;
        if (species == 2)
        {
            CrimsonChoirRig.Draw(batch, center, height * scale, age + species * 37,
                .7f, 0, reveal, false, dissolve: dissolve);
            return;
        }
        ScarletApparitionRig.Draw(batch,species,center,height*scale,age,.7f,0,reveal,dissolve:dissolve);
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
}
