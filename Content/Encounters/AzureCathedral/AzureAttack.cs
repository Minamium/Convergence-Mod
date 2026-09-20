#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.AzureCathedral;

// Receiving-player native hostile damage, explicitly scoped in ADR-0028.
// No manual Hurt loop, no extra health subtraction, no client phase decisions.
public sealed class AzureAttack : ModProjectile
{
    internal AzureAttackPlan Plan;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathLaser;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true; Projectile.timeLeft = 600;
    }
    public override void OnSpawn(IEntitySource source) { if (source is AzureAttackSource a) Plan = a.Plan; }
    public override bool ShouldUpdatePosition() => Plan.Kind==AzureAttackKind.FrostBolt;
    private bool TryEmitter(out NPC? emitter)
    {
        emitter=Plan.Emitter>=0 && Plan.Emitter<Main.maxNPCs?Main.npc[Plan.Emitter]:null;
        return emitter is {active:true,ModNPC:AzureWorm w} && w.Fight==Plan.Fight;
    }
    private Vector2 FrozenVolleyAim => new Vector2(Plan.X,Plan.Y)+Plan.Angle.ToRotationVector2()*Plan.Length;
    internal bool TryGirl(out AzureBoss? girl)
    {
        girl = Plan.Girl >= 0 && Plan.Girl < Main.maxNPCs && Main.npc[Plan.Girl].active ? Main.npc[Plan.Girl].ModNPC as AzureBoss : null;
        return girl is not null && Plan.Fight != Guid.Empty && girl.State.Fight == Plan.Fight && girl.Fresh && girl.State.Live
            && Plan.Born>=girl.State.AttackEpoch
            && (Plan.Kind == AzureAttackKind.FrostBolt ? girl.State.WormLife > 0 : girl.State.GirlLife > 0);
    }
    internal bool Geometry(AzureBoss girl, float age, bool forecast, out Vector2 from, out Vector2 to, out float radius)
    {
        Vector2 d = Plan.Angle.ToRotationVector2(); from = new(Plan.X, Plan.Y); radius = Plan.Width;
        if (Plan.Kind == AzureAttackKind.MouthBeam)
        {
            // Fixed Liora emitter, announced initial angle, smooth 120-degree sweep.
            // No moving worm mouth can instantly drag a beam across a participant.
            float angle=Plan.Angle+2.094395f*AzureRules.Ease((age-Plan.Fire)/(Plan.End-Plan.Fire));
            d = angle.ToRotationVector2(); from = girl.NPC.Center + new Vector2(0,-12) + d * 26;
            radius *= forecast ? 1 : AzureRules.Envelope(age - Plan.Fire, Plan.End - Plan.Fire);
            if (girl.State.Field.ClipAxis(from.X, from.Y, d.X, d.Y, out float first, out float last))
            { to = from + d * Math.Max(0, last); from += d * Math.Max(0, first); return last > 0; }
            to = from; return false;
        }
        if(Plan.Kind==AzureAttackKind.GlacialCut)
        {
            if(!girl.State.Field.ClipAxis(from.X,from.Y,d.X,d.Y,out float first,out float last))
            {to=from;return false;}
            Vector2 end=from+d*last;from+=d*first;
            to=forecast?end:Vector2.Lerp(from,end,AzureRules.CutReach(age-Plan.Fire));
            radius*=forecast?1:AzureRules.CutWidth(age-Plan.Fire);return radius>0;
        }
        if(Plan.Kind==AzureAttackKind.FrostBolt)
        {
            if(forecast)
            {
                if(!TryEmitter(out var emitter)){to=from;return false;}
                from=emitter!.Center;d=(FrozenVolleyAim-from).SafeNormalize(d);to=from+d*4000;return true;
            }
            d=Projectile.velocity.SafeNormalize(d);to=Projectile.Center+d*18;
            from=Projectile.Center-d*30;radius*=AzureRules.Envelope(age-Plan.Fire,Plan.End-Plan.Fire);return true;
        }
        if (forecast) { to = from + d * Plan.Length; return true; }
        float travel = Math.Max(0, age - Plan.Fire) * (Plan.Kind == AzureAttackKind.GlassRain ? 17 : 21);
        to = from + d * Math.Min(Plan.Length, travel);
        from += d * Math.Clamp(travel - 100, 0, Plan.Length);
        radius *= AzureRules.Envelope(age - Plan.Fire, Plan.End - Plan.Fire);
        return travel < Plan.Length + 100;
    }
    public override bool? CanDamage() => TryGirl(out var g) && g!.VisualAge >= Plan.Fire && g.VisualAge < Plan.End ? null : false;
    public override bool CanHitPlayer(Player p) => TryGirl(out var g) && g!.State.Contains(p.whoAmI);
    public override bool? CanHitNPC(NPC target) => false;
    public override bool? Colliding(Rectangle projectile, Rectangle target)
    {
        if (!TryGirl(out var g) || g!.VisualAge < Plan.Fire || g.VisualAge >= Plan.End
            || !Geometry(g, g.VisualAge, false, out var a, out var b, out float radius)) return false;
        float point = 0;
        if(Plan.Kind==AzureAttackKind.MouthBeam)
        {
            var d=(b-a).SafeNormalize(Vector2.UnitX);float length=Vector2.Distance(a,b);
            for(float start=0;start<Math.Min(260,length);start+=20)
            {
                float end=Math.Min(start+20,length),r=AzureRules.ThroatRadius(start,radius);
                if(Collision.CheckAABBvLineCollision(target.TopLeft(),target.Size(),a+d*start,a+d*end,r*2,ref point))return true;
            }
            return length>260 && Collision.CheckAABBvLineCollision(target.TopLeft(),target.Size(),a+d*260,b,radius*2,ref point);
        }
        return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), a, b, radius * 2, ref point);
    }
    public override void AI()
    {
        bool valid = TryGirl(out var g);
        Projectile.hostile = valid && g!.VisualAge >= Plan.Fire && g.VisualAge < Plan.End;
        if (g is not null && valid)
        {
            int residue=Plan.Kind==AzureAttackKind.GlacialCut?AzureRules.CutResidue:0;
            Projectile.timeLeft = Math.Max(2, Plan.End + residue + 16 - (int)g.VisualAge);
            if(Plan.Kind==AzureAttackKind.FrostBolt)
            {
                float t=g.VisualAge-Plan.Fire;
                if(t<0)
                {
                    if(TryEmitter(out var emitter))Projectile.Center=emitter!.Center;
                    else if(Main.netMode!=NetmodeID.MultiplayerClient){Projectile.Kill();return;}
                    Projectile.velocity=Vector2.Zero;
                }
                else
                {
                    if(Projectile.localAI[0]==0)
                    {
                        Projectile.localAI[0]=1;
                        if(Projectile.velocity.LengthSquared()<1)
                        {
                            if(TryEmitter(out var emitter))Projectile.Center=emitter!.Center;
                            Projectile.velocity=(FrozenVolleyAim-Projectile.Center).SafeNormalize(Plan.Angle.ToRotationVector2())*24;
                        }
                        if(Main.netMode!=NetmodeID.MultiplayerClient)Projectile.netUpdate=true;
                    }
                    if(Main.netMode!=NetmodeID.MultiplayerClient && t<45 && g.State.Contains(Plan.Target))
                    {
                        var p=Main.player[Plan.Target];
                        Projectile.ai[0]=p.Center.X;Projectile.ai[1]=p.Center.Y;Projectile.ai[2]=1;
                        if((int)t%6==0)Projectile.netUpdate=true;
                    }
                    if(t<45 && Projectile.ai[2]==1)
                    {
                        // Limited early guidance, then ballistic: dodging is not undone by a U-turn.
                        var delta=new Vector2(Projectile.ai[0],Projectile.ai[1])-Projectile.Center;
                        var v=AzureFlight.Steer(new(Projectile.velocity.X,Projectile.velocity.Y),new(delta.X,delta.Y),26,.012f);
                        Projectile.velocity=new(v.X,v.Y);
                    }
                }
            }
            else if (Geometry(g, g.VisualAge, g.VisualAge < Plan.Fire, out var a, out var b, out _)) Projectile.Center = (a + b) * .5f;
            if (Main.netMode != NetmodeID.MultiplayerClient && g.VisualAge >= Plan.End + residue + 16) Projectile.Kill();
        }
        else if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter writer) => Plan.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var parsed = AzureAttackPlan.Read(reader);
        if (parsed is { } next && Main.netMode != NetmodeID.Server && (Plan.Fight == Guid.Empty || Plan == next)) Plan = next;
    }
}
