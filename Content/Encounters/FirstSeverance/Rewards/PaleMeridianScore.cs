#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

internal enum MeridianShot : byte { None, Note, Round, Heavy }

// Pale Meridian (2026-10 refresh, docs/encounters/first-severance/WEAPONS.md "Pale Meridian"): the held music-box
// siege rifle's score. Ages are real game ticks from the press (the holdout's first update is age 1); use speed never
// compresses them. Every gameplay number of the build, overcharge, notes and release tiers lives here; the release
// geometry is PaleMeridianLattice and the held-gun anchors PaleMeridianRig. Pure (System.Numerics only): linked into
// the domain tests and the offline preview.
internal static class PaleMeridianScore
{
    // 0..ArriveTicks: the bare gun slides into the hand. The first note fires at FirstNote.
    internal const int ArriveTicks = 12, FirstNote = 12;
    // A brass part flies for Flight ticks: an eased approach of FlightApproach ticks, then a settle.
    internal const int Flight = 16, FlightApproach = 12, PartCount = 4;
    // Parts seat on these ages, one stage (one bar of the tune) each: cylinder, shroud, ring sight, spring housing.
    internal static readonly int[] Seats = { 108, 180, 228, 264 };
    // Firing cadence of each stage (seated parts 0..4): 2.5 -> 10 rounds/s.
    internal static readonly int[] Cadence = { 24, 18, 12, 9, 6 };
    // The wind-up key rises at KeyRise over KeyRiseTicks; firing stops; WindSteps ratchet steps of 45 degrees end at
    // Ignite, where the spring lets go into overcharge.
    internal const int KeyRise = 300, KeyRiseTicks = 6, WindSteps = 12, Ignite = 348;
    internal const int WindTicks = Ignite - KeyRise;
    // Overcharge: one round every 3 ticks, a heavy round replacing one every 36, a music-box note every 9.
    internal const int OverchargeCadence = 3, HeavyPeriod = 36, OverchargeNotePeriod = 9;
    internal const float NoteFactor = .65f, RoundFactor = .62f, HeavyFactor = 1.15f;
    internal const int HeavyPierce = 3;
    // Aim turn cap (rad/tick) before and during overcharge.
    internal const float BuildTurn = .085f, OverchargeTurn = .055f;
    // After a release (or a cancel without one) the gun packs away over StowTicks; a new press is accepted after.
    internal const int StowTicks = 24;
    // Round speeds in px per game tick (rounds use one extra update, so a velocity is half of it) and the aim point:
    // build notes fly straight; overcharge rounds leave at the launch speed and home at the seek speed.
    internal const float NoteSpeed = 52, RoundLaunch = 52, HeavyLaunch = 62, RoundSeek = 44, HeavySeek = 52;
    internal const float Focus = 1300;
    internal const int RoundLife = 150;
    // Release tiers (by the held age) and the finisher factors on the weapon's ammo-modified damage.
    internal static readonly int[] TierStart = { 0, 108, 228, 348 };
    internal static readonly float[] MeridianFactors = { 0, 1.5f, 2.5f, 4 };
    internal static readonly float[] LatticeFactors = { 0, 0, 1.5f, 2 };
    internal const int NoteCount = 22, PhraseLength = 16;

    // Music-box notes, as steps of the shared ladder (DollWeaponTuning: F5 Ab5 Bb5 C6 Eb6 F6 Ab6 Bb6 C7).
    // Build: one bar per stage over Fm7, Bb7sus, Abmaj7 and Ebsus, then a rising run to C7 into the wind.
    internal static readonly int[] BuildPhrase =
    {
        0, 1, 3, 4,
        5, 4, 2, 1,
        1, 3, 4, 6,
        7, 6, 4, 2,
        3, 4, 5, 6, 7, 8,
    };
    // Overcharge: a four-bar loop restarting at Ignite, one note every 9 ticks; each bar's first note is a heavy round.
    internal static readonly int[] OverchargePhrase =
    {
        5, 3, 1, 3,
        2, 5, 4, 5,
        1, 4, 3, 4,
        2, 4, 5, 6,
    };

