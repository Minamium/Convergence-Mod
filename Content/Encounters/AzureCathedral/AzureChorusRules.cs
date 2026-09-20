using System;
using System.IO;
using System.Numerics;

namespace Convergence.Content.Encounters.AzureCathedral;

internal enum AzureChorusKind : byte { Stack, Spread }
internal readonly record struct AzureChorusPlan(Guid Fight, short Girl, AzureChorusKind Kind, byte Members,
    int Born, int Fire, int End, Vector2 Center)
{
    internal void Write(BinaryWriter w)
    {
        w.Write(Fight != Guid.Empty); if (Fight == Guid.Empty) return;
        w.Write(Fight.ToByteArray()); w.Write(Girl); w.Write((byte)Kind); w.Write(Members);
        w.Write(Born); w.Write(Fire); w.Write(End); w.Write(Center.X); w.Write(Center.Y);
    }
    internal static AzureChorusPlan? Read(BinaryReader r)
    {
        if (!AzureState.Presence(r)) return null;
        var p = new AzureChorusPlan(AzureState.Id(r),r.ReadInt16(),(AzureChorusKind)r.ReadByte(),r.ReadByte(),
            r.ReadInt32(),r.ReadInt32(),r.ReadInt32(),new(r.ReadSingle(),r.ReadSingle()));
        if (p.Girl is < 0 or >= 200 || !Enum.IsDefined(p.Kind) || p.Members == 0 || p.Born is < 0 or > 72000
            || p.Fire-(long)p.Born is < 180 or > 300 || p.End-(long)p.Fire is < 30 or > 120
            || !AzureChorusRules.Point(p.Center)) throw new InvalidDataException("azure.chorus");
        return p;
    }
}
internal static class AzureChorusRules
{
    internal const float StackRadius = 190, SpreadRadius = 190;
    internal const int Warning = 270, ImpactTicks = 12, Damage = 900;
    internal static bool Point(Vector2 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && Math.Abs(p.X)<400000 && Math.Abs(p.Y)<150000;
    internal static int[] Resolve(AzureChorusKind kind, Vector2 center, Vector2[] positions, byte announced, byte living)
    {
        if (positions.Length is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(positions));
        int[] damage=new int[positions.Length];byte active=(byte)(announced & living);int count=0,gathered=0;
        for(int i=0;i<positions.Length;i++)
            if((active & (1<<i))!=0) { count++; if(Vector2.DistanceSquared(center,positions[i])<=StackRadius*StackRadius)gathered++; }
        if(kind==AzureChorusKind.Stack)
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
}
