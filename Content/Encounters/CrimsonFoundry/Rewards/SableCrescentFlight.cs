#nullable enable
using System;
using System.Numerics;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Pure rules for the Sable Scythe's crescents (刈り月; docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable Scythe",
// Crescents): the throws, the body (draw equals collide), the steering, the targets, the lifetime, the replicated state
// and the owner's throw/engraving ledger. Terraria-free like QuillFlight: the domain tests and the offline preview link
// this file, and SableCrescent (ScarletScythe.cs) and the client visuals call it. Constants are per game tick and live in
// CrimsonRewardRules; a crescent has one extra update and steps them with dt = 1/2, as RitualArmamentRules.Steer does.
//
// Kinds: 0 the Over's crescent, 1 the Under's, 2-6 volley crescent 0-4 (shed by the Whip's lash arc).
internal static class SableCrescentFlight
{
    internal const int Over = 0, Under = 1, FirstVolley = 2, Kinds = FirstVolley + R.VolleyCrescents, StateStride = 8;
    internal static bool IsVolley(int kind) => kind >= FirstVolley;
    internal static int VolleyIndex(int kind) => Math.Clamp(kind - FirstVolley, 0, R.VolleyCrescents - 1);
    internal static float Width(int kind) => IsVolley(kind) ? R.VolleyWidth : R.CrescentWidth;
    internal static float Depth(int kind) => IsVolley(kind) ? R.VolleyDepth : R.CrescentDepth;
    internal static float Radius(int kind) => IsVolley(kind) ? R.VolleyRadius : R.CrescentRadius;
    internal static float Multiplier(int kind) => IsVolley(kind) ? R.VolleyMultiplier : R.CrescentMultiplier;
    internal static float LaunchSpeed(int kind) => IsVolley(kind) ? R.VolleySpeed : R.CrescentThrowSpeed;
    // Free flight: the heading is held for 6 ticks; volley crescent k holds it for 6 + 2|k - 2| (10, 8, 6, 8, 10), so a
    // crescent visibly leaves the blade, or the volley fans out, before anything turns.
    internal static int Hold(int kind) => R.CrescentHold + (IsVolley(kind) ? R.VolleyHoldStep * Math.Abs(VolleyIndex(kind) - 2) : 0);
    internal static float Dt => 1f / (1 + R.CrescentExtraUpdates);
    internal static float Radians(float degrees) => degrees * MathF.PI / 180f;
    internal static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    internal static float Wrap(float angle) => MathF.IEEERemainder(angle, MathF.Tau);

    // ---- Throws ---------------------------------------------------------------------------------------------------
    // An Over or Under throws at stroke age 9 (CrescentThrowAge), from the hook tip, along the aim turned 14 degrees toward
    // the side the cut travels: down for the Over, up for the Under, in the aim frame (mirrored by facing, as
    // SableScytheMotion.ToWorld mirrors it). The two therefore leave on opposite sides of the aim and cross on the target.
    internal static float ThrowHeading(float aim, int facing, int kind)
        => aim + (facing < 0 ? -1 : 1) * (kind == Under ? -1 : 1) * Radians(R.CrescentThrowDegrees);

    // Volley crescent k leaves the lash arc point written at 14 + 6 x (0.1 + 0.2k): 14.6, 15.8, 17, 18.2, 19.4.
    internal static float VolleyWritten(int k)
        => R.WhipLiveStart + (R.WhipLiveEnd - R.WhipLiveStart) * (R.VolleyWrittenFirst + R.VolleyWrittenStep * Math.Clamp(k, 0, R.VolleyCrescents - 1));

