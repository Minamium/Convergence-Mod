#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Shared;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.AzureCathedral;

internal sealed record AzureActorSource(AzureRuntime Runtime, AzureState State) : IEntitySource { public string Context => "AzureCathedral"; }
internal sealed record AzureWormSource(Guid Fight, short Girl, short Head, short Previous, byte Index) : IEntitySource { public string Context => "AzureLeviathan"; }
internal sealed record AzureAttackSource(AzureAttackPlan Plan) : IEntitySource { public string Context => "AzureCathedralAttack"; }

// One exact-Fight owner, one authority tick, one terminal commit. Native NPCs
// carry damage and projections only; they cannot start/advance another session.
internal sealed class AzureRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly ulong sequence;
    private readonly int summoner;
    private readonly IRaidPedestal pedestal;
    private AzureBoss? girl;
    private AzureWorm? worm;
    private AzureMember[] members = Array.Empty<AzureMember>();
    private AzureStage stage;
    private AzurePhase phase;
    private int phaseAt = -1;
    private int age, music = -1, unlock = -1, ending = -1, gm, wm;
    private bool claimed, cleaned, cancelled, enraged;
    private readonly AzureDefeats defeats = new();
    private readonly AzureChorusDirector chorus = new();
    private bool girlDead => defeats.Girl;
    private bool wormDead => defeats.Worm;
    private Vector2 chargeGoal, curveA, curveB, curveC, curveD;
    private int previousDamage;
    private bool projectionDirty, floorReached;
    private Vector2 ceremonyFrom, ceremonyVelocity, volleyExit;
    internal AzureRuntime(FightId id, int sender, IRaidPedestal core, ulong seq)
    { fight = id; summoner = sender; pedestal = core; sequence = seq; gm = AzureRules.Life(1, false); wm = AzureRules.Life(1, true); }
    private AzureState State => new(fight.Value, age, music, unlock, ending, stage, members,
        (int)pedestal.Ground.X, (int)pedestal.Ground.Y, gm, wm,
        girlDead ? 0 : Math.Clamp(girl?.NPC.life ?? gm, 0, gm), wormDead ? 0 : Math.Clamp(worm?.NPC.life ?? wm, 0, wm),
        (short)(worm?.NPC.whoAmI ?? -1), enraged, phase, phaseAt);
    internal bool Matches(AzureBoss value) => !cleaned && ReferenceEquals(girl, value) && value.State.Fight == fight.Value;
    internal void Killed(bool isWorm)
    {
        if (cleaned || !State.Live || isWorm && phase != AzurePhase.Fury || !defeats.Mark(isWorm)) return;
        if (!isWorm) { ClearHazards(AzureAttackKind.MouthBeam); ClearHazards(AzureAttackKind.Icicle); ClearHazards(AzureAttackKind.GlassRain); ClearHazards(AzureAttackKind.GlacialCut); chorus.Clear(fight.Value); }
        else AzureWorm.RetainChain(fight.Value);
        AzurePackets.Log($"event=ActorDefeated fight={fight.Value} actor={(isWorm ? "worm" : "girl")} age={age}"); Project(true);
    }
    internal bool Request(int sender, Guid connection, bool ready, bool cancel)
    {
        if (cleaned || stage != AzureStage.Ready || sender < 0 || sender >= Main.maxPlayers) return false;
        var player = Main.player[sender];
        if (!player.active || player.dead || player.GetModPlayer<AzureConnection>().Token != connection) return false;
        int index = Array.FindIndex(members, m => m.Slot == sender && m.Connection == connection);
        if (index < 0) return false;
        if (cancel) { if (sender != summoner) return false; cancelled = true; }
        else members[index] = members[index] with { Ready = ready };
        Project(true); return true;
    }
    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (cleaned || Main.netMode == NetmodeID.MultiplayerClient) return EncounterRuntimeUpdate.None;
        if (context.FightId != fight || context.EncounterSequence != sequence) return End(EncounterEndReason.Invalidated);
        if (context.Lifecycle == EncounterLifecycle.Validating)
        {
            if (!Roster() || !pedestal.Claim(sequence, fight)) return End(EncounterEndReason.Invalidated);
            claimed = true; Scale();
            int slot = NPC.NewNPC(new AzureActorSource(this, State), (int)pedestal.Ground.X, (int)pedestal.Ground.Y - 70, ModContent.NPCType<AzureBoss>());
            if (slot >= Main.maxNPCs) return End(EncounterEndReason.EncounterActorMissing);
            girl = (AzureBoss)Main.npc[slot].ModNPC; girl.NPC.life = girl.NPC.lifeMax = gm;
            girl.NPC.Center = new(State.Field.CenterX, State.Field.CenterY); Project(true);
            AzurePackets.Log($"event=Preparing fight={fight.Value} members={members.Length}");
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Preparing);
        }
        if (girl is null || !girl.NPC.active || girl.NPC.ModNPC != girl || !pedestal.IsOwned(fight)) return End(EncounterEndReason.EncounterActorMissing);
        age++;
        if (cancelled || age >= 60 * 60 * 15) return End(EncounterEndReason.Cancelled);
        if (context.Lifecycle == EncounterLifecycle.Preparing)
        {
            if (!Roster()) return End(EncounterEndReason.Invalidated);
            if (age >= AzureRules.Deploy) stage = AzureStage.Ready;
            if (stage == AzureStage.Ready && Array.TrueForAll(members, m => m.Ready && !Main.player[m.Slot].dead))
            {
                Scale(); girl.NPC.life = girl.NPC.lifeMax = gm;
                music = age + 45; unlock = music + AzureRules.Intro; stage = AzureStage.Countdown;
                if (!pedestal.Activate(fight)) return End(EncounterEndReason.Invalidated);
                Project(true);
                AzurePackets.Log($"event=AllReady fight={fight.Value} players={members.Length} girl_hp={gm} worm_hp={wm} music={music} unlock={unlock}");
                return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Active);
            }
            if (age > 60 * 180) return End(EncounterEndReason.Cancelled);
        }
        else if (context.Lifecycle == EncounterLifecycle.Active)
        {
            if (worm is not null)
            {
                int parts = 0; bool linked = true;
                foreach (var part in Main.ActiveNPCs)
                    if (part.ModNPC is AzureWorm a && a.Fight == fight.Value)
                    { parts++; linked &= a.Index == 0 || a.TryPrevious(out _); }
                if (parts != AzureRules.Segments + 1 || !linked || !worm!.NPC.active || worm.NPC.ModNPC != worm)
                {
                    AzurePackets.Log($"event=ActorMissing fight={fight.Value} age={age} live_worm=True parts={parts} linked={linked}");
                    return End(EncounterEndReason.EncounterActorMissing);
                }
            }
            for (int i = 0; i < members.Length; i++)
            {
                var p = Main.player[members[i].Slot];
                if (!p.active || p.dead || p.ghost || p.GetModPlayer<AzureConnection>().Token != members[i].Connection)
                    members[i] = members[i] with { Out = true };
            }
            bool allOut = Array.TrueForAll(members, m => m.Out);
            if (ending < 0 && (allOut || girlDead && wormDead))
            {
                ending = age; stage = allOut ? AzureStage.Defeat : AzureStage.Victory;
                if (stage == AzureStage.Victory) { phase=AzurePhase.Melting; phaseAt=age; ceremonyFrom=worm!.NPC.Center; }
                ClearHazards(); Project(true);
                AzurePackets.Log($"event={stage} fight={fight.Value} age={age} remaining_hp={State.TotalLife}");
            }
            if (ending >= 0)
            {
                girl.NPC.velocity = Vector2.Zero;
                if (worm is not null)
                {
                    worm.NPC.ai[2]=0;
                    float t=age-ending;
                    var center=new Vector2(State.Field.CenterX,State.Field.CenterY);
                    // Rush to the stage, brake; the linked chain dissolves from head to tail.
                    Vector2 next=Vector2.Lerp(ceremonyFrom,center,AzureRules.Ease(t/72));
                    worm.NPC.velocity=stage==AzureStage.Victory?next-worm.NPC.Center:Vector2.Zero;
                }
                if (age >= ending + AzureRules.ExitDuration(stage)) return End(stage == AzureStage.Victory ? EncounterEndReason.Victory : EncounterEndReason.Defeat);
                Project(age % 6 == 0); return Publish();
            }
            if (worm is null && age >= music + AzureRules.WormArrival) { SpawnWorm(); Project(true); }
            if (age >= unlock) stage = AzureStage.Performance;
            if (phase==AzurePhase.Duet && worm is not null && worm.NPC.life<=AzureRules.WormFloor(wm))
            {
                worm.NPC.life=AzureRules.WormFloor(wm);
                if(!floorReached){floorReached=true;AzurePackets.Log($"event=WormFloor fight={fight.Value} age={age} hp={worm.NPC.life}");}
            }
            if (stage==AzureStage.Performance && AzureRules.CanDevour(phase,State.GirlLife,State.WormLife,wm))
            {
                phase=AzurePhase.Devouring;phaseAt=age;ceremonyFrom=worm!.NPC.Center;ceremonyVelocity=worm.NPC.velocity;
                ClearHazards();worm.NPC.ai[2]=0;Project(true);
                AzurePackets.Log($"event=Devouring fight={fight.Value} age={age} contact={age+AzureRules.DevourContact} approach_start_distance={Vector2.Distance(V(AzureFlight.DevourStaging(N(ceremonyFrom),N(girl.NPC.Center))),girl.NPC.Center):F0}");
            }
            if(AzureRules.CanRefill(phase,phaseAt,age))
            {
                phase=AzurePhase.Fury;phaseAt=age;enraged=true;worm!.NPC.life=wm;
                previousDamage=0;Project(true);
                AzurePackets.Log($"event=Fury fight={fight.Value} age={age} worm_hp={wm}");
            }
            Move();
            if(worm is not null && !wormDead)
            {
                // The worm may leave the arena, never the actual Terraria map.
                var next=worm.NPC.Center+worm.NPC.velocity;
                next.X=Math.Clamp(next.X,100,Main.maxTilesX*16-100);next.Y=Math.Clamp(next.Y,100,Main.maxTilesY*16-100);
                worm.NPC.velocity=next-worm.NPC.Center;
            }
            if (State.Live)
            {
                chorus.Tick(State, girl, !girlDead);
                Schedule();
            }
            if (age % 300 == 0)
            {
                int damage = State.TotalMax - State.TotalLife;
                AzurePackets.Log($"event=Progress fight={fight.Value} age={age} phrase={AzureRules.Phrase(age,State.AttackEpoch)} phase={phase} girl_hp={State.GirlLife} worm_hp={State.WormLife} interval_dps={Math.Max(0,damage-previousDamage)/5} enraged={enraged}");
                previousDamage = damage;
            }
        }
        Project(age % 6 == 0); return Publish();
    }
    private EncounterRuntimeUpdate Publish()
    { bool dirty=projectionDirty;projectionDirty=false;return dirty?EncounterRuntimeUpdate.ObservableChange():EncounterRuntimeUpdate.None; }
    private bool Roster()
    {
        var next = new List<AzureMember>();
        foreach (var p in Main.ActivePlayers)
        {
            if (p.ghost) continue;
            var id = p.GetModPlayer<AzureConnection>().Token;
            var old = Array.Find(members, m => m.Slot == p.whoAmI && m.Connection == id);
            next.Add(new((byte)p.whoAmI, id, old.Ready && !p.dead, false));
        }
        if (next.Count is 0 or > AzureRules.Members) return false;
        bool changed = next.Count != members.Length;
        for (int i = 0; i < next.Count && !changed; i++) changed |= next[i].Slot != members[i].Slot || next[i].Connection != members[i].Connection;
        if (changed) for (int i = 0; i < next.Count; i++) next[i] = next[i] with { Ready = false };
        members = next.ToArray(); if (changed) Project(true); return true;
    }
    private void Scale() { gm = AzureRules.Life(members.Length, false); wm = AzureRules.Life(members.Length, true); }
    private void SpawnWorm()
    {
        short head = -1, previous = -1;
        var f = State.Field;
        for (byte index = 0; index <= AzureRules.Segments; index++)
        {
            int slot = NPC.NewNPC(new AzureWormSource(fight.Value, (short)girl!.NPC.whoAmI, head, previous, index),
                (int)f.CenterX + 1050 + (int)(index * AzureRules.SegmentSpacing), (int)f.CenterY - 280, ModContent.NPCType<AzureWorm>());
            if (slot >= Main.maxNPCs) throw new InvalidOperationException("azure.worm_capacity");
            if (index == 0) { head = (short)slot; worm = (AzureWorm)Main.npc[slot].ModNPC; }
            var n = Main.npc[slot]; n.Center = new(f.CenterX + 1050 + index * AzureRules.SegmentSpacing, f.CenterY - 280);
            n.rotation = MathHelper.Pi;
            n.life = n.lifeMax = wm; n.realLife = index == 0 ? -1 : head; n.netUpdate = true;
            previous = (short)slot;
        }
    }
    private Vector2 Focus()
    {
        var alive = Array.FindAll(members, m => !m.Out);
        int n = (Math.Max(0, age - State.AttackEpoch) / AzureRules.ChargeTicks) % Math.Max(1, alive.Length);
        return alive.Length == 0 ? girl!.NPC.Center : Main.player[alive[n].Slot].Center;
    }
    private void Move()
    {
        var f = State.Field; int phrase = AzureRules.Phrase(age, State.AttackEpoch), t = AzureRules.Clock(age, State.AttackEpoch);
        girl!.NPC.Center = new(f.CenterX, f.CenterY); girl.NPC.velocity = Vector2.Zero;
        girl.NPC.spriteDirection = Focus().X < girl.NPC.Center.X ? -1 : 1;
        if (worm is null || wormDead) return;
        if(phase==AzurePhase.Devouring)
        {
            int scene=age-phaseAt;
            Vector2 next=V(AzureFlight.DevourPosition(N(ceremonyFrom),N(ceremonyVelocity),N(girl.NPC.Center),scene));
            worm.NPC.velocity=next-worm.NPC.Center;worm.NPC.ai[2]=0;
            if(scene>=AzureRules.DevourRetreat && scene<AzureRules.DevourRush)
                worm.NPC.rotation=worm.NPC.rotation.AngleTowards((girl.NPC.Center-worm.NPC.Center).ToRotation(),.045f);
            return;
        }
        Vector2 goal; float speed;
        if (State.Live && (AzureRules.ChargePhrase(phrase) || enraged && phrase is 2 or 5))
        {
            int dash = t % AzureRules.ChargeTicks;
            if (dash == 0)
            {
                chargeGoal = Focus(); worm!.NPC.ai[0] = chargeGoal.X; worm.NPC.ai[1] = chargeGoal.Y;
                int serial = (age-State.AttackEpoch)/AzureRules.ChargeTicks;
                float side = serial%2 == 0 ? -1 : 1;
                curveA = worm.NPC.Center;
                curveD = new(f.CenterX + side*2050, Math.Clamp(chargeGoal.Y + side*240, f.Top+80, f.Bottom-80));
                var direction = (chargeGoal-curveD).SafeNormalize(Vector2.UnitX);
                curveB = curveA + worm.NPC.velocity.SafeNormalize(-Vector2.UnitX)*1100;
                curveC = curveD - direction*980;
                worm.NPC.netUpdate = true;
            }
            if (dash < AzureRules.ChargeWarning)
            {
                var next = AzureFlight.Curve(N(curveA),N(curveB),N(curveC),N(curveD),(dash+1)/(float)AzureRules.ChargeWarning);
                worm.NPC.ai[2]=0; worm.NPC.velocity=V(next)-worm.NPC.Center; return;
            }
            else if (dash == AzureRules.ChargeWarning)
            {
                worm!.NPC.velocity = (chargeGoal - worm.NPC.Center).SafeNormalize(Vector2.UnitX) * (enraged ? 74 : 62);
                worm.NPC.ai[2] = 1; worm.NPC.netUpdate = true; return;
            }
            else if (dash < AzureRules.ChargeEnd) return;
            else { goal = new(f.CenterX, f.Top - 680); speed = 42; }
        }
        else if(State.Live && phrase is 1 or 4)
        {
            // Join a six-second straight diagonal crossing with a matched tangent.
            // The whole chain travels while firing; there is no stop-and-shoot hold.
            if(t==0)
            {
                float side=phrase==1?-1:1;curveA=worm.NPC.Center;
                curveB=curveA+worm.NPC.velocity.SafeNormalize(-Vector2.UnitX)*800;
                curveD=new(f.CenterX+side*1650,f.Top-120);
                volleyExit=new(f.CenterX-side*1650,f.Bottom+150);
                curveC=curveD-(volleyExit-curveD)/AzureRules.VolleyTransit*(AzureRules.VolleyApproach/3f);
            }
            if(t<AzureRules.VolleyApproach)
            {
                float u=(t+1)/(float)AzureRules.VolleyApproach;
                worm.NPC.velocity=V(AzureFlight.Curve(N(curveA),N(curveB),N(curveC),N(curveD),u))-worm.NPC.Center;
                worm.NPC.ai[2]=0;return;
            }
            if(t<AzureRules.VolleyApproach+AzureRules.VolleyTransit)
            {
                worm.NPC.velocity=Vector2.Lerp(curveD,volleyExit,AzureRules.VolleyProgress(t+1))-worm.NPC.Center;
                worm.NPC.ai[2]=0;return;
            }
            goal=new(f.CenterX+(phrase==1?2200:-2200),f.Top-700);speed=enraged?46:38;
        }
        else
        {
            float theta = (age - Math.Max(0, music)) * (enraged ? .0065f : .005f);
            goal = new(f.CenterX + MathF.Cos(theta) * 2200, f.CenterY + MathF.Sin(theta) * 1080);
            speed = stage == AzureStage.Countdown ? 38 : 42;
        }
        worm!.NPC.ai[2] = 0;
        goal.X=Math.Clamp(goal.X,100,Main.maxTilesX*16-100);goal.Y=Math.Clamp(goal.Y,100,Main.maxTilesY*16-100);
        worm.NPC.velocity = V(AzureFlight.Steer(N(worm.NPC.velocity), N(goal-worm.NPC.Center), speed));
        if (worm.NPC.velocity.LengthSquared() > 1) worm.NPC.rotation = worm.NPC.velocity.ToRotation();
    }
    private static System.Numerics.Vector2 N(Vector2 v) => new(v.X,v.Y);
    private static Vector2 V(System.Numerics.Vector2 v) => new(v.X,v.Y);
    private void Schedule()
    {
        int phrase = AzureRules.Phrase(age, State.AttackEpoch), t = AzureRules.Clock(age, State.AttackEpoch);
        if (AzureRules.ChorusPhrase(phrase)) return;
        if(!girlDead && AzureRules.ChargePhrase(phrase) && t%160==60)
        {
            // Freeze the cut over the announced target position; it never chases
            // the player after its one-second white warning has appeared.
            int note=(age-State.AttackEpoch)/160;
            float angle=note%3==0?-.62f:note%3==1?.62f:0;
            Spawn(AzureAttackKind.GlacialCut,Focus(),angle,4000,AzureRules.CutRadius,340,AzureRules.CutWarning,AzureRules.CutLive);
        }
        if (!girlDead && t % (enraged ? 70 : 105) == 0)
        {
            var f = State.Field; Vector2 source = girl!.NPC.Center + new Vector2(girl.NPC.spriteDirection * 23, -7);
            if (phrase is 1 or 4)
            {
                // Fixed gaps alternate, never randomized into an impossible wall.
                int lane = t / (enraged ? 70 : 105);
                for (int i = 0; i < 15; i++)
                {
                    if (i % 5 == lane % 5 || i % 5 == (lane + 1) % 5) continue;
                    Spawn(AzureAttackKind.GlassRain, new(f.Left + 110 + i * 166, f.Top + 40), MathHelper.PiOver2, 1100, 13, 330, 64, 102);
                }
            }
            else
            {
                float direction = (Focus() - source).ToRotation(); int n = enraged ? 11 : 9;
                for (int i = 0; i < n; i++) Spawn(AzureAttackKind.Icicle, source, direction + (i - (n - 1) * .5f) * .18f, 3000, 12, 300, 60, 140);
            }
        }
        if (!girlDead && phrase is 1 or 4 && t == 200)
            Spawn(AzureAttackKind.MouthBeam, girl!.NPC.Center, (Focus()-girl.NPC.Center).ToRotation()-.6f, 3200, 66, 360, 96, 180);
        if (!wormDead && phrase is 1 or 4 && t==AzureRules.VolleyFire-AzureRules.VolleyWarning)
        {
            var alive=Array.FindAll(members,m=>!m.Out);
            foreach(var n in Main.ActiveNPCs)
                if(n.ModNPC is AzureWorm part && part.Fight==fight.Value && alive.Length>0)
                {
                    // Rotate segments over living connections; freeze each initial aim on authority.
                    var target=alive[part.Index%alive.Length];
                    Vector2 delta=Main.player[target.Slot].Center-n.Center;
                    Spawn(AzureAttackKind.FrostBolt,n.Center,delta.ToRotation(),Math.Clamp(delta.Length(),1,10000),13,300,AzureRules.VolleyWarning,170,target.Slot,(short)n.whoAmI);
                }
            AzurePackets.Log($"event=FrostVolley fight={fight.Value} age={age} parts={AzureRules.Segments+1} targets={alive.Length}");
        }
    }
    private void Spawn(AzureAttackKind kind, Vector2 source, float direction, float length, float width, int damage, int warning, int live, short target=-1,short emitter=-1)
    {
        var plan = new AzureAttackPlan(fight.Value, (short)girl!.NPC.whoAmI, kind, age, age + warning, age + warning + live,
            source.X, source.Y, direction, length, width, damage,target,emitter);
        int slot = Projectile.NewProjectile(new AzureAttackSource(plan), source, Vector2.Zero,
            ModContent.ProjectileType<AzureAttack>(), damage, 0, Main.myPlayer);
        if (slot >= Main.maxProjectiles) throw new InvalidOperationException("azure.attack_capacity");
        Main.projectile[slot].netUpdate = true;
    }
    private void Project(bool sync)
    {
        if (girl is null) return;
        girl.State = State; if (sync) { projectionDirty=true; girl.NPC.netUpdate = true; if (worm is not null && worm.NPC.ModNPC==worm) worm.NPC.netUpdate = true; }
    }
    private EncounterRuntimeUpdate End(EncounterEndReason reason) => EncounterRuntimeUpdate.End(AzureTermination.End(reason));
    private void ClearHazards(AzureAttackKind? kind = null)
    {
        if (kind is null) chorus.Clear(fight.Value);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is AzureAttack a && a.Plan.Fight == fight.Value && (kind is null || a.Plan.Kind == kind)) p.Kill();
    }
    public void Cleanup(in EncounterCleanupContext context)
    {
        if (cleaned || context.FightId != fight) return;
        try
        {
            ClearHazards();
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (!(n.ModNPC is AzureBoss b && b.State.Fight == fight.Value || n.ModNPC is AzureWorm w && w.Fight == fight.Value)) continue;
                n.active = false; if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: n.whoAmI);
            }
        }
        finally { if (claimed) { pedestal.Release(fight); claimed = false; } cleaned = true; }
        AzurePackets.Log($"event=Cleaned fight={fight.Value} reason={context.EndReason} age={age}");
    }
}
