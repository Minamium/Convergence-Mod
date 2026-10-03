using System;
using System.IO;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Immutable admission + monotonic server aim samples. No observer selects a target.
internal static class CrimsonTrackingBeam
{
    internal const int LockTicks = 0;
    internal const int SampleInterval = 3;
    internal const float Radius = 36;
    // Round robin over the living roster. The runtime keeps one ordinal for crossflows and one for the other aimed
    // notes, so every member takes crossflows and ordinary notes in turn whatever the phrase's note count.
    internal static int TargetIndex(int ordinal, int living)
    {
        if (ordinal < 0 || living is < 1 or > 8) throw new ArgumentOutOfRangeException();
        return ordinal % living;
    }
    internal static int LockAt(in CrimsonGesturePlan p) => p.Born;
    internal static bool CanAccept(in CrimsonGesturePlan p, int previousTick, int tick, CrimsonPoint point)
        => tick >= p.Born - 1 && tick <= LockAt(p) && tick > previousTick && point.Finite
            && point.X >= p.Field.Left + 100 && point.X <= p.Field.Right - 100
            && point.Y >= p.Field.Top + 100 && point.Y <= p.Field.Bottom - 100;
    internal static CrimsonPoint Follow(CrimsonPoint old, CrimsonPoint desired)
        => CrimsonPoint.Lerp(old, desired, .45f);
    internal static CrimsonStroke Stroke(in CrimsonGesturePlan p, float age, bool forecast)
    {
        var direction = CrimsonChoreography.Direction(p.Phrase, p.Pulse);
        var f = p.Field;
        if (!f.ClipAxis(p.Target.X, p.Target.Y, direction.X, direction.Y, out float first, out float last)) return default;
        var edge = p.Target + direction * first;
        float release = age - p.Fire;
        bool rift = p.Technique == CrimsonTechnique.SpatialRift;
        float reach = forecast ? 1 : CrimsonInvocation.Ease(release / (rift ? 2 : 5));
        float width = forecast ? 1 : CrimsonInvocation.Ease(release / (rift ? 2 : 6))
            * (1 - CrimsonInvocation.Ease((age - (p.End - 8)) / 8));
        return new(edge, edge + direction * ((last - first) * reach), (rift ? CrimsonSpatialCuts.Radius : Radius) * width);
    }
    internal static void WriteAim(BinaryWriter w, int tick, CrimsonPoint target)
    { w.Write(tick); w.Write(target.X); w.Write(target.Y); }
    internal static (int Tick, CrimsonPoint Target) ReadAim(BinaryReader r)
        => (r.ReadInt32(), new(r.ReadSingle(), r.ReadSingle()));
}
