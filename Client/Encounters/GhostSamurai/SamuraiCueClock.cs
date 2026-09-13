using System;
using System.Collections.Generic;

namespace Convergence.Client.Encounters.GhostSamurai;

// One cue per fight/kind/authority tick, even when thirty grid entities release.
// Only a four-tick catch-up is allowed; a late join never replays prior attacks.
internal sealed class SamuraiCueClock
{
    private Guid fight;
    private int age, firstAge;
    private readonly HashSet<(int Kind, int Tick)> played = new();
    internal void Advance(Guid currentFight, int currentAge)
    {
        if (fight != currentFight) { Clear(); fight = currentFight; age = firstAge = currentAge; }
        age = Math.Max(age, currentAge);
        int oldest = age - 4;
        played.RemoveWhere(cue => cue.Tick < oldest);
    }
    internal bool Try(int kind, int at) => at >= firstAge && at <= age && age - at <= 4 && played.Add((kind, at));
    internal int Count => played.Count;
    internal void Clear() { played.Clear(); fight = Guid.Empty; age = firstAge = 0; }
}

