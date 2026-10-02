using System;
using System.Numerics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.DomainTests;

// Bloodink Quill (docs/encounters/crimson-foundry/REWARDS.md, "Rogue - Bloodink Quill"): the pure flight, ink, burn,
// release timeline, codecs and arm. Native gameplay, multiplayer and the look stay owner play checks.
internal static partial class Program
{
    [DomainTest("Bloodink Quill flight equals native integration: velocity, then position")]
    private static void QuillFlightMatchesNativeIntegration()
    {
        Vector2[] throws = { new(20, 0), new(-20, 0), new(14.142136f, -14.142136f), new(0, 20), new(-3.5f, -19.69f), new(19.9f, 1.3f) };
        foreach (Vector2 v0 in throws)
        {
            Vector2 origin = new(12345.678f, 4321.25f), p = origin, v = v0;
            AssertEqual(origin, QuillRules.At(origin, v0, 0), "starts at the throw point");
            for (int age = 0; age < R.QuillFlight; age++)
            {
                // The projectile's AI sets the velocity, then native movement adds it to the position.
                v = QuillRules.NextVelocity(v, age);
                p += v;
                AssertEqual(p, QuillRules.At(origin, v0, age + 1), $"tick {age + 1} of {v0} is bit-exact");
                AssertEqual(v, QuillRules.VelocityAt(v0, age + 1), "the step velocity");
                Vector2 mid = QuillRules.At(origin, v0, age + .5f), before = QuillRules.At(origin, v0, age);
                AssertNear(0, Vector2.Distance(mid, before + v * .5f), 1e-2f, "a fractional time lies on the straight step");
            }
            AssertEqual(QuillRules.At(origin, v0, R.QuillFlight), QuillRules.At(origin, v0, 500), "clamped to the 60-tick flight");
        }
    }

    [DomainTest("Bloodink Quill flies straight for 12 ticks, then falls to at most 16 px/tick without braking")]
    private static void QuillFlightShape()
    {
        Vector2 v = new(20, 0);
        for (int age = 0; age < R.QuillStraight; age++)
        {
            v = QuillRules.NextVelocity(v, age);
            AssertEqual(new Vector2(20, 0), v, $"straight at tick {age}");
        }
        v = QuillRules.NextVelocity(v, R.QuillStraight);
        AssertNear(.25f, v.Y, 1e-6f, "gravity from tick 12");
        for (int age = R.QuillStraight + 1; age < 400; age++) v = QuillRules.NextVelocity(v, age);
        AssertEqual(16f, v.Y, "terminal fall");
        Vector2 down = QuillRules.NextVelocity(new Vector2(0, 20), 30);
        AssertEqual(20f, down.Y, "a throw already falling faster keeps its speed");
        AssertEqual(20f, QuillRules.NextVelocity(new Vector2(20, -5), 0).X, "horizontal speed is never touched");
    }