    internal static int Parts(float age)
    {
        int parts = 0;
        for (int i = 0; i < PartCount; i++)
            if (age >= Seats[i]) parts++;
        return parts;
    }

    internal static int Launch(int part) => Seats[Math.Clamp(part, 0, PartCount - 1)] - Flight;

    internal static int StageStart(int stage) => stage <= 0 ? FirstNote : Seats[Math.Min(stage, PartCount) - 1];

    internal static MeridianShot Shot(int age)
    {
        if (age < FirstNote || age >= KeyRise && age < Ignite) return MeridianShot.None;
        if (age >= Ignite)
        {
            int since = age - Ignite;
            if (since % OverchargeCadence != 0) return MeridianShot.None;
            return since % HeavyPeriod == 0 ? MeridianShot.Heavy : MeridianShot.Round;
        }
        int stage = Parts(age);
        return (age - StageStart(stage)) % Cadence[stage] == 0 ? MeridianShot.Note : MeridianShot.None;
    }

    internal static bool Fires(int age) => Shot(age) != MeridianShot.None;

    internal static float Factor(MeridianShot shot) => shot switch
    {
        MeridianShot.Note => NoteFactor,
        MeridianShot.Round => RoundFactor,
        MeridianShot.Heavy => HeavyFactor,
        _ => 0,
    };

    // Build notes fired at ages below `age` (0..22).
    internal static int NotesBefore(int age) => notesBefore[Math.Clamp(age, 0, KeyRise)];

    private static readonly int[] notesBefore = CountNotes();
    private static readonly int[] windTicks = FindWindTicks();

    private static int[] CountNotes()
    {
        var counts = new int[KeyRise + 1];
        for (int a = 1; a <= KeyRise; a++) counts[a] = counts[a - 1] + (Shot(a - 1) == MeridianShot.Note ? 1 : 0);
        return counts;
    }

    private static int[] FindWindTicks()
    {
        var ticks = new int[WindSteps + 1];
        for (int step = 1; step <= WindSteps; step++)
        {
            ticks[step] = Ignite;
            for (int age = KeyRise + 1; age <= Ignite; age++)
                if (WindStep(age) >= step) { ticks[step] = age; break; }
        }
        return ticks;
    }

    // The ladder step a music-box note plays at this age, or -1: every build round, and every third overcharge round.
    internal static int Note(int age)
    {
        if (age >= Ignite)
            return (age - Ignite) % OverchargeNotePeriod == 0 ? OverchargePhrase[(age - Ignite) / OverchargeNotePeriod % PhraseLength] : -1;
        return Shot(age) == MeridianShot.Note ? BuildPhrase[NotesBefore(age)] : -1;
    }

    // Ratchet steps taken by `age`: floor(12 ((age - 300) / 48)^2.5), so the steps crowd toward the release.
    internal static int WindStep(float age)
    {
        if (!(age > KeyRise)) return 0;
        if (age >= Ignite) return WindSteps;
        float t = (age - KeyRise) / WindTicks;
        return Math.Clamp((int)MathF.Floor(WindSteps * MathF.Pow(t, 2.5f) + 1e-4f), 0, WindSteps);
    }

    // First age with `step` ratchet steps (1..12): 318, 324, 328, 331, 334, 337, 339, 341, 343, 345, 347, 348.
    internal static int WindTick(int step) => windTicks[Math.Clamp(step, 1, WindSteps)];

    // The key's turn in radians, continuous: each ratchet step snaps 45 degrees over the 1.5 ticks before its tick,
    // then the released spring spins it 45 degrees every 2 ticks from Ignite (3 pi there).
    internal static float KeyAngle(float age)
    {
        if (!float.IsFinite(age) || age <= KeyRise) return 0;
        if (age >= Ignite) return 3 * MathF.PI + (age - Ignite) * MathF.PI / 8;
        float turns = 0;
        for (int step = 1; step <= WindSteps; step++)
            turns += Smooth((age - (WindTick(step) - 1.5f)) / 1.5f);
        return turns * MathF.PI / 4;
    }

