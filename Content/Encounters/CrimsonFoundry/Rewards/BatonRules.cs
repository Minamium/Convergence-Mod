#nullable enable
using System;
using System.Numerics;
using static Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Which black-blood look a stretch of baton ink takes (ScarletInk PathLivePass / PathDormantPass / PathResiduePass).
internal enum BatonLook : byte { Live, Dormant, Residue }

// One stretch [From, To] of a sampled baton path (arc length in px) in one look. Time at arc position u is
// Time0 + TimeSlope * u: for live ink the ticks since that point ignited (the material and the collision both open the
// radius by InkOpen(time)); for residue the remaining fade 1..0; unused for dormant ink. Only Live spans collide.
internal readonly record struct BatonInkSpan(BatonLook Look, float From, float To, float Radius, float Time0, float TimeSlope = 0,
    float Opacity = 1, float Warmth = 0, bool Bead = false)
{
    internal float TimeAt(float u) => Time0 + TimeSlope * u;
}

// A baton stroke: the quadratic A -> B with control K.
internal readonly record struct BatonCurve(Vector2 A, Vector2 K, Vector2 B)
{
    internal Vector2 Point(float s)
    {
        float r = 1 - s;
        return r * r * A + 2 * r * s * K + s * s * B;
    }
    // The same curve as a cubic Bezier, for the shared adaptive sampler.
    internal Vector2 C1 => A + (K - A) * (2f / 3);
    internal Vector2 C2 => B + (K - B) * (2f / 3);
}

// The held baton's frame: the conducting pattern is authored facing right with y down and px about the shoulder; it
// tilts toward the aim by at most FrameTiltDegrees and mirrors when the conductor faces left.
internal readonly record struct BatonFrame(float Tilt, int Facing)
{
    internal Vector2 World(Vector2 local)
    {
        float c = MathF.Cos(Tilt), s = MathF.Sin(Tilt);
        var turned = new Vector2(local.X * c - local.Y * s, local.X * s + local.Y * c);
        return Facing < 0 ? new Vector2(-turned.X, turned.Y) : turned;
    }
}

// Pure rules for the Scarlet Baton (REWARDS.md, "Magic - Scarlet Baton"): gesture motion, stroke geometry from
// chord/bend/skew, the shape codec, the writing/ignition/river timelines with their ink spans (drawn and collided
// alike), the score's bookkeeping and the river's path with its run-skipping. No Terraria or XNA references; linked
// into Tests/Convergence.DomainTests and the offline preview. Spec numbers stay in CrimsonRewardRules; the numbers
// here are this weapon's own authored shapes (pattern points, steering gains) that the spec leaves to the weapon.
internal static class BatonRules
{
    // ---- Gestures: the 4/4 pattern, advanced by the player's clicks ------------------------------------------
    internal const int Down = 0, In = 1, Out = 2, Up = 3, Tutti = 4, GestureKinds = 5, Rest = -1;
    internal const int Ictus = Anticipation + Sweep;            // 11: the end of the sweep, where the beat lands
    internal const int WriteStart = Ictus - WriteTicks;          // 5: the pen writes through the sweep and reaches the end at the ictus
    internal const int WriteEnd = Ictus;                         // 11
    internal const int Written = WriteEnd + WriteLive;           // 17: the last written point has dried
    internal const int ContinueGap = 2;                          // a gesture that starts within this many ticks of the last one continues its pose
    internal static int Duration(int gesture) => gesture == Tutti ? TuttiTicks : GestureTicks;
    internal static int IctusTick(int gesture) => gesture == Tutti ? TuttiLift : Ictus;
    internal static int After(int gesture) => gesture is Down or In or Out ? gesture + 1 : Down;
    // The next gesture after `last` (Rest for none) when the new one starts `idle` ticks after the last one ended.
    internal static int NextGesture(int last, long idle)
        => last is < Down or >= Tutti || idle >= BatonIdleReset ? Down : After(last);
    internal static bool Continues(int last, long idle) => last >= Down && idle is >= 0 and <= ContinueGap;

    // BatonSwing ai[0]: gesture + GestureKinds * (previous + 1), previous = Rest (-1) or the gesture it continues from.
    internal static float EncodeGesture(int gesture, int previous)
    {
        if (gesture is < Down or > Tutti || previous is < Rest or > Tutti) throw new ArgumentOutOfRangeException();
        return gesture + GestureKinds * (previous + 1);
    }
    internal static bool TryDecodeGesture(float code, out int gesture, out int previous)
    {
        gesture = Down; previous = Rest;
        if (!ValidInteger(code, 0, GestureKinds * (GestureKinds + 1) - 1)) return false;
        int value = (int)code;
        gesture = value % GestureKinds; previous = value / GestureKinds - 1;
        return true;
    }

