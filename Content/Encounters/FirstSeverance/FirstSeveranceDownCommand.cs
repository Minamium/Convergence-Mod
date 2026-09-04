using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

public sealed class FirstSeveranceDownCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "convergence-down";

    public override string Usage => "/convergence-down";

    public override string Description =>
        "Enter the experimental Raid-only Downed state for revive testing.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        _ = caller;
        _ = input;
        _ = args;
        FirstSeveranceClientActions.RequestPrototypeDown();
    }
}
