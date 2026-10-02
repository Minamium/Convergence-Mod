#nullable enable
using System;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// The approved apparition gesture (Crown / Mantle). Screen-space px at the nominal body height (350 Crown, 430
// Mantle) and radians, +x right, +y down, + turn clockwise. default = rest (the current picture).
internal readonly record struct ScarletApparitionMotion(float OffsetX, float OffsetY, float Turn, float Flare, float Kick,
    float Sweep, float Row);

// Vespera's command (see Command). Offsets are local px added to the held orb, already limited so the orb never
// comes closer to her than today; Cast is the pose-1 weight that replaces Ease(charge * 2).
internal readonly record struct ScarletCommand(float OrbX, float OrbY, float Radius, float ReactorCharge, float ReactorImpulse,
    float AlphaScale, float Cast, float Tilt, int NudgeX, int NudgeY, float RimAlpha, float RimHeat,
    float Draw, float Snap, float Lift, float Spend, float Heat, float Ignite);

// Pure functions of (age, notes): the approved prototype's Crown censer swing with its pour jolt, the Mantle's
// wind/whip with its low/high rows, the Choir's quarter -> arm table and arm envelope, and Vespera's command.
// Motion comes only from each note's Born/Fire/Span; nothing here reads the music grid or a free-running clock
// (the Choir's existing idle sway is the one accepted exception, kept bit-identical in ChoirArm).
//
// FNA- and Terraria-free (System only): the domain tests link this file and pin it to the prototype (G0).
internal static class ScarletGestureMotion
{
    internal const float CrownAmplitude = .15f, CrownChain = 250, CrownDrop = 22, CrownDropTicks = 40;
    internal const float MantleTravel = 30, MantleTurn = .17f;
    // The Choir's torso drops this far (unscaled canvas px) on a slam, times its burst. Effigy path only.
    internal const float Heave = 14;
    internal const float CommandReach = 24;

    // The arm that owns a slammed quarter: outer quarters take the upper arms (longest reach), inner ones the lower
    // arms. Arms are texture-space (even = texture left), so a mirrored body swaps sides.
    internal static int ArmForQuarter(int quarter, bool flipped)
        => (quarter switch { 0 => 0, 1 => 2, 2 => 3, _ => 1 }) ^ (flipped ? 1 : 0);

    // Ember Crown: a censer on an unseen chain. Each note swings it across from the far side, peaking exactly on its
    // Fire; the pour jolts the body down on a short spring. The crossflow (Broad) is not a swing.
    internal static ScarletApparitionMotion Crown(float age, ReadOnlySpan<ScarletNote> notes)
    {
        float phi = 0, drop = 0, flare = 0, kick = 0;
        bool swinging = false; int last = -1;
        for (int i = 0; i < notes.Length; i++)
        {
            var c = notes[i];
            if (c.Broad) continue;
            if (age >= c.Born && age < c.Fire)
            {
                float p = (age - c.Born) / Math.Max(1, c.Fire - c.Born), amplitude = c.Side * CrownAmplitude * c.Register;
                // A lead note takes the censer over at the previous strike's peak. The approved swing crosses from the
                // far side (the curtain alternates); a basic beam announced on the same side again dips toward the
                // centre and returns instead, so the handover never jumps.
                phi = c.Lead && SameSide(notes, c) ? amplitude * MathF.Cos(MathF.PI * p) * MathF.Cos(MathF.PI * p)
                    : amplitude * -MathF.Cos(MathF.PI * p) * (c.Lead ? 1 : ScarletEnvelope.Ease(p * 2.2f));
                swinging = true;
            }
            else if (age >= c.Fire && age < c.Close && (last < 0 || c.Fire > notes[last].Fire)) last = i;
            var e = ScarletEnvelope.Phase(age, c.Born, c.Fire, c.Span);
            flare = Math.Max(flare, e.Wind * c.Register);
            kick = Math.Max(kick, e.Follow * c.Register);
            if (e.T >= 0 && e.T < CrownDropTicks) drop += CrownDrop * c.Register * MathF.Exp(-e.T / 8) * MathF.Sin(MathF.PI * e.T / 8);
        }
        if (!swinging && last >= 0)
        {
            var c = notes[last];
            float t = age - c.Fire, beat = Math.Max(1, c.Fire - c.Born);
            phi = c.Side * CrownAmplitude * c.Register * MathF.Exp(-t / 16) * MathF.Cos(MathF.PI * t / beat)
                * (1 - ScarletEnvelope.Ease((t - c.Span + 12) / 12));
        }
        return new(CrownChain * MathF.Sin(phi), CrownChain * (1 - MathF.Cos(phi)) + drop, -phi, flare, kick, 0, 0);

        static bool SameSide(ReadOnlySpan<ScarletNote> notes, in ScarletNote c)
        {
            foreach (var previous in notes)
                if (!previous.Broad && previous.Fire == c.Born && previous.Fire < c.Fire)
                    return previous.Side == c.Side;
            return false;
        }
    }

