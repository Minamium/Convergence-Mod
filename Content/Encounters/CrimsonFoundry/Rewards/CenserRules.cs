#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Pure rules for the Ember Censer (REWARDS.md, "Summon - Ember Censer" and "Implementation shape"): no Terraria or XNA
// references; linked into Tests/Convergence.DomainTests and the offline preview. Constants stay in CrimsonRewardRules;
// this file holds the pendulum, the four-pour cycle, the largest-gap phase choice and the falling column.

// ai[0] of EmberCenserMinion.
internal enum CenserState { Idle = 0, Seek = 1, Swing = 2 }

// Sample k (0..CenserRules.FloorSamples) PourSample px below a column point: is it solid? A struct, so the scan
// allocates nothing; the Terraria side reads tiles, the tests read an array.
internal interface ICenserFloor { bool Solid(int sample); }

// One pour of the four-pour cycle as seen at a swing clock: its index in the cycle (3 = the Grand Pour), the clock it
// started on, its age in ticks (fractional while drawing), and the side of the apex it pours from (+1 right, -1 left).
internal readonly record struct CenserPour(int Index, int Start, float Age, int Side)
{
    internal bool Grand => Index == CrimsonRewardRules.GrandEvery - 1;
    internal int Live => Grand ? CrimsonRewardRules.GrandLive : CrimsonRewardRules.PourLive;
    internal float Radius => Grand ? CrimsonRewardRules.GrandRadius : CrimsonRewardRules.PourRadius;
    internal float Fall => Grand ? CrimsonRewardRules.GrandFall : CrimsonRewardRules.PourFall;
    internal float Multiplier => Grand ? CrimsonRewardRules.GrandMultiplier : CrimsonRewardRules.PourMultiplier;
}

internal static class CenserRules
{
    // ---- The swing clock (ai[2]) ------------------------------------------------------------------------------
    // Clock 0 is the censer's arrival with the bowl hanging straight down. The first swing winds the amplitude:
    // a quarter swing out to the small first apex (25 degrees, tick 16), then a half swing across to 55 degrees (tick
    // 48), the second apex and the first pour. From there the four-pour cycle repeats: apexes every 32 ticks, the
    // fourth swung out to 75 degrees and braced for 6 ticks, so a cycle is 32 + 32 + 38 + 32 = 134 ticks. After the
    // wind-up the clock wraps by one cycle (181 -> 48), so it stays a small exact integer however long it swings.
    // Nothing here reads a music, world or shared clock: every censer counts its own ticks from its own arrival.
    internal const int FirstApex = 16;
    internal const int WindUp = FirstApex + CrimsonRewardRules.ApexInterval;             // 48: the first pour
    internal const int Cycle = 4 * CrimsonRewardRules.ApexInterval + CrimsonRewardRules.GrandBrace; // 134
    internal const int ClockEnd = WindUp + Cycle;                                        // 182 (exclusive)
    internal const int GrandApex = 3 * CrimsonRewardRules.ApexInterval;                  // 96: reached, then braced
    internal const int StartWindow = CrimsonRewardRules.ApexInterval;                    // a newcomer starts at 0..31
    internal const int UntipTicks = 8;          // the bowl rights itself after a pour
    internal const int IgniteLead = 2;          // black blood burns in the bowl this long before it leaves the mouth
    internal const float IgniteRate = 2.5f;     // and its blaze cools this much faster once it falls: a hot lip over a
                                                // dark black-blood column with burning lips (it is fully open a tick below)
    internal const int MaxColumnPoints = CrimsonRewardRules.GrandLive + 2; // emission samples of one drawn column
    internal const int FloorSamples = (int)(CrimsonRewardRules.PourMaxDepth / CrimsonRewardRules.PourSample); // 75
    internal const int ReconcileAhead = 8;      // a peer keeps its own clock while it is at most this far ahead
    internal const float Degrees = MathF.PI / 180;

    // Advance the clock one tick (wrapping by one cycle after the wind-up).
    internal static int Advance(int clock)
    {
        int next = Math.Max(0, clock) + 1;
        return next >= ClockEnd ? next - Cycle : next;
    }

