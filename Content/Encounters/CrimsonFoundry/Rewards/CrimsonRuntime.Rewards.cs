#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// The only point where the rewards touch the Raid (REWARDS.md, "Scarlet Score Reliquary"): an accepted
// Victory drops one shared Scarlet Score Reliquary per frozen member (including members Down or disconnected at
// Victory) at the pedestal ground. Cleanup calls it before ClearHazards, after the 150-tick Victory melt; Defeat,
// cancellation, the safety cap and invalidation drop nothing. The counter is taken before each grant, so a cleanup
// retry never drops a duplicate. No packet, policy or coordinator code is involved.
internal sealed partial class CrimsonRuntime
{
    private int rewardsAttempted;

    private void DropRewards()
    {
        int count = members.Length;
        while (rewardsAttempted < count)
        {
            int index = rewardsAttempted++;
            try
            {
                var area = new Rectangle(CrimsonRewardRules.DropLeft((int)ground.X, index, count), CrimsonRewardRules.DropTop((int)ground.Y),
                    CrimsonRewardRules.DropSize, CrimsonRewardRules.DropSize);
                int item = Item.NewItem(new EntitySource_Misc("Convergence:CrimsonFoundryVictory"), area, ModContent.ItemType<CrimsonScoreReliquary>());
                CrimsonPackets.Log(item < Main.maxItems
                    ? $"event=VictoryRewardDropped fight={fight.Value} index={index} item={item}"
                    : $"event=VictoryRewardFailed fight={fight.Value} index={index} reason=item_limit");
            }
            catch (Exception e) { CrimsonPackets.Log($"event=VictoryRewardFailed fight={fight.Value} index={index} reason={e.GetType().Name}"); }
        }
    }
}