    // Sable Mantle: a reaper's figure-eight. It winds against the cut, whips the leading row of hooks through it on
    // Fire (low row for the low cut, high row for the high one), dips or rises toward the cut, and one cut's
    // follow-through is the next one's wind-up. Flare/Kick carry the strongest wind and follow.
    internal static ScarletApparitionMotion Mantle(float age, ReadOnlySpan<ScarletNote> notes)
    {
        float turn = 0, sweep = 0, x = 0, y = 0, row = 0, flare = 0, kick = 0;
        foreach (var c in notes)
        {
            if (c.Broad) continue;
            var e = ScarletEnvelope.Phase(age, c.Born, c.Fire, c.Span);
            if (!e.Live) continue;
            float whip = ScarletEnvelope.Whip(e.T);
            float wind = e.Prepare * (1 - whip) * c.Register, follow = whip * e.Decay * c.Register;
            // A clockwise turn carries the upper row right; the lower row needs the opposite turn.
            float swing = c.Side * (c.Low ? -1 : 1);
            turn += swing * (-.45f * wind + follow);
            sweep += swing * (-.85f * wind + follow);
            x += c.Side * (-.55f * wind + follow);
            y += (c.Low ? 1 : -1.15f) * (wind + .75f * follow);
            row += (c.Low ? 1 : -1) * (wind + follow);
            flare = Math.Max(flare, wind); kick = Math.Max(kick, follow);
        }
        return new(MantleTravel * MathF.Tanh(x), MantleTravel * MathF.Tanh(y), MantleTurn * MathF.Tanh(turn),
            flare, kick, MathF.Tanh(1.2f * sweep), MathF.Tanh(row));
    }

    // The accepted Choir arm (CrimsonChoirMotion.Arm, bit-identical): what the arm's own cues drive.
    internal static (float Lift, float Strike, float Energy, float Burst) ChoirDrive(int index, float age, ReadOnlySpan<CrimsonChoirCue> cues)
    {
        float lift = 0, strike = 0, energy = 0, burst = 0;
        foreach (var cue in cues)
        {
            if ((!cue.Broad && cue.Arm != index) || age < cue.Born || age >= cue.End) continue;
            float p = ScarletEnvelope.Progress(age, cue.Born, cue.Fire);
            float t = age - cue.Fire;
            // Rapid intake -> braked tension -> four-tick unfurl. Continuous at
            // Fire, with shaped recovery completed before descriptor expiry.
            float prepare = ScarletEnvelope.Prepare(p);
            float release = ScarletEnvelope.Release(t);
            float decay = ScarletEnvelope.Decay(t, cue.End - cue.Fire);
            float pulse = ScarletEnvelope.Burst(t, decay);
            lift = Math.Max(lift, prepare * (1 - release) * decay);
            strike = Math.Max(strike, release * decay);
            energy = Math.Max(energy, (prepare * (1 - release) + .7f * release) * decay);
            burst = Math.Max(burst, pulse);
        }
        return (lift, strike, energy, burst);
    }