    // The clock that `steps` ticks of Advance reach.
    internal static int After(int clock, int steps)
    {
        for (int i = 0; i < steps; i++) clock = Advance(clock);
        return clock;
    }

    // Ticks into the four-pour cycle (0 = the first pour's apex) for a clock at or past the wind-up.
    internal static float Cyclic(float clock)
    {
        float q = (clock - WindUp) % Cycle;
        return q < 0 ? q + Cycle : q;
    }

    // ---- The cycle --------------------------------------------------------------------------------------------
    // Apex i of the cycle (i = 0..4; 4 is the next cycle's first) and the tick its pour starts (the Grand Pour waits
    // out its brace). Pour i pours from side +1 for even i and -1 for odd i.
    internal static int ApexTick(int i) => i * CrimsonRewardRules.ApexInterval + (i >= CrimsonRewardRules.GrandEvery ? CrimsonRewardRules.GrandBrace : 0);
    internal static int PourTick(int i) => ApexTick(i) + (i == CrimsonRewardRules.GrandEvery - 1 ? CrimsonRewardRules.GrandBrace : 0);
    internal static int Side(int i) => (i & 1) == 0 ? 1 : -1;
    internal static float Amplitude(int i) => (i & 3) == CrimsonRewardRules.GrandEvery - 1 ? CrimsonRewardRules.GrandSwing : CrimsonRewardRules.SwingTo;
    private static float ApexAngle(int i) => Side(i) * Amplitude(i) * Degrees;
    private static int LiveOf(int i) => i == CrimsonRewardRules.GrandEvery - 1 ? CrimsonRewardRules.GrandLive : CrimsonRewardRules.PourLive;

    // The pour whose live window contains `clock` (integer while simulating, fractional while drawing).
    internal static bool TryPour(float clock, out CenserPour pour)
    {
        pour = default;
        if (!(clock >= WindUp) || !(clock < ClockEnd + 1)) return false;
        float q = Cyclic(clock);
        for (int i = 0; i < CrimsonRewardRules.GrandEvery; i++)
        {
            int start = PourTick(i);
            if (q >= start && q < start + LiveOf(i))
            {
                pour = new CenserPour(i, WindUp + start, q - start, Side(i));
                return true;
            }
        }
        return false;
    }

    // Is `clock` the first tick of a pour (index 0..3)? The windup cue sounds SwingCueLead ticks before a normal
    // pour; the Grand Pour's windup is its brace.
    internal static bool PourStarts(int clock, out int index)
    {
        index = -1;
        if (clock < WindUp) return false;
        int q = (int)Cyclic(clock);
        for (int i = 0; i < CrimsonRewardRules.GrandEvery; i++)
            if (q == PourTick(i)) { index = i; return true; }
        return false;
    }
    internal static bool SwingCue(int clock)
        => PourStarts(After(clock, CrimsonRewardRules.SwingCueLead), out int index) && index != CrimsonRewardRules.GrandEvery - 1;
    internal static bool BraceStarts(int clock) => clock >= WindUp && (int)Cyclic(clock) == GrandApex;
    internal static bool Bracing(float clock)
    {
        if (!(clock >= WindUp)) return false;
        float q = Cyclic(clock);
        return q >= GrandApex && q < PourTick(CrimsonRewardRules.GrandEvery - 1);
    }

    // ---- The pendulum -----------------------------------------------------------------------------------------
    // Signed angle of the bowl from straight down (radians; positive swings the bowl to +x). Each half swing runs
    // between two apexes on a cosine, so the bowl comes to rest only where a pendulum does, at an apex, and never
    // stops mid-swing. The Grand Pour's brace holds the 75 degree apex for 6 ticks.
    internal static float Angle(float clock)
    {
        if (!(clock > 0)) return 0;
        float from = CrimsonRewardRules.SwingFrom * Degrees;
        if (clock < FirstApex) return -from * MathF.Sin(MathF.PI * .5f * clock / FirstApex);
        if (clock < WindUp) return Half(-from, ApexAngle(0), (clock - FirstApex) / CrimsonRewardRules.ApexInterval);
        float q = Cyclic(clock);
        for (int i = 0; i < CrimsonRewardRules.GrandEvery; i++)
        {
            // The bowl leaves apex i when pour i starts, reaches apex i + 1 a half swing later and leaves it when
            // pour i + 1 starts (6 ticks later for the braced Grand Pour; apex 4 is the next cycle's first).
            int leave = i + 1 < CrimsonRewardRules.GrandEvery ? PourTick(i + 1) : Cycle;
            if (q >= leave) continue;
            return q >= ApexTick(i + 1) ? ApexAngle(i + 1)
                : Half(ApexAngle(i), ApexAngle(i + 1), (q - PourTick(i)) / CrimsonRewardRules.ApexInterval);
        }
        return ApexAngle(0);
    }

