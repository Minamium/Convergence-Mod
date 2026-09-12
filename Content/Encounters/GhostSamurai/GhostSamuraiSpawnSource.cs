using System;
using Terraria.DataStructures;

namespace Convergence.Content.Encounters.GhostSamurai;

// OnSpawn must populate ExtraAI before NewNPC/NewProjectile can send their initial
// Sync packet. Setting custom fields only after NewProjectile returns is too late.
internal sealed record GhostSamuraiActorSource(GhostSamuraiRuntime Runtime, Guid Fight, int Target) : IEntitySource
{
    public string Context => "GhostSamuraiSummon";
}
internal sealed record GhostSamuraiAttackSource(Guid Fight, int BossSlot, SamuraiHazard Hazard) : IEntitySource
{
    public string Context => "GhostSamuraiAttack";
}
