using System;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Pure presentation curves shared with the offline sequence/contract checks.
internal static class ScarletGesturePresentation
{
    internal static float WarningOpacity(in CrimsonGesturePlan p, float age)
    {
        float arrive = CrimsonInvocation.Ease((age - p.Born) / 6);
        float release = age < p.Fire ? 1 : 1 - CrimsonInvocation.Ease((age - p.Fire) / 8);
        return .9f * arrive * release;
    }
    internal static float LiveOpacity(in CrimsonGesturePlan p, float age)
        => CrimsonInvocation.Ease((age - p.Fire) / 3)
            * (1 - CrimsonInvocation.Ease((age - p.End) / CrimsonRhythm.ResidueTicks));
    internal static float SampleAge(in CrimsonGesturePlan p, float age)
    {
        if (age >= p.End && p.Technique is CrimsonTechnique.ChoirHook or CrimsonTechnique.ChoirThrust or CrimsonTechnique.MantleScissors)
            return p.Fire + (p.End - p.Fire - 1) * (1 - CrimsonInvocation.Ease((age - p.End) / CrimsonRhythm.ResidueTicks));
        return Math.Min(age, p.End - .001f);
    }
    // Luminance 1.0.14 indexes N-2 trail segments, retaining the last point for
    // tangent support. Append one support point, then remap its unused UV span.
    internal static float TrailCompletionScale(int submittedPoints)
        => (submittedPoints - 1f) / Math.Max(1, submittedPoints - 2);
}