    // a -> b in one half swing: cosine from rest to rest.
    private static float Half(float a, float b, float u)
        => (a + b) * .5f + (a - b) * .5f * MathF.Cos(MathF.PI * Math.Clamp(u, 0, 1));

    // The bowl's tip inward (radians, signed toward the apex's side so the mouth turns toward the target below): it
    // rises over the 4 ticks before a pour, holds through the live window and eases back over 8 ticks. The sprite
    // turns about its mouth, so tipping never moves the mouth the column hangs from.
    internal static float Tip(float clock)
    {
        float tip = CrimsonRewardRules.PourTip * Degrees, lead = CrimsonRewardRules.PourTipTicks;
        if (!(clock > WindUp - lead)) return 0;
        if (clock < WindUp) return Side(0) * tip * CrimsonRewardRules.Smooth((clock - (WindUp - lead)) / lead);
        float q = Cyclic(clock);
        if (q >= Cycle - lead) return Side(0) * tip * CrimsonRewardRules.Smooth((q - (Cycle - lead)) / lead);
        for (int i = 0; i < CrimsonRewardRules.GrandEvery; i++)
        {
            int start = PourTick(i), end = start + LiveOf(i);
            if (q >= start - lead && q < start) return Side(i) * tip * CrimsonRewardRules.Smooth((q - (start - lead)) / lead);
            if (q >= start && q <= end) return Side(i) * tip;
            if (q > end && q < end + UntipTicks) return Side(i) * tip * (1 - CrimsonRewardRules.Smooth((q - end) / UntipTicks));
        }
        return 0;
    }

    // The bowl mouth relative to the ring: BowlDrop along the pendulum line.
    internal static Vector2 Mouth(float clock) => MouthAt(Angle(clock));
    internal static Vector2 MouthAt(float angle) => new(CrimsonRewardRules.BowlDrop * MathF.Sin(angle), CrimsonRewardRules.BowlDrop * MathF.Cos(angle));

    // The bowl's warmth 0..1 for presentation: it smoulders before arriving, warms through the first swing, and
    // builds pour by pour toward the Grand Pour's ember-gold brace, then settles back. No periodic term.
    internal const float WarmthIdle = .15f, WarmthSwing = .45f;
    internal static float Warmth(float clock)
    {
        if (!(clock > 0)) return WarmthIdle;
        if (clock < WindUp) return WarmthIdle + (WarmthSwing - WarmthIdle) * CrimsonRewardRules.Smooth(clock / WindUp);
        float q = Cyclic(clock), step = CrimsonRewardRules.ApexInterval;
        if (q < ApexTick(1)) return Lerp(WarmthSwing, .6f, CrimsonRewardRules.Smooth(q / step));
        if (q < ApexTick(2)) return Lerp(.6f, .75f, CrimsonRewardRules.Smooth((q - ApexTick(1)) / step));
        if (q < GrandApex) return Lerp(.75f, 1, CrimsonRewardRules.Smooth((q - ApexTick(2)) / step));
        int dump = PourTick(CrimsonRewardRules.GrandEvery - 1);
        if (q < dump) return 1;
        return Lerp(1, WarmthSwing, CrimsonRewardRules.Smooth((q - dump) / (Cycle - dump)));
    }
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    // ---- Commitment -------------------------------------------------------------------------------------------
    // May the swing go on this tick? A censer whose owner cannot act or that has no target finishes a pour already
    // live (it started on an earlier tick), but never starts one: on a pour's first tick, or outside a live window,
    // it leaves the swing.
    internal static bool Continues(int clock, bool canPour)
        => canPour || TryPour(clock, out var pour) && pour.Age >= 1;

