using System;
using System.Collections.Generic;
using System.Numerics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using static Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.DomainTests;

// Canticle Organ (docs/encounters/crimson-foundry/REWARDS.md, "Ranged - Canticle Organ"): the mark ledger, the hymn
// schedule, the BoneHand codec and timing, draw equals collide, commitment, the aim spring and the pipe kick.
internal static partial class Program
{
    private sealed class CanticleRecorder : ICanticleInk
    {
        internal readonly List<(CanticleInkLook Look, float Opacity, float Warmth, bool Bead, List<(Vector2 At, float Radius, float Time)> Points)> Paths = new();
        internal int Droplets;
        private (CanticleInkLook Look, float Opacity, float Warmth, List<(Vector2, float, float)> Points)? open;
        public bool Begin(CanticleInkLook look, float seed, float opacity = 1, float warmth = 0) { open = (look, opacity, warmth, new()); return true; }
        public void Point(Vector2 at, float radius, float time) => open?.Points.Add((at, radius, time));
        public void End(bool bead = false)
        {
            if (open is { } o) Paths.Add((o.Look, o.Opacity, o.Warmth, bead, o.Points));
            open = null;
        }
        public void Droplet(Vector2 from, Vector2 to, float radius, float time, float seed) => Droplets++;
        internal int Count(CanticleInkLook look) { int n = 0; foreach (var p in Paths) if (p.Look == look) n++; return n; }
    }

    private static CanticleHand[] CanticlePlan(CanticleMarks marks, params bool[] inRange)
    {
        var hands = new CanticleHand[MaxHands];
        int n = marks.Plan(inRange, hands);
        return hands[..n];
    }

    [DomainTest("Canticle marks cap at eight per NPC and eight NPCs, evicting the NPC whose newest mark is oldest")]
    private static void CanticleMarkCaps()
    {
        var marks = new CanticleMarks();
        for (int i = 0; i < MaxMarksPerNpc; i++)
        {
            var r = marks.Mark(5, 10, 77, (ulong)(100 + i));
            AssertEqual(i + 1, r.Count, $"mark {i + 1}");
            AssertEqual(true, r.Added, $"mark {i + 1} added");
            AssertEqual(-1, r.Evicted, "nothing evicted");
        }
        var capped = marks.Mark(5, 10, 77, 200);
        AssertEqual(MaxMarksPerNpc, capped.Count, "at most eight marks per NPC");
        AssertEqual(false, capped.Added, "a ninth hit adds no mark");
        AssertEqual(200UL, marks[0].Newest, "a hit at the cap refreshes the newest mark's time");

        marks.Clear();
        for (int root = 0; root < MaxMarkedNpcs; root++) marks.Mark(root, 1, 0, (ulong)root);
        marks.Mark(0, 1, 0, 50); // root 0 is now the freshest; root 1 the stalest
        AssertEqual(MaxMarkedNpcs, marks.Count, "eight NPCs");
        var ninth = marks.Mark(40, 1, 0, 60);
        AssertEqual(1, ninth.Evicted, "the NPC whose newest mark is oldest loses its marks");
        AssertEqual(MaxMarkedNpcs, marks.Count, "still eight NPCs");
        AssertEqual(0, marks.MarksOn(1), "evicted marks are gone");
        AssertEqual(1, marks.MarksOn(40), "the ninth NPC starts at one mark");
        AssertEqual(2, marks.MarksOn(0), "the refreshed NPC keeps its marks");
    }

