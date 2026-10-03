#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// A live stretch of a release line: from A to B, damaging within HalfWidth px of the segment.
internal readonly record struct MeridianSegment(Vector2 A, Vector2 B, float HalfWidth);

// One lattice line in the release frame (+along = the meridian's direction, +across = its left normal), centred on
// the node: a parallel sits at `Across` and runs along [-HalfSpan, HalfSpan]; a perpendicular sits at `Along` and
// runs across [-HalfSpan, HalfSpan]. Ring = its ripple order from the node.
internal readonly record struct MeridianLatticeLine(float Across, float Along, float HalfSpan, bool Perpendicular, int Ring);

// Pale Meridian release geometry and live windows: one white meridian from the muzzle through the cursor's node,
// then at tiers 2-3 a small lattice of light around the node. Gameplay owns these constants; the art and light are
// drawn on them. The lattice never has four or more evenly spaced parallel lines (a music staff is Scarlet
// Invocation's motif): tier 3 is the meridian with parallels at +-120 px crossed by perpendiculars at 0 and +-120 px,
// each 480 px (a '#'); tier 2 the meridian with parallels at +-100 px and one perpendicular through the node, 320 px.
// Pure (System.Numerics only): linked into the domain tests and the offline preview.
internal static class PaleMeridianLattice
{
    // The node is the cursor's distance from the muzzle along the release direction, clamped.
    internal const float NodeMin = 160, NodeMax = 1200, Overshoot = 640;
    internal const float MeridianWidth = 28, LatticeWidth = 20;
    // Meridian ages (from the release): a harmless forecast for MeridianFire ticks, then a packet whose head runs the
    // whole line in MeridianTransit ticks with its tail MeridianTail ticks behind; live while any stretch is lit.
    internal const int MeridianFire = 10, MeridianTransit = 8, MeridianTail = 5;
    internal const int MeridianLive = MeridianTransit + MeridianTail - 1;
    // Lattice ages: the projectile starts at -(ceil(NodeAge) + SplitLead), so the split begins as the meridian's head
    // passes the node; the lines slide out over SplitOpen, hold SplitHold, then fire ring by ring every RippleStep
    // ticks, each lighting from its middle to both ends in LatticeTransit ticks with the tail LatticeTail behind.
    internal const int SplitOpen = 6, SplitHold = 4, SplitLead = SplitOpen + SplitHold;
    internal const int RippleStep = 2, LatticeTransit = 6, LatticeTail = 4;
    internal const int LatticeLive = LatticeTransit + LatticeTail - 1;
    internal const int Rings = 3;
    // The projectiles stay (harmless) while their residue cools. A replicated line age at or below EarliestAge is
    // rejected as corrupt; every real LatticeStart lies above it (domain-tested over the whole node range).
    internal const int MeridianEnd = 40, LatticeEnd = 36, EarliestAge = -64;
    internal const float Tier3Spacing = 120, Tier3HalfSpan = 240, Tier2Spacing = 100, Tier2HalfSpan = 160;
    internal const int MaxLines = 5, MaxSegments = 2 * MaxLines;

    internal static float Node(float cursorDistance)
        => float.IsFinite(cursorDistance) ? Math.Clamp(cursorDistance, NodeMin, NodeMax) : NodeMin;

    internal static float Length(float node) => node + Overshoot;

    // Meridian age at which the packet's head passes the node.
    internal static float NodeAge(float node) => MeridianFire - 1 + MeridianTransit * node / Length(node);

    // The lattice projectile's age when it is spawned on the release tick.
    internal static int LatticeStart(float node) => -((int)MathF.Ceiling(NodeAge(node) - 1e-4f) + SplitLead);

    // Ticks after the release tick on which the first hit of each projectile can land (same-tick first update).
    internal static int MeridianFirstHit => MeridianFire - 1;
    internal static int LatticeFirstHit(float node) => -LatticeStart(node) - 1;

    internal static float Spacing(int tier) => tier >= 3 ? Tier3Spacing : Tier2Spacing;
    internal static float HalfSpan(int tier) => tier >= 3 ? Tier3HalfSpan : Tier2HalfSpan;

    // The lattice lines of a tier (tier 2 or 3; none below).
    internal static int Lines(int tier, Span<MeridianLatticeLine> output)
    {
        if (tier < 2 || output.Length < MaxLines) return 0;
        float spacing = Spacing(tier), half = HalfSpan(tier);
        int count = 0;
        output[count++] = new MeridianLatticeLine(0, 0, half, true, 0);
        output[count++] = new MeridianLatticeLine(spacing, 0, half, false, 1);
        output[count++] = new MeridianLatticeLine(-spacing, 0, half, false, 1);
        if (tier >= 3)
        {
            output[count++] = new MeridianLatticeLine(0, spacing, half, true, 2);
            output[count++] = new MeridianLatticeLine(0, -spacing, half, true, 2);
        }
        return count;
    }

