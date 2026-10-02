using System;
using System.Linq;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Doll weapon ladder is F minor pentatonic from F5 to C7, strictly rising")]
    private static void DollWeaponLadderScale()
    {
        AssertEqual(DollWeaponTuning.LadderLength, DollWeaponTuning.LadderNames.Length, "one name per ladder step");
        AssertEqual("0,3,5,7,10,12,15,17,19",
            string.Join(",", Enumerable.Range(0, DollWeaponTuning.LadderLength).Select(DollWeaponTuning.Semitones)),
            "semitones above F5: F Ab Bb C Eb, then the octave");
        for (int step = 0; step < DollWeaponTuning.LadderLength; step++)
        {
            AssertTrue(DollWeaponTuning.InScale(DollWeaponTuning.Midi(step)), $"step {step} is a pitch class of F Ab Bb C Eb");
            if (step > 0)
                AssertTrue(DollWeaponTuning.Semitones(step) > DollWeaponTuning.Semitones(step - 1), $"step {step} rises");
        }
        foreach (int outside in new[] { 66, 67, 69, 71, 73, 74, 76 }) // F# G A B C# D E
            AssertEqual(false, DollWeaponTuning.InScale(outside), $"MIDI {outside} is outside the scale");
        AssertEqual(true, DollWeaponTuning.InScale(65 - 24), "pitch classes hold in low octaves too");
        AssertEqual(77, DollWeaponTuning.Midi(0), "the ladder starts on F5");
        AssertEqual(96, DollWeaponTuning.Midi(8), "and ends on C7");
    }

    [DomainTest("Doll weapon ladder names and frequencies agree (A4 = 440 Hz)")]
    private static void DollWeaponLadderFrequencies()
    {
        string[] letters = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
        for (int step = 0; step < DollWeaponTuning.LadderLength; step++)
        {
            int midi = DollWeaponTuning.Midi(step);
            AssertEqual(letters[midi % 12] + (midi / 12 - 1), DollWeaponTuning.LadderNames[step], $"name of step {step}");
        }
        AssertTrue(Math.Abs(DollWeaponTuning.Frequency(0) - 698.4565) < .001, "F5 = 698.46 Hz");
        AssertTrue(Math.Abs(DollWeaponTuning.Frequency(3) - 1046.5023) < .001, "C6 = 1046.50 Hz");
        AssertTrue(Math.Abs(DollWeaponTuning.Frequency(8) - 2093.0045) < .001, "C7 = 2093.00 Hz");
        AssertEqual(DollWeaponTuning.Frequency(0), DollWeaponTuning.Frequency(-4), "a step below the ladder clamps to F5");
        AssertEqual(DollWeaponTuning.Frequency(8), DollWeaponTuning.Frequency(40), "a step above the ladder clamps to C7");
    }

    [DomainTest("Doll weapon pitch offsets are relative to the recorded root, 0 cents by default")]
    private static void DollWeaponPitchOffsets()
    {
        AssertEqual(0f, DollWeaponTuning.DefaultCents, "no BGM-phase correction by default");
        for (int root = 0; root < DollWeaponTuning.LadderLength; root++)
            AssertEqual(0f, DollWeaponTuning.Pitch(root, root), $"a cue played at its own root is unshifted ({root})");
        AssertEqual(700f, DollWeaponTuning.Cents(3, 0), "C6 from an F5 recording is +700 cents");
        AssertEqual(-300f, DollWeaponTuning.Cents(0, 1), "F5 from an Ab5 recording is -300 cents");
        AssertTrue(Math.Abs(DollWeaponTuning.Pitch(5, 0) - 1f) < 1e-6, "F6 from F5 is exactly one octave up");
        AssertTrue(Math.Abs(DollWeaponTuning.Pitch(3, 0) - 7 / 12f) < 1e-6, "SoundStyle.Pitch is an octave fraction");
        AssertTrue(Math.Abs(DollWeaponTuning.Pitch(0, 0, 50) - 50 / 1200f) < 1e-6, "an explicit cents offset adds on top");
        AssertEqual(1f, DollWeaponTuning.Pitch(8, 0), "beyond an octave the pitch clamps");
        AssertEqual(false, DollWeaponTuning.Reachable(8, 0), "and C7 is not reachable from an F5 recording");
        AssertEqual(true, DollWeaponTuning.Reachable(8, 3), "but is from C6");
        for (int root = 0; root < DollWeaponTuning.LadderLength; root++)
        for (int step = 0; step < DollWeaponTuning.LadderLength; step++)
        {
            float pitch = DollWeaponTuning.Pitch(step, root);
            AssertTrue(pitch >= -1f && pitch <= 1f, $"pitch {step}<-{root} stays in SoundStyle's range");
            if (DollWeaponTuning.Reachable(step, root))
                AssertTrue(Math.Abs(pitch * 12 - (DollWeaponTuning.Semitones(step) - DollWeaponTuning.Semitones(root))) < 1e-4,
                    $"reachable pitch {step}<-{root} is exact");
        }
    }
}
