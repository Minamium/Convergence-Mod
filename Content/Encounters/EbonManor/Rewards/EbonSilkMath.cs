#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Pure geometry and timelines of Severing Silk (docs/encounters/ebon-manor/REWARDS.md "Rogue"). No
// Terraria/XNA references: the domain tests link this file together with EbonRewardRules.
internal static class EbonSilkMath
{
    // --- Sever timeline ---------------------------------------------------------------------
    // "Phase" counts ticks since the owner armed a strand (or the scissors began to snip): 1 on the
    // arming tick. A strand tightens for TightenTicks, parts at SeverPhase and is live for LiveTicks,
    // then stays as a harmless carrier while its halves recoil and the curls cascade out by rank.
    internal const int LiveTicks = 2, RecoilTicks = 14, CascadeStep = 2, CurlLife = 22, RetireTicks = 10, StraightenTicks = 3;
    internal const int SeverPhase = EbonRewardRules.TightenTicks + 1;
    internal const int EndPhase = SeverPhase + CascadeStep * (EbonRewardRules.MaxStrands - 1) + CurlLife;
    // The scissors recover for a moment after the snip, then dissolve.
    internal const int ScissorsRecover = 18, ScissorsEndPhase = SeverPhase + LiveTicks + ScissorsRecover;

    internal static bool Tightening(int phase) => phase >= 1 && phase <= EbonRewardRules.TightenTicks;
    internal static bool IsLive(int phase) => phase >= SeverPhase && phase < SeverPhase + LiveTicks;
    // Phase at which the strand of this rank pops its curls and rings its note.
    internal static int CascadeAt(int rank) => SeverPhase + CascadeStep * Math.Clamp(rank, 0, EbonRewardRules.MaxStrands - 1);
    // 0 -> 1 retraction of a severed half toward its anchor, `ticks` after the parting (fast, then easing out).
    internal static float Retract(float ticks)
    {
        float rest = 1 - Math.Clamp(ticks / RecoilTicks, 0, 1);
        return 1 - rest * rest * rest;
    }

    // Beak opening 0..1 on the scissors' own clock `t` (ticks since the arming tick; negative while still
    // braking in): it starts to gape as the scissors arrive, is fully open when the strands part, then
    // snaps shut in a tick and a half.
    internal const int GapeLead = 4;
    internal static float Gape(float t)
    {
        float open = EbonRewardRules.Smooth((t + GapeLead) / (EbonRewardRules.TightenTicks + GapeLead));
        float close = EbonRewardRules.Smooth((t - EbonRewardRules.TightenTicks) / 1.5f);
        return open * (1 - close);
    }

    // --- Scissors flight --------------------------------------------------------------------
    // Cruise at ScissorsSpeed, then brake linearly to rest over the last BrakeTicks so the scissors arrive
    // instead of stopping dead. A flight of F ticks (4..ScissorsLife) covers ScissorsSpeed * (F - 2) px.
    internal const int BrakeTicks = 4;
    internal static int FlightTicks(float distance)
        => Math.Clamp((int)MathF.Round(distance / EbonRewardRules.ScissorsSpeed + BrakeTicks * .5f), BrakeTicks, EbonRewardRules.ScissorsLife);
    internal static float FlightRange(int ticks) => EbonRewardRules.ScissorsSpeed * (ticks - BrakeTicks * .5f);
    // Displacement applied on tick `tick` (0-based) of a flight of `ticks`.
    internal static float FlightStep(int tick, int ticks)
    {
        if (tick < 0 || tick >= ticks) return 0;
        int brake = tick - (ticks - BrakeTicks);
        return brake < 0 ? EbonRewardRules.ScissorsSpeed : EbonRewardRules.ScissorsSpeed * (1 - (2 * brake + 1) / (2f * BrakeTicks));
    }

