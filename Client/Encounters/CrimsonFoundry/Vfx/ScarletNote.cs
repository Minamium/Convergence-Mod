#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

internal enum ScarletNoteKind : byte { Curtain, Beam, Rope, Rift, Hands, Rakes, Crossflow }

// One attack as a body performs it, derived only from the replicated plan (presentation never reads a beat grid):
//   Born, Fire, End  the plan's warning, strike and strike end; Span is the decay after Fire, so the note's window
//                    is [Born, Close) with Close = Fire + Span (signature moves: the prototype's 44 / 32 / 40).
//   Side             world-space direction of the gesture (+1 = toward +x): the Crown's swing, the Mantle's cut.
//   Low              Mantle: the low row leads (low comb / downward cut).
//   Lead             another note fired on exactly this note's Born (a hand-over), so no wind-up from rest. One plan cannot
//                    know it: TryFrom leaves it false and Collect sets it from the source's notes.
//   Broad            the seal crossflow (all limbs, no swing).
//   Reach            ticks the strike takes to reach across the field; Register scales the gesture (1 = signature).
//   ArmA, ArmB       Choir arms (texture space, flip applied) that own the struck ground; -1 = none.
//   AimX, AimY       screen-space unit vector from Vespera toward what this note announces (its forecast).
//   Seed             deterministic per note (Phrase, Pulse, Source), for analytic particles.
internal readonly record struct ScarletNote(float Born, float Fire, float End, float Span, ScarletNoteKind Kind,
    float Side, bool Low, bool Lead, bool Broad, float Reach, float Register, sbyte ArmA, sbyte ArmB,
    float AimX, float AimY, float Seed, int Phrase = 0, byte Pulse = 0, byte Source = 0)
{
    internal float Close => Fire + Span;
    internal bool Holds(float age) => age >= Born && age < Close;
    internal bool Signature => Kind is ScarletNoteKind.Curtain or ScarletNoteKind.Rope or ScarletNoteKind.Hands;
    internal float Weight => Broad ? 1.35f : Signature ? 1 : .8f; // Vespera's command weight
}

// Plans -> notes, the signal timing and the Choir cues. Terraria- and FNA-free (Vfx and Content authority only, as
// the offline previews link Vfx/*.cs): the domain tests and the harness pass plan lists; the game passes
// ScarletCueFrame's one scan per frame.
internal static class ScarletNotes
{
    internal const int Capacity = 8;
    // The longest true past pose a body re-creates from its notes: the Mantle's wake (16 ticks) and its lower
    // tendrils' four-tick lag, rounded up to the body range's 21 ticks.
    internal const int PastTicks = 21;
    internal const float MaximumSpan = 44, BasicRegister = .6f, CrossflowRegister = 1.25f;
    private const float CloseAim = 48;

    internal static bool KindOf(CrimsonTechnique technique, out ScarletNoteKind kind)
    {
        switch (technique)
        {
            case CrimsonTechnique.CinderCurtain: kind = ScarletNoteKind.Curtain; return true;
            case CrimsonTechnique.TrackingBeam: kind = ScarletNoteKind.Beam; return true;
            case CrimsonTechnique.ShroudRope: kind = ScarletNoteKind.Rope; return true;
            case CrimsonTechnique.SpatialRift: kind = ScarletNoteKind.Rift; return true;
            case CrimsonTechnique.FourHands: kind = ScarletNoteKind.Hands; return true;
            case CrimsonTechnique.ChoirRakes: kind = ScarletNoteKind.Rakes; return true;
            case CrimsonTechnique.SideBeams: kind = ScarletNoteKind.Crossflow; return true;
            default: kind = default; return false;
        }
    }

    // What the field shows after End: a signature move's own residue, a rift's 20, every other strike's 24.
    internal static int ResidueOf(in CrimsonGesturePlan plan)
        => plan.IsSignature ? CrimsonSignatureMoves.ResidueTicks(plan.Technique)
            : plan.IsRift ? CrimsonSpatialCuts.ResidueTicks : CrimsonRhythm.ResidueTicks;

    // Decay length after Fire: strike + residue, at most 44 (the prototype's Curtain); the crossflow is spent with its band.
    internal static float SpanOf(in CrimsonGesturePlan plan)
        => plan.Technique == CrimsonTechnique.SideBeams ? plan.End - plan.Fire
            : Math.Min(MaximumSpan, plan.End - plan.Fire + ResidueOf(plan));

