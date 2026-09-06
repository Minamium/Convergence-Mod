#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

// Authority-owned NPC handles and owner token; no phase advancement or player outcomes.
internal sealed class FirstSeveranceActorSet
{
    private static int nextActorToken;
    private readonly FirstSeverancePartyScaling partyScaling;
    private readonly Vector2 groundCenter;
    private readonly FirstSeveranceEncounterPlan plan;
    private readonly HashSet<int> killedPylons = new();
    private readonly List<int> pylonNpcIndices = new();
    private int actorToken;
    private int bossNpcIndex = -1;
    private int lastObservedBossLife;
    private bool bossKilled;
    private int boundaryDamage;
    private readonly FightId fightId;

    internal FirstSeveranceActorSet(FightId fightId, FirstSeverancePartyScaling partyScaling,
        Vector2 groundCenter, FirstSeveranceEncounterPlan plan)
    {
        this.fightId = fightId;
        this.partyScaling = partyScaling;
        this.groundCenter = groundCenter;
        this.plan = plan;
    }

    internal void BeginFight()
    {
        actorToken = AllocateActorToken();
        lastObservedBossLife = partyScaling.BossLife;
    }
    internal bool Owns(NPC npc) => (int)npc.ai[0] == actorToken;
    internal int[] CapturePylonSlots() => pylonNpcIndices.ToArray();
    internal bool WasPylonKilled(int slot) => killedPylons.Contains(slot);
    internal bool TryObservePylonLife(int slot, out int life)
    {
        bool found = TryResolveOwnedNpc<FirstSeverancePrototypePylon>(slot, out NPC npc);
        life = found ? npc.life : 0;
        return found;
    }
    private Vector2 CoreWorldCenter => groundCenter;

    internal void Cleanup(in EncounterCleanupContext context)
    {
        if (context.FightId != fightId) throw new InvalidOperationException("Actor cleanup has the wrong Fight.");
        CleanupPylons();
        CleanupNpc(bossNpcIndex, ModContent.NPCType<FirstSeverancePrototypeBoss>());
        bossNpcIndex = -1;
        boundaryDamage = 0;
    }

    internal void RecordActorDeath(NPC npc, in FirstSeveranceLoopState state)
    {
        if ((int)npc.ai[0] != actorToken)
            return;
        if (npc.whoAmI == bossNpcIndex
            && npc.type == ModContent.NPCType<FirstSeverancePrototypeBoss>()
            && FirstSeveranceBossPhasePlan.IsDamageState(state.Substate))
            bossKilled = true;
        else if (npc.type == ModContent.NPCType<FirstSeverancePrototypePylon>()
            && pylonNpcIndices.Contains(npc.whoAmI))
            killedPylons.Add(npc.whoAmI);
    }

    internal bool ProtectBossPhaseBoundary(NPC npc, in FirstSeveranceLoopState state, int damageFloor)
    {
        if (npc.whoAmI != bossNpcIndex
            || (int)npc.ai[0] != actorToken)
            return false;
        boundaryDamage = Math.Max(boundaryDamage, Math.Max(0, state.BossLife - damageFloor));
        npc.life = Math.Max(1, damageFloor);
        npc.netUpdate = true;
        return true;
    }

