using System;
using System.Collections.Generic;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

// Choir of the Unmade, 2026-10 refresh: the concert schedule, verse harmony, notes once per beat, budget, beam
// envelope, formation, envelopes and the organ's close after a stop, glide and the art fit, all from the pure
// ChoirConcertRules.
internal static partial class Program
{
    private static readonly int[] ChoirPentatonic = { 5, 8, 10, 0, 3 }; // F Ab Bb C Eb as pitch classes above C

    [DomainTest("Choir concert sits on the 36-tick beat grid and warns one beat before the chorus")]
    private static void ChoirConcertSchedule()
    {
        AssertEqual(648, ChoirConcertRules.Cycle, "18 beats at 100 BPM");
        AssertEqual(36, ChoirConcertRules.Beat, "one beat is 36 ticks");
        foreach (int boundary in new[] { ChoirConcertRules.TapB, ChoirConcertRules.Verse, ChoirConcertRules.Gather, ChoirConcertRules.Pipes,
                     ChoirConcertRules.Inhale, ChoirConcertRules.Fire, ChoirConcertRules.Release, ChoirConcertRules.Cycle })
            AssertEqual(0, boundary % ChoirConcertRules.Beat, $"section boundary {boundary} on the beat grid");
        AssertEqual(36, ChoirConcertRules.WarnTicks, "the warning is exactly one beat");
        AssertEqual(ChoirConcertRules.Fire, ChoirConcertRules.Inhale + ChoirConcertRules.WarnTicks, "the warning runs straight into the chorus");
        AssertEqual(180, ChoirConcertRules.LiveTicks, "a three-second chorus");
        for (int clock = ChoirConcertRules.Inhale; clock < ChoirConcertRules.Fire; clock++)
        {
            AssertEqual(true, ChoirConcertRules.Warning(clock), "warning window");
            AssertEqual(false, ChoirConcertRules.CanHit(clock - ChoirConcertRules.Fire), "the warning axis is harmless");
        }
        int live = 0;
        for (int clock = 1; clock <= ChoirConcertRules.Cycle; clock++) if (ChoirConcertRules.Live(clock)) live++;
        AssertEqual(180, live, "live chorus ticks per concert");
        // The clock: idle 0, a running concert 1..Cycle wrapping to 1 (no idle tick at the seam).
        AssertEqual(0, ChoirConcertRules.Advance(300, false), "a lost target or unusable owner returns to idle");
        AssertEqual(1, ChoirConcertRules.Advance(0, true), "a new target starts at the count-in");
        AssertEqual(301, ChoirConcertRules.Advance(300, true), "running");
        AssertEqual(1, ChoirConcertRules.Advance(ChoirConcertRules.Cycle, true), "the concert loops without an idle tick");
        AssertEqual(2, ChoirConcertRules.Nod(ChoirConcertRules.TapA), "count-in nod on the first tap");
        AssertEqual(2, ChoirConcertRules.Nod(ChoirConcertRules.TapB + 2), "count-in nod on the second tap");
        AssertEqual(0, ChoirConcertRules.Nod(ChoirConcertRules.Verse), "no nod in the verse");
    }

