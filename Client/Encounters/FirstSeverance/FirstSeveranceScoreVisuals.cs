#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

internal sealed class FirstSeveranceScoreVisuals
{
    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    internal void Unload() { } // Shared material resources have their existing owner.

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
            FirstSeveranceImpalingSwordVisuals.Draw(batch, accents, combat, age,
                Math.Max(0, (double)authorityTick - combat.ActionStartedTick), reduced);
            return;
        }
        var rays = FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY);
        var authority = FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex,
            Math.Max(0, (double)authorityTick - combat.ActionStartedTick), combat.CoreX, combat.CoreY);
        for (int index = 0; index < rays.Count; index++)
        {
            var item = rays[index]; var ray = item.BeamRay;
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
        Vector2 direction=new(item.Ray.DirectionX,item.Ray.DirectionY);
        var ray=item.BeamRay;
        Color color=RitualArmamentArt.ColorFor(Convergence.Content.Encounters.FirstSeverance.Rewards.RitualArmamentKind.Magic);
        float retract=1-Window(age,FirstSeveranceChoreography.BladeEnd,FirstSeveranceChoreography.BladeEnd+58);
        bool warning=age<FirstSeveranceChoreography.BladeWindup;
        float opacity=warning?Arrive(age,4):live?1:retract;
        FirstSeveranceRaidVfx.Beam(batch,origin,direction,ray.Length,ray.HalfWidth,age,
            item.Charge,warning?0:1,opacity,color,reduced,release:ReleaseImpulse(age,FirstSeveranceChoreography.BladeWindup),
            fireAge:FirstSeveranceChoreography.BladeWindup,endAge:FirstSeveranceChoreography.BladeEnd);
        accents.ChargeFracture(batch,origin,age,0,FirstSeveranceChoreography.BladeWindup,color,reduced,1.2f);
    }

    private static void DrawBullets(SpriteBatch batch,FirstSeveranceAttackAccents accents,
        FirstSeveranceCombatProjection combat,double age,bool reduced)
    {
        foreach(var b in FirstSeveranceScoreGeometry.Bullets(combat.ActionIndex,age,combat.CoreX,combat.CoreY)) {
            Vector2 point=new(b.X,b.Y),velocity=point-new Vector2(b.PreviousX,b.PreviousY);
            double local=age-FirstSeveranceScoreGeometry.BulletStartTick(combat.ActionIndex,b.Wave);
            float opacity=b.Live?1:Arrive(local+24,5);
            FirstSeveranceRaidVfx.Orb(batch,point,velocity,FirstSeveranceScoreGeometry.BulletRadius,
                age+b.Wave*7,new Color(188,76,255),opacity,b.Live,reduced);
        }
    }

    private static void DrawFlood(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        FirstSeveranceCombatProjection combat, double age, bool reduced)
    {
        int pulse = (int)age / FirstSeveranceScoreGeometry.FloodInterval;
        double local = age - pulse * FirstSeveranceScoreGeometry.FloodInterval;
        if (pulse >= 3 || local >= FirstSeveranceScoreGeometry.FloodFadeTick) return;
        float born = .75f + .25f * Window(local, 0, 6), charge = Window(local, 0, FirstSeveranceScoreGeometry.FloodFireTick);
        float emission = Window(local, FirstSeveranceScoreGeometry.FloodFireTick, FirstSeveranceScoreGeometry.FloodFireTick + 8)
            * (1 - Window(local, FirstSeveranceScoreGeometry.FloodEndTick, FirstSeveranceScoreGeometry.FloodFadeTick));
        Color color = pulse % 2 == 0 ? new(99, 235, 255) : new(206, 155, 255);
        var rays = FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY);
        for (int band = 0; band < rays.Count; band++)
        {
            var full = FirstSeveranceScoreGeometry.FloodBand(combat.ActionIndex, pulse, band, combat.CoreX, combat.CoreY);
            var ray = rays[band].Ray;
            Vector2 origin = new(full.X, full.Y), direction = new(full.DirectionX, full.DirectionY);
            if (local < FirstSeveranceScoreGeometry.FloodFireTick)
            {
                FirstSeveranceHazardSurface.Draw(batch, accents, origin, direction, full.Length, full.HalfWidth,
                    local, charge, 0, born, color, reduced,FirstSeveranceScoreGeometry.FloodFireTick);
            }
            else
            {
                FirstSeveranceHazardSurface.Draw(batch, accents, origin, direction, ray.Length, ray.HalfWidth,
                    local, 1, 1, rays[band].Live ? 1 : emission, color, reduced,
                    FirstSeveranceScoreGeometry.FloodFireTick,FirstSeveranceScoreGeometry.FloodEndTick);
            }
            accents.ChargeFracture(batch, origin + direction * 24, local, 0, FirstSeveranceScoreGeometry.FloodFireTick,
                color, reduced, .75f + charge * .28f);
        }
        // The negative space itself denotes safety; no floating arrow labels.
    }
}
