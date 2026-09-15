using System;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Small bounded feature renderer consuming the existing project-owned shader
// materials. No dependency on either previous encounter's presentation classes.
internal static class CrimsonEnergy
{
    private readonly record struct Ray(Vector2 Origin, Vector2 Direction, float Length, float Width, float Age, float Fire, float End, float Opacity, bool Reduced);
    private static readonly Ray[] rays = new Ray[512];
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6];
    private static int count;
    private readonly record struct Reactor(Vector2 Position, float Radius, float Age, float Charge, float Impulse, float Alpha, bool Reduced);
    private static readonly Reactor[] cores = new Reactor[8];
    private static int coreCount;
    internal static void Begin() { count = coreCount = 0; }
    internal static void AddCore(Vector2 position, float radius, float age, float charge, float impulse, float alpha, bool reduced)
    { if (coreCount < cores.Length) cores[coreCount++] = new(position, radius, age, charge, impulse, alpha, reduced); }
    internal static void Add(Vector2 origin, Vector2 direction, float length, float width, float age, float fire, float end, float opacity, bool reduced)
    {
        if (count < rays.Length && length > .01f && width > .01f && opacity > .001f)
            rays[count++] = new(origin, direction, length, width, age, fire, end, opacity, reduced);
    }
    internal static void Draw(SpriteBatch batch)
    {
        if (count == 0 && coreCount == 0 || Main.dedServ) return;
        var portal = ShaderManager.GetShader("Convergence.PortalBeam");
        var dust = ShaderManager.GetShader("Convergence.RaidEnergy");
        var device = Main.instance.GraphicsDevice;
        batch.End();
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState;
        var a = device.Textures[1]; var b = device.Textures[2]; var c = device.Textures[3];
        var s1 = device.SamplerStates[1]; var s2 = device.SamplerStates[2]; var s3 = device.SamplerStates[3];
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
                var signal = new Vector4(Math.Clamp((ray.Age - ray.Fire + 60) / 60, 0, 1), live ? 1 : 0, ray.Opacity, MathF.Exp(-Math.Max(0, ray.Age - ray.Fire) / 4));
                DrawPass(portal, live ? "AutoloadPass" : "PortalForecastPass", live ? ray.Width : Math.Min(2, ray.Width));
                if (live) DrawPass(portal, "PortalCoronaPass", ray.Width * 2.4f);
                if (!live) DrawPass(dust, "ForecastDustPass", ray.Width);
                DrawMouth();
                void DrawMouth()
                {
                    var r = rays[i]; float radius = live ? 38 + signal.W * 30 : 12 + signal.X * 24;
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
                    shader.TrySetParameter("beamColor", new Vector3(1, .06f, .13f)); shader.TrySetParameter("signal", signal);
                    shader.TrySetParameter("shape", new Vector4(r.Length, width, (r.Origin.X + r.Origin.Y) * .001f % 11, r.Reduced ? .25f : 1));
                    shader.TrySetParameter("clock", r.Age / 60); shader.TrySetParameter("pulse", Vector4.Zero); shader.TrySetParameter("flowOffset", 0f);
                    shader.TrySetParameter("ceremony", new Vector4(r.Age - r.Fire, Math.Max(0, r.Age - r.End), 0, 0));
                    Vector2 n = new(-r.Direction.Y * width, r.Direction.X * width), start = r.Origin - Main.screenPosition;
                    Quad(start - n, n * 2, r.Direction * r.Length);
                    shader.Apply(pass); device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
                }
            }
            if (coreCount > 0)
            {
                var reactor = ShaderManager.GetShader("Convergence.CrimsonReactor");
                reactor.TrySetParameter("uWorldViewProjection", transform);
                reactor.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
                reactor.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
                for (int i = 0; i < coreCount; i++)
                {
                    var core = cores[i];
                    reactor.TrySetParameter("clock", core.Age / 60);
                    reactor.TrySetParameter("signal", new Vector4(core.Charge, core.Impulse, core.Alpha, core.Reduced ? .25f : 1));
                    Quad(core.Position - Main.screenPosition - new Vector2(core.Radius), new(0, core.Radius * 2), new(core.Radius * 2, 0));
                    reactor.Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
                }
            }
        }
        finally
        {
            count = coreCount = 0; device.Textures[1] = a; device.Textures[2] = b; device.Textures[3] = c;
            device.SamplerStates[1] = s1; device.SamplerStates[2] = s2; device.SamplerStates[3] = s3;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
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