    // Which of the four key frames (0 face-on, 1 45 degrees, 2 edge-on, 3 135 degrees) shows at that turn.
    internal static int KeyFrame(float angle)
        => float.IsFinite(angle) ? (int)(((long)MathF.Floor(angle / (MathF.PI / 4) + 1e-3f) % 4 + 4) % 4) : 0;

    // 0..1 as the key rises out of the housing.
    internal static float KeyRiseAmount(float age) => Arrive((age - KeyRise) / KeyRiseTicks);

    // Where each part appears, relative to its seat in the gun frame (px; +x along the aim, +y toward the gun's top):
    // ahead of and above its seat, in front of the owner, so it flies in over open ground and never across the face
    // (domain-tested at a level aim). The spring housing, seated over the owner's shoulder, comes in flat.
    private static readonly Vector2[] partStart = { new(48, 44), new(44, 40), new(46, 52), new(64, 14) };

    internal static Vector2 PartStart(int part) => partStart[Math.Clamp(part, 0, PartCount - 1)];

    // Where a flying part is, relative to its seat in the gun frame: it starts at PartStart, arrives with an ease-out
    // over 12 ticks onto a point 3 px past the seat, then settles onto it over 4 ticks. Zero once seated; position
    // and velocity are continuous.
    internal static Vector2 PartOffset(float age, int part)
    {
        part = Math.Clamp(part, 0, PartCount - 1);
        Vector2 start = partStart[part];
        float t = age - Launch(part);
        if (!(t > 0)) return start;
        Vector2 through = -Vector2.Normalize(start) * 3;
        if (t < FlightApproach) return Vector2.Lerp(start, through, Arrive(t / FlightApproach));
        return through * (1 - Smooth((t - FlightApproach) / (Flight - FlightApproach)));
    }

    internal static int Tier(int age)
    {
        int tier = 0;
        for (int i = 1; i < TierStart.Length; i++)
            if (age >= TierStart[i]) tier = i;
        return tier;
    }

    internal static float MeridianFactor(int tier) => tier is >= 0 and <= 3 ? MeridianFactors[tier] : 0;
    internal static float LatticeFactor(int tier) => tier is >= 0 and <= 3 ? LatticeFactors[tier] : 0;
    internal static bool HasFinisher(int tier) => MeridianFactor(tier) > 0;
    internal static bool HasLattice(int tier) => LatticeFactor(tier) > 0;

    // ---- Budget (raw, one target, every hit landing, per-hit rounding as in RitualArmamentRules.ScaledDamage) ----

    internal static int RoundRaw(int age, int baseDamage)
    {
        MeridianShot shot = Shot(age);
        return shot == MeridianShot.None ? 0 : RitualArmamentRules.ScaledDamage(baseDamage, Factor(shot));
    }

    internal static double BuildRaw(int baseDamage)
    {
        double total = 0;
        for (int age = 1; age < Ignite; age++) total += RoundRaw(age, baseDamage);
        return total;
    }

    // Raw damage of the rounds fired at ages [Ignite, Ignite + 36): one heavy period of overcharge.
    internal static double OverchargePeriodRaw(int baseDamage)
    {
        double total = 0;
        for (int age = Ignite; age < Ignite + HeavyPeriod; age++) total += RoundRaw(age, baseDamage);
        return total;
    }

    internal static int MeridianRaw(int tier, int baseDamage)
        => HasFinisher(tier) ? RitualArmamentRules.ScaledDamage(baseDamage, MeridianFactor(tier)) : 0;

    internal static int LatticeRaw(int tier, int baseDamage)
        => HasLattice(tier) ? RitualArmamentRules.ScaledDamage(baseDamage, LatticeFactor(tier)) : 0;

    // Raw damage of one press released at `release` (rounds up to and including that age, then the finisher).
    internal static double CycleRaw(int release, int baseDamage)
    {
        double total = 0;
        for (int age = 1; age <= release; age++) total += RoundRaw(age, baseDamage);
        int tier = Tier(release);
        return total + MeridianRaw(tier, baseDamage) + LatticeRaw(tier, baseDamage);
    }

    internal static float Arrive(float t) => 1 - MathF.Pow(1 - Math.Clamp(t, 0, 1), 4);

    internal static float Smooth(float t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * t * (10 + t * (-15 + 6 * t));
    }
}