    internal static bool TryFrom(in CrimsonGesturePlan plan, bool flipped, float vesperaX, float vesperaY, out ScarletNote note)
    {
        note = default;
        if (!KindOf(plan.Technique, out var kind)) return false;
        var f = plan.Field;
        float span = SpanOf(plan), side = 0, reach, register = BasicRegister, aimX = 0, aimY = 1;
        bool low = false, broad = false;
        sbyte armA = -1, armB = -1;
        switch (kind)
        {
            case ScarletNoteKind.Curtain:
            {
                register = 1; reach = CrimsonSignatureMoves.CurtainReachTicks;
                side = CurtainWalk(plan) * (plan.Pulse % 2 == 0 ? 1 : -1);
                int safe = CrimsonSignatureMoves.CurtainSafe(CrimsonSignatureMoves.CurtainMask(plan), plan.Phrase, plan.Pulse);
                float sum = 0; int burning = 0;
                for (int column = 0; column < CrimsonSignatureMoves.CurtainColumns; column++)
                    if ((safe >> column & 1) == 0) { sum += f.Left + CrimsonSignatureMoves.ColumnWidth * (column + .5f); burning++; }
                if (burning > 0) (aimX, aimY) = Normalize(Math.Clamp((sum / burning - vesperaX) / 900, -.6f, .6f), 1);
                break;
            }
            case ScarletNoteKind.Beam:
            case ScarletNoteKind.Rift:
            {
                bool rift = kind == ScarletNoteKind.Rift;
                reach = rift ? 2 : 5;
                var direction = CrimsonChoreography.Direction(plan.Phrase, plan.Pulse);
                var stroke = CrimsonTrackingBeam.Stroke(plan, plan.Fire, true);
                float parity = plan.Pulse % 2 == 0 ? 1 : -1;
                if (rift)
                {
                    side = direction.X > .01f ? 1 : direction.X < -.01f ? -1 : parity;
                    low = direction.Y >= .7f;
                }
                else
                {
                    // The Crown swings toward where its beam is announced (its own stage is the swing's pivot).
                    float dx = (stroke.A.X + stroke.B.X) * .5f - plan.Stage.X;
                    side = stroke.Radius <= 0 || MathF.Abs(dx) < 64 ? parity : MathF.Sign(dx);
                }
                (aimX, aimY) = Normalize(direction.X, direction.Y);
                if (stroke.Radius > 0)
                {
                    var nearest = Nearest(stroke.A, stroke.B, vesperaX, vesperaY);
                    float ax = nearest.X - vesperaX, ay = nearest.Y - vesperaY;
                    if (ax * ax + ay * ay >= CloseAim * CloseAim) (aimX, aimY) = Normalize(ax, ay);
                }
                break;
            }
            case ScarletNoteKind.Rope:
                register = 1; reach = 2;
                // The comb that sweeps the floor is the odd steps' (the final step on the downbeat among them) since protocol80, and
                // the rope crosses rightward on even steps (CrimsonSignatureMoves.Write), so the direction comes from the step,
                // never from the row.
                low = CrimsonSignatureMoves.RopeIsLow(plan.Pulse);
                side = (plan.Pulse & 1) == 0 ? 1 : -1;
                (aimX, aimY) = Normalize(side, low ? .35f : -.35f);
                break;
            case ScarletNoteKind.Hands:
            {
                register = 1; reach = CrimsonSignatureMoves.HandsReachTicks;
                var (first, second) = CrimsonSignatureMoves.HandsStruck(plan.Phrase, plan.Pulse);
                armA = (sbyte)ScarletGestureMotion.ArmForQuarter(first, flipped);
                armB = (sbyte)ScarletGestureMotion.ArmForQuarter(second, flipped);
                float mean = f.Left + CrimsonSignatureMoves.QuarterWidth * ((first + second) * .5f + .5f);
                (aimX, aimY) = Normalize((mean - vesperaX) / 1280, .8f);
                break;
            }
            case ScarletNoteKind.Rakes:
            {
                // The claws reach full length when the accepted Strike curve does (one tick before End).
                reach = Math.Max(1, plan.End - plan.Fire - 1);
                int edge = plan.Pulse & 3;
                (int a, int b) = RakeArms(edge, flipped);
                armA = (sbyte)a; armB = (sbyte)b;
                (aimX, aimY) = edge switch { 0 => (0f, 1f), 1 => (-1f, 0f), 2 => (0f, -1f), _ => (1f, 0f) };
                break;
            }
            default: // the seal crossflow: right to left, every limb at once
                register = CrossflowRegister; reach = 7; broad = true;
                (aimX, aimY) = (-1, 0);
                break;
        }
        note = new(plan.Born, plan.Fire, plan.End, span, kind, side, low, false, broad, reach, register,
            armA, armB, aimX, aimY, Seed(plan.Phrase, plan.Pulse, plan.Source), plan.Phrase, plan.Pulse, plan.Source);
        return true;
    }

