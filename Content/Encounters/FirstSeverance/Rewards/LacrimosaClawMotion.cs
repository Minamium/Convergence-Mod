#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// The four poses of the DW01 hand art. Open and Clench carry no hit shape; Rake (A/B live) and Thrust (C live)
// are drawn on the large rung so their talon tips sit on the capsules below.
internal enum LacrimosaPose : byte { Open, Rake, Clench, Thrust }

// One hand at one instant. Kata frames are in the aim frame (+x along the aim, +y below it for an upright,
// right-facing owner; ToWorld mirrors y by facing and gravity); grasp frames are in world space. Axis is the
// direction the hand points (radians in the same frame). Large: the k=1 rung (else k=2); Flash: 0..1 pearl
// flash of a rung swap.
internal readonly record struct LacrimosaHand(Vector2 Wrist, float Axis, LacrimosaPose Pose, bool Large, float Flash);

// Lacrimosa's Claws (NullRefrain), docs/encounters/first-severance/WEAPONS.md#lacrimosas-claws--refresh-2026-10.
// The kata (A down-rake with the right hand, B up-rake with the left, C clap with both), its hit capsules, the
// grasp timeline and its crush ellipse. Pure (System.Numerics only): the domain tests link this file, and the
// client presentation draws from the very same frames, so a hit shape and the hand drawn over it never drift.
//
// Each hand follows one canonical 84-tick track (base ticks): A is track 0-26, B 26-50, C 50-84. Channels are the
// wrist's angle and radius about the owner and the hand's axis, each a cubic Hermite through knots with
// Catmull-Rom tangents (zero at the rest pose at 0 and 84), so pose and speed carry across every stroke join and
// the kata flows A -> B -> C -> A without braking. Native true-melee attack speed shortens a stroke to
// round(base / speed) real ticks (bounded MinTicks..MaxTicks) and its live window with it.
internal static class LacrimosaClawMotion
{
    internal const int BaseDamage = 7700;
    internal const int None = -1, RakeDown = 0, RakeUp = 1, Clap = 2, Strokes = 3;
    internal const int Right = 0, Left = 1;
    internal const int KataTicks = 84, MaxTicks = 90, ComboResetTicks = 45;
    // The palms meet on this base age of C.
    internal const int ClapContact = 22;
    // Swept capsule samples across the last tick (Soboro's 8 + 1).
    internal const int SubSamples = 8;

    // Hit shapes, in world px along the hand axis from the wrist (the art's cuff pivot). Authoritative; the large
    // (k=1) art is fitted to them: the rake's longest talon at RakeTip, the flat hand's middle tip at ClapTip.
    internal const float RakeFrom = 12, RakeTip = 160, RakeRadius = 44;
    internal const float ClapFrom = 0, ClapTip = 196, ClapRadius = 52;
    internal const float MaximumReach = 430;
    // Knuckle front of the large fist (ClawClench_L) from its wrist; presentation only (the crush is the ellipse).
    internal const float FistReach = 140;

    internal static int BaseTicks(int stroke) => stroke switch { RakeUp => 24, Clap => 34, _ => 26 };
    internal static int MinTicks(int stroke) => stroke switch { RakeUp => 11, Clap => 16, _ => 12 };
    internal static int Offset(int stroke) => stroke switch { RakeUp => 26, Clap => 50, _ => 0 };
    internal static int LiveStart(int stroke) => stroke switch { RakeUp => 7, Clap => 18, _ => 9 };
    // Exclusive.
    internal static int LiveEnd(int stroke) => stroke switch { RakeUp => 14, Clap => 23, _ => 17 };
    internal static float Multiplier(int stroke) => stroke == Clap ? 1.30f : .85f;
    // A rakes with the right hand, B with the left, C claps with both.
    internal static bool Active(int stroke, int hand) => stroke == Clap || (stroke == RakeUp) == (hand == Left);
    internal static int Stroke(int value) => Math.Clamp(value, RakeDown, Clap);

    internal static int Duration(int stroke, float speed)
    {
        stroke = Stroke(stroke);
        if (!float.IsFinite(speed)) return BaseTicks(stroke);
        int ticks = (int)MathF.Round(BaseTicks(stroke) / Math.Clamp(speed, .25f, 4f));
        return Math.Clamp(ticks, MinTicks(stroke), MaxTicks);
    }

    internal static int ValidDuration(int stroke, int duration) => Math.Clamp(duration, MinTicks(Stroke(stroke)), MaxTicks);