    // ---- Stroke geometry --------------------------------------------------------------------------------------
    // Each gesture writes its mark in its own direction (facing right, y down; degrees): Down falls and leans in, In
    // runs back toward the body, Out runs away, Up rises. A still cursor gives this clean mark with its own small bow;
    // the bow mirrors with the chord when facing left.
    private static readonly float[] ChordDegrees = { 98, 168, 12, -100 };
    private static readonly int[] CleanBends = { -10, -12, 12, 10 };
    internal const int ShapeHalf = 64;                           // the bend/skew pair codec range (bend uses all of it)
    internal const int SkewMax = 40;                             // keeps every curve rounder than the river's strip
    internal const float SweepFull = 72;                         // px of cursor sweep (over SweepWindow ticks) for the full turn
    internal const float BendGain = .8f, SkewGain = .5f;
    internal const float MinCurveRadius = 44;                    // > the river's strip half-width (28 x 1.12 + 10): no inner folds

    internal static Vector2 ChordDirection(int gesture, int facing)
    {
        float a = ChordDegrees[Math.Clamp(gesture, Down, Up)] * MathF.PI / 180;
        return new Vector2(MathF.Cos(a) * (facing < 0 ? -1 : 1), MathF.Sin(a));
    }
    internal static int CleanBend(int gesture, int facing) => CleanBends[Math.Clamp(gesture, Down, Up)] * (facing < 0 ? -1 : 1);
    internal static Vector2 Perp(Vector2 d) => new(-d.Y, d.X);
    internal static Vector2 Rotate(Vector2 v, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
    internal static float SignedAngle(Vector2 from, Vector2 to) => MathF.Atan2(from.X * to.Y - from.Y * to.X, Vector2.Dot(from, to));

    // The cursor clamped within BatonCursorRange of the player.
    internal static Vector2 ClampCursor(Vector2 player, Vector2 cursor)
    {
        Vector2 offset = cursor - player;
        float length = offset.Length();
        return length <= BatonCursorRange || length < 1e-3f ? cursor : player + offset * (BatonCursorRange / length);
    }

    // The stroke a gesture writes, fixed at its first tick: the half-chord (velocity) and the packed bend/skew. The
    // cursor's sweep over the last SweepWindow ticks turns the chord by up to StrokeTurnDegrees toward itself and, by
    // its components across and along the gesture's direction, sets the bend and the skew.
    internal readonly record struct Steering(Vector2 Half, int Bend, int Skew);
    internal static Steering Steer(int gesture, int facing, Vector2 sweep)
    {
        Vector2 d = ChordDirection(gesture, facing), n = Perp(d);
        if (!float.IsFinite(sweep.X) || !float.IsFinite(sweep.Y)) sweep = Vector2.Zero;
        float w = sweep.Length();
        float turn = 0;
        if (w > .5f)
        {
            float limit = StrokeTurnDegrees * MathF.PI / 180;
            turn = Math.Clamp(SignedAngle(d, sweep / w), -limit, limit) * Smooth(w / SweepFull);
        }
        int bend = (int)MathF.Round(Math.Clamp(CleanBend(gesture, facing) + Vector2.Dot(sweep, n) * BendGain, -StrokeBendMax, StrokeBendMax));
        int skew = (int)MathF.Round(Math.Clamp(Vector2.Dot(sweep, d) * SkewGain, -SkewMax, SkewMax));
        return new Steering(Rotate(d, turn) * (StrokeChord * .5f), bend, skew);
    }

    internal static float PackShape(int bend, int skew) => PackPair(bend, skew, ShapeHalf);
    internal static bool TryUnpackShape(float packed, out int bend, out int skew)
        => TryUnpackPair(packed, ShapeHalf, out bend, out skew) && Math.Abs(skew) <= SkewMax;
    internal static bool ValidHalf(Vector2 half)
        => float.IsFinite(half.X) && float.IsFinite(half.Y) && MathF.Abs(half.Length() - StrokeChord * .5f) <= .5f;

    // The quadratic centred on `center` (the chord's midpoint): A = center - half, B = center + half, bulging `bend` px
    // from the chord (the control sits twice as far out) and leaning `skew` px toward B.
    internal static BatonCurve Curve(Vector2 center, Vector2 half, int bend, int skew)
    {
        float length = half.Length();
        Vector2 d = length > 1e-3f ? half / length : Vector2.UnitX;
        return new BatonCurve(center - half, center + Perp(d) * (2f * bend) + d * skew, center + half);
    }

    // ---- Sampling: the one polyline that is both drawn and collided -----------------------------------------
    internal const int MaxStrokeSamples = 48;
    internal const float StrokeSpacing = 12, StrokeTurn = 10;     // px between samples, degrees of turn per sample
    internal static int SampleStroke(in BatonCurve curve, Span<Vector2> points, Span<float> along)
    {
        int n = SampleCubic(curve.A, curve.C1, curve.C2, curve.B, StrokeSpacing, StrokeTurn, points[..Math.Min(points.Length, MaxStrokeSamples)], 0, false);
        Measure(points, along, 0, n);
        return n;
    }

    // Appends an adaptive sampling of the cubic p0..p3 (each step at most `spacing` px and `turnDegrees` of turn) after
    // `count` points; skips p0 when `skipFirst` (it is already the last point). Returns the new count.
    internal static int SampleCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float spacing, float turnDegrees,
        Span<Vector2> points, int count, bool skipFirst)
    {
        if (count >= points.Length) return count;
        if (!skipFirst) points[count++] = p0;
        float cosLimit = MathF.Cos(turnDegrees * MathF.PI / 180);
        float s = 0, step = .125f;
        Vector2 at = p0, tangent = CubicTangent(p0, p1, p2, p3, 0);
        while (s < 1 && count < points.Length)
        {
            float next = Math.Min(1, s + step);
            Vector2 q = Cubic(p0, p1, p2, p3, next), tq = CubicTangent(p0, p1, p2, p3, next);
            bool fine = Vector2.Distance(at, q) <= spacing && Vector2.Dot(tangent, tq) >= cosLimit;
            if (!fine && step > 1f / 1024) { step *= .5f; continue; }
            points[count++] = q;
            s = next; at = q; tangent = tq;
            if (fine) step = Math.Min(.25f, step * 1.5f);
        }
        if (s < 1 && count > 0) points[count - 1] = p3; // out of room: the path still ends where the curve ends
        return count;
    }
    internal static Vector2 Cubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float s)
    {
        float r = 1 - s;
        return r * r * r * p0 + 3 * r * r * s * p1 + 3 * r * s * s * p2 + s * s * s * p3;
    }
    // Unit tangent; falls back to the chord where a control point coincides with its end.
    internal static Vector2 CubicTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float s)
    {
        float r = 1 - s;
        Vector2 d = 3 * r * r * (p1 - p0) + 6 * r * s * (p2 - p1) + 3 * s * s * (p3 - p2);
        if (d.LengthSquared() < 1e-8f) d = p3 - p0;
        float length = d.Length();
        return length > 1e-6f ? d / length : Vector2.UnitX;
    }
    // Cumulative arc length of points[first..first+count) into along.
    internal static float Measure(ReadOnlySpan<Vector2> points, Span<float> along, int first, int count)
    {
        float total = 0;
        for (int i = first; i < first + count; i++)
        {
            if (i > first) total += Vector2.Distance(points[i - 1], points[i]);
            along[i] = total;
        }
        return total;
    }

    // ---- Writing, ignition and the score ----------------------------------------------------------------------
    // Clock: ticks since the gesture's first tick (the stroke spawns then); the shared ink clock (time + draw fraction)
    // for drawing, the whole tick for collision.
    internal static float WriteHead(float clock) => Math.Clamp((clock - WriteStart) / WriteTicks, 0, 1);
    // Ticks since the head passed fraction `along` of the stroke (negative: not written yet).
    internal static float SinceWritten(float clock, float along) => clock - WriteStart - WriteTicks * along;
    internal static float NaturalOpacity(float clock) => Math.Clamp((StrokeLife - clock) / StrokeFade, 0, 1);
    // A ninth stroke dries the oldest away over StrokeDryAway ticks, starting from the opacity it has (no flash back up).
    internal static int DryAwayStart(float opacity) => (int)Math.Clamp(MathF.Ceiling(StrokeDryAway * (1 - Math.Clamp(opacity, 0, 1))), 0, StrokeDryAway);
    internal static float DryOpacity(float clock) => Math.Clamp(1 - clock / StrokeDryAway, 0, 1);
    // How many of the oldest dormant strokes dry away when a new one is written.
    internal static int Evictions(int dormant) => Math.Max(0, dormant + 1 - MaxStrokes);

    // The tutti: rank i ignites at IgniteTick(i). The countdown is set in the owner's item use, before the strokes'
    // own update of that tick counts it down once, so it carries one extra tick.
    internal static int Countdown(int rank) => IgniteTick(rank) + 1;
    internal static int IgniteEnd => IgniteSwell + IgniteLive;                  // 17
    // Ignition swells the radius from IgniteRadiusFrom to IgniteRadius over IgniteSwell ticks. The material opens a
    // live point by InkOpen(time); this time puts that opening exactly on the swell (time 1 opens a third: 8 of 24).
    internal static float IgniteShaderTime(float t)
    {
        float from = IgniteRadiusFrom / IgniteRadius * InkOpenTicks; // 1
        return t >= IgniteSwell ? t : from + Math.Max(0, t) * (IgniteSwell - from) / IgniteSwell;
    }
    internal static float IgniteRadiusAt(float t) => IgniteRadius * InkOpen(IgniteShaderTime(t));
    internal static bool RiverFor(int strokes) => strokes == MaxStrokes;
    internal const int RiverDryStart = RiverHeadTicks + RiverLive;              // 38: the last point's live window ends
    internal const int RiverEnd = RiverDryStart + IgniteScar;                    // 62: the whole score has dried
    internal static int RiverSpawnAge => -RiverTick - 1;                         // counted up by the cast tick's own update
    // When an ignited stroke dies: a partial score dries each stroke on its own; a full score holds every scar until
    // the river has run and dries it with the river.
    internal static int StrokeEnd(int rank, bool full) => full ? RiverTick + RiverEnd - IgniteTick(rank) : IgniteEnd + IgniteScar;
    internal static float ScarFade(float t, int rank, bool full)
    {
        if (!full) return Math.Clamp(1 - (t - IgniteEnd) / IgniteScar, 0, 1);
        float river = t + IgniteTick(rank) - RiverTick;
        return Math.Clamp(1 - (river - RiverDryStart) / IgniteScar, 0, 1);
    }

    // Commitment: does a stroke stay when its owner is dead or Down? Death clears every stroke that is not burning yet
    // (the build and the parts that have not started); Down kills only the parts that have not started. Live parts
    // always finish.
    internal static bool Survives(CrimsonStrokeKind kind, bool dead, bool down) => kind switch
    {
        CrimsonStrokeKind.Live => true,
        CrimsonStrokeKind.Scheduled => !dead && !down,
        _ => !dead,
    };
    internal static bool RiverSurvives(float age, bool dead, bool down) => age >= 0 || (!dead && !down);

    // ---- Ink spans -------------------------------------------------------------------------------------------
    // A dormant (not yet released) stroke at `clock`: nothing before the pen starts; while written, the dried part
    // and the live part behind the bead; then the dried stroke (fading at the end of its life). `warmth`: the full
    // score's steady lip heat.
    internal static int DormantSpans(float clock, float length, float warmth, Span<BatonInkSpan> spans)
    {
        if (clock < WriteStart || length <= 0) return 0;
        int n = 0;
        float head = length * WriteHead(clock);
        float dry = length * Math.Clamp((clock - WriteStart - WriteLive) / WriteTicks, 0, 1);
        if (dry > 0) spans[n++] = new BatonInkSpan(BatonLook.Dormant, 0, dry, WriteRadius, 0, 0, NaturalOpacity(clock), warmth);
        if (head > dry)
            spans[n++] = new BatonInkSpan(BatonLook.Live, dry, head, WriteRadius, clock - WriteStart, -WriteTicks / length, 1, 0, clock < WriteEnd);
        return n;
    }
    // A stroke waiting for its ignition: its lips heat through the baton's lift.
    internal static BatonInkSpan ScheduledSpan(float length, float sinceCast, float warmth)
        => new(BatonLook.Dormant, 0, length, WriteRadius, 0, 0, 1, Math.Max(warmth, Math.Clamp(sinceCast / TuttiLift, 0, 1)));
    // An ignited stroke `t` ticks after its ignition: the swelling live river, then its scar.
    internal static BatonInkSpan IgnitedSpan(float t, float length, int rank, bool full)
        => t < IgniteEnd
            ? new BatonInkSpan(BatonLook.Live, 0, length, IgniteRadius, IgniteShaderTime(t))
            : new BatonInkSpan(BatonLook.Residue, 0, length, IgniteRadius, ScarFade(t, rank, full));
    // A stroke drying away (a ninth was written).
    internal static BatonInkSpan DryingSpan(float clock, float length)
        => new(BatonLook.Dormant, 0, length, WriteRadius, 0, 0, DryOpacity(clock));
    // The river `age` ticks after it set off: the live stretch behind its head and the held, then drying, scar behind.
    internal static int RiverSpans(float age, float length, Span<BatonInkSpan> spans)
    {
        if (age < 0 || length <= 0) return 0;
        int n = 0;
        float head = length * Math.Clamp(age / RiverHeadTicks, 0, 1);
        float tail = length * Math.Clamp((age - RiverLive) / RiverHeadTicks, 0, 1);
        float fade = Math.Clamp(1 - (age - RiverDryStart) / IgniteScar, 0, 1);
        if (tail > 0 && fade > 0) spans[n++] = new BatonInkSpan(BatonLook.Residue, 0, tail, RiverRadius, fade);
        if (head > tail) spans[n++] = new BatonInkSpan(BatonLook.Live, tail, head, RiverRadius, age, -RiverHeadTicks / length);
        return n;
    }

    // ---- Collision: exactly the live footprint that is drawn -------------------------------------------------
    // Does the live span touch the box? The polyline is clipped to [From, To]; each point's radius is the span radius
    // opened by InkOpen(time), linear between samples, tested as short capsules no more than 1 px apart in radius.
    // Segments that start a new stretch (breaks[i]: point i begins one) are not joined.
    internal static bool SpanTouchesBox(ReadOnlySpan<Vector2> points, ReadOnlySpan<float> along, ReadOnlySpan<bool> breaks, int count,
        in BatonInkSpan span, Vector2 min, Vector2 max)
    {
        if (span.Look != BatonLook.Live || span.To <= span.From || count < 1) return false;
        if (count == 1) return DiscTouchesBox(points[0], span.Radius * InkOpen(span.TimeAt(along[0])), min, max);
        for (int i = 1; i < count; i++)
        {
            if (!breaks.IsEmpty && breaks[i]) continue;
            float u0 = along[i - 1], u1 = along[i];
            if (u1 < span.From || u0 > span.To) continue;
            float a = Math.Max(u0, span.From), b = Math.Min(u1, span.To);
            if (b < a) continue;
            float length = u1 - u0;
            Vector2 pa = length > 1e-5f ? Vector2.Lerp(points[i - 1], points[i], (a - u0) / length) : points[i - 1];
            Vector2 pb = length > 1e-5f ? Vector2.Lerp(points[i - 1], points[i], (b - u0) / length) : points[i];
            // The opening (InkOpen) bends the radius once, at time InkOpenTicks: split there so each piece is linear.
            float ta = span.TimeAt(a), tb = span.TimeAt(b);
            if (span.TimeSlope != 0 && (ta - InkOpenTicks) * (tb - InkOpenTicks) < 0)
            {
                float u = (InkOpenTicks - span.Time0) / span.TimeSlope, f = (u - a) / (b - a);
                Vector2 pm = Vector2.Lerp(pa, pb, f);
                if (TaperTouchesBox(pa, pm, span.Radius * InkOpen(ta), span.Radius, min, max)
                    || TaperTouchesBox(pm, pb, span.Radius, span.Radius * InkOpen(tb), min, max)) return true;
            }
            else if (TaperTouchesBox(pa, pb, span.Radius * InkOpen(ta), span.Radius * InkOpen(tb), min, max)) return true;
        }
        return false;
    }
    // A capsule whose radius runs linearly from ra to rb, as pieces whose radii differ by at most 1 px (each tested
    // with its larger end radius: never more than 1 px beyond the drawn rim).
    internal static bool TaperTouchesBox(Vector2 a, Vector2 b, float ra, float rb, Vector2 min, Vector2 max)
    {
        float reach = Math.Max(ra, rb);
        if (BoxDistance(a, min, max) > Vector2.Distance(a, b) + reach) return false;
        int pieces = Math.Clamp((int)MathF.Ceiling(MathF.Abs(ra - rb)), 1, 32);
        for (int k = 0; k < pieces; k++)
        {
            float s0 = k / (float)pieces, s1 = (k + 1) / (float)pieces;
            float r = Math.Max(ra + (rb - ra) * s0, ra + (rb - ra) * s1);
            if (r > 0 && CapsuleTouchesBox(Vector2.Lerp(a, b, s0), Vector2.Lerp(a, b, s1), r, min, max)) return true;
        }
        return false;
    }

    // ---- The Black-Blood River ------------------------------------------------------------------------------
    internal const float RunArm = 32;          // runs leave each stroke along its end and meet the next along its start
    internal const float RunSpacing = 64, RunTurn = 12;
    // The river through every stroke in writing order: stroke i's samples, then a run from its end to stroke i+1's
    // start, unless that run is longer than RiverMaxRun: then it is left out, and the river continues from stroke i+1
    // (breaks marks the first point of each new stretch). The run is straight but for its last RunArm px at either end,
    // where it bends into the strokes' own directions so the strip never folds at a joint. At most RiverMaxVertices
    // points; along is the arc length over the included parts only, so the head skips an omitted run. Returns the count.
    internal static int BuildRiver(ReadOnlySpan<BatonCurve> strokes, Span<Vector2> points, Span<float> along, Span<bool> breaks, out float length)
    {
        length = 0;
        int limit = Math.Min(Math.Min(points.Length, along.Length), Math.Min(breaks.Length, RiverMaxVertices));
        float spacing = StrokeSpacing, runSpacing = RunSpacing;
        for (int attempt = 0; attempt < 4; attempt++, spacing *= 1.5f, runSpacing *= 2)
        {
            int count = BuildRiverOnce(strokes, points[..limit], breaks[..limit], spacing, runSpacing, out bool fits);
            if (!fits && attempt < 3) continue;
            length = Measure(points, along, 0, count);
            // Breaks add no length: the head jumps the omitted run.
            float removed = 0;
            for (int i = 1; i < count; i++)
            {
                if (breaks[i]) removed += Vector2.Distance(points[i - 1], points[i]);
                along[i] -= removed;
            }
            length -= removed;
            return count;
        }
        return 0;
    }

    private static int BuildRiverOnce(ReadOnlySpan<BatonCurve> strokes, Span<Vector2> points, Span<bool> breaks, float spacing, float runSpacing, out bool fits)
    {
        int count = 0;
        fits = true;
        for (int i = 0; i < strokes.Length; i++)
        {
            var c = strokes[i];
            if (i > 0)
            {
                var previous = strokes[i - 1];
                Vector2 from = previous.B, to = c.A;
                float gap = Vector2.Distance(from, to);
                if (gap > RiverMaxRun)
                {
                    if (count < points.Length) { breaks[count] = true; points[count++] = to; }
                }
                else if (gap > .5f)
                {
                    float arm = Math.Min(RunArm, gap * .4f);
                    Vector2 leave = Direction(previous.B - previous.C2, to - from), arrive = Direction(c.C1 - c.A, to - from);
                    int before = count;
                    count = SampleCubic(from, from + leave * arm, to - arrive * arm, to, runSpacing, RunTurn, points, count, true);
                    for (int k = before; k < count; k++) breaks[k] = false;
                }
            }
            else if (count < points.Length) { breaks[count] = false; points[count++] = c.A; }
            int start = count;
            count = SampleCubic(c.A, c.C1, c.C2, c.B, spacing, StrokeTurn, points, count, true);
            for (int k = start; k < count; k++) breaks[k] = false;
            if (count >= points.Length && i < strokes.Length - 1) { fits = false; return count; }
        }
        return count;
    }

    private static Vector2 Direction(Vector2 v, Vector2 fallback)
    {
        if (v.LengthSquared() < 1e-6f) v = fallback;
        float length = v.Length();
        return length > 1e-6f ? v / length : Vector2.UnitX;
    }

    // ---- Gesture motion ---------------------------------------------------------------------------------------
    // The baton tip (px about the shoulder) for one gesture: anticipation, sweep and follow-through about the grip,
    // as a path through the gesture's ictus and its rebound hook, travelled at a speed that never stops. The pattern:
    //   Down  falls from the top to the low ictus and rebounds up and in
    //   In    curls back toward the body, rebounds up and turns out
    //   Out   swings away, rebounds up and turns back
    //   Up    rises to the top and arches forward over it into the next Down
    //   Tutti lifts high (0-5) and gives the downbeat at tick 8, then holds the sustain while the strokes burn
    // A gesture starts exactly where the previous one ended, moving the way it moved at JoinSpeed (the previous
    // gesture and its aim come with the swing); from rest it draws back against its sweep at JoinSpeed.
    private static readonly Vector2[] IctusAt = { new(40, 14), new(22, 8), new(58, 10), new(38, -28), new(46, 14) };
    private static readonly Vector2[] SweepAt = { Unit(.12f, 1), Unit(-1, .3f), Unit(1, .3f), Unit(-.25f, -1), Unit(.12f, 1) };
    private static readonly float[] Overshoot = { 6, 5, 5, 5, 14 };
    private static readonly Vector2[] HookAt = { new(31, 17), new(18, -1), new(63, 0), new(35, -39), new(36, 22) };
    private static readonly Vector2[] EndAt = { new(31, 2), new(26, -4), new(57, -6), new(42, -37), new(37, -2) };
    internal static readonly Vector2 RestAt = new(28, -8), ApexAt = new(40, -30), ApexDirection = Vector2.UnitX;
    internal const float JoinSpeed = 1.2f, IctusSpeed = 8, TuttiIctusSpeed = 12, ApexSpeed = 2;
    internal const float LeadArm = 8, ApproachArm = 12, ApexArm = 10, DropArm = 24, ShortApproach = 1.5f;
    internal const int TuttiApex = 5;
    internal const float FrameTiltDegrees = 40;
    private static Vector2 Unit(float x, float y) => Vector2.Normalize(new Vector2(x, y));

    internal static Vector2 IctusPoint(int gesture) => IctusAt[Math.Clamp(gesture, Down, Tutti)];
    internal static Vector2 EndPoint(int gesture) => EndAt[Math.Clamp(gesture, Down, Tutti)];

    internal static BatonFrame FrameFor(float aim)
    {
        float x = MathF.Cos(aim), y = MathF.Sin(aim);
        int facing = x < 0 ? -1 : 1;
        float limit = FrameTiltDegrees * MathF.PI / 180;
        return new BatonFrame(Math.Clamp(MathF.Atan2(y, MathF.Abs(x)), -limit, limit), facing);
    }

    // The tip at `time` ticks into `gesture` (0..Duration), continuing from `previous` (Rest: from the rest pose).
    internal static Vector2 Tip(int gesture, int previous, float previousAim, float aim, float time)
    {
        gesture = Math.Clamp(gesture, Down, Tutti);
        var frame = FrameFor(aim);
        Vector2 start, lead;
        float v0;
        if (previous is >= Down and <= Tutti)
        {
            var before = FrameFor(previousAim);
            start = before.World(EndAt[previous]);
            lead = Direction(before.World(EndAt[previous] - HookAt[previous]), Vector2.UnitX);
            v0 = JoinSpeed;
        }
        else
        {
            // From rest the baton appears drawing back against its sweep (the anticipation), already moving.
            start = frame.World(RestAt);
            lead = -frame.World(SweepAt[gesture]);
            v0 = JoinSpeed;
        }
        Vector2 ictus = frame.World(IctusAt[gesture]), sweep = frame.World(SweepAt[gesture]);
        Vector2 f0 = ictus, f1 = ictus + sweep * Overshoot[gesture], f2 = frame.World(HookAt[gesture]), f3 = frame.World(EndAt[gesture]);
        float follow = CubicLength(f0, f1, f2, f3);
        time = Math.Clamp(time, 0, Duration(gesture));
        if (gesture != Tutti)
        {
            Vector2 a1 = start + lead * LeadArm, a2 = ictus - sweep * ApproachArm;
            float approach = CubicLength(start, a1, a2, ictus);
            // A beat that starts close to its ictus, or must turn back on itself (a turned frame), loops wider instead of
            // slowing below JoinSpeed or folding into a hairpin.
            for (float widen = 1.5f; Cramped(start, a1, a2, ictus, approach, v0 * Ictus) && widen <= 6; widen *= 1.5f)
            {
                a1 = start + lead * LeadArm * widen; a2 = ictus - sweep * ApproachArm * widen;
                approach = CubicLength(start, a1, a2, ictus);
            }
            float vI = Math.Max(IctusSpeed, Math.Max(MinRise(v0, Ictus, approach), MinFall(JoinSpeed, GestureTicks - Ictus, follow)));
            return time <= Ictus
                ? CubicAt(start, a1, a2, ictus, approach, Rise(v0, vI, Ictus, approach, time))
                : CubicAt(f0, f1, f2, f3, follow, Fall(vI, JoinSpeed, GestureTicks - Ictus, follow, time - Ictus));
        }
        Vector2 apex = frame.World(ApexAt), across = frame.World(ApexDirection);
        Vector2 l1 = start + lead * LeadArm, l2 = apex - across * ApexArm, d1 = apex + across * ApexArm, d2 = ictus - sweep * DropArm;
        float lift = CubicLength(start, l1, l2, apex), drop = CubicLength(apex, d1, d2, ictus);
        for (float widen = 1.5f; Cramped(start, l1, l2, apex, lift, 0) && widen <= 6; widen *= 1.5f)
        {
            l1 = start + lead * LeadArm * widen; l2 = apex - across * ApexArm * widen;
            lift = CubicLength(start, l1, l2, apex);
        }
        int dropTicks = TuttiLift - TuttiApex, sustain = TuttiTicks - TuttiLift;
        float vT = Math.Max(TuttiIctusSpeed, Math.Max(MinRise(ApexSpeed, dropTicks, drop), MinFall(JoinSpeed, sustain, follow)));
        if (time <= TuttiApex) return CubicAt(start, l1, l2, apex, lift, Glide(v0, ApexSpeed, TuttiApex, lift, time));
        if (time <= TuttiLift) return CubicAt(apex, d1, d2, ictus, drop, Rise(ApexSpeed, vT, dropTicks, drop, time - TuttiApex));
        return CubicAt(f0, f1, f2, f3, follow, Fall(vT, JoinSpeed, sustain, follow, time - TuttiLift));
    }

    // Distance-in-time profiles (px covered after t of T ticks along a path of length L). Each one's speed is v0 at
    // the start and v1 at the end and never falls below the smaller of them; the minimum ictus speeds keep them so.
    internal static float MinRise(float v0, float ticks, float length) => v0 + 2 * (length - v0 * ticks) / ticks;
    internal static float MinFall(float v1, float ticks, float length) => v1 + 2 * (length - v1 * ticks) / ticks;
    // Accelerating to the ictus: the speed climbs as a power of time.
    internal static float Rise(float v0, float v1, float ticks, float length, float t)
    {
        float s = Math.Clamp(t / ticks, 0, 1), extra = length - v0 * ticks;
        if (extra <= 1e-3f || v1 <= v0) return length * s;
        float p = Math.Max(1, (v1 - v0) * ticks / extra - 1);
        return v0 * ticks * s + extra * MathF.Pow(s, p + 1);
    }
    // The rebound: braking hard out of the ictus, then drifting on at the join speed.
    internal static float Fall(float v0, float v1, float ticks, float length, float t)
    {
        float s = Math.Clamp(t / ticks, 0, 1), extra = length - v1 * ticks;
        if (extra <= 1e-3f || v0 <= v1) return length * s;
        float q = Math.Max(1, (v0 - v1) * ticks / extra - 1);
        return v1 * ticks * s + extra * (1 - MathF.Pow(1 - s, q + 1));
    }
    // The tutti's lift: from v0 to v1 with a smooth swell between (an inhale), falling back to even speed when the
    // path is too short to swell.
    internal static float Glide(float v0, float v1, float ticks, float length, float t)
    {
        float s = Math.Clamp(t / ticks, 0, 1);
        float k = (length - ticks * (v0 + v1) * .5f) * 30f / 16f;
        if (k < -ticks * (v0 + v1) * .5f) return length * s;
        return ticks * (v0 * s + (v1 - v0) * s * s * .5f) + k * 16 * (s * s * s / 3 - s * s * s * s / 2 + s * s * s * s * s / 5);
    }

    // Too short for the speed it must keep, or turning back so tightly that its parameter speed nearly vanishes (a
    // hairpin): the path's own tangent never falls below a fifth of its average.
    private static bool Cramped(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float length, float minimum)
    {
        if (length < ShortApproach * minimum) return true;
        for (int i = 0; i <= 32; i++)
        {
            float s = i / 32f, r = 1 - s;
            Vector2 d = 3 * r * r * (p1 - p0) + 6 * r * s * (p2 - p1) + 3 * s * s * (p3 - p2);
            if (d.Length() < .2f * length) return true;
        }
        return false;
    }

    private const int ArcSteps = 128;
    internal static float CubicLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float total = 0;
        Vector2 previous = p0;
        for (int i = 1; i <= ArcSteps; i++)
        {
            Vector2 q = Cubic(p0, p1, p2, p3, i / (float)ArcSteps);
            total += Vector2.Distance(previous, q);
            previous = q;
        }
        return total;
    }
    // The point `distance` px along the cubic (by the same fine chords CubicLength measures).
    internal static Vector2 CubicAt(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float length, float distance)
    {
        if (distance <= 0) return p0;
        if (distance >= length) return p3;
        float walked = 0;
        Vector2 previous = p0;
        for (int i = 1; i <= ArcSteps; i++)
        {
            Vector2 q = Cubic(p0, p1, p2, p3, i / (float)ArcSteps);
            float step = Vector2.Distance(previous, q);
            // On the curve itself (not the chord), so the motion leaves and meets each end along the true tangent.
            if (walked + step >= distance) return Cubic(p0, p1, p2, p3, (i - 1 + (step > 1e-6f ? (distance - walked) / step : 1)) / ArcSteps);
            walked += step;
            previous = q;
        }
        return p3;
    }
}
