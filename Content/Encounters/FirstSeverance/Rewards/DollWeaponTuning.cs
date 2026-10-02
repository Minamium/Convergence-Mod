#nullable enable
using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Tonal home of every Doll reward weapon cue: F minor pentatonic (F Ab Bb C Eb), measured as the
// safest five-note set over the Doll BGM's source frame (DistantLiturgy's F Dorian vamp). Pure: no
// Terraria types; linked into the domain tests and mirrored by tools/doll_sfx_dsp.py (LADDER), which
// renders the tonal cues at their recorded roots.
//
// A single-note cue recorded at one ladder step may be played at another through SoundStyle.Pitch,
// an octave fraction (-1 = one octave down, 1 = one octave up). Composite cues and loops are never
// transposed, and no per-phase BGM correction is applied: the default is 0 cents everywhere.
internal static class DollWeaponTuning
{
    // Semitones above F of the five scale degrees F Ab Bb C Eb.
    internal static readonly int[] Pentatonic = { 0, 3, 5, 7, 10 };
    // The shared ladder, F5 (698.46 Hz) to C7 (2093.00 Hz).
    internal static readonly string[] LadderNames = { "F5", "Ab5", "Bb5", "C6", "Eb6", "F6", "Ab6", "Bb6", "C7" };
    internal const int LadderLength = 9;
    internal const int RootMidi = 77; // F5
    internal const float DefaultCents = 0;
    // SoundStyle.Pitch spans one octave each way.
    internal const int MaxShiftSemitones = 12;

    internal static int Step(int index) => Math.Clamp(index, 0, LadderLength - 1);

    // Semitones of a ladder step above F5 (0, 3, 5, 7, 10, 12, 15, 17, 19).
    internal static int Semitones(int step)
    {
        step = Step(step);
        return Pentatonic[step % Pentatonic.Length] + 12 * (step / Pentatonic.Length);
    }

    internal static int Midi(int step) => RootMidi + Semitones(step);

    // Equal temperament, A4 = 440 Hz.
    internal static double Frequency(int step) => 440.0 * Math.Pow(2, (Midi(step) - 69) / 12.0);

    internal static bool InScale(int midi) => Array.IndexOf(Pentatonic, ((midi - 65) % 12 + 12) % 12) >= 0;

    // Offset in cents that plays a cue recorded at ladder step `root` at ladder step `step`.
    internal static float Cents(int step, int root, float cents = DefaultCents) => (Semitones(step) - Semitones(root)) * 100 + cents;

    internal static bool Reachable(int step, int root) => Math.Abs(Semitones(step) - Semitones(root)) <= MaxShiftSemitones;

    // SoundStyle.Pitch for that offset, clamped to the playable octave either way.
    internal static float Pitch(int step, int root, float cents = DefaultCents) => Math.Clamp(Cents(step, root, cents) / 1200f, -1f, 1f);
}
