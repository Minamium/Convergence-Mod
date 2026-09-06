using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

// Natural spawn hooks only: no deletion of existing wildlife, town NPCs,
// player summons or explicitly spawned encounter actors; no saved/global toggle.
internal sealed class FirstSeveranceAmbientSpawns : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!FirstSeveranceCombatAuthority.IsActive) return;
        // spawnRate is a denominator: zero is not "no spawning".
        spawnRate = int.MaxValue;
        maxSpawns = 0;
    }

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        if (FirstSeveranceCombatAuthority.IsActive) pool.Clear();
    }
}
