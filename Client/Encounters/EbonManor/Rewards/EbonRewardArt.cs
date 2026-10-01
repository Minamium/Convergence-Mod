using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// One switch between the final reward pixel art (ER01-ER10, exported to Assets/Textures/Items/EbonRewards
// at one texel per logical pixel, drawn at PixelScale with point sampling) and the Raid textures that stand
// in until the art is delivered. Draw final art with SamplerState.PointClamp; placeholders are painted and
// may use LinearClamp. Everything resolved here is cached after its first use (the visuals ask every frame):
// the asset lookups, the sprites and their source rectangles. Reset() forgets all of it.
internal static class EbonRewardArt
{
    internal const string Root = "Convergence/Assets/Textures/Items/EbonRewards/";
    internal const float PixelScale = 2f;
    private static readonly Dictionary<string, bool> present = new();
    private static readonly Dictionary<string, Texture2D> finals = new(), raids = new();
    private static readonly Sprite?[] furniture = new Sprite?[FurnitureCount];
    private static Sprite? piano, chandelier, chandelierSmall;

    internal static bool HasFinal(string name)
    {
        if (!present.TryGetValue(name, out bool ok))
            present[name] = ok = ModContent.HasAsset(Root + name);
        return ok;
    }

    internal static Texture2D Final(string name)
    {
        if (!finals.TryGetValue(name, out Texture2D texture))
            finals[name] = texture = ModContent.Request<Texture2D>(Root + name).Value;
        return texture;
    }

    internal static Texture2D Raid(string name)
    {
        if (!raids.TryGetValue(name, out Texture2D texture))
            raids[name] = texture = EbonMaterials.Texture(name);
        return texture;
    }

    internal static void Reset()
    {
        present.Clear();
        finals.Clear();
        raids.Clear();
        Array.Clear(furniture);
        piano = chandelier = chandelierSmall = null;
    }

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
        return furniture[index] ??= BuildFurniture(index);
    }

    private static Sprite BuildFurniture(int index)
    {
        if (HasFinal("Furniture"))
            return new Sprite(Final("Furniture"), new Rectangle(index * 48, 0, 48, 64), PixelScale, true);
        int cell = index == 7 ? 0 : index; // the Raid sheet has no cello
        return new Sprite(Raid("ManorProps"), new Rectangle(cell * 176, 0, 176, 176), .42f, false);
    }

    // Final: Piano.png (76x57 logical). Placeholder: the Raid's wide chandelier is the only large prop.
    internal static Sprite Piano() => piano ??= Whole("Piano", "ChandelierWide", .55f);

    // Final: Chandelier.png / ChandelierSmall.png (unlit, 41x68 / 34x49 logical). Placeholder: ChandelierTall.
    internal static Sprite Chandelier(bool small)
    {
        if (small) return chandelierSmall ??= Whole("ChandelierSmall", "ChandelierTall", .2f);
        return chandelier ??= Whole("Chandelier", "ChandelierTall", .26f);
    }

    // The whole of the final texture `name` at PixelScale, or of the Raid texture `fallback` at `fallbackScale`.
    private static Sprite Whole(string name, string fallback, float fallbackScale)
    {
        bool final = HasFinal(name);
        Texture2D texture = final ? Final(name) : Raid(fallback);
        return new Sprite(texture, texture.Bounds, final ? PixelScale : fallbackScale, final);
    }
}
