#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// The residue of a signature move (CinderCurtain, ShroudRope, FourHands) is drawn UNDER every forecast and
// yields where the next beat is safe, so dried ink never makes safe ground look dangerous. Each residue stroke
// is compared with the forecast footprint of the move's next note (same Fight, Epoch, Phrase and technique,
// Pulse + 1): a stroke that footprint substantially covers keeps the technique's full residue, because that
// ground stays dangerous; every other stroke, and every stroke of the move's last note, dries away in
// YieldTicks. Basic beams keep the approved residue and order. Collision never reads any of this.
//
// Terraria- and FNA-free (System and the Content geometry only): the domain tests link this file.
internal static class ScarletResidueYield
{
    internal const int YieldTicks = 6;

    // The single owner switch. true: under the forecast and yielding. false: a signature residue is drawn like a
    // basic beam's (over the forecast, full length). Static so the offline preview can render both for review.
    internal static bool Enabled = true;

    // Whether this plan's residue (End <= age < End + residue) takes the yielding path.
    internal static bool Applies(in CrimsonGesturePlan plan) => Enabled && plan.IsSignature;

    internal static bool Follows(in CrimsonGesturePlan plan, in CrimsonGesturePlan next)
        => next.Fight == plan.Fight && next.Boss == plan.Boss && next.Epoch == plan.Epoch && next.Phrase == plan.Phrase
            && next.Technique == plan.Technique && next.Pulse == plan.Pulse + 1;

    // Index of the move's next note among candidates, or -1 (the last note, or a note whose successor is gone).
    internal static int Successor(in CrimsonGesturePlan plan, ReadOnlySpan<CrimsonGesturePlan> candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
            if (Follows(plan, candidates[i])) return i;
        return -1;
    }

    // The footprint the next note forecasts (exactly what its warning shows and its strike may cover).
    internal static int Forecast(in CrimsonGesturePlan next, Span<CrimsonStroke> output)
        => CrimsonTechniqueGeometry.Write(next, next.Fire, output, true);

    // A residue stroke holds when a forecast capsule reaches its axis within the larger of the two radii: a
    // capsule over the same column, line or finger. Neighbouring curtain columns share only a 2 px seam and a
    // rope's other comb lies 112 px away; neither counts, so those strokes yield.
    internal static bool Holds(in CrimsonStroke residue, ReadOnlySpan<CrimsonStroke> forecast)
    {
        foreach (var next in forecast)
            if (AxisDistance(residue, next) < Math.Max(residue.Radius, next.Radius)) return true;
        return false;
    }

    // Residue strength scale `since` ticks after End: 1 where the stroke holds, else it falls to 0 at YieldTicks.
    internal static float Factor(bool holds, float since)
        => holds ? 1 : 1 - CrimsonInvocation.Ease(since / YieldTicks);

    // Shortest distance between the two capsule axes (0 when they cross).
    internal static float AxisDistance(in CrimsonStroke a, in CrimsonStroke b)
    {
        if (Cross(a.A, a.B, b.A, b.B)) return 0;
        return MathF.Min(MathF.Min(PointToSegment(a.A, b.A, b.B), PointToSegment(a.B, b.A, b.B)),
            MathF.Min(PointToSegment(b.A, a.A, a.B), PointToSegment(b.B, a.A, a.B)));
    }

    private static float PointToSegment(CrimsonPoint p, CrimsonPoint a, CrimsonPoint b)
    {
        var v = b - a; float length = v.LengthSquared;
        float t = length < 1e-6f ? 0 : Math.Clamp(((p.X - a.X) * v.X + (p.Y - a.Y) * v.Y) / length, 0, 1);
        var d = p - (a + v * t);
        return MathF.Sqrt(d.LengthSquared);
    }

    // Proper crossing of two segments; touching and collinear cases are covered by the endpoint distances.
    private static bool Cross(CrimsonPoint a, CrimsonPoint b, CrimsonPoint c, CrimsonPoint d)
    {
        float d1 = Side(c, d, a), d2 = Side(c, d, b), d3 = Side(a, b, c), d4 = Side(a, b, d);
        return (d1 > 0 && d2 < 0 || d1 < 0 && d2 > 0) && (d3 > 0 && d4 < 0 || d3 < 0 && d4 > 0);
    }

    private static float Side(CrimsonPoint a, CrimsonPoint b, CrimsonPoint p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
}
