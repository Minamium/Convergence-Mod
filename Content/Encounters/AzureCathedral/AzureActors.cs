#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.UI.BigProgressBar;

namespace Convergence.Content.Encounters.AzureCathedral;

public sealed class AzureBoss : ModNPC
{
    internal AzureRuntime? Runtime;
    internal AzureState State;
    private ulong received;
    internal float VisualAge => State.Age + (Main.netMode == NetmodeID.MultiplayerClient ? (float)Math.Min(60UL, Main.GameUpdateCount - received) : 0);
    internal bool Fresh => Main.netMode != NetmodeID.MultiplayerClient || Main.GameUpdateCount - received <= 180;
    internal void ApplyProjection(in AzureState next, ulong at)
    { if(next.CanReplace(State)){State=next;received=Math.Max(received,at);} }
    public override string Texture => "Convergence/Assets/Textures/AzureCathedral/Liora";
    public override void SetStaticDefaults() => NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
    public override void SetDefaults()
    {
        NPC.width = 30; NPC.height = 54; NPC.lifeMax = AzureRules.Life(1, false); NPC.defense = 75;
        NPC.damage = 0; NPC.knockBackResist = 0; NPC.aiStyle = -1;
        NPC.noGravity = NPC.noTileCollide = NPC.lavaImmune = NPC.netAlways = true;
        NPC.dontTakeDamage = true; NPC.boss = true; NPC.BossBar = ModContent.GetInstance<AzureBossBar>();
        if (!Main.dedServ) { NPC.HitSound = SoundID.NPCHit5; Music = 0; }
    }
    public override void OnSpawn(IEntitySource source)
    { if (source is AzureActorSource a) { Runtime = a.Runtime; State = a.State; } }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool? CanBeHitByItem(Player p, Item item) => State.CanFight(p.whoAmI) ? null : false;
    public override bool? CanBeHitByProjectile(Projectile p) => State.CanFight(p.owner) ? null : false;
    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;
        NPC.dontTakeDamage = !Fresh || !State.Live || State.GirlLife <= 0;
        NPC.chaseable = State.Live && State.GirlLife > 0;
        NPC.boss = State.Stage is AzureStage.Countdown or AzureStage.Performance;
        if (State.GirlMax > 0) NPC.lifeMax = State.GirlMax;
        if (Main.netMode != NetmodeID.MultiplayerClient && (Runtime is null || !Runtime.Matches(this)))
        { NPC.active = false; if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI); }
    }
    public override bool CheckDead()
    {
        NPC.life = 1; if (State.Live) Runtime?.Killed(false);
        NPC.dontTakeDamage = true; NPC.netUpdate = Main.netMode != NetmodeID.MultiplayerClient; return false;
    }
    public override void SendExtraAI(BinaryWriter w) => State.WriteEnvelope(w);
    public override void ReceiveExtraAI(BinaryReader r)
    {
        var parsed = AzureState.ReadEnvelope(r);
        if (parsed is not { } next || Main.netMode == NetmodeID.Server || !next.CanReplace(State)) return;
        ApplyProjection(next,Main.GameUpdateCount);
    }
}

