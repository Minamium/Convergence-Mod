using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Accepted epoch -> retreat, dissolution, reconstitution. All streams are
// cosmetic; native HP transfer and actor retirement remain in Runtime.
internal static class ScarletInvocationScene
{
    internal static void Draw(SpriteBatch batch, in CrimsonState state, float age, float alpha)
    {
        if (state.Phase == 0 || Main.dedServ) return;
        float t = age - state.PhaseStart;
        Vector2 center = new(state.Field.CenterX, state.Field.CenterY);
        if (state.Phase < 3)
        {
            if (t >= CrimsonEnsemble.Transition(state.Phase)) return;
            var gate = CrimsonChoreography.SummoningGate(state.Field); center = new(gate.X, gate.Y);
            float load = CrimsonInvocation.Ease(t / 64);
            float opacity = CrimsonInvocation.Ease(t / 22) * (1-CrimsonInvocation.Ease((t-190)/50));
            float radius = 140 + load * 260;
            ScarletSorcery.Seal(batch,center,radius,.82f,-.16f,age,load,opacity*alpha);
            ScarletSorcery.Seal(batch,center,radius*.77f,.82f,.24f,-age*.6f,load,opacity*alpha*.65f,false,3);
            float gathered = CrimsonInvocation.Ease((t-50)/60) * (1-CrimsonInvocation.Ease((t-132)/65));
            Streams(batch,center,age,gathered*alpha,190,false);
            ScarletClusters.Orb(batch,center,30+gathered*100,age,gathered*alpha,load,0);
            Flash(batch,center,t-CrimsonEnsemble.ActRelease,alpha*.6f);
            return;
        }
        float open = CrimsonInvocation.Ease(t / 60), absorb = CrimsonEnsemble.Absorption(t);
        float manifest = CrimsonEnsemble.Emergence(t,true);
        float pressure = CrimsonEnsemble.BloodPressure(t);
        float gateAlpha = alpha * (1-CrimsonInvocation.Ease((t-460)/80));
        ScarletSorcery.Seal(batch,center,350+open*150,.92f,-.14f,age*.6f,open,gateAlpha*.80f);
        ScarletSorcery.Seal(batch,center,312+open*98,.92f,.13f,-age*.45f,open,gateAlpha*.50f,false,5);
        // Rebuild each hidden apparition in its child seal before extraction.
        for (int i=0;i<3;i++)
        {
            var point=CrimsonEnsemble.Binding(state.Field,i); Vector2 at=new(point.X,point.Y);
            float reveal=CrimsonEnsemble.ChildReveal(t,i), retained=1-absorb;
            ScarletSorcery.Seal(batch,at,177,.94f,i*MathF.Tau/3,age,reveal,alpha*reveal*retained,false,i+1);
            CrimsonRig.DrawApparition(batch,i,at,age,reveal*alpha,absorb);
            Vector2 normal = new Vector2(-(center-at).Y,(center-at).X).SafeNormalize(Vector2.UnitX);
            for (int vein=0;vein<(CrimsonVisuals.Reduced?2:5);vein++)
            {
                float offset=(vein-2)*28;
                Vector2 source=at+normal*offset+new Vector2(MathF.Sin(age*.05f+vein+i)*9,0);
                ScarletSorcery.Transfusion(batch,source,center+normal*offset*.18f,
                    22+pressure*24,age+vein*13,pressure*alpha*(.6f+retained*.4f),i*5+vein);
            }
        }
        float swallowed=CrimsonEnsemble.ConductorAbsorption(t);
        if(swallowed>0 && swallowed<1)
            Streams(batch,center,age,MathF.Sin(swallowed*MathF.PI)*alpha,80,false);
        float coreRadius=MathHelper.Lerp(56,176,CrimsonInvocation.Ease((t-80)/310));
        ScarletClusters.Orb(batch,center+new Vector2(0,-20),coreRadius,age,alpha*open*(1-manifest),open,pressure*.8f);
        Streams(batch,center,age,pressure*alpha,230,false);
        if(manifest>0) CrimsonRig.DrawEnsemble(batch,center,age,manifest,alpha);
        Flash(batch,center,t-CrimsonEnsemble.FinalRelease,alpha);
    }
    internal static void Victory(SpriteBatch batch, in CrimsonState state, float age, float elapsed)
    {
        Vector2 center=new(state.Field.CenterX,state.Field.CenterY);
        float melt=CrimsonEnsemble.VictoryMelt(elapsed);
        float tail=1-CrimsonInvocation.Ease((elapsed-115)/35);
        CrimsonRig.DrawEnsemble(batch,center,age,1,tail,melt,melt);
        Streams(batch,center,age,CrimsonInvocation.Ease(elapsed/12)*tail,260+140*melt,true);
        Flash(batch,center,elapsed-92,.65f);
    }
    private static void Streams(SpriteBatch batch,Vector2 center,float age,float alpha,float radius,bool outward)
    {
        if(alpha<.001f)return;
        int count=CrimsonVisuals.Reduced?5:14;
        for(int i=0;i<count;i++)
        {
            float angle=i*2.399963f+MathF.Sin(i*4.7f)*.16f;
            Vector2 rim=center+new Vector2(radius*(.7f+.3f*MathF.Sin(i*3.1f)),0).RotatedBy(angle);
            Vector2 body=center+(rim-center)*.24f;
            if(outward) rim.Y+=150+MathF.Sin(age*.027f+i)*25;
            ScarletSorcery.Transfusion(batch,outward?body:rim,outward?rim:body,20+alpha*24,age+i*7,alpha*.8f,i);
        }
    }
    private static void Flash(SpriteBatch batch,Vector2 center,float elapsed,float alpha)
    {
        if(elapsed<0 || elapsed>46)return;
        float light=CrimsonInvocation.Ease(elapsed/3)*(1-CrimsonInvocation.Ease((elapsed-3)/43));
        light*=alpha*(CrimsonVisuals.Reduced?.18f:1);
        var bloom=MiscTexturesRegistry.BloomCircleSmall.Value;
        batch.Draw(bloom,center-Main.screenPosition,null,new Color(255,35,65,0)*light,0,bloom.Size()*.5f,(700+elapsed*9)/bloom.Width,SpriteEffects.None,0);
        batch.Draw(bloom,center-Main.screenPosition,null,new Color(255,193,189,0)*light*.75f,-.2f,bloom.Size()*.5f,new Vector2(1050,26)/bloom.Width,SpriteEffects.None,0);
    }
}
