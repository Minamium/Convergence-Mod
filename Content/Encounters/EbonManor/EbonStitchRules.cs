using System;
using System.IO;
using System.Numerics;

namespace Convergence.Content.Encounters.EbonManor;

internal enum EbonStitchKind : byte { Stack, Spread }
internal readonly record struct EbonStitchPlan(Guid Fight, short Boss, EbonStitchKind Kind, byte Members,
    int Born, int Fire, int End, Vector2 Center)
{
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight != Guid.Empty); if (Fight == Guid.Empty) return;
        w.Write(Fight.ToByteArray()); w.Write(Boss); w.Write((byte)Kind); w.Write(Members);
        w.Write(Born); w.Write(Fire); w.Write(End); w.Write(Center.X); w.Write(Center.Y);
    }
    internal static EbonStitchPlan? Read(BinaryReader r)
    {
        if (!EbonState.Presence(r)) return null;
        var p = new EbonStitchPlan(EbonState.Id(r),r.ReadInt16(),(EbonStitchKind)r.ReadByte(),r.ReadByte(),
            r.ReadInt32(),r.ReadInt32(),r.ReadInt32(),new(r.ReadSingle(),r.ReadSingle()));
        if (p.Boss is < 0 or >= 200 || !Enum.IsDefined(p.Kind) || p.Members == 0 || p.Born is < 0 or > 72000
            || p.Fire-(long)p.Born is < 180 or > 300 || p.End-(long)p.Fire is < 30 or > 120
            || !EbonStitchRules.Point(p.Center)) throw new InvalidDataException("ebon.stitch");
        return p;
    }
}

internal struct EbonVerdictClock
{
    private bool started;
    private float start;
    internal float Sample(bool resolved,float age,int fire,int end)
    {
        if(!resolved)return -1;
        if(!started){start=EbonStitchRules.VerdictStart(age,fire,end);started=true;}
        return age-start;
    }
}
internal static class EbonStitchRules
{
    internal const float StackRadius = 190, SpreadRadius = 352;
    internal const int Warning = 270, ImpactTicks = 12, Damage = 900;
    internal static bool Point(Vector2 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && Math.Abs(p.X)<400000 && Math.Abs(p.Y)<150000;
    internal static int[] Resolve(EbonStitchKind kind, Vector2 center, Vector2[] positions, byte announced, byte living)
    {
        if (positions.Length is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(positions));
        int[] damage=new int[positions.Length];byte active=(byte)(announced & living);int count=0,gathered=0;
        for(int i=0;i<positions.Length;i++)
        {
            if((announced & (1<<i))!=0)count++;
            if((active & (1<<i))!=0 && Vector2.DistanceSquared(center,positions[i])<=StackRadius*StackRadius)gathered++;
        }
        if(kind==EbonStitchKind.Stack)
        {
            int source=count==0?0:(int)Math.Ceiling(Damage*(count-gathered)/(double)count);
            for(int i=0;i<damage.Length;i++)if((active & (1<<i))!=0)damage[i]=source;
        }
        else
            for(int i=0;i<positions.Length;i++)for(int j=i+1;j<positions.Length;j++)
                if((active & (1<<i))!=0 && (active & (1<<j))!=0 && Vector2.DistanceSquared(positions[i],positions[j])<4*SpreadRadius*SpreadRadius)
                    damage[i]=damage[j]=Damage;
        return damage;
    }
    internal static bool VerdictCanReplace(bool oldResolved, byte oldFailures, bool resolved, byte failures)
        => (!oldResolved || resolved && failures==oldFailures) && (resolved || failures==0);
    // A deadline without a verdict is pending, never implicit success. A late
    // accepted verdict starts locally when recovery time permits; very late
    // receipt samples the remaining tail, never extends its owned lifetime.
    internal static float VerdictStart(float age, int fire, int end) => Math.Clamp(age,fire,Math.Max(fire,end-48));
}
