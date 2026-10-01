#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Point = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.EbonManor;

internal sealed record EbonStitchSource(EbonStitchPlan Plan) : IEntitySource { public string Context => "EbonStitch"; }
internal sealed record EbonStrikeSource(EbonStitchPlan Plan, byte Member, int Damage) : IEntitySource { public string Context => "EbonVerdict"; }

public sealed class EbonStitch : ModProjectile
{
    internal EbonStitchPlan Plan;
    internal bool Resolved;
    internal byte FailedMask;
    internal Point[] Positions = Array.Empty<Point>();
    private EbonVerdictClock visualClock;
    private bool presentationLogged;
    internal bool DrawLogged;
    internal float ImpactAge(float age)
    {
        float after=visualClock.Sample(Resolved,age,Plan.Fire,Plan.End);
        if(Resolved && after>=0 && !presentationLogged && Main.netMode!=NetmodeID.Server)
        {
            presentationLogged=true;
            EbonPackets.Log(FormattableString.Invariant($"event=StitchPresentation fight={Plan.Fight} born={Plan.Born} kind={Plan.Kind} failed_mask={FailedMask} verdict_delay_ticks={age-Plan.Fire:F1} result_positions={Positions.Length} observer={Main.myPlayer}"));
        }
        return after;
    }
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        Projectile.width=Projectile.height=2; Projectile.timeLeft=600;Projectile.penetrate=-1;
        Projectile.tileCollide=false;Projectile.ignoreWater=Projectile.netImportant=true;
    }
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanDamage()=>false;
    public override bool? CanCutTiles()=>false;
    public override bool PreDraw(ref Color lightColor)=>false;
    public override void OnSpawn(IEntitySource source) { if(source is EbonStitchSource own)Plan=own.Plan; }
    internal static bool TryBoss(in EbonStitchPlan plan,out EbonBoss? boss)
    {
        boss=plan.Boss>=0 && plan.Boss<Main.maxNPCs && Main.npc[plan.Boss].active?Main.npc[plan.Boss].ModNPC as EbonBoss:null;
        return plan.Fight!=Guid.Empty && boss is {Fresh:true} && boss.State.Fight==plan.Fight && boss.State.Live
            && boss.State.Life>0 && (plan.Members>>boss.State.Members.Length)==0;
    }
    public override void AI()
    {
        Projectile.Center=new(Plan.Center.X,Plan.Center.Y);
        if(TryBoss(Plan,out var boss))
        {
            Projectile.timeLeft=Math.Max(2,Plan.End+16-(int)boss!.VisualAge);
            if(Main.netMode!=NetmodeID.MultiplayerClient && boss.VisualAge>=Plan.End+16)Projectile.Kill();
        }
        else if(Main.netMode!=NetmodeID.MultiplayerClient)Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        Plan.Write(w);if(Plan.Fight==Guid.Empty)return;
        w.Write(Resolved);w.Write(FailedMask);w.Write((byte)Positions.Length);
        foreach(var p in Positions){w.Write(p.X);w.Write(p.Y);}
    }
    internal void Synchronize()
    {
        // The authority runtime runs after native Projectile.Update. That next
        // update clears netUpdate before AI, losing a post-update verdict flag.
        // Publish the fully committed marker through native ExtraAI explicitly.
        if(Main.netMode==NetmodeID.Server && Plan.Fight!=Guid.Empty && Projectile.active)
            NetMessage.SendData(MessageID.SyncProjectile,number:Projectile.whoAmI);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        var parsed=EbonStitchPlan.Read(r);if(parsed is not {} next)return;
        bool resolved=EbonState.Presence(r);byte failures=r.ReadByte(),count=r.ReadByte();
        if(count>8 || resolved!=(count>0) || (failures & ~next.Members)!=0 || !resolved && failures!=0
            || resolved && (next.Members>>count)!=0)throw new InvalidDataException("ebon.verdict");
        var positions=new Point[count];for(int i=0;i<count;i++)
        {positions[i]=new(r.ReadSingle(),r.ReadSingle());if(!EbonStitchRules.Point(positions[i]))throw new InvalidDataException("ebon.verdict_position");}
        if(Main.netMode==NetmodeID.Server || Plan.Fight!=Guid.Empty && Plan!=next
            || !EbonStitchRules.VerdictCanReplace(Resolved,FailedMask,resolved,failures))return;
        if(Resolved)
        {if(Positions.Length!=positions.Length)return;for(int i=0;i<count;i++)if(Positions[i]!=positions[i])return;}
        bool changed=Plan.Fight==Guid.Empty || Resolved!=resolved;
        Plan=next;Resolved=resolved;FailedMask=failures;Positions=positions;
        if(changed && Main.netMode==NetmodeID.MultiplayerClient)
            EbonPackets.Log($"event=StitchReplicaReceived fight={Plan.Fight} born={Plan.Born} kind={Plan.Kind} resolved={Resolved} failed_mask={FailedMask} result_positions={Positions.Length} observer={Main.myPlayer}");
    }
}