    internal static CrimsonChoirArm ChoirArm(int index, float age, float charge, float recoil, ReadOnlySpan<CrimsonChoirCue> cues)
    {
        var (lift, strike, energy, burst) = ChoirDrive(index, age, cues);
        // Summoning/sacrifice have no attack descriptors: do not invent strikes.
        if (cues.IsEmpty) { lift = charge * .35f; strike = recoil * .45f; energy = charge * .45f; burst = recoil * .3f; }
        float side = (index & 1) == 0 ? -1 : 1, lower = index >= 2 ? -1 : 1;
        return new(side * (MathF.Sin(age * .025f + index * 1.7f) * .065f + lower * (lift * .72f - strike * .80f)),
            side * (MathF.Sin(age * .033f + index * 2.1f) * .09f - lift * .66f + strike * .49f),
            side * (MathF.Sin(age * .046f + index) * .07f + lift * .36f - strike * .54f), energy, burst);
    }

    // Vespera commands her Act's body: she draws the orb back against where the note will strike while it is
    // announced and releases it toward the strike on Fire; the crossflow lifts it for two beats and spends it.
    // `charge`/`recoil` are the existing signal; with no note the result is exactly today's held orb (offset 0,
    // radius 53 + 24 charge + 18 recoil, Cast = Ease(charge * 2), base rim). facing: +1 orb on her right.
    internal static ScarletCommand Command(float age, ReadOnlySpan<ScarletNote> notes, float charge, float recoil, int facing, bool reduced)
    {
        float draw = 0, snap = 0, lift = 0, spend = 0, heat = 0, ignite = 0, window = 0;
        float pullX = 0, pullY = 0, pushX = 0, pushY = 0;
        foreach (var n in notes)
        {
            var e = ScarletEnvelope.Phase(age, n.Born, n.Fire, n.Span);
            float t = e.T, s = n.Holds(age) ? ScarletEnvelope.Snap(t) : 0;
            if (n.Broad)
            {
                // Charge toward the upper right as the seals open, release along the flow (right to left).
                lift = Math.Max(lift, e.Wind);
                spend = Math.Max(spend, n.Holds(age) ? ScarletEnvelope.Spend(age, n.Fire, n.End) : 0);
                pullX += .70710678f * e.Wind * n.Weight; pullY -= .70710678f * e.Wind * n.Weight;
            }
            else
            {
                draw = Math.Max(draw, e.Wind);
                pullX += n.AimX * e.Wind * n.Weight; pullY += n.AimY * e.Wind * n.Weight;
            }
            pushX += n.AimX * s * n.Weight; pushY += n.AimY * s * n.Weight;
            snap = Math.Max(snap, s);
            if (n.Holds(age))
            {
                heat = Math.Max(heat, Math.Min(1, ScarletEnvelope.Heat(e.P, t) * n.Register));
                ignite = Math.Max(ignite, ScarletEnvelope.Ignite(t));
                window = Math.Max(window, ScarletEnvelope.Ease((age - (n.Born - 6)) / 4) * (1 - ScarletEnvelope.Ease((age - (n.Close - 4)) / 4)));
            }
        }
        if (reduced) ignite *= .5f;
        float amplitude = reduced ? .5f : 1;
        float x = amplitude * (-11 * MathF.Tanh(pullX) + 20 * MathF.Tanh(pushX));
        float y = amplitude * (-11 * MathF.Tanh(pullY) + 20 * MathF.Tanh(pushY) - 5 * draw - 14 * lift);
        // Never closer to her than the held orb (her face and the Hands' central gap stay clear), at most 24 px away.
        x = facing >= 0 ? Math.Max(0, x) : Math.Min(0, x);
        float length = MathF.Sqrt(x * x + y * y);
        if (length > CommandReach) { x *= CommandReach / length; y *= CommandReach / length; }
        float core = Math.Max(charge, heat);
        float baseRadius = 53 + 24 * core + 18 * recoil;
        float radius = baseRadius + amplitude * (-baseRadius * .10f * draw + 10 * snap + 16 * lift);
        float idleCast = ScarletEnvelope.Ease(charge * 2);
        float tilt = Math.Clamp(amplitude * facing * (-.03f * draw + .05f * snap), -.09f, .09f);
        return new(x, y, radius, core, recoil + .6f * ignite, 1 - .30f * spend,
            1 - (1 - idleCast) * (1 - window), tilt,
            (int)MathF.Round(amplitude * facing * 2 * snap), (int)MathF.Round(amplitude * snap),
            .26f + amplitude * (.14f * heat + .20f * ignite), ignite, draw, snap, lift, spend, heat, ignite);
    }
}
