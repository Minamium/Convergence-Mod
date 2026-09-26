#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Point = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.AzureCathedral;

internal sealed record AzureChorusSource(AzureChorusPlan Plan) : IEntitySource { public string Context => "AzureChorus"; }
internal sealed record AzureStrikeSource(AzureChorusPlan Plan, byte Member, int Damage) : IEntitySource { public string Context => "AzureVerdict"; }

public sealed class AzureChorus : ModProjectile
{
    internal AzureChorusPlan Plan;
    internal bool Resolved;
    internal byte FailedMask;
    internal Point[] Positions = Array.Empty<Point>();
    private AzureVerdictClock visualClock;
    internal float ImpactAge(float age)=>visualClock.Sample(Resolved,age,Plan.Fire,Plan.End);
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
    public override void OnSpawn(IEntitySource source) { if(source is AzureChorusSource own)Plan=own.Plan; }
    internal static bool TryGirl(in AzureChorusPlan plan,out AzureBoss? girl)
    {
        girl=plan.Girl>=0 && plan.Girl<Main.maxNPCs && Main.npc[plan.Girl].active?Main.npc[plan.Girl].ModNPC as AzureBoss:null;
        return plan.Fight!=Guid.Empty && girl is {Fresh:true} && girl.State.Fight==plan.Fight && girl.State.Live
            && girl.State.GirlLife>0 && (plan.Members>>girl.State.Members.Length)==0;
    }
    public override void AI()
    {
        Projectile.Center=new(Plan.Center.X,Plan.Center.Y);
        if(TryGirl(Plan,out var girl))
        {
            Projectile.timeLeft=Math.Max(2,Plan.End+16-(int)girl!.VisualAge);
            if(Main.netMode!=NetmodeID.MultiplayerClient && girl.VisualAge>=Plan.End+16)Projectile.Kill();
        }
        else if(Main.netMode!=NetmodeID.MultiplayerClient)Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        Plan.Write(w);if(Plan.Fight==Guid.Empty)return;
        w.Write(Resolved);w.Write(FailedMask);w.Write((byte)Positions.Length);
        foreach(var p in Positions){w.Write(p.X);w.Write(p.Y);}
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        var parsed=AzureChorusPlan.Read(r);if(parsed is not {} next)return;
        bool resolved=AzureState.Presence(r);byte failures=r.ReadByte(),count=r.ReadByte();
        if(count>8 || resolved!=(count>0) || (failures & ~next.Members)!=0 || !resolved && failures!=0
            || resolved && (next.Members>>count)!=0)throw new InvalidDataException("azure.verdict");
        var positions=new Point[count];for(int i=0;i<count;i++)
        {positions[i]=new(r.ReadSingle(),r.ReadSingle());if(!AzureChorusRules.Point(positions[i]))throw new InvalidDataException("azure.verdict_position");}
        if(Main.netMode==NetmodeID.Server || Plan.Fight!=Guid.Empty && Plan!=next
            || !AzureChorusRules.VerdictCanReplace(Resolved,FailedMask,resolved,failures))return;
        if(Resolved)
        {if(Positions.Length!=positions.Length)return;for(int i=0;i<count;i++)if(Positions[i]!=positions[i])return;}
        Plan=next;Resolved=resolved;FailedMask=failures;Positions=positions;
    }
}

// Only the authority decides who failed. The receiving player's native hostile
// path applies defense, dodge, immunity and accessory hooks exactly once.
public sealed class AzureChorusStrike : ModProjectile
{
    internal AzureChorusPlan Plan;
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
    {if(source is AzureStrikeSource s){Plan=s.Plan;Member=s.Member;Budget=s.Damage;}}
    private bool Eligible(out Player? p)
    {
        p=null;
        if(spent || Budget<=0 || !AzureChorus.TryGirl(Plan,out var g) || Member>=g!.State.Members.Length
            || g.VisualAge<Plan.Fire || g.VisualAge>=Plan.Fire+AzureChorusRules.ImpactTicks)return false;
        var m=g.State.Members[Member];if(m.Out || m.Recovery.Downed)return false;p=Main.player[m.Slot];
        return p.active && !p.dead && !p.ghost && (Main.netMode!=NetmodeID.MultiplayerClient || Main.myPlayer==m.Slot);
    }
    public override bool? CanDamage()=>Eligible(out _)?null:false;
    public override bool CanHitPlayer(Player target)=>Eligible(out var p) && p!.whoAmI==target.whoAmI;
    public override bool? Colliding(Rectangle a,Rectangle b)=>Eligible(out _);
    public override void ModifyHitPlayer(Player target,ref Player.HurtModifiers modifiers)
        => modifiers.SetMaxDamage(AzureRules.NativeFinalDamageLimit(Budget));
    public override void OnHitPlayer(Player target,Player.HurtInfo info)
    {
        spent=true;Projectile.hostile=false;
        if(Main.netMode!=NetmodeID.Server && Main.myPlayer==target.whoAmI)
            AzurePackets.Log($"event=ChorusNativeImpact fight={Plan.Fight} kind={Plan.Kind} member={Member} slot={target.whoAmI} intended_damage={Budget} native_source_damage={Projectile.damage} final_damage={info.Damage}");
    }
    public override void AI()
    {
        Projectile.damage=AzureRules.NativeSourceDamage(Budget);Projectile.hostile=Eligible(out var player);if(player is not null)Projectile.Center=player.Center;
        if(AzureChorus.TryGirl(Plan,out var g))
        {Projectile.timeLeft=Math.Max(2,Plan.Fire+AzureChorusRules.ImpactTicks+2-(int)g!.VisualAge);if(Main.netMode!=NetmodeID.MultiplayerClient && g.VisualAge>=Plan.Fire+AzureChorusRules.ImpactTicks)Projectile.Kill();}
        else if(Main.netMode!=NetmodeID.MultiplayerClient)Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter w){Plan.Write(w);if(Plan.Fight==Guid.Empty)return;w.Write(Member);w.Write(Budget);}
    public override void ReceiveExtraAI(BinaryReader r)
    {
        var next=AzureChorusPlan.Read(r);if(next is not {} p)return;byte member=r.ReadByte();int budget=r.ReadInt32();
        if(member>=8 || (p.Members & (1<<member))==0 || budget is < 1 or > AzureChorusRules.Damage)throw new InvalidDataException("azure.impact");
        if(Main.netMode==NetmodeID.Server || Plan.Fight!=Guid.Empty && (Plan!=p || Member!=member || Budget!=budget))return;
        Plan=p;Member=member;Budget=budget;
    }
}

