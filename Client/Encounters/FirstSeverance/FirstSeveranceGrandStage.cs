using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Non-interactive scene punctuation. Everything is sampled from the accepted
// combat clock; late snapshots seek the scene instead of replaying flashes.
internal static class FirstSeveranceGrandStage
{
    private static readonly Vector2 axis = FirstSeveranceDissolutionVisuals.RiftAxis;
    private static readonly Color porcelain = new(203, 181, 237);

    internal static void BehindBody(SpriteBatch batch, FirstSeveranceCombatProjection combat,
        Vector2 center, double tick, bool reduced)
    {
        double age = tick - combat.ActionStartedTick;
        if (combat.Substate == FirstSeveranceSubstate.SpawnIntro)
        {
            float t = FirstSeveranceDollCapture.IntroAge(tick, combat.ActionStartedTick, combat.ResolveTick);
            float pull = Window(t, .30, .85), fade = 1 - Window(t, .89, 1);
            FirstSeveranceRaidVfx.Pressure(batch, center, axis, 900, 400, age,
                pull, pull * fade * (reduced ? .2f : .8f), porcelain, reduced);
            if (!reduced) FirstSeveranceRaidVfx.Sparks(batch, center, axis, age,
                pull, fade, porcelain, true, 2.1f);
            FirstSeveranceRaidVfx.Flare(batch, center, age, FirstSeveranceDollCapture.Arrival(t),
                porcelain, reduced, 3.7f);
        }
        else if (combat.Substate == FirstSeveranceSubstate.PhaseTransition)
        {
            float t = FirstSeveranceStageVisuals.RuptureAge(combat, tick);
            float pry = Window(t, .04, .24), held = 1 - Window(t, .42, .73);
            float release = Window(t, .36, .39) * (1 - Window(t, .39, .60));
            bool departing = combat.BossPhase is FirstSeveranceBossPhase.Distant or FirstSeveranceBossPhase.Final;
            Vector2 point = center - new Vector2(0, departing ? Window(t, .08, .64) * 190 : 0);
            FirstSeveranceRaidVfx.Rift(batch, point, axis, 1120 * pry, 160 + pry * 120,
                age, pry, held * (reduced ? .35f : .85f), porcelain, reduced);
            FirstSeveranceRaidVfx.Pressure(batch, point, axis, 1400, 530, age, pry,
                pry * held * .65f, porcelain, reduced);
            FirstSeveranceRaidVfx.Flare(batch, point, age, release, porcelain, reduced, 4.6f);
            if (!reduced) FirstSeveranceRaidVfx.Sparks(batch, point, axis, age,
                pry, held, porcelain, t < .39, 3.1f);
        }
        else if (combat.BossPhase == FirstSeveranceBossPhase.Final && !reduced)
        {
            float escalation = FirstSeveranceChoreography.FinalProgress(combat.ActionIndex);
            FirstSeveranceRaidVfx.Pressure(batch, center, axis, 740, 240, tick,
                escalation, .12f + escalation * .3f, porcelain, false);
            FirstSeveranceRaidVfx.Sparks(batch, center, axis, tick, escalation,
                .18f + escalation * .35f, porcelain, true, 1.8f);
        }
        // Explicit ordering: the pressure field is behind the authored shell/rig,
        // not a late bright overlay that erases the girl's silhouette.
        FirstSeveranceRaidVfx.Flush(batch);
    }

    internal static void EndingFront(SpriteBatch batch, Vector2 center, float age, bool victory, bool reduced)
    {
        float clock = age * (victory ? 210 : 180);
        if (victory)
        {
            float held = Window(age, .06, .29) * (1 - Window(age, .785, .81));
            float plunge = Window(age, .29, .79);
            // A long strained hold, then an accelerating in-fall. Noise flows
            // across every filament instead of making opaque radial spokes.
            if (!reduced)
                for (int stream = 0; stream < 3; stream++)
                    FirstSeveranceRaidVfx.Sparks(batch, center, axis.RotatedBy(stream * .73),
                        clock * (1 + plunge * 2.4f) + stream * 19, 1,
                        held * (.45f + plunge * .4f), porcelain, true, 1.5f + stream * 1.05f);
            FirstSeveranceRaidVfx.Pressure(batch, center, axis, 1200, 460, clock,
                plunge, held * (reduced ? .25f : 1), porcelain, reduced);
            float snap = Window(age, .785, .798) * (1 - Window(age, .798, .89));
            FirstSeveranceRaidVfx.Flare(batch, center, clock, snap, Color.White, reduced, 7);
            if (!reduced && snap > 0)
                FirstSeveranceRaidVfx.Sparks(batch, center, axis, (age - .798) * 360,
                    1, snap, porcelain, false, 4);
        }
        else
        {
            float shut = Window(age, .04, .48), fade = 1 - Window(age, .46, .72);
            Color ember = new(212, 68, 103);
            FirstSeveranceRaidVfx.Pressure(batch, center, Vector2.UnitX, 1600 * (1 - shut) + 40,
                630, clock, shut, fade * (reduced ? .3f : .9f), ember, reduced);
            FirstSeveranceRaidVfx.Rift(batch, center, Vector2.UnitY, 1200, 310 * (1 - shut) + 5,
                clock, 1, fade, ember, reduced);
            float snap = Window(age, .43, .46) * (1 - Window(age, .46, .59));
            FirstSeveranceRaidVfx.Flare(batch, center, clock, snap * .65f, ember, reduced, 3);
        }
    }
}