    [DomainTest("Choir verse: six notes per voice on sixteenths, pentatonic chord tones, every part of the ladder")]
    private static void ChoirVerseHarmony()
    {
        for (int ordinal = 0; ordinal < 40; ordinal++)
        {
            int notes = 0, previous = -1000;
            for (int clock = 1; clock <= ChoirConcertRules.Cycle; clock++)
            {
                int note = ChoirConcertRules.NoteAt(clock, ordinal);
                if (note < 0) continue;
                AssertEqual(notes, note, "notes arrive in order");
                AssertEqual(true, clock >= ChoirConcertRules.Verse && clock < ChoirConcertRules.Gather, "notes only in the verse");
                AssertEqual(0, (clock - ChoirConcertRules.Verse) % ChoirConcertRules.Sixteenth, "notes sit on sixteenths");
                AssertEqual(true, clock - previous >= ChoirConcertRules.Beat, "one voice sings at most one note a beat");
                AssertEqual(clock, ChoirConcertRules.NoteTick(note, ordinal), "note tick round trip");
                previous = clock;
                notes++;
            }
            AssertEqual(6, notes, $"voice {ordinal} sings six notes");
        }
        var used = new HashSet<int>();
        for (int beat = 0; beat < ChoirConcertRules.VerseBeats; beat++)
        for (int part = 0; part < ChoirConcertRules.Parts; part++)
        {
            int pitch = ChoirConcertRules.NotePitch(beat, part);
            used.Add(pitch);
            int pitchClass = ChoirConcertRules.SungMidi[pitch] % 12;
            AssertEqual(true, Array.IndexOf(ChoirPentatonic, pitchClass) >= 0, $"beat {beat} part {part} is F minor pentatonic");
            AssertEqual(true, Array.IndexOf(ChoirConcertRules.VerseChordTones[beat], pitchClass) >= 0,
                $"beat {beat} part {part} is a tone of {ChoirConcertRules.VerseChordNames[beat]}");
        }
        AssertEqual(9, used.Count, "the verse uses all nine sung pitches");
        AssertEqual(9, ChoirConcertRules.SungNames.Length, "nine sung note files");
        foreach (int midi in ChoirConcertRules.SungMidi)
            AssertEqual(true, Array.IndexOf(ChoirPentatonic, midi % 12) >= 0, $"sung pitch {midi} is pentatonic");
        AssertEqual(0, ChoirConcertRules.ChorusChord(0), "Bb7sus opens the chorus");
        AssertEqual(1, ChoirConcertRules.ChorusChord(72), "Fm9 on beat 13");
        AssertEqual(2, ChoirConcertRules.ChorusChord(144), "the open F5 fifth on beat 15");
    }

    [DomainTest("Choir budget stays within 3% of the 0.3.70 baseline per cycle, sustained and from a cold press")]
    private static void ChoirBudget()
    {
        int voice = RitualArmamentRules.Damage(RitualArmamentKind.Summon);
        AssertEqual(968, voice, "Damage(Summon) is frozen (the companion's 9680 depends on it)");
        int note = ChoirConcertRules.NoteDamage(voice), hit = ChoirConcertRules.ChorusDamage(voice);
        AssertEqual(RitualArmamentRules.ScaledDamage(voice, ChoirConcertRules.NoteShare), note, "notes use ScaledDamage rounding");
        AssertEqual(755, note, "a note is round(968 x .78)");
        AssertEqual(1016, hit, "a chorus hit truncates 968 x 1.05");
        int notes = 0, hits = 0;
        for (int clock = 1; clock <= ChoirConcertRules.Cycle; clock++)
        {
            if (ChoirConcertRules.NoteAt(clock, 0) >= 0) notes++;
            if (ChoirConcertRules.ChorusHitAt(clock)) hits++;
        }
        AssertEqual(6, notes, "notes per voice per concert");
        AssertEqual(15, hits, "chorus hits per concert");
        double cycle = ChoirConcertRules.ConcertRaw(voice);
        AssertDollNear(6 * 755 + 15 * 1016, cycle, 1e-9, "concert raw per voice");
        AssertDollNear(6 * .78 + 15 * 1.05, ChoirConcertRules.ConcertMultiplier(), 1e-6, "concert multiplier");
        AssertEqual(true, DollWeaponBudget.Within(cycle, DollWeaponBudget.ChoirConcert.CycleRaw),
            $"per cycle {cycle} vs {DollWeaponBudget.ChoirConcert.CycleRaw} ({DollWeaponBudget.Deviation(cycle, DollWeaponBudget.ChoirConcert.CycleRaw):P2})");
        double sustained = DollWeaponBudget.PerSecond(cycle, ChoirConcertRules.Cycle);
        AssertEqual(true, DollWeaponBudget.Within(sustained, DollWeaponBudget.ChoirConcert.PerSecond),
            $"sustained {sustained:F1}/s vs {DollWeaponBudget.ChoirConcert.PerSecond:F1}/s");
        // Cold press: the summon is tick 0, so concert tick t lands on tick t - 1. A voice summoned into a running
        // concert joins its clock: the best join phase is the cold start, one whole concert inside 600 ticks.
        double best = DollWeaponBudget.BestColdWindow(ChoirConcertRules.Cycle, (phase, tick) =>
        {
            int clock = (phase + tick) % ChoirConcertRules.Cycle + 1;
            return ChoirConcertRules.RawAt(clock, 0, voice);
        }, out int bestPhase);
        AssertEqual(0, bestPhase, "a cold start is the best window");
        AssertDollNear(cycle, best, 1e-9, "every hit of one concert lands inside the cold window");
        AssertEqual(true, DollWeaponBudget.Within(best, DollWeaponBudget.ChoirColdWindow),
            $"cold window {best} vs {DollWeaponBudget.ChoirColdWindow} ({DollWeaponBudget.Deviation(best, DollWeaponBudget.ChoirColdWindow):P2})");
        // The chorus window is unchanged: one 1016 hit per voice every 12 ticks.
        AssertEqual(DollWeaponBudget.ChoirChorus.CycleTicks, ChoirConcertRules.HitCadence, "chorus cadence");
        AssertDollNear(DollWeaponBudget.ChoirChorus.CycleRaw, hit, 1e-9, "chorus hit per voice");
        // Several voices share one beam: (int)(1.05 x the sum), never one beam per voice.
        AssertEqual(10_164, ChoirConcertRules.ChorusDamage(10 * voice), "ten voices on one beam");
        // Every ordinal is equal now (the 0.3.70 concert gave ordinals 4-7 one note fewer).
        for (int ordinal = 0; ordinal < 12; ordinal++)
        {
            double raw = 0;
            for (int clock = 1; clock <= ChoirConcertRules.Cycle; clock++) raw += ChoirConcertRules.RawAt(clock, ordinal, voice);
            AssertDollNear(cycle, raw, 1e-9, $"ordinal {ordinal} concert");
        }
    }