    [DomainTest("Canticle marks expire 360 ticks after the newest mark and are keyed by root, type and incarnation")]
    private static void CanticleMarkExpiryAndIdentity()
    {
        var marks = new CanticleMarks();
        marks.Mark(3, 1, 9, 100);
        marks.Mark(3, 1, 9, 140);
        AssertEqual(0, marks.Expire(140 + (ulong)MarkLife - 1), "alive one tick before 360");
        AssertEqual(2, marks.MarksOn(3), "both marks share the newest mark's expiry");
        AssertEqual(1, marks.Expire(140 + (ulong)MarkLife), "expired 360 ticks after the newest mark");
        AssertEqual(0, marks.Count, "nothing left");

        marks.Mark(3, 1, 9, 10);
        AssertEqual(1, marks.Expire(5), "a clock that went backwards (a new world) never strands marks");

        for (int i = 0; i < 4; i++) marks.Mark(6, 1, 9, 10);
        AssertEqual(1, marks.Mark(6, 1, 12, 11).Count, "a reused slot (new incarnation) never inherits marks");
        AssertEqual(1, marks.Mark(6, 2, 12, 12).Count, "nor does another NPC type in the slot");
        AssertEqual(1, marks.Count, "one entry for the slot");
        AssertEqual(-1, marks.Mark(-1, 1, 0, 0).Evicted, "an invalid root marks nothing");
        AssertEqual(1, marks.Count, "still one entry");
    }

    [DomainTest("Canticle hymn is round-robin in first-mark order, at most 16 hands, skipping NPCs out of range")]
    private static void CanticleHymnRoundRobin()
    {
        var marks = new CanticleMarks();
        // First marks in the order B, A, C; marks: B 2, A 3, C 1.
        marks.Mark(20, 1, 0, 1); marks.Mark(10, 1, 0, 2); marks.Mark(30, 1, 0, 3);
        marks.Mark(10, 1, 0, 4); marks.Mark(10, 1, 0, 5); marks.Mark(20, 1, 0, 6);
        var plan = CanticlePlan(marks, true, true, true);
        int[] roots = { 20, 10, 30, 20, 10, 10 };
        AssertEqual(roots.Length, plan.Length, "one hand per mark");
        for (int i = 0; i < roots.Length; i++)
        {
            AssertEqual(roots[i], plan[i].Root, $"hand {i} target");
            AssertEqual(i, plan[i].Ordinal, $"hand {i} ordinal");
        }
        AssertEqual(CanticleRules.HandRole.Cadence, plan[^1].Role, "the last hand of a hymn without a Clasp carries the cadence");
        for (int i = 0; i < plan.Length - 1; i++) AssertEqual(CanticleRules.HandRole.Hand, plan[i].Role, $"hand {i} is a plain hand");

        // Out of range: skipped, and it keeps its marks.
        var skipped = CanticlePlan(marks, true, false, true);
        AssertEqual(3, skipped.Length, "B and C only");
        marks.Spend(skipped);
        AssertEqual(0, marks.MarksOn(20), "B spent");
        AssertEqual(0, marks.MarksOn(30), "C spent");
        AssertEqual(3, marks.MarksOn(10), "A, out of range, keeps every mark");
        AssertEqual(0, CanticlePlan(marks, false).Length, "nothing in range: no hymn");

        // Three full NPCs: sixteen hands, round-robin; nobody receives an eighth hand, so no Clasp; the rest stay.
        marks.Clear();
        for (int m = 0; m < MaxMarksPerNpc; m++) for (int root = 0; root < 3; root++) marks.Mark(root, 1, 0, (ulong)(m * 3 + root));
        var full = CanticlePlan(marks, true, true, true);
        AssertEqual(MaxHands, full.Length, "at most 16 hands per hymn");
        foreach (var hand in full) AssertEqual(false, hand.Role == CanticleRules.HandRole.Clasp, "no Clasp without an eighth hand");
        marks.Spend(full);
        AssertEqual(2, marks.MarksOn(0), "extra marks stay (first NPC took six hands)");
        AssertEqual(3, marks.MarksOn(1), "extra marks stay");
        AssertEqual(3, marks.MarksOn(2), "extra marks stay");
    }

