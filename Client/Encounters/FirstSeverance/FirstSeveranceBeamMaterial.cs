using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace Convergence.Client.Encounters.FirstSeverance;

// Feature-local adapters preserve every caller's accepted ray/time/opacity.
// Raid and weapon geometry stay separate; both use flowing energy materials.
internal static class FirstSeveranceBeamMaterial
{
    internal static void SingleForecast(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        Vector2 origin, Vector2 direction, float length, float halfWidth,
        double age, float charge, float opacity, Color color, double fireAge)
        => FirstSeveranceRaidVfx.Beam(batch,origin,direction,length,halfWidth,age,
            charge,0,opacity,color,Terraria.ModLoader.ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects,
            mouth:false,fireAge:fireAge);

    internal static void Flow(SpriteBatch batch,Vector2 origin,Vector2 direction,
        float length,float halfWidth,double age,Color color,float power,bool reduced,
        float throatLength=0,float throatWidth=0,double fireAge=double.NaN,double endAge=double.NaN,bool confined=false)
        => FirstSeveranceRaidVfx.Beam(batch,origin,direction,length,halfWidth,age,
            1,1,power,color,reduced,confined:confined||halfWidth>=120,mouth:throatLength>0,fireAge:fireAge,endAge:endAge,
            bellLength:throatLength,bellWidth:throatWidth);

    internal static void DrawTooth(SpriteBatch batch,FirstSeveranceAttackAccents accents,
        Vector2 origin,Vector2 direction,float length,float halfWidth,double clock,
        float charge,float emission,float opacity,Color color,bool reduced,
        double fireAge=double.NaN,double endAge=double.NaN)
        => FirstSeveranceRaidVfx.Beam(batch,origin,direction,length,halfWidth,clock,
            charge,emission,opacity,color,reduced,confined:true,mouth:false,fireAge:fireAge,endAge:endAge);

    internal static void DrawVolume(SpriteBatch batch,FirstSeveranceAttackAccents accents,
        Vector2 origin,Vector2 direction,float length,float halfWidth,double clock,
        float charge,float emission,float opacity,Color color,bool reduced,
        double fireAge=double.NaN,double endAge=double.NaN)
        => FirstSeveranceRaidVfx.Beam(batch,origin,direction,length,halfWidth,clock,
            charge,emission,opacity,color,reduced,confined:true,mouth:false,fireAge:fireAge,endAge:endAge);

    internal static void Draw(SpriteBatch batch,FirstSeveranceAttackAccents accents,
        Vector2 origin,Vector2 direction,float length,float halfWidth,double clock,
        float charge,float emission,float opacity,Color color,bool reduced,
        double fireAge=double.NaN,double endAge=double.NaN)
        => FirstSeveranceRaidVfx.Beam(batch,origin,direction,length,halfWidth,clock,
            charge,emission,opacity,color,reduced,confined:halfWidth>=120,mouth:false,fireAge:fireAge,endAge:endAge);

    internal static void CrushMembrane(SpriteBatch batch,FirstSeveranceAttackAccents accents,
        Vector2 center,float halfWidth,float halfHeight,double age,float charge,
        float live,float opacity,Color color,bool reduced)
    {
        FirstSeveranceRaidVfx.Pressure(batch,center,Vector2.UnitX,halfWidth*2,halfHeight,
            age,charge,opacity,color,reduced);
        if(live>0) {
            FirstSeveranceRaidVfx.Flare(batch,center,age,opacity,color,reduced,2.2f);
            if(!reduced) FirstSeveranceRaidVfx.Sparks(batch,center,Vector2.UnitX,age,1,opacity,color,false,1.8f);
        }
    }
}
