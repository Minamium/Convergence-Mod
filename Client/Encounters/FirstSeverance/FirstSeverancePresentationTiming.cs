using System;

namespace Convergence.Client.Encounters.FirstSeverance;

// Read-only presentation envelopes. No authority duration or player/UI flags.
internal static class FirstSeverancePresentationTiming
{
    internal const int TitleWindowTicks = 210;
    internal const int TitleFadeTicks = 24;

    internal static float TitleOpacity(double tick, ulong start, ulong end)
    {
        if (end <= start || tick < start || tick >= end) return 0;
        double reveal = Math.Max(start, (double)end - TitleWindowTicks);
        float arrival = Smooth((tick - reveal) / TitleFadeTicks);
        float release = Smooth(((double)end - tick) / TitleFadeTicks);
        return arrival * release;
    }

    internal static float CueGain(string name, float volume)
    {
        bool mechanic = name is "StackSummon" or "SpreadSummon" or "ShellMassLatch"
            or "ShellMassArc" or "ShellMassShed" or "ShellMassCollapse"
            or "MechanicTick" or "SpreadExecution" or "SpreadDissolve";
        bool beam = name is "LanceCharge" or "LanceFire" or "CurtainFire" or "BeamGather"
            or "BeamLock" or "EnergyCharge" or "GridCharge" or "GridFire" or "CoreSalvoFire"
            or "PrismBeamCharge" or "PrismBeamFire" or "FloodFire";
        return Math.Min(1, volume * (mechanic ? .65f : name == "PrismBeamSustain" ? .80f : beam ? .88f : .92f));
    }

    internal static int BeamReleaseTicks(string name)
        => name is "LanceFire" or "CurtainFire" or "EnergyCharge" or "GridFire"
            or "CoreSalvoFire" or "PrismBeamFire" or "FloodFire" ? 18 : 0;

    internal static float VoiceFade(ulong tick, ulong end, int release)
        => Math.Clamp((float)((double)end - tick) / (6 + release), 0, 1);

    private static float Smooth(double value)
    {
        float t = (float)Math.Clamp(value, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
