#nullable enable
using System;
using System.Numerics;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// One sample of a quill's written ink: where it lies (world px), its dormant radius (tapering 3 px at the throw point
// to 9 px at the nib, shaped by the broad nib) and its arc distance back from the nib (the burn front runs that way).
internal struct QuillInkSample
{
    internal Vector2 At;
    internal float Radius;
    internal float FromNib;
}

// Pure rules for the Bloodink Quill (REWARDS.md, "Rogue - Bloodink Quill"): no Terraria or XNA references; linked into
// Tests/Convergence.DomainTests and the offline preview, so the game, the tests and the preview frames sample the same
// flight, ink and burn. The spec's numbers stay in CrimsonRewardRules; the choices the spec leaves open (sample spacing,
// live windows of the bursts, the blot, the toll step, the score's glide, the arm curve) are named here.
//
// Clocks: a quill's flight runs on its own AI count from the throw (`age`); a release runs on ticks since the score
// unrolled (`tau`), stamped by each client when it first sees the unroll. Nothing reads a music or world beat clock.
internal static class QuillRules
{
    // ---- Throw: QuillFlight ----------------------------------------------------------------------------------------
    // Hitbox (px, centred on the nib), the flight's swept capsule radius, and how far the nib enters flesh / stone.
    internal const float QuillHitbox = 10, QuillBody = 6, Embed = 4, TileEmbed = QuillHitbox / 2 + 2;

    // The velocity a quill moves by on its `age`-th tick (0-based), from the velocity it had before that tick. Straight
    // for 12 ticks, then gravity 0.25 px/tick^2 until it falls 16 px/tick; a throw already falling faster keeps its
    // speed (no braking). The projectile's AI calls exactly this, then native movement adds the velocity.
    internal static Vector2 NextVelocity(Vector2 velocity, int age)
    {
        if (age >= R.QuillStraight && velocity.Y < R.QuillMaxFall) velocity.Y = MathF.Min(velocity.Y + R.QuillGravity, R.QuillMaxFall);
        return velocity;
    }

    // Where a quill thrown from `origin` with `velocity` is after `t` ticks, stepped exactly as native integration steps
    // it (velocity, then position). A fractional t lies on the straight step between two ticks, which is also where the
    // draw interpolation puts the body. t is clamped to the flight (0..60).
    internal static Vector2 At(Vector2 origin, Vector2 velocity, float t)
    {
        if (!(t > 0)) return origin;
        t = MathF.Min(t, R.QuillFlight);
        int whole = (int)MathF.Floor(t);
        Vector2 p = origin, v = velocity;
        for (int k = 0; k < whole; k++) { v = NextVelocity(v, k); p += v; }
        float f = t - whole;
        if (f > 0) p += NextVelocity(v, whole) * f;
        return p;
    }

    // The velocity in force during the step that ends at time t (the flight direction there).
    internal static Vector2 VelocityAt(Vector2 velocity, float t)
    {
        int steps = Math.Clamp((int)MathF.Ceiling(t), 1, R.QuillFlight);
        Vector2 v = velocity;
        for (int k = 0; k < steps; k++) v = NextVelocity(v, k);
        return v;
    }

