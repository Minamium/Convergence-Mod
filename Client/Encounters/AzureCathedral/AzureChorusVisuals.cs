using System;
using Convergence.Content.Encounters.AzureCathedral;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.AzureCathedral;

internal static class AzureChorusVisuals
{
    internal static void Draw(SpriteBatch batch,AzureBoss girl,AzureChorus marker,float age)
    {
        var p=marker.Plan;if(age<p.Born || age>=p.End)return;
        float progress=Math.Clamp((age-p.Born)/(p.Fire-p.Born),0,1),after=age-p.Fire;
        float remain=1-AzureRules.Ease((after-36)/48),appear=AzureRules.Ease((age-p.Born)/24);
        bool spread=p.Kind==AzureChorusKind.Spread;
        if(!spread && after<0)
        {
            var center=new Vector2(p.Center.X,p.Center.Y);
            Circle(batch,center,AzureChorusRules.StackRadius,age,progress,appear);
            Arrows(batch,center,AzureChorusRules.StackRadius,age,false,appear);
        }
        for(int i=0;i<girl.State.Members.Length;i++)
        {
            var member=girl.State.Members[i];if((p.Members & (1<<i))==0 || member.Out && !marker.Resolved)continue;
            var player=Main.player[member.Slot];if(!player.active)continue;
            var center=marker.Resolved && i<marker.Positions.Length?new Vector2(marker.Positions[i].X,marker.Positions[i].Y):player.Center;
            bool failed=marker.Resolved && (marker.FailedMask & (1<<i))!=0;
            if(spread)
            {
                if(after<0){Circle(batch,center,AzureChorusRules.SpreadRadius,age,progress,appear);Arrows(batch,center,AzureChorusRules.SpreadRadius,age,true,appear);}
                Vector2 foot=center+new Vector2(0,25);
                float size=(.2f+.8f*AzureRules.Ease(progress))*appear;
                AzureMaterials.Effect(batch,"RiftPass",foot,new Vector2(120,42)*size,.04f*MathF.Sin(age*.07f),age,new(remain,size,0,0));
                if(after>=0 && marker.Resolved)
                {
                    float thrust=1-MathF.Exp(-after/2.4f),fade=1-AzureRules.Ease((after-14)/40);
                    if(failed)
                    {
                        var tip=foot-new Vector2(0,190*thrust);
                        AzureMaterials.Shard(batch,foot+new Vector2(0,24),tip,13*fade,age,fade);
                        AzureCeremony.Bloom(batch,center,135,fade*.9f);
                    }
                    else
                    {
                        for(int k=0;k<6;k++)
                        {
                            Vector2 d=(k*MathHelper.TwoPi/6).ToRotationVector2();
                            var pos=foot+d*(12+after*1.8f);
                            AzureMaterials.Shard(batch,pos,pos+d*12,2,age,fade*.75f);
                        }
                    }
                }
            }
            else
            {
                // Paired, faceted ice jaws surround each party member. Their
                // broad silhouette is distinct from the one true gathering ring.
                float squeeze=failed && after>=0?AzureRules.Ease(after/7):0;
                float fall=after>=0 && !failed?after*after*.045f:0;
                for(int side=-1;side<=1;side+=2)for(int k=0;k<3;k++)
                {
                    float grow=AzureRules.Ease((progress-k*.16f)/.55f);
                    float gap=(54+k*12)*(1-squeeze)+8*squeeze;
                    Vector2 pos=center+new Vector2(side*gap,(k-1)*23+fall);
                    float rotation=side*(.17f+k*.11f)+(after>0 && !failed?after*.007f*side:0);
                    AzureMaterials.Effect(batch,"IcePass",pos,new Vector2(19+k*4,55+k*12)*grow,rotation,age,new(appear*remain,.25f+progress*.5f,0,0));
                    if(!AzureVisuals.Reduced && progress>.5f)
                    {
                        float glint=MathF.Pow(.5f+.5f*MathF.Sin(age*.12f+k*5+i),12);
                        AzureCeremony.Bloom(batch,pos,18,glint*.55f*remain);
                    }
                }
                if(failed && after>=0 && after<35)
                {
                    AzureCeremony.Bloom(batch,center,140,MathF.Exp(-after/12)*.9f);
                    for(int k=0;k<8;k++)
                    {
                        Vector2 d=(k*2.399963f).ToRotationVector2(),pos=center+d*after*4;
                        AzureMaterials.Shard(batch,pos,pos+d*18,3,age,1-after/35);
                    }
                }
            }
        }
    }
    private static void Circle(SpriteBatch batch,Vector2 center,float radius,float age,float progress,float alpha)
        =>AzureMaterials.Effect(batch,"CirclePass",center,new Vector2(radius*2/.94f),0,age,new(alpha,progress,0,0));
    private static void Arrows(SpriteBatch batch,Vector2 center,float radius,float age,bool outward,float alpha)
    {
        for(int k=0;k<4;k++)
        {
            var d=(MathHelper.PiOver4+k*MathHelper.PiOver2).ToRotationVector2();var n=new Vector2(-d.Y,d.X);
            float pulse=5*MathF.Sin(age*.08f);
            var at=center+d*(radius+24+(outward?pulse:-pulse));var tip=at+d*(outward?9:-9);
            var color=new Color(174,236,255)*alpha;
            AzureVisuals.Stroke(batch,at+n*7,tip,color,2);AzureVisuals.Stroke(batch,at-n*7,tip,color,2);
        }
    }
}
