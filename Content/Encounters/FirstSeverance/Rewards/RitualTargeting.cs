#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

internal static class RitualTargeting
{
    internal static NPC? Current(Projectile projectile)
    {
        int index = (int)projectile.ai[1];
        return index >= 0 && index < Main.maxNPCs && Valid(Main.npc[index], projectile, RitualArmamentRules.RetainRange)
            ? Main.npc[index] : null;
    }
    private static bool Valid(NPC npc, Projectile projectile, float range) => npc.CanBeChasedBy(projectile)
        && Vector2.DistanceSquared(npc.Center, projectile.Center) <= range * range
        && Collision.CanHitLine(projectile.Center, 1, 1, npc.position, npc.width, npc.height);
    internal static NPC? Acquire(Projectile projectile, Player owner, bool manual = false, ISet<int>? excluded = null)
    {
        // Terraria's ordinary owner projectile replication is used; replicas
        // follow the chosen target rather than all picking different enemies.
        NPC? chosen = Current(projectile);
        if (chosen is not null && excluded?.Contains(Root(chosen)) == true) chosen = null;
        if (projectile.owner != Main.myPlayer) return chosen;
        if (manual && owner.HasMinionAttackTargetNPC)
        {
            int id = owner.MinionAttackTargetNPC;
            if (id >= 0 && id < Main.maxNPCs && Valid(Main.npc[id], projectile, RitualArmamentRules.AcquisitionRange))
                chosen = Main.npc[id];
        }
        if (chosen is null)
        {
            float best = RitualArmamentRules.AcquisitionRange * RitualArmamentRules.AcquisitionRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float distance = Vector2.DistanceSquared(projectile.Center, npc.Center);
                if (distance < best && excluded?.Contains(Root(npc)) != true && Valid(npc, projectile, RitualArmamentRules.AcquisitionRange))
                { chosen = npc; best = distance; }
            }
        }
        int next = chosen?.whoAmI ?? -1;
        if ((int)projectile.ai[1] != next) { projectile.ai[1] = next; projectile.netUpdate = true; }
        return chosen;
    }
    internal static void Home(Projectile projectile, Vector2 target, Vector2 targetVelocity, float speed)
    {
        float updates = projectile.MaxUpdates;
        Vector2 delta = target - projectile.Center;
        float lead = Math.Clamp(delta.Length() / Math.Max(speed, 1), 0, 10);
        var v = RitualArmamentRules.Steer(
            new(projectile.velocity.X * updates, projectile.velocity.Y * updates),
            new(delta.X + targetVelocity.X * lead, delta.Y + targetVelocity.Y * lead), speed, 1 / updates);
        projectile.velocity = new Vector2(v.X, v.Y) / updates;
    }
    internal static bool ValidState(Projectile projectile) => projectile.owner >= 0 && projectile.owner < Main.maxPlayers
        && float.IsFinite(projectile.ai[0]) && float.IsFinite(projectile.ai[1]) && float.IsFinite(projectile.ai[2])
        && float.IsFinite(projectile.position.X) && float.IsFinite(projectile.position.Y)
        && float.IsFinite(projectile.velocity.X) && float.IsFinite(projectile.velocity.Y)
        && projectile.ai[1] >= -1 && projectile.ai[1] < Main.maxNPCs;
    internal static int Root(NPC npc) => npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
}
