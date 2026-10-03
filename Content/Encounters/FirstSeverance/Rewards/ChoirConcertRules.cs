#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Choir of the Unmade (2026-10 refresh): one concert of 18 beats at 100 BPM (one beat = 36 ticks, a sixteenth
// = 9), 648 ticks, looping while the lead voice has a target. The lead voice (the owner's chorister with the
// lowest native identity) keeps the clock in ai[0]: 0 is idle, a running concert counts 1..Cycle and wraps to
// 1, so concert tick t lands t - 1 ticks after the first tick with a target.
//   beats 0-1   count-in taps (the verse warning)          ticks 1, 36
//   beats 2-7   verse: every voice sings 6 homing notes    72..287
//   beat 8      gather: dolls to their chorus seats, the organ case opens
//   beat 9      pipes: raised ranks rise centre-out
//   beat 10     inhale: the chorus warning (harmless locked axis from the organ mouth)
//   beats 11-15 chorus: one beam from the organ mouth      396..575
//   beats 16-17 release: pipes sink, the case closes, the dolls return
// Content constants are authoritative for every hit shape; the art is fitted to them (domain art-fit tests).
// Pure: System and System.Numerics only, linked into the domain tests and the offline preview.
internal static class ChoirConcertRules
{
    // ---- Timing -------------------------------------------------------------------------------
    internal const int Beat = 36, Sixteenth = 9, Beats = 18, Cycle = Beats * Beat;
    internal const int TapA = 1, TapB = Beat;
    internal const int Verse = 2 * Beat, Gather = 8 * Beat, Pipes = 9 * Beat, Inhale = 10 * Beat;
    internal const int Fire = 11 * Beat, Release = 16 * Beat;
    internal const int VerseBeats = 6, Parts = 4, Ranks = 6, PipeColumns = 11;
    // A rank starts PipeStagger ticks after the previous one and seats PipeRiseTicks later.
    internal const int PipeStagger = 5, PipeRiseTicks = 8, PipeSinkStagger = 4;
    internal const int CaseOpenTicks = 14, CaseClose = 604, CaseCloseTicks = 12;
    internal const int SeatTicks = 24, ReturnTicks = 36;

    // ---- Chorus beam --------------------------------------------------------------------------
    internal const int WarnTicks = Fire - Inhale, LiveTicks = Release - Fire;
    internal const int CloseTicks = 6, HitCadence = 12, CloseStart = 168;
    internal const float BeamLength = 2000, BeamHalfWidth = 46, BeamStart = 48, PilotHalfWidth = 1.5f;
    internal const float TurnRate = .045f;
    // The Raid's beam ignition (FirstSeveranceBeamIgnition, mirrored and tested equal): length in 3 ticks,
    // pilot then a quintic swell to full width by tick 7.
    internal const int TravelTicks = 3, FullWidthTicks = 7;

    // ---- Damage (raw, per voice) ----------------------------------------------------------------
    internal const float NoteShare = .78f;
    internal const double ChorusShare = 1.05;
    internal const int MaxChordVoices = 6;

    // ---- Notes --------------------------------------------------------------------------------
    internal const float NoteLaunch = 8, NoteLift = 5, NoteSpeed = 30;
    internal const int NoteLife = 240, NoteHomingDelay = 6, NoteSize = 16, NoteFadeTicks = 8;

    // ---- Targeting (measured from the owner) ----------------------------------------------------
    internal const float AcquireRange = 1800, RetainRange = 2100;

    // ---- Placement (world px, y down, facing right) ---------------------------------------------
    // The stage anchor S is the organ mouth and the beam origin.
    internal static readonly Vector2 StageOffset = new(0, -210);
    internal static readonly Vector2 CloudOffset = new(0, -80);
    internal const float StageEase = .25f, StageSnap = 1200, HomeTeleport = 2400;
    // Where a new voice appears: the baton tip at the top of the downbeat.
    internal static readonly Vector2 SpawnOffset = new(20, -72);
    internal const float GlideGain = .14f, GlideResponse = .30f, GlideCap = 30;
    internal const float CloudArc = 140 * MathF.PI / 180, CloudRadius = 96, CloudRingStep = 46;

