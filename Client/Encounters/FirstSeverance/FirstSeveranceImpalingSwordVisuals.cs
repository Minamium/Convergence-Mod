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
    internal static void Draw(SpriteBatch batch, FirstSeveranceAttackAccents accents, Texture2D texture,
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
            Color color = sword.Slot < FirstSeveranceImpalingSwords.DenseCount ? new(205, 142, 244) : new(134, 220, 255);
            bool warning = age < sword.Fire;
            float arrival = 1 - MathF.Pow(1 - Math.Clamp((float)(age - FirstSeveranceImpalingSwords.WarningStart(sword.Wave)) / 5, 0, 1), 3);
            float brake = Window(age, sword.Fire - 16, sword.Fire - 7);
            // Held aura forecasts the whole blade volume. A rigid textured sword
            // then translates through the field plane; it never scales in length.
            float length = warning ? ray.Length : ray.Length * sword.Extension;
            accents.Ribbon(batch, origin, direction, length, ray.HalfWidth * 2, color,
                born * (live ? .85f : warning ? .48f : .18f));
            for (int strand = 0; strand < (reduced ? 2 : 4); strand++)
            {
                Vector2 last = origin;
                for (int n = 1; n <= 18; n++)
                {
                    float t = n / 18f;
                    Vector2 next = origin + direction * (t * length) + normal *
                        (MathF.Sin(t * 16 - (float)age * .15f + strand * 1.9f + sword.Slot) * ray.HalfWidth * .58f * MathF.Sin(t * MathF.PI));
                    Line(batch, last, next, FirstSeveranceAttackAccents.Neon(color, born * (live ? .65f : .35f)), live ? 2.5f : 1.5f);
                    last = next;
                }
            }
            if (sword.Extension > .001f)
            {
                float scaleX = ray.Length / 2131;
                int skip = Math.Clamp((int)(25 + (ray.Length - length) / scaleX), 25, texture.Width - 1);
                int count = Math.Min(texture.Width - skip, Math.Max(1, (int)(length / scaleX)));
                batch.Draw(texture, origin - Main.screenPosition, new Rectangle(skip, 0, count, texture.Height),
                    Color.White * sword.Fade, angle, new Vector2(0, 362),
                    new Vector2(scaleX, ray.HalfWidth * 2 / 300), SpriteEffects.None, 0);
                float stab = Window(age, sword.Fire, sword.Fire + 2) * (1 - Window(age, sword.Fire + 6, sword.Fire + 18));
                Vector2 tip = origin + direction * length;
                accents.Halo(batch, tip, new Vector2(90, 10), Color.White, stab * (reduced ? .2f : .75f), angle + MathF.PI * .5f);
            }
            else if (warning)
            {
                // Harmless tip OUTSIDE the arena: snap from the slit, settle into
                // tension, then the existing shared six-tick insertion takes over.
                float scaleX = ray.Length / 2131;
                Vector2 point = origin - direction * (2 + (1 - arrival) * 140 + brake * 14);
                int tipWidth = Math.Min(420, texture.Width);
                batch.Draw(texture, point - Main.screenPosition,
                    new Rectangle(texture.Width - tipWidth, 0, tipWidth, texture.Height),
                    Color.White * (born * .45f), angle, new Vector2(tipWidth, 362),
                    new Vector2(scaleX, ray.HalfWidth * 2 / 300), SpriteEffects.None, 0);
            }
            // Edge rifts gather/close continuously; no hard forecast rails.
            accents.CastSeal(batch, origin + direction * 12, age,
                FirstSeveranceImpalingSwords.WarningStart(sword.Wave), sword.Fire, color, reduced, .35f);
            float rift = born * (1 - Window(age, sword.Retract, sword.Retract + 24));
            accents.Halo(batch, origin, new Vector2(32 - brake * 21, ray.HalfWidth * (2.5f + brake)), color,
                rift * (.45f + brake * .4f), angle);
        }
    }
}
