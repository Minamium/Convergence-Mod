#nullable enable
using System;
using Convergence.Client.Weapons;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Runtime half of the Doll weapon layer. Verified draw order in the installed tModLoader (Main.DoDraw):
// CheckMonoliths (camera final) -> ... -> DrawProjectiles -> DrawPlayers_AfterProjectiles -> over-player
// projectiles, NPCs, items, gore, dust; the SpriteBatch is ended after both of those calls. So:
//  - CheckMonoliths, before orig: sources record; Front sprites render into the Art target and light into
//    the Light target (two half-resolution Luminance targets, created on first use).
//  - DrawProjectiles, after orig: Back sprites draw directly in world space, behind the players.
//  - DrawPlayers_AfterProjectiles, after orig: Art, then Light, composite in front of every player.
// A throwing source is dropped (one warning). A render/composite failure or a missing shader degrades the
// layer to direct point-sampled sprite draws without light (one warning); a failing fallback disables it.
// The targets are released after IdleFrames frames without Front content and recreated on demand.
// Each recording is consumed once: Back by DrawBack, Front by CompositeFront (see there).
// Client presentation only: nothing here touches hits, input, packets or saved state.
internal static partial class DollWeaponLayer
{
    internal const int IdleFrames = 600;

    // Deliberately no static initializers: the canvas alone holds about 1.7 MB of command arrays, which a dedicated
    // server must never allocate (a type initializer would run on the first Add). Hook() creates both on the
    // client, and every hooked path reads them through Sources and Canvas.
    private static IDollWeaponSource?[]? sources;
    private static DollWeaponCanvas? canvas;
    private static ManagedRenderTarget? artTarget, lightTarget;
    private static int count, idle, refused;
    private static bool hooked, recorded, backPending, artReady, lightReady, degraded, disabled, sourceWarned, materialWarned;

    private static IDollWeaponSource?[] Sources => sources!;
    private static DollWeaponCanvas Canvas => canvas!;

    internal static int Count => count;
    // Sources refused because MaxSources were live (since load).
    internal static int Refused => refused;
    // Commands dropped over budget in the last recorded frame.
    internal static int Dropped => canvas?.Dropped ?? 0;
    internal static bool Degraded => degraded;

    static partial void AddSource(IDollWeaponSource source, ref bool accepted)
    {
        if (Main.dedServ || disabled || !hooked || source is null) return;
        IDollWeaponSource?[] live = Sources;
        for (int i = 0; i < count; i++)
        {
            if (ReferenceEquals(live[i], source))
            {
                accepted = true;
                return;
            }
        }
        if (count >= MaxSources)
        {
            refused++;
            return;
        }
        live[count++] = source;
        accepted = true;
    }

    // Drops every source and recorded command (world unload, Mod unload). Idempotent.
    internal static void Clear()
    {
        if (sources is not null) Array.Clear(sources, 0, count);
        count = 0;
        canvas?.Clear();
        recorded = backPending = artReady = lightReady = false;
    }

    internal static void Hook()
    {
        if (hooked || Main.dedServ) return;
        sources ??= new IDollWeaponSource?[MaxSources];
        canvas ??= new DollWeaponCanvas();
        On_Main.CheckMonoliths += RenderLayer;
        On_Main.DrawProjectiles += DrawBack;
        On_Main.DrawPlayers_AfterProjectiles += CompositeFront;
        hooked = true;
    }

    // Idempotent: unhooks, drops all state and disposes both targets on the main thread.
    internal static void Unhook()
    {
        if (hooked)
        {
            On_Main.CheckMonoliths -= RenderLayer;
            On_Main.DrawProjectiles -= DrawBack;
            On_Main.DrawPlayers_AfterProjectiles -= CompositeFront;
        }
        hooked = false;
        Clear();
        sources = null;
        canvas = null;
        degraded = disabled = sourceWarned = materialWarned = false;
        idle = refused = 0;
        ManagedRenderTarget? oldArt = artTarget, oldLight = lightTarget;
        artTarget = lightTarget = null;
        if (oldArt is not null || oldLight is not null)
            Main.QueueMainThreadAction(() => { oldArt?.Dispose(); oldLight?.Dispose(); });
    }

