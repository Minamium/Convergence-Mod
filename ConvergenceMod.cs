#nullable enable

using System;
using System.IO;
using Convergence.Common.Networking;
using Terraria.ModLoader;

namespace Convergence;

public sealed class ConvergenceMod : Mod
{
    private static ConvergenceMod? instance;

    internal static ConvergenceMod Instance => instance
        ?? throw new InvalidOperationException("ConvergenceMod has not finished loading.");

    public override void Load()
    {
        instance = this;
    }

    public override void Unload()
    {
        EncounterPacketRouter.Reset();
        instance = null;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        EncounterPacketRouter.Handle(reader, whoAmI);
    }
}