    internal bool TryObservePylons(
        in FirstSeveranceLoopState state,
        ulong authorityTick,
        out int destroyedPylons)
    {
        destroyedPylons = 0;
        if (state.Substate != FirstSeveranceSubstate.PylonCheck)
        {
            return pylonNpcIndices.Count == 0;
        }

        bool damageWindowOpen = authorityTick >= state.SubstateEnteredTick
            + (ulong)plan.Timing.PylonTelegraphTicks;
        for (int index = pylonNpcIndices.Count - 1; index >= 0; index--)
        {
            int npcIndex = pylonNpcIndices[index];
            if (killedPylons.Remove(npcIndex) && damageWindowOpen)
            {
                destroyedPylons++;
                pylonNpcIndices.RemoveAt(index);
                continue;
            }
            if (TryResolveOwnedNpc<FirstSeverancePrototypePylon>(npcIndex, out _))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    internal bool TryObserveBoss(
        in FirstSeveranceLoopState state,
        out int acceptedBossDamage)
    {
        acceptedBossDamage = 0;
        if (!TryResolveOwnedNpc<FirstSeverancePrototypeBoss>(bossNpcIndex, out NPC boss))
        {
            if (FirstSeveranceBossPhasePlan.IsDamageState(state.Substate) && bossKilled)
            {
                acceptedBossDamage = lastObservedBossLife;
                return true;
            }

            return false;
        }

        int observedLife = Math.Clamp(boss.life, 0, state.BossMaximumLife);
        if (FirstSeveranceBossPhasePlan.IsDamageState(state.Substate))
        {
            acceptedBossDamage = Math.Max(boundaryDamage, Math.Max(0, lastObservedBossLife - observedLife));
        }
        else if (observedLife != state.BossLife)
        {
            boss.life = Math.Max(1, state.BossLife);
            boss.netUpdate = true;
        }

        boundaryDamage = 0;
        return true;
    }

    internal void UpdateDamageWindows(
        in FirstSeveranceLoopState state,
        ulong authorityTick, int damageFloor)
    {
        if (TryResolveOwnedNpc<FirstSeverancePrototypeBoss>(bossNpcIndex, out NPC boss))
        {
            bool shielded = !plan.Boss.CanTakeDamage(state.Substate) || state.BossLife <= damageFloor
                || state.BossPhase == FirstSeveranceBossPhase.Final;
            if (boss.dontTakeDamage != shielded || boss.chaseable == shielded)
            {
                boss.dontTakeDamage = shielded;
                boss.chaseable = !shielded;
                boss.ai[2] = shielded ? 0f : 1f;
                boss.netUpdate = true;
            }
        }

        bool pylonsShielded = state.Substate != FirstSeveranceSubstate.PylonCheck
            || authorityTick < state.SubstateEnteredTick
                + (ulong)plan.Timing.PylonTelegraphTicks;
        for (int index = 0; index < pylonNpcIndices.Count; index++)
        {
            if (!TryResolveOwnedNpc<FirstSeverancePrototypePylon>(
                    pylonNpcIndices[index],
                    out NPC pylon)
                || (pylon.dontTakeDamage == pylonsShielded
                    && pylon.chaseable != pylonsShielded))
            {
                continue;
            }

            pylon.dontTakeDamage = pylonsShielded;
            pylon.chaseable = !pylonsShielded;
            pylon.ai[2] = pylonsShielded ? 0f : 1f;
            pylon.netUpdate = true;
        }
    }

    internal void SynchronizeBossLife(in FirstSeveranceLoopState state)
    {
        if (!TryResolveOwnedNpc<FirstSeverancePrototypeBoss>(bossNpcIndex, out NPC boss))
        {
            return;
        }

        int life = Math.Max(1, state.BossLife);
        if (boss.life != life)
        {
            boss.life = life;
            boss.netUpdate = true;
        }

        lastObservedBossLife = state.BossLife;
    }

    internal bool TrySpawnBoss(out int npcIndex)
    {
        Vector2 center = CoreWorldCenter + new Vector2(0f, -FirstSeveranceLanceTuning.BossHeightAboveCore);
        npcIndex = SpawnNpc(
            ModContent.NPCType<FirstSeverancePrototypeBoss>(),
            center,
            actorIndex: 0);
        return npcIndex >= 0;
    }

    internal bool TrySpawnPylons(int count, ulong authorityTick)
    {
        _ = authorityTick;
        CleanupPylons();
        Vector2[] offsets =
        {
            new(-500f, -470f),
            new(500f, -470f),
            new(-650f, -810f),
            new(650f, -810f),
        };
        for (int index = 0; index < count; index++)
        {
            int npcIndex = SpawnNpc(
                ModContent.NPCType<FirstSeverancePrototypePylon>(),
                CoreWorldCenter + offsets[index],
                index + 1);
            if (npcIndex < 0)
            {
                CleanupPylons();
                return false;
            }

        }

        return true;
    }

    private int SpawnNpc(int npcType, Vector2 center, int actorIndex)
    {
        int npcIndex = NPC.NewNPC(
            new EntitySource_Misc("Convergence.FirstSeverancePrototype"),
            (int)center.X,
            (int)center.Y,
            npcType,
            0,
            actorToken,
            actorIndex);
        if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
        {
            return -1;
        }

        // Record ownership before any customization or network call can fail.
        if (npcType == ModContent.NPCType<FirstSeverancePrototypeBoss>())
            bossNpcIndex = npcIndex;
        else
            pylonNpcIndices.Add(npcIndex);

        NPC npc = Main.npc[npcIndex];
        if (npc.ModNPC is FirstSeverancePrototypeBoss boss) boss.ConfigureParty(partyScaling.ParticipantCount);
        else if (npc.ModNPC is FirstSeverancePrototypePylon pylon) pylon.ConfigureParty(partyScaling.ParticipantCount);
        npc.life = npc.lifeMax;
        npc.Center = center;
        npc.ai[0] = actorToken;
        npc.ai[1] = actorIndex;
        npc.netUpdate = true;
        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.SyncNPC, number: npcIndex);
        }

        return npcIndex;
    }

    private bool TryResolveOwnedNpc<TNpc>(int npcIndex, out NPC npc)
        where TNpc : ModNPC
    {
        if (npcIndex >= 0
            && npcIndex < Main.maxNPCs
            && Main.npc[npcIndex].active
            && Main.npc[npcIndex].type == ModContent.NPCType<TNpc>()
            && (int)Main.npc[npcIndex].ai[0] == actorToken)
        {
            npc = Main.npc[npcIndex];
            return true;
        }

        npc = null!;
        return false;
    }

    internal void CleanupPylons()
    {
        int type = ModContent.NPCType<FirstSeverancePrototypePylon>();
        for (int index = 0; index < pylonNpcIndices.Count; index++)
        {
            CleanupNpc(pylonNpcIndices[index], type);
        }

        pylonNpcIndices.Clear();
        killedPylons.Clear();
    }

    private void CleanupNpc(int npcIndex, int expectedType)
    {
        if (npcIndex < 0
            || npcIndex >= Main.maxNPCs
            || !Main.npc[npcIndex].active
            || Main.npc[npcIndex].type != expectedType
            || (int)Main.npc[npcIndex].ai[0] != actorToken)
        {
            return;
        }

        Main.npc[npcIndex].active = false;
        Main.npc[npcIndex].netUpdate = true;
        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.SyncNPC, number: npcIndex);
        }
    }

    private static int AllocateActorToken()
    {
        nextActorToken = nextActorToken >= 1_000_000 ? 1 : nextActorToken + 1;
        return nextActorToken;
    }
}
