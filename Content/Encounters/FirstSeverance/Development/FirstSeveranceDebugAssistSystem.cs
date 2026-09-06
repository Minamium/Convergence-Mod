#nullable enable

using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Identifiers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Development;

internal sealed class FirstSeveranceDebugAssistSystem : ModSystem
{
    internal const string LaunchFlag = "-convergence-dev-assist";
    private static readonly FirstSeveranceDebugAssistPermit Permit = new();
    // Registry only. Ownership and cleanup stay with the exact-Fight preparation runtime.
    private static FirstSeveranceDebugAssistLease? activeLease;

    internal static bool IsOptedIn => Main.dedServ && Program.LaunchParameters.ContainsKey(LaunchFlag);

    internal static void ExecuteConsole(CommandCaller caller, string[] args)
    {
        if (caller.CommandType != CommandType.Console || !IsOptedIn || Main.netMode != NetmodeID.Server)
        {
            caller.Reply("first_severance.debug_assist.console_and_launch_opt_in_required");
            return;
        }

        if (args.Length == 1 && args[0] == "status")
        {
            caller.Reply($"Debug Assist: pending={Permit.IsArmed} active={activeLease is { IsRevoked: false }}. No normal-play client command exists.");
            if (activeLease is { } lease)
                caller.Reply($"Fight={lease.Owner} participant={lease.Member.ParticipantId.Value} slot={lease.Member.ServerWhoAmI} epoch={lease.Member.ConnectionEpoch} revoked={lease.IsRevoked}");
            for (int slot = 0; slot < Main.maxPlayers; slot++)
                if (FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(slot, out ulong epoch))
                    caller.Reply($"slot={slot} epoch={epoch} character={Main.player[slot].name} alive={!Main.player[slot].dead}");
            return;
        }

        if (args.Length == 1 && args[0] == "off")
        {
            Reset();
            caller.Reply("Debug Assist revoked; next combat snapshot clears client protection (within 30 ticks).");
            global::Convergence.ConvergenceMod.Instance.Logger.Info("FirstSeverance event=DebugAssistRevoked");
            return;
        }

        if (args.Length != 3 || args[0] != "arm"
            || !int.TryParse(args[1], out int targetSlot) || !ulong.TryParse(args[2], out ulong targetEpoch))
        {
            caller.Reply("Usage: convergence-assist status | arm <slot> <epoch> | off");
            return;
        }

        var snapshot = ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot;
        if (!snapshot.FightId.IsNone && snapshot.Termination.IsNone
            || activeLease is { IsRevoked: false }
            || !FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(targetSlot, out ulong currentEpoch)
            || currentEpoch != targetEpoch || Main.player[targetSlot].dead
            || !Permit.TryArm(Main.dedServ, IsOptedIn, caller.CommandType == CommandType.Console,
                targetSlot, targetEpoch, Main.GameUpdateCount))
        {
            caller.Reply("first_severance.debug_assist.arm_rejected (must be idle, current alive binding, no pending permit)");
            return;
        }

        caller.Reply($"Armed ONE next Raid for slot={targetSlot} epoch={targetEpoch}; expires in 10 minutes. Core and Ready remain manual.");
        global::Convergence.ConvergenceMod.Instance.Logger.Info($"FirstSeverance event=DebugAssistArmed slot={targetSlot} epoch={targetEpoch}");
    }

    internal static FirstSeveranceDebugAssistLease? Claim(FightId fightId, FirstSeveranceRoster roster)
    {
        if (!IsOptedIn || Main.netMode != NetmodeID.Server)
            return null;
        activeLease = Permit.Claim(fightId, roster, Main.GameUpdateCount);
        if (activeLease is { } lease)
            global::Convergence.ConvergenceMod.Instance.Logger.Info($"FirstSeverance event=DebugAssistBound fight={fightId} participant={lease.Member.ParticipantId.Value} slot={lease.Member.ServerWhoAmI} epoch={lease.Member.ConnectionEpoch}");
        return activeLease;
    }

    internal static void Release(FirstSeveranceDebugAssistLease? lease)
    {
        lease?.Revoke();
        if (ReferenceEquals(activeLease, lease))
            activeLease = null;
    }

    private static void Reset()
    {
        Permit.Clear();
        activeLease?.Revoke();
        activeLease = null;
    }

    public override void ClearWorld() => Reset();
    public override void OnWorldUnload() => Reset();
    public override void Unload() => Reset();
}

public sealed class FirstSeveranceDebugAssistCommand : ModCommand
{
    public override string Command => "convergence-assist";
    public override CommandType Type => CommandType.Console;
    public override string Description => "One-pull auxiliary protection; dedicated development server console only.";
    public override string Usage => "convergence-assist status | arm <slot> <epoch> | off";
    public override bool IsLoadingEnabled(Mod mod) => FirstSeveranceDebugAssistSystem.IsOptedIn;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Dedicated console input may originate on its input thread, never mutate Raid state there.
        if (caller.CommandType != CommandType.Console || !FirstSeveranceDebugAssistSystem.IsOptedIn)
            return;
        string[] capturedArgs = (string[])args.Clone();
        Main.QueueMainThreadAction(() => FirstSeveranceDebugAssistSystem.ExecuteConsole(caller, capturedArgs));
    }
}
