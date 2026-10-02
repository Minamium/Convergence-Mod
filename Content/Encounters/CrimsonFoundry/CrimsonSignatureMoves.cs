using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// One signature move per Act (protocol79): every third phrase (serial % 3 == 0) of Acts I-III
// replaces the four basic notes with the Act's own physical trick and keeps the seal crossflow
// as its fifth note. Terraria-free: the server collides these exact capsules, clients draw them.
//
// Fairness model (the tests simulate it): a forecast is visible for one beat (28 ticks) and the player
// reacts only to what is shown. BASE mobility is Terraria's run (0.08 px/tick^2 up to 3 px/tick) with
// the field's flight (gravity 0.4, and Lerp(vy, -12, .18) while jump is held; wings never run out).
// ENDGAME mobility is 0.2 px/tick^2 up to 6 px/tick. Curtain and rope are meant for BASE; the hands
// (like the basic beams) assume ENDGAME horizontal mobility.
//
//   Act I   CinderCurtain  a 2,560 px curtain of fire in ten columns; every eligible member gets a
//                           1,024 px (four column) corridor that walks one column per beat toward the
//                           open side, away from the nearer wall (columns 0-4 walk right, 5-9 left),
//                           so the direction is readable from where the member stands. Their column
//                           keeps one column of margin on the side the walk heads for and is safe
//                           for three notes (all four against a wall, where the corridor stays put). The columns occupied at scheduling
//                           travel as a bit mask in Target; the safe columns of a note are the union
//                           of the corridors, so a crowd leaves less burning. At most six columns burn.
//   Act II  ShroudRope     a five-line staff crossing the whole field from alternating sides: even
//                           notes and odd notes are two combs 112 px apart. Grounded players are hit
//                           on even beats and safe on odd beats; flying higher is never permanently
//                           safe, because the other comb covers every height the first one spares.
//   Act III FourHands      the field is four 640 px quarters; two are slammed per beat and two are
//                           left safe. Consecutive beats always share a safe quarter. A slammed
//                           quarter is three upright fingers with 93 px gaps.
internal static class CrimsonSignatureMoves
{
    internal const int Cadence = 3, MaximumStrokes = 10;
    internal const int CurtainLiveTicks = 20, CurtainResidueTicks = 24, CurtainReachTicks = 3;
    internal const int RopeLiveTicks = CrimsonSpatialCuts.LiveTicks, RopeResidueTicks = CrimsonSpatialCuts.ResidueTicks;
    internal const int HandsLiveTicks = 16, HandsResidueTicks = 24, HandsReachTicks = 3;

    // Curtain: columns are exactly 256 px so the corridor edges sit on column bounds. Burning columns
    // overlap their burning neighbour by Join px (no hidden sliver); a burning column that touches a safe
    // column stays at exactly half a column so a corridor is never narrowed. At most six columns burn.
    internal const int CurtainColumns = 10, CurtainCorridorColumns = 4;
    internal const float ColumnWidth = 256, CurtainJoin = 2;

    // Rope: two combs of five horizontal lines, 224 px apart within a comb and 112 px apart between
    // combs, radius 36. The first (even) line spans the floor surface upward (centre 36, edge at 0);
    // the odd comb sits 112 px higher and its top line reaches the field ceiling (1008..1080 against a
    // body that can rise to 1078), so no height is ever safe on both beats: the free gap between a line
    // of one comb and its neighbour in the other is 40 px, less than the 42 px body.
    internal const int RopeLines = 5;
    internal const float RopeRadius = 36, RopeBase = 36, RopeLineSpacing = 224, RopeCombShift = 112;

    // Hands: three upright fingers (radius 60, spacing 213) per slammed quarter. The 93 px gaps between
    // them (94 px across a quarter boundary, 47 px against a field wall) admit a 20 px body. The fingers
    // span +/-273 px about the quarter centre, 47 px inside its edges.
    internal const int HandsQuarters = 4, HandsClaws = 3;
    internal const float QuarterWidth = 640, ClawRadius = 60, ClawSpacing = 213;
    // Strike pairs per note, ordered so that the two safe quarters of one beat and the next always
    // share a quarter: safe {1,3} {1,2} {0,2} {0,3}.
    private static readonly byte[][] Pairs = { new byte[] { 0, 2 }, new byte[] { 0, 3 }, new byte[] { 1, 3 }, new byte[] { 1, 2 } };