    [DomainTest("Bloodink Quill ink is sampled from the flight, tapers 3 to 9 px and ends at the nib")]
    private static void QuillInkSamples()
    {
        Span<QuillInkSample> samples = new QuillInkSample[QuillRules.MaxStrokeSamples];
        Vector2 origin = new(100, 200), v0 = new(20, -4);
        foreach (float stop in new[] { 0f, .4f, 3f, 11.5f, 12f, 27.25f, 60f })
        {
            int count = QuillRules.SampleStroke(origin, v0, stop, samples, out float length);
            AssertEqual(true, count >= 1 && count <= QuillRules.MaxStrokeSamples, "bounded");
            AssertEqual(origin, samples[0].At, "starts at the throw point");
            AssertNear(0, Vector2.Distance(QuillRules.At(origin, v0, stop), samples[count - 1].At), 1e-2f, $"ends at the nib at {stop}");
            AssertNear(0, samples[count - 1].FromNib, 1e-3f, "the nib is 0 px from the nib");
            AssertNear(length, samples[0].FromNib, 1e-3f, "the throw point is the whole length back");
            float arc = 0;
            for (int i = 1; i < count; i++)
            {
                float gap = Vector2.Distance(samples[i - 1].At, samples[i].At);
                arc += gap;
                AssertEqual(true, gap <= QuillRules.SampleStep + 1e-3f, "sampled at most every 10 px");
                AssertEqual(true, samples[i].FromNib <= samples[i - 1].FromNib, "distance to the nib shrinks toward it");
            }
            AssertNear(length, arc, 1e-2f, "length is the arc length");
            for (int i = 0; i < count; i++)
                AssertEqual(true, samples[i].Radius >= R.InkTail * .78f - 1e-4f && samples[i].Radius <= R.InkNib + 1e-4f, "radius within the taper");
            if (count > 2)
                AssertEqual(true, samples[count - 1].Radius > samples[0].Radius, "thick at the nib, thin at the throw point");
        }
        AssertEqual(true, QuillRules.IsBlot(47.9f) && !QuillRules.IsBlot(48), "shorter than 48 px is a blot");
        // The whole flight fits the sample budget, even straight down at the fastest fall.
        int full = QuillRules.SampleStroke(origin, new Vector2(0, 20), R.QuillFlight, samples, out float fall);
        AssertEqual(true, full < QuillRules.MaxStrokeSamples, "a full flight fits");
        AssertNear(1200, fall, .5f, "60 ticks at 20 px/tick");
        AssertNear(R.InkNib, QuillRules.InkRadius(1, new Vector2(1, 1)), 1e-4f, "across the nib: full width");
        AssertNear(R.InkTail * .78f, QuillRules.InkRadius(0, new Vector2(1, -1)), 1e-4f, "along the nib: thinner");
    }

    [DomainTest("Bloodink Quill stop time finds the nib on the flight")]
    private static void QuillStopNear()
    {
        Vector2 origin = new(0, 0), v0 = new(20, -6);
        for (int age = 1; age <= R.QuillFlight; age += 7)
            foreach (float s in new[] { 0f, .3f, .75f, 1f })
            {
                float t = age - 1 + s;
                float found = QuillRules.StopNear(origin, v0, age, QuillRules.At(origin, v0, t));
                AssertNear(t, found, 1e-3f, $"recovers {t}");
            }
        AssertEqual(0f, QuillRules.StopNear(origin, v0, 0, new Vector2(50, 50)), "nothing flown");
    }

    [DomainTest("Bloodink Quill burn runs from the nib back at 180 px/tick and each part burns 12 ticks")]
    private static void QuillBurnFront()
    {
        AssertEqual(0f, QuillRules.BurnAge(0, 0), "the nib ignites at the unroll");
        AssertEqual(-1f, QuillRules.BurnAge(0, 180), "180 px back ignites a tick later");
        AssertEqual(360f, QuillRules.BurnFront(2), "the front after two ticks");
        AssertEqual(true, QuillRules.Burning(0) && QuillRules.Burning(11.9f) && !QuillRules.Burning(12) && !QuillRules.Burning(-.1f), "12 live ticks");
        AssertNear(400 / 180f + 12, QuillRules.BurnEnd(400), 1e-4f, "a 400 px stroke has burnt out when the tail's 12 ticks end");
        AssertEqual(true, QuillRules.StrokeDone(400) - QuillRules.BurnEnd(400) is >= R.ResidueMin and <= R.ResidueMax, "residue 20-24");
        AssertEqual(0f, QuillRules.BurnReach(0), "opens from nothing");
        AssertNear(R.BurnRadius / 3, QuillRules.BurnReach(1), 1e-4f, "opens over three ticks");
        AssertEqual(R.BurnRadius, QuillRules.BurnReach(5), "18 px");
        AssertEqual(0f, QuillRules.BurnReach(12), "dry");
    }

