using System;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Score = Convergence.Content.Encounters.FirstSeverance.Rewards.LacunaTestamentScore;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static int LacunaBase => RitualArmamentRules.Damage(RitualArmamentKind.Magic);

    [DomainTest("Lacuna Testament reproduces the Doll weapon budget per cycle, sustained and over the best cold window")]
    private static void LacunaTestamentBudget()
    {
        int magic = LacunaBase;
        AssertEqual(2024, magic, "Lacuna base damage stays frozen");
        AssertEqual(1113, Score.PelletDamage(magic), "pellet x0.55 with per-hit rounding");
        AssertEqual(4048, Score.BeamDamage(magic), "beam x2.0 with per-hit rounding");

        // Per cycle: the build, ages 1 .. Fire - 1.
        double build = 0, buildMultiplier = 0;
        int pellets = 0;
        for (int age = 1; age < Score.Fire; age++)
        {
            int shots = Score.ShotsAt(age);
            pellets += shots;
            build += shots * Score.PelletDamage(magic);
            buildMultiplier += shots * Score.PelletFactor;
        }
        AssertEqual(26, pellets, "26 pellets per full build (9 + 6 + 4 + 3 + 2 + 1 + 1)");
        AssertTrue(DollWeaponBudget.Within(build, DollWeaponBudget.LacunaBuild.CycleRaw), $"build {build} within 3% of {DollWeaponBudget.LacunaBuild.CycleRaw}");
        AssertDollNear(DollWeaponBudget.LacunaBuild.CycleMultiplier, buildMultiplier, 1e-3, "build multiplier");
        AssertEqual(DollWeaponBudget.LacunaBuild.CycleTicks, Score.Fire, "the build lasts until the fire tick");

        // Sustained: the beam on one root, one hit every HitCadence ticks.
        double second = 0;
        for (int age = Score.FirstBeamHit + 600; age < Score.FirstBeamHit + 600 + 60; age++) second += Score.RawAt(age, magic);
        AssertTrue(DollWeaponBudget.Within(second, DollWeaponBudget.LacunaBeam.PerSecond), $"beam {second}/s within 3% of {DollWeaponBudget.LacunaBeam.PerSecond}/s");
        double tenMinutes = 0;
        for (int age = Score.FirstBeamHit; age < Score.FirstBeamHit + 36000; age++) tenMinutes += Score.RawAt(age, magic);
        AssertTrue(DollWeaponBudget.Within(tenMinutes / 600, DollWeaponBudget.LacunaBeam.PerSecond), "ten held minutes average the same per second");

        // Best 600-tick window from a cold press over every release-and-repress timing: holding through is best and
        // equals the baseline; a release at r stops damage that tick, the press waits for the 20-tick fade.
        double Timeline(int release, int tick)
        {
            // timing 0: held throughout; otherwise released at score age `release`, pressed again when the fade ends.
            int age = tick + 1;
            if (release == 0 || age < release) return Score.RawAt(age, magic);
            int again = release + Score.FadeTicks + 1;
            return age >= again ? Score.RawAt(age - again + 1, magic) : 0;
        }
        double best = DollWeaponBudget.BestColdWindow(DollWeaponBudget.ColdWindowTicks, Timeline, out int bestRelease);
        AssertEqual(0, bestRelease, "holding through is the best timing");
        AssertTrue(DollWeaponBudget.Within(best, DollWeaponBudget.LacunaColdWindow), $"best cold window {best} within 3% of {DollWeaponBudget.LacunaColdWindow}");
        AssertDollNear(DollWeaponBudget.LacunaColdWindow, best, 1e-6, "cold window: 26 pellets and 19 beam hits");
        for (int release = 1; release < DollWeaponBudget.ColdWindowTicks; release++)
            AssertTrue(DollWeaponBudget.ColdWindow(tick => Timeline(release, tick)) <= best, $"release at {release} never beats holding");
    }

    [DomainTest("Lacuna Testament iris score: accelerating births, four-step opening, tells and the unchanged pellet schedule")]
    private static void LacunaTestamentIrisScore()
    {
        for (int i = 1; i < Score.Irises; i++)
        {
            int gap = Score.Birth(i) - Score.Birth(i - 1);
            AssertTrue(gap >= 20, $"iris {i} gap {gap} >= 20");
            if (i > 1) AssertTrue(gap < Score.Birth(i - 1) - Score.Birth(i - 2), $"iris {i} arrives sooner than the last gap");
        }
        for (int i = 0; i < Score.Irises; i++)
        {
            int b = Score.Birth(i);
            AssertEqual(-1, Score.Frame(b - .5f, i), $"iris {i} unborn before its birth");
            AssertEqual(0, Score.Frame(b, i), $"iris {i} closed at birth");
            AssertEqual(1, Score.Frame(b + Score.FrameParting, i), $"iris {i} parting");
            AssertEqual(2, Score.Frame(b + Score.FrameHalf, i), $"iris {i} half");
            AssertEqual(3, Score.Frame(b + Score.ShotDelay, i), $"iris {i} open on its first shot");
            AssertTrue(Score.IsFirstShot(b + Score.ShotDelay, i) && Score.ShotAt(b + Score.ShotDelay, i), $"iris {i} fires the moment it opens");
        }
        int count = 0;
        for (int tick = 0; tick < 36000; tick++)
        {
            for (int i = 0; i < Score.Irises; i++)
            {
                AssertEqual(RitualGrandScore.MagicBoltAt(tick, i), Score.ShotAt(tick, i), $"pellet schedule at {tick}, iris {i}");
                if (!Score.ShotAt(tick, i)) continue;
                count++;
                AssertTrue(tick < Score.Merge, "no pellet at or after the merge");
                if (Score.IsFirstShot(tick, i)) continue;
                for (int ahead = 1; ahead <= Score.ShotTell; ahead++)
                {
                    AssertEqual(2, Score.Frame(tick - ahead, i), $"iris {i} half-closes {ahead} tick(s) before its shot at {tick}");
                    AssertTrue(Score.TellAt(tick - Score.ShotTell, i), $"the tell of the shot at {tick} starts {Score.ShotTell} ticks early");
                }
                AssertEqual(3, Score.Frame(tick, i), $"iris {i} snaps open on its shot at {tick}");
            }
        }
        AssertEqual(26, count, "26 pellets in all");
        for (int i = 0; i < Score.Irises; i++)
            AssertEqual(3, Score.Frame(Score.Merge + 5, i), $"iris {i} stays open through the merge");
    }

    [DomainTest("Lacuna Testament merge, charge and beam clocks: docks, seven accelerating clicks, a still breath, a widening beam")]
    private static void LacunaTestamentBeamClocks()
    {
        for (int i = 0; i < Score.Irises; i++)
        {
            AssertEqual(350 + 2 * i, Score.Docked(i), $"iris {i} docks");
            AssertDollNear(0, Score.MergeT(Score.Merge, i), 1e-6, $"iris {i} leaves its seat at the merge");
            AssertDollNear(1, Score.MergeT(Score.Docked(i), i), 1e-6, $"iris {i} docked");
            AssertTrue(Score.Docked(i) <= Score.Formed, $"iris {i} docks by the seat tick");
        }
        int previous = Score.Formed, lastGap = int.MaxValue, clicks = 0;
        for (int tick = Score.Formed + 1; tick < Score.Fire; tick++)
        {
            if (Score.ChargeClick(tick) < 0) continue;
            clicks++;
            int gap = tick - previous;
            // Gaps between clicks shrink (10, 8, 6, 5, 4, 3); the first click comes 6 ticks after the aperture forms.
            if (clicks > 2) AssertTrue(gap < lastGap, $"click {clicks} accelerates ({gap} < {lastGap})");
            if (clicks > 1) lastGap = gap;
            previous = tick;
        }
        AssertEqual(7, clicks, "seven ratchet clicks in the charge");
        AssertTrue(Score.Breath <= Score.Fire - 6, "the last click leaves a still breath of at least 6 ticks");
        for (float age = Score.Breath; age < Score.Fire; age += .25f)
            AssertTrue(Score.Tension(age) && Score.Spiral(age) == 0, $"nothing moves in the breath at {age}");
        AssertDollNear(1, Score.VoidDepth(Score.Breath), 1e-5, "the hole is black by the breath");

        AssertDollNear(0, Score.Opening(Score.Fire), 1e-6, "the beam opens after the fire tick");
        AssertDollNear(1, Score.Opening(Score.Fire + Score.OpenTicks), 1e-6, "open after OpenTicks");
        AssertDollNear(Score.OpenWidth, Score.Width(Score.WidenStart), 1e-3, "opening width");
        AssertDollNear(Score.FullWidth, Score.Width(Score.WidenStart + Score.WidenTicks), 1e-3, "full width after 6 s");
        AssertTrue(Score.WidenStart + Score.WidenTicks - Score.Fire <= 360, "full width within 6 s of the fire tick");
        float last = 0;
        for (float age = 0; age < 36000; age += .5f)
        {
            float width = Score.Width(age);
            AssertTrue(width >= last - 1e-4f && width <= Score.FullWidth + 1e-4f, $"width monotone and bounded at {age}");
            last = width;
        }
        double sum = 0;
        int samples = 0;
        for (int age = Score.WidenStart; age < Score.WidenStart + Score.WidenTicks; age++) { sum += Score.Width(age + .5f); samples++; }
        AssertDollNear(116, sum / samples, .25, "mean width over the widening equals the former constant 116 px");
        AssertDollNear(Score.ThroatShare * 120, Score.CollisionWidth(Score.ThroatLength - .01f, 120), 1e-4, "the throat collides narrower");
        AssertDollNear(120, Score.CollisionWidth(Score.ThroatLength, 120), 1e-4, "past the throat the full width");

        AssertDollNear(Score.TurnBuild, Score.TurnCap(Score.Fire - 1), 1e-6, "build turn cap");
        AssertDollNear(Score.TurnBeam, Score.TurnCap(Score.Fire), 1e-6, "beam turn cap");
        AssertDollNear(Score.TurnBeamFull, Score.TurnCap(Score.WidenStart + Score.WidenTicks), 1e-6, "full-width turn cap");
        for (float age = Score.Fire; age < Score.Fire + 600; age += 1)
            AssertTrue(Score.TurnCap(age + 1) <= Score.TurnCap(age) + 1e-6f, $"turn cap never rises in the beam ({age})");

        int beats = 0;
        for (int tick = 0; tick < 36000; tick++) if (Score.WidenBeat(tick) >= 0) beats++;
        AssertEqual(3, beats, "three widen beats");
        AssertEqual(Score.Clicks + Score.WidenBeats, Score.RatchetSteps(36000), "every click and widen beat turns the ring one notch");
        for (float age = 0; age < 2000; age += .5f)
        {
            Score.PulseBands(age, out float a, out float b);
            if (age < Score.PulseFirst) AssertTrue(a < 0 && b < 0, $"no pulse before {Score.PulseFirst} ({age})");
            AssertTrue(a <= Score.Length + Score.PulseTail && b <= Score.Length + Score.PulseTail, $"pulses stay on the beam ({age})");
        }
    }

    [DomainTest("Lacuna Testament live windows: no damage before the beam opens or after a release; mana keeps the legacy cadence")]
    private static void LacunaTestamentLiveWindows()
    {
        for (float age = 0; age <= Score.Fire; age += .25f)
            AssertEqual(false, Score.Live(age, 0), $"nothing damages before the beam opens ({age})");
        AssertEqual(true, Score.Live(Score.FirstBeamHit, 0), "the beam can hit the tick after the fire tick");
        for (int fade = -1; fade >= -Score.FadeTicks; fade--)
            AssertEqual(false, Score.Live(900, fade), $"a released beam never damages (fade {fade})");
        AssertEqual(true, Score.ValidFade(0) && Score.ValidFade(-Score.FadeTicks), "fade bounds");
        AssertEqual(false, Score.ValidFade(1) || Score.ValidFade(-Score.FadeTicks - 1) || Score.ValidFade(float.NaN), "invalid fades");
        for (int fade = 0; fade <= Score.RetractTicks; fade++)
            AssertTrue(Score.Retract(fade) <= Score.Retract(fade - 1) + 1e-6f, $"the beam retracts monotonically ({fade})");
        AssertDollNear(0, Score.Retract(Score.RetractTicks), 1e-6, "retracted into the hole");
        for (int i = 0; i < Score.Irises; i++)
            AssertEqual(0, Score.ClosingFrame(i, Score.FadeTicks - 1, 3), $"iris {i} shut before the fade ends");

        AssertEqual(false, Score.Pays(0), "the item use pays the first 8");
        int build = 0, sustain = 0;
        for (int tick = 0; tick < Score.Fire; tick++) if (Score.Pays(tick)) build++;
        for (int tick = Score.Fire; tick < Score.Fire + 480; tick++) if (Score.Pays(tick)) sustain++;
        AssertEqual(13, build, "13 build payments (ticks 30 .. 390)");
        AssertEqual(60, sustain, "60 sustain payments in 8 s");
        AssertEqual(true, Score.Pays(Score.Fire), "the fire tick pays");
        AssertEqual(3, Score.ManaCost(8, Score.Fire - 1), "build pays 35% of the cost, rounded up");
        AssertEqual(8, Score.ManaCost(8, Score.Fire), "sustain pays the full cost");
        AssertEqual(0, Score.ManaCost(0, 30), "zero-cost equipment is honoured");
        for (int tick = 0; tick < 36000; tick++)
            AssertEqual(RitualGrandScore.MagicPays(tick), Score.Pays(tick), $"legacy mana cadence at {tick}");
    }

    [DomainTest("Lacuna Testament geometry: a seat arch above the owner, a rosette at the muzzle and continuous iris paths")]
    private static void LacunaTestamentGeometry()
    {
        foreach (int facing in new[] { 1, -1 })
        foreach (float gravDir in new[] { 1f, -1f })
        {
            for (int i = 0; i < Score.Irises; i++)
            {
                Vector2 seat = Score.Seat(i, facing, gravDir);
                AssertTrue(seat.Y * gravDir <= -40, $"seat {i} above the owner ({seat})");
                AssertDollNear(Score.Seat(i, 1, 1).X * facing, seat.X, 1e-4, $"seat {i} mirrored by facing");
                AssertDollNear(Score.Seat(i, 1, 1).Y * gravDir, seat.Y, 1e-4, $"seat {i} mirrored by gravity");
                for (int j = 0; j < i; j++)
                    AssertTrue(Vector2.Distance(seat, Score.Seat(j, facing, gravDir)) >= 60, $"seats {i} and {j} keep 60 px apart");
                AssertDollNear(Score.DockRadius, Score.Dock(i, facing, gravDir).Length(), 1e-3, $"dock {i} on the rosette");
                for (int j = 0; j < i; j++)
                    AssertTrue(Vector2.Distance(Score.Dock(i, facing, gravDir), Score.Dock(j, facing, gravDir)) >= 50, $"docks {i} and {j} hold a 50 px iris apart");
            }
            Vector2 bookHole = new(facing * 30, -2), muzzle = new(facing * Score.MuzzleDistance, 0);
            for (int i = 0; i < Score.Irises; i++)
            {
                Vector2 previous = Score.IrisOffset(i, Score.Birth(i), facing, gravDir, bookHole, muzzle);
                AssertDollNear(bookHole, previous, 1e-3f, $"iris {i} leaves the book's hole");
                for (float age = Score.Birth(i) + .25f; age <= Score.Formed; age += .25f)
                {
                    Vector2 at = Score.IrisOffset(i, age, facing, gravDir, bookHole, muzzle);
                    AssertTrue(Vector2.Distance(previous, at) <= 40, $"iris {i} path is continuous at {age}");
                    previous = at;
                }
                AssertDollNear(Score.Seat(i, facing, gravDir), Score.IrisOffset(i, Score.Birth(i) + Score.ShotDelay, facing, gravDir, bookHole, muzzle), 1e-3f,
                    $"iris {i} is seated when it first fires");
                AssertDollNear(muzzle + Score.Dock(i, facing, gravDir), Score.IrisOffset(i, Score.Docked(i), facing, gravDir, bookHole, muzzle), 1e-3f,
                    $"iris {i} docks on the rosette");
            }
        }
        // The great aperture clears the book, and the seats clear the great aperture's centre.
        AssertTrue(Score.MuzzleDistance - LacunaArtFit.GreatOuterRadius > Score.BookDistance + 12 + DollArtAnchors.LacunaBook_S.Width,
            "the great aperture's ring stays past the held book");
        for (int i = 0; i < Score.Irises; i++)
            AssertTrue(Vector2.Distance(Score.Seat(i, 1), new Vector2(Score.MuzzleDistance, 0)) > 50, $"seat {i} off the muzzle");
    }

    [DomainTest("Lacuna Testament art fit: drawn holes, mouth and rosette land within one dot of their design anchors")]
    private static void LacunaTestamentArtFit()
    {
        AssertEqual(2, LacunaArtFit.BookK, "the held book is the k=2 rung");
        AssertEqual(2, LacunaArtFit.IrisK, "irises are the k=2 rung");
        AssertEqual(1, LacunaArtFit.GreatK, "the great aperture is the k=1 rung");
        AssertEqual(4, DollArtAnchors.LacunaIris.Width / DollArtAnchors.LacunaIris.FrameWidth, "four iris frames");
        var random = new Random(410);
        for (int n = 0; n < 400; n++)
        {
            Vector2 at = new(2000 + (float)random.NextDouble() * 900, 900 + (float)random.NextDouble() * 600);
            // Mouth: the great aperture's hole on the muzzle (the beam origin), at every ratchet step.
            float rotation = n % (Score.Clicks + Score.WidenBeats + 1) * Score.RatchetStep;
            AssertDollNear(at, LacunaArtFit.GreatHoleAt(at, rotation), LacunaArtFit.Tolerance, $"mouth at step {n % 11}");
            // Hole: each iris's hole on its seat (pellets leave the seat).
            AssertDollNear(at, LacunaArtFit.IrisHoleAt(at), LacunaArtFit.Tolerance, "iris hole");
            // The book's hole stays inside the book (irises are born there).
            Vector2 hole = LacunaArtFit.BookHoleAt(at, n % 2 == 0 ? DollFlip.None : DollFlip.Horizontal);
            AssertTrue(MathF.Abs(hole.X - at.X) < DollArtAnchors.LacunaBook_S.Width && MathF.Abs(hole.Y - at.Y) < DollArtAnchors.LacunaBook_S.Height,
                "the book's hole is on the book");
        }
        // The rosette sits on the great ring, so the docked irises become its band.
        AssertDollNear(Score.DockRadius, LacunaArtFit.GreatRingRadius, LacunaArtFit.Tolerance, "docks on the great ring");
        // The opening throat pours from inside the hole; a pellet leaves through an open iris.
        AssertTrue(Score.ThroatShare * Score.OpenWidth * .5f <= LacunaArtFit.GreatHoleRadius, "opening throat inside the great hole");
        AssertTrue(Score.PelletWidth * .5f <= LacunaArtFit.IrisApertureRadius(3), "a pellet fits the open iris");
        AssertTrue(LacunaArtFit.IrisApertureRadius(0) == 0 && LacunaArtFit.IrisApertureRadius(1) < LacunaArtFit.IrisApertureRadius(2)
            && LacunaArtFit.IrisApertureRadius(2) < LacunaArtFit.IrisApertureRadius(3), "apertures open in order");
        AssertTrue(Score.PelletHoleOffset <= LacunaArtFit.IrisApertureRadius(3), "pellets spawn inside the open hole");
        AssertDollNear(2, LacunaArtFit.Tolerance, 1e-6, "the art-fit tolerance is one 2-px dot at every rung");
    }

    [DomainTest("Lacuna Testament book never covers the owner's head at any aim, and glides rather than jumps")]
    private static void LacunaTestamentBookClearsHead()
    {
        Vector2 half = LacunaArtFit.BookHalf;
        AssertDollNear(new Vector2(21, 28), half, 1e-4f, "the held book is 42 x 56 px");
        Vector2 centre = new(3000, 1800);
        foreach (float gravDir in new[] { 1f, -1f })
        foreach (int direction in new[] { 1, -1 })
        foreach (float recoil in new[] { 0f, 1f })
        foreach (float bob in new[] { -2f, 0f, 2f })
        {
            Vector2 previous = default;
            for (int step = 0; step <= 1440; step++)
            {
                float aim = step * MathF.Tau / 1440;
                Vector2 axis = new(MathF.Cos(aim), MathF.Sin(aim));
                // The front hand at full stretch (Player.GetFrontHandPosition, roughly): a shoulder a little behind and
                // above the centre, the arm 13 px along the aim.
                Vector2 hand = centre + new Vector2(-3 * direction, -4 * gravDir) + axis * 13;
                Vector2 bookBase = hand + axis * (Score.BookDistance - 6 * recoil) + new Vector2(0, -2);
                Vector2 book = LacunaArtFit.ClearHead(bookBase, axis, centre, gravDir) + new Vector2(0, bob);
                float minY = gravDir > 0 ? LacunaArtFit.HeadMin.Y : -LacunaArtFit.HeadMax.Y;
                float maxY = gravDir > 0 ? LacunaArtFit.HeadMax.Y : -LacunaArtFit.HeadMin.Y;
                // One more px either way for the dot-grid snap.
                bool apart = book.X + half.X + 1 <= centre.X + LacunaArtFit.HeadMin.X || book.X - half.X - 1 >= centre.X + LacunaArtFit.HeadMax.X
                    || book.Y + half.Y + 1 <= centre.Y + minY || book.Y - half.Y - 1 >= centre.Y + maxY;
                AssertTrue(apart, $"the book clears the head at aim {aim * 180 / MathF.PI:0.##} deg (gravity {gravDir}, facing {direction})");
                AssertTrue(Vector2.Distance(book, bookBase + new Vector2(0, bob)) <= 60, $"the book stays near the hand at {aim:0.###}");
                if (step > 0) AssertTrue(Vector2.Distance(book, previous) <= 4, $"the book glides at {aim:0.###} rad");
                previous = book;
            }
        }
        // A level aim leaves the book where it was: only steep upward aims push it out.
        Vector2 level = centre + new Vector2(16 + Score.BookDistance, -6);
        AssertDollNear(level, LacunaArtFit.ClearHead(level, Vector2.UnitX, centre, 1), 1e-4f, "a level aim is untouched");
    }

    [DomainTest("Lacuna Testament running dry gutters out in at most 1.5 flashes, as a plain fade under Reduced Effects")]
    private static void LacunaTestamentStarveFlicker()
    {
        foreach (bool reduced in new[] { false, true })
        {
            int changes = 0;
            bool lit = true;
            float last = 1;
            for (float fade = 0; fade <= Score.FadeTicks; fade += .125f)
            {
                float v = Score.StarveFlicker(fade, reduced);
                AssertTrue(v >= 0 && v <= 1, $"flicker bounded at {fade}");
                if (fade >= Score.StarveTicks) AssertEqual(0f, v, $"the beam is gone by {Score.StarveTicks} ticks ({fade})");
                if (reduced) AssertTrue(v <= last + 1e-6f, $"Reduced Effects fades without a flicker ({fade})");
                if (v > 0 != lit) { changes++; lit = v > 0; }
                last = v;
            }
            AssertTrue(changes <= 3, $"at most three on/off changes, 1.5 flashes (reduced {reduced}): {changes}");
            AssertDollNear(1, Score.StarveFlicker(0, reduced), 1e-6, "lit on the starving tick");
        }
        AssertEqual(0f, Score.StarveFlicker(float.NaN, false), "a bad fade draws nothing");
    }

    [DomainTest("Lacuna Testament cue schedule finds each event once, in order")]
    private static void LacunaTestamentCueSchedule()
    {
        int births = 0, openings = 0, shots = 0, tells = 0, widens = 0;
        int lastBirth = -1, lastOpening = -1, lastShot = -1, lastTell = -1, lastWiden = -1;
        for (int age = 0; age < 1200; age++)
        {
            int tick = Score.LatestBirth(age, out int iris);
            if (tick == age) { births++; AssertEqual(Score.Birth(iris), tick, "birth tick"); }
            AssertTrue(tick >= lastBirth, "births in order"); lastBirth = tick;
            tick = Score.LatestOpening(age, out iris);
            if (tick == age) { openings++; AssertTrue(Score.IsFirstShot(tick, iris), "opening is a first shot"); }
            AssertTrue(tick >= lastOpening, "openings in order"); lastOpening = tick;
            tick = Score.LatestShot(age, false, out iris);
            if (tick == age) { shots++; AssertTrue(Score.ShotAt(tick, iris) && !Score.IsFirstShot(tick, iris), "later shot"); }
            AssertTrue(tick >= lastShot, "shots in order"); lastShot = tick;
            tick = Score.LatestShot(age, true, out iris);
            if (tick == age) { tells++; AssertTrue(Score.TellAt(tick, iris), "tell"); }
            AssertTrue(tick >= lastTell, "tells in order"); lastTell = tick;
            tick = Score.LatestWiden(age, out int beat);
            if (tick == age) { widens++; AssertEqual(Score.WidenBeatAt(beat), tick, "widen beat"); }
            AssertTrue(tick >= lastWiden, "widens in order"); lastWiden = tick;
        }
        AssertEqual(7, births, "seven births");
        AssertEqual(7, openings, "seven openings");
        AssertEqual(19, shots, "19 later shots (26 pellets less 7 openings)");
        AssertEqual(19, tells, "one tell per later shot");
        AssertEqual(3, widens, "three widen beats");
    }
}
