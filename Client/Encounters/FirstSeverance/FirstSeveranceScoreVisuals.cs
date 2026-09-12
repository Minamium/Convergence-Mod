#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

internal sealed class FirstSeveranceScoreVisuals
{
    private Asset<Texture2D>? blade;
    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    internal void Unload() => blade = null; // Content manager owns the borrowed PNG.

    internal void Draw(SpriteBatch batch, FirstSeveranceCombatProjection combat, double tick,
        ulong authorityTick, FirstSeveranceEmissionVisuals emissions, bool reduced)
    {
        var accents = emissions.Accents;
        if (Main.dedServ || tick < combat.ActionStartedTick || authorityTick >= combat.ResolveTick) return;
        double age = tick - combat.ActionStartedTick;
        if (combat.Substate == FirstSeveranceSubstate.FinalSlicer)
        {
            FirstSeveranceFinalBeamVisuals.Draw(batch, emissions, combat, tick, authorityTick, reduced);
            return;
        }
        if (combat.Substate == FirstSeveranceSubstate.RemoteClaws)
        {
            DrawFlood(batch, accents, combat, age, reduced);
            return;
        }
        if (combat.Substate == FirstSeveranceSubstate.HalfField)
        {
            blade ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/VFX/SeveranceBlade");
            FirstSeveranceImpalingSwordVisuals.Draw(batch, accents, blade.Value, combat, age,
                Math.Max(0, (double)authorityTick - combat.ActionStartedTick), reduced);
            return;
        }
        var rays = FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY);
        var authority = FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex,
            Math.Max(0, (double)authorityTick - combat.ActionStartedTick), combat.CoreX, combat.CoreY);
        for (int index = 0; index < rays.Count; index++)
        {
            var item = rays[index]; var ray = item.Ray;
            bool live = index < authority.Count && authority[index].Live && authority[index].Pulse == item.Pulse;
            Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY), normal = new(-direction.Y, direction.X);
            Vector2 end = origin + direction * ray.Length;
            bool crush = combat.Substate == FirstSeveranceSubstate.RemoteCrush;
            Color color = crush ? new(255, 96, 163)
                : combat.Substate == FirstSeveranceSubstate.RemoteClaws ? new(107, 230, 238)
                : new(194, 146, 255);
            float born = Window(age, 0, 12);
            float fade = crush ? 1 - Window(age, 180, 240) : 1;
            if (crush)
            {
                FirstSeveranceBeamMaterial.CrushMembrane(batch, accents, (origin + end) * .5f,
                    ray.Length * .5f, ray.HalfWidth, age, item.Charge, live ? 1 : 0,
                    born * fade, color, reduced);
                continue;
            }
            if (combat.Substate == FirstSeveranceSubstate.RotatingBlade)
            {
                DrawBlade(batch, accents, origin, item, age, live, reduced);
                continue;
            }
            float power = live ? .92f : (.20f + item.Charge * .30f) * born * fade;
            FirstSeveranceBeamMaterial.Draw(batch, accents, origin, direction, ray.Length, ray.HalfWidth,
                age, item.Charge, live ? 1 : 0, power, color, reduced);
            accents.Halo(batch, origin, new Vector2(100 + item.Charge * 90), color, power);
        }
        if (combat.Substate == FirstSeveranceSubstate.FinalBullets)
            DrawBullets(batch, accents, combat, age, reduced);
    }

    private void DrawBlade(SpriteBatch batch, FirstSeveranceAttackAccents accents, Vector2 origin,
        FirstSeveranceScoreRay item, double age, bool live, bool reduced)
    {
        blade ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/VFX/SeveranceBlade");
        Texture2D texture = blade.Value;
        int bladeIndex = item.Pulse % FirstSeveranceScoreGeometry.BladeCount;
        float angle = FirstSeveranceScoreGeometry.BladeAngle(age) + bladeIndex * MathHelper.Pi;
        Vector2 direction = Unit(angle), normal = new(-direction.Y, direction.X);
        Color color = bladeIndex == 0 ? new(162, 213, 255) : new(217, 166, 255);
        float extension = FirstSeveranceScoreGeometry.BladeExtension(age);
        float retract = 1 - Window(age, FirstSeveranceChoreography.BladeEnd, FirstSeveranceChoreography.BladeEnd + 58);
        float born = Window(age, 0, 12) * retract;
        // Full footprint is visible throughout the held windup, independent of the short draw animation.
        accents.Ribbon(batch, origin, direction, item.Ray.Length, 88, color, born * (live ? .90f : .44f));
        accents.Ribbon(batch, origin, direction, item.Ray.Length, 54, Color.White, born * (live ? .40f : .14f));
        // Flowing surface filaments stay inside the damage aura, not rigid side rails.
        for (int strand = 0; strand < (reduced ? 2 : 5); strand++)
        {
            Vector2 prior = origin;
            for (int segment = 1; segment <= 24; segment++)
            {
                float p = segment / 24f;
                Vector2 next = origin + direction * (p * item.Ray.Length) + normal
                    * MathF.Sin(p * 13 - (float)age * .09f + strand * 1.4f) * (10 + strand * 5) * MathF.Sin(p * MathF.PI);
                Line(batch, prior, next, FirstSeveranceAttackAccents.Neon(color, born * (live ? .60f : .28f)), live ? 2.4f : 1.3f);
                prior = next;
            }
        }
        if (extension > .001f)
        {
            // Detailed material never changes topology. Trailing samples follow the same
            // integrated authority angle; the full-opacity leading blade is always current.
            if (live && !reduced)
                for (int echo = 4; echo >= 1; echo--)
                    Sprite(FirstSeveranceScoreGeometry.BladeAngle(Math.Max(180, age - echo * 1.2)) + bladeIndex * MathHelper.Pi,
                        color * (.055f * (5 - echo)), 1);
            Sprite(angle, Color.White * retract, extension);
            float drawFlash = Window(age, 143, 148) * (1 - Window(age, 156, 174));
            accents.Ribbon(batch, origin, direction, item.Ray.Length * extension, 125, color, drawFlash * .70f);
            accents.Halo(batch, origin + direction * item.Ray.Length * extension,
                new Vector2(190, 120), color, drawFlash * .8f, angle);
        }
        accents.ChargeFracture(batch, origin, age, 0, 180, color, reduced, 1.1f);
        void Sprite(float rotation, Color tint, float lengthScale)
        {
            Vector2 dir = Unit(rotation);
            float visible = FirstSeveranceScoreGeometry.ToEdge(dir.X, dir.Y);
            // A rigid 1400 px blade, clipped at the field plane instead of stretching
            // its material to the rectangle's changing radius as it turns.
            float scaleX = 1400 * lengthScale / 2131;
            int width = Math.Clamp((int)(25 + visible / scaleX), 26, texture.Width);
            batch.Draw(texture, origin - Main.screenPosition, new Rectangle(0, 0, width, texture.Height), tint, rotation,
                new Vector2(25, 362), new Vector2(scaleX, .24f), SpriteEffects.None, 0);
        }
    }

    private static void DrawBullets(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        FirstSeveranceCombatProjection combat, double age, bool reduced)
    {
        foreach (var b in FirstSeveranceScoreGeometry.Bullets(combat.ActionIndex, age, combat.CoreX, combat.CoreY))
        {
            Vector2 point = new(b.X, b.Y), previous = new(b.PreviousX, b.PreviousY);
            Vector2 velocity = point - previous;
            Vector2 direction = velocity.LengthSquared() > .01f ? Vector2.Normalize(velocity) : Vector2.UnitY;
            Vector2 normal = new(-direction.Y, direction.X);
            Color color = b.Wave % 2 == 0 ? new(100, 238, 255) : new(236, 132, 255);
            if (b.Live)
            {
                if (!reduced)
                {
                    accents.Ribbon(batch, point - velocity * 8, direction, velocity.Length() * 8, 30, color, .28f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 prior = point;
                        for (int segment = 1; segment <= 7; segment++)
                        {
                            float t = segment / 7f;
                            Vector2 next = point - velocity * (t * 7)
                                + normal * side * MathF.Sin(t * MathF.PI) * (9 + 3 * MathF.Sin((float)age * .18f + b.Wave));
                            Line(batch, prior, next, FirstSeveranceAttackAccents.Neon(color, (1 - t) * .8f), 2.3f);
                            prior = next;
                        }
                    }
                }
                accents.Halo(batch, point, new Vector2(68, 50), color, reduced ? .35f : .72f, direction.ToRotation());
                // Solid pointed projectile material, not a circular HUD outline.
                Line(batch, point - direction * 12, point + direction * 12, new Color(19, 11, 30), 24);
                Line(batch, point - direction * 10, point + direction * 10, color, 18);
                // An ivory lancet within the exact dark-outlined collision core.
                Line(batch, point - direction * 8, point + direction * 10, Color.White, 5);
                Line(batch, point - normal * 5, point + direction * 10, Color.White * .9f, 2);
                Line(batch, point + normal * 5, point + direction * 10, Color.White * .9f, 2);
            }
            else
            {
                double local = age - FirstSeveranceScoreGeometry.BulletStartTick(combat.ActionIndex, b.Wave);
                float charge = CastTension(local, -24, 0);
                accents.Halo(batch, point, new Vector2(65), color, charge * .55f);
                // Two halves of the live lancet assemble around its actual spawn
                // position, brake, then meet before release; no extra fake bullets.
                float join = Window(local, -5, 0);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 tip = point + normal * (side * (1 - join) * (34 - charge * 16));
                    Line(batch, tip - direction * 13, tip + direction * 8,
                        FirstSeveranceAttackAccents.Neon(color, .45f + charge * .5f), 2);
                }
            }
        }
    }

    private static void DrawFlood(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        FirstSeveranceCombatProjection combat, double age, bool reduced)
    {
        int pulse = (int)age / FirstSeveranceScoreGeometry.FloodInterval;
        double local = age - pulse * FirstSeveranceScoreGeometry.FloodInterval;
        if (pulse >= 3 || local >= FirstSeveranceScoreGeometry.FloodFadeTick) return;
        float born = .75f + .25f * Window(local, 0, 6), charge = Window(local, 0, FirstSeveranceScoreGeometry.FloodFireTick);
        float growth = FirstSeveranceScoreGeometry.FloodGrowth(local);
        float emission = Window(local, FirstSeveranceScoreGeometry.FloodFireTick, FirstSeveranceScoreGeometry.FloodFireTick + 8)
            * (1 - Window(local, FirstSeveranceScoreGeometry.FloodEndTick, FirstSeveranceScoreGeometry.FloodFadeTick));
        Color color = pulse % 2 == 0 ? new(99, 235, 255) : new(206, 155, 255);
        var rays = FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY);
        for (int band = 0; band < rays.Count; band++)
        {
            var full = FirstSeveranceScoreGeometry.FloodBand(combat.ActionIndex, pulse, band, combat.CoreX, combat.CoreY);
            var ray = rays[band].Ray;
            Vector2 origin = new(full.X, full.Y), direction = new(full.DirectionX, full.DirectionY);
            // Foretell the final occupied volume and the genuine surviving pocket.
            // The bright layer then advances and widens using the exact authority curve.
            FirstSeveranceHazardSurface.Draw(batch, accents, origin, direction, full.Length, full.HalfWidth,
                local, charge, 0, born * (1 - growth) * .90f, color, reduced);
            if (local < FirstSeveranceScoreGeometry.FloodFireTick)
            {
                accents.Ribbon(batch, origin, direction, full.Length, 24, color, born * (.40f + charge * .30f));
                Line(batch, origin, origin + direction * full.Length, Color.White * born * (.25f + charge * .4f), 2.5f);
            }
            else
            {
                FirstSeveranceHazardSurface.Draw(batch, accents, origin, direction, ray.Length, ray.HalfWidth,
                    local, 1, emission, rays[band].Live ? 1 : emission * .12f, color, reduced);

                // Keep the bright moving front inside the same volume; a round
                // end-halo used to wash over the safe strip and obscure its edge.
                float front = Math.Min(36, ray.Length);
                accents.Ribbon(batch, origin + direction * (ray.Length - front), direction,
                    front, ray.HalfWidth * 2, Color.White, emission * .65f);
            }
            accents.ChargeFracture(batch, origin + direction * 24, local, 0, FirstSeveranceScoreGeometry.FloodFireTick,
                color, reduced, .75f + charge * .28f);
        }
        // The negative space itself denotes safety; no floating arrow labels.
    }
}