    [DomainTest("Bloodink Quill burn collides exactly where its live samples are")]
    private static void QuillBurnCollision()
    {
        var samples = new QuillInkSample[QuillRules.MaxStrokeSamples];
        Vector2 origin = new(0, 0), v0 = new(20, 0);
        int count = QuillRules.SampleStroke(origin, v0, R.QuillStraight, samples, out float length); // 240 px straight right
        var stroke = new ReadOnlySpan<QuillInkSample>(samples, 0, count);
        AssertNear(240, length, 1e-3f, "a straight 240 px stroke");
        // A box 10 px under the nib: live from the unroll once opened past 10 px (radius 18 x t/3 >= 10 at t >= 5/3).
        Vector2 min = new(235, 10), max = new(245, 30);
        AssertEqual(false, QuillRules.BurnTouches(stroke, false, -1, min, max), "dormant before the unroll");
        AssertEqual(false, QuillRules.BurnTouches(stroke, false, 1, min, max), "still opening (6 px)");
        AssertEqual(true, QuillRules.BurnTouches(stroke, false, 2, min, max), "open (12 px)");
        AssertEqual(false, QuillRules.BurnTouches(stroke, false, 12.5f, min, max), "the nib end has dried");
        // The throw point: 240 px back, ignites at tau = 1.33.
        Vector2 tailMin = new(-5, 10), tailMax = new(5, 30);
        AssertEqual(false, QuillRules.BurnTouches(stroke, false, 1, tailMin, tailMax), "the front has not arrived");
        AssertEqual(true, QuillRules.BurnTouches(stroke, false, 5, tailMin, tailMax), "it has");
        AssertEqual(false, QuillRules.BurnTouches(stroke, false, 5, new Vector2(115, 19), new Vector2(125, 40)), "19 px off is outside 18");
        AssertEqual(true, QuillRules.BurnTouches(stroke, false, 5, new Vector2(115, 18), new Vector2(125, 40)), "18 px off touches");
        // A blot burns as one disc at the nib.
        int blotCount = QuillRules.SampleStroke(origin, v0, 2, samples, out float shortLength);
        var blot = new ReadOnlySpan<QuillInkSample>(samples, 0, blotCount);
        AssertEqual(true, QuillRules.IsBlot(shortLength), "40 px is a blot");
        AssertEqual(true, QuillRules.BurnTouches(blot, true, 4, new Vector2(40 - 5, 17), new Vector2(45, 30)), "the blot's 18 px disc");
        AssertEqual(false, QuillRules.BurnTouches(blot, true, 4, new Vector2(0 - 5, 17), new Vector2(5, 30)), "not along the stroke");
        // Owner decision 2: the ink stays where it was written, so a target that moved off it is missed.
        AssertEqual(false, QuillRules.BurnTouches(stroke, false, 5, new Vector2(100, 200), new Vector2(140, 260)), "moved away");
    }

    [DomainTest("Sealed Score flies 22 px/tick toward the cursor, glides to a halt and unrolls 8 ticks later")]
    private static void SealedScoreFlight()
    {
        AssertEqual(18, QuillRules.ScoreFlightTicks(400), "400 px");
        AssertEqual(26, QuillRules.UnrollAge(QuillRules.ScoreFlightTicks(400)), "about 26 ticks to the unroll");
        AssertEqual(R.ScoreFlight, QuillRules.ScoreFlightTicks(5000), "at most 30 ticks");
        AssertEqual(1, QuillRules.ScoreFlightTicks(0), "at least one tick");
        AssertEqual(1, QuillRules.ScoreFlightTicks(float.NaN), "garbage distance");
        int flight = 18;
        float previous = float.MaxValue, glide = 0;
        for (int age = 0; age < flight + 12; age++)
        {
            float speed = QuillRules.ScoreSpeedAt(age, flight);
            AssertEqual(true, speed <= previous, "never speeds up");
            if (age < flight) AssertEqual(R.ScoreSpeed, speed, "cruise");
            else glide += speed;
            AssertEqual(true, previous == float.MaxValue || previous - speed <= R.ScoreSpeed * (1 - QuillRules.ScoreGlideKeep) + 1e-3f,
                "a settle, not a stop");
            previous = speed;
        }
        AssertEqual(0f, QuillRules.ScoreSpeedAt(flight + QuillRules.ScoreGlide, flight), "halted inside the windup");
        AssertEqual(true, glide < R.ScoreSpeed, "the glide adds less than a tick of flight");
    }

