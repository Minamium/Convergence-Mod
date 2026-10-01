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

namespace Convergence.Content.Encounters.EbonManor;

internal sealed class EbonDefinition : EncounterDefinition
{
    internal const string EncounterKey = "ebon_manor";
    internal static readonly EbonDefinition Instance = new();
    private EbonDefinition() : base(EbonTermination.Instance, EbonTermination.External) { }
    public override string Key => EncounterKey;
    public override EncounterKind Kind => EncounterKind.Raid;
    public override int MaximumParticipants => EbonRules.Members;
    public override IEncounterRuntimeFactory RuntimeFactory => EbonFactory.Instance;
}
internal sealed class EbonTermination : IEncounterTerminationContract
{
    internal static readonly EbonTermination Instance = new();
    internal static readonly IReadOnlyList<EncounterTerminationDescriptor> External = new[]
        { End(EncounterEndReason.WorldUnload), End(EncounterEndReason.InternalFailure), End(EncounterEndReason.ProtocolFailure) };
    public ushort FeatureSchemaId => 5;
    public byte FeatureSchemaVersion => 1;
    internal static EncounterTerminationDescriptor End(EncounterEndReason reason)
        => EncounterTerminationDescriptor.Create(reason, EncounterFeatureTermination.Create(5, 1, (byte)reason));
    public bool IsValid(in EncounterTerminationDescriptor value) => value.Feature.SchemaId == 5
        && value.Feature.SchemaVersion == 1 && value.EndReason != EncounterEndReason.None
        && Enum.IsDefined(value.EndReason) && value.Feature.Cause == (byte)value.EndReason;
}
internal sealed class EbonFactory : IEncounterRuntimeFactory
{
    internal static readonly EbonFactory Instance = new();
    public IEncounterRuntime Create(EncounterDefinition definition, in EncounterRuntimeCreationContext context)
    {
        Player p = Main.player[context.Start.RequesterWhoAmI];
        if (p.dead || p.HeldItem.type != ModContent.ItemType<EbonInvitation>())
            throw new EncounterStartRejectedException("ebon.summon_not_held");
        var anchor = context.Start.RequestedAnchor;
        if (!RaidPedestal.TryResolve(anchor.X, anchor.Y, out var core))
            throw new EncounterStartRejectedException("ebon.pedestal_missing");
        if (Vector2.DistanceSquared(p.Center, core.Ground) > 260 * 260)
            throw new EncounterStartRejectedException("ebon.pedestal_out_of_range");
        if (!RaidFieldGeometry.FromGround(core.Ground.X, core.Ground.Y).FitsWorld(Main.maxTilesX, Main.maxTilesY))
            throw new EncounterStartRejectedException("ebon.field_outside_world");
        int count = 0; foreach (Player player in Main.ActivePlayers) if (!player.ghost) count++;
        if (count > EbonRules.Members) throw new EncounterStartRejectedException("ebon.party_above_eight");
        return new EbonRuntime(context.FightId, p.whoAmI, core, context.EncounterSequence);
    }
}
internal sealed class EbonRegistration : ModSystem
{
    public override void PostSetupContent()
    {
        EncounterCatalogSystem.Registry.Register(EbonDefinition.Instance);
        EncounterPacketRouter.Routes.Register(EbonDefinition.EncounterKey, ModContent.GetInstance<EbonPackets>());
    }
}
