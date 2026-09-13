using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

internal static class FirstSeveranceImpalingSwordVisuals
{
    internal static void Draw(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        FirstSeveranceCombatProjection combat, double age, double authorityAge, bool reduced)
    {
        if (Main.dedServ) return;
        foreach (var sword in FirstSeveranceImpalingSwords.At(combat.ActionIndex, age, combat.CoreX, combat.CoreY))
        {
            var ray = sword.FullRay;
            Vector2 origin = new(ray.X, ray.Y), direction = new(0, ray.DirectionY), normal = new(-direction.Y, 0);
            bool live = sword.Live && authorityAge > sword.Fire && authorityAge < sword.Retract;
            float angle = direction.ToRotation(), born = Window(age, FirstSeveranceImpalingSwords.WarningStart(sword.Wave),
                FirstSeveranceImpalingSwords.WarningStart(sword.Wave) + 9) * sword.Fade;
            Color color = RitualArmamentArt.ColorFor(Convergence.Content.Encounters.FirstSeverance.Rewards.RitualArmamentKind.Magic);
            bool warning = age < sword.Fire;
            float arrival = Arrive(age - FirstSeveranceImpalingSwords.WarningStart(sword.Wave), 5);
            float brake = Window(age, sword.Fire - 16, sword.Fire - 7);
            float commit = Window(age, sword.Fire - 5, sword.Fire);
            var body = FirstSeveranceImpalingSwords.BeamAt(sword, age);
            float length = body.Length;
            if(warning) FirstSeveranceRaidVfx.Beam(batch,origin,direction,ray.Length,ray.HalfWidth,
                age,CastTension(age,FirstSeveranceImpalingSwords.WarningStart(sword.Wave),sword.Fire),
                0,born,color,reduced,confined:true,mouth:false,fireAge:sword.Fire,endAge:sword.Retract);
            if (sword.Extension > .001f)
            {
                // Both length and width follow the shared ignition geometry.
                FirstSeveranceBeamMaterial.Flow(batch, origin, direction, length, body.HalfWidth,
                    age, color, sword.Fade * (live || age >= sword.Retract ? 1 : .10f), reduced,
                    fireAge:sword.Fire,endAge:sword.Retract);
                float stab = Window(age, sword.Fire, sword.Fire + 2) * (1 - Window(age, sword.Fire + 6, sword.Fire + 18));
                accents.Halo(batch, origin + direction * length, new Vector2(75, 12),
                    Color.White, stab * (reduced ? .18f : .65f), angle + MathF.PI * .5f);
            }
            else if (warning)
            {
                // Pressure draws back into the entry slit without introducing a
                // metal tip. The thin locked axis above stays put.
                Vector2 point = origin - direction * ((2 + (1 - arrival) * 140 + brake * 14) * (1 - commit));
                accents.Halo(batch, point, new Vector2(18 + commit * 12, ray.HalfWidth * 1.4f),
                    color, born * (.28f + commit * .42f), angle);
            }
            // Edge rifts gather/close continuously; no hard forecast rails.
            accents.ChargeFracture(batch, origin + direction * 12, age,
                FirstSeveranceImpalingSwords.WarningStart(sword.Wave), sword.Fire, color, reduced, .35f);
            float rift = born * (1 - Window(age, sword.Retract, sword.Retract + 24));
            accents.Halo(batch, origin, new Vector2(32 - brake * 21, ray.HalfWidth * (2.5f + brake)), color,
                rift * (.45f + brake * .4f), angle);
        }
    }
}
