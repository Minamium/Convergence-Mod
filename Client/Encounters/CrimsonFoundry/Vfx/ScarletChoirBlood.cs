#nullable enable
using System;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// The blood one Choir arm carries for its strikes, read from that arm's own cues (the plans' Born/Fire and the
// note window), so two consecutive notes sharing an arm each send their own front: while the earlier strike's
// blood is still returning, the later warning already sends the next one.
//   Send    warning front, heart (0) -> fingertip (1), exactly 1 on Fire; 0 = no pending strike on this arm
//   Return  the latest strike's blood running back to the heart (1 -> 0 over the window); 0 = none
//   Flash   the latest strike's ignition on this arm (ScarletInk's constant); halved under Reduced Effects
//   Fill    the arm's blood after the strike, draining away in six ticks
//   Gain    how strongly the warning sends (the body's Heat, so a basic phrase's register .6 sends less)
// FNA- and Terraria-free (System only), a pure function of (age, cues): no beat grid or free-running clock.
internal readonly record struct ScarletChoirBlood(float Send, float Return, float Flash, float Fill, float Gain)
{
    // Where the hand begins on the heart -> shoulder -> elbow -> wrist -> fingertip path: the fingertips ignite.
    internal const float HandFrom = .78f;

    internal static ScarletChoirBlood Of(ReadOnlySpan<CrimsonChoirCue> cues, int arm, float age, float heat, bool reduced)
    {
        float send = 0, back = 0, since = float.MaxValue;
        foreach (var cue in cues)
        {
            if ((!cue.Broad && cue.Arm != arm) || age < cue.Born || age >= cue.End) continue;
            float t = age - cue.Fire;
            if (t < 0) send = Math.Max(send, ScarletEnvelope.Send(ScarletEnvelope.Progress(age, cue.Born, cue.Fire)));
            else
            {
                back = Math.Max(back, ScarletEnvelope.Return(t, cue.End - cue.Fire));
                since = Math.Min(since, t);
            }
        }
        if (since == float.MaxValue) return new(send, back, 0, 0, Gain(heat));
        float flash = ScarletEnvelope.Ignite(since) * (reduced ? ScarletBodyMaterial.ReducedIgnite : 1);
        return new(send, back, flash, 1 - ScarletEnvelope.Ease(since / 6), Gain(heat));

        static float Gain(float heat) => Math.Clamp(.3f + .8f * heat, 0, 1);
    }

    // The front at u (0 heart .. 1 fingertip along the arm's rest skeleton): a bright bead with the sent blood
    // behind it, the dimmer (.4) returning bead, and the fingertips' ignition. 0..1, written to vertex alpha.
    internal float At(float u)
    {
        float a = 0;
        if (Send > 0) a = Gain * Math.Max(Bead(u - Send, 9), Math.Clamp((Send - u) / .2f, 0, 1) * .35f);
        if (Return > 0) a = Math.Max(a, Math.Max(Bead(u - Return, 7) * .4f, Math.Clamp((Return - u) / .2f, 0, 1) * .35f * Fill));
        if (Flash > 0) a = Math.Max(a, Flash * ScarletEnvelope.Ease((u - HandFrom) / (1 - HandFrom)));
        return Math.Clamp(a, 0, 1);

        static float Bead(float d, float sharpness) => MathF.Pow(2, -(d * sharpness) * (d * sharpness));
    }

    // The afterimage gate of one arm: inside one of its cue windows, in over two ticks and out over the last six.
    internal static float Window(ReadOnlySpan<CrimsonChoirCue> cues, int arm, float age)
    {
        float gate = 0;
        foreach (var cue in cues)
            if ((cue.Broad || cue.Arm == arm) && age >= cue.Born && age < cue.End)
                gate = Math.Max(gate, ScarletEnvelope.Ease((age - cue.Born) / 2) * (1 - ScarletEnvelope.Ease((age - (cue.End - 6)) / 6)));
        return gate;
    }
}
