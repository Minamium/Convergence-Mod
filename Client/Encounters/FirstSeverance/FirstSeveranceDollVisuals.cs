#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Authored porcelain/dress/hair, procedural joints and suspension. Owns no actor
// and never supplies a collision coordinate; CoreCenter remains the only target.
internal sealed class FirstSeveranceDollVisuals
{
    internal const string ArtRoot = "Convergence/Assets/Textures/NPCs/DollTheater/";
    private Asset<Texture2D>? atlas, harness, coffin, attendant, shell;
    private Asset<Texture2D>? handFrames, bodyFrames;
    private float bodyCast;
    private readonly FirstSeveranceDollPose pose = new();
    private readonly FirstSeveranceAttackAccents captureAccents = new();
    private readonly FirstSeveranceDollSurface surface = new();
    private static Vector2 V(System.Numerics.Vector2 p) => new(p.X,p.Y);

    // Caller is the owned world-space AlphaBlend/LinearClamp presentation pass.
    // Isolate pixel sampling; do not change any engine-global UI or zoom setting.
    internal readonly struct PixelPass : IDisposable
    {
        private readonly SpriteBatch batch;
        internal PixelPass(SpriteBatch batch)
        {
            this.batch=batch; batch.End();
            batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,
                DepthStencilState.None,Main.Rasterizer,null,Main.GameViewMatrix.TransformationMatrix);
        }
        public void Dispose()
        {
            batch.End();
            batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                DepthStencilState.None,Main.Rasterizer,null,Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal void DrawBody(SpriteBatch batch, Vector2 center, float opacity, float seconds, float cast, float kick,
        float unfurl, float emergence, float pry, bool handsOnly, bool hideHands, float depth,
        float breakup,float consumption,Vector2 sink,bool reduced)
    {
        if (opacity <= .001f || depth <= .001f) return;
        Ensure();
        bodyCast=cast;
        pose.Body(seconds,cast,kick,unfurl,emergence,reduced,pry);
        float scale=depth; // Visible P1 arm keeps the same size and joint endpoints while the casing peels.
        // Continuous subpixel rotation for Boss parts. Only the small NPC uses
        // nearest-neighbor sampling; it must not quantize the entire giant rig.
        if (!handsOnly)
            DrawCords(batch,center,scale,opacity*(1-consumption),seconds,reduced);
        foreach (var part in pose.Sprites)
        {
            if (handsOnly && part.Cell!=5 || hideHands && part.Cell==5) continue;
            DrawPart(batch,part,center,scale,Color.White*opacity,breakup,consumption,sink,seconds*60,reduced);
        }
        if (!handsOnly)
        {
            // Remnant plates hang at the dress hem, proof of a violent extraction.
            for (int side=-1;side<=1;side+=2)
            {
                Vector2 hip=center+new Vector2(side*122,200).RotatedBy(pose.Tilt)*scale;
                batch.Draw(coffin!.Value,hip-Main.screenPosition,new Rectangle(side<0?77:136,174,40,54),
                    new Color(207,202,196)*opacity*(1-breakup)*(1-consumption),side*.26f+pose.Tilt,
                    new Vector2(20,0),1.1f*scale,SpriteEffects.None,0);
            }
        }
    }

    internal void DrawEncased(SpriteBatch batch,Vector2 center,double tick,float opacity,float pressure,bool reduced)
    {
        Ensure();
        float seconds=(float)(tick%216000)/60f;
        pose.Encased(seconds,pressure,reduced);
        DrawCords(batch,center,1,opacity,seconds,reduced);
        foreach(var part in pose.Sprites) DrawPart(batch,part,center,1,Color.White*opacity,0,0,center,seconds*60,reduced);
    }

    internal void DrawRemoteArm(SpriteBatch batch,Vector2 shoulder,Vector2 elbow,Vector2 wrist,
        float handRotation,float brace,float tension,float strike,Color tint,float breakup,float consume,Vector2 sink,float time,bool reduced)
    {
        Ensure();
        // Move the upper rig around the exact wrist. The hazard/emitter point
        // stays authoritative, including the instant-kill closing hands.
        float strength=reduced?.18f:1;
        float seconds=time/60;
        shoulder+=new Vector2(8*FirstSeveranceDollPose.Wave(seconds,.2f),12*FirstSeveranceDollPose.Wave(seconds,.7f))*strength;
        elbow+=new Vector2(15*FirstSeveranceDollPose.Wave(seconds,1.0f),19*FirstSeveranceDollPose.Wave(seconds,1.5f))*strength;
        // Original remote rig/material and pre-doll nominal dimensions. The
        // accepted porcelain arms on the distant body are a different draw path.
        Arm(3,shoulder,elbow,new(185,350)); Arm(4,elbow,wrist,new(150,340));
        int frame=FirstSeveranceDollFrames.Hand(seconds,tension,strike,reduced);
        Vector2 handScale=new Vector2((200+brace*400)/301,(380+brace*80)/655)*2;
        DrawFrame(batch,true,frame,wrist,handScale,handRotation,tint,breakup,consume,sink,time,reduced);
        void Arm(int cell,Vector2 start,Vector2 end,Vector2 size)
        {
            var (top,_)=FirstSeveranceDollPose.Anchors(cell,true);
            var region=FirstSeveranceDollPose.Region(new(cell,default,default,default,0,Harness:true));
            var part=new DollSprite(cell,new(0,0),top,new(size.X/region.Width,size.Y/region.Height),
                (end-start).ToRotation()-MathHelper.PiOver2,Harness:true);
            DrawPart(batch,part,start,1,tint,breakup,consume,sink,time,reduced);
        }
    }

    internal void DrawPreparation(SpriteBatch batch,FirstSeverancePreparationProjection prep,double tick,bool reduced)
    {
        Ensure();
        float age=Math.Clamp((float)((tick-prep.EnteredTick)/FirstSeverancePreparationTimeline.DeploymentTicks),0,1);
        Vector2 ground=new(prep.GroundX,prep.GroundY);
        Vector2 core=ground-new Vector2(0,FirstSeveranceLanceTuning.BossHeightAboveCore);
        float seconds=(float)(tick%216000)/60f;
        // Preparation builds the empty theatre. The real NPC stays on the
        // plinth through Ready; her capture belongs exclusively to SpawnIntro.
        Vector2 shellSize=V(FirstSeveranceShellSurface.Size(tick));
        batch.Draw(shell!.Value,core-Main.screenPosition,null,Color.White,0,
            new Vector2(shell.Value.Width,shell.Value.Height)*.5f,
            shellSize/new Vector2(shell.Value.Width,shell.Value.Height),SpriteEffects.None,0);
        for(int side=-1;side<=1;side+=2)
            Cord(batch,FoundationCoreVisuals.HoistAnchor(ground,side,true,age),core+new Vector2(side*66,-215),
                Window(age,.08,.36),seconds,side,side<0?13:2,reduced);
    }

    internal void DrawCapture(SpriteBatch batch,FirstSeveranceCombatProjection combat,double tick,bool reduced)
    {
        Ensure();
        float age=FirstSeveranceDollCapture.IntroAge(tick,combat.ActionStartedTick,combat.ResolveTick);
        Vector2 ground=new(combat.CoreX,combat.CoreY),core=CoreCenter(combat);
        Vector2 foot=ground-new Vector2(0,54);
        // Only read the existing Core/NPC. Never create a second actor/client
        // participant to play a cinematic, including late snapshots.
        foreach(NPC npc in Main.ActiveNPCs)
            if(npc.ModNPC is FirstSeveranceDollAttendant && FirstSeveranceDollAttendant.FindCore(npc) is { } owner
                && Vector2.DistanceSquared(owner.GroundCenter,ground)<1)
            { foot=FirstSeveranceDollAttendant.StandingFoot(owner); break; }
        float seconds=(float)(tick%216000)/60;
        float tension=Window(age,.05,.24)*(1-Window(age,.41,.60));
        for(int side=-1;side<=1;side+=2)
            Cord(batch,core+new Vector2(side*32,55),foot+new Vector2(side*7,-32),tension*.6f,
                seconds,side,16*(1-tension),reduced);
        using(new PixelPass(batch))
        {
            if(age<.87f)
            {
                for(int i=0;i<FirstSeveranceDollCapture.Count;i++)
                {
                    var part=FirstSeveranceDollCapture.Sample(i,age,new(foot.X,foot.Y),new(core.X,core.Y),reduced);
                    if(part.Opacity<=.001f) continue;
                    var source=new Rectangle(part.X,part.Y,part.Width,part.Height);
                    // A short afterimage is sampled from the very same path;
                    // no second clock or lingering particle after capture.
                    if(!reduced && age>.43f)
                    {
                        for(int trail=3;trail>=1;trail--)
                        {
                            var prior=FirstSeveranceDollCapture.Sample(i,Math.Max(0,age-.006f*trail),new(foot.X,foot.Y),new(core.X,core.Y),false);
                            batch.Draw(attendant!.Value,V(prior.Position)-Main.screenPosition,source,
                                new Color(185,176,161)*(.22f/trail*part.Opacity),prior.Rotation,
                                new Vector2(part.Width,part.Height)*.5f,prior.Scale,SpriteEffects.None,0);
                        }
                    }
                    batch.Draw(attendant!.Value,V(part.Position)-Main.screenPosition,source,Color.White*part.Opacity,
                        part.Rotation,new Vector2(part.Width,part.Height)*.5f,part.Scale,SpriteEffects.None,0);
                }
            }
        }
        float capture=Window(age,.56,.75)*(1-Window(age,.85,.99));
        if(capture>.001f)
            captureAccents.Halo(batch,core,new Vector2(96-Window(age,.7,.88)*72),new Color(226,212,189),capture*(reduced?.25f:.65f));
        float arrival=FirstSeveranceDollCapture.Arrival(age);
        if(arrival>.001f)
        {
            captureAccents.Halo(batch,core,new Vector2(180+arrival*120),new Color(235,224,210),arrival*(reduced?.1f:.46f));
            Ring(batch,core,24+Window(age,.87,.97)*180,new Color(207,196,183)*arrival*(reduced?.2f:.7f),1.8f);
        }
    }

    private void DrawCords(SpriteBatch batch,Vector2 center,float scale,float opacity,float seconds,bool reduced)
    {
        foreach(var cord in pose.Cords)
            Cord(batch,center+new Vector2(cord.AnchorX,cord.AnchorY)*scale,center+V(cord.Attachment)*scale,
                opacity,seconds,cord.AnchorX*.011f,cord.Slack*scale,reduced);
    }

    internal static void Cord(SpriteBatch batch,Vector2 from,Vector2 to,float opacity,float seconds,float phase,float slack,bool reduced)
    {
        Vector2 prior=from;
        int count=reduced?8:16;
        for(int i=1;i<=count;i++)
        {
            float t=i/(float)count;
            Vector2 next=Vector2.Lerp(from,to,t)+new Vector2(MathF.Sin(t*MathF.PI)*(slack+(reduced?0:MathF.Sin(seconds*1.2f+phase)*1.2f)),0);
            Line(batch,prior,next,new Color(23,19,24)*opacity,3.5f);
            Line(batch,prior+new Vector2(-.7f,0),next+new Vector2(-.7f,0),new Color(166,149,126)*(.75f*opacity),1.25f);
            prior=next;
        }
    }

    private void DrawPart(SpriteBatch batch,DollSprite part,Vector2 root,float scale,Color tint,float breakup,float consume,Vector2 sink,float time,bool reduced)
    {
        if(part.Harness&&part.Cell==0)
        {
            DrawFrame(batch,false,FirstSeveranceDollFrames.Body(time/60,bodyCast,reduced),root+V(part.Position)*scale,
                V(part.Scale)*scale*2,part.Rotation,tint,breakup,consume,sink,time,reduced);
            return;
        }
        var region=FirstSeveranceDollPose.Region(part);
        var rect=new Rectangle(region.X,region.Y,region.Width,region.Height);
        var texture=part.Harness?harness!.Value:atlas!.Value;
        Vector2 position=root+V(part.Position)*scale;
        // Keep her face coherent during Final; the dress/limbs fragment first.
        float amount=part.Cell==0&&!part.Harness?breakup*.27f:breakup;
        if(FirstSeveranceDollPose.Flexible(part))
        {
            if(amount<.12f&&consume<=0)
            {
                surface.Draw(batch,texture,rect,part,position,V(part.Scale)*scale,tint,time/60,reduced,1-Window(amount,0,.12));
                return;
            }
            // Settle the bend before breaking it, so Final has no mesh/sprite pop.
            amount=Window(amount,.12,1);
        }
        if(amount>.001f || consume>0)
            FirstSeveranceDissolutionVisuals.Bone(batch,texture,rect,position,V(part.Pivot),V(part.Scale)*scale,
                part.Rotation,tint,amount,consume,sink,time,reduced);
        else batch.Draw(texture,position-Main.screenPosition,rect,tint,part.Rotation,V(part.Pivot),V(part.Scale)*scale,SpriteEffects.None,0);
    }

    private void DrawFrame(SpriteBatch batch,bool hand,int frame,Vector2 at,Vector2 scale,float rotation,
        Color tint,float breakup,float consume,Vector2 sink,float time,bool reduced)
    {
        var uv=FirstSeveranceDollFrames.Region(hand,frame);
        var rect=new Rectangle(uv.X,uv.Y,uv.Width,uv.Height);
        var texture=hand?handFrames!.Value:bodyFrames!.Value;
        var pivot=V(FirstSeveranceDollFrames.Pivot(hand));
        if(breakup>.001f||consume>0)
            FirstSeveranceDissolutionVisuals.Bone(batch,texture,rect,at,pivot,scale,rotation,tint,breakup,consume,sink,time,reduced);
        else batch.Draw(texture,at-Main.screenPosition,rect,tint,rotation,pivot,scale,SpriteEffects.None,0);
    }

    private void Ensure()
    {
        atlas??=ModContent.Request<Texture2D>(ArtRoot+"DollRigAtlas",AssetRequestMode.ImmediateLoad);
        harness??=ModContent.Request<Texture2D>("Convergence/Assets/Textures/NPCs/NullCantorRigAtlas",AssetRequestMode.ImmediateLoad);
        coffin??=ModContent.Request<Texture2D>(ArtRoot+"DollCoffin",AssetRequestMode.ImmediateLoad);
        shell??=ModContent.Request<Texture2D>(FirstSeveranceShellSurface.TexturePath,AssetRequestMode.ImmediateLoad);
        attendant??=ModContent.Request<Texture2D>(ArtRoot+"DollAttendant",AssetRequestMode.ImmediateLoad);
        handFrames??=ModContent.Request<Texture2D>(ArtRoot+"RemoteClawFrames",AssetRequestMode.ImmediateLoad);
        bodyFrames??=ModContent.Request<Texture2D>(ArtRoot+"RestraintFrames",AssetRequestMode.ImmediateLoad);
    }
    internal void Unload() { atlas=null;harness=null;coffin=null;attendant=null;shell=null;handFrames=null;bodyFrames=null;captureAccents.Unload();surface.Unload(); }
}
