using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Cosmetic projection of the accepted transition; no independent phase timer.
internal static class ScarletInvocationScene
{
    private static readonly Vector2[] tether = new Vector2[33];
    internal static void Draw(SpriteBatch batch, in CrimsonState state, float age, float alpha)
    {
        if (state.Phase == 0 || Main.dedServ) return;
        float elapsed = age - state.PhaseStart;
        Vector2 center = new(state.Field.CenterX, state.Field.CenterY);
        if (state.Phase < 3)
        {
            float charge = CrimsonInvocation.Ease(elapsed / CrimsonEnsemble.ActRelease);
            float opacity = CrimsonInvocation.Ease(elapsed / 22) * (1 - CrimsonInvocation.Ease((elapsed - 106) / 44));
            var gate = CrimsonChoreography.SummoningGate(state.Field);
            center = new(gate.X, gate.Y);
            float size = 140 + 230 * CrimsonInvocation.Ease(elapsed / 42) + 35 * charge;
            ScarletSorcery.Seal(batch, center, size, .82f, -.16f, age, charge, opacity * alpha);
            ScarletSorcery.Seal(batch, center, size * .77f, .82f, .24f, -age * .6f, charge, opacity * alpha * .65f, false, 3);
            CrimsonRig.DrawPressure(batch, center, state.Phase, age, charge * opacity,
                elapsed < CrimsonEnsemble.ActRelease ? 0 : MathF.Exp(-(elapsed - CrimsonEnsemble.ActRelease) / 12));
            return;
        }
        float open = CrimsonInvocation.Ease(elapsed / 48);
        float manifest = CrimsonEnsemble.Emergence(elapsed, true);
        float absorption = CrimsonEnsemble.Absorption(elapsed);
        float alive = state.PerformerDefeated ? .16f : 1;
        ScarletSorcery.Seal(batch, center, 350 + open * 95, .92f, -.14f, age * .6f, open, alpha * alive * .75f);
        ScarletSorcery.Seal(batch, center, 312 + open * 60, .92f, .13f, -age * .45f, open, alpha * alive * .42f, false, 5);
        for (int i = 0; i < 3; i++)
        {
            var binding = CrimsonEnsemble.Binding(state.Field, i);
            Vector2 at = new(binding.X, binding.Y);
            float child = CrimsonInvocation.Ease((elapsed - 24 - i * 14) / 38);
            float retained = (state.DefeatedMask & 1 << i) == 0 ? 1 - absorption : 0;
            ScarletSorcery.Seal(batch, at, 157 + child * 20, .94f, i * MathF.Tau / 3, age, child, alpha * child * retained, false, i + 1);
            // Unequal helical tension, attached to the three fixed native roots.
            if (retained > .001f) using (var scope = new Convergence.Client.Graphics.WorldGraphicsScope(batch))
            {
                Vector2 delta = at - center, normal = new Vector2(-delta.Y, delta.X) / delta.Length();
                for (int strand = -1; strand <= 1; strand += 2)
                {
                    for (int k = 0; k < tether.Length; k++)
                    {
                        float t = k / (float)(tether.Length - 1);
                        tether[k] = Vector2.Lerp(center, at, t) + normal * MathF.Sin(t * MathF.PI)
                            * (strand * 18 + MathF.Sin(t * 13 - age * .075f + i) * (CrimsonVisuals.Reduced ? 2 : 6));
                    }
                    ScarletMaterials.Path(tether, 2.6f, 0, age, alpha * child * retained * .60f);
                }
            }
        }
        if (manifest > 0) CrimsonRig.DrawEnsemble(batch, center, age, manifest, alpha * alive);
    }
}