    [DomainTest("Sealed Score plays the quills back in throw order and bursts a sixteenth after the last")]
    private static void SealedScoreTimeline()
    {
        int[] sixteenths = { 0, 7, 14, 21, 28, 35, 42, 49 };
        for (int i = 0; i < R.MaxQuills; i++)
        {
            AssertEqual(sixteenths[i], QuillRules.BurstStart(i), $"quill {i} at U + S({i})");
            AssertEqual(100 + sixteenths[i], R.QuillBurstTick(100, i), "the shared tick helper agrees");
        }
        for (int n = 0; n <= R.MaxQuills; n++)
        {
            AssertEqual(R.S(n), QuillRules.ScoreBurstStart(n), $"score after {n}");
            if (n > 0) AssertEqual(true, QuillRules.ScoreBurstStart(n) > QuillRules.BurstStart(n - 1), "after the last quill");
            AssertEqual(n == 8, QuillRules.FullMelody(n), "Full Melody only at eight");
            AssertEqual(n == 8 ? 160f : 120f, QuillRules.ScoreBurstRadius(n), "score burst radius");
            AssertEqual(n == 8 ? 3f : 2f, QuillRules.ScoreBurstMultiplier(n), "score burst multiplier");
            AssertEqual(true, QuillRules.ScoreDone(n) >= QuillRules.QuillDone(Math.Max(0, n - 1)), "the score outlives its quills");
            AssertEqual(true, QuillRules.UnrollAge(R.ScoreFlight) + QuillRules.ScoreDone(n) <= QuillRules.MaxScoreAge, "age stays valid");
        }
        AssertEqual(0, QuillRules.ScoreBurstStart(0), "nothing claimed: only the score bursts, at the unroll");
        AssertEqual(true, QuillRules.BurstReach(-.5f, 56, QuillRules.QuillBurstLive) == 0 && QuillRules.BurstReach(QuillRules.QuillBurstLive, 56, QuillRules.QuillBurstLive) == 0,
            "a burst is live only inside its window");
        AssertNear(56, QuillRules.BurstReach(3, 56, QuillRules.QuillBurstLive), 1e-4f, "a 56 px disc once open");
        // The melody: higher quills ring higher tolls, 32 px a step, clamped to the ladder.
        int previous = -1;
        for (float rise = -200; rise <= 200; rise += 4)
        {
            int toll = QuillRules.TollForHeight(1000 - rise, 1000);
            AssertEqual(true, toll >= previous && toll is >= 0 and <= 7, "non-decreasing with height, on the ladder");
            previous = toll;
        }
        AssertEqual(4, QuillRules.TollForHeight(1000, 1000), "level with the score");
        AssertEqual(3, QuillRules.TollForHeight(1001, 1000), "just below");
        AssertEqual(7, QuillRules.TollForHeight(0, 1000), "far above");
        AssertEqual(0, QuillRules.TollForHeight(2000, 1000), "far below");
        AssertEqual(4, QuillRules.TollForHeight(float.NaN, 1000), "garbage height");
    }

    [DomainTest("Bloodink Quill commitment: unstarted parts die on death or Down, started parts finish")]
    private static void QuillCommitment()
    {
        AssertEqual(true, QuillRules.PartSurvives(float.NegativeInfinity, 0, true), "a usable owner keeps a waiting part");
        AssertEqual(false, QuillRules.PartSurvives(float.NegativeInfinity, 0, false), "Down before the unroll kills it");
        AssertEqual(false, QuillRules.PartSurvives(20, QuillRules.BurstStart(3), false), "quill 3 had not burst");
        AssertEqual(true, QuillRules.PartSurvives(21, QuillRules.BurstStart(3), false), "quill 3 bursting finishes");
        AssertEqual(true, QuillRules.PartSurvives(0, 0, false), "a stroke already burning finishes");
        AssertEqual(true, QuillRules.ClaimWait > QuillRules.UnrollAge(R.ScoreFlight), "a claim outwaits the longest flight");
        AssertEqual(false, QuillRules.MustPull(7), "seven standing");
        AssertEqual(true, QuillRules.MustPull(8), "a ninth pulls the oldest out");
        AssertEqual(0, QuillRules.Oldest(new[] { 1020, 1021, 3, 2 }), "oldest across the wrap");
        AssertEqual(-1, QuillRules.Oldest(ReadOnlySpan<int>.Empty), "none");
        Span<int> order = stackalloc int[5];
        QuillRules.ThrowOrder(new[] { 2, 1022, 0, 1023, 1 }, order);
        AssertEqual("1,3,2,4,0", string.Join(",", order.ToArray()), "throw order across the wrap");
    }

