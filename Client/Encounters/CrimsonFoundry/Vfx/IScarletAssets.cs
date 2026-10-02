#nullable enable
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// Effects and textures looked up by name, so a Vfx class never asks tModLoader
// or Luminance for a resource. Production implements it over ModContent /
// ShaderManager; the offline preview implements it over loose .fxc files, PNGs
// and the noise packed in Luminance.tmod.
//
// Names are plain and extension-less:
//   effects  : "ScarletRibbon", "PortalBeam", ... (Assets/AutoloadedEffects/Shaders/<name>.fxc)
//   textures : "Noise/WavyBlotchNoise", "Noise/TurbulentNoise", "Noise/DendriticNoiseZoomedOut",
//              "Backgrounds/ScarletSanctum", "CrimsonFoundry/ScarletConjurer", ...
internal interface IScarletAssets
{
    Effect GetEffect(string name);

    // Colour is premultiplied alpha, like every texture Terraria hands to a SpriteBatch.
    Texture2D GetTexture(string name);
}