    [DomainTest("Choir chorus beam uses the Raid's ignition and stays inside its declared shape")]
    private static void ChoirBeamEnvelope()
    {
        for (float age = -1; age <= 12; age += .25f)
        {
            AssertDollNear(FirstSeveranceBeamIgnition.LengthFactor(age), ChoirConcertRules.LengthFactor(Math.Max(0, age)), 1e-6, $"length factor at {age}");
            AssertDollNear(FirstSeveranceBeamIgnition.WidthFactor(age), ChoirConcertRules.WidthFactor(age), 1e-6, $"width factor at {age}");
        }
        AssertDollNear(1.5, ChoirConcertRules.HalfWidth(0), 1e-6, "pilot at live 0");
        AssertDollNear(1.5, ChoirConcertRules.HalfWidth(1), 1e-6, "pilot at live 1");
        AssertDollNear(46, ChoirConcertRules.HalfWidth(7), 1e-6, "full width by live 7");
        AssertDollNear(46, ChoirConcertRules.HalfWidth(ChoirConcertRules.CloseStart), 1e-6, "full width until the close");
        AssertDollNear(1.5, ChoirConcertRules.HalfWidth(180), 1e-6, "back to the pilot at live 180");
        AssertDollNear(0, ChoirConcertRules.End(0), 1e-6, "zero length on the first live tick");
        AssertDollNear(2000, ChoirConcertRules.End(3), 1e-3, "full length in three ticks");
        AssertEqual(false, ChoirConcertRules.CanHit(0), "the first live tick cannot hit");
        AssertEqual(true, ChoirConcertRules.CanHit(1), "the second can");
        AssertEqual(false, ChoirConcertRules.CanHit(180), "nothing after the chorus");
        for (float live = -40; live <= 220; live += .5f)
        {
            AssertEqual(true, ChoirConcertRules.End(live) <= ChoirConcertRules.BeamLength + 1e-3f, "never longer than 2000 px");
            AssertEqual(true, ChoirConcertRules.HalfWidth(live) <= ChoirConcertRules.BeamHalfWidth + 1e-4f, "never wider than 92 px");
            AssertEqual(true, ChoirConcertRules.HalfWidth(live) >= ChoirConcertRules.PilotHalfWidth - 1e-4f, "never thinner than the pilot");
        }
        // Turning: the first warning tick snaps, then at most .045 rad a tick.
        AssertDollNear(2f, ChoirConcertRules.Turn(0, 2f, true), 1e-6, "first tick snaps");
        AssertDollNear(ChoirConcertRules.TurnRate, ChoirConcertRules.Turn(0, 2f, false), 1e-6, "turn cap");
        AssertDollNear(3.1f + ChoirConcertRules.TurnRate, ChoirConcertRules.Turn(3.1f, -3.1f, false), 1e-5, "turns the short way across pi");
    }