    internal static bool IsSignatureMove(CrimsonTechnique technique)
        => technique is CrimsonTechnique.CinderCurtain or CrimsonTechnique.ShroudRope or CrimsonTechnique.FourHands;
    internal static bool IsSignaturePhrase(int phase, int phrase) => phase is >= 0 and <= 2 && phrase >= 0 && phrase % Cadence == 0;
    internal static CrimsonTechnique ForAct(int phase) => phase switch
    {
        0 => CrimsonTechnique.CinderCurtain,
        1 => CrimsonTechnique.ShroudRope,
        2 => CrimsonTechnique.FourHands,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
    internal static int Owner(CrimsonTechnique technique) => technique switch
    {
        CrimsonTechnique.CinderCurtain => 0,
        CrimsonTechnique.ShroudRope => 1,
        CrimsonTechnique.FourHands => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(technique))
    };
    internal static int LiveTicks(CrimsonTechnique technique) => technique switch
    {
        CrimsonTechnique.CinderCurtain => CurtainLiveTicks,
        CrimsonTechnique.ShroudRope => RopeLiveTicks,
        CrimsonTechnique.FourHands => HandsLiveTicks,
        _ => throw new ArgumentOutOfRangeException(nameof(technique))
    };
    internal static int ResidueTicks(CrimsonTechnique technique) => technique switch
    {
        CrimsonTechnique.CinderCurtain => CurtainResidueTicks,
        CrimsonTechnique.ShroudRope => RopeResidueTicks,
        CrimsonTechnique.FourHands => HandsResidueTicks,
        _ => throw new ArgumentOutOfRangeException(nameof(technique))
    };

    // Column (0..9) holding x.
    internal static int CurtainColumn(RaidFieldGeometry field, float x)
        => Math.Clamp((int)MathF.Floor((x - field.Left) / ColumnWidth), 0, CurtainColumns - 1);
    // The plan's Target carries one bit per column that held an eligible member when the phrase was
    // scheduled (Target = (mask, 0), an exact integer 1..1023): every member gets a corridor, because a
    // forecast lasts one beat and a distant member could not reach somebody else's.
    internal const int MaximumCurtainMask = (1 << CurtainColumns) - 1;
    internal static CrimsonPoint CurtainTarget(int mask)
        => mask is >= 1 and <= MaximumCurtainMask ? new(mask, 0) : throw new ArgumentOutOfRangeException(nameof(mask));
    internal static bool ValidMask(CrimsonPoint target)
        => target.Y == 0 && target.X >= 1 && target.X <= MaximumCurtainMask && target.X == MathF.Floor(target.X);
    internal static int CurtainMask(in CrimsonGesturePlan p) => (int)p.Target.X;
    // The walk of a phrase for a member standing in `column`: first corridor's left column and direction
    // (+1 right, -1 left). The walk heads AWAY FROM THE NEARER WALL (columns 0-4 right, 5-9 left), so a
    // member knows it from where they stand and the direction never has to be learned from a forecast:
    // under base mobility a member in the trailing edge of their column could not cover the distance if
    // the direction only showed at note 1. The corridor keeps one column of margin on the side the walk
    // heads for, so the member's column is safe for the first three notes: a right walk is
    // [o-2..o+1] [o-1..o+2] [o..o+3] [o+1..o+4], a left walk [o-1..o+2] [o-2..o+1] [o-3..o] [o-4..o-1].
    // Next to a wall the walk does not fit (columns 0, 1, 8, 9): it turns toward that wall and every
    // note's corridor is clamped against it, so it slides in and stays ([0..3] on all four notes for
    // columns 0 and 1, [6..9] for 8 and 9). `phrase` is kept so callers do not change; the walk no longer
    // depends on the signature ordinal.
    internal static (int Start, int Direction) CurtainWalk(int column, int phrase)
    {
        int last = CurtainColumns - CurtainCorridorColumns, steps = CrimsonChoreography.BasicNotes - 1;
        if (column is < 0 or >= CurtainColumns) throw new ArgumentOutOfRangeException(nameof(column));
        int away = column < CurtainColumns / 2 ? 1 : -1;
        if (Fits(away)) return (Start(away), away);
        return (Start(-away), -away); // toward the nearer wall; the per-note clamp in CurtainCorridor slides it in

        int Start(int direction) => direction > 0 ? column - (CurtainCorridorColumns - 2) : column - 1;
        bool Fits(int direction)
        {
            int start = Start(direction), end = start + steps * direction;
            return start >= 0 && start <= last && end >= 0 && end <= last;
        }
    }
    // Left column of one member's corridor (it spans CurtainCorridorColumns columns) for a note.
    internal static int CurtainCorridor(int column, int phrase, int note)
    {
        var (start, direction) = CurtainWalk(column, phrase);
        int last = CurtainColumns - CurtainCorridorColumns, steps = CrimsonChoreography.BasicNotes - 1;
        return Math.Clamp(start + Math.Clamp(note, 0, steps) * direction, 0, last);
    }
    // Safe columns of a note (bit c = column c): the union of every observed member's corridor.
    internal static int CurtainSafe(int mask, int phrase, int note)
    {
        int safe = 0, corridor = (1 << CurtainCorridorColumns) - 1;
        for (int column = 0; column < CurtainColumns; column++)
            if ((mask >> column & 1) != 0) safe |= corridor << CurtainCorridor(column, phrase, note);
        return safe;
    }
    // Columns that burn on this note (a full crowd may leave none).
    internal static int CurtainBurning(in CrimsonGesturePlan p)
        => CurtainColumns - System.Numerics.BitOperations.PopCount((uint)CurtainSafe(CurtainMask(p), p.Phrase, p.Pulse));
    // Where the strike is felt and heard: the burning column nearest to referenceX (the local player),
    // at mid-height; the field centre when nothing burns.
    internal static CrimsonPoint CurtainImpact(in CrimsonGesturePlan p, float referenceX)
    {
        var f = p.Field;
        int safe = CurtainSafe(CurtainMask(p), p.Phrase, p.Pulse);
        float best = float.MaxValue, bestX = f.CenterX;
        for (int column = 0; column < CurtainColumns; column++)
        {
            if ((safe >> column & 1) != 0) continue;
            float x = f.Left + ColumnWidth * (column + .5f), distance = MathF.Abs(x - referenceX);
            if (distance < best) { best = distance; bestX = x; }
        }
        return new(bestX, f.CenterY);
    }