    // The fractional flight time, up to `age`, whose position lies nearest `point` (where the nib actually stopped; a
    // remote client may learn of the stick a few ticks late). The ink is rebuilt to this time, so it ends at the nib.
    internal static float StopNear(Vector2 origin, Vector2 velocity, int age, Vector2 point)
    {
        age = Math.Clamp(age, 0, R.QuillFlight);
        if (age == 0) return 0;
        float best = float.MaxValue, stop = age;
        Vector2 a = origin, v = velocity;
        for (int k = 0; k < age; k++)
        {
            v = NextVelocity(v, k);
            float len = v.LengthSquared();
            float s = len < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(point - a, v) / len, 0, 1);
            float d = Vector2.DistanceSquared(point, a + v * s);
            if (d <= best) { best = d; stop = k + s; }
            a += v;
        }
        return stop;
    }

    // ---- Ink (the build) -------------------------------------------------------------------------------------------
    internal const float SampleStep = 10, BlotRadius = 12;
    internal const int MaxStrokeSamples = 256;
    // The broad nib is held at 45 degrees: strokes across it are full, strokes along it a little thinner.
    private static readonly Vector2 NibAxis = Vector2.Normalize(new Vector2(1, -1));

    // Dormant radius at `along` (0 at the throw point, 1 at the nib) for a stroke heading `direction`.
    internal static float InkRadius(float along, Vector2 direction)
    {
        float taper = R.InkTail + (R.InkNib - R.InkTail) * Math.Clamp(along, 0, 1);
        float len = direction.Length();
        float across = len < 1e-4f ? 1 : MathF.Abs(direction.X * NibAxis.Y - direction.Y * NibAxis.X) / len;
        return taper * (.78f + .22f * across);
    }

    // A stroke shorter than 48 px is kept as a blot at the nib, so a close throw still builds.
    internal static bool IsBlot(float length) => length < R.InkBlot;

    // The ink written from the throw point to time `stop`, as samples about every 10 px (more where one tick's step is
    // long), ordered from the throw point to the nib. Returns the sample count; `length` is the stroke's arc length.
    // The trail, the flying quill's wet ink, the burn collision and the offline preview all use these samples.
    internal static int SampleStroke(Vector2 origin, Vector2 velocity, float stop, Span<QuillInkSample> into, out float length)
    {
        length = 0;
        if (into.Length == 0) return 0;
        stop = Math.Clamp(float.IsFinite(stop) ? stop : 0, 0, R.QuillFlight);
        int count = 0;
        Vector2 p = origin, v = velocity;
        into[count++] = new QuillInkSample { At = p, Radius = 0, FromNib = 0 };
        for (int k = 0; k < stop && count < into.Length; k++)
        {
            v = NextVelocity(v, k);
            float f = MathF.Min(1, stop - k);
            Vector2 step = v * f;
            float stepLength = step.Length();
            int pieces = Math.Max(1, (int)MathF.Ceiling(stepLength / SampleStep));
            for (int j = 1; j <= pieces && count < into.Length; j++)
            {
                Vector2 at = j == pieces ? p + step : p + step * (j / (float)pieces);
                length += Vector2.Distance(into[count - 1].At, at);
                into[count++] = new QuillInkSample { At = at, Radius = 0, FromNib = length }; // arc length from the throw point
            }
            p += step;
        }
        for (int i = 0; i < count; i++)
        {
            float s = into[i].FromNib;
            Vector2 direction = i + 1 < count ? into[i + 1].At - into[i].At : i > 0 ? into[i].At - into[i - 1].At : velocity;
            into[i].Radius = InkRadius(length > 1e-3f ? s / length : 1, direction);
            into[i].FromNib = length - s;
        }
        return count;
    }

    // A standing stroke's opacity over its last 60 ticks.
    internal static float InkFade(int timeLeft) => Math.Clamp(timeLeft / (float)R.QuillFade, 0, 1);

    // ---- Ignition: the burn front --------------------------------------------------------------------------------
    internal const int Residue = 22; // the scar after a live part, inside the spec's 20-24 ticks

    // Every claimed stroke ignites at its nib at the unroll (tau = 0); the front runs back to the throw point at
    // 180 px/tick and each point burns for 12 ticks behind it. Ticks since a point `fromNib` px back ignited:
    internal static float BurnAge(float tau, float fromNib) => tau - fromNib / R.BurnSpeed;
    internal static bool Burning(float age) => age >= 0 && age < R.BurnLive;
    internal static float BurnFront(float tau) => tau <= 0 ? 0 : tau * R.BurnSpeed;
    // The tau at which the throw-point end stops burning, and at which its scar has dried away.
    internal static float BurnEnd(float length) => Math.Max(0, length) / R.BurnSpeed + R.BurnLive;
    internal static float StrokeDone(float length) => BurnEnd(length) + Residue;
    // Draw equals collide: a live point's radius is 18 x min(age / 3, 1).
    internal static float BurnReach(float age) => Burning(age) ? R.BurnRadius * R.InkOpen(age) : 0;

    // Does the burning part of a stroke touch the box at release time `tau`? Exactly the samples that are drawn live:
    // a disc at every burning sample and a capsule (the smaller end radius) between burning neighbours. A blot burns as
    // one 18 px disc at the nib.
    internal static bool BurnTouches(ReadOnlySpan<QuillInkSample> samples, bool blot, float tau, Vector2 min, Vector2 max)
    {
        if (samples.Length == 0) return false;
        if (blot)
        {
            float reach = BurnReach(tau);
            return reach > 0 && R.DiscTouchesBox(samples[^1].At, reach, min, max);
        }
        float previous = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float reach = BurnReach(BurnAge(tau, samples[i].FromNib));
            if (reach > 0)
            {
                if (R.DiscTouchesBox(samples[i].At, reach, min, max)) return true;
                if (previous > 0 && R.CapsuleTouchesBox(samples[i - 1].At, samples[i].At, MathF.Min(previous, reach), min, max)) return true;
            }
            previous = reach;
        }
        return false;
    }

    // ---- Stealth strike: the Sealed Score ----------------------------------------------------------------------
    internal const int ScoreGlide = 4, QuillBurstLive = 8, ScoreBurstLive = 12;
    internal const float ScoreGlideKeep = .35f;

    // Flight ticks toward the cursor at 22 px/tick, at most 30 (a tile stops it earlier).
    internal static int ScoreFlightTicks(float distance)
        => Math.Clamp((int)MathF.Round(float.IsFinite(distance) ? Math.Max(0, distance) / R.ScoreSpeed : 0), 1, R.ScoreFlight);
    // It cruises, then glides to a halt over four ticks as the windup begins (a settle, not a stop).
    internal static float ScoreSpeedAt(int age, int flight)
    {
        if (age < flight) return R.ScoreSpeed;
        int glide = age - flight;
        return glide < ScoreGlide ? R.ScoreSpeed * MathF.Pow(ScoreGlideKeep, glide + 1) : 0;
    }
    internal static int UnrollAge(int flight) => flight + R.ScoreWindup;

    // The release timeline, in ticks since the unroll: quill `rank` bursts at S(rank); the score after the last of `n`.
    internal static int BurstStart(int rank) => R.S(Math.Clamp(rank, 0, R.MaxQuills - 1));
    internal static int ScoreBurstStart(int quills) => R.S(Math.Clamp(quills, 0, R.MaxQuills));
    internal static bool FullMelody(int quills) => quills == R.MaxQuills;
    internal static float ScoreBurstRadius(int quills) => FullMelody(quills) ? R.MelodyRadius : R.ScoreBurstRadius;
    internal static float ScoreBurstMultiplier(int quills) => FullMelody(quills) ? R.MelodyMultiplier : R.ScoreBurstMultiplier;
    internal static int ScoreDone(int quills) => ScoreBurstStart(quills) + ScoreBurstLive + Residue;
    internal static int QuillDone(int rank) => BurstStart(rank) + QuillBurstLive + Residue;
    internal static bool Live(float since, int window) => since >= 0 && since < window;
    internal static float BurstReach(float since, float radius, int window) => Live(since, window) ? radius * R.InkOpen(since) : 0;

    // Commitment: a release part that has not started dies when its owner is dead or Down; a started part finishes.
    internal static bool PartSurvives(float tau, float start, bool usable) => usable || tau >= start;
    // A claimed part whose score never unrolls (it died first) gives up after this many ticks.
    internal const int ClaimWait = R.ScoreFlight + R.ScoreWindup + ScoreGlide + 16;
    internal static int MaxScoreAge => R.ScoreFlight + R.ScoreWindup + ScoreDone(R.MaxQuills) + 16;

    // The playback melody: the higher the quill relative to the halted score, the higher its toll (32 px a step).
    internal const float TollStep = 32;
    internal static int TollForHeight(float quillY, float scoreY)
    {
        float steps = (scoreY - quillY) / TollStep;
        if (!float.IsFinite(steps)) return 4;
        return Math.Clamp((int)MathF.Floor(4 + Math.Clamp(steps, -16, 16)), 0, 7);
    }

    // The windup's flourish: five faint dormant staff lines flash outward from the score, gone at the unroll.
    internal const float FlourishGap = 9, FlourishFrom = 14, FlourishReach = 72;
    internal static float FlourishRow(int line) => (Math.Clamp(line, 0, 4) - 2) * FlourishGap;
    internal static float FlourishHalfLength(float windup)
    {
        float x = Math.Clamp(windup / R.ScoreWindup, 0, 1);
        return FlourishFrom + FlourishReach * (1 - (1 - x) * (1 - x) * (1 - x));
    }
    internal static float FlourishOpacity(float windup)
    {
        float x = Math.Clamp(windup / R.ScoreWindup, 0, 1);
        return .55f * Math.Clamp(windup / 2, 0, 1) * (1 - R.Smooth(x));
    }

    // ---- State codecs (exact integers in the native ai floats) --------------------------------------------------
    // BloodinkQuill ai = (stuck, serial, offset): stuck 0 flying, NPC slot + 1, or -1 fixed in the world (a tile, or
    // where it was when its NPC died). Offset packs the stick offset from the NPC's centre, +-512 px, 1 px steps.
    internal const int Flying = 0, InWorld = -1;
    internal static float StuckOn(int npcSlot) => npcSlot + 1;
    internal static float PackOffset(Vector2 offset)
    {
        int x = (int)MathF.Round(Math.Clamp(float.IsFinite(offset.X) ? offset.X : 0, -R.OffsetHalf, R.OffsetHalf));
        int y = (int)MathF.Round(Math.Clamp(float.IsFinite(offset.Y) ? offset.Y : 0, -R.OffsetHalf, R.OffsetHalf));
        return R.PackPair(x, y, R.OffsetHalf);
    }
    internal static bool TryUnpackOffset(float packed, out Vector2 offset)
    {
        bool ok = R.TryUnpackPair(packed, R.OffsetHalf, out int x, out int y);
        offset = new Vector2(x, y);
        return ok;
    }
    internal static int MaxOffsetCode => (2 * R.OffsetHalf + 1) * (2 * R.OffsetHalf + 1) - 1;
    internal static bool ValidQuill(float stuck, float serial, float offset, int npcSlots)
        => R.ValidInteger(stuck, InWorld, npcSlots) && R.ValidInteger(serial, 0, R.QuillSerials - 1)
            && R.ValidInteger(offset, 0, MaxOffsetCode);

    // BloodinkTrail ai = (stop tick, serial, state): state 0 standing, else claimed by Sealed Score `cast` as rank `rank`.
    internal const int Standing = 0;
    internal static float Claim(int cast, int rank)
    {
        if (cast < 0 || cast >= R.QuillSerials || rank < 0 || rank >= R.MaxQuills) throw new ArgumentOutOfRangeException();
        return 1 + rank + R.MaxQuills * cast;
    }
    internal static int MaxClaim => R.MaxQuills * R.QuillSerials;
    internal static bool TryClaim(float state, out int cast, out int rank)
    {
        cast = rank = 0;
        if (!R.ValidInteger(state, 1, MaxClaim)) return false;
        int value = (int)state - 1;
        rank = value % R.MaxQuills; cast = value / R.MaxQuills;
        return true;
    }
    internal static bool ValidTrail(float stop, float serial, float state)
        => R.Valid(stop, 0, R.QuillFlight) && R.ValidInteger(serial, 0, R.QuillSerials - 1)
            && (state == Standing || TryClaim(state, out _, out _));

    // SealedScore ai = (age, flight ticks, tag): the tag names the cast its parts were claimed by and how many quills it
    // took, so every client knows the timeline without a packet: tag = cast x 9 + n, as REWARDS.md's SealedScore row.
    internal static float ScoreTag(int cast, int quills)
    {
        if (cast < 0 || cast >= R.QuillSerials || quills < 0 || quills > R.MaxQuills) throw new ArgumentOutOfRangeException();
        return cast * (R.MaxQuills + 1) + quills;
    }
    internal static bool TryScoreTag(float tag, out int cast, out int quills)
    {
        cast = quills = 0;
        if (!R.ValidInteger(tag, 0, R.QuillSerials * (R.MaxQuills + 1) - 1)) return false;
        int value = (int)tag;
        quills = value % (R.MaxQuills + 1); cast = value / (R.MaxQuills + 1);
        return true;
    }
    internal static bool ValidScore(float age, float flight, float tag)
        => R.ValidInteger(age, 0, MaxScoreAge) && R.ValidInteger(flight, 0, R.ScoreFlight) && TryScoreTag(tag, out _, out _);

    // ---- Bookkeeping ---------------------------------------------------------------------------------------------
    // Index of the oldest serial (serials compare modulo 1024), -1 when empty.
    internal static int Oldest(ReadOnlySpan<int> serials)
    {
        int oldest = -1;
        for (int i = 0; i < serials.Length; i++)
            if (oldest < 0 || R.SerialNewer(serials[oldest], serials[i])) oldest = i;
        return oldest;
    }
    // order[k] = index of the k-th thrown (oldest first): the playback order.
    internal static void ThrowOrder(ReadOnlySpan<int> serials, Span<int> order)
    {
        int n = Math.Min(serials.Length, order.Length);
        for (int i = 0; i < n; i++) order[i] = i;
        for (int i = 1; i < n; i++)
        {
            int key = order[i], j = i - 1;
            while (j >= 0 && R.SerialNewer(serials[order[j]], serials[key])) { order[j + 1] = order[j]; j--; }
            order[j + 1] = key;
        }
    }
    // A ninth standing quill pulls the oldest out.
    internal static bool MustPull(int standing) => standing >= R.MaxQuills;

    // ---- The throwing arm ----------------------------------------------------------------------------------------
    // The quill leaves the hand on the first tick of the use; the arm follows through past the aim, down and back, then
    // returns along a curve to hang just forward. Angles are world radians (0 = right, y down); `facing` mirrors them.
    internal const float ArmFollowEnd = .35f, ArmRest = MathF.PI / 2 - .25f, ArmFollowMin = MathF.PI / 2 + .35f, ArmFollowMax = MathF.PI / 2 + .75f;
    internal static float ArmAngle(float progress, float aim, int facing)
    {
        int f = facing == -1 ? -1 : 1;
        float local = f == 1 ? aim : MathF.PI - aim;
        local = MathF.IEEERemainder(float.IsFinite(local) ? local : 0, MathF.Tau);
        local = Math.Clamp(local, -1.45f, 1.45f);
        float follow = Math.Clamp(local + 1.25f, ArmFollowMin, ArmFollowMax);
        float p = Math.Clamp(float.IsFinite(progress) ? progress : 1, 0, 1), angle;
        if (p < ArmFollowEnd)
        {
            // Hermite: leaves the aim with the throw's momentum (1.6x the mean rate), arrives at rest-velocity zero.
            float u = p / ArmFollowEnd, u2 = u * u, u3 = u2 * u;
            float delta = follow - local;
            angle = (2 * u3 - 3 * u2 + 1) * local + (u3 - 2 * u2 + u) * 1.6f * delta + (-2 * u3 + 3 * u2) * follow;
        }
        else
        {
            float u = (p - ArmFollowEnd) / (1 - ArmFollowEnd);
            angle = follow + (ArmRest - follow) * (u * u * (3 - 2 * u));
        }
        return f == 1 ? angle : MathF.PI - angle;
    }
}
