using System;
using System.IO;
using System.Linq;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

// Act signature moves (protocol 79, retimed in 80): selection cadence, exact corridor / rope heights / quarter
// geometry against a 20x42 player, survival under the mobility model at the dotted-quarter step timing, and the
// descriptor codec.
internal static partial class Program
{
    private const float ScarletPlayerWidth = 20, ScarletPlayerHeight = 42;
    private static readonly Guid ScarletSignatureFight = Guid.Parse("5b1d3a52-0f0e-4c42-9a55-1d4c6a0b7e11");

    private static CrimsonGesturePlan ScarletSignaturePlan(int phase, int phrase, int note, float observedX, int groundX = 8000, int groundY = 6000, int earliest = -1, int curtainMask = 0)
    {
        var f = RaidFieldGeometry.FromGround(groundX, groundY);
        var rhythm = CrimsonChoreography.Create(earliest < 0 ? CrimsonChoreography.OpeningTicks : earliest, phrase, phase, false);
        var hit = rhythm.Hits.First(h => h.Pulse == note);
        var technique = CrimsonEnsemble.Technique(phase, phrase, note, false);
        var stage = new CrimsonPoint(f.CenterX, f.Top + 210);
        // A curtain carries the occupied-column mask (solo: the observed column) instead of a position.
        var target = phase == 0 ? CrimsonSignatureMoves.CurtainTarget(curtainMask > 0 ? curtainMask : 1 << ScarletColumnOf(f, observedX))
            : new CrimsonPoint(f.CenterX, f.CenterY);
        return new(ScarletSignatureFight, 3, 500, phrase, (byte)note, (byte)phase, technique, (byte)note, 4, hit.Accent,
            rhythm.FirstWarning - CrimsonRhythm.LookAheadTicks, hit.Warning, hit.Fire, CrimsonEnsemble.NoteEnd(technique, hit),
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

    [DomainTest("Scarlet signature moves append stable IDs and replace the notes and closing crossflow of every third Act I-III phrase only")]
    private static void ScarletSignatureSelection()
    {
        AssertEqual(15, (int)CrimsonTechnique.ClusterVolley, "old ID unchanged");
        AssertEqual(16, (int)CrimsonTechnique.ChoirRakes, "old ID unchanged");
        AssertEqual(17, (int)CrimsonTechnique.CinderCurtain, "append-only ID");
        AssertEqual(18, (int)CrimsonTechnique.ShroudRope, "append-only ID");
        AssertEqual(19, (int)CrimsonTechnique.FourHands, "append-only ID");
        AssertEqual(20, Enum.GetValues<CrimsonTechnique>().Length, "no other technique was added");
        AssertEqual((ushort)80, EncounterProtocol.CurrentVersion, "matching peers are required for the retimed phrases and the shorter crossflow stream");
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
                    var rhythm = CrimsonChoreography.Create(900, serial, phase, false);
                    AssertEqual(signature ? 4 : 3, rhythm.Hits.Count(h => !CrimsonChoreography.IsCrossflow(h.Pulse)), "four steps or three notes");
                    AssertEqual(!signature, rhythm.Hits.Any(h => h.Pulse == CrimsonChoreography.Closer), "the crossflow closes an ordinary phrase; the move's final step takes its place");
                    AssertEqual(rhythm.End, rhythm.Hits[^1].Fire, "either way the closing hit lands on the next downbeat");
                    if (signature) AssertEqual((byte)2, rhythm.Hits[^1].Accent, "the final step is accented like a crossflow");
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

    // ---------------------------------------------------------------- mobility profiles
    // BASE: Terraria's run, 0.08 px/tick^2 up to 3 px/tick. ENDGAME: typical hardmode wings, 0.2 up to 6.
    // Flight is unlimited in the field (wings never run out): gravity 0.4 (fall at most 10), and holding
    // jump lifts with Lerp(vy, -12, .18). Players react only to forecasts that are already visible.
    private readonly record struct ScarletMobility(float Acceleration, float MaxSpeed);
    private static readonly ScarletMobility ScarletBaseRun = new(.08f, 3f), ScarletEndgameRun = new(.2f, 6f);

    // Direction -1, 0 or +1 is the player's input this tick; 0 brakes at the same rate.
    private static void ScarletRunStep(ScarletMobility mobility, ref float x, ref float speed, int input)
    {
        if (input != 0) speed = Math.Clamp(speed + input * mobility.Acceleration, -mobility.MaxSpeed, mobility.MaxSpeed);
        else speed = Math.Abs(speed) <= mobility.Acceleration ? 0 : speed - Math.Sign(speed) * mobility.Acceleration;
        x += speed;
    }
    // Horizontal clearance between a standing body at x and the nearest vertical stroke edge (negative = overlap).
    private static float ScarletClearance(ReadOnlySpan<CrimsonStroke> strokes, float x)
    {
        float clearance = float.MaxValue;
        foreach (var stroke in strokes)
            clearance = Math.Min(clearance, Math.Max(stroke.A.X - stroke.Radius - (x + ScarletPlayerWidth), x - (stroke.A.X + stroke.Radius)));
        return clearance;
    }

    // The statement of the curtain walk, written without the production helpers: first corridor and
    // direction for a member in `column` (heading away from the nearer wall: columns 0-4 right, 5-9 left,
    // independent of the phrase), then the corridor of a note (four columns wide, 0..6).
    private static (int Start, int Direction) ScarletOracleWalk(int column, int phrase)
    {
        int away = column <= 4 ? 1 : -1;
        int start = away > 0 ? column - 2 : column - 1, end = start + 3 * away;
        if (start >= 0 && start <= 6 && end >= 0 && end <= 6) return (start, away);
        // Next to a wall: turn toward it; the per-note clamp slides the corridor in and it stays.
        return (-away > 0 ? column - 2 : column - 1, -away);
    }
    private static int ScarletOracleCorridor(int column, int phrase, int note)
    {
        var (start, direction) = ScarletOracleWalk(column, phrase);
        return Math.Clamp(start + note * direction, 0, 6);
    }
    private static int ScarletOracleSafe(int mask, int phrase, int note)
    {
        int safe = 0;
        for (int column = 0; column < 10; column++)
            if ((mask & 1 << column) != 0) safe |= 15 << ScarletOracleCorridor(column, phrase, note);
        return safe;
    }

    [DomainTest("Scarlet cinder curtain leaves an exact 1024 px corridor with every other point burning")]
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
                int corridor = ScarletOracleCorridor(observed, phrase, note);
                AssertEqual(corridor, CrimsonSignatureMoves.CurtainCorridor(observed, plan.Phrase, note), "production corridor equals the stated rule");
                float gapLeft = f.Left + 256 * corridor, gapRight = gapLeft + 1024;
                int nf = CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true);
                int nl = CrimsonTechniqueGeometry.Write(plan, plan.Fire + 10, live);
                AssertEqual(6, nf, "six burning columns");
                AssertEqual(true, nf <= CrimsonSignatureMoves.MaximumStrokes, "stroke budget");
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
                // Exactly 1024 px free at three heights, edges on column bounds, nothing else free.
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
                if (corridor < 6) AssertEqual(true, ScarletHits(forecast[..nf], gapRight - 1, f.Bottom - ScarletPlayerHeight), "one pixel into the right wall burns");
            }
            AssertEqual(7, scanned.Count, "all seven corridor positions are reached and scanned");
        }
    }

    [DomainTest("Scarlet cinder curtain walks away from the member's column, keeps it safe, and slides into a wall and stays")]
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
                    var (start, direction) = ScarletOracleWalk(column, phrase);
                    bool interior = column is >= 2 and <= 7;
                    var plans = new CrimsonGesturePlan[4];
                    var corridors = new int[4];
                    for (int note = 0; note < 4; note++)
                    {
                        plans[note] = ScarletSignaturePlan(0, phrase, note, observed, groundX, groundY);
                        plans[note].Validate();
                        AssertEqual(CrimsonTechnique.CinderCurtain, plans[note].Technique, "Act I signature");
                        corridors[note] = CrimsonSignatureMoves.CurtainCorridor(column, plans[note].Phrase, note);
                        AssertEqual(ScarletOracleCorridor(column, phrase, note), corridors[note], $"stated rule phrase={phrase} column={column}");
                        AssertEqual(ScarletOracleCorridor(column, 3, note), corridors[note], "the walk does not depend on the signature ordinal");
                        AssertEqual(true, corridors[note] is >= 0 and <= 6, "corridor stays inside the field");
                    }
                    if (interior)
                    {
                        AssertEqual(column <= 4 ? 1 : -1, CrimsonSignatureMoves.CurtainWalk(column, phrase).Direction, "the walk heads away from the nearer wall");
                        for (int note = 0; note < 4; note++) AssertEqual(start + note * direction, corridors[note], "one column per beat in one direction");
                        // One column of forward margin: the member stands one column short of the corridor's leading end.
                        AssertEqual(column + (direction > 0 ? 1 : -1), direction > 0 ? corridors[0] + 3 : corridors[0], "first corridor leads one column past the member");
                    }
                    else for (int note = 0; note < 4; note++)
                        AssertEqual(column <= 4 ? 0 : 6, corridors[note], "wall members get a corridor that slides into the wall and stays");
                    // The member's own column: safe for 3 notes inside the field, all 4 against a wall.
                    int safeNotes = interior ? 3 : 4;
                    float columnLeft = f.Left + 256 * column;
                    for (int note = 0; note < 4; note++)
                    {
                        int count = CrimsonTechniqueGeometry.Write(plans[note], plans[note].Fire, now, true);
                        AssertEqual(true, count <= 6, "at most six columns burn");
                        for (float x = columnLeft + .5f; x <= columnLeft + 256 - ScarletPlayerWidth - .5f; x += 9)
                            AssertEqual(note >= safeNotes, ScarletHits(now[..count], x, f.Bottom - ScarletPlayerHeight),
                                $"member column {column} is safe exactly for its first {safeNotes} notes (phrase={phrase} note={note} x={x - columnLeft})");
                    }
                    for (int note = 0; note < 3; note++)
                    {
                        var a = plans[note]; var b = plans[note + 1];
                        int sharedLeft = Math.Max(corridors[note], corridors[note + 1]), sharedRight = Math.Min(corridors[note], corridors[note + 1]) + 4;
                        AssertEqual(true, Math.Abs(corridors[note] - corridors[note + 1]) <= 1, "consecutive corridors are at most one column apart");
                        AssertEqual(true, sharedRight - sharedLeft >= 3, "consecutive corridors overlap by at least three columns");
                        AssertEqual(true, a.End <= b.Fire, "a curtain is gone before the next one falls");
                        int na = CrimsonTechniqueGeometry.Write(a, a.Fire, now, true), nb = CrimsonTechniqueGeometry.Write(b, b.Fire, next, true);
                        float left = f.Left + 256 * sharedLeft;
                        for (float x = left + .5f; x <= f.Left + 256 * sharedRight - ScarletPlayerWidth - .5f; x += 9)
                        {
                            AssertEqual(false, ScarletHits(now[..na], x, f.Bottom - ScarletPlayerHeight), "shared columns are free in this note");
                            AssertEqual(false, ScarletHits(next[..nb], x, f.Bottom - ScarletPlayerHeight), "shared columns are free in the next note");
                        }
                    }
                }
        }
    }

    // Runs a member of the curtain: at rest at x0 until `begin`, then running in `direction` under `mobility`.
    // Returns whether any live stroke ever touches the body, and the smallest horizontal clearance seen.
    private static bool ScarletCurtainRun(RaidFieldGeometry f, CrimsonGesturePlan[] plans, float x0, int direction, ScarletMobility mobility, int begin, out float clearance)
    {
        Span<CrimsonStroke> now = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        float x = x0, speed = 0;
        clearance = float.MaxValue;
        bool survived = true;
        for (int tick = plans[0].Born; tick < plans[3].End; tick++)
        {
            ScarletRunStep(mobility, ref x, ref speed, tick >= begin ? direction : 0);
            float clamped = Math.Clamp(x, f.Left, f.Right - ScarletPlayerWidth);
            if (clamped != x) { x = clamped; speed = 0; }
            for (int note = 0; note < 4; note++)
            {
                int count = CrimsonTechniqueGeometry.Write(plans[note], tick, now);
                if (count == 0) continue;
                if (ScarletHits(now[..count], x, f.Bottom - ScarletPlayerHeight)) survived = false;
                if (tick >= plans[note].Fire + 3) clearance = Math.Min(clearance, ScarletClearance(now[..count], x)); // once fully fallen
            }
        }
        return survived;
    }

    [DomainTest("Scarlet cinder curtain is survivable under base mobility from anywhere in the member's column, moving toward the open side")]
    private static void ScarletCinderCurtainBaseMobility()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        Span<CrimsonStroke> now = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        float tightest = float.MaxValue, tightestAt = 0, worstCentre = float.MaxValue;
        int backward = int.MaxValue, forward = int.MaxValue;
        for (int column = 0; column < 10; column++)
        {
            int open = column <= 4 ? 1 : -1; // away from the nearer wall: the walk's direction inside the field
            bool interior = column is >= 2 and <= 7;
            foreach (int phrase in new[] { 3, 6 })
            {
                var plans = new CrimsonGesturePlan[4];
                for (int note = 0; note < 4; note++) plans[note] = ScarletSignaturePlan(0, phrase, note, f.Left + 256 * column + 128);
                float columnLeft = f.Left + 256 * column, centre = columnLeft + 128 - ScarletPlayerWidth * .5f;
                // From ANY x inside the observed column, moving toward the open side from note 0's warning.
                for (float x = columnLeft; x <= columnLeft + 256 - ScarletPlayerWidth; x += phrase == 3 ? 1 : 7)
                {
                    AssertEqual(true, ScarletCurtainRun(f, plans, x, open, ScarletBaseRun, plans[0].Born, out float clearance), $"column {column} from x={x - columnLeft} phrase={phrase}");
                    if (clearance < tightest) { tightest = clearance; tightestAt = x - columnLeft + column * 1000; }
                }
                // From the column centre, starting only when note 1's forecast shows.
                AssertEqual(true, ScarletCurtainRun(f, plans, centre, open, ScarletBaseRun, plans[1].Born, out float late), $"column {column} from the centre at note 1 phrase={phrase}");
                worstCentre = Math.Min(worstCentre, late);
                if (!interior) AssertEqual(true, ScarletCurtainRun(f, plans, centre, open, ScarletBaseRun, int.MaxValue, out _), $"a wall member standing still survives column {column}");
                if (interior && phrase == 3)
                {
                    // How far from the column centre the look-ahead may have carried the member (against / along the walk) and still survive
                    // the whole phrase when they start moving at note 0's warning.
                    int back = 0, ahead = 0;
                    while (back < 400 && ScarletCurtainRun(f, plans, centre - open * (back + 1), open, ScarletBaseRun, plans[0].Born, out _)) back++;
                    while (ahead < 400 && ScarletCurtainRun(f, plans, centre + open * (ahead + 1), open, ScarletBaseRun, plans[0].Born, out _)) ahead++;
                    backward = Math.Min(backward, back); forward = Math.Min(forward, ahead);
                }
            }
        }
        // Reported numbers (px): the smallest clearance from any start in the column, and the tolerated look-ahead drift.
        AssertEqual(true, tightest >= 41, $"every start in the column keeps at least 41 px of clearance (smallest {tightest:F1} px at {tightestAt})");
        AssertEqual(true, worstCentre >= 150, $"the centre start at note 1 keeps at least 150 px (74 px at protocol 79) ({worstCentre:F1} px)");
        AssertEqual(true, backward >= 280 && forward >= 280, $"look-ahead drift tolerated when moving from note 0's warning: {backward} px against the walk, {forward} px along it");
    }

    // ---------------------------------------------------------------- rope staff
    // Vertical flight under BASE: gravity 0.4 (fall at most 10), Lerp(vy, -12, .18) while jump is held.
    private static (float Height, float Rise) ScarletFlightStep(float height, float rise, bool jump)
    {
        rise = Math.Max(rise - .4f, -10f);
        if (jump) rise += (12f - rise) * .18f;
        height += rise;
        if (height <= 0) return (0, 0);
        if (height >= 1078) return (1078, 0); // the ceiling: the body is 42 tall in a 1120 px field
        return (height, rise);
    }
    private const int ScarletStaffCells = 2157; // feet heights 0..1078 in half pixels
    private static bool[] ScarletStaffSafe(ReadOnlySpan<CrimsonStroke> strokes, RaidFieldGeometry f)
    {
        var safe = new bool[ScarletStaffCells];
        for (int i = 0; i < safe.Length; i++) safe[i] = !ScarletHits(strokes, f.CenterX, f.Bottom - i * .5f - ScarletPlayerHeight);
        return safe;
    }
    private static bool ScarletStaffSafeAt(bool[] safe, float height)
        => safe[Math.Clamp((int)MathF.Floor(height * 2), 0, safe.Length - 1)] && safe[Math.Clamp((int)MathF.Ceiling(height * 2), 0, safe.Length - 1)];
    // Can a body that is at `start` (at rest) when a note's lines strike stay safe through its 12 live ticks
    // and be safe from the next note's strike (28 ticks later) through its live ticks? Exhaustive over jump held / released.
    private static bool ScarletStaffTransition(bool[] now, bool[] next, float start, int gap = 28)
    {
        const int live = 12, speeds = 49;
        int total = gap + live;
        var heights = new float[ScarletStaffCells * speeds];
        var rises = new float[heights.Length];
        var stamp = new int[heights.Length];
        var active = new System.Collections.Generic.List<int>();
        var following = new System.Collections.Generic.List<int>();
        void Add(float h, float v, int tick)
        {
            int cell = Math.Clamp((int)MathF.Round(h * 2), 0, ScarletStaffCells - 1) * speeds + Math.Clamp((int)MathF.Round((v + 12) * 2), 0, speeds - 1);
            if (stamp[cell] == tick) return;
            stamp[cell] = tick; heights[cell] = h; rises[cell] = v; following.Add(cell);
        }
        Add(start, 0, 1);
        (active, following) = (following, active);
        for (int tick = 1; tick < total; tick++)
        {
            following.Clear();
            foreach (int cell in active)
                for (int jump = 0; jump < 2; jump++)
                {
                    var (h, v) = ScarletFlightStep(heights[cell], rises[cell], jump == 1);
                    bool ok = tick < live ? ScarletStaffSafeAt(now, h) : tick >= gap ? ScarletStaffSafeAt(next, h) : true;
                    if (ok) Add(h, v, tick + 1);
                }
            if (following.Count == 0) return false;
            (active, following) = (following, active);
        }
        return true;
    }

    [DomainTest("Scarlet shroud rope draws a five-line staff of two combs inside the field with a grounded jump-rope rhythm")]
    private static void ScarletShroudRopeStaff()
    {
        Span<CrimsonStroke> forecast = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var (groundX, groundY) in new[] { (8000, 6000), (2000, 1500) })
        {
            var f = RaidFieldGeometry.FromGround(groundX, groundY);
            AssertEqual(1120f, f.Bottom - f.Top, "field height");
            for (int note = 0; note < 4; note++)
            {
                var plan = ScarletSignaturePlan(1, 9, note, 0, groundX, groundY);
                plan.Validate();
                AssertEqual(CrimsonTechnique.ShroudRope, plan.Technique, "Act II signature");
                AssertEqual(12, plan.End - plan.Fire, "rift-like live window");
                bool low = note % 2 == 1; // odd steps (the final one among them) sweep the floor
                AssertEqual(5, CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true), "five lines per note");
                AssertEqual(0, CrimsonTechniqueGeometry.Write(plan, plan.Fire, live), "zero-width ignition is harmless");
                for (int line = 0; line < 5; line++)
                {
                    var cut = forecast[line];
                    float height = (low ? 36 : 148) + 224 * line;
                    AssertEqual(f.Bottom - height, cut.A.Y, $"line {line} of note {note} sits {height} px above the floor");
                    AssertEqual(cut.A.Y, cut.B.Y, "horizontal");
                    AssertEqual(36f, cut.Radius, "line radius");
                    AssertEqual(true, height - cut.Radius >= 0 && height + cut.Radius <= f.Bottom - f.Top, "the line lies inside the field");
                    AssertEqual(note % 2 == 0 ? f.Left : f.Right, cut.A.X, "alternating travel direction starts at the entering side");
                    AssertEqual(note % 2 == 0 ? f.Right : f.Left, cut.B.X, "and crosses the whole field");
                }
                for (float age = plan.Fire + .25f; age < plan.End; age += .25f)
                {
                    int count = CrimsonTechniqueGeometry.Write(plan, age, live);
                    AssertEqual(5, count, "five live lines");
                    for (int line = 0; line < 5; line++)
                    {
                        AssertEqual(forecast[line].A, live[line].A, "enters from the announced side");
                        AssertEqual(forecast[line].A.Y, live[line].B.Y, "stays on the announced height");
                        AssertEqual(forecast[line].Radius, live[line].Radius, "same width as announced");
                        if (age >= plan.Fire + 2) AssertEqual(forecast[line], live[line], "full width after two ticks");
                    }
                }
                AssertEqual(0, CrimsonTechniqueGeometry.Write(plan, plan.End, live), "exclusive end");
                // Grounded players keep the jump-rope rhythm: hit on even beats, safe on odd beats.
                foreach (float x in new[] { f.Left + 5, f.CenterX - 30, f.CenterX, f.Right - 25 })
                {
                    AssertEqual(low, ScarletBodyHit(forecast[..5], f, x, 0), "a standing player is hit on odd steps (the final one on the downbeat) and safe on even steps");
                    AssertEqual(low, ScarletBodyHit(forecast[..5], f, x, 60), "anything under 70 px is the floor band of the high-comb step");
                    AssertEqual(false, ScarletBodyHit(forecast[..5], f, x, low ? 127 : 239), "the middle of this beat's lowest airborne band is safe");
                    AssertEqual(true, ScarletBodyHit(forecast[..5], f, x, low ? 239 : 127), "and the other beat's band is hit");
                    AssertEqual(true, ScarletBodyHit(forecast[..5], f, x, low ? 36 : 148), "heights inside a line are hit");
                }
            }
        }
    }

    [DomainTest("Scarlet shroud rope: no height is safe on both beats and every safe height reaches the other beat's safe height within the beat")]
    private static void ScarletShroudRopeReach()
    {
        Span<CrimsonStroke> a = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> b = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        // "Even" names the low comb that sweeps the floor (odd steps since protocol80), "odd" the comb 112 px higher.
        int lowStep = CrimsonSignatureMoves.RopeIsLow(0) ? 0 : 1;
        AssertEqual(1, lowStep, "the final step (3) is a low-comb step");
        var even = ScarletSignaturePlan(1, 9, lowStep, 0);
        var odd = ScarletSignaturePlan(1, 9, 1 - lowStep, 0);
        var safeEven = ScarletStaffSafe(a[..CrimsonTechniqueGeometry.Write(even, even.Fire, a, true)], f);
        var safeOdd = ScarletStaffSafe(b[..CrimsonTechniqueGeometry.Write(odd, odd.Fire, b, true)], f);
        float widest = 0, farthest = 0, farthestAt = 0;
        for (int i = 0; i < ScarletStaffCells; i++)
        {
            AssertEqual(false, safeEven[i] && safeOdd[i], $"no height stays safe on both beats ({i * .5f} px)");
            // Distance to the nearest height that is safe on the other beat.
            foreach (var (source, to) in new[] { (safeEven, safeOdd), (safeOdd, safeEven) })
            {
                if (!source[i]) continue;
                int d = 0;
                while ((i - d < 0 || !to[i - d]) && (i + d >= to.Length || !to[i + d])) d++;
                if (d * .5f > farthest) { farthest = d * .5f; farthestAt = i * .5f; }
            }
        }
        AssertEqual(true, farthest >= 50 && farthest <= 115, $"the farthest a body has to move to change beat is {farthest} px (from {farthestAt} px up)");
        // Exhaustive over jump held or released each tick (base flight), from rest at every 4th safe height.
        int cases = 0;
        for (int i = 0; i < ScarletStaffCells; i += 12)
        {
            if (safeEven[i]) { cases++; AssertEqual(true, ScarletStaffTransition(safeEven, safeOdd, i * .5f), $"even to odd from {i * .5f} px up is reachable within the beat"); }
            if (safeOdd[i]) { cases++; AssertEqual(true, ScarletStaffTransition(safeOdd, safeEven, i * .5f), $"odd to even from {i * .5f} px up is reachable within the beat"); }
        }
        AssertEqual(true, cases > 100, $"enough starting heights were tried ({cases})");
        // The hardest start is the ceiling on an even beat (112 px to fall): it needs about 24 of the 28 ticks, and half a beat is not enough.
        int shortest = 12;
        while (shortest < 28 && !ScarletStaffTransition(safeEven, safeOdd, 1078, shortest)) shortest++;
        AssertEqual(true, shortest is >= 20 and <= 26, $"from the ceiling the transition needs {shortest} of the 28 ticks");
        AssertEqual(false, ScarletStaffTransition(safeEven, safeOdd, 1078, 14), "control: half a beat from the ceiling is not enough");
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
                            // Three fingers 213 px apart: solid across their 120 px width, open between them.
                            for (int finger = -1; finger <= 1; finger++)
                                foreach (float y in new[] { f.Top, f.CenterY, f.Bottom - 1 })
                                {
                                    float axis = left + 320 + finger * 213;
                                    AssertEqual(true, ScarletHits(forecast[..count], axis - 59, y, .001f, .001f), $"finger {finger} of quarter {q} is solid left");
                                    AssertEqual(true, ScarletHits(forecast[..count], axis + 59, y, .001f, .001f), $"finger {finger} of quarter {q} is solid right");
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
                        AssertEqual(60f, forecast[i].Radius, "finger radius");
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

    [DomainTest("Scarlet four hands fingers leave body-sized gaps and a clear spot within 71 px of anywhere on the floor")]
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
            // Every gap between neighbouring fingers admits the body with at least 30 px on each side.
            var axes = new System.Collections.Generic.List<float>();
            foreach (var finger in fingers) axes.Add(finger.A.X);
            axes.Sort();
            for (int i = 0; i + 1 < axes.Count; i++)
            {
                float gapLeft = axes[i] + 60, gapRight = axes[i + 1] - 60;
                if (gapRight - gapLeft > 150) continue; // a whole spared quarter, not a finger gap
                narrowest = Math.Min(narrowest, gapRight - gapLeft);
                AssertEqual(true, gapRight - gapLeft - ScarletPlayerWidth >= 60, $"a body fits a finger gap with 30 px each side ({gapRight - gapLeft})");
                foreach (float y in new[] { f.Top, f.CenterY, f.Bottom - ScarletPlayerHeight })
                    for (float x = gapLeft + 10; x <= gapRight - 10 - ScarletPlayerWidth; x += 1)
                        AssertEqual(false, ScarletHits(fingers, x, y), $"finger gap admits a body x={x - f.Left} y={y - f.Top}");
            }
            // A slammed quarter against a field wall leaves a strip the body fits in.
            foreach (float strip in new[] { axes[0] - 60 - f.Left, f.Right - (axes[^1] + 60) })
                if (strip < 150) wallStrip = Math.Min(wallStrip, strip);
        }
        AssertEqual(true, narrowest >= 93f - .01f, $"finger gaps are at least 93 px ({narrowest})");
        AssertEqual(true, wallStrip >= 47f - .01f && wallStrip - ScarletPlayerWidth >= 25, $"the strip against a wall takes a body ({wallStrip})");
        // On the 1 px grid the farthest is 71 px (from the middle of a finger); the touching limit is 70 px.
        AssertEqual(true, worst >= 60 && worst <= 71, $"the worst standing spot is within 71 px of a clear one ({worst})");
    }

    // Hands mobility: a body at rest at x0 when a note's forecast first shows (its Born) heads for a gap with the
    // given mobility, braking to a stop there. Real strokes are checked every tick from the strike on.
    // Returns the best (over candidate gaps) smallest horizontal clearance from the landing on; negative = hit.
    private static float ScarletHandsDodge(RaidFieldGeometry f, CrimsonGesturePlan plan, CrimsonStroke[] fingers, CrimsonStroke[][] byTick, float x0, ScarletMobility mobility)
    {
        var spans = new System.Collections.Generic.List<(float Low, float High)>();
        var edges = new System.Collections.Generic.List<(float Low, float High)>();
        foreach (var finger in fingers) edges.Add((finger.A.X - finger.Radius, finger.A.X + finger.Radius));
        edges.Sort();
        float cursor = f.Left;
        foreach (var (low, high) in edges) { if (low > cursor) spans.Add((cursor, low)); cursor = Math.Max(cursor, high); }
        if (cursor < f.Right) spans.Add((cursor, f.Right));
        float best = float.NegativeInfinity;
        foreach (var (low, high) in spans)
        {
            float margin = Math.Min(30, (high - low - ScarletPlayerWidth) * .5f);
            if (margin < 0) continue;
            foreach (float target in new[] { low + margin, high - ScarletPlayerWidth - margin })
            {
                float x = x0, speed = 0, clearance = float.MaxValue;
                bool hit = false;
                for (int tick = plan.Born; tick < plan.End; tick++)
                {
                    // Predicted rest position decides between accelerating and braking.
                    float rest = x + speed * Math.Abs(speed) / (2 * mobility.Acceleration);
                    int input = Math.Abs(target - rest) <= mobility.Acceleration * 2 ? 0 : Math.Sign(target - rest);
                    ScarletRunStep(mobility, ref x, ref speed, input);
                    float clamped = Math.Clamp(x, f.Left, f.Right - ScarletPlayerWidth);
                    if (clamped != x) { x = clamped; speed = 0; }
                    if (tick < plan.Fire) continue;
                    var live = byTick[tick - plan.Fire];
                    if (ScarletHits(live, x, f.Bottom - ScarletPlayerHeight)) hit = true;
                    if (tick >= plan.Fire + CrimsonSignatureMoves.HandsReachTicks) clearance = Math.Min(clearance, ScarletClearance(live, x));
                }
                best = Math.Max(best, hit ? Math.Min(clearance, -1) : clearance);
            }
        }
        return best;
    }

    [DomainTest("Scarlet four hands can be dodged from rest under endgame mobility but not under base mobility")]
    private static void ScarletFourHandsMobility()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        float worstEndgame = float.MaxValue, worstBase = float.MaxValue, worstEndgameAt = 0, worstBaseAt = 0;
        Span<CrimsonStroke> buffer = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (int phrase in new[] { 3, 6 }) for (int note = 0; note < 4; note++)
        {
            var plan = ScarletSignaturePlan(2, phrase, note, 0);
            var byTick = new CrimsonStroke[plan.End - plan.Fire][];
            for (int tick = plan.Fire; tick < plan.End; tick++) byTick[tick - plan.Fire] = buffer[..CrimsonTechniqueGeometry.Write(plan, tick, buffer)].ToArray();
            // The landed fingers decide the candidates; they stand still once down.
            var landed = new CrimsonStroke[6];
            buffer[..CrimsonTechniqueGeometry.Write(plan, plan.Fire, buffer, true)].CopyTo(landed);
            // Worst-case starts: every 5 px plus the middle of every finger.
            var starts = new System.Collections.Generic.List<float>();
            for (float x = f.Left; x <= f.Right - ScarletPlayerWidth; x += 5) starts.Add(x);
            foreach (var finger in landed) starts.Add(finger.A.X - ScarletPlayerWidth * .5f);
            foreach (float x0 in starts)
            {
                float endgame = ScarletHandsDodge(f, plan, landed, byTick, x0, ScarletEndgameRun);
                float baseline = ScarletHandsDodge(f, plan, landed, byTick, x0, ScarletBaseRun);
                if (endgame < worstEndgame) { worstEndgame = endgame; worstEndgameAt = x0 - f.Left; }
                if (baseline < worstBase) { worstBase = baseline; worstBaseAt = x0 - f.Left; }
            }
        }
        // Reported numbers: the smallest clearance (px) between the dodging body and a finger over the live window.
        AssertEqual(true, worstEndgame > 0, $"under endgame mobility every start can clear the fingers (smallest clearance {worstEndgame:F1} px, worst start x={worstEndgameAt})");
        // Documentation of the model, not a target: base mobility cannot cover the ~70 px in one beat from the middle of a finger.
        AssertEqual(true, worstBase < 0, $"four hands assumes endgame horizontal mobility like the basic beams (base worst {worstBase:F1} px, start x={worstBaseAt})");
    }

    // A member observed at x0 moving at v0 (px/tick) keeps that input through the look-ahead (the phrase is issued, and the
    // columns observed, LookAheadTicks before step 0's warning), then runs in `direction` once step 0's forecast shows.
    private static bool ScarletCurtainDrift(RaidFieldGeometry f, CrimsonGesturePlan[] plans, float x0, float v0, int direction, ScarletMobility mobility, out float clearance)
    {
        Span<CrimsonStroke> now = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        float x = x0, speed = v0;
        int lead = Math.Sign(v0);
        clearance = float.MaxValue;
        bool survived = true;
        for (int tick = plans[0].Born - CrimsonRhythm.LookAheadTicks; tick < plans[3].End; tick++)
        {
            ScarletRunStep(mobility, ref x, ref speed, tick >= plans[0].Born ? direction : lead);
            float clamped = Math.Clamp(x, f.Left, f.Right - ScarletPlayerWidth);
            if (clamped != x) { x = clamped; speed = 0; }
            for (int note = 0; note < 4; note++)
            {
                int count = CrimsonTechniqueGeometry.Write(plans[note], tick, now);
                if (count == 0) continue;
                if (ScarletHits(now[..count], x, f.Bottom - ScarletPlayerHeight)) survived = false;
                if (tick >= plans[note].Fire + 3) clearance = Math.Min(clearance, ScarletClearance(now[..count], x));
            }
        }
        return survived;
    }

    [DomainTest("Scarlet cinder curtain tolerates base-mobility drift during the look-ahead before its first step better than protocol 79")]
    private static void ScarletCinderCurtainLookAhead()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        float restClearance = float.MaxValue;
        int[] runningAway = new int[2], acceleratingAway = new int[2]; // [protocol80, protocol79 control]
        for (int column = 0; column < 10; column++) foreach (int phrase in new[] { 3, 6 })
        {
            var plans = new CrimsonGesturePlan[4];
            for (int note = 0; note < 4; note++) plans[note] = ScarletSignaturePlan(0, phrase, note, f.Left + 256 * column + 128);
            AssertEqual(plans[0].Born - CrimsonRhythm.LookAheadTicks, plans[0].Begin, "the columns are observed one look-ahead before step 0's warning");
            // Control: protocol 79 warned on beats 0-3 of the phrase and struck one beat later, with the same 30-tick look-ahead.
            var control = new CrimsonGesturePlan[4];
            int start = plans[0].Born - 70;
            for (int note = 0; note < 4; note++)
            {
                int fire = start + CrimsonMeter.BeatTick(note + 1) - CrimsonMeter.BeatTick(0);
                control[note] = plans[note] with { Born = start + CrimsonMeter.BeatTick(note) - CrimsonMeter.BeatTick(0), Fire = fire, End = fire + CrimsonSignatureMoves.CurtainLiveTicks };
            }
            var (_, direction) = ScarletOracleWalk(column, phrase);
            float columnLeft = f.Left + 256 * column;
            for (float x = columnLeft; x <= columnLeft + 256 - ScarletPlayerWidth; x += 1)
            {
                // Observed at rest, standing through the look-ahead or already accelerating the wrong way: always survives.
                AssertEqual(true, ScarletCurtainDrift(f, plans, x, 0, direction, ScarletBaseRun, out float still), $"column {column} phrase {phrase} at rest from x={x - columnLeft}");
                AssertEqual(true, ScarletCurtainDrift(f, plans, x, -direction * .0001f, direction, ScarletBaseRun, out float accelerating), $"column {column} phrase {phrase} accelerating away from x={x - columnLeft}");
                restClearance = Math.Min(restClearance, Math.Min(still, accelerating));
                if (!ScarletCurtainDrift(f, control, x, -direction * .0001f, direction, ScarletBaseRun, out _)) acceleratingAway[1]++;
                // Observed already running away from the open side at full base speed: the only failures, at the trailing edge.
                for (int timing = 0; timing < 2; timing++)
                    if (!ScarletCurtainDrift(f, timing == 0 ? plans : control, x, -direction * ScarletBaseRun.MaxSpeed, direction, ScarletBaseRun, out _))
                    {
                        runningAway[timing]++;
                        if (timing == 0) AssertEqual(true, direction > 0 ? x - columnLeft < 92 : columnLeft + 256 - ScarletPlayerWidth - x < 92, $"only the trailing 92 px fail (column {column}, x={x - columnLeft})");
                    }
            }
        }
        // Reported numbers: the smallest clearance from rest, and failing 1 px starts (20 interior column phrases of 237 each).
        AssertEqual(true, restClearance >= 3, $"from rest every start keeps clear of the fire (smallest {restClearance:F1} px)");
        AssertEqual(true, runningAway[0] <= 12 * 92 && runningAway[1] > 2 * runningAway[0], $"running away at full speed: {runningAway[0]} failing starts, protocol 79 {runningAway[1]}");
        AssertEqual(true, acceleratingAway[1] > 0, $"protocol 79 lost {acceleratingAway[1]} starts that accelerate away; protocol 80 none");
    }

    [DomainTest("Scarlet cinder curtain gives every observed member a corridor and burns only the rest")]
    private static void ScarletCinderCurtainCrowd()
    {
        Span<CrimsonStroke> forecast = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        // Every mask: the production union equals an independently written one, and each member's whole corridor is safe.
        for (int mask = 1; mask <= CrimsonSignatureMoves.MaximumCurtainMask; mask++) foreach (int phrase in new[] { 3, 6, 9, 12 }) for (int note = 0; note < 4; note++)
        {
            int safe = CrimsonSignatureMoves.CurtainSafe(mask, phrase, note);
            AssertEqual(ScarletOracleSafe(mask, phrase, note), safe, $"union equals the oracle mask={mask} phrase={phrase} note={note}");
            for (int column = 0; column < 10; column++)
            {
                if ((mask >> column & 1) == 0) continue;
                int promised = column is >= 2 and <= 7 ? 3 : 4;
                if (note < promised) AssertEqual(true, (safe >> column & 1) != 0, $"member in column {column} is safe on note {note} mask={mask} phrase={phrase}");
                int corridor = ScarletOracleCorridor(column, phrase, note);
                for (int k = 0; k < 4; k++) AssertEqual(true, (safe >> (corridor + k) & 1) != 0, "a member's whole corridor is safe");
            }
            AssertEqual(true, 10 - System.Numerics.BitOperations.PopCount((uint)safe) <= 6, "at most six columns burn");
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
            int safe = ScarletOracleSafe(mask, phrase, note);
            int count = CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true);
            AssertEqual(10 - System.Numerics.BitOperations.PopCount((uint)safe), count, "every column outside every corridor burns");
            AssertEqual(true, count <= 6 && count <= CrimsonSignatureMoves.MaximumStrokes, "strokes stay within the budget");
            AssertEqual(count, CrimsonSignatureMoves.CurtainBurning(plan), "burning column count helper");
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
            // The impact point is a burning column nearest to the reference.
            if (count > 0)
            {
                var at = CrimsonSignatureMoves.CurtainImpact(plan, f.Left + 5);
                int impactColumn = ScarletColumnOf(f, at.X);
                AssertEqual(false, (safe >> impactColumn & 1) != 0, "the impact lands on a burning column");
            }
        }
        // A solo mask is exactly the single-corridor curtain.
        foreach (int phrase in new[] { 3, 6, 9, 12 }) for (int column = 0; column < 10; column++) for (int note = 0; note < 4; note++)
        {
            var plan = ScarletSignaturePlan(0, phrase, note, f.Left + 256 * column + 128);
            int corridor = ScarletOracleCorridor(column, phrase, note);
            int count = CrimsonTechniqueGeometry.Write(plan, plan.Fire, forecast, true), expected = 0;
            for (int burning = 0; burning < 10; burning++)
            {
                if (burning >= corridor && burning < corridor + 4) continue;
                bool touches = burning == corridor - 1 || burning == corridor + 4;
                float x = f.Left + 256 * (burning + .5f);
                AssertEqual(new CrimsonStroke(new(x, f.Top), new(x, f.Bottom), touches ? 128f : 130f), forecast[expected++], "solo geometry is the single-corridor curtain");
            }
            AssertEqual(expected, count, "solo burns exactly the columns outside the corridor");
        }
        // The full crowd is accepted even where nothing is left to burn.
        var full = ScarletSignaturePlan(0, 3, 0, 0, curtainMask: CrimsonSignatureMoves.MaximumCurtainMask);
        full.Validate();
        AssertEqual(true, CrimsonTechniqueGeometry.Write(full, full.Fire, forecast, true) <= 6, "bounded strokes");
    }

    [DomainTest("Scarlet cinder curtain members stay survivable under base mobility with other members in the field")]
    private static void ScarletCinderCurtainCrowdBaseMobility()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        var random = new Random(0xC40D);
        for (int column = 0; column < 10; column++) foreach (int phrase in new[] { 3, 6 })
        {
            var (_, direction) = ScarletOracleWalk(column, phrase);
            bool interior = column is >= 2 and <= 7;
            float centre = f.Left + 256 * column + 128 - ScarletPlayerWidth * .5f;
            for (int trial = 0; trial < 6; trial++)
            {
                int mask = trial == 0 ? CrimsonSignatureMoves.MaximumCurtainMask : random.Next(1, 1024) | 1 << column;
                var crowd = new CrimsonGesturePlan[4];
                for (int note = 0; note < 4; note++) crowd[note] = ScarletSignaturePlan(0, phrase, note, 0, curtainMask: mask);
                AssertEqual(true, ScarletCurtainRun(f, crowd, centre, direction, ScarletBaseRun, interior ? crowd[0].Born : int.MaxValue, out _),
                    $"member in column {column} still survives with mask={mask} phrase={phrase}");
            }
        }
    }

    [DomainTest("Scarlet cinder curtain strikes at the burning column nearest the given x")]
    private static void ScarletCinderCurtainImpact()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        int checks = 0;
        foreach (int mask in new[] { 1 << 4, 1 << 2 | 1 << 7, 1 << 0 | 1 << 9, 1 << 5, 0b0100010010, 1 << 3 | 1 << 6 })
            foreach (int phrase in new[] { 3, 6 }) for (int note = 0; note < 4; note++)
            {
                var plan = ScarletSignaturePlan(0, phrase, note, 0, curtainMask: mask);
                int safe = ScarletOracleSafe(mask, phrase, note);
                var burning = new System.Collections.Generic.List<float>();
                for (int column = 0; column < 10; column++) if ((safe >> column & 1) == 0) burning.Add(f.Left + 256 * column + 128);
                if (burning.Count == 0) continue; // a crowd can leave nothing to burn; the fallback is checked below
                for (float x = f.Left - 300; x <= f.Right + 300; x += 37)
                {
                    // Skip a reference that is within a pixel of the middle between two burning columns.
                    var byDistance = burning.OrderBy(c => MathF.Abs(c - x)).ToArray();
                    if (byDistance.Length > 1 && MathF.Abs(MathF.Abs(byDistance[0] - x) - MathF.Abs(byDistance[1] - x)) < 1) continue;
                    var impact = CrimsonSignatureMoves.CurtainImpact(plan, x);
                    AssertEqual(byDistance[0], impact.X, $"nearest burning column to x={x - f.Left} (mask={mask} phrase={phrase} note={note})");
                    AssertEqual(f.CenterY, impact.Y, "mid-height");
                    checks++;
                }
            }
        AssertEqual(true, checks > 500, $"enough references were checked ({checks})");
        // With nothing burning the strike falls back to the field centre.
        var full = ScarletSignaturePlan(0, 3, 3, 0, curtainMask: CrimsonSignatureMoves.MaximumCurtainMask);
        if (CrimsonSignatureMoves.CurtainBurning(full) == 0) AssertEqual(f.CenterX, CrimsonSignatureMoves.CurtainImpact(full, f.Left).X, "field centre when nothing burns");
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
            var rhythm = CrimsonChoreography.Create(earliest, phrase, phase, false);
            int live = CrimsonSignatureMoves.LiveTicks(CrimsonSignatureMoves.ForAct(phase));
            AssertEqual(new[] { 20, 12, 16 }[phase], live, "live ticks per Act");
            AssertEqual(new[] { 24, 20, 24 }[phase], CrimsonSignatureMoves.ResidueTicks(CrimsonSignatureMoves.ForAct(phase)), "residue ticks per Act");
            for (int note = 0; note < 4; note++)
            {
                var plan = ScarletSignaturePlan(phase, phrase, note, 8000, 8000, 6000, earliest);
                var hit = rhythm.Hits[note];
                AssertEqual(hit.Fire + live, CrimsonEnsemble.NoteEnd(plan.Technique, hit), "signature notes end after their own live window");
                if (note < 3)
                {
                    var following = rhythm.Hits[note + 1];
                    AssertEqual(true, following.Fire - hit.Fire is 42 or 43, "steps a dotted quarter apart");
                    AssertEqual(true, CrimsonEnsemble.NoteEnd(plan.Technique, hit) <= following.Fire, "the previous step clears before the next strikes");
                    if (phase == 1) AssertEqual(true, CrimsonEnsemble.NoteEnd(plan.Technique, hit) <= following.Warning, "a rope comb is gone before the next comb is shown");
                }
                else AssertEqual(rhythm.End, hit.Fire, "the final step lands on the next phrase's downbeat");
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