internal sealed class AzureChorusDirector
{
    private AzureChorus? marker;
    internal void Tick(AzureState state,AzureBoss girl,bool enabled)
    {
        if(!enabled)return;
        int phrase=AzureRules.Phrase(state.Age,state.UnlockAt),clock=AzureRules.Clock(state.Age,state.UnlockAt);
        if(AzureRules.ChorusPhrase(phrase) && clock==24)
        {
            byte mask=0;for(int i=0;i<state.Members.Length;i++)
                if(phrase==2 || !state.Members[i].Out && !state.Members[i].Recovery.Downed)mask|=(byte)(1<<i);
            if(mask==0)return;
            var plan=new AzureChorusPlan(state.Fight,(short)girl.NPC.whoAmI,phrase==2?AzureChorusKind.Stack:AzureChorusKind.Spread,
                mask,state.Age,state.Age+AzureChorusRules.Warning,state.Age+AzureChorusRules.Warning+84,new(state.Field.CenterX,state.Field.CenterY+175));
            int slot=Projectile.NewProjectile(new AzureChorusSource(plan),new(plan.Center.X,plan.Center.Y),Vector2.Zero,ModContent.ProjectileType<AzureChorus>(),0,0,Main.myPlayer);
            if(slot>=Main.maxProjectiles)throw new InvalidOperationException("azure.chorus_capacity");
            marker=(AzureChorus)Main.projectile[slot].ModProjectile;marker.Projectile.netUpdate=true;
            AzurePackets.Log($"event=ChorusCalled fight={state.Fight} kind={plan.Kind} born={plan.Born} fire={plan.Fire} members={mask}");
        }
        if(marker is not {} m)return;
        if(state.Age>=m.Plan.End){marker=null;return;}
        if(!m.Projectile.active || m.Projectile.ModProjectile!=m)throw new InvalidOperationException("azure.chorus_missing");
        if(m.Resolved || state.Age<m.Plan.Fire)return;
        var positions=new Point[state.Members.Length];byte living=0;
        for(int i=0;i<positions.Length;i++)
        {
            var member=state.Members[i];var p=Main.player[member.Slot];positions[i]=new(p.Center.X,p.Center.Y);
            if(!member.Out && !member.Recovery.Downed && p.active && !p.dead && !p.ghost && p.GetModPlayer<AzureConnection>().Token==member.Connection)living|=(byte)(1<<i);
        }
        var damage=AzureChorusRules.Resolve(m.Plan.Kind,m.Plan.Center,positions,m.Plan.Members,living);
        m.Positions=positions;m.Resolved=true;
        for(byte i=0;i<damage.Length;i++)if(damage[i]>0 && state.Age<m.Plan.Fire+AzureChorusRules.ImpactTicks)
        {
            m.FailedMask|=(byte)(1<<i);
            int slot=Projectile.NewProjectile(new AzureStrikeSource(m.Plan,i,damage[i]),Main.player[state.Members[i].Slot].Center,
                Vector2.Zero,ModContent.ProjectileType<AzureChorusStrike>(),AzureRules.NativeSourceDamage(damage[i]),0,Main.myPlayer);
            if(slot>=Main.maxProjectiles)throw new InvalidOperationException("azure.impact_capacity");
            Main.projectile[slot].netUpdate=true;
        }
        m.Projectile.netUpdate=true;
        AzurePackets.Log($"event=ChorusResolved fight={state.Fight} kind={m.Plan.Kind} age={state.Age} living={living} failed={m.FailedMask} intended_damage={string.Join(",",damage)} native_source_damage={string.Join(",",Array.ConvertAll(damage,AzureRules.NativeSourceDamage))}");
        for(int i=0;i<positions.Length;i++)if((living & m.Plan.Members & (1<<i))!=0)
        {
            float nearest=float.PositiveInfinity;
            for(int j=0;j<positions.Length;j++)if(i!=j && (living & m.Plan.Members & (1<<j))!=0)
                nearest=Math.Min(nearest,Point.Distance(positions[i],positions[j]));
            AzurePackets.Log(FormattableString.Invariant($"event=ChorusMember fight={state.Fight} kind={m.Plan.Kind} age={state.Age} slot={state.Members[i].Slot} gather_distance={Point.Distance(positions[i],m.Plan.Center):F1} nearest_peer={(float.IsPositiveInfinity(nearest)?-1:nearest):F1} intended_damage={damage[i]} native_source_damage={AzureRules.NativeSourceDamage(damage[i])}"));
        }
    }
    internal void Clear(Guid fight)
    {
        marker=null;
        foreach(Projectile p in Main.ActiveProjectiles)
            if(p.ModProjectile is AzureChorus a && a.Plan.Fight==fight || p.ModProjectile is AzureChorusStrike s && s.Plan.Fight==fight)p.Kill();
    }
}