    // ---- Design anchors (the art is fitted to these) ------------------------------------------
    // Chorister, from the projectile centre (facing right): the stand tip (sprite pivot) and, per variant
    // (Chorister0/1/2 = identity % 3, the same on every client), the wide-open mouth of singing frame 2, the frame
    // on screen from the tick a note leaves it (SingFrame). The art-fit test keeps each within one dot of its art.
    internal static readonly Vector2 StandTip = new(0, 38);
    internal static readonly Vector2[] Mouths = { new(10, -12), new(11, -13), new(7, -12) };

    internal static Vector2 Mouth(int variant) => Mouths[Math.Clamp(variant, 0, Mouths.Length - 1)];
    // Organ, from S: the top-left of the 190 x 192 px case, its mouth radius (the beam throat) and the row
    // where the case top rail starts (pipes stand above it).
    internal static readonly Vector2 OrganTopLeft = new(-95, -147);
    internal const float OrganMouthRadius = 18;
    internal const int OrganRailRow = 47;

    // ---- Envelopes ------------------------------------------------------------------------------
    internal static float Clamp01(float t) => Math.Clamp(t, 0, 1);
    internal static float Arrive(float t) => 1 - MathF.Pow(1 - Clamp01(t), 4);
    internal static float Smooth(float t)
    {
        t = Clamp01(t);
        return t * t * t * (10 + t * (-15 + 6 * t));
    }

    internal static bool Running(float clock) => clock >= 1 && clock <= Cycle;

    // The next clock value of a running concert (wrapping Cycle -> 1); idle stays 0.
    internal static int Advance(int clock, bool running) => !running ? 0 : clock >= Cycle || clock < 1 ? 1 : clock + 1;

    // Dolls: cloud (0) to chorus seats (1) and back.
    internal static float Seating(float clock)
        => Arrive((clock - Gather) / SeatTicks) * (1 - Smooth((clock - Release) / ReturnTicks));

    internal static float CaseOpen(float clock)
        => Arrive((clock - Gather) / CaseOpenTicks) * (1 - Smooth((clock - CaseClose) / CaseCloseTicks));

    internal static int PipeStart(int rank) => Pipes + PipeStagger * rank;
    internal static int PipeSeat(int rank) => PipeStart(rank) + PipeRiseTicks;
    // Outer ranks sink first; the centre pipe is down by CaseClose.
    internal static int PipeSink(int rank) => Release + PipeSinkStagger * (Ranks - 1 - rank);

    internal static float PipeRise(float clock, int rank)
        => Arrive((clock - PipeStart(rank)) / PipeRiseTicks) * (1 - Smooth((clock - PipeSink(rank)) / PipeRiseTicks));

    // ---- The organ after a stop (presentation) ----------------------------------------------------
    // The case is gone at OrganGone. A concert that stops in its release keeps closing on its own clock; a chorus
    // whose target died closes quietly as a release from the state it had: the clock restarts at Release and the
    // stop clock caps every envelope, so nothing opens further. A lost target crumbles the organ instead.
    internal const int OrganGone = CaseClose + CaseCloseTicks;

    internal static float ClosingClock(int stop, float since, bool finished) => (finished ? Release : stop) + Math.Max(0, since);

    internal static float CaseOpen(float clock, float cap) => cap > 0 ? Math.Min(CaseOpen(clock), CaseOpen(cap)) : CaseOpen(clock);

    internal static float PipeRise(float clock, int rank, float cap)
        => cap > 0 ? Math.Min(PipeRise(clock, rank), PipeRise(cap, rank)) : PipeRise(clock, rank);

    // More voices, richer chord: chord lines (and chorus cue) and raised pipe ranks.
    internal static int ChordVoices(int voices) => Math.Clamp(voices, 1, MaxChordVoices);
    internal static int RaisedRanks(int voices) => Math.Clamp(voices + 1, 2, Ranks);
    internal static int PipeRank(int column) => Math.Abs(Math.Clamp(column, 0, PipeColumns - 1) - PipeColumns / 2);

    // ---- Verse ----------------------------------------------------------------------------------
    internal static int Part(int ordinal) => ordinal & (Parts - 1);
    internal static int NoteOffset(int ordinal) => Part(ordinal) * Sixteenth;
    internal static int NoteTick(int noteBeat, int ordinal) => Verse + noteBeat * Beat + NoteOffset(ordinal);

    // The verse note (0..5) a voice sings on this concert tick, or -1.
    internal static int NoteAt(int clock, int ordinal)
    {
        int t = clock - Verse - NoteOffset(ordinal);
        if (t < 0 || t % Beat != 0) return -1;
        int beat = t / Beat;
        return beat < VerseBeats ? beat : -1;
    }

