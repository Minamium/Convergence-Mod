#nullable enable

using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

public sealed class FoundationCoreTileEntity : ModTileEntity, Convergence.Content.Shared.IRaidPedestal
{
    Microsoft.Xna.Framework.Vector2 Convergence.Content.Shared.IRaidPedestal.Ground => GroundCenter;
    bool Convergence.Content.Shared.IRaidPedestal.Claim(ulong sequence, Convergence.Common.Foundation.Identifiers.FightId fight)
        => FoundationCoreProtectionSystem.TryClaim(this, sequence, fight);
    bool Convergence.Content.Shared.IRaidPedestal.IsOwned(Convergence.Common.Foundation.Identifiers.FightId fight)
        => FoundationCoreProtectionSystem.TryResolveOwnedCore(ID, fight, out var core) && ReferenceEquals(core, this);
    bool Convergence.Content.Shared.IRaidPedestal.Activate(Convergence.Common.Foundation.Identifiers.FightId fight)
        => FoundationCoreProtectionSystem.TryEnterActive(ID, fight);
    void Convergence.Content.Shared.IRaidPedestal.Release(Convergence.Common.Foundation.Identifiers.FightId fight)
        => FoundationCoreProtectionSystem.TryRelease(ID, fight);
    internal static bool IsCoreType(int type) => type == ModContent.TileType<FoundationCoreTile>()
        || type == ModContent.TileType<FoundationPlinthTile>();
    internal int FootprintWidth => Main.tile[Position.X, Position.Y].TileType == ModContent.TileType<FoundationPlinthTile>() ? 12 : 2;
    internal int FootprintHeight => FootprintWidth == 12 ? 4 : 2;
    internal Microsoft.Xna.Framework.Vector2 GroundCenter => new((Position.X + FootprintWidth * .5f) * 16f,
        (Position.Y + FootprintHeight) * 16f);
    internal FirstSeveranceCoreProtectionState ProtectionState { get; private set; }

    public override bool IsTileValidForEntity(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        return tile.HasTile
            && IsCoreType(tile.TileType);
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