    // Real stroke age -> base age of the canonical track.
    internal static float BaseAge(int stroke, float age, int duration)
    {
        stroke = Stroke(stroke);
        duration = ValidDuration(stroke, duration);
        return float.IsFinite(age) ? Math.Clamp(age, 0, duration) * BaseTicks(stroke) / duration : 0;
    }

    // The base age the hits have swept to by the end of a stroke's last live tick (the swept capsule's last
    // sample): LiveEnd itself at the base length, a little less when attack speed rounds the window.
    internal static float LastLiveBaseAge(int stroke, int duration)
    {
        stroke = Stroke(stroke);
        duration = ValidDuration(stroke, duration);
        int last = (LiveEnd(stroke) * duration + BaseTicks(stroke) - 1) / BaseTicks(stroke) - 1;
        return BaseAge(stroke, Math.Max(FirstLiveAge(stroke, duration), last), duration);
    }

    // age * base >= liveStart * duration, in integers where possible so the first live tick is exact.
    internal static bool Live(int stroke, float age, int duration)
    {
        stroke = Stroke(stroke);
        duration = ValidDuration(stroke, duration);
        if (!float.IsFinite(age)) return false;
        double scaled = (double)age * BaseTicks(stroke);
        return scaled >= (double)LiveStart(stroke) * duration - 1e-4 && scaled < (double)LiveEnd(stroke) * duration - 1e-4;
    }

    // The first whole stroke age that is live: where a stroke's hit lands when its target is already in reach.
    internal static int FirstLiveAge(int stroke, int duration)
    {
        stroke = Stroke(stroke);
        duration = ValidDuration(stroke, duration);
        return Math.Max(1, (LiveStart(stroke) * duration + BaseTicks(stroke) - 1) / BaseTicks(stroke));
    }

    internal static float TrackTime(int stroke, float baseAge)
        => Offset(Stroke(stroke)) + Math.Clamp(float.IsFinite(baseAge) ? baseAge : 0, 0, BaseTicks(Stroke(stroke)));

    // Draw-time base age from the accepted whole age and the render fraction (Soboro's policy): never the harmless
    // wind-up on a live tick, never the live pose on a recovery tick.
    internal static float DrawBaseAge(int stroke, int ageTick, int duration, float fraction)
    {
        stroke = Stroke(stroke);
        duration = ValidDuration(stroke, duration);
        float age = Math.Clamp(ageTick - 1f + Math.Clamp(float.IsFinite(fraction) ? fraction : 1f, 0f, 1f), 0f, duration);
        float baseAge = BaseAge(stroke, age, duration);
        float tickBase = BaseAge(stroke, ageTick, duration);
        if (tickBase >= LiveStart(stroke) - 1e-4f) baseAge = MathF.Max(baseAge, LiveStart(stroke));
        if (tickBase >= LiveEnd(stroke) - 1e-4f) baseAge = MathF.Max(baseAge, LiveEnd(stroke));
        return baseAge;
    }

    // ---- The kata track -------------------------------------------------------------------------

    // (t, wrist angle, wrist radius, axis). Rest (t 0 and 84): the right hand floats above the front shoulder,
    // the left above the back one, both pointing up and out.
    private static readonly float[] RightKnots =
    {
        0, -.675f, 38.4f, -1f,
        4, -1.22f, 66, -1.42f,
        8, -1.74f, 98, -1.58f,
        // A live 9-16: the wrist rides a 104 px orbit and the hand rakes down through the aim.
        9, -1.76f, 104, -1.46f,
        11, -1.28f, 104, -.98f,
        13, -.18f, 104, .12f,
        15, .80f, 104, 1.10f,
        17, 1.27f, 104, 1.57f,
        21, 1.46f, 94, 1.66f,
        26, 1.42f, 84, 1.52f,
        38, 1.30f, 76, 1.25f,
        50, 1.22f, 84, .98f,
        // C: flung out wide below, then driven flat toward the aim line; live 68-72, the palms meet at 72.
        58, 1.30f, 160, .48f,
        64, 1.16f, 184, .14f,
        68, 1.0808f, 170, 0,
        70, .85f, 128, 0,
        72, .5369f, 97.79f, 0,
        74, .62f, 104, .06f,
        78, .25f, 78, -.45f,
        84, -.675f, 38.4f, -1f,
    };

    // The left hand coils low behind during A, rakes up through the aim in B (live 33-39), and mirrors the right
    // hand through C before floating back over the owner's head to its rest behind the back shoulder.
    private static readonly float[] LeftKnots = BuildLeft();

