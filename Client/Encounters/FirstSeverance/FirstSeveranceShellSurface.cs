using System;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance;

// One material/footprint for preparation, combat petals and offline inspection.
// The original microfractured metal is retained, not reduced to a 256px cutout.
internal static class FirstSeveranceShellSurface
{
    internal const string TexturePath="Convergence/Assets/Textures/NPCs/NullCantorShell";
    internal const int MaskSize=1024;
    internal const float Aspect=.84f;
    internal static float Radius(double tick,float charge=0)
        => 288+MathF.Sin((float)(tick%36000)*.021f)*1.3f+charge*3;
    internal static Vector2 Size(double tick,float charge=0) => new Vector2(Aspect,1)*(Radius(tick,charge)*2);
    internal static int Sector(int x,int y,int size)
    {
        float half=(size-1)*.5f;
        float nx=(x-half)/half,ny=(y-half)/half;
        float radius=MathF.Sqrt(nx*nx+ny*ny),angle=MathF.Atan2(ny,nx)+MathF.PI;
        float warped=angle+.045f*MathF.Sin(radius*17+angle*5);
        return Math.Clamp((int)((warped+MathF.Tau)%MathF.Tau/MathF.Tau*8),0,7);
    }
}
