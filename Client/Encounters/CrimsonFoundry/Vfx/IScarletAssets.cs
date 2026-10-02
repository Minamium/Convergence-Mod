#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// Shaders and textures looked up by name, so a Vfx class never asks tModLoader
// or Luminance for a resource. Production implements it over ShaderManager /
// MiscTexturesRegistry (ScarletVfxHost); the offline preview implements it over
// loose .fxc files, PNGs and the noise packed in Luminance.tmod.
//
// Names are plain and extension-less:
//   shaders  : "ScarletInk", ... (Assets/AutoloadedEffects/Shaders/<name>.fxc; production adds the "Convergence." prefix)
//   textures : "Noise/WavyBlotchNoise", "Noise/TurbulentNoise", "Noise/DendriticNoiseZoomedOut", ...
internal interface IScarletAssets
{
    IScarletShader GetShader(string name);

    // Colour is premultiplied alpha, like every texture Terraria hands to a SpriteBatch.
    // Production hands out the live Luminance asset, so callers bind it at once and never keep it.
    Texture2D GetTexture(string name);
}

// The only things a Vfx class does with a shader: set parameters and apply a pass.
// An unknown parameter is ignored (the preview's raw Effect and Luminance's ManagedShader both do that);
// textures are bound straight on the GraphicsDevice by the caller.
internal interface IScarletShader
{
    void Set(string name, float value);
    void Set(string name, Vector4 value);
    void Set(string name, Matrix value);
    void Apply(string pass);
}
