using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// The accepted P2 material, shared by aimed, rotating and Final cannon casts.
internal static class FirstSeveranceCoreCannonVisuals
{
    internal static void Draw(SpriteBatch batch, FirstSeveranceAttackAccents accents, FirstSeveranceLanceRay full,
        double tick, double start, double fire, double end, bool active, bool reduced)
    {
        if (tick < start || tick >= end + 12) return;
        Vector2 origin = new(full.X, full.Y), direction = new(full.DirectionX, full.DirectionY);
        Color violet = RitualArmamentArt.ColorFor(RitualArmamentKind.Magic);
        if (tick < fire)
        {
            float pulse = FirstSeveranceEnergyPulse.Charge(tick, start, fire);
            Vector2 mouth = origin + direction * 44;
            accents.Halo(batch, mouth, new Vector2(160 + pulse * 370), violet, pulse * .66f);
            FirstSeveranceRaidVfx.Charge(batch, mouth, direction, tick - start,
                pulse, 0, Arrive(tick - start, 4), violet, reduced, .65f + pulse * 1.35f);
            FirstSeveranceRaidVfx.Orb(batch, mouth, Vector2.Zero, 12 + pulse * 62,
                tick - start, violet, pulse, true, reduced);
            FirstSeveranceBeamMaterial.SingleForecast(batch, accents, origin, direction, full.Length, full.HalfWidth,
                tick - start, Window(tick, start, fire), Arrive(tick - start, 4), violet, fire - start);
            return;
        }
        var ray = FirstSeveranceBeamIgnition.At(full, tick - fire);
        float power = active ? 1 : tick < end ? .10f : 1 - Window(tick, end, end + 12);
        FirstSeveranceBeamMaterial.Flow(batch, origin, direction, ray.Length, ray.HalfWidth,
            tick - start, violet, power, reduced, throatLength: 260, throatWidth: 20,
            fireAge: fire - start, endAge: end - start);
        float kick = ReleaseImpulse(tick, fire);
        accents.Halo(batch, origin + direction * 32, new Vector2(125 + kick * 110, 25 + kick * 35), violet,
            power * (reduced ? .22f : .55f), direction.ToRotation());
    }
}