    // ---- The falling column -----------------------------------------------------------------------------------
    // Black blood leaves the mouth every tick of the live window and falls straight down at the pour's speed until it
    // meets the floor (the first solid sample, every 8 px, at most 600 px down). The column's top rides the mouth
    // through the return swing, so a pour is the curve of everything that has left the mouth since it started, all
    // relative to the ring (the column moves with its censer). Point k left the mouth on clock pour.Start + k; the
    // newest point is the mouth itself. Its collision radius is radius x InkOpen(ignition), exactly what is drawn.
    // Blood that reaches the floor pools into the newest point resting there, so the column runs from that point up
    // to the mouth and its foot slides across the floor with the sweep (Bottom).
    internal const float RestLift = .6f; // a resting point sits this many radii above the floor, so the foot stays on it
    internal static float Ignition(float emitted, float clock) => Math.Max(0, clock - emitted) * IgniteRate + IgniteLead;
    internal static float FallDistance(in CenserPour pour, float emitted, float clock)
        => Math.Min(CrimsonRewardRules.PourMaxDepth, pour.Fall * Math.Max(0, clock - emitted));
    // How far below the mouth a point comes to rest over a floor `depth` below it (PourMaxDepth: no floor found).
    internal static float Rest(in CenserPour pour, float depth)
        => depth >= CrimsonRewardRules.PourMaxDepth ? CrimsonRewardRules.PourMaxDepth : Math.Max(0, depth - pour.Radius * RestLift);
    internal static Vector2 Stream(in CenserPour pour, float emitted, float clock, float depth)
        => Mouth(emitted) + new Vector2(0, Math.Min(FallDistance(pour, emitted, clock), Rest(pour, depth)));
    internal static float StreamRadius(in CenserPour pour, float emitted, float clock)
        => pour.Radius * CrimsonRewardRules.InkOpen(Ignition(emitted, clock));
    // How far to sample the floor below a point this tick: as far as it can fall by the next tick (plus the rest
    // lift), at most 600.
    internal static float ScanDepth(in CenserPour pour, float emitted, float clock)
        => Math.Min(CrimsonRewardRules.PourMaxDepth, pour.Fall * (Math.Max(0, clock - emitted) + 1) + pour.Radius * RestLift);
    internal static bool Landed(in CenserPour pour, float emitted, float clock, float depth)
        => depth < CrimsonRewardRules.PourMaxDepth && FallDistance(pour, emitted, clock) >= Rest(pour, depth);
    // The oldest point still drawn and collided: the newest one resting on the floor (older ones have pooled into it).
    internal static int Bottom(in CenserPour pour, ReadOnlySpan<float> emitted, ReadOnlySpan<float> depths, float clock)
    {
        for (int k = Math.Min(emitted.Length, depths.Length) - 1; k >= 0; k--)
            if (Landed(pour, emitted[k], clock, depths[k])) return k;
        return 0;
    }

    // The column at `clock` relative to the ring, top (the mouth) first: every point still falling, then the foot.
    // When the stream rests on the floor its foot is where the stream's own line meets the rest height, so the column
    // never kinks into the floor. `depths[k]` is the floor below emission k (PourMaxDepth: none). The one geometry the
    // minion collides with and every client draws; returns the count and whether the foot rests on the floor.
    internal static int Column(in CenserPour pour, float clock, ReadOnlySpan<float> depths, Span<Vector2> points, Span<float> radii,
        Span<float> times, out bool rests)
    {
        Span<float> emitted = stackalloc float[MaxColumnPoints];
        int n = Math.Min(Emissions(pour, clock, emitted), depths.Length);
        rests = false;
        if (n == 0) return 0;
        int bottom = Bottom(pour, emitted[..n], depths[..n], clock);
        rests = Landed(pour, emitted[bottom], clock, depths[bottom]);
        int lowest = rests && bottom < n - 1 ? bottom + 1 : bottom, count = 0;
        for (int k = n - 1; k >= lowest && count < points.Length; k--, count++)
        {
            points[count] = Stream(pour, emitted[k], clock, depths[k]);
            radii[count] = StreamRadius(pour, emitted[k], clock);
            times[count] = Ignition(emitted[k], clock);
        }
        if (lowest == bottom || count == 0 || count >= points.Length) return count;
        Vector2 above = points[count - 1], under = Mouth(emitted[bottom]) + new Vector2(0, FallDistance(pour, emitted[bottom], clock));
        float floor = Mouth(emitted[bottom]).Y + Rest(pour, depths[bottom]), drop = under.Y - above.Y;
        float s = drop > 1e-3f ? Math.Clamp((floor - above.Y) / drop, 0, 1) : 1;
        points[count] = Vector2.Lerp(above, under, s);
        radii[count] = radii[count - 1] + (StreamRadius(pour, emitted[bottom], clock) - radii[count - 1]) * s;
        times[count] = times[count - 1] + (Ignition(emitted[bottom], clock) - times[count - 1]) * s;
        return count + 1;
    }