    // --- Strands ----------------------------------------------------------------------------
    // An idle strand fades out over its last FadeTicks (visual only).
    internal const int FadeTicks = 60;
    internal const float MinStrand = 40, MaxStrand = 1600, CutMin = .2f, CutMax = .8f;
    internal static bool Worthy(Vector2 a, Vector2 b)
    {
        float length = Vector2.Distance(a, b);
        return float.IsFinite(length) && length >= MinStrand && length <= MaxStrand;
    }
    // Where along a strand it parts: under the scissors' reach, kept off the very ends so both halves read.
    internal static float CutParameter(Vector2 a, Vector2 b, Vector2 from)
    {
        Vector2 ab = b - a; float length = ab.LengthSquared();
        float t = length < 1e-6f ? .5f : Vector2.Dot(from - a, ab) / length;
        return Math.Clamp(t, CutMin, CutMax);
    }
    // Ripple order: the strand whose cut is nearest the scissors goes first (ties by index).
    internal static void Rank(ReadOnlySpan<float> distance, Span<int> rank)
    {
        for (int i = 0; i < distance.Length; i++)
        {
            int r = 0;
            for (int j = 0; j < distance.Length; j++)
                if (distance[j] < distance[i] || distance[j] == distance[i] && j < i) r++;
            rank[i] = Math.Min(r, EbonRewardRules.MaxStrands - 1);
        }
    }
    // Index of the eligible entry with the least time left (the oldest strand), or -1.
    internal static int Oldest(ReadOnlySpan<int> timeLeft, ReadOnlySpan<bool> eligible)
    {
        int best = -1;
        for (int i = 0; i < timeLeft.Length; i++)
            if (eligible[i] && (best < 0 || timeLeft[i] < timeLeft[best])) best = i;
        return best;
    }
    // Arpeggio step of the strand ranked `rank` among `count`, spread so the last one rings the top note.
    internal static int NoteFor(int rank, int count)
        => count <= 1 ? 0 : (int)MathF.Round(Math.Clamp(rank, 0, count - 1) * (EbonRewardRules.NoteCount - 1) / (float)(count - 1));

    // --- Silk curve (visual) ----------------------------------------------------------------
    // A pinned strand rests with a slight gravity sag, which the tightening straightens away.
    internal static float Sag(float length) => MathF.Min(7f, length * .012f);
    // Multiplier of the resting sag `ticks` after the pin: starts taut, overshoots and settles.
    internal static float Settle(float ticks) => ticks <= 0 ? 0 : 1 - MathF.Exp(-ticks / 9f) * MathF.Cos(1.15f * ticks);
    // The bow's side is gravity's: toward +Y (or +X on a level tie). `Flipped` says that is the RIGHT normal of
    // a -> b, i.e. a string drawn from b to a (whose wave leans left) bows that same way.
    internal static bool Flipped(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a; float length = d.Length();
        if (length < 1e-4f) return false;
        Vector2 n = new Vector2(-d.Y, d.X) / length;
        return n.Y < 0 || n.Y == 0 && n.X < 0;
    }
    // Point at fraction u of a strand from a to b, bowed by `offset` px (a half sine) toward gravity.
    internal static Vector2 Bow(Vector2 a, Vector2 b, float offset, float u)
    {
        Vector2 d = b - a; float length = d.Length();
        Vector2 n = length < 1e-4f ? Vector2.UnitY : new Vector2(-d.Y, d.X) / length;
        if (Flipped(a, b)) n = -n;
        return a + d * u + n * (offset * MathF.Sin(MathF.PI * u));
    }
    internal static int Segments(float length) => Math.Clamp((int)(length / 80f), 3, 10);
    internal static void Polyline(Vector2 a, Vector2 b, float offset, Span<Vector2> points)
    {
        int n = Math.Max(1, points.Length - 1);
        for (int i = 0; i < points.Length; i++) points[i] = Bow(a, b, offset, i / (float)n);
    }
    // First crossing of two polylines: the silk knot (visual only).
    internal static bool Knot(ReadOnlySpan<Vector2> first, ReadOnlySpan<Vector2> second, out Vector2 at)
    {
        for (int i = 0; i + 1 < first.Length; i++)
        for (int j = 0; j + 1 < second.Length; j++)
            if (EbonRewardRules.SegmentsCross(first[i], first[i + 1], second[j], second[j + 1], out at)) return true;
        at = default;
        return false;
    }

    // --- Hit shapes -------------------------------------------------------------------------
    internal static bool CircleTouchesBox(Vector2 center, float radius, Vector2 min, Vector2 max)
        => Vector2.DistanceSquared(center, Vector2.Clamp(center, min, max)) <= radius * radius;
    // The strand's hit test. Vanilla asks every live NPC, so a conservative axis-aligned rejection (the box
    // grown by the strand's half width against the strand's own bounds) goes in front of the sampled segment
    // test: a far NPC costs four comparisons instead of up to 256 samples, and the answer never changes.
    internal static bool StrandTouchesBox(Vector2 min, Vector2 max, Vector2 a, Vector2 b, float width)
    {
        float reach = width * .5f;
        if (max.X < MathF.Min(a.X, b.X) - reach || min.X > MathF.Max(a.X, b.X) + reach
            || max.Y < MathF.Min(a.Y, b.Y) - reach || min.Y > MathF.Max(a.Y, b.Y) + reach) return false;
        return EbonRewardRules.BoxTouchesSegment(min, max, a, b, width);
    }
}