    // World end points of a lattice line, from its middle: Middle +- Direction * HalfSpan.
    internal static void Place(in MeridianLatticeLine line, Vector2 origin, Vector2 axis, float node, out Vector2 middle, out Vector2 direction)
    {
        Vector2 across = new(-axis.Y, axis.X);
        Vector2 centre = origin + axis * node;
        middle = centre + axis * line.Along + across * line.Across;
        direction = line.Perpendicular ? across : axis;
    }

    // 0..1 as the lattice lines slide out of the meridian and unfold through the node (lattice ages -SplitLead..).
    internal static float SplitOpening(float latticeAge)
        => PaleMeridianScore.Arrive((latticeAge + SplitLead) / SplitOpen);

    // Fraction lit (head) and fraction passed (tail) of a packet `s` ticks after it fires.
    internal static void Packet(float s, int transit, int tail, out float head, out float passed)
    {
        head = Math.Clamp((s + 1) / transit, 0, 1);
        passed = Math.Clamp((s + 1 - tail) / transit, 0, 1);
    }

    // The damaging stretches at `age` (meridian age, or lattice age when `lattice`). Nothing is live during the
    // forecast or the split, or after a tail has passed. `output` needs MaxSegments entries.
    internal static int Live(Vector2 origin, Vector2 axis, float node, int tier, bool lattice, float age, Span<MeridianSegment> output)
    {
        if (!Valid(origin, axis, node, tier, lattice) || !float.IsFinite(age) || output.Length < MaxSegments) return 0;
        int count = 0;
        if (!lattice)
        {
            float s = MathF.Floor(age) - MeridianFire;
            if (s < 0 || s >= MeridianLive) return 0;
            Packet(s, MeridianTransit, MeridianTail, out float head, out float passed);
            float length = Length(node);
            if (head > passed) output[count++] = new MeridianSegment(origin + axis * (length * passed), origin + axis * (length * head), MeridianWidth / 2);
            return count;
        }
        Span<MeridianLatticeLine> lines = stackalloc MeridianLatticeLine[MaxLines];
        int n = Lines(tier, lines);
        for (int i = 0; i < n; i++)
        {
            float s = MathF.Floor(age) - RippleStep * lines[i].Ring;
            if (s < 0 || s >= LatticeLive) continue;
            Packet(s, LatticeTransit, LatticeTail, out float head, out float passed);
            if (!(head > passed)) continue;
            Place(lines[i], origin, axis, node, out Vector2 middle, out Vector2 direction);
            float half = lines[i].HalfSpan;
            output[count++] = new MeridianSegment(middle + direction * (half * passed), middle + direction * (half * head), LatticeWidth / 2);
            output[count++] = new MeridianSegment(middle - direction * (half * passed), middle - direction * (half * head), LatticeWidth / 2);
        }
        return count;
    }

    // Separating-axis test of the segment's rectangle (half width out from the segment, no caps) against an
    // axis-aligned box.
    internal static bool Hits(in MeridianSegment segment, Vector2 boxCenter, Vector2 boxHalf)
    {
        Vector2 delta = segment.B - segment.A;
        float length = delta.Length();
        if (!(length > 1e-4f) || !(segment.HalfWidth > 0)) return false;
        Vector2 u = delta / length, v = new(-u.Y, u.X);
        Vector2 centre = (segment.A + segment.B) * .5f, d = boxCenter - centre;
        float hu = length * .5f, hv = segment.HalfWidth;
        // Box axes.
        if (MathF.Abs(d.X) > boxHalf.X + hu * MathF.Abs(u.X) + hv * MathF.Abs(v.X)) return false;
        if (MathF.Abs(d.Y) > boxHalf.Y + hu * MathF.Abs(u.Y) + hv * MathF.Abs(v.Y)) return false;
        // Segment axes.
        if (MathF.Abs(Vector2.Dot(d, u)) > hu + boxHalf.X * MathF.Abs(u.X) + boxHalf.Y * MathF.Abs(u.Y)) return false;
        if (MathF.Abs(Vector2.Dot(d, v)) > hv + boxHalf.X * MathF.Abs(v.X) + boxHalf.Y * MathF.Abs(v.Y)) return false;
        return true;
    }

    // Replicated state a line projectile accepts: finite, a unit direction, a node in range and a tier that has
    // this part of the finisher.
    internal static bool Valid(Vector2 origin, Vector2 axis, float node, int tier, bool lattice)
        => float.IsFinite(origin.X) && float.IsFinite(origin.Y) && float.IsFinite(axis.X) && float.IsFinite(axis.Y)
            && MathF.Abs(axis.Length() - 1) <= 1e-3f && float.IsFinite(node) && node >= NodeMin - 1e-3f && node <= NodeMax + 1e-3f
            && (lattice ? PaleMeridianScore.HasLattice(tier) : PaleMeridianScore.HasFinisher(tier));
}