    [DomainTest("Canticle Clasp is only a fully marked NPC's eighth hand and pays the full-build figure")]
    private static void CanticleClaspOnlyAtEight()
    {
        var marks = new CanticleMarks();
        for (int m = 0; m < 7; m++) marks.Mark(4, 1, 0, (ulong)m);
        foreach (var hand in CanticlePlan(marks, true)) AssertEqual(false, hand.Role == CanticleRules.HandRole.Clasp, "seven marks: no Clasp");
        marks.Mark(4, 1, 0, 9);
        var plan = CanticlePlan(marks, true);
        AssertEqual(8, plan.Length, "eight hands");
        AssertEqual(CanticleRules.HandRole.Clasp, plan[7].Role, "the eighth hand is the Clasp");
        float total = 0;
        foreach (var hand in plan) total += CanticleRules.SlamMultiplier(hand.Role, true);
        AssertNear(HymnMultiplier(8), total, 1e-4f, "seven hands and the Clasp: 20.25x");
        AssertNear(1, CanticleRules.SlamMultiplier(CanticleRules.HandRole.Clasp, false), 0, "the crown pays x1.0 to other roots");
        AssertNear(HandMultiplier, CanticleRules.SlamMultiplier(CanticleRules.HandRole.Cadence, false), 0, "a cadence hand is a plain hand");
        marks.Spend(plan);
        AssertEqual(0, marks.Count, "every mark spent");

        // Two full NPCs: both receive an eighth hand inside the sixteen, as the last two.
        for (int m = 0; m < MaxMarksPerNpc; m++) { marks.Mark(1, 1, 0, (ulong)m); marks.Mark(2, 1, 0, (ulong)m); }
        var two = CanticlePlan(marks, true, true);
        AssertEqual(16, two.Length, "sixteen hands");
        AssertEqual(CanticleRules.HandRole.Clasp, two[14].Role, "first Clasp");
        AssertEqual(CanticleRules.HandRole.Clasp, two[15].Role, "second Clasp");
        AssertEqual(1, two[14].Root, "first marked first");
    }

    [DomainTest("Canticle hands slam in sixteenths from the cast and live within the effect bounds")]
    private static void CanticleHandTiming()
    {
        for (int k = 0; k < MaxHands; k++)
        {
            // AI adds one before reading: the cast tick reads SpawnAge + 1; the slam (age 0) is HandSlamTick(k) later.
            AssertEqual(-HandSlamTick(k), CanticleRules.SpawnAge(k) + 1, $"hand {k} waits 10 + S({k})");
            AssertEqual(10 + S(k), HandSlamTick(k), $"hand {k} slam tick");
            AssertEqual(true, CanticleRules.ValidAge(CanticleRules.SpawnAge(k)), $"hand {k} spawn age is valid");
        }
        AssertEqual(30, CanticleRules.HandTail, "live 3, dry 24, slack");
        AssertEqual(HandMaxLife, HandSlamTick(MaxHands - 1) + CanticleRules.HandTail, "the last of 16 is gone 145 ticks after the cast");
        AssertEqual(true, HandLive + CanticleRules.Residue <= CanticleRules.HandTail, "the residue dries before the hand ends");
        AssertEqual(false, CanticleRules.Visible(-HandLead - .01f), "no hand exists earlier than 8 ticks before its slam");
        AssertEqual(true, CanticleRules.Visible(-HandLead), "it condenses 8 ticks before");
        AssertNear(HandDrop, CanticleRules.FallHeight(-HandLead), 0, "it forms 240 px above");
        AssertNear(HandDrop, CanticleRules.FallHeight(-HandFall), 0, "two ticks forming, then the fall");
        AssertNear(0, CanticleRules.FallHeight(0), 0, "onto the landing point on the slam");
        float previousHeight = HandDrop, previousStep = 0;
        for (float a = -HandFall + .5f; a <= 0; a += .5f)
        {
            float h = CanticleRules.FallHeight(a), step = previousHeight - h;
            AssertEqual(true, step > previousStep - 1e-4f, $"the fall accelerates at {a}");
            previousHeight = h; previousStep = step;
        }
        for (int age = -3; age <= 6; age++) AssertEqual(age >= 1 && age <= 3, CanticleRules.IsLive(age), $"live window at {age}");
        AssertEqual(-25f, CanticleRules.LandingOffset(0, CanticleRules.HandRole.Hand, 100), "hand 0: the outer left arm");
        AssertEqual(0f, CanticleRules.LandingOffset(7, CanticleRules.HandRole.Clasp, 100), "the Clasp closes on the centre");
        AssertNear(-25f, CanticleRules.ClaspHandOffset(0, -HandFall, 100), 1e-4f, "the Clasp's hands start at the arm offsets");
        AssertNear(-25f * CanticleRules.ClaspClose, CanticleRules.ClaspHandOffset(0, 0, 100), 1e-4f, "and close on the target");
    }

