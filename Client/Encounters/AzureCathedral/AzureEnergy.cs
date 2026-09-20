using AzureGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
using System;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.AzureCathedral;

// Small bounded feature renderer consuming the existing project-owned shader
// materials. No dependency on either previous encounter's presentation classes.
internal static class AzureEnergy
{
    private readonly record struct Ray(Vector2 Origin, Vector2 Direction, float Length, float Width, float Age, float Fire, float End, float Opacity, bool Reduced, float Born, bool FieldBeam, bool Bell);
    private static readonly Ray[] rays = new Ray[512];
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6];
    private static int count;
    internal static void Begin() => count = 0;
    internal static void Add(Vector2 origin, Vector2 direction, float length, float width, float age, float fire, float end, float opacity, bool reduced,
        float born = -1, bool fieldBeam = false, bool bell = false)
    {
        if (count < rays.Length && length > .01f && width > .01f && opacity > .001f)
            rays[count++] = new(origin, direction, length, width, age, fire, end, opacity, reduced, born < 0 ? fire - 60 : born, fieldBeam,bell);
    }
    internal static void Draw(SpriteBatch batch)
    {
        if (count == 0 || Main.dedServ) return;
        var portal = ShaderManager.GetShader("Convergence.PortalBeam");
        var dust = ShaderManager.GetShader("Convergence.RaidEnergy");
        var device = Main.instance.GraphicsDevice;
        using var scope = new AzureGraphicsScope(batch);
        try
        {
            device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            var transform = Main.GameViewMatrix.TransformationMatrix * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1);
            portal.TrySetParameter("uWorldViewProjection", transform); dust.TrySetParameter("uWorldViewProjection", transform);
            portal.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
            portal.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
            portal.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 3, SamplerState.LinearWrap);
            for (int i = 0; i < count; i++)
            {
                ref var ray = ref rays[i]; bool live = ray.Age >= ray.Fire;
                var signal = new Vector4(Math.Clamp((ray.Age - ray.Born) / Math.Max(1, ray.Fire - ray.Born), 0, 1), live ? 1 : 0, ray.Opacity, MathF.Exp(-Math.Max(0, ray.Age - ray.Fire) / 4));
                // Field beams use the actual Doll timed forecast/jet passes:
                // soft veil, fine central thread and sparse footprint grains, no rails/caps.
                DrawPass(portal, live ? "AutoloadPass" : "PortalForecastPass", live || ray.FieldBeam ? ray.Width : Math.Min(2, ray.Width));
                if (live) DrawPass(portal, "PortalCoronaPass", ray.FieldBeam ? ray.Width + Math.Min(24, ray.Width * .3f) : ray.Width * 2.4f);
                if (!live) {
                    var before = signal;
                    if (ray.FieldBeam) {
                        float dip = 1 - .84f * Convergence.Content.Encounters.AzureCathedral.AzureRules.Ease((ray.Age - ray.Fire + 8) / 7);
                        signal.Z *= dip;
                    }
                    DrawPass(dust, "ForecastDustPass", ray.Width);
                    signal = before;
                }
                DrawMouth();
                void DrawMouth()
                {
                    var r = rays[i]; float pulse=MathF.Pow(.5f+.5f*MathF.Sin(r.Age*.24f),5);
                    float radius = live ? 38 + signal.W * 55 : 12 + signal.X * (r.Bell?68:24) + (r.Bell?28:6)*pulse*signal.X;
                    portal.TrySetParameter("shape", new Vector4(radius * 2, radius, i, r.Reduced ? .25f : 1));
                    portal.TrySetParameter("signal", signal);
                    portal.TrySetParameter("clock", r.Age / 60);
                    portal.TrySetParameter("ceremony", new Vector4(r.Age - r.Fire, Math.Max(0, r.Age - r.End), 0, 0));
                    Vector2 normal = new(-r.Direction.Y, r.Direction.X);
                    Quad(r.Origin - Main.screenPosition - r.Direction * radius - normal * radius, normal * radius * 2, r.Direction * radius * 2);
                    portal.Apply("PortalMouthPass"); device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
                }
                void DrawPass(ManagedShader shader, string pass, float width)
                {
                    var r = rays[i];
                    shader.TrySetParameter("beamColor", new Vector3(.12f, .68f, 1f)); shader.TrySetParameter("signal", signal);
                    shader.TrySetParameter("shape", new Vector4(r.Length, width, (r.Origin.X + r.Origin.Y) * .001f % 11, r.Reduced ? .25f : 1));
                    shader.TrySetParameter("clock", r.Age / 60); shader.TrySetParameter("pulse", Vector4.Zero); shader.TrySetParameter("flowOffset", 0f);
                    shader.TrySetParameter("ceremony", new Vector4(r.Age - r.Fire, Math.Max(0, r.Age - r.End), r.Bell?260:0, r.Bell?32:0));
                    Vector2 n = new(-r.Direction.Y * width, r.Direction.X * width), start = r.Origin - Main.screenPosition;
                    Quad(start - n, n * 2, r.Direction * r.Length);
                    shader.Apply(pass); device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
                }
            }

        }
        finally
        {
            count = 0;
        }
    }
    private static void Quad(Vector2 start, Vector2 across, Vector2 along)
    {
        vertices[0] = new(new Vector3(start, 0), Color.White, new(0, 0));
        vertices[1] = new(new Vector3(start + across, 0), Color.White, new(0, 1));
        vertices[2] = new(new Vector3(start + along, 0), Color.White, new(1, 0));
        vertices[3] = vertices[2]; vertices[4] = vertices[1];
        vertices[5] = new(new Vector3(start + across + along, 0), Color.White, new(1, 1));
    }
}
