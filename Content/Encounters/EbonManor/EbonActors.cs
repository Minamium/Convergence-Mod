#nullable enable
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor;

// Noirette carries the accepted projection and takes weapon damage; her position,
// phase floors and outcome belong to EbonRuntime.
public sealed class EbonBoss : ModNPC
{
    internal EbonRuntime? Runtime;
    internal EbonState State;
    private ulong received;
    internal float VisualAge => State.Age + (Main.netMode == NetmodeID.MultiplayerClient ? (float)Math.Min(60UL, Main.GameUpdateCount - received) : 0);
    internal bool Fresh => Main.netMode != NetmodeID.MultiplayerClient || Main.GameUpdateCount - received <= 180;
    internal void ApplyProjection(in EbonState next, ulong at)
    { if (next.CanReplace(State)) { State = next; received = Math.Max(received, at); } }
    public override string Texture => "Convergence/Assets/Textures/EbonManor/Noirette";
    public override void SetStaticDefaults() => NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
    public override void SetDefaults()
    {
        // Covers the body of the 2x pixel sprite (tails and parasol excluded).
        NPC.width = 44; NPC.height = 96; NPC.lifeMax = EbonRules.Life(1); NPC.defense = 80;
        NPC.damage = 0; NPC.knockBackResist = 0; NPC.aiStyle = -1;
        NPC.noGravity = NPC.noTileCollide = NPC.lavaImmune = NPC.netAlways = true;
        NPC.dontTakeDamage = true; NPC.boss = true; NPC.BossBar = ModContent.GetInstance<EbonBossBar>();
        if (!Main.dedServ) { NPC.HitSound = SoundID.NPCHit5 with { Pitch = .35f, Volume = .6f }; Music = 0; }
    }
    public override void OnSpawn(IEntitySource source)
    { if (source is EbonActorSource a) { Runtime = a.Runtime; State = a.State; } }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool? CanBeHitByItem(Player p, Item item) => State.CanFight(p.whoAmI) ? null : false;
    public override bool? CanBeHitByProjectile(Projectile p) => State.CanFight(p.owner) ? null : false;
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        // A phase floor is never crossed by one hit; the runtime turns the floor
        // into the next act. DoT/lethal fallbacks are clamped by the runtime.
        int floor = EbonRules.Floor(State.MaxLife, State.Phase);
        if (floor > 0) modifiers.SetMaxDamage(Math.Max(1, NPC.life - floor));
    }
    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;
        NPC.dontTakeDamage = !Fresh || !State.Live || State.Life <= 0 || State.EndAt >= 0;
        NPC.chaseable = State.Live && State.Life > 0;
        NPC.boss = State.Stage is EbonStage.Countdown or EbonStage.Performance;
        if (State.MaxLife > 0) NPC.lifeMax = State.MaxLife;
        if (Main.netMode != NetmodeID.MultiplayerClient && (Runtime is null || !Runtime.Matches(this)))
        { NPC.active = false; if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI); }
    }
    public override bool CheckDead()
    {
        NPC.life = 1; if (State.Live && State.Phase == EbonPhase.Finale) Runtime?.Defeated();
        NPC.dontTakeDamage = true; NPC.netUpdate = Main.netMode != NetmodeID.MultiplayerClient; return false;
    }
    public override void SendExtraAI(BinaryWriter w) => State.WriteEnvelope(w);
    public override void ReceiveExtraAI(BinaryReader r)
    {
        var parsed = EbonState.ReadEnvelope(r);
        if (parsed is not { } next || Main.netMode == NetmodeID.Server || !next.CanReplace(State)) return;
        ApplyProjection(next, Main.GameUpdateCount);
    }
}

internal sealed class EbonConnection : ModPlayer
{
    internal Guid Token;
    internal uint LastNonce;
    internal ulong NextRequest;
    private void Reset() { Token = Guid.NewGuid(); LastNonce = 0; NextRequest = 0; }
    public override void Initialize() => Reset();
    public override void OnEnterWorld() => Reset();
    public override void PlayerDisconnect() => Reset();
}

public sealed class EbonBossBar : ModBossBar
{
    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs) return false;
        if (Main.npc[info.npcIndexToAimAt].ModNPC is not EbonBoss boss || !boss.Fresh || boss.State.Stage != EbonStage.Performance) return false;
        life = boss.State.Life; lifeMax = boss.State.MaxLife; shield = shieldMax = 0; return life > 0;
    }
}