    // The source's notes whose window holds age (or closed less than `lookback` ticks ago, for past poses), at most
    // Capacity, deduplicated by (Phrase, Pulse, Source) and ordered by Fire (then Pulse) so a body's response never
    // depends on projectile order. Every consumer of the current tick reads only notes that hold it (Holds / Phase),
    // so a closed note changes nothing now; it only lets a past pose replay the motion the body really made.
    internal static int Collect(ReadOnlySpan<CrimsonGesturePlan> plans, int source, float age, bool flipped,
        float vesperaX, float vesperaY, Span<ScarletNote> output, float lookback = 0)
    {
        int count = 0;
        foreach (var plan in plans)
        {
            if (plan.Source != source || age < plan.Born || age >= plan.Fire + SpanOf(plan) + lookback) continue;
            if (!TryFrom(plan, flipped, vesperaX, vesperaY, out var note)) continue;
            bool duplicate = false;
            for (int i = 0; i < count && !duplicate; i++)
                duplicate = output[i].Phrase == note.Phrase && output[i].Pulse == note.Pulse && output[i].Source == note.Source;
            if (duplicate) continue;
            int at = count;
            while (at > 0 && (output[at - 1].Fire > note.Fire || output[at - 1].Fire == note.Fire && output[at - 1].Pulse > note.Pulse)) at--;
            if (count == output.Length)
            {
                // A closed note (kept only for past poses) gives way to one that holds the tick; otherwise the latest
                // note of a full list is dropped, never an earlier one.
                int stale = -1;
                if (note.Holds(age)) for (int i = 0; i < count && stale < 0; i++) if (!output[i].Holds(age)) stale = i;
                if (stale >= 0)
                {
                    for (int i = stale; i < count - 1; i++) output[i] = output[i + 1];
                    count--;
                    if (at > stale) at--;
                }
                else if (at == count) continue;
                else count--;
            }
            for (int i = count; i > at; i--) output[i] = output[i - 1];
            output[at] = note; count++;
        }
        for (int i = 0; i < count; i++)
            if (HandedOver(plans, source, output[i])) output[i] = output[i] with { Lead = true };
        return count;
    }

    // A swing takes over from the previous strike's peak only when that strike fires on this note's Born. Since protocol80
    // a phrase's notes sit on the eighth-note grid (one beat of warning, strikes one to one and a half beats apart), so
    // most notes start from rest and only a note whose warning opens as another strikes is a hand-over. It is a property of
    // the plans, not of the collected window: the strike may already have left the window when the swing is still going.
    private static bool HandedOver(ReadOnlySpan<CrimsonGesturePlan> plans, int source, in ScarletNote note)
    {
        if (note.Broad) return false;
        foreach (var previous in plans)
            if (previous.Source == source && previous.Technique != CrimsonTechnique.SideBeams && KindOf(previous.Technique, out _)
                && previous.Fire == note.Born && previous.Fire < note.Fire) return true;
        return false;
    }

    // The accepted signal timing of a body (source < 0: every source plus the chorus): ticks until the nearest
    // pending Fire (at most 60) and since the latest Fire (at most 100). CrimsonRig.Signal maps them through
    // CrimsonRigMotion.Charge/Recoil.
    internal static (float Until, float Since) SignalTimes(ReadOnlySpan<CrimsonGesturePlan> gestures,
        ReadOnlySpan<CrimsonChorusPlan> choruses, int source, float age)
    {
        float until = 60, since = 100;
        foreach (var p in gestures)
            if (age >= p.Born && (source < 0 || p.Source == source))
            {
                float delta = p.Fire - age;
                if (delta >= 0) until = Math.Min(until, delta); else since = Math.Min(since, -delta);
            }
        if (source is -1 or 3)
            foreach (var c in choruses)
                if (age >= c.Born && age < c.End)
                {
                    float delta = c.Fire - age;
                    if (delta >= 0) until = Math.Min(until, delta); else since = Math.Min(since, -delta);
                }
        return (until, since);
    }

