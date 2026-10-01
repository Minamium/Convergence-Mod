#nullable enable
using System;
using Convergence.Client.Weapons;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Runtime half of the Ebon pixel layer (Soboro's ordering): sources emit into the canvas in
// CheckMonoliths once the camera is final, the half-resolution target is drawn there, and the
// projectile layer composites it once. The source list is a fixed array; a throwing source is
// dropped, a rendering failure disables the layer for the session with one warning. Client
// presentation only: nothing here touches hits, input, packets or saved state.
internal static partial class EbonPixelLayer
{
    private static readonly IEbonPixelSource?[] sources = new IEbonPixelSource?[MaxSources];
    private static readonly EbonPixelCanvas canvas = new();
    private static ManagedRenderTarget? target;
    private static Asset<Texture2D>? noise;
    private static int count;
    private static bool hooked, prepared, disabled, sourceWarned;
    private static Rectangle bounds;

    internal static int Count => count;

    static partial void AddSource(IEbonPixelSource source, ref bool accepted)
    {
        if (Main.dedServ || disabled || !hooked || source is null) return;
        for (int i = 0; i < count; i++)
        {
            if (ReferenceEquals(sources[i], source))
            {
                accepted = true;
                return;
            }
        }
        if (count >= MaxSources) return;
        sources[count++] = source;
        accepted = true;
    }

    internal static void Clear()
    {
        Array.Clear(sources, 0, count);
        count = 0;
        prepared = false;
    }

    internal static void Hook()
    {
        if (hooked) return;
        On_Main.CheckMonoliths += RenderLayer;
        On_Main.DrawProjectiles += CompositeLayer;
        hooked = true;
    }

    internal static void Unhook()
    {
        if (hooked)
        {
            On_Main.CheckMonoliths -= RenderLayer;
            On_Main.DrawProjectiles -= CompositeLayer;
        }
        hooked = false;
        Clear();
        noise = null;
        disabled = sourceWarned = false;
        ManagedRenderTarget? old = target;
        target = null;
        if (old is not null) Main.QueueMainThreadAction(old.Dispose);
    }

    private static void Disable(string stage, Exception exception)
    {
        disabled = true;
        prepared = false;
        Clear();
        ConvergenceMod.Instance.Logger.Warn($"Ebon pixel layer disabled after a {stage} failure; reward bodies still draw: {exception}");
    }

    private static void RenderLayer(On_Main.orig_CheckMonoliths orig)
    {
        prepared = false;
        if (!disabled && !Main.gameMenu && count > 0)
        {
            try
            {
                Render();
            }
            catch (Exception exception)
            {
                Disable("render", exception);
            }
        }
        orig();
    }

    private static void Render()
    {
        float fraction = WeaponDrawClock.Fraction;
        bool reduced = ModContent.GetInstance<EbonVisualConfig>().ReducedEffects;
        canvas.Begin(Main.screenPosition, Math.Max(1, Main.screenWidth / 2), Math.Max(1, Main.screenHeight / 2),
            fraction, reduced, Main.GameUpdateCount + (double)fraction);
        Emit();
        Rectangle dots = canvas.DotBounds;
        if (canvas.Empty || dots.IsEmpty) return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        target ??= new ManagedRenderTarget(true, (width, height) =>
            new RenderTarget2D(Main.instance.GraphicsDevice, Math.Max(1, width / 2), Math.Max(1, height / 2)));
        RenderTarget2D texture = target.Target;
        Effect effect = ShaderManager.GetShader(EbonPixelArt.ShaderName).WrappedEffect;
        noise ??= ModContent.Request<Texture2D>(EbonPixelArt.NoisePath, AssetRequestMode.ImmediateLoad);
        EbonDeviceState state = EbonDeviceState.Capture(device, true);
        try
        {
            device.SetRenderTarget(texture);
            device.Clear(Color.Transparent);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            canvas.Draw(device, effect, noise.Value, texture.Width, texture.Height);
        }
        finally
        {
            state.Restore(device);
        }
        bounds = Rectangle.Intersect(new Rectangle(dots.X * 2, dots.Y * 2, dots.Width * 2, dots.Height * 2),
            new Rectangle(0, 0, Main.screenWidth, Main.screenHeight));
        prepared = bounds.Width > 0 && bounds.Height > 0;
    }

    // Compacts in place; a source registered during Emit is appended and emits this frame too.
    private static void Emit()
    {
        int kept = 0;
        for (int i = 0; i < count; i++)
        {
            IEbonPixelSource source = sources[i]!;
            EbonPixelCanvas.Checkpoint mark = canvas.Mark();
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
                    ConvergenceMod.Instance.Logger.Warn($"Ebon pixel source {source.GetType().Name} threw and was dropped (later drops are silent): {exception}");
                }
            }
            if (keep) sources[kept++] = source;
        }
        Array.Clear(sources, kept, count - kept);
        count = kept;
    }

    private static void CompositeLayer(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);
        if (!prepared || disabled || target is null) return;
        prepared = false;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        EbonDeviceState state = EbonDeviceState.Capture(device, false);
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            EbonPixelArt.Composite(device, ShaderManager.GetShader(EbonPixelArt.ShaderName).WrappedEffect, target.Target,
                bounds, Main.GameViewMatrix.TransformationMatrix, canvas.Reduced);
        }
        catch (Exception exception)
        {
            Disable("composite", exception);
        }
        finally
        {
            state.Restore(device);
        }
    }
}

// tML lifetime for the layer: hooks on load, every source dropped on world unload, the target
// released on the main thread at Mod unload. Never loaded on a dedicated server.
[Autoload(Side = ModSide.Client)]
internal sealed class EbonPixelLayerSystem : ModSystem
{
    public override void Load() => EbonPixelLayer.Hook();
    public override void Unload() => EbonPixelLayer.Unhook();
    public override void OnWorldUnload() => EbonPixelLayer.Clear();
}