    [DomainTest("Choir formation: seats finite, unique, spaced, inside reach and clear of the player")]
    private static void ChoirFormation()
    {
        for (int count = 1; count <= 40; count++)
        {
            var seats = new Vector2[count];
            for (int ordinal = 0; ordinal < count; ordinal++)
            {
                Vector2 seat = ChoirConcertRules.ChorusSeat(ordinal, count);
                AssertEqual(true, float.IsFinite(seat.X) && float.IsFinite(seat.Y), "finite seat");
                AssertEqual(true, seat.Length() <= 420, $"seat {ordinal}/{count} within 420 px of the mouth");
                if (ChoirConcertRules.ChorusRow(ordinal) == 0) AssertEqual(true, seat.Y <= 110 + 1e-3f, "row 0 never below S + 110");
                for (int other = 0; other < ordinal; other++)
                    AssertEqual(true, Vector2.Distance(seat, seats[other]) > 1, $"unique seats {ordinal}/{other} of {count}");
                seats[ordinal] = seat;
            }
            int front = Math.Min(count, ChoirConcertRules.Row0);
            if (front % 2 == 1) AssertDollNear(new Vector2(0, 110), seats[0], 1e-3f, "the lead stands centre front under the mouth");
            for (int other = 1; other < front; other++)
                AssertEqual(true, MathF.Abs(seats[0].X) <= MathF.Abs(seats[other].X) + 1e-3f && seats[0].X >= -1e-3f,
                    $"the lead takes the front seat nearest the centre (ties right) at {count} voices");
            for (int row = 0; row < 3; row++)
            {
                float minimum = MinimumRowSpacing(seats, row);
                if (row < 2 && count <= 16) AssertEqual(true, minimum >= 40, $"row {row} spacing {minimum:F1} px at {count} voices");
                if (row == 2 && count <= 33) AssertEqual(true, minimum >= 24, $"gallery spacing {minimum:F1} px at {count} voices");
            }
            // Odd rows are mirror-symmetric: every seat has a mirror seat.
            for (int ordinal = 0; ordinal < Math.Min(count, ChoirConcertRules.Row0); ordinal++)
            {
                int members = Math.Min(count, ChoirConcertRules.Row0);
                if (members % 2 == 0) break;
                bool mirrored = false;
                for (int other = 0; other < members; other++)
                    mirrored |= Vector2.Distance(new Vector2(-seats[ordinal].X, seats[ordinal].Y), seats[other]) < .01f;
                AssertEqual(true, mirrored, $"row 0 seat {ordinal} of {count} has a mirror");
            }
            // Cloud seats: the stand tip stays at least 40 px over the head of a 42 px player (top at -21).
            if (count > 21) continue;
            for (int ordinal = 0; ordinal < count; ordinal++)
            for (float time = 0; time < 400; time += 7)
            {
                Vector2 cloud = ChoirConcertRules.CloudSeat(ordinal, count, time);
                AssertEqual(true, cloud.Y + ChoirConcertRules.StandTip.Y <= -21 - 40, $"cloud seat {ordinal}/{count} clears the head");
            }
        }
        AssertEqual(0, ChoirConcertRules.CentreOut(0, 7, true) - 3, "odd row: the first voice takes the centre");
        AssertEqual(2, ChoirConcertRules.CentreOut(1, 7, true), "odd row: the second voice goes right");
        AssertEqual(4, ChoirConcertRules.CentreOut(2, 7, true), "odd row: the third voice goes left");
        AssertEqual(1, ChoirConcertRules.CentreOut(0, 4, true), "even row: ties go right");
    }

