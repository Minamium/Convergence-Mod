using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;
namespace Convergence.Client.Encounters.FirstSeverance;

// Accepted eight-cast timing adapter. The whole Raid now uses the material suite.
internal sealed class FirstSeverancePursuitBeamVisuals
{
    internal void Draw(SpriteBatch batch,FirstSeveranceLanceRay ray,double now,
        ulong start,ulong fire,ulong end,bool active,ulong? cancelledAt,Color color,bool reduced)
    {
        if(Main.dedServ || now<start) return;
        double clock=cancelledAt is { } cancel?Math.Min(now,cancel):now;
        float fade=cancelledAt is { } stop?1-Window(now,stop,stop+12d):1;
        bool warning=clock<fire;
        float opacity=fade*(warning?Arrive(clock-start,4):active?1:(1-Window(clock,end,end+16d))*(cancelledAt.HasValue?.12f:1));
        float charge=CastTension(clock,start,fire);
        float kick=active?ReleaseImpulse(clock,fire):0;
        if(!warning) ray=FirstSeveranceBeamIgnition.At(ray,clock-fire);
        Vector2 origin=new(ray.X,ray.Y),direction=new(ray.DirectionX,ray.DirectionY);
        FirstSeveranceRaidVfx.Beam(batch,origin,direction,ray.Length,ray.HalfWidth,clock-start,
            charge,warning?0:1,opacity,color,reduced,release:kick,fireAge:fire-start,endAge:end-start);
        FirstSeveranceRaidVfx.Charge(batch,origin,direction,clock-start,charge,kick,
            opacity*(warning?1:active?.55f:.04f),color,reduced,.85f);
    }
}
