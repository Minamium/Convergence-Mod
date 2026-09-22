using System;
using System.Collections.Generic;
using System.Numerics;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.AzureCathedral;

// Authority creates immutable native slash plans. The renderer and hit test
// consume exactly those lines; neither samples a client RNG or player position.
internal static class AzureLattice
{
    internal const int Warning=66, Stagger=3, MaxLines=24, FuryCadence=144;
    internal const float Spacing=240;
    internal const int Duration=Warning+(MaxLines-1)*Stagger+AzureRules.CutLive+AzureRules.CutResidue;
    internal static bool Due(AzurePhase phase,int elapsed,out int pattern)
    {
        pattern=0;if(elapsed<0)return false;
        int clock=elapsed%AzureRules.CycleTicks;
        if(phase==AzurePhase.Duet)
        {
            // Before Stack, before/during/after Spread. Even the last line of
            // the opening volley releases before its chorus starts at +24.
            int stack=2*AzureRules.PhraseTicks+24,spread=5*AzureRules.PhraseTicks+24;
            return clock==stack-90-Warning || clock==spread-90-Warning
                || clock==spread+120-Warning || clock==spread+330-Warning;
        }
        if(phase!=AzurePhase.Fury)return false;
        int phrase=clock/AzureRules.PhraseTicks,t=clock%AzureRules.PhraseTicks;
        if(phrase is 1 or 4 || t%FuryCadence!=0 || t+Duration>AzureRules.PhraseTicks)return false;
        // Diagonal, orthogonal, diagonal, with different phase offsets each time.
        pattern=t/FuryCadence==1?0:1;
        return true;
    }
    internal static AzureAttackPlan[] Create(Guid fight,short girl,RaidFieldGeometry field,int born,int pattern)
    {
        var lines=new List<(Vector2 Origin,float Angle)>(MaxLines);
        uint seed=2166136261;
        foreach(byte b in fight.ToByteArray())seed=unchecked((seed^b)*16777619);
        seed=unchecked((seed^(uint)born)*16777619);
        float shift=(Next(ref seed)%1000)/1000f;
        var center=new Vector2(field.CenterX,field.CenterY);
        for(int axis=0;axis<2;axis++)
        {
            float angle=axis*MathF.PI/2+(pattern==0?0:MathF.PI/4);
            var direction=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
            var normal=new Vector2(-direction.Y,direction.X);
            float extent=(Math.Abs(normal.X)*(field.Right-field.Left)+Math.Abs(normal.Y)*(field.Bottom-field.Top))*.5f;
            for(float offset=-extent+Spacing*(.3f+shift*.65f);offset<extent-35;offset+=Spacing)
            {
                var origin=center+normal*offset;
                if(field.ClipAxis(origin.X,origin.Y,direction.X,direction.Y,out float first,out float last) && last-first>=120)
                    lines.Add((origin,angle));
            }
        }
        if(lines.Count>MaxLines)throw new InvalidOperationException("azure.lattice_capacity");
        for(int i=lines.Count-1;i>0;i--)
        {int j=(int)(Next(ref seed)%(uint)(i+1));(lines[i],lines[j])=(lines[j],lines[i]);}
        var plans=new AzureAttackPlan[lines.Count];
        for(int i=0;i<plans.Length;i++)
        {
            var line=lines[i];int fire=born+Warning+i*Stagger;
            plans[i]=new(fight,girl,AzureAttackKind.GlacialCut,born,fire,fire+AzureRules.CutLive,
                line.Origin.X,line.Origin.Y,line.Angle,4000,AzureRules.CutRadius,340);
        }
        return plans;
    }
    private static uint Next(ref uint state)
    {state=unchecked(state*1664525+1013904223);return state;}
}
