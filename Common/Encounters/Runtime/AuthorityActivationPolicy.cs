using Convergence.Common.Encounters.Abstractions;
using Terraria;
using Terraria.ID;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class AuthorityActivationPolicy : IEncounterActivationPolicy
{
    public static AuthorityActivationPolicy Instance { get; } = new();

    private AuthorityActivationPolicy()
    {
    }

    public EncounterActivationDecision Evaluate(
        in EncounterStartCommand command,
        EncounterDefinition definition)
    {
        _ = definition;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return EncounterActivationDecision.Reject("encounter.not_authority");
        }

        if (command.SenderWhoAmI < 0
            || command.SenderWhoAmI >= Main.maxPlayers
            || !Main.player[command.SenderWhoAmI].active)
        {
            return EncounterActivationDecision.Reject("encounter.invalid_sender");
        }

        if (Main.netMode == NetmodeID.SinglePlayer
            && command.SenderWhoAmI != Main.myPlayer)
        {
            return EncounterActivationDecision.Reject("encounter.sender_mismatch");
        }

        if (!WorldGen.InWorld(command.RequestedAnchor.X, command.RequestedAnchor.Y, 10))
        {
            return EncounterActivationDecision.Reject("encounter.anchor_out_of_world");
        }

        return EncounterActivationDecision.Allow;
    }
}