    // A voice sings each verse note once per concert, whatever happens to its place. `lastSung` is the last note it
    // sang in this concert (-1 none; reset on a wrap or a stop). The due note is the latest whose tick has passed,
    // sung at most `late` ticks after it: a voice whose part moved earlier (a voice joined with a lower identity,
    // or one was sacrificed) still sings the note it was about to sing, one whose part moved later never sings a
    // note twice. NoteCatchUp covers the largest part move (three sixteenths) plus a two-tick clock step. The caller
    // caps `late` by the voice's own age, so a new voice never sings a note from before it appeared and no 600-tick
    // window holds more than one concert's notes.
    internal const int NoteCatchUp = (Parts - 1) * Sixteenth + 2;

    internal static int DueNote(int clock, int ordinal, int lastSung, int late = NoteCatchUp)
    {
        int t = clock - Verse - NoteOffset(ordinal);
        if (t < 0) return -1;
        int beat = t / Beat;
        return beat < VerseBeats && beat > lastSung && t - beat * Beat <= Math.Clamp(late, 0, NoteCatchUp) ? beat : -1;
    }

    // Sung pitches: F minor pentatonic one octave under the shared ladder (F4 Ab4 Bb4 C5 Eb5 F5 Ab5 Bb5 C6).
    internal static readonly string[] SungNames = { "F4", "Ab4", "Bb4", "C5", "Eb5", "F5", "Ab5", "Bb5", "C6" };
    internal static readonly int[] SungMidi = { 65, 68, 70, 72, 75, 77, 80, 82, 84 };

    // Verse harmony per note beat: Fm7, Fm7, Bb7sus, Bb7sus, Abmaj7 (sung as Ab C Eb), Ebsus.
    internal static readonly string[] VerseChordNames = { "Fm7", "Fm7", "Bb7sus", "Bb7sus", "Abmaj7", "Ebsus" };
    // Chord tones as pitch classes above C (F=5, Ab=8, Bb=10, C=0, Eb=3, G=7).
    internal static readonly int[][] VerseChordTones =
    {
        new[] { 5, 8, 0, 3 }, new[] { 5, 8, 0, 3 }, new[] { 10, 3, 5, 8 }, new[] { 10, 3, 5, 8 },
        new[] { 8, 0, 3, 7 }, new[] { 3, 8, 10 },
    };

    // part x note beat -> sung index. Part 0 is the melody (the lead), 1-3 roll an arpeggio a sixteenth apart.
    private static readonly int[,] lines =
    {
        { 6, 8, 7, 6, 8, 7 }, // Ab5 C6 Bb5 Ab5 C6 Bb5
        { 5, 4, 4, 5, 4, 4 }, // F5 Eb5 Eb5 F5 Eb5 Eb5
        { 3, 1, 2, 5, 3, 1 }, // C5 Ab4 Bb4 F5 C5 Ab4
        { 0, 3, 2, 4, 1, 4 }, // F4 C5 Bb4 Eb5 Ab4 Eb5
    };

    internal static int NotePitch(int noteBeat, int part) => lines[Math.Clamp(part, 0, Parts - 1), Math.Clamp(noteBeat, 0, VerseBeats - 1)];

    // Chorus harmony by live tick: 0 = Bb7sus (beats 11-12), 1 = Fm9 (13-14), 2 = the open F5 fifth (15).
    internal static int ChorusChord(float live) => live < 2 * Beat ? 0 : live < 4 * Beat ? 1 : 2;

    // ---- Singing frames -------------------------------------------------------------------------
    // 0 closed, 1 small "o" (6 ticks before a note, and the inhale), 2 wide with the head up (8 ticks after a
    // note, and the chorus).
    internal static int SingFrame(float clock, int ordinal)
    {
        if (!Running(clock)) return 0;
        if (clock >= Fire && clock < Release) return 2;
        if (clock >= Inhale && clock < Fire) return 1;
        for (int beat = 0; beat < VerseBeats; beat++)
        {
            float d = clock - NoteTick(beat, ordinal);
            if (d >= -6 && d < 0) return 1;
            if (d >= 0 && d < 8) return 2;
        }
        return 0;
    }

    // The count-in nod: every head lifts one dot (2 px) for 6 ticks on each tap.
    internal static float Nod(float clock) => clock >= TapA && clock < TapA + 6 || clock >= TapB && clock < TapB + 6 ? 2 : 0;

