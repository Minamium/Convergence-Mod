#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiMaterials
{
    private static ManagedShader? surface, ribbon;
    private static PrimitiveSettings? trailSettings;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static readonly List<Vector2> points = new(16);
    private static float trailRadius, trailAlpha;
    internal static void Reset() { surface = ribbon = null; trailSettings = null; points.Clear(); }
    internal static void Prepare(float age, float charge, float hit, float dissolution)
    {
        surface ??= ShaderManager.GetShader("Convergence.SamuraiSpirit");
        surface.TrySetParameter("clock", age / 60);
        surface.TrySetParameter("charge", charge); surface.TrySetParameter("hit", hit);
        surface.TrySetParameter("dissolution", dissolution);
        surface.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height, 0, -1, 1));
        surface.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.LinearWrap);
        surface.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
    }
    internal static void Part(Texture2D texture, int part, Vector2 at, Rectangle source, Color color,
        float rotation, Vector2 origin, Vector2 scale, bool flip)
    {
        if (Main.dedServ || surface is null) return;
        surface.SetTexture(texture, 0, SamplerState.LinearClamp);
        surface.TrySetParameter("part", (float)part);
        surface.TrySetParameter("texel", new Vector2(1f / texture.Width, 1f / texture.Height));
        surface.TrySetParameter("region", new Vector4(source.X / (float)texture.Width, source.Y / (float)texture.Height,
            source.Width / (float)texture.Width, source.Height / (float)texture.Height));
        for (int i = 0; i < 6; i++)
        {
            int corner = i switch { 0 => 0, 1 => 2, 2 => 1, 3 => 1, 4 => 2, _ => 3 };
            Vector2 unit = new(corner % 2, corner / 2);
            Vector2 p = at + ((unit * new Vector2(source.Width, source.Height) - origin) * scale).RotatedBy(rotation);
            float u = flip ? 1 - unit.X : unit.X;
            quad[i] = new(new(p, 0), color, new((source.X + u * source.Width) / texture.Width, (source.Y + unit.Y * source.Height) / texture.Height));
        }
        surface.Apply(); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }
    internal static void Trails(SpriteBatch batch, in SamuraiRigPose pose, SamuraiRigHistory history, double tick)
    {
        if (Main.dedServ || history.Count < 2 || GhostSamuraiRigArt.Reduced) return;
        ribbon ??= ShaderManager.GetShader("Convergence.SamuraiRibbon");
        trailSettings ??= new PrimitiveSettings(u => trailRadius * (.15f + .85f * u), _ => Color.White,
            Smoothen: false, Shader: ribbon);
        using var scope = new WorldGraphicsScope(batch);
        ribbon.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        ribbon.TrySetParameter("clock", pose.Age / 60);
        for (int side = -1; side <= 1; side += 2)
        {
            points.Clear(); trailAlpha = 0; trailRadius = 10;
            for (int i = 0; i < history.Count; i++)
            {
                var h = history.At(i); float age = (float)(tick - h.Tick);
                if (age < 0) continue;
                var blade = side < 0 ? h.Pose.Left : h.Pose.Right;
                if (blade.Trail <= 0) { Flush(); continue; }
                Vector2 point = GhostSamuraiRigArt.Tip(h.Pose, side);
                if (points.Count > 0 && Vector2.DistanceSquared(points[^1], point) > 200 * 200) Flush();
                if (points.Count == 0 || Vector2.DistanceSquared(points[^1], point) > .01f) points.Add(point);
                trailRadius = Math.Max(trailRadius, blade.Size * 12);
                trailAlpha = Math.Max(trailAlpha, SamuraiRigMotion.Fade(age) * blade.Trail * .7f);
            }
            var current = side < 0 ? pose.Left : pose.Right;
            if (current.Trail > 0 && points.Count > 0)
            {
                Vector2 tip = GhostSamuraiRigArt.Tip(pose, side);
                float distance = Vector2.DistanceSquared(points[^1], tip);
                if (distance > .01f && distance < 200 * 200) points.Add(tip);
            }
            Flush();
        }
    }
    private static void Flush()
    {
        if (points.Count == 2) points.Insert(1, (points[0] + points[1]) * .5f);
        if (points.Count >= 3)
        {
            ribbon!.TrySetParameter("opacity", trailAlpha);
            PrimitiveRenderer.RenderTrail(points, trailSettings!, points.Count);
        }
        points.Clear(); trailAlpha = 0;
    }
}
