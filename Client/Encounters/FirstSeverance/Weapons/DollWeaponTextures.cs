#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Final Doll weapon textures (Assets/Textures/Items/DollWeapons/). The Asset handle is requested with
// ImmediateLoad on its first use on the draw thread and kept; every draw reads Asset.Value. A Texture2D
// taken from an AsyncLoad request can still be the empty placeholder and would then draw nothing for the
// rest of the session. A missing file is remembered and returns null without asking again.
internal static class DollWeaponTextures
{
    internal const string Root = "Convergence/Assets/Textures/Items/DollWeapons/";

    private static readonly Dictionary<string, Asset<Texture2D>?> assets = new();

    internal static Texture2D? Get(string name)
    {
        if (Main.dedServ || string.IsNullOrEmpty(name)) return null;
        if (!assets.TryGetValue(name, out Asset<Texture2D>? asset))
        {
            string path = Root + name;
            asset = ModContent.HasAsset(path) ? ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad) : null;
            assets[name] = asset;
        }
        return asset?.Value;
    }

    // Mod unload: drops the handles (tModLoader owns and disposes the assets themselves).
    internal static void Reset() => assets.Clear();
}