    [DomainTest("Bloodink Quill codecs are exact and every projectile rejects invalid ai")]
    private static void QuillCodecs()
    {
        foreach (var offset in new[] { new Vector2(0, 0), new Vector2(-512, 512), new Vector2(37.4f, -12.6f), new Vector2(900, -2000) })
        {
            float packed = QuillRules.PackOffset(offset);
            AssertEqual(true, packed == MathF.Floor(packed) && packed >= 0 && packed <= QuillRules.MaxOffsetCode, "an exact integer");
            AssertEqual(true, QuillRules.TryUnpackOffset(packed, out Vector2 back), "decodes");
            Vector2 expected = new(MathF.Round(Math.Clamp(offset.X, -512, 512)), MathF.Round(Math.Clamp(offset.Y, -512, 512)));
            AssertEqual(expected, back, $"{offset} round trip, clamped to +-512");
        }
        AssertEqual(true, QuillRules.TryUnpackOffset(QuillRules.PackOffset(new Vector2(float.NaN, 3)), out Vector2 nan) && nan == new Vector2(0, 3), "NaN packs as 0");
        for (int cast = 0; cast < R.QuillSerials; cast += 31)
            for (int rank = 0; rank < R.MaxQuills; rank++)
            {
                float claim = QuillRules.Claim(cast, rank);
                AssertEqual(true, QuillRules.TryClaim(claim, out int c, out int r) && c == cast && r == rank, "claim round trip");
                AssertEqual(true, claim > QuillRules.Standing && claim <= QuillRules.MaxClaim, "never the standing state");
            }
        AssertEqual(true, QuillRules.TryClaim(QuillRules.Claim(1023, 7), out int topCast, out int topRank) && topCast == 1023 && topRank == 7, "largest claim");
        AssertEqual(false, QuillRules.TryClaim(QuillRules.Standing, out _, out _), "standing is not a claim");
        for (int cast = 0; cast < R.QuillSerials; cast += 17)
            for (int n = 0; n <= R.MaxQuills; n++)
                AssertEqual(true, QuillRules.TryScoreTag(QuillRules.ScoreTag(cast, n), out int c, out int q) && c == cast && q == n, "tag round trip");
        AssertThrows<ArgumentOutOfRangeException>(() => QuillRules.Claim(1024, 0), "cast has 10 bits");
        AssertThrows<ArgumentOutOfRangeException>(() => QuillRules.Claim(0, 8), "rank 0..7");
        AssertThrows<ArgumentOutOfRangeException>(() => QuillRules.ScoreTag(0, 9), "at most eight quills");

        int npcs = 200;
        AssertEqual(true, QuillRules.ValidQuill(0, 0, 0, npcs), "a fresh quill");
        AssertEqual(true, QuillRules.ValidQuill(QuillRules.StuckOn(199), 1023, QuillRules.PackOffset(new Vector2(5, -5)), npcs), "stuck in the last NPC slot");
        AssertEqual(true, QuillRules.ValidQuill(QuillRules.InWorld, 7, 0, npcs), "fixed in the world");
        foreach (var (a, b, c) in new[] { (float.NaN, 0f, 0f), (-2f, 0f, 0f), (201f, 0f, 0f), (.5f, 0f, 0f), (0f, 1024f, 0f), (0f, 3.5f, 0f),
                     (0f, 0f, -1f), (0f, 0f, QuillRules.MaxOffsetCode + 1f), (0f, float.PositiveInfinity, 0f) })
            AssertEqual(false, QuillRules.ValidQuill(a, b, c, npcs), $"quill rejects ({a}, {b}, {c})");
        AssertEqual(true, QuillRules.ValidTrail(0, 0, 0) && QuillRules.ValidTrail(60, 1023, QuillRules.Claim(3, 2)) && QuillRules.ValidTrail(12.37f, 5, 0), "trails");
        foreach (var (a, b, c) in new[] { (float.NaN, 0f, 0f), (-.1f, 0f, 0f), (60.5f, 0f, 0f), (1f, -1f, 0f), (1f, 0f, -1f), (1f, 0f, QuillRules.MaxClaim + 1f), (1f, 0f, 2.5f) })
            AssertEqual(false, QuillRules.ValidTrail(a, b, c), $"trail rejects ({a}, {b}, {c})");
        AssertEqual(true, QuillRules.ValidScore(0, 18, QuillRules.ScoreTag(4, 8)) && QuillRules.ValidScore(QuillRules.MaxScoreAge, 0, 0), "scores");
        foreach (var (a, b, c) in new[] { (float.NaN, 1f, 0f), (-1f, 1f, 0f), (QuillRules.MaxScoreAge + 1f, 1f, 0f), (0f, 31f, 0f), (0f, 1.5f, 0f), (0f, 1f, -1f), (0f, 1f, 9216f) })
            AssertEqual(false, QuillRules.ValidScore(a, b, c), $"score rejects ({a}, {b}, {c})");
    }