// Only the authority decides who failed. The receiving player's native hostile
// path applies defense, dodge, immunity and accessory hooks exactly once.
public sealed class EbonStitchStrike : ModProjectile
{
    internal EbonStitchPlan Plan;
    internal byte Member;
    internal int Budget;
    private bool spent;
    public override string Texture=>"Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {Projectile.width=Projectile.height=2;Projectile.timeLeft=90;Projectile.penetrate=-1;Projectile.tileCollide=false;Projectile.ignoreWater=Projectile.netImportant=true;}
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanCutTiles()=>false;
    public override bool? CanHitNPC(NPC target)=>false;
    public override bool PreDraw(ref Color lightColor)=>false;
    public override void OnSpawn(IEntitySource source)
    {if(source is EbonStrikeSource s){Plan=s.Plan;Member=s.Member;Budget=s.Damage;}}
    private bool Eligible(out Player? p)
    {
        p=null;
        if(spent || Budget<=0 || !EbonStitch.TryBoss(Plan,out var g) || Member>=g!.State.Members.Length
            || g.VisualAge<Plan.Fire || g.VisualAge>=Plan.Fire+EbonStitchRules.ImpactTicks)return false;
        var m=g.State.Members[Member];if(m.Out || m.Recovery.Downed)return false;p=Main.player[m.Slot];
        return p.active && !p.dead && !p.ghost && (Main.netMode!=NetmodeID.MultiplayerClient || Main.myPlayer==m.Slot);
    }
    public override bool? CanDamage()=>Eligible(out _)?null:false;
    public override bool CanHitPlayer(Player target)=>Eligible(out var p) && p!.whoAmI==target.whoAmI;
    public override bool? Colliding(Rectangle a,Rectangle b)=>Eligible(out _);
    public override void ModifyHitPlayer(Player target,ref Player.HurtModifiers modifiers)
        => modifiers.SetMaxDamage(EbonRules.NativeFinalDamageLimit(Budget));
    public override void OnHitPlayer(Player target,Player.HurtInfo info)
    {
        spent=true;Projectile.hostile=false;
        if(Main.netMode!=NetmodeID.Server && Main.myPlayer==target.whoAmI)
            EbonPackets.Log($"event=StitchNativeImpact fight={Plan.Fight} kind={Plan.Kind} member={Member} slot={target.whoAmI} intended_damage={Budget} native_source_damage={Projectile.damage} final_damage={info.Damage}");
    }
    public override void AI()
    {
        Projectile.damage=EbonRules.NativeSourceDamage(Budget);Projectile.hostile=Eligible(out var player);if(player is not null)Projectile.Center=player.Center;
        if(EbonStitch.TryBoss(Plan,out var g))
        {Projectile.timeLeft=Math.Max(2,Plan.Fire+EbonStitchRules.ImpactTicks+2-(int)g!.VisualAge);if(Main.netMode!=NetmodeID.MultiplayerClient && g.VisualAge>=Plan.Fire+EbonStitchRules.ImpactTicks)Projectile.Kill();}
        else if(Main.netMode!=NetmodeID.MultiplayerClient)Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter w){Plan.Write(w);if(Plan.Fight==Guid.Empty)return;w.Write(Member);w.Write(Budget);}
    public override void ReceiveExtraAI(BinaryReader r)
    {
        var next=EbonStitchPlan.Read(r);if(next is not {} p)return;byte member=r.ReadByte();int budget=r.ReadInt32();
        if(member>=8 || (p.Members & (1<<member))==0 || budget is < 1 or > EbonStitchRules.Damage)throw new InvalidDataException("ebon.impact");
        if(Main.netMode==NetmodeID.Server || Plan.Fight!=Guid.Empty && (Plan!=p || Member!=member || Budget!=budget))return;
        Plan=p;Member=member;Budget=budget;
    }
}