    // The emission clocks of a column at `clock`: pour.Start, pour.Start + 1, ..., and the mouth now if it lies
    // between whole ticks. Returns the count written (at most MaxColumnPoints).
    internal static int Emissions(in CenserPour pour, float clock, Span<float> emitted)
    {
        int whole = Math.Clamp((int)MathF.Floor(clock) - pour.Start, 0, MaxColumnPoints - 2);
        int n = 0;
        for (int k = 0; k <= whole && n < emitted.Length; k++) emitted[n++] = pour.Start + k;
        if (n < emitted.Length && clock - (pour.Start + whole) > 1e-4f) emitted[n++] = clock;
        return n;
    }

    // The depth to the floor below a point from samples every PourSample px (0..FloorSamples): the first solid sample
    // after at least one free sample (a censer inside a wall pours through it); PourMaxDepth when there is none
    // within `limit` (or within 600 px).
    internal static float FloorDepth<T>(in T probe, float limit) where T : struct, ICenserFloor
    {
        bool free = false;
        int samples = Math.Min(FloorSamples, (int)MathF.Ceiling(Math.Max(0, limit) / CrimsonRewardRules.PourSample));
        for (int k = 0; k <= samples; k++)
        {
            if (!probe.Solid(k)) { free = true; continue; }
            if (free) return Math.Min(CrimsonRewardRules.PourMaxDepth, k * CrimsonRewardRules.PourSample);
        }
        return CrimsonRewardRules.PourMaxDepth;
    }

    // Draw equals collide: does the box touch the column (capsules between consecutive points, each end with its
    // own radius)? A capsule whose ends differ is split so no part of it is wider than what is drawn there.
    internal static bool ColumnTouches(ReadOnlySpan<Vector2> points, ReadOnlySpan<float> radii, Vector2 min, Vector2 max)
    {
        int n = Math.Min(points.Length, radii.Length);
        if (n == 0) return false;
        if (n == 1) return radii[0] > 0 && CrimsonRewardRules.DiscTouchesBox(points[0], radii[0], min, max);
        for (int i = 1; i < n; i++)
        {
            Vector2 a = points[i - 1], b = points[i];
            float ra = radii[i - 1], rb = radii[i];
            int parts = MathF.Abs(ra - rb) < .5f ? 1 : ColumnTaperParts;
            for (int s = 0; s < parts; s++)
            {
                float u0 = s / (float)parts, u1 = (s + 1) / (float)parts;
                float r = Math.Min(ra + (rb - ra) * u0, ra + (rb - ra) * u1);
                if (r > 0 && CrimsonRewardRules.CapsuleTouchesBox(Vector2.Lerp(a, b, u0), Vector2.Lerp(a, b, u1), r, min, max)) return true;
            }
        }
        return false;
    }
    internal const int ColumnTaperParts = 8;