    private static float MinimumRowSpacing(Vector2[] seats, int row)
    {
        float minimum = float.MaxValue;
        for (int a = 0; a < seats.Length; a++)
        for (int b = a + 1; b < seats.Length; b++)
            if (ChoirConcertRules.ChorusRow(a) == row && ChoirConcertRules.ChorusRow(b) == row)
                minimum = MathF.Min(minimum, Vector2.Distance(seats[a], seats[b]));
        return minimum;
    }

    [DomainTest("Choir envelopes are continuous and closed at the concert seam; pipes seat before the inhale")]
    private static void ChoirEnvelopes()
    {
        Func<float, float>[] envelopes =
        {
            ChoirConcertRules.Seating, ChoirConcertRules.CaseOpen,
            c => ChoirConcertRules.PipeRise(c, 0), c => ChoirConcertRules.PipeRise(c, 5),
        };
        string[] names = { "seating", "case", "centre pipe", "outer pipe" };
        for (int e = 0; e < envelopes.Length; e++)
        {
            AssertDollNear(0, envelopes[e](0), 1e-6, $"{names[e]} closed when idle");
            AssertDollNear(0, envelopes[e](ChoirConcertRules.Cycle), 1e-6, $"{names[e]} closed at the seam");
            float previous = envelopes[e](0);
            for (float clock = .25f; clock <= ChoirConcertRules.Cycle; clock += .25f)
            {
                float value = envelopes[e](clock);
                AssertEqual(true, value >= -1e-6f && value <= 1 + 1e-6f, $"{names[e]} in 0..1");
                AssertEqual(true, MathF.Abs(value - previous) <= .2f, $"{names[e]} continuous at {clock}");
                previous = value;
            }
        }
        for (int rank = 0; rank < ChoirConcertRules.Ranks; rank++)
        {
            AssertDollNear(1, ChoirConcertRules.PipeRise(ChoirConcertRules.Inhale - 1, rank), 1e-4, $"rank {rank} seated before the inhale");
            AssertDollNear(1, ChoirConcertRules.PipeRise(ChoirConcertRules.Release - 1, rank), 1e-4, $"rank {rank} up through the chorus");
            AssertEqual(true, ChoirConcertRules.PipeSeat(rank) < ChoirConcertRules.Inhale, $"rank {rank} seat cue before the inhale");
            AssertEqual(true, ChoirConcertRules.PipeSink(rank) + ChoirConcertRules.PipeRiseTicks <= ChoirConcertRules.CaseClose,
                $"rank {rank} sinks before the case closes");
            if (rank > 0) AssertEqual(true, ChoirConcertRules.PipeSink(rank) < ChoirConcertRules.PipeSink(rank - 1), "outer ranks sink first");
        }
        AssertDollNear(1, ChoirConcertRules.Seating(ChoirConcertRules.Inhale), 1e-4, "dolls seated by the inhale");
        AssertDollNear(1, ChoirConcertRules.CaseOpen(ChoirConcertRules.Pipes), 1e-4, "the case is open before the pipes rise");
        int previousRanks = 0;
        for (int voices = 1; voices <= 40; voices++)
        {
            int ranks = ChoirConcertRules.RaisedRanks(voices);
            AssertEqual(true, ranks >= 2 && ranks <= 6 && ranks >= previousRanks, "raised ranks grow with voices, 2..6");
            previousRanks = ranks;
            int chord = ChoirConcertRules.ChordVoices(voices);
            AssertEqual(true, chord >= 1 && chord <= 6, "chord voices 1..6");
        }
        AssertEqual(3, 2 * ChoirConcertRules.RaisedRanks(1) - 1, "one voice raises three pipes");
        AssertEqual(11, 2 * ChoirConcertRules.RaisedRanks(5) - 1, "five voices raise all eleven pipes");
        for (int column = 0; column < ChoirConcertRules.PipeColumns; column++)
            AssertEqual(Math.Abs(column - 5), ChoirConcertRules.PipeRank(column), "pipe ranks run centre-out");
    }

