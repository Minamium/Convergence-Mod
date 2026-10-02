using System;
using System.IO;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

// Act signature moves (protocol 79): selection cadence, exact corridor / rope heights / quarter
// geometry against a 20x42 player, and the descriptor codec.
internal static partial class Program
{
    private const float ScarletPlayerWidth = 20, ScarletPlayerHeight = 42;
    private static readonly Guid ScarletSignatureFight = Guid.Parse("5b1d3a52-0f0e-4c42-9a55-1d4c6a0b7e11");

    private static CrimsonGesturePlan ScarletSignaturePlan(int phase, int phrase, int note, float observedX, int groundX = 8000, int groundY = 6000, int earliest = -1)
    {
        var f = RaidFieldGeometry.FromGround(groundX, groundY);
        var rhythm = CrimsonChoreography.Create(earliest < 0 ? CrimsonChoreography.OpeningTicks : earliest, phrase, false);
        var hit = rhythm.Hits[note];
        var technique = CrimsonEnsemble.Technique(phase, phrase, note, false);
        var stage = new CrimsonPoint(f.CenterX, f.Top + 210);
        var target = phase == 0 ? new CrimsonPoint(observedX, f.CenterY) : new CrimsonPoint(f.CenterX, f.CenterY);
        return new(ScarletSignatureFight, 3, 500, phrase, (byte)note, (byte)phase, technique, (byte)note, 4, hit.Accent,
            rhythm.Start - 30, hit.Warning, hit.Fire, CrimsonEnsemble.NoteEnd(technique, hit),
            rhythm.Hits[0].Fire, CrimsonEnsemble.NoteEnd(technique, rhythm.Hits[3]),
            stage, stage, target, groundX, groundY, 1);
    }
    private static bool ScarletHits(ReadOnlySpan<CrimsonStroke> strokes, float x, float y, float width = ScarletPlayerWidth, float height = ScarletPlayerHeight)
    {
        foreach (var stroke in strokes)
            if (CrimsonTechniqueGeometry.Intersects(stroke, x, y, width, height)) return true;
        return false;
    }
    // h = rise of the feet above the support surface (0 = standing), x = left edge of the body.
    private static bool ScarletBodyHit(ReadOnlySpan<CrimsonStroke> strokes, RaidFieldGeometry f, float x, float h)
        => ScarletHits(strokes, x, f.Bottom - h - ScarletPlayerHeight);
    private static int ScarletColumnOf(RaidFieldGeometry f, float x) => Math.Clamp((int)MathF.Floor((x - f.Left) / 256), 0, 9);