    // The point of a written arc at time `written`, from its samples (sample i written at WhipLiveStart + i / perTick),
    // interpolated between the two samples around it (the arc as this client wrote it).
    internal static Vector2 ArcPoint(ReadOnlySpan<Vector2> arc, float written, int perTick)
    {
        if (arc.Length == 0) return Vector2.Zero;
        float at = Math.Clamp((written - R.WhipLiveStart) * perTick, 0, arc.Length - 1);
        int i = Math.Min((int)at, arc.Length - 1), j = Math.Min(i + 1, arc.Length - 1);
        return Vector2.Lerp(arc[i], arc[j], at - i);
    }

    // Volley crescent k heads along the aim turned (2 - k) x 16 degrees toward the side of the aim where the arc began
    // (`first`) relative to where it ended (`last`): the fan opens the way the arc was written, so neighbours stay
    // neighbours and their paths do not cross as they leave.
    internal static float VolleyHeading(float aim, int k, Vector2 first, Vector2 last)
    {
        Vector2 normal = new(-MathF.Sin(aim), MathF.Cos(aim));
        float side = Vector2.Dot(first - last, normal) < 0 ? -1 : 1;
        return aim + side * (2 - Math.Clamp(k, 0, R.VolleyCrescents - 1)) * Radians(R.VolleyFanDegrees);
    }

    // A crescent is born with its apex on the hook tip (or the arc point); its centre lies halfway between apex and chord.
    internal static Vector2 CenterFromApex(Vector2 apex, Vector2 heading, int kind) => apex - heading * (Depth(kind) * .5f);
    internal static Vector2 Apex(Vector2 center, Vector2 heading, int kind) => center + heading * (Depth(kind) * .5f);

    // The ticks of one held measure (strokes back to back from tick 0) at which crescents leave: each Over and Under
    // at its start + 9, the volley's five at the Whip's start + 20. Returns the count written (9).
    internal static int MeasureThrows(Span<int> ticks)
    {
        int count = 0, start = 0;
        for (int stroke = 0; stroke < SableScytheMotion.Strokes; stroke++)
        {
            if (SableScytheMotion.Kind(stroke) == SableScytheMotion.Whip)
                for (int k = 0; k < R.VolleyCrescents && count < ticks.Length; k++) ticks[count++] = start + R.VolleyAge;
            else if (count < ticks.Length) ticks[count++] = start + R.CrescentThrowAge;
            start += SableScytheMotion.Duration(stroke);
        }
        return count;
    }

    // ---- Body (draw equals collide) -------------------------------------------------------------------------------
    // An arc of live black blood, convex side forward. In the crescent's frame (f along its heading, n across it) sample
    // i = 0-6 at s = -1 + i/3 lies at Center + f (h(1 - s^2) - h/2) + n s w/2, with radius R (1 - 0.65 s^2) x open.
    internal static int ApexSample => R.CrescentSamples / 2;
    internal static float SampleS(int i) => -1 + i * 2f / (R.CrescentSamples - 1);
    internal static void Body(Vector2 center, Vector2 heading, int kind, float open, Span<Vector2> at, Span<float> radius)
    {
        Vector2 n = new(-heading.Y, heading.X);
        float w = Width(kind), h = Depth(kind), r = Radius(kind) * Math.Clamp(open, 0, 1);
        int count = Math.Min(R.CrescentSamples, Math.Min(at.Length, radius.Length));
        for (int i = 0; i < count; i++)
        {
            float s = SampleS(i);
            at[i] = center + heading * (h * (1 - s * s) - h * .5f) + n * (s * w * .5f);
            radius[i] = r * (1 - (1 - R.CrescentTipRadius) * s * s);
        }
    }
    // Collision: the six capsules between consecutive samples, each with its smaller end's radius, after a disc
    // broadphase of radius w/2 + R + 8 about the centre.
    internal static float Broadphase(int kind) => Width(kind) * .5f + Radius(kind) + 8;
    internal static bool Touches(Vector2 center, int kind, ReadOnlySpan<Vector2> at, ReadOnlySpan<float> radius, Vector2 min, Vector2 max)
    {
        if (R.BoxDistance(center, min, max) > Broadphase(kind)) return false;
        for (int i = 0; i + 1 < at.Length && i + 1 < radius.Length; i++)
        {
            float r = MathF.Min(radius[i], radius[i + 1]);
            if (r > 0 && R.CapsuleTouchesBox(at[i], at[i + 1], r, min, max)) return true;
        }
        return false;
    }

