#nullable enable
namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// When a Doll weapon cue fires on a client. Cues are keyed to the accepted projectile age (sampled once
// per real tick, after AI), not to wall time: a peer's age can jitter by a tick, jump after a lost
// update, or resync one or two ticks backwards, and a long score can start over.
//  - A cue fires once per cue tick: `lastTick` (one slot per cue name and projectile identity, cue
//    ticks increasing within a score) remembers the last cue tick consumed.
//  - A cue whose tick passed more than `late` ticks ago is consumed silently (no late one-shots);
//    a loop that must start anyway passes a large `late`.
//  - A backward resync never refires a consumed tick; only a fresh score (the age dropping by more
//    than FreshScoreDrop between samples) re-arms the slot. Shorter restarts re-arm through Reset.
// Pure: linked into the domain tests.
internal static class DollCueClock
{
    internal const int Armed = int.MinValue;
    internal const int DefaultLate = 3;
    internal const float FreshScoreDrop = 30;

    internal static bool Take(ref int lastTick, float prevAge, float age, int cueTick, int late = DefaultLate)
    {
        if (!float.IsFinite(age)) return false;
        if (float.IsFinite(prevAge) && age < prevAge - FreshScoreDrop) lastTick = Armed;
        if (cueTick <= lastTick || age < cueTick) return false;
        lastTick = cueTick;
        return age - cueTick <= late;
    }

    internal static void Reset(ref int lastTick) => lastTick = Armed;
}
