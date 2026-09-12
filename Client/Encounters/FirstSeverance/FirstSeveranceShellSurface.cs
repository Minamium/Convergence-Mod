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
    // One fractional, continuous suspension pose for shell, cuff and cords in
    // preparation AND combat. Cosmetic only: never displaces the real Core.
    internal static (Vector2 Offset,float Roll) Suspension(double tick,bool reduced)
    {
        float t=(float)(tick%216000)/60, strength=reduced?.18f:1;
        float beat=MathF.Sin(t*1.31f+.32f*MathF.Sin(t*.37f));
        float catchWeight=MathF.Pow((1+MathF.Sin(t*.83f+.9f))*.5f,6);
        return(new Vector2(10*beat+3*MathF.Sin(t*2.13f),7*MathF.Sin(t*1.31f-.7f)-5*catchWeight)*strength,
            -.065f+strength*(.032f*beat+.009f*MathF.Sin(t*2.13f-1)));
    }

    internal static Vector2 Attachment(int side,bool outer,double tick,float roll)
        => Vector2.Transform(new Vector2(side*(outer?66:188),outer?-215:-65)*(Radius(tick)/288),
            Matrix3x2.CreateRotation(roll));
    internal static int Sector(int x,int y,int size)
    {
        float half=(size-1)*.5f;
        float nx=(x-half)/half,ny=(y-half)/half;
        float radius=MathF.Sqrt(nx*nx+ny*ny),angle=MathF.Atan2(ny,nx)+MathF.PI;
        float warped=angle+.045f*MathF.Sin(radius*17+angle*5);
        return Math.Clamp((int)((warped+MathF.Tau)%MathF.Tau/MathF.Tau*8),0,7);
    }
}