    // ---- Steering ---------------------------------------------------------------------------------------------------
    // The turn limit: w = 0.16 x Smooth((age - hold) / 10) rad/tick, nothing while the heading is held.
    internal static float TurnLimit(float age, int kind)
        => age <= Hold(kind) ? 0 : R.CrescentTurnRate * R.Smooth((age - Hold(kind)) / R.CrescentTurnRamp);
    // The speed it eases toward: 20 x (0.6 + 0.4 max(0, cos err)), so 12 px/tick in a hard turn (tightest curve 75 px).
    internal static float CruiseFor(float error) => R.CrescentCruise * (R.CrescentHardTurn + (1 - R.CrescentHardTurn) * MathF.Max(0, MathF.Cos(error)));
    internal static float TightestCurve => R.CrescentCruise * R.CrescentHardTurn / R.CrescentTurnRate; // 75
    // The aim point: the target's centre plus its velocity x clamp(distance / speed, 0, 8).
    internal static Vector2 AimPoint(Vector2 from, Vector2 targetCenter, Vector2 targetVelocity, float speed)
    {
        float lead = speed > 1e-3f ? Math.Clamp(Vector2.Distance(from, targetCenter) / speed, 0, R.CrescentLeadMax) : 0;
        return targetCenter + targetVelocity * lead;
    }
    // One update of a flying crescent. `velocity` is per game tick in and out; `aim` the point it steers for (none: it
    // flies straight). The heading turns by clamp(0.3 err, -w, w) per tick and the speed eases toward its cruise, both
    // scaled by dt. While the heading is held the speed eases toward the plain cruise.
    // Turning circle: an aim point behind the beam and inside the tightest circle the crescent can turn on (75 px, on the
    // side of the turn) cannot be reached by turning, so the crescent holds its heading (slowing as for a hard turn) until
    // the point leaves that circle, then turns: one that overshoots loops out and comes back instead of orbiting.
    internal static Vector2 Steer(Vector2 velocity, Vector2 position, float age, int kind, Vector2? aim, float dt)
    {
        if (!Finite(velocity) || !Finite(position) || !(dt > 0)) return Vector2.Zero;
        float speed = velocity.Length();
        float heading = speed > 1e-4f ? MathF.Atan2(velocity.Y, velocity.X) : 0;
        float wanted = R.CrescentCruise, limit = TurnLimit(age, kind);
        if (aim is { } point && limit > 0 && Finite(point))
        {
            Vector2 d = point - position;
            if (d.LengthSquared() > 1e-4f)
            {
                float error = Wrap(MathF.Atan2(d.Y, d.X) - heading);
                wanted = CruiseFor(error);
                if (!InsideTurn(d, heading, error))
                    heading += Math.Clamp(R.CrescentTurnGain * error * dt, -limit * dt, limit * dt);
            }
        }
        speed += (wanted - speed) * (1 - MathF.Exp(-R.CrescentSpeedEase * dt));
        return Unit(heading) * speed;
    }
    // Is the point `d` (from the crescent) behind its beam (more than 90 degrees off the heading) and strictly inside the
    // tightest circle it can turn on toward it? Such a point cannot be reached by turning. Ahead of the beam the crescent
    // always turns: turning brings a point ahead closer, so the hold never alternates with the turn into an orbit.
    internal static bool InsideTurn(Vector2 d, float heading, float error)
    {
        if (MathF.Abs(error) <= MathF.PI * .5f) return false;
        float radius = TightestCurve;
        Vector2 side = Unit(heading + (error < 0 ? -1 : 1) * MathF.PI * .5f);
        return Vector2.DistanceSquared(d, side * radius) < radius * radius;
    }
    // A breaking crescent no longer steers: it slows into the wound, to 30% of its speed over the 6-tick close.
    internal static Vector2 Brake(Vector2 velocity, float dt) => velocity * MathF.Pow(R.CrescentBreakSlow, dt / R.CrescentClose);
    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);

    // ---- Lifetime ---------------------------------------------------------------------------------------------------
    // 70 ticks; a crescent that breaks at age b ends at b + 6 (its close), and one that never breaks closes over 64-70.
    internal static float End(int breakAge) => breakAge > 0 ? MathF.Min(R.CrescentLife, breakAge + R.CrescentClose) : R.CrescentLife;
    // Live ticks left for the whole body (ScarletInkStyle.Remaining: the material's close over the last 6).
    internal static float Remaining(float age, int breakAge) => End(breakAge) - age;
    internal static bool Done(float age, int breakAge) => age >= End(breakAge);
    // The break is recorded in whole ticks (ai is integer): the first whole tick at or after it, at least 1.
    internal static int BreakAgeAt(float age) => Math.Clamp((int)MathF.Ceiling(age), 1, R.CrescentLife);

    // ---- Targets ----------------------------------------------------------------------------------------------------
    // C is the cursor clamped within 960 px of the player.
    internal static Vector2 ClampCursor(Vector2 player, Vector2 cursor)
    {
        Vector2 d = cursor - player;
        float length = d.Length();
        if (!float.IsFinite(length)) return player;
        return length > R.CrescentCursorRange ? player + d / length * R.CrescentCursorRange : cursor;
    }
    // Acquisition, over the candidates' hitboxes (`skip` marks those the caller ruled out: a root this crescent already
    // hit, no line of sight): the first match of (1) the hitbox nearest C within 240 px of C; (2) the hitbox nearest the
    // crescent within 400 px whose centre lies within 75 degrees of its heading; (3) none (-1).
    internal static int Acquire(Vector2 cursor, Vector2 at, Vector2 heading, ReadOnlySpan<Vector2> min, ReadOnlySpan<Vector2> max, ReadOnlySpan<bool> skip)
    {
        int best = Nearest(cursor, min, max, skip, R.CrescentCursorSnap, at, heading, false);
        return best >= 0 ? best : Nearest(at, min, max, skip, R.CrescentConeRange, at, heading, true);
    }
    // The chain after a hit: the nearest candidate within 320 px of the crescent, in any direction.
    internal static int Chain(Vector2 at, ReadOnlySpan<Vector2> min, ReadOnlySpan<Vector2> max, ReadOnlySpan<bool> skip)
        => Nearest(at, min, max, skip, R.CrescentChainRange, at, Vector2.Zero, false);
    // A target is kept while it is within 1,400 px (its hitbox from the crescent).
    internal static bool InKeepRange(Vector2 at, Vector2 min, Vector2 max) => R.BoxDistance(at, min, max) <= R.CrescentKeepRange;
    // Whether a candidate can be the answer of Acquire at all (the caller's cheap pre-filter before line of sight).
    internal static bool InAcquireRange(Vector2 cursor, Vector2 at, Vector2 min, Vector2 max)
        => R.BoxDistance(cursor, min, max) <= R.CrescentCursorSnap || R.BoxDistance(at, min, max) <= R.CrescentConeRange;

    private static int Nearest(Vector2 from, ReadOnlySpan<Vector2> min, ReadOnlySpan<Vector2> max, ReadOnlySpan<bool> skip, float range,
        Vector2 at, Vector2 heading, bool cone)
    {
        float cos = MathF.Cos(Radians(R.CrescentConeDegrees)), best = float.MaxValue;
        int found = -1;
        for (int i = 0; i < min.Length && i < max.Length; i++)
        {
            if (i < skip.Length && skip[i]) continue;
            float d = R.BoxDistance(from, min[i], max[i]);
            if (!(d <= range) || d >= best) continue;
            if (cone)
            {
                Vector2 toward = (min[i] + max[i]) * .5f - at;
                float length = toward.Length();
                if (length > 1e-3f && Vector2.Dot(toward / length, heading) < cos) continue;
            }
            best = d; found = i;
        }
        return found;
    }

    // ---- Live cap -----------------------------------------------------------------------------------------------------
    // At most 8 crescents in flight per owner: a ninth throw breaks the oldest one still flying.
    internal static bool MustBreakOldest(int flying) => flying >= R.MaxCrescents;

    // ---- Replicated state (ai = target slot or -1, kind + 8 x break age, age) ----------------------------------------
    internal static float State(int kind, int breakAge) => Math.Clamp(kind, 0, Kinds - 1) + StateStride * Math.Clamp(breakAge, 0, R.CrescentLife);
    internal static int KindOf(float state) => Math.Clamp((int)state % StateStride, 0, Kinds - 1);
    internal static int BreakAgeOf(float state) => Math.Clamp((int)state / StateStride, 0, R.CrescentLife);
    // ai[0] an integer in -1..199; ai[1] an integer in 0..566 whose kind is at most 6 (its break age at most 70);
    // ai[2] in 0..71; the velocity (per update) finite and at most 16 px. Anything else kills the crescent.
    internal static bool Valid(float target, float state, float age, Vector2 velocityPerUpdate)
        => R.ValidInteger(target, -1, R.MaxRoots - 1)
            && R.ValidInteger(state, 0, Kinds - 1 + StateStride * R.CrescentLife) && (int)state % StateStride < Kinds
            && R.Valid(age, 0, R.CrescentLife + 1)
            && Finite(velocityPerUpdate) && velocityPerUpdate.Length() <= R.CrescentMaxStep;
}