    [DomainTest("Choir glide settles a 240 px move by tick 17 with at most 4 px overshoot and no hard stop")]
    private static void ChoirGlide()
    {
        Vector2 position = Vector2.Zero, velocity = Vector2.Zero, destination = new(240, 0);
        float overshoot = 0, peak = 0;
        int settled = -1;
        for (int tick = 1; tick <= 80; tick++)
        {
            Vector2 before = velocity;
            ChoirConcertRules.Glide(ref position, ref velocity, destination);
            overshoot = MathF.Max(overshoot, position.X - 240);
            peak = MathF.Max(peak, velocity.Length());
            AssertEqual(true, (velocity - before).Length() <= ChoirConcertRules.GlideCap * ChoirConcertRules.GlideResponse + 1e-3f,
                "acceleration is bounded (no snap)");
            if (settled < 0 && MathF.Abs(position.X - 240) < 4) settled = tick;
        }
        AssertEqual(true, settled > 0 && settled <= 17, $"settled by tick {settled}");
        AssertEqual(true, overshoot <= 4, $"overshoot {overshoot:F2} px");
        AssertEqual(true, peak <= ChoirConcertRules.GlideCap + 1e-3f, "speed cap");
    }

    [DomainTest("Choir singing frames follow each note and the chorus")]
    private static void ChoirSingingFrames()
    {
        for (int ordinal = 0; ordinal < 8; ordinal++)
        {
            int first = ChoirConcertRules.NoteTick(0, ordinal);
            AssertEqual(1, ChoirConcertRules.SingFrame(first - 3, ordinal), "small o before a note");
            AssertEqual(2, ChoirConcertRules.SingFrame(first, ordinal), "wide on the note");
            AssertEqual(2, ChoirConcertRules.SingFrame(first + 7, ordinal), "still wide after the note");
            AssertEqual(0, ChoirConcertRules.SingFrame(first + 12, ordinal), "closed between notes");
        }
        AssertEqual(0, ChoirConcertRules.SingFrame(0, 0), "closed while idle");
        AssertEqual(1, ChoirConcertRules.SingFrame(ChoirConcertRules.Inhale + 4, 3), "the inhale");
        AssertEqual(2, ChoirConcertRules.SingFrame(ChoirConcertRules.Fire + 90, 3), "the chorus");
        AssertEqual(0, ChoirConcertRules.SingFrame(ChoirConcertRules.Release + 10, 3), "closed in the release");
    }

