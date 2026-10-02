#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Production side of the Terraria-free Vfx/ seam: Luminance shaders and noise textures, and the
// per-frame ScarletView. The offline preview supplies the same two things from a hidden FNA device
// (tools/fixtures/ScarletPreviewAssets.cs). Client only; nothing here runs on a Dedicated Server.
internal sealed class ScarletVfxHost : IScarletAssets
{
    internal static readonly ScarletVfxHost Assets = new();
    private readonly Dictionary<string, LuminanceShader> shaders = new();

    // "ScarletInk" -> ShaderManager "Convergence.ScarletInk", the same autoload route as "Convergence.PortalBeam"
    // (Assets/AutoloadedEffects/Shaders/ScarletInk.fxc). The wrapper is reused while ShaderManager hands out the same instance.
    public IScarletShader GetShader(string name)
    {
        var managed = ShaderManager.GetShader("Convergence." + name);
        if (shaders.TryGetValue(name, out var found) && ReferenceEquals(found.Shader, managed)) return found;
        var wrapper = new LuminanceShader(managed);
        shaders[name] = wrapper;
        return wrapper;
    }

    // Looked up on every call and never cached: an Asset can still be loading, and the registry owns its lifetime.
    public Texture2D GetTexture(string name) => name switch
    {
        "Noise/WavyBlotchNoise" => MiscTexturesRegistry.WavyBlotchNoise.Value,
        "Noise/TurbulentNoise" => MiscTexturesRegistry.TurbulentNoise.Value,
        "Noise/DendriticNoiseZoomedOut" => MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,
        _ => throw new ArgumentException("Scarlet Vfx has no production texture named " + name, nameof(name))
    };

    // The same transform CrimsonEnergy.Draw builds: GameViewMatrix.TransformationMatrix * ortho(0, W, H, 0) over
    // (world - Main.screenPosition), with the real back-buffer viewport. 'age' is the fractional Raid clock
    // (CrimsonVisuals.RenderAge), split into whole ticks and the remainder; Tick + Fraction gives it back exactly.
    internal static ScarletView View(float age)
    {
        var device = Main.instance.GraphicsDevice;
        int tick = (int)MathF.Floor(age);
        return new ScarletView(device, Main.screenPosition, Main.GameViewMatrix.TransformationMatrix,
            Main.GameViewMatrix.Zoom.X, device.Viewport.Width, device.Viewport.Height,
            tick, age - tick, CrimsonVisuals.Reduced);
    }

    private sealed class LuminanceShader : IScarletShader
    {
        internal readonly ManagedShader Shader;
        internal LuminanceShader(ManagedShader shader) => Shader = shader;
        public void Set(string name, float value) => Shader.TrySetParameter(name, value);
        public void Set(string name, Vector4 value) => Shader.TrySetParameter(name, value);
        public void Set(string name, Matrix value) => Shader.TrySetParameter(name, value);
        public void Apply(string pass) => Shader.Apply(pass);
    }
}
