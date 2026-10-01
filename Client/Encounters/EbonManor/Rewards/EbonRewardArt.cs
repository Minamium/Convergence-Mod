using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// One switch between the final reward pixel art (ER01-ER10, exported to Assets/Textures/Items/EbonRewards
// at one texel per logical pixel, drawn at PixelScale with point sampling) and the Raid textures that stand
// in until the art is delivered. Draw final art with SamplerState.PointClamp; placeholders are painted and
// may use LinearClamp.
internal static class EbonRewardArt
{
    internal const string Root = "Convergence/Assets/Textures/Items/EbonRewards/";
    internal const float PixelScale = 2f;
    private static readonly Dictionary<string, bool> present = new();

    internal static bool HasFinal(string name)
    {
        if (!present.TryGetValue(name, out bool ok))
            present[name] = ok = ModContent.HasAsset(Root + name);
        return ok;
    }
    internal static Texture2D Final(string name) => ModContent.Request<Texture2D>(Root + name).Value;
    internal static Texture2D Raid(string name) => EbonMaterials.Texture(name);
    internal static void Reset() => present.Clear();

    // Furniture for the thimble and the companion. Final: Furniture.png, 8 cells of 48x64 logical pixels in a
    // row (armchair, candelabra, portrait, clock, birdcage, mirror, music box, cello), eyelet at the top centre.
    // Placeholder: the Raid's painted ManorProps.png (seven 176 px cells), the cello replaced by the armchair.
    internal readonly record struct Sprite(Texture2D Texture, Rectangle Source, float Scale, bool Pixel)
    {
        internal Vector2 Origin => new(Source.Width * .5f, Source.Height * .5f);
        internal Vector2 Size => new(Source.Width * Scale, Source.Height * Scale);
    }

    internal const int FurnitureCount = 8;
    internal static Sprite Furniture(int index)
    {
        index = ((index % FurnitureCount) + FurnitureCount) % FurnitureCount;
        if (HasFinal("Furniture"))
            return new Sprite(Final("Furniture"), new Rectangle(index * 48, 0, 48, 64), PixelScale, true);
        int cell = index == 7 ? 0 : index; // the Raid sheet has no cello
        return new Sprite(Raid("ManorProps"), new Rectangle(cell * 176, 0, 176, 176), .42f, false);
    }

    // Final: Piano.png (about 80x48 logical). Placeholder: the Raid's wide chandelier is the only large prop.
    internal static Sprite Piano()
        => HasFinal("Piano")
            ? new Sprite(Final("Piano"), new Rectangle(0, 0, Final("Piano").Width, Final("Piano").Height), PixelScale, true)
            : new Sprite(Raid("ChandelierWide"), new Rectangle(0, 0, Raid("ChandelierWide").Width, Raid("ChandelierWide").Height), .55f, false);

    // Final: Chandelier.png / ChandelierSmall.png (unlit, about 32x36 / 24x28 logical). Placeholder: ChandelierTall.
    internal static Sprite Chandelier(bool small)
    {
        string name = small ? "ChandelierSmall" : "Chandelier";
        if (HasFinal(name))
            return new Sprite(Final(name), new Rectangle(0, 0, Final(name).Width, Final(name).Height), PixelScale, true);
        var tall = Raid("ChandelierTall");
        return new Sprite(tall, new Rectangle(0, 0, tall.Width, tall.Height), small ? .2f : .26f, false);
    }
}