public sealed class AzureWorm : ModNPC
{
    internal Guid Fight;
    internal short Girl = -1, Head = -1, Previous = -1;
    internal byte Index;
    private int poseEpoch = -1;
    private AzurePhase posePhase;
    private Vector2 retreatFrom;
    private float retreatAngle;
    private ulong nextNativeHitLog;
    public override string Texture => "Convergence/Assets/Textures/AzureCathedral/Vitrion";
    public override void SetStaticDefaults() => NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
    public override void SetDefaults()
    {
        NPC.width = NPC.height = 86; NPC.lifeMax = AzureRules.Life(1, true); NPC.defense = 90;
        NPC.damage = AzureRules.NativeSourceDamage(360); NPC.knockBackResist = 0; NPC.aiStyle = -1;
        NPC.noGravity = NPC.noTileCollide = NPC.lavaImmune = NPC.netAlways = true;
        NPC.dontTakeDamage = true; NPC.BossBar = ModContent.GetInstance<AzureBossBar>();
        if (!Main.dedServ) NPC.HitSound = SoundID.NPCHit4;
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (source is not AzureWormSource a) return;
        Fight = a.Fight; Girl = a.Girl; Head = a.Head; Previous = a.Previous; Index = a.Index;
    }
    internal bool TryGirl(out AzureBoss? girl)
    {
        girl = Girl >= 0 && Girl < Main.maxNPCs && Main.npc[Girl].active ? Main.npc[Girl].ModNPC as AzureBoss : null;
        return girl is not null && Fight != Guid.Empty && girl.State.Fight == Fight && girl.Fresh;
    }
    internal bool TryPrevious(out NPC? previous)
    {
        previous = Previous >= 0 && Previous < Main.maxNPCs ? Main.npc[Previous] : null;
        return previous is { active: true, ModNPC: AzureWorm a } && a.Fight == Fight && a.Index == Index - 1;
    }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        => TryGirl(out var girl) && girl!.State.Live && girl.State.WormLife > 0 && girl.State.CanFight(target.whoAmI)
        && (Index == 0 ? NPC : Head >= 0 && Head < Main.maxNPCs ? Main.npc[Head] : NPC).ai[2] == 1;
    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    {
        if(AzureRules.DebugOneDamagePlaytest)modifiers.SetMaxDamage(1);
    }
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        if(Main.netMode==NetmodeID.Server || Main.myPlayer!=target.whoAmI || Main.GameUpdateCount<nextNativeHitLog)return;
        nextNativeHitLog=Main.GameUpdateCount+300;
        AzurePackets.Log($"event=WormNativeImpact fight={Fight} segment={Index} slot={target.whoAmI} intended_damage=360 native_source_damage={NPC.damage} final_damage={info.Damage}");
    }
    private bool Hittable(AzureBoss g) => AzureRules.WormDamageable(g.State.Phase,Index,g.State.Live,g.State.WormLife,g.State.WormMax);
    public override bool? CanBeHitByItem(Player p, Item item) => TryGirl(out var g) && Hittable(g!) && g!.State.CanFight(p.whoAmI) ? null : false;
    public override bool? CanBeHitByProjectile(Projectile p) => TryGirl(out var g) && Hittable(g!) && g!.State.CanFight(p.owner) ? null : false;
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if(Index==0 && TryGirl(out var g) && g!.State.Phase==AzurePhase.Duet)
            modifiers.SetMaxDamage(Math.Max(1,NPC.life-AzureRules.WormFloor(g.State.WormMax)));
    }
    public override void HitEffect(NPC.HitInfo hit)
    {
        // StrikeNPC copies the depleted shared HP onto the struck segment before
        // calling the HEAD's CheckDead. Segment CheckDead is never called for a
        // realLife child. Preserve only its native shell; do not heal the head or
        // duplicate damage/decide the outcome here.
        if(Index>0 && NPC.life<=0 && TryGirl(out _))NPC.life=1;
    }
    internal static void RetainChain(Guid fight)
    {
        foreach(var n in Main.ActiveNPCs)
        {
            if(n.ModNPC is not AzureWorm w || w.Fight!=fight)continue;
            n.life=Math.Max(1,n.life);n.dontTakeDamage=true;n.chaseable=false;
            // Late already-in-flight native strike packets must not destroy a
            // retained ending part. AI releases this only for an actual live pool.
            n.immortal=true;n.netUpdate=Main.netMode!=NetmodeID.MultiplayerClient;
        }
    }
    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;
        if (!TryGirl(out var g) || g is null)
        {
            NPC.dontTakeDamage = true;
            if (Main.netMode != NetmodeID.MultiplayerClient) NPC.active = false;
            return;
        }
        NPC.dontTakeDamage = !Hittable(g);
        NPC.immortal = g.State.WormLife<=0 || g.State.EndAt>=0;
        NPC.chaseable = !NPC.dontTakeDamage;
        NPC.boss = Index == 0 && g.State.Live;
        NPC.lifeMax = g.State.WormPoolMax;
        if (Index > 0) { NPC.realLife = Head; NPC.life = Math.Max(1, g.State.WormLife); }
        if (CeremonyPose(g)) return;
        if (Index == 0)
        {
            if(Main.netMode!=NetmodeID.MultiplayerClient && g.State.Phase==AzurePhase.Duet)
                NPC.life=Math.Max(NPC.life,AzureRules.WormFloor(g.State.WormMax));
            if (NPC.velocity.LengthSquared() > .2f) NPC.rotation = NPC.velocity.ToRotation();
            return;
        }
        if (!TryPrevious(out var previous)) return;
        Vector2 difference = previous!.Center - NPC.Center;
        Vector2 direction = difference.SafeNormalize(Vector2.UnitX);
        NPC.Center = previous.Center - direction * AzureRules.SegmentSpacing;
        NPC.rotation = direction.ToRotation(); NPC.velocity = Vector2.Zero;
        NPC.realLife = Head; NPC.life = Math.Max(1, g.State.WormLife);
    }
    private bool CeremonyPose(AzureBoss girl)
    {
        var s = girl.State;
        bool retreat = s.Phase == AzurePhase.Duet && s.StagingAt >= 0 && s.EndAt < 0;
        if (!retreat && s.Phase is not (AzurePhase.Devouring or AzurePhase.Melting)) return false;
        int epoch = retreat ? s.StagingAt : s.PhaseAt;
        if (poseEpoch != epoch || posePhase != s.Phase)
        { poseEpoch = epoch; posePhase = s.Phase; retreatFrom = NPC.Center; retreatAngle = NPC.rotation; }
        float t = girl.VisualAge - epoch;
        var center = new System.Numerics.Vector2(s.Field.CenterX, s.Field.CenterY);
        var direction = AzureFlight.CeremonyDirection(center, s.CeremonySide);
        var next = retreat || s.Phase == AzurePhase.Melting && t < AzureRules.StagingTicks
            ? AzureFlight.Retreat(new(retreatFrom.X, retreatFrom.Y), center, s.CeremonySide, Index, t)
            : s.Phase == AzurePhase.Devouring ? AzureFlight.DevourPosition(center, s.CeremonySide, Index, t)
            : AzureFlight.MeltPosition(center, s.CeremonySide, Index, t);
        float angle = MathF.Atan2(direction.Y, direction.X);
        NPC.rotation = retreat || s.Phase == AzurePhase.Melting && t < AzureRules.StagingTicks
            ? retreatAngle + MathHelper.WrapAngle(angle - retreatAngle) * AzureRules.Ease(t / AzureRules.StagingTicks) : angle;
        NPC.Center = new(next.X, next.Y); NPC.velocity = Vector2.Zero; NPC.ai[2] = 0;
        return true;
    }
    public override bool CheckDead()
    {
        NPC.life = 1;
        if (TryGirl(out var g))
        {
            if(g!.State.Phase==AzurePhase.Duet && Index==0) NPC.life=AzureRules.WormFloor(g.State.WormMax);
            else if(g.State.Live && g.State.Phase==AzurePhase.Fury)
            {
                // Native realLife owns subtraction. Only an actually lethal head
                // result may finish the encounter; a segment never has its own HP pool.
                var head=Index==0?NPC:Head>=0 && Head<Main.maxNPCs?Main.npc[Head]:null;
                if(Index==0 || head is {active:true,ModNPC:AzureWorm a} && a.Fight==Fight && head.life<=0)
                {g.Runtime?.Killed(true);RetainChain(Fight);}
            }
        }
        NPC.dontTakeDamage = true; NPC.netUpdate = Main.netMode != NetmodeID.MultiplayerClient; return false;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(Fight != Guid.Empty); if (Fight == Guid.Empty) return;
        w.Write(Fight.ToByteArray()); w.Write(Girl); w.Write(Head); w.Write(Previous); w.Write(Index);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        if (!AzureState.Presence(r)) return;
        var id = AzureState.Id(r); short girl = r.ReadInt16(), head = r.ReadInt16(), previous = r.ReadInt16(); byte index = r.ReadByte();
        if (girl is < 0 or >= 200 || head is < -1 or >= 200 || previous is < -1 or >= 200 || index > AzureRules.Segments
            || index > 0 && (head < 0 || previous < 0)) throw new InvalidDataException("azure.worm");
        if (Main.netMode == NetmodeID.Server || Fight != Guid.Empty && (Fight != id || Girl != girl || Index != index || Head != head || Previous != previous)) return;
        Fight = id; Girl = girl; Head = head; Previous = previous; Index = index;
    }
}

internal sealed class AzureConnection : ModPlayer
{
    internal Guid Token;
    internal uint LastNonce;
    internal ulong NextRequest;
    private void Reset() { Token = Guid.NewGuid(); LastNonce = 0; NextRequest = 0; }
    public override void Initialize() => Reset();
    public override void OnEnterWorld() => Reset();
    public override void PlayerDisconnect() => Reset();
}
public sealed class AzureBossBar : ModBossBar
{
    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs) return false;
        var npc = Main.npc[info.npcIndexToAimAt]; AzureBoss? girl = npc.ModNPC as AzureBoss;
        if (girl is null && npc.ModNPC is AzureWorm worm) worm.TryGirl(out girl);
        if (girl is null || !girl.Fresh || !girl.State.Live) return false;
        life = girl.State.TotalLife; lifeMax = girl.State.TotalMax; shield = shieldMax = 0; return life > 0;
    }
}
