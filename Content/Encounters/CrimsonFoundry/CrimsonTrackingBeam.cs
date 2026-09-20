using System;
using System.IO;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Immutable admission + monotonic server aim samples. No observer selects a target.
internal static class CrimsonTrackingBeam
{
    internal const int LockTicks = 10;
    internal const int SampleInterval = 3;
    internal const float Radius = 36;
    internal static int TargetIndex(int phrase, int pulse, int living)
    {
        if (phrase < 1 || pulse is < 0 or > 1 || living is < 1 or > 8)
            throw new ArgumentOutOfRangeException();
        return ((phrase - 1) * 2 + pulse) % living;
    }
    internal static int LockAt(in CrimsonGesturePlan p) => p.Fire - LockTicks;
    internal static bool CanAccept(in CrimsonGesturePlan p, int previousTick, int tick, CrimsonPoint point)
        => tick >= p.Born - 1 && tick <= LockAt(p) && tick > previousTick && point.Finite
            && point.X >= p.Field.Left + 100 && point.X <= p.Field.Right - 100
            && point.Y >= p.Field.Top + 100 && point.Y <= p.Field.Bottom - 100;
    internal static CrimsonPoint Follow(CrimsonPoint old, CrimsonPoint desired)
        => CrimsonPoint.Lerp(old, desired, .45f);
    internal static CrimsonStroke Stroke(in CrimsonGesturePlan p, float age, bool forecast)
    {
        var delta = p.Target - p.Stage;
        float distance = MathF.Sqrt(delta.LengthSquared);
        var direction = distance > 1 ? delta * (1 / distance) : new CrimsonPoint(0, 1);
        var f = p.Field;
        float length = 5000;
        if (direction.X > .0001f) length = Math.Min(length, (f.Right - p.Stage.X) / direction.X);
        if (direction.X < -.0001f) length = Math.Min(length, (f.Left - p.Stage.X) / direction.X);
        if (direction.Y > .0001f) length = Math.Min(length, (f.Bottom - p.Stage.Y) / direction.Y);
        if (direction.Y < -.0001f) length = Math.Min(length, (f.Top - p.Stage.Y) / direction.Y);
        float backward = 5000;
        if (direction.X > .0001f) backward = Math.Min(backward, (p.Stage.X - f.Left) / direction.X);
        if (direction.X < -.0001f) backward = Math.Min(backward, (p.Stage.X - f.Right) / direction.X);
        if (direction.Y > .0001f) backward = Math.Min(backward, (p.Stage.Y - f.Top) / direction.Y);
        if (direction.Y < -.0001f) backward = Math.Min(backward, (p.Stage.Y - f.Bottom) / direction.Y);
        var edge = p.Stage - direction * backward;
        float release = age - p.Fire;
        float reach = forecast ? 1 : CrimsonInvocation.Ease(release / 5);
        float width = forecast ? 1 : CrimsonInvocation.Ease(release / 6)
            * (1 - CrimsonInvocation.Ease((age - (p.End - 8)) / 8));
        return new(edge, edge + direction * ((length + backward) * reach), Radius * width);
    }
    internal static void WriteAim(BinaryWriter w, int tick, CrimsonPoint target)
    { w.Write(tick); w.Write(target.X); w.Write(target.Y); }
    internal static (int Tick, CrimsonPoint Target) ReadAim(BinaryReader r)
        => (r.ReadInt32(), new(r.ReadSingle(), r.ReadSingle()));
}
