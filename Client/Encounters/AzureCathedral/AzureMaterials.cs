using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.AzureCathedral;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.AzureCathedral;

internal static class AzureMaterials
{
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6];
    private static readonly AzureWorm[] parts = new AzureWorm[AzureRules.Segments + 1];
    internal static void Quad(ManagedShader shader, Vector2 center, Vector2 size, float rotation, Vector4 uv, Color color, string pass = "AutoloadPass")
    {
        Vector2 dx = rotation.ToRotationVector2() * size.X * .5f, dy = new Vector2(-MathF.Sin(rotation), MathF.Cos(rotation)) * size.Y * .5f;
        Vector2 a = center - dx - dy, b = center - dx + dy, c = center + dx - dy, d = center + dx + dy;
        vertices[0] = new(new(a, 0), color, new(uv.X, uv.Y)); vertices[1] = new(new(b, 0), color, new(uv.X, uv.Y + uv.W));
        vertices[2] = new(new(c, 0), color, new(uv.X + uv.Z, uv.Y)); vertices[3] = vertices[2]; vertices[4] = vertices[1];
        vertices[5] = new(new(d, 0), color, new(uv.X + uv.Z, uv.Y + uv.W));
        shader.Apply(pass); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
    }
    internal static ManagedShader Begin(bool screen = false)
    {
        var device = Main.instance.GraphicsDevice;
        device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
        var shader = ShaderManager.GetShader("Convergence.AzureGlass");
        var matrix = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1);
        shader.TrySetParameter("uWorldViewProjection", screen ? matrix : Main.GameViewMatrix.TransformationMatrix * matrix);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        return shader;
    }
    internal static void Worm(AzureWorm head, AzureBoss girl, SpriteBatch batch, Vector2 screen)
    {
        Array.Clear(parts);
        foreach (NPC n in Main.ActiveNPCs) if (n.ModNPC is AzureWorm a && a.Fight == head.Fight) parts[a.Index] = a;
        float age = AzureVisuals.RenderAge(girl), presence = 1;
        if (girl.State.WormLife <= 0) presence *= .16f;
        if (girl.State.EndAt >= 0) presence *= 1 - AzureRules.Ease((age - girl.State.EndAt) / 150);
        var art = ModContent.Request<Texture2D>("Convergence/Assets/Textures/AzureCathedral/Vitrion").Value;
        using var scope = new WorldGraphicsScope(batch);
        var shader = Begin(); shader.SetTexture(art, 0, SamplerState.PointClamp); shader.TrySetParameter("clock", age / 60);
        for (int i = AzureRules.Segments; i >= 0; i--)
        {
            var part = parts[i]; if (part is null) continue;
            int cell = i == 0 ? 0 : i == AzureRules.Segments ? 3 : i % 3 == 0 ? 2 : 1;
            Vector4 uv = new(cell % 2 * .5f, cell / 2 * .5f, .5f, .5f);
            // Authored part spines sit at y=.54 / .48 of their cells. The shader
            // mirrors around that spine, not a guessed atlas midpoint.
            float spine = cell < 2 ? .542f : .48f;
            float angle = part.NPC.rotation, scale = i == 0 ? 292 : i == AzureRules.Segments ? 205 : 188;
            if (i == 0)
                foreach (Projectile p in Main.ActiveProjectiles)
                    if (p.ModProjectile is AzureAttack a && a.Plan.Fight == head.Fight && a.Plan.Kind == AzureAttackKind.MouthBeam && age < a.Plan.End)
                    { var f = girl.State.Field; angle = (new Vector2(f.CenterX, f.CenterY) - head.NPC.Center).ToRotation() + AzureRules.Sweep(age-a.Plan.Fire,a.Plan.End-a.Plan.Fire); break; }
            // Tail art points away from the head; the barrel root overlaps its previous joint.
            if (i == AzureRules.Segments) angle += MathHelper.Pi;
            float flex = AzureVisuals.Reduced ? 0 : MathF.Sin(age * .020f - i * .22f) * .018f;
            shader.TrySetParameter("region", uv);
            float emerging = AzureRules.Ease((age-girl.State.MusicStart-AzureRules.WormArrival-i*6)/24);
            shader.TrySetParameter("spine",spine);
            shader.TrySetParameter("signal", new Vector4(presence*emerging, head.NPC.ai[2], AzureVisuals.Reduced ? 1 : 0, i));
            Quad(shader, part.NPC.Center - screen, new(scale, scale * (1 + flex)), angle, uv, Color.White);
        }
        Array.Clear(parts);
    }
    internal static void Effect(SpriteBatch batch, string pass, Vector2 center, Vector2 size, float rotation, float age, Vector4 signal)
    {
        using var scope = new WorldGraphicsScope(batch);
        var shader=Begin();shader.TrySetParameter("clock",age/60);shader.TrySetParameter("signal",signal);
        Quad(shader,center-Main.screenPosition,size,rotation,new(0,0,1,1),Color.White,pass);
    }
    internal static void Shard(SpriteBatch batch, Vector2 a, Vector2 b, float radius, float age, float alpha)
    {
        if (Vector2.DistanceSquared(a,b)<1) return;
        using var scope = new WorldGraphicsScope(batch);
        var shader = Begin(); shader.TrySetParameter("clock", age / 60); shader.TrySetParameter("signal", new Vector4(alpha, 0, 0, 0));
        Quad(shader, (a+b)*.5f-Main.screenPosition, new(Vector2.Distance(a,b),radius*2), (b-a).ToRotation(), new(0,0,1,1), Color.White,"ShardPass");
    }
}