    [DomainTest("Bloodink Quill throwing arm follows through and returns along a smooth curve")]
    private static void QuillArm()
    {
        foreach (int facing in new[] { 1, -1 })
            foreach (float aimLocal in new[] { -1.4f, -.6f, 0f, .5f, 1.3f })
            {
                float aim = facing == 1 ? aimLocal : MathF.PI - aimLocal;
                AssertNear(0, Wrap(QuillRules.ArmAngle(0, aim, facing) - aim), 1e-4f, $"leaves the hand along the aim ({facing}, {aimLocal})");
                float rest = facing == 1 ? QuillRules.ArmRest : MathF.PI - QuillRules.ArmRest;
                AssertNear(0, Wrap(QuillRules.ArmAngle(1, aim, facing) - rest), 1e-4f, "returns to rest");
                float previous = QuillRules.ArmAngle(0, aim, facing);
                for (int i = 1; i <= 400; i++)
                {
                    float p = i / 400f, angle = QuillRules.ArmAngle(p, aim, facing);
                    AssertEqual(true, MathF.Abs(Wrap(angle - previous)) < .045f, "continuous: no pop between samples");
                    previous = angle;
                }
                float follow = QuillRules.ArmAngle(QuillRules.ArmFollowEnd, aim, facing);
                float followLocal = facing == 1 ? follow : MathF.PI - follow;
                AssertEqual(true, followLocal >= QuillRules.ArmFollowMin - 1e-4f && followLocal <= QuillRules.ArmFollowMax + 1e-4f,
                    "follows through down and back past the rest");
            }
        static float Wrap(float a) => MathF.IEEERemainder(a, MathF.Tau);
    }

    [DomainTest("Sealed Score flourish flashes outward and is gone at the unroll")]
    private static void SealedScoreFlourish()
    {
        AssertEqual(-18f, QuillRules.FlourishRow(0), "five rows 9 px apart");
        AssertEqual(18f, QuillRules.FlourishRow(4), "symmetric");
        AssertEqual(QuillRules.FlourishFrom, QuillRules.FlourishHalfLength(0), "starts at the score");
        AssertEqual(QuillRules.FlourishFrom + QuillRules.FlourishReach, QuillRules.FlourishHalfLength(R.ScoreWindup), "reaches out");
        AssertEqual(0f, QuillRules.FlourishOpacity(0), "fades in");
        AssertEqual(0f, QuillRules.FlourishOpacity(R.ScoreWindup), "gone at the unroll");
        AssertEqual(true, QuillRules.FlourishOpacity(3) > 0 && QuillRules.FlourishOpacity(3) <= .55f, "faint");
        AssertEqual(1f, QuillRules.InkFade(480), "standing ink at full");
        AssertEqual(.5f, QuillRules.InkFade(30), "fading over the last 60 ticks");
        AssertEqual(0f, QuillRules.InkFade(0), "gone");
    }
}
