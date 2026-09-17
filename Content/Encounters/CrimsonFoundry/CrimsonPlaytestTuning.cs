using System;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Temporary rehearsal tuning: does not touch player weapons or other raids.
internal static class CrimsonPlaytestTuning
{
    internal const int AttackDamage = 1;
    internal const int SoloTargetLife = 750000;
    internal const int ExtraPlayerTargetLife = 500000;
    internal static int TargetLife(int members)
        => SoloTargetLife + ExtraPlayerTargetLife * (Math.Clamp(members, 1, 8) - 1);
}

// Admission is not completion. Wait for the last recovery and any chorus.
internal sealed class CrimsonActCycle
{
    internal const int PhrasesPerCycle = 12;
    internal int Issued { get; private set; }
    internal int Completed { get; private set; }
    internal int FinishAt { get; private set; } = -1;
    internal bool Full => Issued == PhrasesPerCycle;
    internal void Admit(int phraseEnd, int lastRecoveryEnd)
    {
        if (Full || phraseEnd < 0 || lastRecoveryEnd < 0)
            throw new InvalidOperationException("scarlet.cycle_admission_invalid");
        Issued++;
        FinishAt = Math.Max(FinishAt, Math.Max(phraseEnd, lastRecoveryEnd));
    }
    internal bool TryComplete(int now, bool chorusInFlight)
    {
        if (!Full || now < FinishAt || chorusInFlight) return false;
        Completed++; Issued = 0; FinishAt = -1;
        return true;
    }
    internal void Reset() { Issued = Completed = 0; FinishAt = -1; }
}