    [DomainTest("Choir art fits the design anchors within one dot")]
    private static void ChoirArtFit()
    {
        // Choristers (k = 2 export rung; one texel is one dot, 2 world px): the sprite pivots on the stand tip and
        // each variant's mouth on the frame on screen when a note leaves it lands within one dot of that variant's
        // design mouth, where the note spawns.
        int noteFrame = ChoirConcertRules.SingFrame(ChoirConcertRules.NoteTick(0, 0), 0);
        for (int beat = 0; beat < ChoirConcertRules.VerseBeats; beat++)
        for (int ordinal = 0; ordinal < 8; ordinal++)
            AssertEqual(noteFrame, ChoirConcertRules.SingFrame(ChoirConcertRules.NoteTick(beat, ordinal), ordinal),
                "every note leaves on the same singing frame");
        AssertEqual(2, noteFrame, "notes leave the wide-open mouth");
        (string Name, int K, int FrameWidth, int FrameHeight, Vector2 StandTip, Vector2[] Mouths)[] choristers =
        {
            ("Chorister0", DollArtAnchors.Chorister0.K, DollArtAnchors.Chorister0.FrameWidth, DollArtAnchors.Chorister0.FrameHeight,
                DollArtAnchors.Chorister0.StandTip, DollArtAnchors.Chorister0.Mouths),
            ("Chorister1", DollArtAnchors.Chorister1.K, DollArtAnchors.Chorister1.FrameWidth, DollArtAnchors.Chorister1.FrameHeight,
                DollArtAnchors.Chorister1.StandTip, DollArtAnchors.Chorister1.Mouths),
            ("Chorister2", DollArtAnchors.Chorister2.K, DollArtAnchors.Chorister2.FrameWidth, DollArtAnchors.Chorister2.FrameHeight,
                DollArtAnchors.Chorister2.StandTip, DollArtAnchors.Chorister2.Mouths),
        };
        const float dot = DollSpritePlacement.WorldPerTexel;
        AssertEqual(3, ChoirConcertRules.Mouths.Length, "one design mouth per chorister variant");
        for (int variant = 0; variant < choristers.Length; variant++)
        {
            var c = choristers[variant];
            AssertEqual(2, c.K, $"{c.Name} rung");
            AssertEqual(3, c.Mouths.Length, $"{c.Name} has three singing frames");
            Vector2 designMouth = ChoirConcertRules.Mouth(variant) - ChoirConcertRules.StandTip;
            Vector2 mouth = (c.Mouths[noteFrame] - c.StandTip) * dot;
            AssertEqual(true, Vector2.Distance(mouth, designMouth) <= dot,
                $"{c.Name} mouth on frame {noteFrame} {mouth} vs design {designMouth} ({Vector2.Distance(mouth, designMouth):F2} px, one dot {dot} px)");
            AssertDollNear(ChoirConcertRules.StandTip.Y * 2, c.FrameHeight * dot, dot,
                $"{c.Name} stands on its tip: centre to tip is half the drawn height");
        }
        // Organ (k = 1): drawn from the design top-left, its see-through mouth is the beam origin S.
        Vector2 organMouth = ChoirConcertRules.OrganTopLeft + DollArtAnchors.ChoirOrgan.Mouth * DollSpritePlacement.WorldPerTexel;
        float organTolerance = dot;
        AssertEqual(true, organMouth.Length() <= organTolerance, $"organ mouth {organMouth} px from the beam origin");
        AssertDollNear(ChoirConcertRules.OrganMouthRadius, DollArtAnchors.ChoirOrgan.MouthRadius * DollSpritePlacement.WorldPerTexel,
            organTolerance, "organ mouth radius is the beam throat");
        AssertEqual(true, ChoirConcertRules.OrganRailRow < DollArtAnchors.ChoirOrgan.Height, "the rail row lies inside the organ");
        AssertDollNear(-ChoirConcertRules.OrganTopLeft.X * 2, DollArtAnchors.ChoirOrgan.Width * DollSpritePlacement.WorldPerTexel, organTolerance,
            "the organ is centred on the mouth");
    }

    [DomainTest("Choir voices sing each verse note once per concert, whatever happens to their place")]
    private static void ChoirNotesOncePerBeat()
    {
        // Steady: the once-per-concert rule sings exactly the scheduled notes.
        for (int ordinal = 0; ordinal < 12; ordinal++)
        {
            int last = -1;
            for (int clock = 1; clock <= ChoirConcertRules.Cycle; clock++)
            {
                int due = ChoirConcertRules.DueNote(clock, ordinal, last, int.MaxValue);
                AssertEqual(ChoirConcertRules.NoteAt(clock, ordinal), due, $"ordinal {ordinal} at {clock}");
                if (due >= 0) last = due;
            }
            AssertEqual(ChoirConcertRules.VerseBeats - 1, last, "all six notes");
        }
        // A voice's place changes mid-verse (a voice joined with a lower identity, or one was sacrificed); the clock
        // also steps by two now and then. Every switch, at every tick of the verse: six notes, in order, none twice.
        int[][] moves = { new[] { 4, 3 }, new[] { 0, 1 }, new[] { 1, 0 }, new[] { 3, 4 }, new[] { 0, 3 }, new[] { 3, 0 }, new[] { 2, 5 } };
        foreach (int[] move in moves)
        for (int at = ChoirConcertRules.Verse; at < ChoirConcertRules.Gather; at++)
        foreach (int step in new[] { 1, 2 })
        {
            int last = -1, sung = 0;
            for (int clock = 1; clock <= ChoirConcertRules.Cycle; clock += clock % 7 == 0 ? step : 1)
            {
                int ordinal = clock < at ? move[0] : move[1];
                int due = ChoirConcertRules.DueNote(clock, ordinal, last, int.MaxValue);
                if (due < 0) continue;
                AssertEqual(last + 1, due, $"{move[0]}->{move[1]} at {clock}: in order, none twice or skipped");
                last = due;
                sung++;
            }
            AssertEqual(ChoirConcertRules.VerseBeats, sung, $"{move[0]}->{move[1]} at {at} (step {step}): six notes");
        }
        // A new voice never sings a note from before it appeared (its age caps the lateness).
        for (int born = 1; born < ChoirConcertRules.Gather; born++)
        for (int ordinal = 0; ordinal < 4; ordinal++)
        {
            int last = -1;
            for (int clock = born; clock < ChoirConcertRules.Gather; clock++)
            {
                int due = ChoirConcertRules.DueNote(clock, ordinal, last, clock - born);
                if (due < 0) continue;
                AssertEqual(true, ChoirConcertRules.NoteTick(due, ordinal) >= born, $"voice born at {born} sings note {due}");
                last = due;
            }
        }
        AssertEqual(29, ChoirConcertRules.NoteCatchUp, "a part moves at most three sixteenths, and a clock steps at most two ticks");
    }

