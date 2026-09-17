using System;
using Convergence.Content.Items.Oboro;

namespace Convergence.Client.Weapons;

internal readonly record struct OboroBladePose(float X, float Y, float Angle, float Length, float Progress, int Step);
internal readonly record struct OboroEcho(OboroBladePose Pose, uint Swing, ulong At);

// Per-player, client-only presentation history. No hit decisions, projectile spawns
// or packets. Plain values keep cleanup/late-snapshot behavior testable without graphics.
internal sealed class OboroSwingPresentation
{
    internal const int Capacity = 16, FadeTicks = 14, SettleTicks = 10;
    private readonly OboroEcho[] echoes = new OboroEcho[Capacity];
    private int head, count;
    private bool initialized, wasSwinging, swingSeen;
    private ulong generation, settleAt;
    private uint swing;
    private float entryCorrection, entryLength, lastAge;
    private OboroBladePose settleFrom;
    internal OboroBladePose Pose { get; private set; }
    internal bool Settling { get; private set; }
    internal bool Swinging => wasSwinging;
    internal int Count => count;
    internal OboroEcho Echo(int index) => echoes[(head + index) % Capacity];
    internal static float Wrap(float angle) => MathF.IEEERemainder(angle, MathF.Tau);
    internal static float Opacity(ulong at, ulong now)
    {
        if (now < at || now - at >= FadeTicks) return 0;
        float remaining = 1 - (now - at) / (float)FadeTicks;
        return remaining * remaining;
    }
    internal void Clear()
    {
        head = count = 0; initialized = wasSwinging = swingSeen = Settling = false;
        generation = settleAt = 0; swing = 0; entryCorrection = entryLength = lastAge = 0;
        Pose = settleFrom = default;
        Array.Clear(echoes);
    }
    internal void Update(OboroSnapshot view, float age, bool swinging, bool eligible,
        float x, float y, int facing, ulong now)
    {
        if (!eligible) { if (initialized) Clear(); return; }
        if (initialized && (generation != view.Generation
            || (Pose.X - x) * (Pose.X - x) + (Pose.Y - y) * (Pose.Y - y) > 320 * 320)) Clear();
        float restAngle = facing == 1 ? -1.1f : -2.04f;
        if (!initialized)
        {
            initialized = true; generation = view.Generation;
            Pose = new(x + facing * 12, y + 8, restAngle, 145, 1, 0);
        }
        while (count > 0 && Opacity(Echo(0).At, now) == 0)
        { head = (head + 1) % Capacity; count--; }
        // Once a replica reached the end, an older-but-newly-arrived clock must
        // not replay that same serial while waiting for the terminal snapshot.
        if (!wasSwinging && swingSeen && swing == view.Swing) swinging = false;
        if (swinging && view.Duration > 0)
        {
            bool fresh = !wasSwinging || swing != view.Swing;
            if (fresh)
            {
                // Blend a changed aim only while harmless; reach the authoritative
                // angle exactly before the live window begins.
                entryCorrection = Wrap(Pose.Angle - (view.Aim + view.Facing * OboroRules.Offset(view.Step, 0)));
                entryLength = Pose.Length;
                swing = view.Swing; swingSeen = true; lastAge = -1;
            }
            age = Math.Max(age, lastAge); // a delayed snapshot must not rewind a trail
            float p = Math.Clamp(age / view.Duration, 0, 1);
            float angle = view.Aim + view.Facing * OboroRules.Offset(view.Step, p);
            if (p < OboroRules.Windup(view.Step))
                angle += entryCorrection * (1 - OboroRules.Ease(p / OboroRules.Windup(view.Step)));
            float drawLength = entryLength + (OboroRules.Reach - entryLength) * OboroRules.Ease(p / OboroRules.Windup(view.Step));
            Pose = new(x, y, angle, drawLength, p, view.Step);
            if (age > lastAge && OboroRules.Live(view.Step, p))
            {
                if (count == Capacity) { head = (head + 1) % Capacity; count--; }
                echoes[(head + count++) % Capacity] = new(Pose, swing, now);
            }
            lastAge = age; wasSwinging = true; Settling = false;
        }
        else
        {
            if (wasSwinging) { settleAt = now; settleFrom = Pose; Settling = true; }
            wasSwinging = false;
            float t = Settling ? OboroRules.Ease(Math.Min(SettleTicks, now - settleAt) / (float)SettleTicks) : 1;
            Pose = new(x + facing * 12 * t, y + 8 * t,
                Settling ? settleFrom.Angle + Wrap(restAngle - settleFrom.Angle) * t : restAngle,
                Settling ? settleFrom.Length + (145 - settleFrom.Length) * t : 145, 1, Pose.Step);
            if (t >= 1) Settling = false;
        }
    }
}