    private static float[] BuildLeft()
    {
        float[] head =
        {
            0, 3.8166f, 38.4f, 4.1416f,
            10, 3.12f, 54, 3.30f,
            20, 2.38f, 74, 2.30f,
            26, 2.08f, 88, 1.92f,
            30, 1.97f, 100, 1.70f,
            33, 1.80f, 104, 1.50f,
            35, 1.27f, 104, .97f,
            37, .25f, 104, -.05f,
            39, -.60f, 104, -.90f,
            40, -.86f, 104, -1.16f,
            44, -1.36f, 96, -1.56f,
            50, -1.22f, 84, -.98f,
        };
        float[] tail = { 78, -1.62f, 72, -1.55f, 84, 3.8166f - MathF.Tau, 38.4f, 4.1416f - MathF.Tau };
        int mirrored = 0;
        for (int i = 0; i < RightKnots.Length; i += 4) if (RightKnots[i] > 50 && RightKnots[i] < 78) mirrored++;
        var knots = new float[head.Length + mirrored * 4 + tail.Length];
        head.CopyTo(knots, 0);
        int at = head.Length;
        for (int i = 0; i < RightKnots.Length; i += 4)
        {
            if (!(RightKnots[i] > 50 && RightKnots[i] < 78)) continue;
            knots[at++] = RightKnots[i];
            knots[at++] = -RightKnots[i + 1];
            knots[at++] = RightKnots[i + 2];
            knots[at++] = -RightKnots[i + 3];
        }
        tail.CopyTo(knots, at);
        return knots;
    }

    private static readonly float[][] Tangents = { BuildTangents(RightKnots), BuildTangents(LeftKnots) };

    private static float[] BuildTangents(float[] knots)
    {
        int count = knots.Length / 4;
        var tangents = new float[knots.Length];
        for (int i = 1; i < count - 1; i++)
        {
            float span = knots[(i + 1) * 4] - knots[(i - 1) * 4];
            for (int c = 1; c < 4; c++) tangents[i * 4 + c] = (knots[(i + 1) * 4 + c] - knots[(i - 1) * 4 + c]) / span;
        }
        return tangents;
    }

    // Rung swaps (base track ticks) per hand: small -> large into each live window and back after it.
    private static readonly int[][] Swaps = { new[] { 9, 17, 68, 73 }, new[] { 33, 40, 68, 73 } };
    // A rung swap hides under a short pearl flash: two ticks at FlashPeak, then the art as authored. The peak steps
    // the art's light tones to pearl and its dark ones to pearl grey, so the hand stays a two-tone shape, never a
    // white silhouette. Only a rung swap flashes; a pose change on the same rung (open -> fist) does not.
    internal const float FlashTicks = 2, FlashPeak = .45f;

    internal static float Flash(float since) => since >= 0 && since < FlashTicks ? FlashPeak : 0;

    internal static ReadOnlySpan<int> RungSwaps(int hand) => Swaps[hand == Left ? Left : Right];

    // The hand on the canonical track at base time t (0..84).
    internal static LacrimosaHand Hand(int hand, float t)
    {
        hand = hand == Left ? Left : Right;
        t = Math.Clamp(float.IsFinite(t) ? t : 0, 0, KataTicks);
        float[] knots = hand == Left ? LeftKnots : RightKnots;
        float[] tangents = Tangents[hand];
        int count = knots.Length / 4, i = 0;
        while (i < count - 2 && t > knots[(i + 1) * 4]) i++;
        float t0 = knots[i * 4], t1 = knots[(i + 1) * 4], span = t1 - t0, u = Math.Clamp((t - t0) / span, 0, 1);
        float u2 = u * u, u3 = u2 * u;
        float h00 = 2 * u3 - 3 * u2 + 1, h10 = (u3 - 2 * u2 + u) * span, h01 = -2 * u3 + 3 * u2, h11 = (u3 - u2) * span;
        float Channel(int c) => h00 * knots[i * 4 + c] + h10 * tangents[i * 4 + c] + h01 * knots[(i + 1) * 4 + c] + h11 * tangents[(i + 1) * 4 + c];
        float angle = Channel(1), radius = Channel(2), axis = Channel(3);
        (LacrimosaPose pose, bool large) = PoseAt(hand, t);
        return new LacrimosaHand(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius, axis, pose, large, FlashAt(hand, t));
    }

    internal static LacrimosaHand Rest(int hand) => Hand(hand, 0);