    // ---- Beam -----------------------------------------------------------------------------------
    internal static bool Warning(float clock) => clock >= Inhale && clock < Fire;
    internal static bool Live(float clock) => clock >= Fire && clock < Release;

    internal static float LengthFactor(float live)
    {
        float t = Clamp01(live / TravelTicks);
        return 1 - (1 - t) * (1 - t) * (1 - t);
    }

    internal static float WidthFactor(float live)
    {
        float t = Clamp01((live - 1.5f) / (FullWidthTicks - 1.5f));
        return t * t * t * (10 + t * (-15 + 6 * t));
    }

    internal static float HalfWidth(float live)
        => live < 0 || live > LiveTicks ? PilotHalfWidth
            : PilotHalfWidth + (BeamHalfWidth - PilotHalfWidth) * WidthFactor(live) * (1 - Smooth((live - CloseStart) / (LiveTicks - CloseStart)));

    internal static float End(float live) => live < 0 ? 0 : BeamLength * LengthFactor(live);

    internal static bool CanHit(float live) => live >= 0 && live < LiveTicks && End(live) > BeamStart;

    // The designed chorus ledger: one hit per NPC root every HitCadence ticks from the first hittable tick.
    internal static bool ChorusHitAt(int clock)
    {
        int live = clock - Fire;
        return CanHit(live) && (live - 1) % HitCadence == 0;
    }

    // Raw per-voice damage of one note and one chorus hit (ScaledDamage rounding; the beam truncates).
    internal static int NoteDamage(int voiceDamage) => (int)Math.Clamp(Math.Round((double)Math.Max(0, voiceDamage) * NoteShare), 1, int.MaxValue / 4);
    internal static int ChorusDamage(double voiceDamageSum) => (int)Math.Clamp(voiceDamageSum * ChorusShare, 1, int.MaxValue / 4.0);

    // Raw damage one voice lands on concert tick `clock` against one target, every hit landing.
    internal static double RawAt(int clock, int ordinal, int voiceDamage)
    {
        double raw = 0;
        if (NoteAt(clock, ordinal) >= 0) raw += NoteDamage(voiceDamage);
        if (ChorusHitAt(clock)) raw += ChorusDamage(voiceDamage);
        return raw;
    }

    // ---- Formation ------------------------------------------------------------------------------
    internal const int Row0 = 7, Row1 = 9;

    internal static int ChorusRow(int ordinal) => ordinal < Row0 ? 0 : ordinal < Row0 + Row1 ? 1 : 2;

    // Seat offset from S for voice `ordinal` of `count`: row 0 is a cup under the mouth (centre (0,-40), r 150,
    // 35-145 deg), row 1 a wider cup in front of the organ ((0,-122), r 196, 30-150 deg), row 2 the gallery over
    // the pipes ((0,-70), r 175, 200-340 deg). A row's members share its arc evenly; the first voices of a row
    // take the seats nearest its centre (ties go right), so the lead stands centre front under the mouth.
    internal static Vector2 ChorusSeat(int ordinal, int count)
    {
        count = Math.Max(1, count);
        ordinal = Math.Clamp(ordinal, 0, count - 1);
        int row = ChorusRow(ordinal), start = row == 0 ? 0 : row == 1 ? Row0 : Row0 + Row1;
        int members = row == 2 ? count - start : Math.Min(row == 0 ? Row0 : Row1, count - start);
        (Vector2 centre, float radius, float from, float to) = ChorusArc(row);
        int seat = CentreOut(ordinal - start, members, rightIsLow: row != 2);
        float angle = (from + (to - from) * (seat + .5f) / members) * MathF.PI / 180;
        return centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }

    // A chorus row's arc from S: centre, radius and seat range in degrees (y down, 90 straight below).
    internal static (Vector2 Centre, float Radius, float From, float To) ChorusArc(int row) => row switch
    {
        0 => (new Vector2(0, -40), 150f, 35f, 145f),
        1 => (new Vector2(0, -122), 196f, 30f, 150f),
        _ => (new Vector2(0, -70), 175f, 200f, 340f),
    };

