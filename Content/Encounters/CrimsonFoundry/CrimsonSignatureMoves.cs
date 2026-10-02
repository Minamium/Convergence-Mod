using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// One signature move per Act (protocol79): every third phrase (serial % 3 == 0) of Acts I-III
// replaces the four basic notes with the Act's own physical trick and keeps the seal crossflow
// as its fifth note. Terraria-free: the server collides these exact capsules, clients draw them.
//
//   Act I   CinderCurtain  a 2,560 px curtain of fire in ten columns; a 768 px (three column)
//                           corridor walks one column per beat, away from the observed player's
//                           column (its leading end), so that column is safe for three notes.
//                           Consecutive corridors share 512 px.
//   Act II  ShroudRope     a full-width cut that alternates a low (jump it) and a high (170 px up,
//                           an ordinary jump clears it) height on every beat.
//   Act III FourHands      the field is four 640 px quarters; two are slammed per beat and two are
//                           left safe. Consecutive beats always share a safe quarter.
internal static class CrimsonSignatureMoves
{
    internal const int Cadence = 3, MaximumStrokes = 10;
    internal const int CurtainLiveTicks = 20, CurtainResidueTicks = 24, CurtainReachTicks = 3;
    internal const int RopeLiveTicks = CrimsonSpatialCuts.LiveTicks, RopeResidueTicks = CrimsonSpatialCuts.ResidueTicks;
    internal const int HandsLiveTicks = 16, HandsResidueTicks = 24, HandsReachTicks = 3;

    // Curtain: columns are exactly 256 px so the corridor edges sit on column bounds. Blocked columns
    // overlap their blocked neighbour by Join px (no hidden sliver); the two columns that touch the
    // corridor stay at exactly half a column so the corridor is never narrowed. Seven columns burn.
    internal const int CurtainColumns = 10, CurtainCorridorColumns = 3;
    internal const float ColumnWidth = 256, CurtainJoin = 2;

    // Rope: heights above the support surface (field Bottom). A standing player (42 tall) is hit by
    // the low cut and clears the high one by 118 px; feet risen 37..117 px (an ordinary full jump
    // reaches about 106) clear both, and only a rise of 118..180 px meets the high cut.
    internal const float RopeRadius = 10, RopeLowHeight = 26, RopeHighHeight = 170;

    // Hands: three upright claws per slammed quarter. Spacing 213 < 2 x 110 keeps them one solid slab
    // (+/-323 px about the quarter centre, 3 px past its edges). Any sideways hook would either leave
    // a player-sized nook inside the slab or push it past the quarter, so the claws stay upright.
    internal const int HandsQuarters = 4, HandsClaws = 3;
    internal const float QuarterWidth = 640, ClawRadius = 110, ClawSpacing = 213;
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

    // Column (0..9) holding x. The single authority observation lives in the plan's Target.
    internal static int CurtainColumn(RaidFieldGeometry field, float x)
        => Math.Clamp((int)MathF.Floor((x - field.Left) / ColumnWidth), 0, CurtainColumns - 1);
    // The walk of a phrase: first corridor's left column and direction (+1 right, -1 left). The observed
    // column is the corridor's LEADING end, so the walk heads away from it and the player's own column
    // stays safe for the first three notes. Right on even signature ordinals (serial / 3), left on odd
    // ones; the other way when the preferred walk does not fit the field. Near a wall neither fits:
    // walk away from the nearer wall with the first corridor clamped against it.
    internal static (int Start, int Direction) CurtainWalk(RaidFieldGeometry field, CrimsonPoint observed, int phrase)
    {
        int last = CurtainColumns - CurtainCorridorColumns, steps = CrimsonChoreography.BasicNotes - 1;
        int column = CurtainColumn(field, observed.X);
        int preferred = phrase / Cadence % 2 == 0 ? 1 : -1;
        if (Fits(preferred)) return (Start(preferred), preferred);
        if (Fits(-preferred)) return (Start(-preferred), -preferred);
        int away = column < CurtainColumns / 2 ? 1 : -1;
        return (Math.Clamp(Start(away), 0, last), away);

        int Start(int direction) => direction > 0 ? column - (CurtainCorridorColumns - 1) : column;
        bool Fits(int direction)
        {
            int start = Start(direction), end = start + steps * direction;
            return start >= 0 && start <= last && end >= 0 && end <= last;
        }
    }
    // Left column of the corridor (it spans CurtainCorridorColumns columns) for a note of the phrase.
    internal static int CurtainCorridor(RaidFieldGeometry field, CrimsonPoint observed, int phrase, int note)
    {
        var (start, direction) = CurtainWalk(field, observed, phrase);
        int last = CurtainColumns - CurtainCorridorColumns, steps = CrimsonChoreography.BasicNotes - 1;
        return Math.Clamp(start + Math.Clamp(note, 0, steps) * direction, 0, last);
    }
    internal static (float Left, float Right) CurtainGap(RaidFieldGeometry field, int corridor)
        => (field.Left + corridor * ColumnWidth, field.Left + (corridor + CurtainCorridorColumns) * ColumnWidth);

    internal static float RopeY(RaidFieldGeometry field, int note)
        => field.Bottom - (RopeIsLow(note) ? RopeLowHeight : RopeHighHeight);
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
                int corridor = CurtainCorridor(f, p.Target, p.Phrase, p.Pulse);
                for (int column = 0; column < CurtainColumns; column++)
                {
                    if (column >= corridor && column < corridor + CurtainCorridorColumns) continue;
                    bool touchesGap = column == corridor - 1 || column == corridor + CurtainCorridorColumns;
                    float x = f.Left + ColumnWidth * (column + .5f);
                    Add(output, ref count, new(x, f.Top), new(x, f.Bottom), reach, ColumnWidth * .5f + (touchesGap ? 0 : CurtainJoin));
                }
                break;
            }
            case CrimsonTechnique.ShroudRope:
            {
                float reach = forecast ? 1 : CrimsonSpatialCuts.Reach(age - p.Fire);
                if (reach <= 0) return 0;
                float y = RopeY(f, p.Pulse);
                bool rightward = (p.Pulse & 1) == 0;
                Add(output, ref count, new(rightward ? f.Left : f.Right, y), new(rightward ? f.Right : f.Left, y), reach, RopeRadius);
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
