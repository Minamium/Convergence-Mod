using System;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance;

internal readonly record struct DollCaptureShard(int X,int Y,int Width,int Height,Vector2 Position,float Rotation,float Scale,float Opacity);

// Stateless client choreography, keyed only by the accepted deployment age.
// All cuts reconstruct the original 32x52 NPC at age zero and end at the
// existing coffin center. No debris actors, gameplay RNG or retained particles.
internal static class FirstSeveranceDollCapture
{
    internal const int Count=28;

    internal static DollCaptureShard Sample(int index,float age,Vector2 foot,Vector2 core,bool reduced)
    {
        if (index<0 || index>=Count) throw new ArgumentOutOfRangeException(nameof(index));
        int x=index%4*8,y=index/4*8,height=Math.Min(8,52-y);
        Vector2 local=new(x+4-16,y+height*.5f-50);
        Vector2 start=foot+local;
        float stagger=((index*17)%29)/29f;
        float release=.14f+stagger*.14f;
        float separate=EaseOut(Window(age,release,release+.08f));
        float travel=Window(age,release+.15f,.79f+stagger*.075f);
        // Fast separation -> held tension -> accelerating suction, never a
        // second intact NPC lifted towards a newly closing shell.
        float plunge=travel*travel*travel;
        float sign=index%2==0?-1:1;
        Vector2 split=new(sign*(16+stagger*29),-16-stagger*39);
        if(reduced) split*=.45f;
        Vector2 opened=start+split*separate;
        Vector2 bend=new(sign*(reduced?22:92)*MathF.Sin(plunge*MathF.PI),0);
        Vector2 position=Vector2.Lerp(opened,core,plunge)+bend;
        float rotation=sign*((.22f+stagger*.46f)*separate+plunge*2.4f)*(reduced?.4f:1);
        float fade=Window(plunge,.83f,1);
        return new(x,y,8,height,position,rotation,1-.87f*plunge,1-fade);
    }

    private static float Window(float value,float start,float end) => Math.Clamp((value-start)/(end-start),0,1);
    private static float EaseOut(float value) => 1-(1-value)*(1-value)*(1-value);
}
