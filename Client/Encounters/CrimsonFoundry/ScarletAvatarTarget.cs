#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// The final avatar is composed before world drawing. Its two Luminance-owned
// wrappers persist across fights; only their bounded GPU surfaces are recreated.
[Autoload(Side = ModSide.Client)]
internal sealed class ScarletAvatarTarget : ModSystem
{
    private const int Canvas = 1536;
    private static ManagedRenderTarget? body;
    private static ManagedRenderTarget? emission;
    private static int bodySize, emissionSize;
    private static bool targetReduced, releasePending, disabled;
    private static Guid preparedFight;
    private static int preparedEpoch;
    private static CrimsonStage preparedStage;
    private static float preparedAge, preparedEmergence, preparedDissolve, preparedMelt;

    public override void Load()
    {
        if (!Main.dedServ)
            RenderTargetManager.RenderTargetUpdateLoopEvent += Prepare;
    }

    public override void Unload()
    {
        if (Main.dedServ) return;
        RenderTargetManager.RenderTargetUpdateLoopEvent -= Prepare;
        Invalidate();
        ManagedRenderTarget? oldBody = body, oldEmission = emission;
        body = emission = null;
        // Target disposal remains on the rendering thread even if Mod unload is
        // entered outside PreDraw. The wrappers are registered only once.
        Main.QueueMainThreadAction(() => { oldBody?.Dispose(); oldEmission?.Dispose(); });
    }

    public override void ClearWorld() => ResetWorld();
    public override void OnWorldUnload() => ResetWorld();

    private static void ResetWorld()
    {
        Invalidate();
        disabled = false;
    }

    private static void Invalidate()
    {
        preparedFight = Guid.Empty;
        releasePending = true;
    }

    private static void ReleaseOnRenderThread()
    {
        if (!releasePending) return;
        body?.Dispose();
        emission?.Dispose();
        releasePending = false;
    }

    private static void Prepare()
    {
        if (Main.dedServ || disabled) return;
        // Luminance invokes this during OnPreDraw. Its manager restores targets,
        // but our handler also restores viewport and device state on failure.
        ReleaseOnRenderThread();
        CrimsonBoss? boss = CrimsonPackets.Boss;
        if (!ScarletArticulation.Participant(boss) || boss is not { Fresh: true } || boss.State.Phase != 3 ||
            boss.State.Fight == Guid.Empty || boss.State.Stage == CrimsonStage.Defeat)
        {
            Invalidate();
            ReleaseOnRenderThread();
            return;
        }

        Guid fight = boss.State.Fight;
        int epoch = boss.State.PhaseStart;
        if (preparedFight != Guid.Empty && (preparedFight != fight || preparedEpoch != epoch))
        {
            Invalidate();
            ReleaseOnRenderThread();
        }
        bool reduced = CrimsonVisuals.Reduced;
        int wantedBody = reduced ? 768 : Canvas, wantedEmission = reduced ? 384 : 768;
        bodySize = wantedBody;
        emissionSize = wantedEmission;
        bool sizeChanged = targetReduced != reduced;
        try
        {
            EnsureTargets(sizeChanged);
            targetReduced = reduced;
            float age = CrimsonVisuals.RenderAge(boss);
            float emergence = boss.State.Stage == CrimsonStage.Victory ? 1 :
                CrimsonEnsemble.Emergence(age - epoch, true);
            float dissolve = boss.State.Stage == CrimsonStage.Victory ?
                CrimsonEnsemble.VictoryMelt(CrimsonVisuals.EndingElapsed(boss)) : 0;
            Compose(age, emergence, dissolve, dissolve, reduced);
            preparedFight = fight;
            preparedEpoch = epoch;
            preparedStage = boss.State.Stage;
            preparedAge = age;
            preparedEmergence = emergence;
            preparedDissolve = dissolve;
            preparedMelt = dissolve;
        }
        catch (Exception exception)
        {
            Invalidate();
            disabled = true;
            global::Convergence.ConvergenceMod.Instance.Logger.Warn(
                $"Scarlet avatar target disabled; direct art fallback remains available: {exception}");
            ReleaseOnRenderThread();
        }
    }

    private static void EnsureTargets(bool sizeChanged)
    {
        if (body is null)
            body = new ManagedRenderTarget(false,
                (_, _) => new RenderTarget2D(Main.instance.GraphicsDevice, bodySize, bodySize), false);
        if (emission is null)
            emission = new ManagedRenderTarget(false,
                (_, _) => new RenderTarget2D(Main.instance.GraphicsDevice, emissionSize, emissionSize), false);
        if (sizeChanged || body.IsDisposed)
            body.Recreate(Main.screenWidth, Main.screenHeight);
        if (sizeChanged || emission.IsDisposed)
            emission.Recreate(Main.screenWidth, Main.screenHeight);
    }