// The owner's crescent bookkeeping (REWARDS.md, "Spacing" and "Engraving and commitment"), never replicated:
// - every stroke gets a serial (0..1023, wrapping); the last 16 are kept with an engraved bit, so a stroke engraves one
//   line when any of its parts (blade, lash arc, a crescent it threw) first damages an NPC, however late a crescent lands;
//   a serial no longer kept fails closed;
// - Over and Under throws are at least 18 ticks apart, counted here on the player, so swapping items and back cannot
//   throw faster than the figure eight.
internal sealed class SableThrowLedger
{
    private const int Kept = CrimsonRewardRules.CrescentKeptStrokes;
    private readonly int[] serials = new int[Kept];
    private readonly bool[] engraved = new bool[Kept];
    private int serial = -1;
    private ulong lastThrow;
    private bool thrown;

    internal SableThrowLedger() => Reset();
    internal int Serial => serial;

    internal int BeginStroke()
    {
        serial = (serial + 1) & (CrimsonRewardRules.CrescentSerials - 1);
        int slot = serial & (Kept - 1);
        serials[slot] = serial;
        engraved[slot] = false;
        return serial;
    }

    internal bool TryEngrave(int stroke)
    {
        if (stroke < 0 || stroke >= CrimsonRewardRules.CrescentSerials) return false;
        int slot = stroke & (Kept - 1);
        if (serials[slot] != stroke || engraved[slot]) return false;
        engraved[slot] = true;
        return true;
    }

    internal bool TryThrow(ulong now)
    {
        if (thrown && now < lastThrow + (ulong)CrimsonRewardRules.CrescentThrowSpacing) return false;
        lastThrow = now;
        thrown = true;
        return true;
    }

    // Death and entering a world forget every stroke (late crescents then engrave nothing) and the spacing; the serial
    // keeps counting, so an old serial can never alias a new stroke.
    internal void Reset()
    {
        Array.Fill(serials, -1);
        Array.Clear(engraved);
        thrown = false;
        lastThrow = 0;
    }
}
