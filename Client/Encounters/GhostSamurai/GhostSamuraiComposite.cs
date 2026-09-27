#nullable enable
using System;
using Convergence.Client.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// One pair of lazily allocated Luminance targets. No per-NPC or per-frame
// allocations of GPU surfaces; invalidation is exact-Fight and render-thread safe.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiComposite : ModSystem
{
    private const int Canvas = 1024;
    private static ManagedRenderTarget? body, emission;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static Guid preparedFight;
    private static float preparedAge;
    private static SamuraiRigPose preparedPose;
    private static double preparedTick;
    private static ulong preparedUpdate;
    private static int bodySize, emissionSize;
    private static bool reducedTarget, pendingRelease, disabled;
    public override void Load() { if (!Main.dedServ) RenderTargetManager.RenderTargetUpdateLoopEvent += Prepare; }
    public override void Unload()
    {
        if (Main.dedServ) return;
        RenderTargetManager.RenderTargetUpdateLoopEvent -= Prepare;
        var oldBody = body; var oldEmission = emission; body = emission = null;
        Invalidate();
        Main.QueueMainThreadAction(() => { oldBody?.Dispose(); oldEmission?.Dispose(); });
    }
    public override void ClearWorld() { Invalidate(); disabled = false; }
    public override void OnWorldUnload() { Invalidate(); disabled = false; }
    private static void Invalidate()
    { preparedFight = Guid.Empty; pendingRelease = true; preparedPose = default; preparedTick = 0; preparedUpdate = 0; }
    private static void Release()
    {
        if (!pendingRelease) return;
        body?.Dispose(); emission?.Dispose(); pendingRelease = false;
    }
    private static void Prepare()
    {
        if (Main.dedServ || disabled) return;
        Release();
        if (!GhostSamuraiPresentation.TryPose(out var pose, out var fight)) { Invalidate(); Release(); return; }
        bool reduced = GhostSamuraiRigArt.Reduced;
        try
        {
            if (preparedFight != Guid.Empty && preparedFight != fight) { Invalidate(); Release(); }
            bodySize = reduced ? 512 : 1024; emissionSize = reduced ? 256 : 512;
            body ??= new ManagedRenderTarget(false, (_, _) => new RenderTarget2D(Main.instance.GraphicsDevice, bodySize, bodySize), false);
            emission ??= new ManagedRenderTarget(false, (_, _) => new RenderTarget2D(Main.instance.GraphicsDevice, emissionSize, emissionSize), false);
            if (body.IsDisposed || reducedTarget != reduced) body.Recreate(Main.screenWidth, Main.screenHeight);
            if (emission.IsDisposed || reducedTarget != reduced) emission.Recreate(Main.screenWidth, Main.screenHeight);
            reducedTarget = reduced;
            double sampledTick = Main.GameUpdateCount + (double)GhostSamuraiPresentation.Fraction - 1;
            Compose(pose);
            preparedFight = fight; preparedAge = pose.Age;
            preparedPose = pose; preparedUpdate = Main.GameUpdateCount;
            preparedTick = sampledTick;
        }
        catch (Exception exception)
        {
            Invalidate(); disabled = true; Release();
            global::Convergence.ConvergenceMod.Instance.Logger.Warn($"Samurai spectral composite disabled; direct rig retained: {exception}");
        }
    }
    // The already composed body, its swords and the external trails must use
    // one rendered pose, not separately sampled wall clocks during the frame.
    internal static bool TryPreparedPose(Guid fight, out SamuraiRigPose pose, out double renderTick)
    {
        pose = preparedPose; renderTick = preparedTick;
        return !Main.dedServ && !disabled && !pendingRelease && fight != Guid.Empty
            && preparedFight == fight && Main.GameUpdateCount == preparedUpdate
            && body is { IsDisposed: false, IsUninitialized: false }
            && emission is { IsDisposed: false, IsUninitialized: false };
    }
    private static void Compose(in SamuraiRigPose pose)
    {
        var device = Main.instance.GraphicsDevice;
        var targets = device.GetRenderTargets(); var viewport = device.Viewport;
        var scissor = device.ScissorRectangle; var blend = device.BlendState;
        var depth = device.DepthStencilState; var raster = device.RasterizerState;
        var vertices = device.GetVertexBuffers(); var indices = device.Indices;
        var t0 = device.Textures[0]; var t1 = device.Textures[1]; var t2 = device.Textures[2];
        var s0 = device.SamplerStates[0]; var s1 = device.SamplerStates[1]; var s2 = device.SamplerStates[2];
        try
        {
            var bodyTexture = body!.Target; var emissionTexture = emission!.Target;
            device.Textures[0] = device.Textures[1] = device.Textures[2] = null;
            device.SetRenderTarget(bodyTexture); device.Clear(Color.Transparent);
            var matrix = Matrix.CreateScale(bodySize / (float)Canvas) * Matrix.CreateOrthographicOffCenter(0, bodySize, bodySize, 0, -1, 1);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            try
            {
                // Relative root moves into the target; joints/secondary motion
                // still read the same world pose, never a second simulation.
                GhostSamuraiRigArt.DrawCore(Main.spriteBatch, pose, new Vector2(pose.X, pose.Y) - new Vector2(Canvas / 2),
                    Color.White * SamuraiSpriteFrames.BodyOpacity(pose.Age), matrix);
            }
            finally { Main.spriteBatch.End(); }
            device.Textures[0] = device.Textures[1] = device.Textures[2] = null;
            device.SetRenderTarget(emissionTexture); device.Clear(Color.Transparent);
            device.BlendState = BlendState.Opaque; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            var shader = ShaderManager.GetShader("Convergence.SamuraiComposite");
            shader.TrySetParameter("uWorldViewProjection", Matrix.CreateOrthographicOffCenter(0, emissionSize, emissionSize, 0, -1, 1));
            shader.SetTexture(bodyTexture, 0, SamplerState.LinearClamp);
            Quad(shader, Vector2.Zero, new Vector2(emissionSize), "ExtractPass");
        }
        finally
        {
            device.SetRenderTargets(targets); device.Viewport = viewport;
            device.Textures[0] = t0; device.Textures[1] = t1; device.Textures[2] = t2;
            device.SamplerStates[0] = s0; device.SamplerStates[1] = s1; device.SamplerStates[2] = s2;
            device.SetVertexBuffers(vertices); device.Indices = indices;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster; device.ScissorRectangle = scissor;
        }
    }
    internal static bool Draw(SpriteBatch batch, in SamuraiRigPose pose, Vector2 screen)
    {
        if (Main.dedServ || disabled || pendingRelease || body is null || emission is null || body.IsDisposed || emission.IsDisposed ||
            body.IsUninitialized || emission.IsUninitialized || !GhostSamuraiPresentation.TryPose(out _, out var fight) ||
            fight != preparedFight || MathF.Abs(preparedAge - pose.Age) > 1.25f) return false;
        using var scope = new WorldGraphicsScope(batch);
        var shader = ShaderManager.GetShader("Convergence.SamuraiComposite");
        shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("clock", pose.Age / 60); shader.TrySetParameter("charge", Math.Max(pose.Left.Charge, pose.Right.Charge));
        shader.TrySetParameter("hit", pose.Hit); shader.TrySetParameter("reduced", reducedTarget ? 1f : 0f);
        shader.TrySetParameter("texel", new Vector2(1f / emissionSize));
        shader.SetTexture(body.Target, 0, SamplerState.LinearClamp); shader.SetTexture(emission.Target, 1, SamplerState.LinearClamp);
        shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
        Quad(shader, new Vector2(pose.X, pose.Y) - screen - new Vector2(Canvas / 2), new Vector2(Canvas), "AutoloadPass");
        return true;
    }
    private static void Quad(ManagedShader shader, Vector2 at, Vector2 size, string pass)
    {
        for (int i = 0; i < 6; i++)
        {
            int corner = i switch { 0 => 0, 1 => 2, 2 => 1, 3 => 1, 4 => 2, _ => 3 };
            var uv = new Vector2(corner % 2, corner / 2);
            quad[i] = new(new(at + uv * size, 0), Color.White, uv);
        }
        shader.Apply(pass); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }
}
