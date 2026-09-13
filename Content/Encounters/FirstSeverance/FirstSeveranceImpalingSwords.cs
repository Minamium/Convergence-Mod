using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceImpalingSword(FirstSeveranceLanceRay FullRay,
    int Wave, int Slot, int Fire, int Retract, float Charge, float Extension, float Fade, bool Live);

// Fixed action seed, never local randomness/targets. Shared warning, insertion
// and collision geometry; no projectile or world-resource allocation.
internal static class FirstSeveranceImpalingSwords
{
    internal const int Duration = 300, DenseCount = 9, SparseCount = 7, Count = DenseCount + SparseCount + 2;
    internal const float HalfWidth = 52, DenseMinimumGap = 26, SparseMinimumGap = 62;
    internal const int InsertionTicks = FirstSeveranceBeamIgnition.TravelTicks;
    private static readonly int[] Stagger = { 0, 7, 3, 12, 5, 16 };
    internal static int FireBase(int wave) => wave == 0 ? 108 : 214;
    internal static int WarningStart(int wave) => wave == 0 ? 0 : 174;

    internal static FirstSeveranceLanceRay BeamAt(in FirstSeveranceImpalingSword sword, double age)
    {
        var ray = FirstSeveranceBeamIgnition.At(sword.FullRay, age - sword.Fire);
        return ray with { Length = sword.FullRay.Length * sword.Extension };
    }

    internal static IReadOnlyList<FirstSeveranceImpalingSword> At(int step, double age, float groundX, float groundY,
        ulong actionSeed = 0)
    {
        var blades = new List<FirstSeveranceImpalingSword>(Count);
        if (age < 0 || age >= Duration) return blades;
        int wave = age < WarningStart(1) ? 0 : 1;
        int fireBase = FireBase(wave), retract = fireBase + 50;
        if (age >= retract + 28) return blades;
        bool denseLeft = step < 4;
        float denseStart = denseLeft ? -1280 : 0, sparseStart = denseLeft ? 0 : -1280;
        Span<float> positions = stackalloc float[DenseCount];
        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            bool dense = sideIndex == 0;
            int count = dense ? DenseCount : SparseCount;
            Positions(positions[..count], dense ? DenseMinimumGap : SparseMinimumGap, actionSeed, step, wave, sideIndex);
            // Wrap edge fragments too; walls never create a large extra sanctuary.
            for (int lane = -1; lane <= count; lane++)
            {
                float localX = lane < 0 ? positions[count - 1] - 1280
                    : lane == count ? positions[0] + 1280 : positions[lane];
                if (localX < -HalfWidth || localX > 1280 + HalfWidth) continue;
                float x = (dense ? denseStart : sparseStart) + localX;
                int ordinal = (lane + count) % count;
                int fire = fireBase + Stagger[(ordinal * 5 + step + wave * 3) % Stagger.Length];
                bool fromTop = (ordinal * 3 + wave + step) % 4 < 2;
                var ray = new FirstSeveranceLanceRay(groundX + x, fromTop ? groundY - 1120 : groundY,
                    0, fromTop ? 1 : -1, 1120, HalfWidth);
                float extension = FirstSeveranceBeamIgnition.LengthFactor(age - fire)
                    * (1 - FirstSeveranceScoreGeometry.Smooth((float)((age - retract) / 20)));
                float fade = 1 - FirstSeveranceScoreGeometry.Smooth((float)((age - retract - 20) / 8));
                float charge = FirstSeveranceScoreGeometry.Smooth((float)((age - WarningStart(wave)) / (fire - WarningStart(wave))));
                blades.Add(new(ray, wave, blades.Count, fire, retract, charge, extension, fade,
                    age >= fire && age < retract && extension > 0));
            }
        }
        return blades;
    }

    private static void Positions(Span<float> positions, float minimumGap, ulong actionSeed, int step, int wave, int side)
    {
        // Accepted action-start tick is the seed. Xorshift is stable across
        // framework versions and all peers; draw never advances an RNG.
        uint seed = unchecked((uint)actionSeed ^ (uint)(actionSeed >> 32)
            ^ (uint)(step + 1) * 747796405u ^ (uint)side * 277803737u) | 1;
        Span<float> weights = stackalloc float[DenseCount + 1];
        float total = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            weights[i] = .5f + (seed % 1000) / 1000f;
            total += weights[i];
        }
        float free = 1280 - positions.Length * (HalfWidth * 2 + minimumGap);
        float cursor = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            float gap = minimumGap + free * weights[i] / total;
            // Every first-wave gap is narrower than a beam; the second strike
            // occupies its center. Standing still in an old gap must fail.
            positions[i] = cursor + (wave == 0 ? 0 : HalfWidth + gap * .5f);
            cursor += HalfWidth * 2 + gap;
        }
    }
}
