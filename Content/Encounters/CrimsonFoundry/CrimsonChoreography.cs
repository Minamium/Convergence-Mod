using System;
using System.Collections.Generic;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Musical composition only: every phrase is two whole bars of the 128 BPM grid. Only the key moments land on
// bar heads (a crossflow's release, a signature move's final hit; chorus calls and verdicts, act changes and the
// Final drop elsewhere); the ordinary notes between them are sparse and syncopated on the eighth-note grid, so a
// phrase is no longer a strike on every beat (owner feedback 2026-10-03).
internal static class CrimsonChoreography
{
    // The intro's eight bars before Act I unlocks; Ember Crown emerges on bar four.
    internal static readonly int OpeningTicks = CrimsonMeter.BarTick(CrimsonMeter.OpeningBars);
    internal static readonly int SummonAt = CrimsonMeter.BarTick(CrimsonMeter.SummonBar);
    internal const float SideHalfWidth = 140, SealOffset = 470;
    // Note identities (CrimsonGesturePlan.Pulse). Ordinary notes and signature steps use 0..3; Final's second family
    // repeats an ordinary note with Pulse + PairOffset (5..8).
    internal const int BasicNotes = 4;
    internal const int Closer = BasicNotes; // the crossflow (Final: cluster) charging on beat 6, released on the next phrase's downbeat
    internal const int Pickup = 9;          // the crossflow (Final: cluster) released on this phrase's own downbeat after a gap
    internal const int PairOffset = 5;
    internal static bool IsCrossflow(int pulse) => pulse is Closer or Pickup;
    internal const int PhraseEighths = 16;
    // Ordinary cells, in eighth notes from the bar head, with the pulse each note carries. Strikes never fall on a bar
    // head, are at least a beat apart (so ordinary forecasts never overlap), the first warns only after a pickup has
    // collapsed (beat 2) and the third always strikes on beat 6 as the closing crossflow's seals bloom.
    private static readonly (int Fire, byte Pulse)[][] Cells =
    {
        new[] { (6, (byte)0), (9, (byte)1), (12, (byte)2) },  // serial % 3 == 1: beats 3, 4.5, 6
        new[] { (7, (byte)1), (10, (byte)2), (12, (byte)3) }, // serial % 3 == 2: beats 3.5, 5, 6
        new[] { (6, (byte)0), (10, (byte)2), (12, (byte)3) }, // serial % 3 == 0 (Final): beats 3, 5, 6
    };
    // Signature steps: dotted quarters from beat 3.5; the fourth, the move's final hit, lands on the next phrase's downbeat.
    private static readonly int[] SignatureSteps = { 7, 10, 13, 16 };
    internal static int SignatureClimax => SignatureSteps.Length - 1;
    internal static CrimsonPoint Conductor(RaidFieldGeometry field) => new(field.CenterX, field.CenterY);
    internal static CrimsonPoint SummoningGate(RaidFieldGeometry field) => new(field.CenterX, field.CenterY - 200);
    internal static float OpeningAge(float age, int musicStart)
        => musicStart < 0 ? -1 : age - musicStart + CrimsonInvocation.MusicLeadTicks;
    internal static float Reveal(float opening) => CrimsonInvocation.Ease((opening - 100) / 150);
    internal static float Backdrop(float opening) => CrimsonInvocation.Ease((opening - 300) / 180);
    internal static float Seal(float opening) => CrimsonInvocation.Ease((opening - 230) / 170)
        * (1 - CrimsonInvocation.Ease((opening - 720) / 180));
    // A phrase begins on the first bar head at or after earliest (a tick that rounds to a bar head belongs to that bar),
    // so music sections and phrases share boundaries. An ordinary phrase is three cell notes and the closing crossflow,
    // whose seals bloom on beat 6 and whose stream releases on the next phrase's downbeat. A signature phrase (serial % 3
    // == 0 in Acts I-III) is the Act's move in four dotted-quarter steps, the last on that downbeat in the crossflow's
    // place. A pickup is requested where no closer releases on this downbeat (an Act's first phrase, after a chorus or a
    // cycle boundary): it charges over the two beats before the bar head and releases on it. Signature phrases take none.
    internal static CrimsonRhythmPhrase Create(int earliest, int serial, int phase, bool pickup)
    {
        if (earliest < 0 || serial < 0 || phase is < 0 or > CrimsonPhaseRules.FinalPhase) throw new ArgumentOutOfRangeException();
        bool signature = CrimsonSignatureMoves.IsSignaturePhrase(phase, serial);
        pickup &= !signature;
        int bar = CrimsonMeter.BarAtOrAfter(Math.Max(0, earliest - .499999d));
        if (pickup && bar == 0) bar = 1; // the pickup's charge needs the two beats before the bar head
        int first = bar * CrimsonMeter.BeatsPerBar * 2;
        int At(int eighth) => CrimsonMeter.EighthTick(first + eighth);
        var hits = new List<CrimsonRhythmHit>(6);
        if (pickup) hits.Add(new(At(-4), At(0), At(4), 2, Pickup));
        if (signature)
            for (int step = 0; step < SignatureSteps.Length; step++)
            {
                int fire = SignatureSteps[step];
                hits.Add(new(At(fire - 2), At(fire), At(fire) + CrimsonRhythm.LiveTicks, (byte)(step == SignatureClimax ? 2 : 1), (byte)step));
            }
        else
        {
            foreach (var (fire, pulse) in Cells[(serial + 2) % 3])
                hits.Add(new(At(fire - 2), At(fire), At(fire) + CrimsonRhythm.LiveTicks, 1, pulse));
            hits.Add(new(At(12), At(16), At(20), 2, Closer));
        }
        return new(At(0), At(PhraseEighths), CrimsonRhythmKind.Groove, hits.AsReadOnly());
    }
    // When the runtime issues a booked phrase (relative to musicStart): one look-ahead before its first forecast, so
    // replication lead and the Cinder Curtain's single column observation sit 30 ticks before that forecast.
    internal static int IssueAt(int booked, bool pickup, int serial, int phase)
        => Create(booked, serial, phase, pickup).FirstWarning - CrimsonRhythm.LookAheadTicks;
    // The phrase the runtime admits at `now` (all ticks relative to musicStart). A booked phrase keeps its bar head:
    // the previous closer (or signature finale) releases on it, so the phrase starts exactly there. With nothing booked
    // (after an unlatched cycle boundary) it takes the first bar head after now and the unlock. A phrase whose first
    // forecast can no longer reach every peer one look-ahead early (a stalled or all-Down runtime) moves to the next
    // feasible bar head, and any phrase that leaves its booked bar head takes a pickup, because nothing else releases
    // on its new downbeat (signature phrases never take one).
    internal static CrimsonRhythmPhrase Admit(int booked, bool pickup, int now, int unlock, int serial, int phase)
    {
        int earliest = booked >= 0 ? Math.Max(booked, unlock) : Math.Max(Math.Max(now, unlock), 0);
        var rhythm = Create(earliest, serial, phase, pickup);
        if (booked >= 0 && rhythm.Start != booked && !pickup) rhythm = Create(rhythm.Start, serial, phase, true);
        while (rhythm.FirstWarning < now + CrimsonRhythm.LookAheadTicks)
            rhythm = Create(rhythm.Start + 1, serial, phase, true);
        return rhythm;
    }
    // Pickups and closers are the seal crossflow (Final: the cluster orb, CrimsonEnsemble); a signature phrase plays
    // the Act's move on its four steps. Final has its own pairs.
    internal static CrimsonTechnique Technique(int phase, int phrase, int note)
        => IsCrossflow(note) ? CrimsonTechnique.SideBeams
            : CrimsonSignatureMoves.IsSignaturePhrase(phase, phrase) ? CrimsonSignatureMoves.ForAct(phase)
            : phase == 2 ? CrimsonTechnique.ChoirRakes
            : phase == 1 ? CrimsonTechnique.SpatialRift : CrimsonTechnique.TrackingBeam;
    // Same capped eighteen-tick velocity lead as Doll's eight pursuit prisms.
    // One authority observation at the warning; never drag a shown forecast.
    internal static CrimsonPoint Predict(RaidFieldGeometry field, CrimsonPoint position, CrimsonPoint velocity)
        => CrimsonTechniqueGeometry.Clamp(field, position + new CrimsonPoint(
            Math.Clamp(velocity.X * 18, -220, 220), Math.Clamp(velocity.Y * 18, -160, 160)), 100);
    internal static CrimsonPoint Direction(int phrase, int note)
        => ((phrase + note) & 3) switch {
            0 => new(0, 1), 1 => new(1, 0), 2 => new(.70710678f, .70710678f), _ => new(-.70710678f, .70710678f) };
    // The stream's two ends around the captured position, right then left, SealOffset either side of a centre clamped so
    // that an end may sit on a field wall but never beyond it: the stream then still reaches a body pressed against that
    // wall. Forecast band, drawn stream and collision all run between these two points.
    internal static (CrimsonPoint Right, CrimsonPoint Left) Reach(in CrimsonGesturePlan p)
    {
        float center = Math.Clamp(p.Target.X, p.Field.Left + SealOffset, p.Field.Right - SealOffset);
        float y = Math.Clamp(p.Target.Y, p.Field.Top + SideHalfWidth + 20, p.Field.Bottom - SideHalfWidth - 20);
        return (new(center + SealOffset, y), new(center - SealOffset, y));
    }
    // The drawn seals (ScarletSorcery.CrossflowSeals): at the stream's ends, except that a seal is held SealInset inside a
    // wall, so the whole seal (radius 185 at full charge, flattened to .28, 51.8 px either side of its centre) stays inside
    // the field mask instead of being cut in half by it. The stream, drawn as a band cut square on its end, then still
    // reaches the wall, under the seal (the seals are drawn over a live stream).
    internal const float SealInset = 52;
    internal static (CrimsonPoint Right, CrimsonPoint Left) Seals(in CrimsonGesturePlan p)
    {
        var (right, left) = Reach(p);
        float low = p.Field.Left + SealInset, high = p.Field.Right - SealInset;
        return (new(Math.Clamp(right.X, low, high), right.Y), new(Math.Clamp(left.X, low, high), left.Y));
    }
    // Forecast: the band from one stream end to the other, the seal centres in the open (CrimsonEnergy draws it as a capless
    // veil). Live: one capsule that leaves the right end and grows leftward, its round ends stopping at the two ends, so it
    // never runs past either. Collision uses this capsule. The picture (ScarletInkStroke) is the band over the capsule's whole
    // span, half-width the capsule's radius, cut square on the two ends and on the growing front, as the original stream was,
    // with a seal drawn over each cut: the capsule lies inside it, and both lie inside the forecast band.
    internal static CrimsonStroke Side(in CrimsonGesturePlan p, float age, bool forecast)
    {
        var (right, left) = Reach(p);
        if (forecast) return new(right, left, SideHalfWidth);
        float width = CrimsonInvocation.Ease((age - p.Fire) / 10)
            * (1 - CrimsonInvocation.Ease((age - (p.End - 15)) / 15));
        float reach = CrimsonInvocation.Ease((age - p.Fire) / 7);
        float front = right.X - (right.X - left.X) * reach;
        float radius = Math.Min(SideHalfWidth * width, (right.X - front) * .5f);
        return new(new(right.X - radius, right.Y), new(front + radius, right.Y), radius);
    }
    internal static (float X, float Y) ClampParticipant(RaidFieldGeometry field, float x, float y, int width, int height)
        // The floor is the support tile top, not an invisible floor two pixels above it.
        => field.ClampBody(x, y, width, height, 0);
}