    internal static (LacrimosaPose Pose, bool Large) PoseAt(int hand, float t)
    {
        if (t >= 68 && t < 73) return (LacrimosaPose.Thrust, true);
        if (t >= 64 && t < 68) return (LacrimosaPose.Thrust, false);
        bool raking = hand == Left ? t >= 33 && t < 40 : t >= 9 && t < 17;
        return raking ? (LacrimosaPose.Rake, true) : (LacrimosaPose.Open, false);
    }

    internal static float FlashAt(int hand, float t)
    {
        float flash = 0;
        foreach (int swap in RungSwaps(hand))
        {
            float since = t - swap;
            flash = MathF.Max(flash, Flash(since));
        }
        return flash;
    }

    // The stroke's frame for one hand at a base age of that stroke.
    internal static LacrimosaHand Frame(int stroke, int hand, float baseAge) => Hand(hand, TrackTime(stroke, baseAge));

    // The live capsule of one hand at a base age, in the aim frame; false outside the live window or for the idle hand.
    internal static bool TryCapsule(int stroke, int hand, float baseAge, out Vector2 a, out Vector2 b, out float radius)
    {
        a = b = Vector2.Zero;
        radius = 0;
        stroke = Stroke(stroke);
        if (!Active(stroke, hand) || !float.IsFinite(baseAge) || baseAge < LiveStart(stroke) - 1e-4f || baseAge >= LiveEnd(stroke)) return false;
        LacrimosaHand frame = Frame(stroke, hand, baseAge);
        Vector2 along = new(MathF.Cos(frame.Axis), MathF.Sin(frame.Axis));
        bool clap = stroke == Clap;
        a = frame.Wrist + along * (clap ? ClapFrom : RakeFrom);
        b = frame.Wrist + along * (clap ? ClapTip : RakeTip);
        radius = clap ? ClapRadius : RakeRadius;
        return true;
    }

    // Aim frame -> world offset from the owner's centre: y mirrors by facing x gravity, then the aim rotation.
    internal static Vector2 ToWorld(Vector2 local, float aim, int facing, float gravDir)
        => Rotate(new Vector2(local.X, local.Y * Mirror(facing, gravDir)), aim);

    internal static float AxisToWorld(float axis, float aim, int facing, float gravDir) => aim + axis * Mirror(facing, gravDir);

    internal static float Mirror(int facing, float gravDir) => (facing < 0 ? -1 : 1) * (gravDir < 0 ? -1 : 1);

    internal static int Facing(float aim) => MathF.Cos(aim) < 0 ? -1 : 1;

    // ---- Grasp ----------------------------------------------------------------------------------

    // Real ticks (attack speed never compresses the grasp). Age 1 is the click's tick.
    internal const int GraspFlight = 3, GraspArrive = 14, GraspContact = 16, GraspContactEnd = 18, GraspBrace = 38,
        GraspCrush = 40, GraspCrushEnd = 44, GraspRelease = 54, GraspEnd = 64;
    internal static readonly int[] SqueezeBeats = { 22, 28, 33, 36 };
    internal const float ContactMultiplier = .3f, CrushMultiplier = 4.0f;
    internal const float TargetRange = 1120, AcquireRadius = 160, TeleportJump = 64;
    // v1's crush ellipse radii, unchanged.
    internal const float CrushRadiusX = 166, CrushRadiusY = 132;
    internal const float Tilt = -.30f, FlightBow = 160, DrawBack = 20;

    internal static bool ContactLive(float age) => float.IsFinite(age) && age >= GraspContact && age < GraspContactEnd;
    internal static bool CrushLive(float age) => float.IsFinite(age) && age >= GraspCrush && age < GraspCrushEnd;
    // Strokes may start again (and the meter runs again) from GraspRelease.
    internal static bool GraspBusy(float age) => !float.IsFinite(age) || age < GraspRelease;

    // Exact ellipse against an AABB: the nearest point of the box, not an expanded square.
    internal static bool CrushHits(Vector2 delta, Vector2 halfSize)
    {
        if (!Finite(delta) || !Finite(halfSize)) return false;
        float x = MathF.Max(0, MathF.Abs(delta.X) - halfSize.X) / CrushRadiusX;
        float y = MathF.Max(0, MathF.Abs(delta.Y) - halfSize.Y) / CrushRadiusY;
        return x * x + y * y <= 1;
    }

    internal static Vector2 ClampTarget(Vector2 origin, Vector2 requested)
    {
        if (!Finite(origin) || !Finite(requested)) return origin;
        Vector2 delta = requested - origin;
        float length = delta.Length();
        return length <= TargetRange ? requested : origin + delta * (TargetRange / length);
    }