    // Choir arm cues. Acts: the arm that owns the struck ground (FourHands: the two slammed quarters; Rakes: the two
    // arms on the edge the claws enter from), each held through the note's whole window [Born, Fire + Span) so the
    // recovery ends with the residue. Final (`final`, every source) and a viewer outside the fight (`accepted`, the
    // Choir's own plans) keep the accepted single-arm cues unchanged.
    internal static int ChoirCues(ReadOnlySpan<CrimsonGesturePlan> plans, float age, bool flipped,
        Span<CrimsonChoirCue> cues, bool final = false, bool accepted = false)
    {
        int count = 0;
        foreach (var p in plans)
        {
            bool broad = p.Technique is CrimsonTechnique.SideBeams or CrimsonTechnique.SpatialGrid;
            if (final || accepted)
            {
                if (!final && p.Source != 2) continue;
                if (age < p.Born || age >= p.End) continue;
                Add(cues, ref count, new(p.Born, p.Fire, p.End, p.Step % 4, broad));
                continue;
            }
            if (p.Source != 2) continue;
            float close = p.Fire + SpanOf(p);
            if (age < p.Born || age >= close) continue;
            if (!broad && ChoirArms(p, flipped, out int a, out int b))
            {
                Add(cues, ref count, new(p.Born, p.Fire, close, a));
                Add(cues, ref count, new(p.Born, p.Fire, close, b));
            }
            else Add(cues, ref count, new(p.Born, p.Fire, close, p.Step % 4, broad));
        }
        return count;

        static void Add(Span<CrimsonChoirCue> cues, ref int count, CrimsonChoirCue cue)
        {
            for (int i = 0; i < count; i++) if (cues[i] == cue) return;
            if (count < cues.Length) cues[count++] = cue;
        }
    }

    internal static bool ChoirArms(in CrimsonGesturePlan plan, bool flipped, out int first, out int second)
    {
        switch (plan.Technique)
        {
            case CrimsonTechnique.FourHands:
                var (a, b) = CrimsonSignatureMoves.HandsStruck(plan.Phrase, plan.Pulse);
                first = ScarletGestureMotion.ArmForQuarter(a, flipped); second = ScarletGestureMotion.ArmForQuarter(b, flipped);
                return true;
            case CrimsonTechnique.ChoirRakes:
                (first, second) = RakeArms(plan.Pulse & 3, flipped);
                return true;
            default:
                first = second = -1; return false;
        }
    }

    // CrimsonChoirRakes' entry edge (Pulse & 3): 0 top -> upper arms, 2 bottom -> lower arms, 1 enters from the
    // world's right, 3 from its left -> that side's two arms (texture left = even arms, mirrored when flipped).
    internal static (int First, int Second) RakeArms(int edge, bool flipped)
    {
        int flip = flipped ? 1 : 0;
        return (edge & 3) switch
        {
            0 => (0, 1),
            2 => (2, 3),
            1 => (1 ^ flip, 3 ^ flip),
            _ => (0 ^ flip, 2 ^ flip)
        };
    }

    // The walk of a curtain phrase: one member's own direction, or alternating by signature ordinal for a crowd.
    internal static int CurtainWalk(in CrimsonGesturePlan plan)
    {
        int mask = CrimsonSignatureMoves.CurtainMask(plan);
        if (mask > 0 && (mask & (mask - 1)) == 0)
            return CrimsonSignatureMoves.CurtainWalk(System.Numerics.BitOperations.TrailingZeroCount(mask), plan.Phrase).Direction;
        return plan.Phrase / CrimsonSignatureMoves.Cadence % 2 == 0 ? 1 : -1;
    }

    internal static float Seed(int phrase, int pulse, int source)
    {
        uint h = (uint)phrase * 2654435761u ^ (uint)(pulse + 1) * 2246822519u ^ (uint)(source + 1) * 3266489917u;
        h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
        return (h & 0xFFFF) / 65536f;
    }

    private static (float X, float Y) Normalize(float x, float y)
    {
        float length = MathF.Sqrt(x * x + y * y);
        return length < 1e-6f ? (0, 1) : (x / length, y / length);
    }

    private static CrimsonPoint Nearest(CrimsonPoint a, CrimsonPoint b, float x, float y)
    {
        var v = b - a;
        float t = v.LengthSquared < 1e-6f ? 0 : Math.Clamp(((x - a.X) * v.X + (y - a.Y) * v.Y) / v.LengthSquared, 0, 1);
        return a + v * t;
    }
}
