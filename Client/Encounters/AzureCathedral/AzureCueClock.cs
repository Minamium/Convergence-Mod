#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.AzureCathedral;

namespace Convergence.Client.Encounters.AzureCathedral;

// One cue per Fight, cue kind and identity. Unlike a "previous age to current age"
// crossing, a regressing visual age cannot play a cue twice, and a cue that is
// observed too late is recorded as played so it never fires afterwards.
// No Terraria types: linked into the domain tests.
internal sealed class AzureCueClock
{
    // Pruning horizon. Every Late window is far smaller, so a pruned tick can never become audible again.
    internal const int Horizon = 600;
    private Guid fight;
    private bool seen;
    private int high;
    private readonly Dictionary<(int Cue, int Id), int> played = new();
    private readonly HashSet<(int Cue, int Id)> latched = new();
    private readonly List<(int Cue, int Id)> stale = new();
    private int nextPrune;
    internal int High => high;
    internal int Count => played.Count + latched.Count;

    // Returns true when this observation starts a new Fight (all memory is dropped).
    internal bool Advance(Guid id, int age)
    {
        if (!seen || id != fight)
        {
            Clear();
            fight = id; seen = true; high = age;
            return true;
        }
        high = Math.Max(high, age); // never moves backwards, whatever the replicated age does
        if (played.Count > 256 && high >= nextPrune)
        {
            stale.Clear();
            foreach (var entry in played) if (entry.Value < high - Horizon) stale.Add(entry.Key);
            foreach (var key in stale) played.Remove(key);
            nextPrune = high + 60; // at most once a second, even when nothing was old enough
        }
        return false;
    }

    // Tick-based cue: becomes due once, no later than `late` ticks after its tick.
    // An old cue is remembered as played without sounding (late join, reconnect).
    internal bool Due(int cue, int id, int tick, int late)
    {
        if (tick < 0 || tick > high) return false; // future ticks are not recorded: they are awaited
        if (!played.TryAdd((cue, id), tick)) return false;
        return high - tick <= Math.Min(late, Horizon);
    }

    // State-based cue without a tick: the first observation fires once.
    internal bool Latch(int cue, int id) => latched.Add((cue, id));

    // Already-true state seen on the first frame of a Fight: remember, never sound.
    internal void Baseline(int cue, int id) => latched.Add((cue, id));

    internal void Clear()
    {
        played.Clear(); latched.Clear();
        fight = Guid.Empty; seen = false; high = 0; nextPrune = 0;
    }
}
