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
    private Asset<Texture2D>? atlas, coffin, attendant;
    private readonly FirstSeveranceDollPose pose = new();
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
        pose.Body(seconds,cast,kick,unfurl,emergence,reduced,pry);
        float scale=depth; // Visible P1 arm keeps the same size and joint endpoints while the casing peels.
        using var pixels=new PixelPass(batch);
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

    internal void DrawEncased(SpriteBatch batch,Vector2 center,double tick,float opacity,float pressure,bool front,bool reduced)
    {
        Ensure();
        float seconds=(float)(tick%216000)/60f;
        if (front) pose.EncasedFace(); else pose.Encased(seconds,pressure,reduced);
        using var pixels=new PixelPass(batch);
        if (!front) DrawCords(batch,center,1,opacity,seconds,reduced);
        foreach(var part in pose.Sprites) DrawPart(batch,part,center,1,Color.White*opacity,0,0,center,seconds*60,reduced);
    }

    internal void DrawRemoteArm(SpriteBatch batch,Vector2 shoulder,Vector2 elbow,Vector2 wrist,
        float handRotation,float handScale,Color tint,float breakup,float consume,Vector2 sink,float time,bool reduced)
    {
        Ensure();
        using var pixels=new PixelPass(batch);
        Arm(3,shoulder,elbow); Arm(4,elbow,wrist);
        var hand=new DollSprite(5,new(0,0),new(69,10),new(handScale),handRotation);
        DrawPart(batch,hand,wrist,1,tint,breakup,consume,sink,time,reduced);
        void Arm(int cell,Vector2 start,Vector2 end)
        {
            var (top,bottom)=FirstSeveranceDollPose.Anchors(cell);
            Vector2 authored=V(bottom-top),target=end-start;
            var part=new DollSprite(cell,new(0,0),top,new(target.Length()/authored.Length()),target.ToRotation()-authored.ToRotation());
            DrawPart(batch,part,start,1,tint,breakup,consume,sink,time,reduced);
        }
    }

    internal void DrawPreparation(SpriteBatch batch,FirstSeverancePreparationProjection prep,double tick,bool reduced)
    {
        Ensure();
        float age=Math.Clamp((float)((tick-prep.EnteredTick)/FirstSeverancePreparationTimeline.DeploymentTicks),0,1);
        Vector2 ground=new(prep.GroundX,prep.GroundY);
        Vector2 core=ground-new Vector2(0,FirstSeveranceLanceTuning.BossHeightAboveCore);
        Vector2 foot=ground-new Vector2(0,54);
        if (TileEntity.ByPosition.TryGetValue(new Point16(prep.CoreTopLeft.X,prep.CoreTopLeft.Y),out var entity)
            && entity is FoundationCoreTileEntity foundation) foot=FirstSeveranceDollAttendant.StandingFoot(foundation);
        // Upward pull -> held breath -> abrupt final capture. Same 180 server ticks.
        float pull=.72f*Window(age,.13,.43)+.28f*Window(age,.56,.67);
        float closure=Window(age,.58,.78);
        float grown=Window(age,.77,1);
        Vector2 captive=Vector2.Lerp(foot-new Vector2(0,24),core,pull);
        float seconds=(float)(tick%216000)/60f;
        using (new PixelPass(batch))
        {
            if (age<.80f)
            {
                for(int side=-1;side<=1;side+=2)
                    Cord(batch,FoundationCoreVisuals.HoistAnchor(ground,side,true,age),captive+new Vector2(side*9,-12),
                        Window(age,.05,.16)*(1-grown),seconds,side,2,reduced);
                var texture=attendant!.Value;
                batch.Draw(texture,captive-Main.screenPosition,new Rectangle(0,0,32,52),Color.White*(1-closure),
                    .35f*pull,new Vector2(16,26),1+.14f*pull,SpriteEffects.None,0);
            }
            // Two halves close *around* the visible captive; not a cross-faded enlargement.
            for(int side=-1;side<=1;side+=2)
            {
                var source=new Rectangle(side<0?0:128,0,128,256);
                Vector2 offset=new(side*(1-closure)*175,0);
                batch.Draw(coffin!.Value,core+offset-Main.screenPosition,source,
                    Color.White*Window(age,.30,.53),0,new Vector2(side<0?128:0,128),2.25f,SpriteEffects.None,0);
            }
        }
        if (grown>.001f)
        {
            DrawEncased(batch,core,tick,grown,0,false,reduced);
            using (new PixelPass(batch)) batch.Draw(coffin!.Value,core-Main.screenPosition,null,Color.White*grown,0,new Vector2(128),2.25f,SpriteEffects.None,0);
            DrawEncased(batch,core,tick,grown,0,true,reduced);
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
        var rect=new Rectangle(part.Cell%3*128,part.Cell/3*128,128,part.CropHeight);
        Vector2 position=root+V(part.Position)*scale;
        // Keep her face coherent during Final; the dress/limbs fragment first.
        float amount=part.Cell==0?breakup*.27f:breakup;
        if(amount>.001f || consume>0)
            FirstSeveranceDissolutionVisuals.Bone(batch,atlas!.Value,rect,position,V(part.Pivot),V(part.Scale)*scale,
                part.Rotation,tint,amount,consume,sink,time,reduced);
        else batch.Draw(atlas!.Value,position-Main.screenPosition,rect,tint,part.Rotation,V(part.Pivot),V(part.Scale)*scale,SpriteEffects.None,0);
    }

    private void Ensure()
    {
        atlas??=ModContent.Request<Texture2D>(ArtRoot+"DollRigAtlas",AssetRequestMode.ImmediateLoad);
        coffin??=ModContent.Request<Texture2D>(ArtRoot+"DollCoffin",AssetRequestMode.ImmediateLoad);
        attendant??=ModContent.Request<Texture2D>(ArtRoot+"DollAttendant",AssetRequestMode.ImmediateLoad);
    }
    internal void Unload() { atlas=null;coffin=null;attendant=null; }
}
