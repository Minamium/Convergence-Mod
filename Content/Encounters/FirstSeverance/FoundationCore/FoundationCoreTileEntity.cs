#nullable enable

using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

public sealed class FoundationCoreTileEntity : ModTileEntity
{
    internal FirstSeveranceCoreProtectionState ProtectionState { get; private set; }

    public override bool IsTileValidForEntity(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        return tile.HasTile
            && tile.TileType == ModContent.TileType<FoundationCoreTile>();
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)ProtectionState);
    }

    public override void NetReceive(BinaryReader reader)
    {
        var incoming = (FirstSeveranceCoreProtectionState)reader.ReadByte();
        ProtectionState = Enum.IsDefined(incoming)
            ? incoming
            : FirstSeveranceCoreProtectionState.Idle;
    }

    public override void OnNetPlace()
    {
        ProtectionState = FirstSeveranceCoreProtectionState.Idle;
    }

    public override void OnKill()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            FoundationCoreProtectionSystem.RecordMissing(ID);
        }

        ProtectionState = FirstSeveranceCoreProtectionState.Idle;
    }

    internal void SetProtectionState(FirstSeveranceCoreProtectionState state)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        ProtectionState = state;
    }
}