    // Fast start, braked arrival.
    internal static float FlightProgress(float age)
    {
        float x = Math.Clamp((age - GraspFlight) / (GraspArrive - GraspFlight), 0f, 1f);
        return 1 - (1 - x) * (1 - x) * (1 - x);
    }

    // Distance from the grasp centre to each fist's front along the tilt axis. halfExtent: the target's half size
    // along that axis (24..140; an empty grasp uses EmptyHalfExtent). Squeeze beats jolt the fists inward.
    internal const float EmptyHalfExtent = 30;

    internal static float FistGap(float age, float halfExtent)
    {
        float h = Math.Clamp(float.IsFinite(halfExtent) ? halfExtent : EmptyHalfExtent, 24, 140);
        if (!float.IsFinite(age)) age = 0;
        if (age < GraspArrive) return h + 46;
        if (age < GraspContact) return h + 46 - 40 * Smooth((age - GraspArrive) / 2f);
        float gap = h + 6;
        foreach (int beat in SqueezeBeats)
        {
            // A 6 px jolt inward within one tick, eased back over three.
            float since = age - beat;
            if (since >= 0 && since < 4) gap -= 6 * (since < 1 ? Smooth(since) : 1 - Smooth((since - 1) / 3));
        }
        // Brace: the fists part 14 px; crush: they drive through until their fronts are 12 px apart; then burst.
        gap += 14 * Smooth((age - GraspBrace) / 2f) * (1 - Smooth((age - GraspCrush) / 1.5f));
        gap -= h * Smooth((age - GraspCrush) / 2f);
        gap += 70 * Smooth((age - GraspCrushEnd) / 6f);
        return gap;
    }

    // The tilt axis: from the near hand (Right) toward the far one; side +1 when the target is right of the owner.
    internal static Vector2 GraspAxis(int side)
        => new(MathF.Cos(Tilt) * (side < 0 ? -1 : 1), MathF.Sin(Tilt));

    // A grasping hand in world space. From: the hand as shown when the grasp began; rest: where it returns.
    internal static LacrimosaHand GraspHand(int hand, float age, Vector2 center, float halfExtent, int side,
        Vector2 fromWrist, float fromAxis, Vector2 restWrist, float restAxis)
    {
        hand = hand == Left ? Left : Right;
        if (!float.IsFinite(age)) age = 0;
        Vector2 d = GraspAxis(side);
        float sign = hand == Right ? -1 : 1;
        // Each fist points at the centre: the near (right) hand along +d, the far one along -d.
        float axis = MathF.Atan2(-sign * d.Y, -sign * d.X);
        float gap = FistGap(age, halfExtent);
        bool fist = age >= GraspContact && age < GraspCrushEnd;
        float reach = fist ? FistReach : 150;
        Vector2 seat = center + d * sign * (gap + reach);
        if (age < GraspFlight)
        {
            // Discharge: the hand snaps flat and draws back.
            float back = DrawBack * Smooth(age / (GraspFlight - 1f));
            Vector2 away = new(MathF.Cos(fromAxis), MathF.Sin(fromAxis));
            return new LacrimosaHand(fromWrist - away * back, fromAxis, LacrimosaPose.Thrust, false, 0);
        }
        if (age < GraspArrive)
        {
            float p = FlightProgress(age);
            Vector2 start = fromWrist - new Vector2(MathF.Cos(fromAxis), MathF.Sin(fromAxis)) * DrawBack;
            Vector2 chord = seat - start;
            Vector2 normal = chord.LengthSquared() > 1e-4f ? Vector2.Normalize(new Vector2(-chord.Y, chord.X)) : Vector2.UnitY;
            Vector2 control = (start + seat) * .5f + normal * FlightBow * (hand == Right ? 1 : -1);
            Vector2 wrist = (1 - p) * (1 - p) * start + 2 * (1 - p) * p * control + p * p * seat;
            Vector2 tangent = 2 * (1 - p) * (control - start) + 2 * p * (seat - control);
            float heading = tangent.LengthSquared() > 1e-4f ? MathF.Atan2(tangent.Y, tangent.X) : axis;
            float turn = Smooth((age - GraspFlight) / (GraspArrive - GraspFlight - 2f));
            return new LacrimosaHand(wrist, LerpAngle(heading, axis, turn), LacrimosaPose.Thrust, false, 0);
        }
        if (age < GraspCrushEnd)
        {
            // The arrival swaps to the large rung (flash); clenching at contact stays on it (no flash).
            var pose = fist ? LacrimosaPose.Clench : LacrimosaPose.Open;
            return new LacrimosaHand(seat, axis, pose, true, Flash(age - GraspArrive));
        }
        // Burst open, then fly home to the rest pose.
        float home = Smooth((age - GraspCrushEnd - 2) / (GraspRelease - GraspCrushEnd - 2f));
        Vector2 burst = center + d * sign * (FistGap(GraspCrushEnd + 2, halfExtent) + 150);
        float releaseFlash = Flash(age - GraspCrushEnd);
        return new LacrimosaHand(Vector2.Lerp(age < GraspCrushEnd + 2 ? seat : burst, restWrist, home),
            LerpAngle(axis, restAxis, home), LacrimosaPose.Open, false, releaseFlash);
    }