    private static void RenderLayer(On_Main.orig_CheckMonoliths orig)
    {
        recorded = backPending = artReady = lightReady = false;
        if (!disabled && !Main.dedServ && !Main.gameMenu)
        {
            try
            {
                Record();
            }
            catch (Exception exception)
            {
                Disable("record", exception);
            }
            if (recorded && !degraded)
            {
                try
                {
                    RenderTargets();
                }
                catch (Exception exception)
                {
                    Degrade("render", exception);
                }
            }
        }
        orig();
    }

    private static void Record()
    {
        float fraction = WeaponDrawClock.Fraction;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        DollWeaponCanvas canvas = Canvas;
        canvas.Begin(Main.screenPosition, Main.screenWidth, Main.screenHeight, fraction, reduced,
            Main.GameUpdateCount + (double)fraction);
        if (count > 0) Emit(canvas);
        canvas.EndRecording();
        recorded = true;
        backPending = canvas.HasBack;
        if (canvas.HasArt || canvas.HasLight) idle = 0;
        else if ((idle = Math.Min(idle + 1, IdleFrames)) >= IdleFrames) ReleaseTargets();
    }

    // Compacts in place; a source registered during Emit is appended and emits this frame too.
    private static void Emit(DollWeaponCanvas canvas)
    {
        IDollWeaponSource?[] sources = Sources;
        int kept = 0;
        for (int i = 0; i < count; i++)
        {
            IDollWeaponSource source = sources[i]!;
            DollWeaponCanvas.Checkpoint mark = canvas.Mark();
            bool keep;
            try
            {
                keep = source.Emit(canvas);
            }
            catch (Exception exception)
            {
                keep = false;
                canvas.Rewind(mark);
                if (!sourceWarned)
                {
                    sourceWarned = true;
                    ConvergenceMod.Instance.Logger.Warn($"Doll weapon source {source.GetType().Name} threw and was dropped (later drops are silent): {exception}");
                }
            }
            if (keep) sources[kept++] = source;
        }
        Array.Clear(sources, kept, count - kept);
        count = kept;
    }

    private static void RenderTargets()
    {
        DollWeaponCanvas canvas = Canvas;
        if (!canvas.HasArt && !canvas.HasLight) return;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Effect effect = Shader();
        DollDeviceState state = DollDeviceState.Capture(device, true);
        try
        {
            if (canvas.HasArt)
            {
                RenderTarget2D art = Target(ref artTarget);
                Bind(device, art);
                canvas.DrawArt(device, effect, art.Width, art.Height);
                artReady = true;
            }
            if (canvas.HasLight)
            {
                RenderTarget2D light = Target(ref lightTarget);
                Bind(device, light);
                canvas.DrawLight(device, effect, light.Width, light.Height);
                lightReady = true;
            }
        }
        finally
        {
            state.Restore(device);
        }
        if (canvas.MaterialError is { } error && !materialWarned)
        {
            materialWarned = true;
            ConvergenceMod.Instance.Logger.Warn($"A Doll weapon energy material failed; its batches are skipped (later failures are silent): {error}");
        }
    }

    private static void Bind(GraphicsDevice device, RenderTarget2D target)
    {
        device.SetRenderTarget(target);
        device.Clear(Color.Transparent);
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
    }

    // One Luminance wrapper per target for the session (Dispose does not unregister it); an idle-released
    // surface is recreated here on the main thread.
    private static RenderTarget2D Target(ref ManagedRenderTarget? slot)
    {
        slot ??= new ManagedRenderTarget(true, CreateTarget, false);
        if (slot.IsDisposed) slot.Recreate(Main.screenWidth, Main.screenHeight);
        return slot.Target;
    }

    private static RenderTarget2D CreateTarget(int width, int height)
        => new(Main.instance.GraphicsDevice, Math.Max(1, width / 2 + 2), Math.Max(1, height / 2 + 2));

    // Main thread (called while drawing): releases the surfaces, keeping the wrappers.
    private static void ReleaseTargets()
    {
        if (artTarget is { IsDisposed: false }) artTarget.Dispose();
        if (lightTarget is { IsDisposed: false }) lightTarget.Dispose();
    }