    private static void Compose(float age, float emergence, float dissolve, float melt, bool reduced)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        Viewport oldViewport = device.Viewport;
        Rectangle oldScissor = device.ScissorRectangle;
        BlendState oldBlend = device.BlendState;
        DepthStencilState oldDepth = device.DepthStencilState;
        RasterizerState oldRaster = device.RasterizerState;
        VertexBufferBinding[] oldVertices = device.GetVertexBuffers();
        IndexBuffer? oldIndices = device.Indices;
        Texture? t0 = device.Textures[0], t1 = device.Textures[1], t2 = device.Textures[2];
        SamplerState s0 = device.SamplerStates[0], s1 = device.SamplerStates[1], s2 = device.SamplerStates[2];
        try
        {
            RenderTarget2D bodyTexture = body!.Target, emissionTexture = emission!.Target;
            device.Textures[0] = device.Textures[1] = device.Textures[2] = null;
            device.SetRenderTarget(bodyTexture);
            device.Clear(Color.Transparent);
            Matrix bodyProjection = Matrix.CreateScale(bodySize / (float)Canvas) *
                Matrix.CreateOrthographicOffCenter(0, bodySize, bodySize, 0, -1, 1);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            try
            {
                ScarletAvatarArt.Draw(Main.spriteBatch, new Vector2(Canvas / 2), bodyProjection,
                    age, emergence, 1, dissolve, melt);
            }
            finally { Main.spriteBatch.End(); }

            device.Textures[0] = device.Textures[1] = device.Textures[2] = null;
            device.SetRenderTarget(emissionTexture);
            device.Clear(Color.Transparent);
            device.BlendState = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            ManagedShader shader = ShaderManager.GetShader("Convergence.ScarletAvatarComposite");
            shader.TrySetParameter("uWorldViewProjection",
                Matrix.CreateOrthographicOffCenter(0, emissionSize, emissionSize, 0, -1, 1));
            shader.TrySetParameter("clock", age / 60);
            shader.TrySetParameter("texel", new Vector2(1f / bodySize));
            shader.TrySetParameter("signal", new Vector4(0, 0, 1, reduced ? 1 : 0));
            shader.SetTexture(bodyTexture, 0, SamplerState.LinearClamp);
            shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 2, SamplerState.LinearWrap);
            ScarletMaterials.DrawQuad(shader, Vector2.Zero, new Vector2(emissionSize), "ExtractPass");
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldViewport;
            device.Textures[0] = t0; device.Textures[1] = t1; device.Textures[2] = t2;
            device.SamplerStates[0] = s0; device.SamplerStates[1] = s1; device.SamplerStates[2] = s2;
            device.SetVertexBuffers(oldVertices); device.Indices = oldIndices;
            device.BlendState = oldBlend; device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRaster; device.ScissorRectangle = oldScissor;
        }
    }

    internal static void Draw(SpriteBatch batch, Vector2 center, float age, float emergence,
        float alpha, float dissolve = 0, float melt = 0)
    {
        if (Main.dedServ || alpha <= .001f || emergence <= .001f) return;
        CrimsonBoss? boss = CrimsonPackets.Boss;
        bool ready = !disabled && !releasePending && body is not null && emission is not null &&
            !body.IsUninitialized && !emission.IsUninitialized && boss is { Fresh: true } &&
            boss.State.Fight == preparedFight && boss.State.PhaseStart == preparedEpoch &&
            boss.State.Stage == preparedStage && MathF.Abs(preparedAge - age) < 1.25f &&
            MathF.Abs(preparedEmergence - emergence) < .03f &&
            MathF.Abs(preparedDissolve - dissolve) < .03f && MathF.Abs(preparedMelt - melt) < .03f;
        if (!ready)
        {
            ScarletAvatarArt.Draw(batch, center - Main.screenPosition, ScarletMaterials.WorldMatrix,
                age, emergence, alpha, dissolve, melt);
            return;
        }

        ManagedShader shader = ShaderManager.GetShader("Convergence.ScarletAvatarComposite");
        using var scope = new WorldGraphicsScope(batch);
        shader.TrySetParameter("uWorldViewProjection", ScarletMaterials.WorldMatrix);
        shader.TrySetParameter("clock", age / 60);
        shader.TrySetParameter("texel", new Vector2(1f / emissionSize));
        shader.TrySetParameter("signal", new Vector4(0, 0, alpha, targetReduced ? 1 : 0));
        shader.SetTexture(body!.Target, 0, SamplerState.LinearClamp);
        shader.SetTexture(emission!.Target, 1, SamplerState.LinearClamp);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 2, SamplerState.LinearWrap);
        ScarletMaterials.DrawQuad(shader, center - Main.screenPosition - new Vector2(Canvas / 2),
            new Vector2(Canvas), "AutoloadPass");
    }
}
