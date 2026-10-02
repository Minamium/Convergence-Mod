#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// Terraria side of ScarletRewardInk (REWARDS.md#layering): reward ink draws once per frame through DrawWorld, which
// is frame-stamped and idempotent. Scarlet's PostDrawTiles drawers (CrimsonGestureVisuals, CrimsonChorusVisuals) call
// it before they draw forecasts and chorus markers, and ScarletRewardInkSystem calls it otherwise; whichever runs
// first draws. Reward ink therefore always lies beneath the Raid's forecasts and chorus markers, in the Raid strikes'
// world layer (beneath NPCs and players). A rendering exception disables reward ink for the session, with one warning.
// Client only: nothing here exists or runs on a Dedicated Server.
internal sealed partial class ScarletRewardInk
{
    private static readonly List<IScarletInkEmitter> emitters = new();
    private static ScarletRewardInk? instance;
    private static ScarletRewardParticles? particles;
    private static long frame, drawnFrame = -1;
    private static bool disabled;

    // The shared particle pool (embers, smoke, bone chips, wax flakes). Spawn from client visuals only.
    internal static ScarletRewardParticles Particles => particles ??= new ScarletRewardParticles();

    // Weapon visuals register once (ModSystem.Load on the client) and unregister on Unload.
    internal static void Register(IScarletInkEmitter emitter)
    {
        if (!emitters.Contains(emitter)) emitters.Add(emitter);
    }
    internal static void Unregister(IScarletInkEmitter emitter) => emitters.Remove(emitter);

    // The ink clock: whole game ticks plus the draw fraction, wrapped every ten minutes so the shader's float clock
    // keeps its precision. Nothing about it is shared with other players or with the music.
    internal static ScarletView View()
    {
        const uint wrap = 60 * 60 * 10;
        var device = Main.instance.GraphicsDevice;
        return new ScarletView(device, Main.screenPosition, Main.GameViewMatrix.TransformationMatrix, Main.GameViewMatrix.Zoom.X,
            device.Viewport.Width, device.Viewport.Height, (int)(Main.GameUpdateCount % wrap), WeaponDrawClock.Fraction,
            CrimsonVisuals.Reduced);
    }

    internal static void BeginFrame() => frame++;

    internal static void DrawWorld()
    {
        if (Main.dedServ || Main.gameMenu || disabled || drawnFrame == frame) return;
        drawnFrame = frame;
        if (emitters.Count == 0 && (particles is null || particles.Count == 0)) return;
        var device = Main.instance.GraphicsDevice;
        try
        {
            var view = View();
            instance ??= new ScarletRewardInk();
            var canvas = instance.Begin(view);
            for (int i = 0; i < emitters.Count; i++) emitters[i].Emit(canvas, view);
            if (canvas.PathCount == 0 && (particles is null || particles.Count == 0)) return;
            // Device state is saved only when something draws (saving reads the vertex-buffer bindings into a new array).
            var saved = new SavedDevice(device);
            try
            {
                instance.Draw(view, ScarletVfxHost.Assets);
                particles?.Draw(Main.spriteBatch, view, ScarletVfxHost.Assets);
            }
            finally { saved.Restore(device); }
        }
        catch (Exception e)
        {
            disabled = true;
            global::Convergence.ConvergenceMod.Instance.Logger.Warn("Scarlet reward ink disabled for this session after a drawing error; the weapons still work.", e);
        }
    }

    internal static void Tick()
    {
        if (!Main.gamePaused) particles?.Update();
    }

    internal static void ClearWorld() => particles?.Clear();

    internal static void Unload()
    {
        emitters.Clear();
        particles?.Dispose();
        particles = null;
        instance = null;
        disabled = false;
        drawnFrame = -1;
    }

    // DrawWorld runs outside any SpriteBatch; it puts back exactly what it touched.
    internal readonly struct SavedDevice
    {
        private readonly BlendState blend;
        private readonly DepthStencilState depth;
        private readonly RasterizerState raster;
        private readonly VertexBufferBinding[] bindings;
        private readonly IndexBuffer? indices;
        private readonly Texture? t0, t1, t2;
        private readonly SamplerState s0, s1, s2;
        internal SavedDevice(GraphicsDevice device)
        {
            blend = device.BlendState; depth = device.DepthStencilState; raster = device.RasterizerState;
            bindings = device.GetVertexBuffers(); indices = device.Indices;
            t0 = device.Textures[0]; t1 = device.Textures[1]; t2 = device.Textures[2];
            s0 = device.SamplerStates[0]; s1 = device.SamplerStates[1]; s2 = device.SamplerStates[2];
        }
        internal void Restore(GraphicsDevice device)
        {
            device.Textures[0] = t0; device.Textures[1] = t1; device.Textures[2] = t2;
            device.SamplerStates[0] = s0; device.SamplerStates[1] = s1; device.SamplerStates[2] = s2;
            device.SetVertexBuffers(bindings); device.Indices = indices;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
        }
    }
}

// Drives ScarletRewardInk on the client: the per-frame stamp, the fallback draw, particle ticks and cleanup.
[Autoload(Side = ModSide.Client)]
internal sealed class ScarletRewardInkSystem : ModSystem
{
    // tML calls this once at the start of every DoDraw: one stamp per rendered frame.
    public override void ModifyScreenPosition() => ScarletRewardInk.BeginFrame();
    public override void PostDrawTiles() => ScarletRewardInk.DrawWorld();
    public override void PostUpdateEverything() => ScarletRewardInk.Tick();
    public override void ClearWorld() => ScarletRewardInk.ClearWorld();
    public override void OnWorldUnload() => ScarletRewardInk.ClearWorld();
    public override void Unload() => ScarletRewardInk.Unload();
}
