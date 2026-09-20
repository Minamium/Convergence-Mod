using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Shared;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.AzureCathedral;

internal sealed class AzureDefinition : EncounterDefinition
{
    internal const string EncounterKey = "azure_cathedral";
    internal static readonly AzureDefinition Instance = new();
    private AzureDefinition() : base(AzureTermination.Instance, AzureTermination.External) { }
    public override string Key => EncounterKey;
    public override EncounterKind Kind => EncounterKind.Raid;
    public override int MaximumParticipants => AzureRules.Members;
    public override IEncounterRuntimeFactory RuntimeFactory => AzureFactory.Instance;
}
internal sealed class AzureTermination : IEncounterTerminationContract
{
    internal static readonly AzureTermination Instance = new();
    internal static readonly IReadOnlyList<EncounterTerminationDescriptor> External = new[]
        { End(EncounterEndReason.WorldUnload), End(EncounterEndReason.InternalFailure), End(EncounterEndReason.ProtocolFailure) };
    public ushort FeatureSchemaId => 4;
    public byte FeatureSchemaVersion => 1;
    internal static EncounterTerminationDescriptor End(EncounterEndReason reason)
        => EncounterTerminationDescriptor.Create(reason, EncounterFeatureTermination.Create(4, 1, (byte)reason));
    public bool IsValid(in EncounterTerminationDescriptor value) => value.Feature.SchemaId == 4
        && value.Feature.SchemaVersion == 1 && value.EndReason != EncounterEndReason.None
        && Enum.IsDefined(value.EndReason) && value.Feature.Cause == (byte)value.EndReason;
}
internal sealed class AzureFactory : IEncounterRuntimeFactory
{
    internal static readonly AzureFactory Instance = new();
    public IEncounterRuntime Create(EncounterDefinition definition, in EncounterRuntimeCreationContext context)
    {
        Player p = Main.player[context.Start.RequesterWhoAmI];
        if (p.dead || p.HeldItem.type != ModContent.ItemType<AzureConductor>())
            throw new EncounterStartRejectedException("azure.summon_not_held");
        var anchor = context.Start.RequestedAnchor;
        if (!RaidPedestal.TryResolve(anchor.X, anchor.Y, out var core))
            throw new EncounterStartRejectedException("azure.pedestal_missing");
        if (Vector2.DistanceSquared(p.Center, core.Ground) > 260 * 260)
            throw new EncounterStartRejectedException("azure.pedestal_out_of_range");
        if (!RaidFieldGeometry.FromGround(core.Ground.X, core.Ground.Y).FitsWorld(Main.maxTilesX, Main.maxTilesY))
            throw new EncounterStartRejectedException("azure.field_outside_world");
        int count = 0; foreach (Player player in Main.ActivePlayers) if (!player.ghost) count++;
        if (count > AzureRules.Members) throw new EncounterStartRejectedException("azure.party_above_eight");
        return new AzureRuntime(context.FightId, p.whoAmI, core, context.EncounterSequence);
    }
}
internal sealed class AzureRegistration : ModSystem
{
    public override void PostSetupContent()
    {
        EncounterCatalogSystem.Registry.Register(AzureDefinition.Instance);
        EncounterPacketRouter.Routes.Register(AzureDefinition.EncounterKey, ModContent.GetInstance<AzurePackets>());
    }
}
