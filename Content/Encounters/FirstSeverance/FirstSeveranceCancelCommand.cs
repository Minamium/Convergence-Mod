using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

public sealed class FirstSeveranceCancelCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "convergence-cancel";

    public override string Usage => "/convergence-cancel";

    public override string Description => "End your Doll Play preparation or experimental combat as its initiator.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        _ = caller;
        _ = input;
        _ = args;
        FirstSeveranceClientActions.RequestCancel();
    }
}