    [DomainTest("Choir organ keeps closing to the case vanish after a stop and never re-opens")]
    private static void ChoirOrganClose()
    {
        AssertEqual(616, ChoirConcertRules.OrganGone, "the case is gone at 616");
        // A stop in the release: the organ closes on its own clock, exactly as if the concert had run on.
        for (int stop = ChoirConcertRules.Release; stop < ChoirConcertRules.OrganGone; stop++)
        {
            float previous = float.MaxValue;
            for (float since = 0; ; since += .5f)
            {
                float clock = ChoirConcertRules.ClosingClock(stop, since, finished: false);
                AssertDollNear(ChoirConcertRules.CaseOpen(stop + since), ChoirConcertRules.CaseOpen(clock, 0), 1e-6, "same release");
                float open = ChoirConcertRules.CaseOpen(clock, 0);
                AssertEqual(true, open <= previous + 1e-6f, $"stop {stop}: the case only closes");
                previous = open;
                if (clock >= ChoirConcertRules.OrganGone) { AssertDollNear(0, open, 1e-6, "gone"); break; }
            }
        }
        // A target that died before the release: the organ closes from the state it had, never opening further.
        for (int stop = ChoirConcertRules.Gather; stop < ChoirConcertRules.Release; stop += 3)
        {
            AssertDollNear(ChoirConcertRules.CaseOpen(stop), ChoirConcertRules.CaseOpen(ChoirConcertRules.ClosingClock(stop, 0, true), stop), 1e-6,
                $"stop {stop}: the case continues from where it stood");
            for (int rank = 0; rank < ChoirConcertRules.Ranks; rank++)
            {
                float previous = ChoirConcertRules.PipeRise(stop, rank);
                AssertDollNear(previous, ChoirConcertRules.PipeRise(ChoirConcertRules.ClosingClock(stop, 0, true), rank, stop), 1e-6,
                    $"stop {stop} rank {rank}: the pipe continues from where it stood");
                for (float since = .5f; since <= ChoirConcertRules.OrganGone - ChoirConcertRules.Release; since += .5f)
                {
                    float rise = ChoirConcertRules.PipeRise(ChoirConcertRules.ClosingClock(stop, since, true), rank, stop);
                    AssertEqual(true, rise <= previous + 1e-6f, $"stop {stop} rank {rank}: the pipe only sinks");
                    previous = rise;
                }
                AssertDollNear(0, previous, 1e-6, "every pipe down");
            }
            AssertDollNear(0, ChoirConcertRules.CaseOpen(ChoirConcertRules.ClosingClock(stop, ChoirConcertRules.OrganGone - ChoirConcertRules.Release, true), stop),
                1e-6, $"stop {stop}: the case is gone after the release's 40 ticks");
        }
    }
}
