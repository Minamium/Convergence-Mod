using System;
using Convergence.Content.Encounters.AzureCathedral;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.AzureCathedral;

// All presentation follows the accepted encounter clock; no camera/input flags
// are left behind and none of these decorative primitives have hitboxes.
internal static class AzureCeremony
{
    internal static void Girl(SpriteBatch batch,AzureBoss g,Vector2 screen)
    {
        float age=AzureVisuals.RenderAge(g),t=g.State.MusicStart<0?-1:age-g.State.MusicStart;
        if(g.State.Phase>=AzurePhase.Fury)return;
        float consume=g.State.Phase==AzurePhase.Devouring?age-g.State.PhaseAt:-1;
        bool sealedGirl=t<AzureRules.IceBreak;
        float alpha=consume>=0?1-AzureRules.Ease((consume-AzureRules.DevourContact+2)/12):g.State.GirlLife<=0?.42f:1;
        if(g.State.EndAt>=0)alpha*=1-AzureRules.Ease((age-g.State.EndAt)/150);
        Vector2 center=g.NPC.Center+new Vector2(0,AzureVisuals.Reduced?0:MathF.Sin(age*.022f)*2.5f);
        float fracture=AzureRules.Ease((t-100)/80),tilt=MathF.Sin(age*.009f)*.045f;
        if(sealedGirl)AzureMaterials.Effect(batch,"IcePass",center,new(98,140),tilt,age,new(.62f,fracture,0,0));
        int frame=sealedGirl?(t<80?0:t<158?4:6):t<280?3:t<380?4:t<670?5:((int)age%230<8?2:1);
        if(consume>=0)frame=consume<70?4:5;
        float release=0;bool cutting=false;float charge=0;
        if(g.State.Live)
            foreach(Projectile p in Main.ActiveProjectiles)
                if(p.ModProjectile is AzureAttack a && a.Plan.Fight==g.State.Fight && a.Plan.Kind!=AzureAttackKind.FrostBolt && age<a.Plan.Fire+22 && age>=a.Plan.Born)
                {release=age-a.Plan.Fire;frame=release< -22?4:release<0?5:6;cutting=a.Plan.Kind==AzureAttackKind.GlacialCut;charge=AzureRules.Ease((age-a.Plan.Born)/Math.Max(1,a.Plan.Fire-a.Plan.Born));break;}
        var art=ModContent.Request<Texture2D>("Convergence/Assets/Textures/AzureCathedral/Liora").Value;
        var src=new Rectangle(frame%4*48,frame/4*64,48,64);
        float lean=frame==6?MathF.Exp(-Math.Max(0,release)/9)*-.045f:MathF.Sin(age*.021f)*.008f;
        // Logical pixel export, drawn at native density rather than shrinking a
        // 128px illustration. Align the bodies across sword/hover atlas cells.
        batch.Draw(art,center-screen,src,(sealedGirl?new Color(181,220,239):Color.White)*alpha,lean,
            new(24,frame>=4?40:35),1f,SpriteEffects.None,0);
        if(cutting)
        {
            var sword=center+new Vector2(-6,release<0?-46:-20);
            float glow=release<0?charge*.6f:MathF.Exp(-release/6);
            Bloom(batch,sword,45+charge*35,glow);
        }
        if(sealedGirl)AzureMaterials.Effect(batch,"IcePass",center,new(98,140),tilt,age,new(.28f,fracture,0,0));
        float broken=t-AzureRules.IceBreak;
        if(broken>=0 && broken<125)
        {
            float burst=1-MathF.Exp(-broken/13),fade=1-AzureRules.Ease((broken-45)/80);
            for(int i=0;i<10;i++)
            {
                float a=i*2.399963f;Vector2 d=a.ToRotationVector2();
                Vector2 pos=center+d*(26+burst*(60+i*6))+new Vector2(0,broken*broken*.006f);
                AzureMaterials.Effect(batch,"IcePass",pos,new(18+i%3*8,34+i%4*7),a+broken*.005f*(i%2==0?1:-1),age,new(fade,.8f,0,0));
            }
            if(broken<18)AzureMaterials.Shard(batch,center-new Vector2(62,66),center+new Vector2(72,65),5,age,1-broken/18);
        }
    }
    internal static void Stage(SpriteBatch batch,AzureState state,float age)
    {
        if(state.MusicStart<0)return;
        float t=age-state.MusicStart;var f=state.Field;Vector2 center=new(f.CenterX,f.CenterY);
        if(state.Phase==AzurePhase.Devouring)
        {
            float c=age-state.PhaseAt;
            float e=AzureRules.Ease(c/100)*(1-AzureRules.Ease((c-AzureRules.DevourContact)/18));
            Vector2 sword=center+new Vector2(-6,-48);
            Bloom(batch,sword,80+MathF.Pow(.5f+.5f*MathF.Sin(c*.12f),5)*100,.62f*e);
            AzureEnergy.Add(sword,-Vector2.UnitY,1000,5*e,age,state.PhaseAt+110,state.PhaseAt+AzureRules.DevourContact,e,AzureVisuals.Reduced,state.PhaseAt,true);
            float shock=AzureRules.Ease((c-AzureRules.DevourContact)/3)*(1-AzureRules.Ease((c-AzureRules.DevourContact-8)/40));
            if(shock>0 && !AzureVisuals.Reduced)
                for(int i=0;i<18;i++)
                {
                    Vector2 d=(i*2.399963f).ToRotationVector2();float distance=(c-AzureRules.DevourContact)*12;
                    AzureMaterials.Shard(batch,center+d*distance,center+d*(distance+35+i%4*16),3,age,shock*.7f);
                }
        }
        if(state.Phase==AzurePhase.Melting)
        {
            float c=age-state.EndAt;
            float e=AzureRules.Ease((c-AzureRules.MeltContact)/30)*(1-AzureRules.Ease((c-AzureRules.MeltEnding+120)/120));
            AzureMaterials.Effect(batch,"FrostPass",center+new Vector2(0,190),new(1000,410),0,age,new(e*.65f,0,0,4));
            if(!AzureVisuals.Reduced)
                for(int i=0;i<24;i++)
                {
                    float u=(c*.008f+i*.071f)%1;
                    Vector2 from=center+new Vector2(MathF.Sin(i*6.31f)*430,40+u*410);
                    AzureMaterials.Shard(batch,from,from+new Vector2(0,12+u*22),2+u*3,age,e*(1-u)*.55f);
                }
        }
        if(t>=AzureRules.SwordLight && t<760)
        {
            float e=AzureRules.Ease((t-AzureRules.SwordLight)/35)*(1-AzureRules.Ease((t-640)/120));
            Vector2 sword=center+new Vector2(-6,-48);
            AzureEnergy.Add(sword,-Vector2.UnitY,1000,7*e,age,state.MusicStart+AzureRules.SwordLight,
                state.MusicStart+760,e,AzureVisuals.Reduced,state.MusicStart+AzureRules.SwordLight-100,true);
            Bloom(batch,sword,85+45*MathF.Sin(t*.09f)*MathF.Sin(t*.09f),.65f*e);
        }
        float open=AzureRules.Ease((t-540)/70)*(1-AzureRules.Ease((t-790)/145));
        if(open>0)
        {
            Vector2 portal=center+new Vector2(1050,-280);
            AzureMaterials.Effect(batch,"RiftPass",portal,new Vector2(500,180)*open,MathHelper.PiOver2+.10f,age,new(open,open,0,0));
            if(!AzureVisuals.Reduced)
                for(int i=0;i<12;i++)
                {
                    float a=i*2.39996f,flow=(t*.009f+i*.21f)%1;
                    Vector2 offset=new(MathF.Cos(a)*flow*165,MathF.Sin(a)*flow*260);
                    AzureMaterials.Shard(batch,portal+offset,portal+offset*1.07f,2,age,open*(1-flow)*.65f);
                }
        }
    }
    internal static void Silhouette(SpriteBatch batch,AzureBoss girl,Matrix worldToViewport,float age)
    {
        float c=age-girl.State.PhaseAt;
        if(c>=AzureRules.DevourContact+7)return;
        var art=ModContent.Request<Texture2D>("Convergence/Assets/Textures/AzureCathedral/Liora").Value;
        Vector2 center=Vector2.Transform(girl.NPC.Center,worldToViewport);
        float sx=new Vector2(worldToViewport.M11,worldToViewport.M12).Length();
        float sy=new Vector2(worldToViewport.M21,worldToViewport.M22).Length();
        float fade=1-AzureRules.Ease((c-AzureRules.DevourContact)/7);
        batch.Draw(art,center,new Rectangle(48,64,48,64),new Color(2,6,10)*fade,0,new(24,40),new Vector2(sx,sy),SpriteEffects.None,0);
    }
    internal static void Bloom(SpriteBatch batch,Vector2 center,float diameter,float alpha)
    {
        var art=MiscTexturesRegistry.BloomCircleSmall.Value;
        batch.Draw(art,center-Main.screenPosition,null,new Color(115,226,255,0)*alpha,0,art.Size()*.5f,diameter/art.Width,SpriteEffects.None,0);
    }
}
