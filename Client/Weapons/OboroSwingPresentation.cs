#nullable enable
using System;
using Convergence.Content.Items.Oboro;

namespace Convergence.Client.Weapons;

internal readonly record struct OboroBladePose(float X, float Y, float Angle, float Length, float Progress, int Step, int Facing = 1);
internal readonly record struct OboroEcho(OboroBladePose Pose, OboroBladePose Sword, uint Swing, ulong At);

// Per-player, client-only presentation history. No hit decisions, projectile spawns
// or packets. Plain values keep cleanup/late-snapshot behavior testable without graphics.
internal sealed class OboroSwingPresentation
{
    internal const int Capacity = 16, FadeTicks = 8;
    internal const int ReturnCutFadeTicks = 6, FinisherFadeTicks = 10;
    // 実体の刀は常に人が握れる長さ。560pxの攻撃範囲は斬撃時だけ霊刃で表す。
    internal const float SwordLength = 132;
    // Runtime binds Luminance's installed Cubic InOut. It only softens harmless entry.
    internal static Func<float, float>? ComboEntryEase { get; set; }
    private static float EntryEase(int step, float t) => ComboEntryEase is not null
        ? ComboEntryEase(Math.Clamp(t, 0, 1)) : OboroRules.Ease(t);
    private readonly OboroEcho[] echoes = new OboroEcho[Capacity];
    private int head, count;
    private bool initialized, wasSwinging, swingSeen;
    private ulong generation;
    private uint swing;
    private float entryCorrection, lastAge;
    internal OboroBladePose Pose { get; private set; }
    internal OboroBladePose SwordPose { get; private set; }
    internal float ArmAngle => SwordPose.Angle + (Swinging ? SwordPose.Facing * OboroSwordMotion.Wrist(SwordPose.Step, SwordPose.Progress) : 0);
    internal bool Swinging => wasSwinging;
    internal int Count => count;
    internal OboroEcho Echo(int index) => echoes[(head + index) % Capacity];
    internal static float Wrap(float angle) => MathF.IEEERemainder(angle, MathF.Tau);
    internal static float Opacity(ulong at, ulong now, int step = 0)
    {
        int duration = step switch { 1 => ReturnCutFadeTicks, 2 => FinisherFadeTicks, _ => FadeTicks };
        if (now < at || now - at >= (ulong)duration) return 0;
        float remaining = 1 - (now - at) / (float)duration;
        return remaining * remaining;
    }
    internal void Clear()
    {
        head = count = 0; initialized = wasSwinging = swingSeen = false;
        generation = 0; swing = 0; entryCorrection = lastAge = 0;
        Pose = SwordPose = default;
        Array.Clear(echoes);
    }
    internal void Update(OboroSnapshot view, float age, bool swinging, bool eligible,
        float x, float y, int facing, ulong now, OboroHandBasis hand = default)
    {
        if (!eligible) { if (initialized) Clear(); return; }
        if (initialized && (generation != view.Generation
            || (Pose.X - x) * (Pose.X - x) + (Pose.Y - y) * (Pose.Y - y) > 320 * 320)) Clear();
        float entryAngle = view.Aim + view.Facing * OboroRules.Offset(view.Step, 0);
        if (!initialized)
        {
            initialized = true; generation = view.Generation;
            Pose = new(x, y, entryAngle, 0, 1, view.Step, facing);
            SwordPose = Pose;
        }
        while (count > 0 && Opacity(Echo(0).At, now, Echo(0).Pose.Step) == 0)
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
                // First cut from idle begins in the authored high guard; do not spend
                // its entire anticipation rotating from the unrelated inventory pose.
                if (view.Step == 0 && !wasSwinging) entryCorrection = 0;
                swing = view.Swing; swingSeen = true; lastAge = -1;
            }
            age = Math.Max(age, lastAge); // a delayed snapshot must not rewind a trail
            float p = Math.Clamp(age / view.Duration, 0, 1);
            float angle = view.Aim + view.Facing * OboroRules.Offset(view.Step, p);
            if (p < OboroRules.EntryEnd(view.Step))
                angle += entryCorrection * (1 - EntryEase(view.Step, p / OboroRules.EntryEnd(view.Step)));
            float drawLength = OboroRules.Reach;
            var root = OboroRules.RootOffset(view.Step, p, view.Aim, angle, hand);
            Pose = new(x + root.X, y + root.Y, angle, drawLength, p, view.Step, view.Facing);
            SwordPose = OboroSwordMotion.AtHand(Pose, x, y, hand, true);
            if (age > lastAge && OboroRules.Live(view.Step, p))
            {
                if (count == Capacity) { head = (head + 1) % Capacity; count--; }
                echoes[(head + count++) % Capacity] = new(Pose, SwordPose, swing, now);
            }
            lastAge = age; wasSwinging = true;
        }
        else
        {
            // Retain the accepted angle for the next replica; hide weapon and arm
            // immediately. Only bounded spectral cut residue may outlive the swing.
            wasSwinging = false;
            Pose = Pose with { X = x, Y = y, Length = 0, Progress = 1, Facing = facing };
            SwordPose = Pose;
        }
    }
}