    [DomainTest("Scarlet signature moves append stable IDs and replace the four basic notes of every third Act I-III phrase only")]
    private static void ScarletSignatureSelection()
    {
        AssertEqual(15, (int)CrimsonTechnique.ClusterVolley, "old ID unchanged");
        AssertEqual(16, (int)CrimsonTechnique.ChoirRakes, "old ID unchanged");
        AssertEqual(17, (int)CrimsonTechnique.CinderCurtain, "append-only ID");
        AssertEqual(18, (int)CrimsonTechnique.ShroudRope, "append-only ID");
        AssertEqual(19, (int)CrimsonTechnique.FourHands, "append-only ID");
        AssertEqual(20, Enum.GetValues<CrimsonTechnique>().Length, "no other technique was added");
        AssertEqual((ushort)79, EncounterProtocol.CurrentVersion, "matching peers are required for the new descriptor values");
        var signatures = new[] { CrimsonTechnique.CinderCurtain, CrimsonTechnique.ShroudRope, CrimsonTechnique.FourHands };
        for (int phase = 0; phase < 3; phase++)
        {
            AssertEqual(phase, CrimsonTechniqueGeometry.Owner(signatures[phase]), "each Act's own apparition owns its move");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                int signaturePhrases = 0;
                for (int slot = 1; slot <= 12; slot++)
                {
                    int serial = cycle * 12 + slot;
                    bool signature = slot % 3 == 0;
                    if (signature) signaturePhrases++;
                    AssertEqual(signature, CrimsonSignatureMoves.IsSignaturePhrase(phase, serial), "3rd, 6th, 9th and 12th phrase of a cycle");
                    for (int note = 0; note < CrimsonChoreography.BasicNotes; note++)
                    {
                        var expected = signature ? signatures[phase]
                            : phase == 2 ? CrimsonTechnique.ChoirRakes : phase == 1 ? CrimsonTechnique.SpatialRift : CrimsonTechnique.TrackingBeam;
                        AssertEqual(expected, CrimsonEnsemble.Technique(phase, serial, note, false), "basic note technique");
                        AssertEqual(expected, CrimsonChoreography.Technique(phase, serial, note), "choreography and ensemble agree");
                    }
                    AssertEqual(CrimsonTechnique.SideBeams, CrimsonEnsemble.Technique(phase, serial, 4, false), "the seal crossflow always closes the phrase");
                }
                AssertEqual(4, signaturePhrases, "four signature phrases per twelve-phrase cycle");
            }
        }
        for (int phrase = 1; phrase <= 36; phrase++) for (int note = 0; note < 5; note++) for (int second = 0; second < 2; second++)
        {
            var technique = CrimsonEnsemble.Technique(3, phrase, note, second == 1);
            AssertEqual(false, CrimsonSignatureMoves.IsSignatureMove(technique), "Final keeps its paired families and cluster volley");
        }
        AssertEqual(false, CrimsonSignatureMoves.IsSignaturePhrase(3, 3), "Final is not a signature Act");
        for (int source = 0; source < 4; source++) for (int serial = 0; serial < 60; serial++)
            AssertEqual(false, CrimsonSignatureMoves.IsSignatureMove(CrimsonTechniqueGeometry.Select(source, serial)), "retained decks are unchanged");
        AssertEqual(10, CrimsonSignatureMoves.MaximumStrokes, "signature stroke budget");
        AssertEqual(true, CrimsonSignatureMoves.MaximumStrokes <= CrimsonTechniqueGeometry.MaximumStrokes, "fits the shared stroke buffer");
    }

    [DomainTest("Scarlet cinder curtain leaves an exact 768 px corridor with every other point burning")]
    private static void ScarletCinderCurtainCorridor()
    {
        Span<CrimsonStroke> forecast = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var (groundX, groundY) in new[] { (8000, 6000), (3333, 2500) })
        {
            var f = RaidFieldGeometry.FromGround(groundX, groundY);
            AssertEqual(2560f, f.Right - f.Left, "ten 256 px columns");
            var scanned = new System.Collections.Generic.HashSet<int>();
            for (int observed = 0; observed <= 9; observed++) foreach (int phrase in new[] { 3, 6 }) for (int note = 0; note < 4; note++)
            {
                var plan = ScarletSignaturePlan(0, phrase, note, f.Left + 256 * observed + 128, groundX, groundY);
                plan.Validate();
                int corridor = CrimsonSignatureMoves.CurtainCorridor(f, plan.Target, plan.Phrase, note);
                AssertEqual(true, corridor is >= 0 and <= 7, "corridor stays inside the field");
                float gapLeft = f.Left + 256 * corridor, gapRight = gapLeft + 768;
                int nf = CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true);
                int nl = CrimsonTechniqueGeometry.Write(plan, plan.Fire + 10, live);
                AssertEqual(7, nf, "seven burning columns");
                AssertEqual(nf, nl, "live curtain has the same columns");
                for (int i = 0; i < nf; i++)
                {
                    AssertEqual(forecast[i], live[i], "once fully fallen the live strokes are the announced strokes");
                    AssertEqual(f.Top, forecast[i].A.Y, "falls from the field top");
                    AssertEqual(f.Bottom, forecast[i].B.Y, "to the support surface");
                    AssertEqual(forecast[i].A.X, forecast[i].B.X, "vertical column");
                    AssertEqual(true, forecast[i].Radius is >= 128 and <= 130, "column capsule width");
                }
                if (!scanned.Add(corridor)) continue;
                // Exactly 768 px free at three heights, edges on column bounds, nothing else free.
                foreach (float y in new[] { f.Top + 1, f.CenterY, f.Bottom - 1 })
                    for (float x = f.Left; x <= f.Right; x += .25f)
                    {
                        bool burning = ScarletHits(forecast[..nf], x, y, .001f, .001f);
                        if (x <= gapLeft - .25f || x >= gapRight + .25f) AssertEqual(true, burning, $"outside the corridor burns x={x - f.Left} corridor={corridor}");
                        else if (x >= gapLeft + .25f && x <= gapRight - .25f) AssertEqual(false, burning, $"corridor is free x={x - f.Left} corridor={corridor}");
                    }
                for (float x = gapLeft + .5f; x <= gapRight - ScarletPlayerWidth - .5f; x += 7)
                    foreach (float y in new[] { f.Top, f.CenterY, f.Bottom - ScarletPlayerHeight })
                        AssertEqual(false, ScarletHits(forecast[..nf], x, y), "a full player body fits anywhere in the corridor");
                if (corridor > 0) AssertEqual(true, ScarletHits(forecast[..nf], gapLeft - 19, f.Bottom - ScarletPlayerHeight), "one pixel into the left wall burns");
                if (corridor < 7) AssertEqual(true, ScarletHits(forecast[..nf], gapRight - 1, f.Bottom - ScarletPlayerHeight), "one pixel into the right wall burns");
            }
            AssertEqual(8, scanned.Count, "all eight corridor positions are reached and scanned");
        }
    }

    // Independent statement of the walk: the observed column leads the first corridor; the preferred
    // direction (right on even serial/3) when it fits, else the other, else away from the nearer wall.
    private static (int Start, int Direction) ScarletCurtainExpectedWalk(int column, int phrase)
    {
        int preferred = phrase / 3 % 2 == 0 ? 1 : -1;
        foreach (int direction in new[] { preferred, -preferred })
        {
            int start = direction > 0 ? column - 2 : column, end = start + 3 * direction;
            if (start >= 0 && start <= 7 && end >= 0 && end <= 7) return (start, direction);
        }
        int away = column <= 4 ? 1 : -1;
        return (Math.Clamp(away > 0 ? column - 2 : column, 0, 7), away);
    }

    [DomainTest("Scarlet cinder curtain walks away from the observed column, which stays safe for three notes, and shares two columns between beats")]
    private static void ScarletCinderCurtainWalk()
    {
        Span<CrimsonStroke> now = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> next = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var (groundX, groundY) in new[] { (8000, 6000), (4100, 2000) })
        {
            var f = RaidFieldGeometry.FromGround(groundX, groundY);
            for (int phrase = 3; phrase <= 60; phrase += 3)
                for (int target = 0; target <= 9; target++) foreach (float offset in new[] { 100f, 128f, 255f })
                {
                    float observed = Math.Clamp(f.Left + 256 * target + offset, f.Left + 100, f.Right - 100);
                    int column = ScarletColumnOf(f, observed);
                    var (start, direction) = ScarletCurtainExpectedWalk(column, phrase);
                    var plans = new CrimsonGesturePlan[4];
                    var corridors = new int[4];
                    for (int note = 0; note < 4; note++)
                    {
                        plans[note] = ScarletSignaturePlan(0, phrase, note, observed, groundX, groundY);
                        plans[note].Validate();
                        AssertEqual(CrimsonTechnique.CinderCurtain, plans[note].Technique, "Act I signature");
                        corridors[note] = CrimsonSignatureMoves.CurtainCorridor(f, plans[note].Target, plans[note].Phrase, note);
                        AssertEqual(start + note * direction, corridors[note], $"one column per beat in the stated direction phrase={phrase} column={column}");
                        AssertEqual(true, corridors[note] is >= 0 and <= 7, "corridor stays inside the field");
                    }
                    bool interior = column is >= 2 and <= 7;
                    if (interior) AssertEqual(column, direction > 0 ? corridors[0] + 2 : corridors[0], "the observed column is the leading end of the first corridor");
                    // The player's own column: free for 3 notes inside the field, 2 at columns 1 and 8, 1 at the walls.
                    int safeNotes = interior ? 3 : column is 1 or 8 ? 2 : 1;
                    float columnLeft = f.Left + 256 * column;
                    for (int note = 0; note < 4; note++)
                    {
                        int count = CrimsonTechniqueGeometry.Write(plans[note], plans[note].Fire, now, true);
                        for (float x = columnLeft + .5f; x <= columnLeft + 256 - ScarletPlayerWidth - .5f; x += 9)
                            AssertEqual(note >= safeNotes, ScarletHits(now[..count], x, f.Bottom - ScarletPlayerHeight),
                                $"observed column {column} is safe exactly for its first {safeNotes} notes (phrase={phrase} note={note} x={x - columnLeft})");
                    }
                    for (int note = 0; note < 3; note++)
                    {
                        var a = plans[note]; var b = plans[note + 1];
                        int shared = Math.Max(corridors[note], corridors[note + 1]);
                        AssertEqual(1, Math.Abs(corridors[note] - corridors[note + 1]), "consecutive corridors are one column apart and overlap by two");
                        AssertEqual(true, a.End <= b.Fire, "a curtain is gone before the next one falls");
                        int na = CrimsonTechniqueGeometry.Write(a, a.Fire, now, true), nb = CrimsonTechniqueGeometry.Write(b, b.Fire, next, true);
                        float sharedLeft = f.Left + 256 * shared;
                        for (float x = sharedLeft + .5f; x <= sharedLeft + 512 - ScarletPlayerWidth - .5f; x += 9)
                        {
                            AssertEqual(false, ScarletHits(now[..na], x, f.Bottom - ScarletPlayerHeight), "shared columns are free in this note");
                            AssertEqual(false, ScarletHits(next[..nb], x, f.Bottom - ScarletPlayerHeight), "shared columns are free in the next note");
                        }
                    }
                }
        }
    }

    [DomainTest("Scarlet cinder curtain can be followed at base run speed from the player's own column")]
    private static void ScarletCinderCurtainFollow()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        float worstInterior = 0, worstNear = 0, worstWall = 0;
        for (int column = 0; column <= 9; column++) foreach (int phrase in new[] { 3, 6 })
        {
            var plans = new CrimsonGesturePlan[4];
            for (int note = 0; note < 4; note++) plans[note] = ScarletSignaturePlan(0, phrase, note, f.Left + 256 * column + 128);
            var (_, direction) = ScarletCurtainExpectedWalk(column, phrase);
            bool wall = column is 0 or 1 or 8 or 9;
            // Inside the field the walk's direction shows with note 1's warning; at the walls it is forced from note 0.
            float begin = wall ? plans[0].Born : plans[1].Born;
            float needed = ScarletCurtainSlowestRunner(f, plans, column, direction, begin);
            if (!wall) worstInterior = Math.Max(worstInterior, needed);
            else if (column is 1 or 8) worstNear = Math.Max(worstNear, needed);
            else worstWall = Math.Max(worstWall, needed);
            if (!wall) AssertEqual(true, ScarletCurtainRuns(f, plans, f.Left + 256 * column + 118, direction, 3.0f, begin), $"a 3.0 px/tick runner follows column {column} phrase={phrase}");
        }
        AssertEqual(true, worstInterior <= 3.0f, $"interior columns need at most 3 px/tick when moving from note 1's warning ({worstInterior:F2})");
        // Measured, not a fairness target: the walls force the walk away from a player standing against them
        // (about 3.45 px/tick at columns 1/8 and 5.65 at columns 0/9 when moving from note 0's warning).
        AssertEqual(true, worstNear is > 3.0f and <= 3.5f, $"columns 1 and 8 need {worstNear:F2} px/tick from note 0's warning");
        AssertEqual(true, worstWall is > 5.0f and <= 5.7f, $"columns 0 and 9 need {worstWall:F2} px/tick from note 0's warning");
    }
    // Stands at the centre of the column until begin, then runs toward the walk at a constant speed.
    private static bool ScarletCurtainRuns(RaidFieldGeometry f, CrimsonGesturePlan[] plans, float x0, int direction, float speed, float begin)
    {
        Span<CrimsonStroke> now = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        for (float tick = plans[0].Born; tick < plans[3].End; tick += .25f)
        {
            float x = Math.Clamp(x0 + direction * speed * Math.Max(0, tick - begin), f.Left + 1, f.Right - ScarletPlayerWidth - 1);
            for (int note = 0; note < 4; note++)
            {
                int count = CrimsonTechniqueGeometry.Write(plans[note], tick, now);
                if (ScarletHits(now[..count], x, f.Bottom - ScarletPlayerHeight)) return false;
            }
        }
        return true;
    }
    private static float ScarletCurtainSlowestRunner(RaidFieldGeometry f, CrimsonGesturePlan[] plans, int column, int direction, float begin)
    {
        float x0 = f.Left + 256 * column + 128 - ScarletPlayerWidth * .5f;
        for (float speed = 0; speed <= 12; speed += .05f)
            if (ScarletCurtainRuns(f, plans, x0, direction, speed, begin)) return speed;
        return float.PositiveInfinity;
    }

    [DomainTest("Scarlet shroud rope low cut hits a standing body while an ordinary jump clears both cuts")]
    private static void ScarletShroudRopeHeights()
    {
        Span<CrimsonStroke> forecast = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var (groundX, groundY) in new[] { (8000, 6000), (2000, 1500) })
        {
            var f = RaidFieldGeometry.FromGround(groundX, groundY);
            for (int note = 0; note < 4; note++)
            {
                var plan = ScarletSignaturePlan(1, 9, note, 0, groundX, groundY);
                plan.Validate();
                AssertEqual(CrimsonTechnique.ShroudRope, plan.Technique, "Act II signature");
                AssertEqual(12, plan.End - plan.Fire, "rift-like live window");
                bool low = note % 2 == 0;
                AssertEqual(1, CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true), "one full-width cut");
                AssertEqual(0, CrimsonTechniqueGeometry.Write(plan, plan.Fire, live), "zero-width ignition is harmless");
                var cut = forecast[0];
                AssertEqual(low ? f.Bottom - 26 : f.Bottom - 170, cut.A.Y, "low / high / low / high");
                AssertEqual(cut.A.Y, cut.B.Y, "horizontal");
                AssertEqual(10f, cut.Radius, "rope radius");
                AssertEqual(note % 2 == 0 ? f.Left : f.Right, cut.A.X, "alternating travel direction starts at the entering side");
                AssertEqual(note % 2 == 0 ? f.Right : f.Left, cut.B.X, "and crosses the whole field");
                for (float age = plan.Fire + .25f; age < plan.End; age += .25f)
                {
                    int count = CrimsonTechniqueGeometry.Write(plan, age, live);
                    AssertEqual(1, count, "one live cut");
                    AssertEqual(cut.A, live[0].A, "enters from the announced side");
                    AssertEqual(cut.A.Y, live[0].B.Y, "stays on the announced height");
                    AssertEqual(cut.Radius, live[0].Radius, "same width as announced");
                    if (age >= plan.Fire + 2) AssertEqual(cut, live[0], "full width after two ticks");
                }
                AssertEqual(0, CrimsonTechniqueGeometry.Write(plan, plan.End, live), "exclusive end");
                foreach (float x in new[] { f.Left + 5, f.CenterX - 30, f.CenterX, f.Right - 25 })
                {
                    var rope = forecast[..1];
                    AssertEqual(low, ScarletBodyHit(rope, f, x, 0), "a standing player is hit by the low cut only");
                    AssertEqual(low, ScarletBodyHit(rope, f, x, 35.5f), "just under a 36 px rise the low cut still hits");
                    for (float h = 36.5f; h <= 117.5f; h += .5f) AssertEqual(false, ScarletBodyHit(rope, f, x, h), $"a 37-117 px rise clears both cuts h={h}");
                    AssertEqual(!low, ScarletBodyHit(rope, f, x, 118.5f), "from a 118 px rise the high cut hits");
                    for (float h = 119; h <= 180; h += 1) AssertEqual(!low, ScarletBodyHit(rope, f, x, h), "a boosted jump meets the high cut only");
                    AssertEqual(false, ScarletBodyHit(rope, f, x, 181), "above both cuts");
                    AssertEqual(false, ScarletBodyHit(rope, f, x, 106), "an ordinary full jump (feet up to about 106 px) clears both cuts");
                }
            }
            AssertEqual(118f, f.Bottom - ScarletPlayerHeight - (f.Bottom - CrimsonSignatureMoves.RopeHighHeight + CrimsonSignatureMoves.RopeRadius),
                "a grounded head clears the high cut by 118 px");
        }
    }

    [DomainTest("Scarlet four hands slam two of four 640 px quarters per beat and always leave a shared safe quarter")]
    private static void ScarletFourHandsQuarters()
    {
        Span<CrimsonStroke> forecast = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var (groundX, groundY) in new[] { (8000, 6000), (2500, 1800) })
        {
            var f = RaidFieldGeometry.FromGround(groundX, groundY);
            for (int phrase = 3; phrase <= 48; phrase += 3)
            {
                var struckCount = new int[4];
                var previousSafe = new bool[4];
                for (int note = 0; note < 4; note++)
                {
                    var plan = ScarletSignaturePlan(2, phrase, note, 0, groundX, groundY);
                    plan.Validate();
                    AssertEqual(CrimsonTechnique.FourHands, plan.Technique, "Act III signature");
                    var struck = CrimsonSignatureMoves.HandsStruck(phrase, note);
                    AssertEqual(true, struck.First != struck.Second && struck.First is >= 0 and < 4 && struck.Second is >= 0 and < 4, "two distinct quarters");
                    var isStruck = new bool[4];
                    isStruck[struck.First] = isStruck[struck.Second] = true;
                    var safe = new bool[4];
                    for (int q = 0; q < 4; q++) { safe[q] = !isStruck[q]; if (isStruck[q]) struckCount[q]++; }
                    if (note > 0)
                    {
                        bool shared = false;
                        for (int q = 0; q < 4; q++) shared |= safe[q] && previousSafe[q];
                        AssertEqual(true, shared, $"consecutive beats share a safe quarter phrase={phrase} note={note}");
                    }
                    previousSafe = safe;

                    int count = CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true);
                    AssertEqual(6, count, "three claws in each of two quarters");
                    AssertEqual(0, CrimsonTechniqueGeometry.Write(plan, plan.Fire, live), "zero-width ignition is harmless");
                    AssertEqual(16, plan.End - plan.Fire, "live window");
                    int liveCount = CrimsonTechniqueGeometry.Write(plan, plan.Fire + 8, live);
                    AssertEqual(count, liveCount, "live hands have the announced claws");
                    for (int i = 0; i < count; i++) AssertEqual(forecast[i], live[i], "fully fallen claws are the announced claws");
                    for (int q = 0; q < 4; q++)
                    {
                        var (left, right) = CrimsonSignatureMoves.HandsQuarter(f, q);
                        AssertEqual(640f, right - left, "quarter width");
                        if (isStruck[q])
                        {
                            for (float x = left + 8; x <= right - 8; x += 3)
                                for (float y = f.Top; y <= f.Bottom; y += 56)
                                    AssertEqual(true, ScarletHits(forecast[..count], x, y, .001f, .001f), $"slammed quarter {q} is solid x={x - left} y={y - f.Top}");
                        }
                        else
                        {
                            for (float x = left + 8; x <= right - 8 - ScarletPlayerWidth; x += 4)
                                foreach (float y in new[] { f.Top, f.CenterY, f.Bottom - ScarletPlayerHeight })
                                    AssertEqual(false, ScarletHits(forecast[..count], x, y), $"safe quarter {q} takes a full body x={x - left}");
                        }
                    }
                    // Claw capsules stay within 8 px of their quarter, so a quarter edge is a real fence.
                    for (int i = 0; i < count; i++)
                    {
                        float lo = Math.Min(forecast[i].A.X, forecast[i].B.X) - forecast[i].Radius, hi = Math.Max(forecast[i].A.X, forecast[i].B.X) + forecast[i].Radius;
                        int q = (int)MathF.Floor(((forecast[i].A.X + forecast[i].B.X) * .5f - f.Left) / 640);
                        var (left, right) = CrimsonSignatureMoves.HandsQuarter(f, q);
                        AssertEqual(true, lo >= left - 8 && hi <= right + 8, "claw union stays inside its quarter within 8 px");
                        AssertEqual(110f, forecast[i].Radius, "claw radius");
                    }
                }
                for (int q = 0; q < 4; q++) AssertEqual(2, struckCount[q], "every quarter is slammed twice and spared twice per phrase");
            }
            // Rotation by signature ordinal changes the pattern between phrases.
            var seen = new System.Collections.Generic.HashSet<(int, int)>();
            for (int phrase = 3; phrase <= 12; phrase += 3) seen.Add(CrimsonSignatureMoves.HandsStruck(phrase, 0));
            AssertEqual(4, seen.Count, "four signature phrases of a cycle open on four different pairs");
        }
    }

    [DomainTest("Scarlet signature phrases keep their live windows apart, validate every note and reject forged descriptors")]
    private static void ScarletSignatureComposition()
    {
        for (int phase = 0; phase < 3; phase++) for (int phrase = 3; phrase <= 36; phrase += 3)
        {
            int earliest = CrimsonChoreography.OpeningTicks + phrase * 53;
            var rhythm = CrimsonChoreography.Create(earliest, phrase, false);
            int live = CrimsonSignatureMoves.LiveTicks(CrimsonSignatureMoves.ForAct(phase));
            AssertEqual(new[] { 20, 12, 16 }[phase], live, "live ticks per Act");
            AssertEqual(new[] { 24, 20, 24 }[phase], CrimsonSignatureMoves.ResidueTicks(CrimsonSignatureMoves.ForAct(phase)), "residue ticks per Act");
            for (int note = 0; note < 4; note++)
            {
                var plan = ScarletSignaturePlan(phase, phrase, note, 8000, 8000, 6000, earliest);
                var hit = rhythm.Hits[note];
                AssertEqual(hit.Fire + live, CrimsonEnsemble.NoteEnd(plan.Technique, hit), "signature notes end after their own live window");
                AssertEqual(true, CrimsonEnsemble.NoteEnd(plan.Technique, hit) <= rhythm.Hits[note + 1].Fire, "one beat is long enough for the previous move to clear");
                AssertEqual(true, hit.Fire - hit.Warning >= CrimsonRhythm.MinimumWarningTicks, "one measured beat of warning");
                plan.Validate();
            }
        }
        foreach (var technique in new[] { CrimsonTechnique.CinderCurtain, CrimsonTechnique.ShroudRope, CrimsonTechnique.FourHands })
        {
            int phase = CrimsonTechniqueGeometry.Owner(technique);
            var p = ScarletSignaturePlan(phase, 9, 2, 8000);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) p.Write(writer);
            byte[] bytes = stream.ToArray();
            using (var reader = new BinaryReader(new MemoryStream(bytes))) AssertEqual(p, CrimsonGesturePlan.Read(reader), "descriptor round trip");
            AssertEqual(false, p.Aimed, "no player target identity");
            AssertEqual(true, p.IsSignature && !p.IsRift, "signature kind");
            AssertEqual(true, CrimsonSignatureMoves.IsSignatureMove(technique), "technique is a signature move");
            void Reject(CrimsonGesturePlan bad, string why)
            {
                bool rejected = false;
                try { bad.Validate(); } catch (InvalidDataException) { rejected = true; }
                AssertEqual(true, rejected, why);
            }
            Reject(p with { Source = (byte)((p.Source + 1) % 4) }, "another apparition cannot throw this move");
            Reject(p with { Pulse = 4, Step = 3 }, "signature moves are basic notes only");
            Reject(p with { Pulse = 9, Step = 3 }, "signature moves are basic notes only");
            Reject(p with { TargetSlot = 0, TargetConnection = Guid.NewGuid() }, "no retargeting identity");
            Reject(p with { End = p.Fire + CrimsonSignatureMoves.LiveTicks(technique) + 1, LastEnd = p.LastEnd + 2 }, "live window cannot be stretched");
            Reject(p with { Born = p.Fire - CrimsonRhythm.MinimumWarningTicks + 1 }, "warning stays at least the minimum");
            Reject(p with { Technique = (CrimsonTechnique)20 }, "unknown technique");
            Reject(p with { Target = new(float.NaN, 0) }, "non-finite observation");
            Reject(p with { Target = new(p.Field.Left + 10, p.Field.CenterY) }, "observation outside the field margin");
            p.Validate();
        }
    }
}
