using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

internal sealed class GhostSamuraiDefinition : EncounterDefinition
{
    internal const string EncounterKey = "ghost_samurai";
    internal static readonly GhostSamuraiDefinition Instance = new();
    private GhostSamuraiDefinition() : base(GhostSamuraiTermination.Instance, GhostSamuraiTermination.External) { }
    public override string Key => EncounterKey;
    public override EncounterKind Kind => EncounterKind.Boss;
    public override int MaximumParticipants => 255;
    public override IEncounterRuntimeFactory RuntimeFactory => GhostSamuraiFactory.Instance;
}

// A distinct, bounded schema; existing feature causes and packet IDs are unchanged.
internal sealed class GhostSamuraiTermination : IEncounterTerminationContract
{
    internal static readonly GhostSamuraiTermination Instance = new();
    internal static readonly IReadOnlyList<EncounterTerminationDescriptor> External = new[]
    {
        End(EncounterEndReason.WorldUnload), End(EncounterEndReason.InternalFailure), End(EncounterEndReason.ProtocolFailure),
    };
    public ushort FeatureSchemaId => 2;
    public byte FeatureSchemaVersion => 1;
    internal static EncounterTerminationDescriptor End(EncounterEndReason reason)
        => EncounterTerminationDescriptor.Create(reason, EncounterFeatureTermination.Create(2, 1, (byte)reason));
    public bool IsValid(in EncounterTerminationDescriptor value)
        => value.Feature.SchemaId == 2 && value.Feature.SchemaVersion == 1 && value.EndReason != EncounterEndReason.None
        && Enum.IsDefined(value.EndReason) && value.Feature.Cause == (byte)value.EndReason;
}

internal sealed class GhostSamuraiFactory : IEncounterRuntimeFactory
{
    internal static readonly GhostSamuraiFactory Instance = new();
    public IEncounterRuntime Create(EncounterDefinition definition, in EncounterRuntimeCreationContext context)
    {
        Player p = Main.player[context.Start.RequesterWhoAmI];
        if (p.dead || p.HeldItem.type != ModContent.ItemType<GhostSamuraiSummon>())
            throw new EncounterStartRejectedException("ghost_samurai.summon_not_held");
        // The factory creates no world entities: its runtime is registered for cleanup
        // by the coordinator before the first Tick can spawn anything.
        var arena = SamuraiArenaBounds.Create(p.Center.X, p.Center.Y, Main.maxTilesX * 16, Main.maxTilesY * 16);
        if (!arena.IsValid) throw new EncounterStartRejectedException("ghost_samurai.arena_world_edge");
        return new GhostSamuraiRuntime(context.FightId, context.Start.RequesterWhoAmI, arena);
    }
}

internal sealed class GhostSamuraiRegistration : ModSystem
{
    public override void PostSetupContent()
    {
        EncounterCatalogSystem.Registry.Register(GhostSamuraiDefinition.Instance);
        EncounterPacketRouter.Routes.Register(GhostSamuraiDefinition.EncounterKey, ModContent.GetInstance<GhostSamuraiPackets>());
    }
}