    // ---- Phase: the largest gap -------------------------------------------------------------------------------
    // When a censer arrives over a target, the owner starts its clock at 0..31 so its pours land as far as possible
    // from the pours of the censers already swinging over that target (the middle of the largest gap, measured over
    // two whole cycles so the braced Grand Pours count too). Ties start earliest in the wind-up. The others are never
    // re-phased: only the newcomer's start is chosen, so adding or losing a censer never makes the rest jump.
    internal const int PhaseHorizon = 2 * Cycle, MaxPhaseOthers = 20;
    internal static int ChooseStart(ReadOnlySpan<int> others)
    {
        if (others.IsEmpty) return 0;
        // A newcomer's first pour is more than WindUp - StartWindow ticks away, so pours older than one apex interval
        // can never be the nearest; that much history is enough.
        Span<int> theirs = stackalloc int[MaxPhaseOthers * 16];
        int count = 0, used = 0;
        foreach (int other in others)
        {
            if (used++ >= MaxPhaseOthers) break;
            count += Pours(other, -StartWindow, WindUp + PhaseHorizon + StartWindow, theirs[count..]);
        }
        if (count == 0) return 0;
        int best = 0, bestScore = -1;
        Span<int> mine = stackalloc int[16];
        for (int start = 0; start < StartWindow; start++)
        {
            int score = Score(start, theirs[..count], mine);
            if (score > bestScore) { bestScore = score; best = start; }
        }
        return best;
    }

    // The smallest distance (ticks) between the pours of a newcomer starting at `start` (over the horizon) and the
    // given pour times.
    internal static int Score(int start, ReadOnlySpan<int> theirs, Span<int> mine)
    {
        int n = Pours(start, 0, WindUp - start + PhaseHorizon, mine);
        int score = int.MaxValue;
        for (int i = 0; i < n; i++)
            foreach (int t in theirs) score = Math.Min(score, Math.Abs(mine[i] - t));
        return score;
    }

    // Pour start times (ticks from now, in [from, to)) of a censer whose clock reads `clock` now. Before the wind-up
    // ends nothing has poured; afterwards the cycle repeats both ways.
    internal static int Pours(int clock, int from, int to, Span<int> times)
    {
        if (clock < 0 || clock >= ClockEnd) return 0;
        int n = 0;
        int origin = clock < WindUp ? WindUp - clock : -(int)Cyclic(clock); // ticks until (or since) the cycle's tick 0
        int first = clock < WindUp ? 0 : (int)MathF.Floor((from - origin) / (float)Cycle) - 1;
        for (int c = first; n < times.Length; c++)
        {
            int cycleStart = origin + c * Cycle;
            if (cycleStart >= to) break;
            for (int i = 0; i < CrimsonRewardRules.GrandEvery && n < times.Length; i++)
            {
                int t = cycleStart + PourTick(i);
                if (t >= from && t < to) times[n++] = t;
            }
        }
        return n;
    }

    // ---- Replication ------------------------------------------------------------------------------------------
    // Signed ticks by which a peer's own clock leads one received from the owner (a stale packet arrives behind).
    internal static int Lead(int local, int received)
    {
        if (local < WindUp || received < WindUp) return local - received;
        int d = ((local - received) % Cycle + Cycle) % Cycle;
        return d > Cycle / 2 ? d - Cycle : d;
    }
    // A peer keeps its own clock while it leads the owner's last packet by at most ReconcileAhead ticks (the packet is
    // simply older); anything else is adopted.
    internal static bool KeepLocal(int local, int received) => Lead(local, received) is >= 0 and <= ReconcileAhead;

    internal static bool ValidAi(float state, float target, float clock)
        => CrimsonRewardRules.ValidInteger(state, (int)CenserState.Idle, (int)CenserState.Swing)
            && CrimsonRewardRules.ValidInteger(target, -1, MaxNpcSlot)
            && CrimsonRewardRules.ValidInteger(clock, 0, ClockEnd - 1);
    internal const int MaxNpcSlot = 199; // Main.maxNPCs - 1

    // ---- Roster -----------------------------------------------------------------------------------------------
    // The owner stamps each censer with the update it first ran on (1..SummonOrderWrap); it rides ExtraAI so a
    // censer's place follows summon order whichever projectile slot it landed in. Order 0 (not stamped yet) sorts last.
    internal const long SummonOrderWrap = 2_000_000_000;
    internal static int SummonOrder(ulong tick) => 1 + (int)(tick % (ulong)SummonOrderWrap);
    internal static long RosterKey(int order, int identity)
        => ((order > 0 ? order : (long)int.MaxValue) << 10) | (long)Math.Clamp(identity, 0, 1023);
}