    private static Effect Shader() => ShaderManager.GetShader(DollPixelArt.ShaderName).WrappedEffect;

    private static void DrawBack(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);
        // A recording draws its Back stratum once. Main.DrawCapture (camera mode, screenshots) calls this hook
        // without CheckMonoliths, and must not replay the last frame's sprites at that frame's position and zoom.
        bool pending = backPending;
        backPending = false;
        if (!pending || disabled) return;
        DollWeaponCanvas canvas = Canvas;
        try
        {
            if (degraded)
            {
                DrawFallback(DollStratum.Back);
                return;
            }
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Effect effect = Shader();
            DollDeviceState state = DollDeviceState.Capture(device, false);
            try
            {
                device.BlendState = BlendState.AlphaBlend;
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;
                canvas.DrawBack(device, effect, Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
            }
            finally
            {
                state.Restore(device);
            }
        }
        catch (Exception exception)
        {
            if (degraded) Disable("fallback", exception);
            else Degrade("back stratum", exception);
        }
    }

    private static void CompositeFront(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        orig(self);
        if (!recorded || disabled) return;
        DollWeaponCanvas canvas = Canvas;
        try
        {
            if (degraded)
            {
                if (canvas.HasArt) DrawFallback(DollStratum.Front);
                return;
            }
            if (!artReady && !lightReady) return;
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Effect effect = Shader();
            DollDeviceState state = DollDeviceState.Capture(device, false);
            try
            {
                device.BlendState = BlendState.AlphaBlend;
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;
                Matrix view = Main.GameViewMatrix.TransformationMatrix;
                Vector2 offset = canvas.Origin - Main.screenPosition;
                RenderTarget2D? art = artReady && artTarget is { IsDisposed: false } ? artTarget.Target : null;
                if (art is not null) DollPixelArt.CompositeArt(device, effect, art, canvas.ArtArea, offset, view);
                if (lightReady && lightTarget is { IsDisposed: false })
                    DollPixelArt.CompositeLight(device, effect, lightTarget.Target, art, canvas.LightArea, offset, view, canvas.Reduced);
            }
            finally
            {
                state.Restore(device);
            }
        }
        catch (Exception exception)
        {
            if (degraded) Disable("fallback", exception);
            else Degrade("composite", exception);
        }
        finally
        {
            // Consumed: the recording belongs to the frame CheckMonoliths made. Main.DrawCapture (camera mode,
            // screenshots) calls this hook without CheckMonoliths and would otherwise composite the previous
            // frame's targets again, framed for the wrong view and zoom.
            recorded = artReady = lightReady = false;
        }
    }

    // Degraded path: plain point-sampled sprites at 2 px per texel; light is skipped.
    private static void DrawFallback(DollStratum stratum)
    {
        DollWeaponCanvas canvas = Canvas;
        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            canvas.DrawSpritesDirect(batch, stratum, Main.screenPosition);
        }
        finally
        {
            batch.End();
        }
    }

    private static void Degrade(string stage, Exception exception)
    {
        artReady = lightReady = false;
        if (degraded) return;
        degraded = true;
        ConvergenceMod.Instance.Logger.Warn($"Doll weapon layer degraded after a {stage} failure; weapon sprites draw directly without light: {exception}");
    }

    private static void Disable(string stage, Exception exception)
    {
        bool wasDisabled = disabled;
        disabled = true;
        Clear();
        if (!wasDisabled)
            ConvergenceMod.Instance.Logger.Warn($"Doll weapon layer disabled after a {stage} failure: {exception}");
    }
}

// tML lifetime for the layer: hooks on load, every source dropped on world unload, the targets released on
// the main thread at Mod unload. Never loaded on a dedicated server.
[Autoload(Side = ModSide.Client)]
internal sealed class DollWeaponLayerSystem : ModSystem
{
    public override void Load() => DollWeaponLayer.Hook();

    public override void Unload()
    {
        DollWeaponLayer.Unhook();
        DollWeaponTextures.Reset();
        DollWeaponArmDraw.ClearAll();
    }

    public override void OnWorldUnload()
    {
        DollWeaponLayer.Clear();
        DollWeaponArmDraw.ClearAll();
    }
}