    // ---- Helpers --------------------------------------------------------------------------------

    internal static float Smooth(float t)
    {
        if (!float.IsFinite(t)) return 0;
        t = Math.Clamp(t, 0, 1);
        return t * t * t * (10 + t * (-15 + t * 6));
    }

    internal static float LerpAngle(float from, float to, float t)
        => from + MathF.IEEERemainder(to - from, MathF.Tau) * Math.Clamp(t, 0, 1);

    internal static Vector2 Rotate(Vector2 p, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(p.X * c - p.Y * s, p.X * s + p.Y * c);
    }

    internal static bool Finite(Vector2 p) => float.IsFinite(p.X) && float.IsFinite(p.Y);

    // A live capsule (segment ab swept by radius, round ends) against an axis-aligned hitbox [min, max]: true when
    // the segment comes within radius of the box or crosses it. Unlike a band test it hits a box just beyond the
    // tip and a box big enough to contain the whole segment, so the reach is |b| + radius and a large body pressed
    // against the owner is never missed.
    internal static bool CapsuleHitsBox(Vector2 a, Vector2 b, float radius, Vector2 min, Vector2 max)
    {
        if (!Finite(a) || !Finite(b) || !Finite(min) || !Finite(max) || !float.IsFinite(radius) || radius < 0
            || max.X < min.X || max.Y < min.Y) return false;
        return SegmentBoxDistanceSquared(a, b, min, max) <= radius * radius;
    }

    // Squared distance between segment ab and the box [min, max]; 0 when they touch. For two disjoint convex
    // shapes the nearest pair includes a vertex of one of them: an end of the segment or a corner of the box.
    internal static float SegmentBoxDistanceSquared(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        if (SegmentTouchesBox(a, b, min, max)) return 0;
        float best = MathF.Min(Vector2.DistanceSquared(a, Vector2.Clamp(a, min, max)), Vector2.DistanceSquared(b, Vector2.Clamp(b, min, max)));
        best = MathF.Min(best, Vector2.DistanceSquared(min, Closest(a, b, min)));
        best = MathF.Min(best, Vector2.DistanceSquared(max, Closest(a, b, max)));
        Vector2 c = new(min.X, max.Y), d = new(max.X, min.Y);
        best = MathF.Min(best, Vector2.DistanceSquared(c, Closest(a, b, c)));
        return MathF.Min(best, Vector2.DistanceSquared(d, Closest(a, b, d)));
    }

    // Liang-Barsky: does any point of segment ab lie inside the box?
    private static bool SegmentTouchesBox(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        Vector2 d = b - a;
        float enter = 0, exit = 1;
        return Clip(-d.X, a.X - min.X, ref enter, ref exit) && Clip(d.X, max.X - a.X, ref enter, ref exit)
            && Clip(-d.Y, a.Y - min.Y, ref enter, ref exit) && Clip(d.Y, max.Y - a.Y, ref enter, ref exit);
    }

    // One slab of Liang-Barsky: keeps the parameters t in [enter, exit] with p * t <= q.
    private static bool Clip(float p, float q, ref float enter, ref float exit)
    {
        if (p == 0) return q >= 0;
        float t = q / p;
        if (p < 0)
        {
            if (t > exit) return false;
            if (t > enter) enter = t;
        }
        else
        {
            if (t < enter) return false;
            if (t < exit) exit = t;
        }
        return true;
    }

    // Closest point to p on segment ab.
    internal static Vector2 Closest(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float length = ab.LengthSquared();
        return length < 1e-6f ? a : a + ab * Math.Clamp(Vector2.Dot(p - a, ab) / length, 0, 1);
    }
}
