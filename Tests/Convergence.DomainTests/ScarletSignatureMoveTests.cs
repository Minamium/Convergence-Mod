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

    private static CrimsonGesturePlan ScarletSignaturePlan(int phase, int phrase, int note, float observedX, int groundX = 8000, int groundY = 6000, int earliest = -1, int curtainMask = 0)
    {
        var f = RaidFieldGeometry.FromGround(groundX, groundY);
        var rhythm = CrimsonChoreography.Create(earliest < 0 ? CrimsonChoreography.OpeningTicks : earliest, phrase, false);
        var hit = rhythm.Hits[note];
        var technique = CrimsonEnsemble.Technique(phase, phrase, note, false);
        var stage = new CrimsonPoint(f.CenterX, f.Top + 210);
        // A curtain carries the occupied-column mask (solo: the observed column) instead of a position.
        var target = phase == 0 ? CrimsonSignatureMoves.CurtainTarget(curtainMask > 0 ? curtainMask : 1 << ScarletColumnOf(f, observedX))
            : new CrimsonPoint(f.CenterX, f.CenterY);
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
                int corridor = CrimsonSignatureMoves.CurtainCorridor(observed, plan.Phrase, note);
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
                        corridors[note] = CrimsonSignatureMoves.CurtainCorridor(column, plans[note].Phrase, note);
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
                    AssertEqual(6, count, "three fingers in each of two quarters");
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
                            // Three fingers 213 px apart: solid across their width, open between them.
                            for (int finger = -1; finger <= 1; finger++)
                                foreach (float y in new[] { f.Top, f.CenterY, f.Bottom - 1 })
                                {
                                    float axis = left + 320 + finger * 213;
                                    AssertEqual(true, ScarletHits(forecast[..count], axis - 74, y, .001f, .001f), $"finger {finger} of quarter {q} is solid left");
                                    AssertEqual(true, ScarletHits(forecast[..count], axis + 74, y, .001f, .001f), $"finger {finger} of quarter {q} is solid right");
                                    AssertEqual(false, ScarletHits(forecast[..count], axis + 106.5f, y, .001f, .001f), $"gap after finger {finger} of quarter {q} is open");
                                }
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
                        AssertEqual(75f, forecast[i].Radius, "finger radius");
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

    [DomainTest("Scarlet four hands fingers leave body-sized gaps and a clear spot within a beat's run")]
    private static void ScarletFourHandsFingers()
    {
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        float worst = 0, narrowest = float.MaxValue, wallStrip = float.MaxValue;
        int positions = (int)(f.Right - f.Left - ScarletPlayerWidth) + 1;
        for (int phrase = 3; phrase <= 48; phrase += 3) for (int note = 0; note < 4; note++)
        {
            var plan = ScarletSignaturePlan(2, phrase, note, 0);
            int count = CrimsonTechniqueGeometry.Write(plan, plan.Fire, strokes, true);
            var fingers = strokes[..count].ToArray();
            // Nearest standing spot (left edge, 1 px grid) that is clear of every finger, from every x along the floor.
            var clear = new bool[positions];
            for (int i = 0; i < positions; i++) clear[i] = !ScarletHits(fingers, f.Left + i, f.Bottom - ScarletPlayerHeight);
            for (int i = 0; i < positions; i++)
            {
                int distance = 0;
                while ((i - distance < 0 || !clear[i - distance]) && (i + distance >= positions || !clear[i + distance]))
                    if (++distance > positions) throw new InvalidOperationException("no clear spot at all");
                worst = Math.Max(worst, distance);
            }
            // Every gap between neighbouring fingers admits the body with at least 10 px on each side.
            var axes = new System.Collections.Generic.List<float>();
            foreach (var finger in fingers) axes.Add(finger.A.X);
            axes.Sort();
            for (int i = 0; i + 1 < axes.Count; i++)
            {
                float gapLeft = axes[i] + 75, gapRight = axes[i + 1] - 75;
                if (gapRight - gapLeft > 100) continue; // a whole spared quarter, not a finger gap
                narrowest = Math.Min(narrowest, gapRight - gapLeft);
                AssertEqual(true, gapRight - gapLeft - ScarletPlayerWidth >= 20, $"a body fits a finger gap with 10 px each side ({gapRight - gapLeft})");
                foreach (float y in new[] { f.Top, f.CenterY, f.Bottom - ScarletPlayerHeight })
                    for (float x = gapLeft + 10; x <= gapRight - 10 - ScarletPlayerWidth; x += 1)
                        AssertEqual(false, ScarletHits(fingers, x, y), $"finger gap admits a body x={x - f.Left} y={y - f.Top}");
            }
            // A slammed quarter against a field wall leaves a strip the body fits in.
            foreach (float strip in new[] { axes[0] - 75 - f.Left, f.Right - (axes[^1] + 75) })
                if (strip < 100) wallStrip = Math.Min(wallStrip, strip);
        }
        AssertEqual(true, narrowest >= 63f - .01f, $"finger gaps are at least 63 px ({narrowest})");
        AssertEqual(true, wallStrip >= 32f - .01f && wallStrip - ScarletPlayerWidth >= 10, $"the strip against a wall takes a body ({wallStrip})");
        // On the 1 px grid the farthest is 86 px (from the middle of a finger); the touching limit is 85 px,
        // 3.0 px per tick over the 28 ticks of one beat.
        AssertEqual(true, worst >= 80 && worst <= 86, $"the worst standing spot is within 86 px of a clear one ({worst})");
    }

    [DomainTest("Scarlet cinder curtain gives every observed member a corridor and burns only the rest")]
    private static void ScarletCinderCurtainCrowd()
    {
        Span<CrimsonStroke> forecast = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        // Pure walk over every mask: a member's whole corridor is safe, so their column is safe for the promised notes.
        for (int mask = 1; mask <= CrimsonSignatureMoves.MaximumCurtainMask; mask++) foreach (int phrase in new[] { 3, 6, 9, 12 }) for (int note = 0; note < 4; note++)
        {
            int safe = CrimsonSignatureMoves.CurtainSafe(mask, phrase, note);
            for (int column = 0; column < 10; column++)
            {
                if ((mask >> column & 1) == 0) continue;
                int promised = column is >= 2 and <= 7 ? 3 : column is 1 or 8 ? 2 : 1;
                if (note < promised) AssertEqual(true, (safe >> column & 1) != 0, $"member in column {column} is safe on note {note} mask={mask} phrase={phrase}");
                int corridor = CrimsonSignatureMoves.CurtainCorridor(column, phrase, note);
                for (int k = 0; k < 3; k++) AssertEqual(true, (safe >> (corridor + k) & 1) != 0, "a member's whole corridor is safe");
            }
        }
        // Geometry for every single column, the full mask and pseudo-random crowds.
        var masks = new System.Collections.Generic.List<int>();
        for (int c = 0; c < 10; c++) masks.Add(1 << c);
        masks.Add(CrimsonSignatureMoves.MaximumCurtainMask);
        var random = new Random(0x5CA1E7);
        while (masks.Count < 160) masks.Add(random.Next(1, 1024));
        foreach (int mask in masks) foreach (int phrase in new[] { 3, 6 }) for (int note = 0; note < 4; note++)
        {
            var plan = ScarletSignaturePlan(0, phrase, note, 0, curtainMask: mask);
            plan.Validate();
            AssertEqual(mask, CrimsonSignatureMoves.CurtainMask(plan), "the mask travels in Target");
            int safe = CrimsonSignatureMoves.CurtainSafe(mask, phrase, note);
            int count = CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true);
            AssertEqual(10 - System.Numerics.BitOperations.PopCount((uint)safe), count, "every column outside every corridor burns");
            AssertEqual(count, CrimsonTechniqueGeometry.Write(plan, plan.Fire + 10, live), "live curtain has the announced columns");
            for (int i = 0; i < count; i++) AssertEqual(forecast[i], live[i], "fallen strokes are the announced strokes");
            for (int column = 0; column < 10; column++)
            {
                bool isSafe = (safe >> column & 1) != 0, nextSafe = column < 9 && (safe >> (column + 1) & 1) != 0;
                float columnLeft = f.Left + 256 * column;
                AssertEqual(!isSafe, ScarletHits(forecast[..count], columnLeft + 127.5f, f.Bottom - 1, 1, 1), $"column {column} burns exactly when no corridor covers it");
                if (isSafe)
                    for (float x = columnLeft + .5f; x <= columnLeft + 256 - ScarletPlayerWidth - .5f; x += 9)
                        AssertEqual(false, ScarletHits(forecast[..count], x, f.Bottom - ScarletPlayerHeight), "a body fits a safe column");
                if (isSafe && nextSafe)
                    AssertEqual(false, ScarletHits(forecast[..count], columnLeft + 246, f.Bottom - ScarletPlayerHeight), "a body straddling two safe columns is safe");
            }
            for (int i = 0; i < count; i++)
            {
                int column = ScarletColumnOf(f, forecast[i].A.X);
                bool touchesSafe = column > 0 && (safe >> (column - 1) & 1) != 0 || column < 9 && (safe >> (column + 1) & 1) != 0;
                AssertEqual(touchesSafe ? 128f : 130f, forecast[i].Radius, "half-column wall beside a safe column, overlap between burning neighbours");
            }
        }
        // A solo mask is exactly the single-corridor curtain.
        foreach (int phrase in new[] { 3, 6, 9, 12 }) for (int column = 0; column < 10; column++) for (int note = 0; note < 4; note++)
        {
            var plan = ScarletSignaturePlan(0, phrase, note, f.Left + 256 * column + 128);
            var (start, direction) = ScarletCurtainExpectedWalk(column, phrase);
            int corridor = Math.Clamp(start + note * direction, 0, 7);
            int count = CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true), expected = 0;
            for (int burning = 0; burning < 10; burning++)
            {
                if (burning >= corridor && burning < corridor + 3) continue;
                bool touches = burning == corridor - 1 || burning == corridor + 3;
                float x = f.Left + 256 * (burning + .5f);
                AssertEqual(new CrimsonStroke(new(x, f.Top), new(x, f.Bottom), touches ? 128f : 130f), forecast[expected++], "solo geometry is the single-corridor curtain");
            }
            AssertEqual(expected, count, "solo burns exactly the columns outside the corridor");
        }
        // The full crowd is accepted even where nothing is left to burn.
        var full = ScarletSignaturePlan(0, 3, 0, 0, curtainMask: CrimsonSignatureMoves.MaximumCurtainMask);
        full.Validate();
        AssertEqual(true, CrimsonTechniqueGeometry.Write(full, full.Fire, forecast, true) <= 7, "bounded strokes");
    }

    [DomainTest("Scarlet cinder curtain follow speeds are unchanged by the other members in the field")]
    private static void ScarletCinderCurtainCrowdFollow()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        var random = new Random(0xC40D);
        for (int column = 0; column < 10; column++) foreach (int phrase in new[] { 3, 6 })
        {
            var (_, direction) = ScarletCurtainExpectedWalk(column, phrase);
            bool wall = column is 0 or 1 or 8 or 9;
            var solo = new CrimsonGesturePlan[4];
            for (int note = 0; note < 4; note++) solo[note] = ScarletSignaturePlan(0, phrase, note, 0, curtainMask: 1 << column);
            float begin = wall ? solo[0].Born : solo[1].Born;
            float soloSpeed = ScarletCurtainSlowestRunner(f, solo, column, direction, begin);
            for (int trial = 0; trial < 4; trial++)
            {
                int mask = trial == 0 ? CrimsonSignatureMoves.MaximumCurtainMask : random.Next(1, 1024) | 1 << column;
                var crowd = new CrimsonGesturePlan[4];
                for (int note = 0; note < 4; note++) crowd[note] = ScarletSignaturePlan(0, phrase, note, 0, curtainMask: mask);
                AssertEqual(true, ScarletCurtainRuns(f, crowd, f.Left + 256 * column + 118, direction, soloSpeed, begin), $"the solo speed still follows with mask={mask}");
                float crowdSpeed = ScarletCurtainSlowestRunner(f, crowd, column, direction, begin);
                AssertEqual(true, crowdSpeed <= soloSpeed + .0001f, $"other members never slow a member down column={column} mask={mask} ({crowdSpeed} vs {soloSpeed})");
            }
        }
    }

    [DomainTest("Scarlet cinder curtain column mask round trips and every malformed mask is rejected")]
    private static void ScarletCinderCurtainMaskCodec()
    {
        foreach (int mask in new[] { 1, 2, 513, 1023 })
        {
            var p = ScarletSignaturePlan(0, 9, 1, 0, curtainMask: mask);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) p.Write(writer);
            byte[] bytes = stream.ToArray();
            using (var reader = new BinaryReader(new MemoryStream(bytes))) AssertEqual(p, CrimsonGesturePlan.Read(reader), "descriptor round trip");
            AssertEqual(mask, CrimsonSignatureMoves.CurtainMask(p), "mask survives");
            for (int length = 0; length < bytes.Length; length++)
            {
                bool rejected = false;
                try { using var reader = new BinaryReader(new MemoryStream(bytes, 0, length)); CrimsonGesturePlan.Read(reader); }
                catch (IOException) { rejected = true; }
                AssertEqual(true, rejected, "all truncated prefixes rejected");
            }
        }
        var good = ScarletSignaturePlan(0, 9, 1, 0, curtainMask: 33);
        foreach (var bad in new CrimsonPoint[] { new(0, 0), new(1024, 0), new(5.5f, 0), new(33.0001f, 0), new(float.NaN, 0), new(float.PositiveInfinity, 0),
            new(-3, 0), new(3, 1), new(3, -.5f), new(8000, 5500) })
        {
            bool rejected = false;
            try { (good with { Target = bad }).Validate(); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, $"malformed mask ({bad.X}, {bad.Y}) rejected");
        }
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonSignatureMoves.CurtainTarget(0), "empty crowd");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonSignatureMoves.CurtainTarget(1024), "mask above ten bits");
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