    [DomainTest("Canticle projectiles reject invalid ai")]
    private static void CanticleCodec()
    {
        for (int k = 0; k < MaxHands; k++)
            foreach (CanticleRules.HandRole role in Enum.GetValues<CanticleRules.HandRole>())
            {
                AssertEqual(true, CanticleRules.TryHand(CanticleRules.HandCode(k, role), out int ordinal, out var back), $"code {k} {role}");
                AssertEqual(k, ordinal, "ordinal round-trips");
                AssertEqual(role, back, "role round-trips");
            }
        foreach (float bad in new[] { -1f, CanticleRules.MaxHandCode + 1f, 1.5f, float.NaN, float.PositiveInfinity })
            AssertEqual(false, CanticleRules.TryHand(bad, out _, out _), $"code {bad} rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => CanticleRules.HandCode(MaxHands, CanticleRules.HandRole.Hand), "ordinal 16");
        AssertEqual(true, CanticleRules.ValidAge(CanticleRules.SpawnAge(MaxHands - 1)), "the longest wait");
        AssertEqual(false, CanticleRules.ValidAge(CanticleRules.SpawnAge(MaxHands - 1) - 1), "longer than any wait");
        AssertEqual(true, CanticleRules.ValidAge(CanticleRules.HandTail), "the last tick");
        foreach (float bad in new[] { CanticleRules.HandTail + 1f, .5f, float.NaN })
            AssertEqual(false, CanticleRules.ValidAge(bad), $"age {bad} rejected");
        AssertEqual(true, CanticleRules.Expired(CanticleRules.HandTail + 1), "past its tail the hand ends");
    }

    [DomainTest("Canticle hand ink draws exactly what collides, and nothing hovers before a hand appears")]
    private static void CanticleDrawEqualsCollide()
    {
        Vector2 landing = new(500, 300);
        foreach (var role in new[] { CanticleRules.HandRole.Hand, CanticleRules.HandRole.Clasp })
        {
            bool clasp = role == CanticleRules.HandRole.Clasp;
            for (int t = 1; t <= HandLive; t++)
            {
                // The collision on tick t is drawn while the drawn clock runs from t - 1 to t; at its end they coincide.
                var ink = new CanticleRecorder();
                CanticleRules.HandInk(ink, landing, 3, role, 80, t - 1e-3f, 1, false);
                int live = 0;
                foreach (var path in ink.Paths)
                {
                    if (path.Look != CanticleInkLook.Live) continue;
                    live++;
                    foreach (var point in path.Points)
                    {
                        float drawn = point.Radius * InkOpen(point.Time);
                        bool disc = path.Points.Count == 1;
                        float expected = disc ? CanticleRules.SlamRadius(clasp, t) : CanticleRules.CrownRadiusAt(t);
                        AssertNear(expected, drawn, .02f * expected + .01f, $"{role} t{t} drawn radius = collision radius");
                    }
                }
                AssertEqual(clasp ? 1 + CrownSpokes : 1, live, $"{role} t{t}: the disc{(clasp ? " and six crown capsules" : "")}");
            }
            var after = new CanticleRecorder();
            CanticleRules.HandInk(after, landing, 3, role, 80, HandLive + .5f, 1, false);
            AssertEqual(0, after.Count(CanticleInkLook.Live), $"{role}: no live ink after the live window");
            AssertEqual(true, after.Count(CanticleInkLook.Residue) > 0, $"{role}: it dries into residue");
            var before = new CanticleRecorder();
            CanticleRules.HandInk(before, landing, 3, role, 80, -HandLead - .5f, 1, false);
            AssertEqual(0, before.Paths.Count + before.Droplets, $"{role}: nothing exists before it condenses");
            var falling = new CanticleRecorder();
            CanticleRules.HandInk(falling, landing, 3, role, 80, -3, 1, false);
            AssertEqual(0, falling.Count(CanticleInkLook.Live), $"{role}: a falling hand is dormant ink only");
        }

        Vector2 min = landing - new Vector2(10, 10), max = landing + new Vector2(10, 10);
        AssertEqual(false, CanticleRules.SlamTouches(landing, false, 0, min, max), "not live on the slam tick itself");
        AssertEqual(true, CanticleRules.SlamTouches(landing, false, 1, min, max), "the target under the palm on the first live tick");
        AssertEqual(false, CanticleRules.SlamTouches(landing, false, 4, min, max), "not after three live ticks");
        Vector2 side = landing + new Vector2(HandRadius + 2, 0);
        AssertEqual(false, CanticleRules.SlamTouches(landing, false, 3, side, side + new Vector2(20, 20)), "48 px disc: radius 48");
        AssertEqual(true, CanticleRules.SlamTouches(landing, false, 3, side - new Vector2(3, 0), side + new Vector2(20, 20)), "inside 48 px at full open");
        Vector2 crownTip = landing + new Vector2(0, -CrownOuter + 4);
        AssertEqual(false, CanticleRules.SlamTouches(landing, false, 3, crownTip, crownTip + new Vector2(4, 4)), "a hand has no crown");
        AssertEqual(true, CanticleRules.SlamTouches(landing, true, 3, crownTip, crownTip + new Vector2(4, 4)), "the Clasp's crown reaches 140 px");
        for (int i = 0; i < CrownSpokes; i++)
            AssertNear(1, CanticleRules.CrownDirection(i).Length(), 1e-5f, $"spoke {i} is radial");
        AssertNear(-1, CanticleRules.CrownDirection(0).Y, 1e-5f, "the first spoke is straight up");
    }

    [DomainTest("Canticle commitment: unlanded hands retire on owner loss, landed hands finish, builds outlast Down")]
    private static void CanticleCommitment()
    {
        for (int age = CanticleRules.SpawnAge(MaxHands - 1) + 1; age < 0; age++)
        {
            AssertEqual(true, CanticleRules.RetireOnOwnerLoss(age), $"age {age}: not landed, retired on death or Down");
            AssertEqual(true, CanticleRules.Pending(age), $"age {age}: the hymn is still pending");
        }
        for (int age = 0; age <= CanticleRules.HandTail; age++)
        {
            AssertEqual(false, CanticleRules.RetireOnOwnerLoss(age), $"age {age}: landed, finishes its window");
            AssertEqual(false, CanticleRules.Pending(age), $"age {age}: no longer pending");
        }
        // The ledger only forgets on expiry, eviction, a lost NPC or death (Clear): an item swap or a Down changes nothing.
        var marks = new CanticleMarks();
        for (int i = 0; i < 5; i++) marks.Mark(2, 1, 0, 1000);
        AssertEqual(0, marks.Expire(1000 + 300), "marks survive a long Down within their lifetime");
        AssertEqual(5, marks.MarksOn(2), "the build is kept");
        marks.Clear();
        AssertEqual(0, marks.Total, "death clears the build");
    }

    [DomainTest("Canticle aim spring and pipe kick are critically damped and never overshoot")]
    private static void CanticleSpringAndKick()
    {
        float angle = 0, velocity = 0, previous = 0;
        for (int t = 1; t <= 30; t++)
        {
            (angle, velocity) = CanticleRules.SpringAim(angle, velocity, 1);
            AssertEqual(true, angle >= previous - 1e-5f && angle <= 1 + 1e-5f, $"tick {t}: monotonic, never past the cursor");
            previous = angle;
            if (t == 6) AssertEqual(true, angle > .9f, "about 92% of a flick in six ticks");
        }
        AssertNear(1, angle, 1e-3f, "settles on the cursor");
        // Across the seam the spring goes the short way round.
        (angle, _) = CanticleRules.SpringAim(3.0f, 0, -3.0f);
        AssertEqual(true, MathF.Abs(angle) > 3.0f, "turns through pi, not through zero");
        AssertNear(0, CanticleRules.SpringAim(float.NaN, 0, 0).Angle, 0, "invalid state snaps to the target");
        AssertNear(CanticleRules.KickDistance, CanticleRules.Kick(0), 1e-5f, "the pipe kicks back 2 px");
        float last = CanticleRules.Kick(0);
        for (float t = .5f; t <= 20; t += .5f)
        {
            float k = CanticleRules.Kick(t);
            AssertEqual(true, k <= last + 1e-6f && k >= 0, $"springs home without passing its rest ({t})");
            last = k;
        }
        AssertEqual(true, CanticleRules.Kick(12) < .05f, "home within a use");
        AssertNear(0, CanticleRules.Inhale(0), 0, "the inhale starts at the cast");
        AssertNear(1, CanticleRules.Inhale(HymnInhale), 1e-4f, "full at tick 10");
        AssertNear(0, CanticleRules.Inhale(HymnUseTicks), 0, "settled by the end of the 24-tick use");
    }

    [DomainTest("Canticle shards fly 1,600 px, four pipes fire in turn, marks draw one arc per NPC")]
    private static void CanticleShardsAndMarks()
    {
        AssertNear(1600, CanticleRules.ShardRange, 0, "speed 20, one extra update, 40 ticks");
        AssertEqual(80, CanticleRules.ShardTimeLeft, "timeLeft counts both updates of a tick");
        int pipe = 0;
        for (int i = 0; i < 8; i++) { AssertEqual(i % 4, pipe, $"shot {i} pipe"); pipe = CanticleRules.NextPipe(pipe); }
        Vector2 right = CanticleRules.MouthOffset(0, 0), left = CanticleRules.MouthOffset(0, MathF.PI);
        AssertNear(CanticleRules.MouthForward, right.X, 1e-4f, "mouths ahead of the grip");
        AssertEqual(true, right.Y < 0 && left.Y < 0, "flipped when aiming left: the pipes stay on top");
        AssertNear(-right.X, left.X, 1e-3f, "mirrored");

        for (int marks = 1; marks <= MaxMarksPerNpc; marks++)
        {
            var ink = new CanticleRecorder();
            CanticleRules.MarkInk(ink, new Vector2(0, 0), marks, 40, 1);
            AssertEqual(1, ink.Paths.Count, $"{marks} marks: one path per NPC");
            AssertEqual(CanticleInkLook.Dormant, ink.Paths[0].Look, "marks are dormant ink");
            AssertNear(marks == MaxMarksPerNpc ? 1 : 0, ink.Paths[0].Warmth, 0, "at eight marks the lips warm");
            for (int i = 0; i < marks; i++)
                AssertNear(CanticleRules.MarkHead, CanticleRules.MarkRadius(CanticleRules.MarkSlotX(i), marks, 1), .3f, $"a note-head at mark {i + 1}");
        }
        AssertEqual(true, CanticleRules.MarkRadius(CanticleRules.MarkSlotX(0) + CanticleRules.MarkSlotGap * .5f, 2, 1) < CanticleRules.MarkHead * .6f, "thin between heads");
        AssertEqual(true, CanticleRules.MarkFlare(0) > 1.5f && CanticleRules.MarkFlare(20) < 1.02f, "the newest head flares as it lands, then settles");
        var fading = new CanticleRecorder();
        CanticleRules.MarkInk(fading, Vector2.Zero, 3, MarkLife, 1);
        AssertEqual(0, fading.Paths.Count, "expired marks draw nothing");
    }
}
