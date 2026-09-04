#nullable enable

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceConnectionEpochSystem : ModSystem
{
    private static bool[] observedActive = [];
    private static ulong[] epochs = [];
    private static ulong nextEpoch;

    public override void ClearWorld()
    {
        observedActive = new bool[Main.maxPlayers];
        epochs = new ulong[Main.maxPlayers];
        nextEpoch = 0;
    }

    public override void PostUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }

        EnsureCapacity();
        for (int slot = 0; slot < Main.maxPlayers; slot++)
        {
            bool active = Main.player[slot].active;
            if (!active)
            {
                observedActive[slot] = false;
                epochs[slot] = 0;
                continue;
            }

            if (!observedActive[slot])
            {
                if (nextEpoch == ulong.MaxValue)
                {
                    epochs[slot] = 0;
                    continue;
                }

                nextEpoch++;
                epochs[slot] = nextEpoch;
                observedActive[slot] = true;
            }
        }
    }

    public override void OnWorldUnload()
    {
        observedActive = [];
        epochs = [];
        nextEpoch = 0;
    }

    public override void Unload()
    {
        observedActive = [];
        epochs = [];
        nextEpoch = 0;
    }

    internal static bool TryGetCurrentEpoch(int serverWhoAmI, out ulong connectionEpoch)
    {
        EnsureCapacity();
        if (Main.netMode != NetmodeID.MultiplayerClient
            && serverWhoAmI >= 0
            && serverWhoAmI < Main.maxPlayers
            && Main.player[serverWhoAmI].active
            && observedActive[serverWhoAmI]
            && epochs[serverWhoAmI] != 0)
        {
            connectionEpoch = epochs[serverWhoAmI];
            return true;
        }

        connectionEpoch = 0;
        return false;
    }

    private static void EnsureCapacity()
    {
        if (observedActive.Length != Main.maxPlayers)
        {
            observedActive = new bool[Main.maxPlayers];
            epochs = new ulong[Main.maxPlayers];
            nextEpoch = 0;
        }
    }
}
