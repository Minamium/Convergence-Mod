#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Convergence.Content.Encounters.AzureCathedral;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace Convergence.Client.Encounters.AzureCathedral;

public sealed class AzureVisualConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue(false)] public bool ReducedEffects;
    [DefaultValue(true)] public bool ScreenShake = true;
}

[Autoload(Side = ModSide.Client)]
internal sealed class AzureVisuals : ModSystem
{
    private Guid fight;
    private int previous = -1, lastCharge = -1, lastFire = -1, lastDash = -1;
    private float shake;
    private Matrix worldToViewport;
    private Guid projectedFight;
    private readonly List<ReLogic.Utilities.SlotId> voices = new();
    private static int lastTick;
    private static long received;
    internal static bool Reduced => ModContent.GetInstance<AzureVisualConfig>().ReducedEffects;
    internal static bool Local(AzureBoss girl) => Array.Exists(girl.State.Members ?? Array.Empty<AzureMember>(), m => m.Slot == Main.myPlayer);
    internal static float RenderAge(AzureBoss girl)
    {
        int tick = (int)girl.VisualAge;
        if (lastTick != tick) { lastTick = tick; received = Stopwatch.GetTimestamp(); }
        return tick + (Main.gamePaused ? 0 : (float)Math.Clamp((Stopwatch.GetTimestamp()-received)/(double)Stopwatch.Frequency*60,0,1));
    }
    public override void PostUpdateEverything()
    {
        var girl = AzurePackets.Boss;
        if (girl is null || !girl.Fresh || !Local(girl)) { Reset(); return; }
        int age = (int)girl.VisualAge;
        if (fight != girl.State.Fight) { Reset(); fight = girl.State.Fight; previous = age - 1; }
        shake *= .85f;
        if(girl.State.MusicStart>=0) CueAt(girl.State.MusicStart + 170, "PhaseRupture", .46f, 8);
        if (girl.State.EndAt >= 0) CueAt(girl.State.EndAt, girl.State.Stage==AzureStage.Victory?"RaidVictory":"RaidDefeat",.45f,9);
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not AzureAttack a || a.Plan.Fight != fight) continue;
            if (a.Plan.Born != lastCharge && Crossed(a.Plan.Born)) { lastCharge=a.Plan.Born; Play("Beams/PortalCharge",.36f,.22f); }
            if (a.Plan.Fire != lastFire && Crossed(a.Plan.Fire))
            { lastFire=a.Plan.Fire; Play(a.Plan.Kind==AzureAttackKind.MouthBeam?"Beams/PortalFire":"CoreHit",a.Plan.Kind==AzureAttackKind.MouthBeam?.62f:.50f,.20f); shake=Math.Max(shake,a.Plan.Kind==AzureAttackKind.MouthBeam?7:2.2f); }
        }
        if (girl.State.Live && AzureRules.Phrase(age,girl.State.UnlockAt) is 0 or 2)
        {
            int t = AzureRules.Clock(age,girl.State.UnlockAt);
            int serial = (age-girl.State.UnlockAt)/140;
            if (t%140>=82 && t%140<87 && serial!=lastDash) { lastDash=serial;Play("Beams/PortalFire",.52f,-.14f);shake=6; }
        }
        for (int i=voices.Count-1;i>=0;i--) if (!SoundEngine.TryGetActiveSound(voices[i],out _)) voices.RemoveAt(i);
        previous=age;
        bool Crossed(int tick)=>tick>=0 && previous<tick && age>=tick && age-tick<8;
        void CueAt(int tick,string sound,float volume,float intensity) { if(Crossed(tick)){Play(sound,volume,0);shake=Math.Max(shake,intensity);} }
    }
    private void Play(string cue,float volume,float pitch)
    {
        if(voices.Count>=24) return;
        voices.Add(SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/"+cue){Volume=volume*(Reduced?.75f:1),Pitch=pitch,MaxInstances=3}));
    }
    private void Reset()
    {
        foreach(var id in voices) if(SoundEngine.TryGetActiveSound(id,out var voice)) voice.Stop();
        voices.Clear();fight=projectedFight=Guid.Empty;previous=lastCharge=lastFire=lastDash=-1;shake=0;
    }
    public override void ClearWorld()=>Reset();
    public override void OnWorldUnload()=>Reset();
    public override void Unload()=>Reset();
    public override void ModifyScreenPosition()
    {
        if(Reduced || !ModContent.GetInstance<AzureVisualConfig>().ScreenShake || shake<.02f) return;
        float t=Main.GameUpdateCount%6000;
        Main.screenPosition+=new Vector2(MathF.Sin(t*2.11f),MathF.Cos(t*1.87f))*Math.Min(10,shake);
    }
    internal static void Stroke(SpriteBatch batch,Vector2 a,Vector2 b,Color color,float width)
    {
        var delta=b-a;
        batch.Draw(TextureAssets.MagicPixel.Value,a-Main.screenPosition,new Rectangle(0,0,1,1),color,delta.ToRotation(),new(0,.5f),new Vector2(delta.Length(),width),SpriteEffects.None,0);
    }
    public override void PostDrawTiles()
    {
        var girl=AzurePackets.Boss;if(Main.gameMenu || girl is null || !girl.Fresh) return;
        // Capture in the world pass. UI layers may temporarily change screen metrics;
        // neither their UI scale nor a second camera conversion belongs in this mask.
        worldToViewport=Matrix.CreateTranslation(-Main.screenPosition.X,-Main.screenPosition.Y,0)*Main.GameViewMatrix.TransformationMatrix;
        projectedFight=girl.State.Fight;
        var batch=Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,Main.Rasterizer,null,Main.GameViewMatrix.TransformationMatrix);
        try
        {
            float age=RenderAge(girl);AzureEnergy.Begin();
            foreach(Projectile p in Main.ActiveProjectiles)
            {
                if(p.ModProjectile is not AzureAttack attack || attack.Plan.Fight!=girl.State.Fight || !attack.TryGirl(out _))continue;
                var h=attack.Plan;if(age<h.Born || age>=h.End)continue;
                bool forecast=age<h.Fire;
                if(!attack.Geometry(girl,age,forecast,out var a,out var b,out float radius))continue;
                var dir=(b-a).SafeNormalize(Vector2.UnitY);float length=Vector2.Distance(a,b);
                if(h.Kind==AzureAttackKind.MouthBeam || forecast)
                    AzureEnergy.Add(a,dir,length,radius,age,h.Fire,h.End,1,Reduced,h.Born,true);
                else
                {
                    var glow=MiscTexturesRegistry.BloomCircleSmall.Value;
                    batch.Draw(glow,(a+b)*.5f-Main.screenPosition,null,new Color(82,207,255,0)*.5f,dir.ToRotation(),glow.Size()*.5f,new Vector2(length*1.2f,radius*3)/glow.Size(),SpriteEffects.None,0);
                    AzureMaterials.Shard(batch,a,b,radius,age,1);
                }
                if(!Reduced)
                    for(int i=0;i<(forecast?3:5);i++)
                    {
                        float u=(i*.217f+age*.009f+h.Born*.03f)%1;
                        Vector2 n=new(-dir.Y,dir.X),at=a+(b-a)*u+n*MathF.Sin(i*3.7f+age*.06f)*radius*.6f;
                        var bloom=MiscTexturesRegistry.BloomCircleSmall.Value;
                        batch.Draw(bloom,at-Main.screenPosition,null,new Color(160,231,255,0)*(forecast?.18f:.4f),0,bloom.Size()*.5f,(forecast?5:8)/bloom.Width,SpriteEffects.None,0);
                    }
            }
            if(girl.State.WormSlot>=0 && Main.npc[girl.State.WormSlot].ModNPC is AzureWorm worm && girl.State.Live && girl.State.WormLife>0
                && AzureRules.Phrase((int)age,girl.State.UnlockAt) is 0 or 2)
            {
                int dash=AzureRules.Clock((int)age,girl.State.UnlockAt)%140;
                if(dash<82)
                {
                    Vector2 target=new(worm.NPC.ai[0],worm.NPC.ai[1]);var dir=(target-worm.NPC.Center).SafeNormalize(Vector2.UnitX);
                    if(girl.State.Field.ClipAxis(worm.NPC.Center.X,worm.NPC.Center.Y,dir.X,dir.Y,out float first,out float last))
                        AzureEnergy.Add(worm.NPC.Center+dir*Math.Max(0,first),dir,Math.Max(0,last-Math.Max(0,first)),AzureRules.SegmentRadius,
                            age,age+(82-dash),age+(117-dash),.6f,Reduced,age-dash,true);
                }
            }
            AzureEnergy.Draw(batch);
        }
        finally{batch.End();}
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var girl=AzurePackets.Boss;if(Main.gameMenu || girl is null || !girl.Fresh || !Local(girl)) return;
        layers.Insert(0,new LegacyGameInterfaceLayer("Convergence: Azure field",()=>Overlay(girl),InterfaceScaleType.None));
    }
    private bool Overlay(AzureBoss girl)
    {
        var batch=Main.spriteBatch;var s=girl.State;var f=s.Field;float age=RenderAge(girl);var v=Main.instance.GraphicsDevice.Viewport;
        if(projectedFight!=s.Fight) return true;
        Vector2 p0=Vector2.Transform(new(f.Left,f.Top),worldToViewport),p1=Vector2.Transform(new(f.Right,f.Top),worldToViewport);
        Vector2 p2=Vector2.Transform(new(f.Left,f.Bottom),worldToViewport),p3=Vector2.Transform(new(f.Right,f.Bottom),worldToViewport);
        Vector2 a=Vector2.Min(Vector2.Min(p0,p1),Vector2.Min(p2,p3)),b=Vector2.Max(Vector2.Max(p0,p1),Vector2.Max(p2,p3));
        int l=Math.Clamp((int)a.X,0,v.Width),r=Math.Clamp((int)b.X,0,v.Width),t=Math.Clamp((int)a.Y,0,v.Height),bottom=Math.Clamp((int)b.Y,0,v.Height);
        if(s.Contains(Main.myPlayer) && !Main.LocalPlayer.dead)
        {
            Fill(new(0,0,v.Width,t),Color.Black);Fill(new(0,bottom,v.Width,v.Height-bottom),Color.Black);
            Fill(new(0,t,l,Math.Max(0,bottom-t)),Color.Black);Fill(new(r,t,v.Width-r,Math.Max(0,bottom-t)),Color.Black);
            var ice=new Color(128,215,237);
            if(a.X>=0)Fill(new(l,t,2,Math.Max(0,bottom-t)),ice);
            if(b.X<=v.Width)Fill(new(Math.Max(0,r-2),t,2,Math.Max(0,bottom-t)),ice);
            if(a.Y>=0)Fill(new(l,t,Math.Max(0,r-l),2),ice);
        }
        bool opening=s.Stage==AzureStage.Countdown,ending=s.EndAt>=0,deploy=s.Stage==AzureStage.Deployment;
        if(opening || ending || deploy)
        {
            float clock=ending?age-s.EndAt:opening?age-s.MusicStart:age;
            float duration=ending?AzureRules.Ending:opening?AzureRules.Intro:AzureRules.Deploy;
            float opacity=AzureRules.Ease(clock/30)*AzureRules.Ease((duration-clock)/45);
            Fill(new(0,0,v.Width,(int)(v.Height*.1f)),Color.Black*opacity);
            Fill(new(0,(int)(v.Height*.9f),v.Width,(int)(v.Height*.11f)),Color.Black*opacity);
            if(opening)
                Utils.DrawBorderString(batch,"CATHEDRAL OF THE WHITE NIGHT",new(v.Width*.5f,v.Height*.84f),new Color(201,239,250)*opacity,.92f,.5f);
            if(ending && s.Stage==AzureStage.Victory)
                Utils.DrawBorderString(batch,"THE GLASS FALLS SILENT",new(v.Width*.5f,v.Height*.84f),new Color(201,239,250)*opacity,.86f,.5f);
            return false;
        }
        if(s.Stage!=AzureStage.Ready) return true;
        int count=0;foreach(var member in s.Members)if(member.Ready)count++;
        foreach(var m in s.Members)
        {
            var player=Main.player[m.Slot];if(!player.active)continue;
            Vector2 pos=Vector2.Transform(player.Top,worldToViewport)-new Vector2(0,34);
            if(m.Slot!=Main.myPlayer){if(m.Ready)Utils.DrawBorderString(batch,"Ready!",pos,Color.LightCyan,.68f,.5f);continue;}
            var button=new Rectangle((int)Math.Clamp(pos.X-78,4,v.Width-160),(int)Math.Clamp(pos.Y,4,v.Height-36),156,30);
            bool hover=button.Contains(Main.mouseX,Main.mouseY);
            Fill(button,hover?new Color(30,63,76):new Color(12,25,36));
            Fill(new(button.X,button.Bottom-2,button.Width*count/s.Members.Length,2),Color.LightCyan);
            Utils.DrawBorderString(batch,(m.Ready?"Ready!":"READY")+$"  {count}/{s.Members.Length}",new(button.Center.X,button.Y+6),Color.LightCyan,.68f,.5f);
            if(hover){Main.LocalPlayer.mouseInterface=true;if(Main.mouseLeft && Main.mouseLeftRelease){Main.mouseLeftRelease=false;AzurePackets.Ready(!m.Ready);}}
        }
        return true;
        void Fill(Rectangle rect,Color c)=>batch.Draw(TextureAssets.MagicPixel.Value,rect,new Rectangle(0,0,1,1),c);
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class AzureActorVisuals : GlobalNPC
{
    public override bool AppliesToEntity(NPC n,bool lateInstantiation)=>n.ModNPC is AzureBoss or AzureWorm;
    public override bool PreDraw(NPC npc,SpriteBatch batch,Vector2 screen,Color drawColor)
    {
        if(npc.ModNPC is AzureWorm worm)
        {if(worm.Index==0 && worm.TryGirl(out var girl))AzureMaterials.Worm(worm,girl!,batch,screen);return false;}
        if(npc.ModNPC is not AzureBoss g || g.State.Fight==Guid.Empty) return false;
        float age=AzureVisuals.RenderAge(g),alpha=g.State.GirlLife<=0?.25f:1;
        if(g.State.EndAt>=0)alpha*=1-AzureRules.Ease((age-g.State.EndAt)/150);
        int frame=g.State.Stage<=AzureStage.Ready?((int)age%260<9?1:0):7;
        float pose=0;
        foreach(Projectile p in Main.ActiveProjectiles)
            if(p.ModProjectile is AzureAttack a && a.Plan.Fight==g.State.Fight && a.Plan.Kind!=AzureAttackKind.MouthBeam && age<a.Plan.Fire+24 && age>=a.Plan.Born)
            {pose=age-a.Plan.Fire;frame=pose< -22?4:pose<0?5:6;break;}
        var art=ModContent.Request<Texture2D>("Convergence/Assets/Textures/AzureCathedral/Liora").Value;
        var src=new Rectangle(frame%4*128,frame/4*128,128,128);
        float lean=frame==6?MathF.Exp(-Math.Max(0,pose)/9)*-.07f:MathF.Sin(age*.025f)*.018f;
        batch.Draw(art,npc.Center-screen,src,Color.White*alpha,lean,new(64,68),.56f,npc.spriteDirection==1?SpriteEffects.None:SpriteEffects.FlipHorizontally,0);
        if(!AzureVisuals.Reduced && g.State.Stage>=AzureStage.Countdown)
        {
            var glow=MiscTexturesRegistry.BloomCircleSmall.Value;
            for(int i=0;i<9;i++)
            {
                float y=(age*.65f+i*11)%110;Vector2 pos=npc.Center+new Vector2(MathF.Sin(i*4.1f+age*.02f)*25,20+y);
                batch.Draw(glow,pos-screen,null,new Color(131,225,255,0)*(.28f*(1-y/110))*alpha,0,glow.Size()*.5f,5f/glow.Width,SpriteEffects.None,0);
            }
        }
        return false;
    }
}
[Autoload(Side = ModSide.Client)]
internal sealed class AzureProjectileVisuals : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile p,bool lateInstantiation)=>p.ModProjectile is AzureAttack;
    public override bool PreDraw(Projectile p,ref Color lightColor)=>false;
}
