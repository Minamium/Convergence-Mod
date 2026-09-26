using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

// Accepted native NPC kinematics, not a second attack/target selector.
internal readonly record struct SamuraiMotionSample(float X, float Y, float VX, float VY)
{
    internal bool Valid => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(VX) && float.IsFinite(VY)
        && Math.Abs(X) <= 1000000 && Math.Abs(Y) <= 1000000 && Math.Abs(VX) <= 200 && Math.Abs(VY) <= 200;
    internal void Write(BinaryWriter w) { w.Write(X); w.Write(Y); w.Write(VX); w.Write(VY); }
    internal static SamuraiMotionSample Read(BinaryReader r)
    {
        var value = new SamuraiMotionSample(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        if (!value.Valid) throw new InvalidDataException("ghost_samurai.motion_invalid");
        return value;
    }
}

internal sealed class SamuraiClientMotion
{
    internal const int SyncTicks = 6, PredictionTicks = 18, CorrectionTicks = 6;
    private SamuraiMotionSample sample;
    private float correctionX, correctionY;
    private ulong received;
    internal int Age { get; private set; } = -1;

    internal bool Accept(int age, SamuraiMotionSample next, ulong now, bool discontinuity = false)
    {
        if (age <= Age || !next.Valid) return false; // Duplicate hit-sync cannot rewind/restart the curve.
        var old = At(now);
        float dx = old.X - next.X, dy = old.Y - next.Y;
        bool snap = Age < 0 || discontinuity || now < received || now - received > 45 || dx * dx + dy * dy > 320 * 320;
        correctionX = snap ? 0 : dx; correctionY = snap ? 0 : dy;
        sample = next; Age = age; received = now;
        return true;
    }
    internal (float X, float Y) At(ulong now)
    {
        float elapsed = now >= received ? Math.Min(60UL, now - received) : 0;
        float move = Math.Min(PredictionTicks, elapsed);
        float t = Math.Clamp(elapsed / CorrectionTicks, 0, 1);
        float keep = 1 - t * t * (3 - 2 * t);
        return (sample.X + sample.VX * move + correctionX * keep,
            sample.Y + sample.VY * move + correctionY * keep);
    }
}
