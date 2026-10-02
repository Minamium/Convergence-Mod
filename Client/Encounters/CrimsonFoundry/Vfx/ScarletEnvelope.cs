#nullable enable
using System;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// The named attack curves every Scarlet body reads: the approved motion, the body material, Vespera's command
// and the Choir's arms. The only inputs are a note's Born, Fire and Span (the length of its decay after Fire):
// p = clamp((age - Born) / max(1, Fire - Born)), t = age - Fire. No beat grid, music clock or free-running
// phase enters here, so every body answers its own attack and nothing else.
//
// FNA- and Terraria-free (System only): the domain tests link this file.
internal static class ScarletEnvelope
{
    // Ignite shares ScarletInk's ignite constant, so the body and the river light on the same tick.
    internal const float IgniteRate = .21f;
    internal const float WhipTicks = 5;

    internal static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static float Out(float x) { x = 1 - Math.Clamp(x, 0, 1); return 1 - x * x * x; }

    // Warning progress (same arithmetic as the accepted Choir arm).
    internal static float Progress(float age, float born, float fire)
    {
        float warning = Math.Max(1, fire - born);
        return Math.Clamp((age - born) / warning, 0, 1);
    }

    // Approved: rapid intake -> braked tension -> settle exactly on Fire (Prepare(1) = 1).
    internal static float Prepare(float p) => .80f * Out(p / .38f) + .12f * Ease((p - .38f) / .43f) + .08f * Ease((p - .81f) / .19f);
    // Approved four-tick unfurl.
    internal static float Release(float t) => t <= 0 ? 0 : Out(t / 4f);
    // Approved recovery, finished before the note's window closes (span = Close - Fire).
    internal static float Decay(float t, float span) => t <= 0 ? 1 : 1 - Ease((t - 6) / Math.Max(7, span - 6));
    // Approved Choir slam pulse.
    internal static float Burst(float t, float decay) => t <= 0 ? 0 : Out(t / 1.8f) * MathF.Exp(-t / 11f) * decay;
    // Approved Mantle whip: five ticks, fast enough to land on the strike, slow enough to read as a swing.
    internal static float Whip(float t) => t <= 0 ? 0 : Ease(t / WhipTicks);

    // Heat gathered by the warning, cooling after the strike (continuous at Fire: Prepare(1) = 1).
    internal static float Heat(float p, float t) => t < 0 ? MathF.Pow(Prepare(p), 1.5f) : MathF.Pow(2, -t / 6);
    // The body lights on the strike tick, as the ink does.
    internal static float Ignite(float t) => t < 0 ? 0 : MathF.Pow(2, -IgniteRate * t);
    // A front that runs through the body as fast as the move reaches across the field (-1 = none yet).
    internal static float Front(float t, float reach) => t < 0 ? -1 : Ease(t / Math.Max(.001f, reach));
    // The light is swallowed after the flash, then given back before the window closes.
    internal static float Drain(float t, float span) => t < 0 ? 0 : Ease(t / 5) * (1 - Ease((t - 8) / Math.Max(6, span - 8)));
    // Blood sent from the heart to the striking extremity; exactly 1 on Fire.
    internal static float Send(float p) => Ease((p - .45f) / .55f);
    // Blood returning to the heart, 0 when the window closes.
    internal static float Return(float t, float span) => t < 0 ? 0 : 1 - Ease((t - 4) / Math.Max(8, span - 4));
    // Vespera's release: out in three ticks, half-life six.
    internal static float Snap(float t) => t < 0 ? 0 : Out(t / 3) * MathF.Pow(2, -t / 6);
    // A Choir sleeve tears open in two ticks and closes by fourteen.
    internal static float Tear(float t) => t < 0 ? 0 : Out(t / 2) * (1 - Ease((t - 4) / 10));

    // The seal crossflow swells with its seals and is spent while the band flows (the band's own width envelope).
    internal static float CrossflowHeat(float p, float t) => t < 0 ? MathF.Pow(p, 1.3f) : MathF.Pow(2, -t / 6);
    internal static float Spend(float age, float fire, float end)
        => age < fire ? 0 : Ease((age - fire) / 6) * (1 - Ease((age - (end - 15)) / 15));

    // Inside a note's window [Born, Fire + span), eased in and out so a free idle oscillation handed to the attack
    // clock never pops at the window edges.
    internal static float Engaged(float age, float born, float fire, float span)
        => age < born || age >= fire + span ? 0 : Ease((age - born) / 6) * (1 - Ease((age - (fire + span - 8)) / 8));

    // The approved prototype's envelope (motion.js `envelope`): Wind and Follow are zero outside the window.
    internal static ScarletPhase Phase(float age, float born, float fire, float span)
    {
        float p = Progress(age, born, fire), t = age - fire;
        float prepare = Prepare(p), release = Release(t), decay = Decay(t, span);
        bool live = age >= born && age < fire + span;
        return new(p, t, live, prepare, decay, live ? prepare * (1 - release) : 0, live ? release * decay : 0);
    }
}

internal readonly record struct ScarletPhase(float P, float T, bool Live, float Prepare, float Decay, float Wind, float Follow);
