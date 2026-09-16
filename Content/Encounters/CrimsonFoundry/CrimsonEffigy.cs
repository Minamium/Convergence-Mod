#nullable enable
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Independently damageable native NPC. Only its owner advances the death mask.
public sealed class CrimsonEffigy : ModNPC
{
    internal CrimsonRuntime? Runtime;
    internal CrimsonEffigyState State;
    public override string Texture => "Convergence/Assets/Textures/CrimsonFoundry/EmberCrown";
    public override void SetStaticDefaults() => NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
    public override void SetDefaults()
    {
        NPC.width = 190; NPC.height = 240; NPC.lifeMax = 3000000; NPC.defense = 65;
        NPC.damage = 0; NPC.knockBackResist = 0; NPC.aiStyle = -1;
        NPC.noGravity = NPC.noTileCollide = NPC.lavaImmune = NPC.netAlways = true;
        NPC.dontTakeDamage = true;
        NPC.BossBar = ModContent.GetInstance<CrimsonBossBar>();
        if (!Main.dedServ) NPC.HitSound = SoundID.NPCHit4;
    }
    internal bool TryBoss(out CrimsonBoss? boss)
    {
        boss = State.Boss >= 0 && State.Boss < Main.maxNPCs && Main.npc[State.Boss].active
            ? Main.npc[State.Boss].ModNPC as CrimsonBoss : null;
        return State.Fight != Guid.Empty && boss is not null && boss.State.Fight == State.Fight && boss.Fresh;
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (source is not CrimsonEffigySource owned) return;
        Runtime = owned.Runtime; State = owned.State;
        NPC.life = NPC.lifeMax = owned.Life;
        if (State.Index == 1) { NPC.width = 270; NPC.height = 180; }
    }
    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;
        NPC.GivenName = State.Index switch { 0 => "Ember Crown", 1 => "Sable Mantle", _ => "Thorn Choir" };
        NPC.dontTakeDamage = !TryBoss(out var boss) || !boss!.State.SummonVulnerable(State.Index);
        NPC.boss = !NPC.dontTakeDamage;
        if (boss is not null)
        {
            NPC.lifeMax = boss.State.TargetLife;
            CrimsonGesture.ProjectMotion(NPC, boss, State.Index);
        }
        if (Main.netMode != NetmodeID.MultiplayerClient && (Runtime is null || !Runtime.Matches(this)))
        {
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
        }
    }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    private bool AboveRetreatFloor(CrimsonBoss boss) => boss.State.Phase == 3
        || NPC.life > CrimsonPhaseRules.RetreatLife(boss.State.TargetLife);
    public override bool? CanBeHitByItem(Player player, Item item) => TryBoss(out var boss) && AboveRetreatFloor(boss!) && boss!.State.Contains(player.whoAmI) ? null : false;
    public override bool? CanBeHitByProjectile(Projectile projectile) => TryBoss(out var boss) && AboveRetreatFloor(boss!) && boss!.State.Contains(projectile.owner) ? null : false;
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (TryBoss(out var boss) && boss!.State.Phase < 3)
            modifiers.SetMaxDamage(Math.Max(1, NPC.life - CrimsonPhaseRules.RetreatLife(boss.State.TargetLife)));
    }
    public override bool CheckDead()
    {
        if (!TryBoss(out var boss) || boss!.State.Phase < 3)
        {
            NPC.life = CrimsonPhaseRules.RetreatLife(boss?.State.TargetLife ?? NPC.lifeMax);
            NPC.dontTakeDamage = true;
            return false;
        }
        return true;
    }
    public override void OnKill()
    { if (Main.netMode != NetmodeID.MultiplayerClient) Runtime?.SummonKilled(this); }
    public override void SendExtraAI(BinaryWriter writer) => State.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var next = CrimsonEffigyState.Read(reader);
        if (Main.netMode == NetmodeID.Server || State.Fight != Guid.Empty && State != next) return;
        State = next;
        NPC.width = State.Index == 1 ? 270 : 190; NPC.height = State.Index == 1 ? 180 : 240;
    }
}