    // Height above the support surface of line `line` (0..4, bottom to top) of a note: even notes use
    // the lower comb, odd notes the comb 112 px higher.
    internal static float RopeHeight(int note, int line)
        => RopeBase + line * RopeLineSpacing + (RopeIsLow(note) ? 0 : RopeCombShift);
    internal static float RopeY(RaidFieldGeometry field, int note, int line) => field.Bottom - RopeHeight(note, line);
    internal static bool RopeIsLow(int note) => (note & 1) == 0;

    // The two quarters (0 = leftmost) a note slams. The labels rotate with the signature ordinal so
    // phrases differ; rotation is a bijection, so the two-safe-quarters and shared-safe rules hold.
    internal static (int First, int Second) HandsStruck(int phrase, int note)
    {
        int rotation = phrase / Cadence % HandsQuarters;
        var pair = Pairs[note & (CrimsonChoreography.BasicNotes - 1)];
        return ((pair[0] + rotation) % HandsQuarters, (pair[1] + rotation) % HandsQuarters);
    }
    internal static (float Left, float Right) HandsQuarter(RaidFieldGeometry field, int quarter)
        => (field.Left + quarter * QuarterWidth, field.Left + (quarter + 1) * QuarterWidth);
    internal static (CrimsonPoint Top, CrimsonPoint Bottom) HandsClaw(RaidFieldGeometry field, int quarter, int claw)
    {
        float x = field.Left + QuarterWidth * (quarter + .5f) + (claw - 1) * ClawSpacing;
        return (new(x, field.Top), new(x, field.Bottom));
    }

    internal static int Write(in CrimsonGesturePlan p, float age, Span<CrimsonStroke> output, bool forecast)
    {
        if (!forecast && !p.Live(age)) return 0;
        var f = p.Field;
        int count = 0;
        switch (p.Technique)
        {
            case CrimsonTechnique.CinderCurtain:
            {
                float reach = forecast ? 1 : CrimsonInvocation.Ease((age - p.Fire) / CurtainReachTicks);
                if (reach <= 0) return 0;
                int safe = CurtainSafe(CurtainMask(p), p.Phrase, p.Pulse);
                for (int column = 0; column < CurtainColumns; column++)
                {
                    if ((safe >> column & 1) != 0) continue;
                    bool touchesSafe = column > 0 && (safe >> (column - 1) & 1) != 0 || column < CurtainColumns - 1 && (safe >> (column + 1) & 1) != 0;
                    float x = f.Left + ColumnWidth * (column + .5f);
                    Add(output, ref count, new(x, f.Top), new(x, f.Bottom), reach, ColumnWidth * .5f + (touchesSafe ? 0 : CurtainJoin));
                }
                break;
            }
            case CrimsonTechnique.ShroudRope:
            {
                float reach = forecast ? 1 : CrimsonSpatialCuts.Reach(age - p.Fire);
                if (reach <= 0) return 0;
                bool rightward = (p.Pulse & 1) == 0;
                for (int line = 0; line < RopeLines; line++)
                {
                    float y = RopeY(f, p.Pulse, line);
                    Add(output, ref count, new(rightward ? f.Left : f.Right, y), new(rightward ? f.Right : f.Left, y), reach, RopeRadius);
                }
                break;
            }
            case CrimsonTechnique.FourHands:
            {
                float reach = forecast ? 1 : CrimsonInvocation.Ease((age - p.Fire) / HandsReachTicks);
                if (reach <= 0) return 0;
                var struck = HandsStruck(p.Phrase, p.Pulse);
                for (int hand = 0; hand < 2; hand++)
                    for (int claw = 0; claw < HandsClaws; claw++)
                    {
                        var (top, bottom) = HandsClaw(f, hand == 0 ? struck.First : struck.Second, claw);
                        Add(output, ref count, top, bottom, reach, ClawRadius);
                    }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(p));
        }
        return count;
    }

    private static void Add(Span<CrimsonStroke> output, ref int count, CrimsonPoint a, CrimsonPoint b, float reach, float radius)
    {
        if (count >= output.Length) throw new ArgumentException("crimson.stroke_capacity");
        output[count++] = new(a, CrimsonPoint.Lerp(a, b, reach), radius);
    }
}
