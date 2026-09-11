using System;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance;

// Eight newly drawn poses per material, not resampled screenshots of a rig.
// Pure presentation mapping; clocks pause with the simulation and own no state.
internal static class FirstSeveranceDollFrames
{
    internal const int Count=8, Height=352, HandWidth=224, BodyWidth=240;
    internal static Vector2 Pivot(bool hand) => new(120,hand?28:138);
    internal static (int X,int Y,int Width,int Height) Region(bool hand,int frame)
    {
        if(frame<0||frame>=Count) throw new ArgumentOutOfRangeException(nameof(frame));
        int width=hand?HandWidth:BodyWidth;
        return(frame%4*width,frame/4*Height,width,Height);
    }
    internal static int Hand(float seconds,float tension,float strike,bool reduced)
    {
        // Splay -> brief hesitation -> curl. Strike comes from the same accepted
        // attack curve as the wrist, never from a private looping attack clock.
        if(strike>.12f) return Math.Clamp(4+(int)(strike*4),4,7);
        float idle=reduced?0:(MathF.Sin(seconds*2.2f)+1)*.28f;
        return Math.Clamp((int)(Math.Clamp(tension,0,1)*3.99f+idle),0,3);
    }
    internal static int Body(float seconds,float cast,bool reduced)
    {
        if(reduced) return Math.Clamp((int)(Math.Clamp(cast,0,1)*4),0,4);
        // Hold relaxed, fast opening, short suspended pose, slower recovery.
        float p=seconds%2.4f;
        if(p<0) p+=2.4f;
        int frame=p<.8f?0:p<1.2f?1+(int)((p-.8f)*10):p<1.4f?4:5+(int)((p-1.4f)*3);
        return Math.Clamp(Math.Max(frame,(int)(Math.Clamp(cast,0,1)*4)),0,7);
    }
}