    // Idle and verse seats around the owner: ring r holds 5 + 2r voices at radius 96 + 46r over the 140 deg arc
    // above the player, filled from the top centre outward; seats bob +-3 px across and +-5 px up and down.
    internal static Vector2 CloudSeat(int ordinal, int count, float time)
    {
        count = Math.Max(1, count);
        ordinal = Math.Clamp(ordinal, 0, count - 1);
        int ring = 0, start = 0;
        while (ordinal >= start + 5 + 2 * ring) { start += 5 + 2 * ring; ring++; }
        int members = Math.Min(5 + 2 * ring, count - start);
        int seat = CentreOut(ordinal - start, members, rightIsLow: false);
        float angle = 1.5f * MathF.PI + CloudArc * ((seat + .5f) / members - .5f);
        float radius = CloudRadius + CloudRingStep * ring;
        Vector2 bob = new(3 * MathF.Sin(time * .031f + ordinal * 1.7f), 5 * MathF.Sin(time * .047f + ordinal * 2.3f));
        return CloudOffset + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius + bob;
    }

    // The seat index (0..members-1 along the arc) of the j-th voice, centre first and alternating right/left
    // (right first). `rightIsLow`: seat 0 is the rightmost (a cup measured clockwise from +x).
    internal static int CentreOut(int j, int members, bool rightIsLow)
    {
        members = Math.Max(1, members);
        j = Math.Clamp(j, 0, members - 1);
        int fromRight;
        if (members % 2 == 1)
        {
            int c = (members - 1) / 2;
            fromRight = j == 0 ? c : j % 2 == 1 ? c - (j + 1) / 2 : c + j / 2;
        }
        else fromRight = j % 2 == 0 ? members / 2 - 1 - j / 2 : members / 2 + (j - 1) / 2;
        return rightIsLow ? fromRight : members - 1 - fromRight;
    }

    // Velocity steering toward a moving seat: no hard stop, at most a few px of overshoot. The projectile's own
    // position update then moves it by the returned velocity.
    internal static Vector2 GlideVelocity(Vector2 position, Vector2 velocity, Vector2 destination)
    {
        Vector2 desired = (destination - position) * GlideGain;
        float length = desired.Length();
        if (length > GlideCap) desired *= GlideCap / length;
        Vector2 next = velocity + (desired - velocity) * GlideResponse;
        return float.IsFinite(next.X) && float.IsFinite(next.Y) ? next : Vector2.Zero;
    }

    internal static void Glide(ref Vector2 position, ref Vector2 velocity, Vector2 destination)
    {
        velocity = GlideVelocity(position, velocity, destination);
        position += velocity;
    }

    // Where a voice is headed: its cloud seat around the owner, blended into its chorus seat around S.
    internal static Vector2 Destination(Vector2 ownerCenter, Vector2 stage, int ordinal, int count, float clock, float time)
        => Vector2.Lerp(ownerCenter + CloudSeat(ordinal, count, time), stage + ChorusSeat(ordinal, count), Seating(clock));

    // The stage anchor eases toward the owner's stage point and snaps when unset or far away.
    internal static Vector2 EaseStage(Vector2 stage, bool set, Vector2 ownerCenter)
    {
        Vector2 goal = ownerCenter + StageOffset;
        return !set || Vector2.DistanceSquared(stage, goal) > StageSnap * StageSnap ? goal : Vector2.Lerp(stage, goal, StageEase);
    }

    // The beam turns toward the target by at most TurnRate a tick; the first warning tick snaps.
    internal static float Turn(float current, float wanted, bool snap)
    {
        if (snap || !float.IsFinite(current)) return wanted;
        float delta = MathF.IEEERemainder(wanted - current, MathF.Tau);
        return current + Math.Clamp(delta, -TurnRate, TurnRate);
    }

    // ---- Nominal budget -------------------------------------------------------------------------
    // Raw per-voice damage over one concert, every hit landing on one target.
    internal static double ConcertRaw(int voiceDamage)
    {
        double total = 0;
        for (int clock = 1; clock <= Cycle; clock++) total += RawAt(clock, 0, voiceDamage);
        return total;
    }

    // Multiplier view of the same concert (notes x NoteShare + chorus hits x ChorusShare).
    internal static double ConcertMultiplier()
    {
        int notes = 0, hits = 0;
        for (int clock = 1; clock <= Cycle; clock++)
        {
            if (NoteAt(clock, 0) >= 0) notes++;
            if (ChorusHitAt(clock)) hits++;
        }
        return notes * NoteShare + hits * ChorusShare;
    }
}