internal sealed class EbonStitchDirector
{
    private EbonStitch? marker;
    // Called by the runtime's schedule; the living roster is frozen into the plan.
    internal void Call(EbonState state,EbonBoss boss,EbonStitchKind kind,Point center)
    {
        byte mask=0;for(int i=0;i<state.Members.Length;i++)
            if(kind==EbonStitchKind.Stack || !state.Members[i].Out && !state.Members[i].Recovery.Downed)mask|=(byte)(1<<i);
        if(mask==0 || marker is not null)return;
        var plan=new EbonStitchPlan(state.Fight,(short)boss.NPC.whoAmI,kind,mask,state.Age,state.Age+EbonStitchRules.Warning,
            state.Age+EbonStitchRules.Warning+84,center);
        int slot=Projectile.NewProjectile(new EbonStitchSource(plan),new(plan.Center.X,plan.Center.Y),Vector2.Zero,ModContent.ProjectileType<EbonStitch>(),0,0,Main.myPlayer);
        if(slot>=Main.maxProjectiles)throw new InvalidOperationException("ebon.stitch_capacity");
        marker=(EbonStitch)Main.projectile[slot].ModProjectile;
        marker.Synchronize();
        EbonPackets.Log($"event=StitchCalled fight={state.Fight} kind={plan.Kind} born={plan.Born} fire={plan.Fire} members={mask}");
    }
    internal void Tick(EbonState state)
    {
        if(marker is not {} m)return;
        if(state.Age>=m.Plan.End){marker=null;return;}
        if(!m.Projectile.active || m.Projectile.ModProjectile!=m)throw new InvalidOperationException("ebon.stitch_missing");
        if(m.Resolved || state.Age<m.Plan.Fire)return;
        var positions=new Point[state.Members.Length];byte living=0;
        for(int i=0;i<positions.Length;i++)
        {
            var member=state.Members[i];var p=Main.player[member.Slot];positions[i]=new(p.Center.X,p.Center.Y);
            if(!member.Out && !member.Recovery.Downed && p.active && !p.dead && !p.ghost && p.GetModPlayer<EbonConnection>().Token==member.Connection)living|=(byte)(1<<i);
        }
        var damage=EbonStitchRules.Resolve(m.Plan.Kind,m.Plan.Center,positions,m.Plan.Members,living);
        m.Positions=positions;m.Resolved=true;
        for(byte i=0;i<damage.Length;i++)if(damage[i]>0)
        {
            m.FailedMask|=(byte)(1<<i);
            if(state.Age>=m.Plan.Fire+EbonStitchRules.ImpactTicks)continue;
            int slot=Projectile.NewProjectile(new EbonStrikeSource(m.Plan,i,damage[i]),Main.player[state.Members[i].Slot].Center,
                Vector2.Zero,ModContent.ProjectileType<EbonStitchStrike>(),EbonRules.NativeSourceDamage(damage[i]),0,Main.myPlayer);
            if(slot>=Main.maxProjectiles)throw new InvalidOperationException("ebon.impact_capacity");
            Main.projectile[slot].netUpdate=true;
        }
        m.Synchronize();
        EbonPackets.Log($"event=StitchResolved fight={state.Fight} kind={m.Plan.Kind} age={state.Age} living={living} failed={m.FailedMask} intended_damage={string.Join(",",damage)} native_source_damage={string.Join(",",Array.ConvertAll(damage,EbonRules.NativeSourceDamage))}");
        for(int i=0;i<positions.Length;i++)if((living & m.Plan.Members & (1<<i))!=0)
        {
            float nearest=float.PositiveInfinity;
            for(int j=0;j<positions.Length;j++)if(i!=j && (living & m.Plan.Members & (1<<j))!=0)
                nearest=Math.Min(nearest,Point.Distance(positions[i],positions[j]));
            EbonPackets.Log(FormattableString.Invariant($"event=StitchMember fight={state.Fight} kind={m.Plan.Kind} age={state.Age} slot={state.Members[i].Slot} gather_distance={Point.Distance(positions[i],m.Plan.Center):F1} nearest_peer={(float.IsPositiveInfinity(nearest)?-1:nearest):F1} intended_damage={damage[i]} native_source_damage={EbonRules.NativeSourceDamage(damage[i])}"));
        }
    }
    internal void Clear(Guid fight)
    {
        marker=null;
        foreach(Projectile p in Main.ActiveProjectiles)
            if(p.ModProjectile is EbonStitch a && a.Plan.Fight==fight || p.ModProjectile is EbonStitchStrike s && s.Plan.Fight==fight)p.Kill();
    }
}
