using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceImpalingSword(FirstSeveranceLanceRay FullRay,
    int Wave, int Slot, int Fire, int Retract, float Charge, float Extension, float Fade, bool Live);

// Fixed action seed, never local randomness/targets. Shared warning, insertion
// and collision geometry; no projectile or world-resource allocation.
internal static class FirstSeveranceImpalingSwords
{
    internal const int Duration = 300, DenseCount = 20, SparseCount = 8, Count = DenseCount + SparseCount;
    internal const int InsertionTicks = FirstSeveranceBeamIgnition.TravelTicks;
    private static readonly int[] Stagger = { 0, 7, 3, 12, 5, 16 };
    internal static int FireBase(int wave) => wave == 0 ? 108 : 214;
    internal static int WarningStart(int wave) => wave == 0 ? 0 : 174;

    internal static FirstSeveranceLanceRay BeamAt(in FirstSeveranceImpalingSword sword, double age)
    {
        var ray = FirstSeveranceBeamIgnition.At(sword.FullRay, age - sword.Fire);
        return ray with { Length = sword.FullRay.Length * sword.Extension };
    }

    internal static IReadOnlyList<FirstSeveranceImpalingSword> At(int step, double age, float groundX, float groundY)
    {
        var blades = new List<FirstSeveranceImpalingSword>(Count);
        if (age < 0 || age >= Duration) return blades;
        int wave = age < WarningStart(1) ? 0 : 1;
        int fireBase = FireBase(wave), retract = fireBase + 50;
        if (age >= retract + 28) return blades;
        bool denseLeft = step < 4;
        float denseStart = denseLeft ? -1280 : 0, sparseStart = denseLeft ? 0 : -1280;
        for (int slot = 0; slot < Count; slot++)
        {
            bool dense = slot < DenseCount;
            int lane = dense ? slot : slot - DenseCount;
            // 64px pitch / 68px aura fully closes one half. The other half's
            // 160px pitch / 104px aura leaves narrow irregular clear slots.
            float jitter = dense ? 0 : ((lane * 7 + wave * 5 + step) % 5 - 2) * 4;
            float x = dense ? denseStart + 32 + lane * 64
                : sparseStart + 80 + lane * 160 + jitter + (wave == 0 ? -16 : 64);
            int fire = fireBase + Stagger[(lane * 5 + step + wave * 3) % Stagger.Length];
            bool fromTop = (lane * 3 + wave + step) % 4 < 2;
            var ray = new FirstSeveranceLanceRay(groundX + x, fromTop ? groundY - 1120 : groundY,
                0, fromTop ? 1 : -1, 1120, dense ? 34 : 52);
            float extension = FirstSeveranceBeamIgnition.LengthFactor(age - fire)
                * (1 - FirstSeveranceScoreGeometry.Smooth((float)((age - retract) / 20)));
            float fade = 1 - FirstSeveranceScoreGeometry.Smooth((float)((age - retract - 20) / 8));
            float charge = FirstSeveranceScoreGeometry.Smooth((float)((age - WarningStart(wave)) / (fire - WarningStart(wave))));
            blades.Add(new(ray, wave, slot, fire, retract, charge, extension, fade,
                age >= fire && age < retract && extension > 0));
        }
        return blades;
    }
}
