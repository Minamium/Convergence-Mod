namespace Convergence.Content.Items.Oboro;

internal static class OboroNetworkRules
{
    internal const ulong StateLeaseTicks = 90, HelloRetryTicks = 60;
    internal static bool Fresh(ulong now, ulong received) => now >= received && now - received <= StateLeaseTicks;
    internal static bool NeedRepair(ulong generation, ulong now, ulong received, ulong nextHello)
        => now >= nextHello && (generation == 0 || !Fresh(now, received));
}
