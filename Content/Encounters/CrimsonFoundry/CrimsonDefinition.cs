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

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal sealed class CrimsonDefinition : EncounterDefinition
{
    internal const string EncounterKey = "crimson_foundry";
    internal static readonly CrimsonDefinition Instance = new();
    private CrimsonDefinition() : base(CrimsonTermination.Instance, CrimsonTermination.External) { }
    public override string Key => EncounterKey;
    public override EncounterKind Kind => EncounterKind.Raid;
    public override int MaximumParticipants => CrimsonState.MaxMembers;
    public override IEncounterRuntimeFactory RuntimeFactory => CrimsonFactory.Instance;
}

internal sealed class CrimsonTermination : IEncounterTerminationContract
{
    internal static readonly CrimsonTermination Instance = new();
    internal static readonly IReadOnlyList<EncounterTerminationDescriptor> External = new[]
        { End(EncounterEndReason.WorldUnload), End(EncounterEndReason.InternalFailure), End(EncounterEndReason.ProtocolFailure) };
    public ushort FeatureSchemaId => 3;
    public byte FeatureSchemaVersion => 1;
    internal static EncounterTerminationDescriptor End(EncounterEndReason reason)
        => EncounterTerminationDescriptor.Create(reason, EncounterFeatureTermination.Create(3, 1, (byte)reason));
    public bool IsValid(in EncounterTerminationDescriptor value) => value.Feature.SchemaId == 3
        && value.Feature.SchemaVersion == 1 && value.EndReason != EncounterEndReason.None
        && Enum.IsDefined(value.EndReason) && value.Feature.Cause == (byte)value.EndReason;
}

internal sealed class CrimsonFactory : IEncounterRuntimeFactory
{
    internal static readonly CrimsonFactory Instance = new();
    public IEncounterRuntime Create(EncounterDefinition definition, in EncounterRuntimeCreationContext context)
    {
        Player p = Main.player[context.Start.RequesterWhoAmI];
        if (p.dead || p.HeldItem.type != ModContent.ItemType<CrimsonConductor>())
            throw new EncounterStartRejectedException("crimson.summon_not_held");
        if (!global::Convergence.ConvergenceMod.Instance.FileExists("Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg"))
            throw new EncounterStartRejectedException("crimson.licensed_music_not_installed");
        int count = 0;
        foreach (Player player in Main.ActivePlayers) if (!player.ghost) count++;
        if (count > CrimsonState.MaxMembers) throw new EncounterStartRejectedException("crimson.party_above_eight");
        var anchor = context.Start.RequestedAnchor;
        if (!RaidPedestal.TryResolve(anchor.X, anchor.Y, out var core))
            throw new EncounterStartRejectedException("crimson.pedestal_missing");
        if (Vector2.DistanceSquared(p.Center, core.Ground) > 260 * 260)
            throw new EncounterStartRejectedException("crimson.pedestal_out_of_range");
        if (!RaidFieldGeometry.FromGround(core.Ground.X, core.Ground.Y).FitsWorld(Main.maxTilesX, Main.maxTilesY))
            throw new EncounterStartRejectedException("crimson.field_outside_world");
        return new CrimsonRuntime(context.FightId, p.whoAmI, core, context.EncounterSequence);
    }
}

internal sealed class CrimsonRegistration : ModSystem
{
    internal static CrimsonScore Score { get; private set; } = null!;
    public override void PostSetupContent()
    {
        Score = CrimsonScore.Read(Mod.GetFileBytes("Assets/Music/CrimsonFoundry/Score.json"));
        EncounterCatalogSystem.Registry.Register(CrimsonDefinition.Instance);
        EncounterPacketRouter.Routes.Register(CrimsonDefinition.EncounterKey, ModContent.GetInstance<CrimsonPackets>());
    }
    public override void Unload() => Score = null!;
}
