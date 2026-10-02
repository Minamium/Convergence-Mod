#nullable enable
using System;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// What an apparition body (Crown species 0, Mantle species 1) shows of its attacks: ScarletApparitions.fx's
// attack = (Heat * Register, Ignite, Front, Drain) and attack2 = (Lean, PourLimit, Run, Row). default (Active
// false) is the rest picture: rigs draw exactly what they draw today.
//   Heat      warning heat, cooling after the strike (crossflow: grows with its seals, times 1.25)
//   Ignite    the strike flash (ScarletInk's constant); halved under Reduced Effects
//   Front     latest strike's front through the body, 0..1 over its Reach; -1 = none
//   Drain     the body swallowing its light after the flash (crossflow: Spend, the band's own envelope)
//   Lean      texture-space side of the latest strike (+1 = texture right; 0 for the crossflow, both sides)
//   PourLimit lowest uv.y the Crown's pour reaches (1 signature / crossflow, .70 basic)
//   Run       Mantle: heat running root -> hook tip (warning Send, then the whip); -1 = none
//   Row       Mantle lead row (+1 low, -1 high, 0 both): the approved motion's own row blend, never a pop
//   Swing     texture-space side of the gesture being announced (falls back to Lean)
//   Wind      the approved wind (Crown smoke plumes shrink and lean with it)
//   Send/Return/Snap  the named envelopes; Engaged hands free idle pulses to the attack clock without a pop
// The latest strike's Front and Run hold their final value until the note's window closes; consumers fade
// them with Heat/Return rather than reading the window edge.
internal readonly record struct ScarletBodyState(bool Active, float Heat, float Ignite, float Front, float Drain,
    float Lean, float PourLimit, float Run, float Row, float Swing, float Wind, float Send, float Return, float Snap,
    float Engaged);

// One Choir arm (texture-space index): lift and burst as the accepted arm envelope, the blood front sent to the
// fingers (exactly 1 on Fire), its return to the heart, and the sleeve tear.
internal readonly record struct ScarletChoirLimb(float Lift, float Send, float Return, float Tear, float Burst);

// ScarletChoir.fx attack = (Heat, Ignite, Drain, Surge) plus the four limbs. default (Active false) = rest.
internal readonly record struct ScarletChoirState(bool Active, float Heat, float Ignite, float Drain, float Surge, float Engaged,
    ScarletChoirLimb Arm0, ScarletChoirLimb Arm1, ScarletChoirLimb Arm2, ScarletChoirLimb Arm3)
{
    internal ScarletChoirLimb Limb(int arm) => arm switch { 0 => Arm0, 1 => Arm1, 2 => Arm2, _ => Arm3 };
}

// A stateless particle sample: offset from its anchor, life 0..1, premultiplied strength, size factor.
internal readonly record struct ScarletMote(float X, float Y, float Life, float Alpha, float Size);

// Body material state and analytic particles, all pure functions of (age, notes). Particles have no state and
// live inside their note's window; Reduced Effects keeps every timing value and spawns none of them.
// FNA- and Terraria-free (System only): the domain tests link this file.
internal static class ScarletBodyMaterial
{
    internal const float SurgeLimit = 1.6f, BasicPourLimit = .70f, ReducedIgnite = .5f, ReducedTear = .7f;
    // Crown embers rise only (UV of the body height), <= EmberRise; Mantle/Choir sparks stop within SparkReach px.
    internal const float EmberRise = .16f, EmberBuoyancy = .00005f, EmberDrag = .96f;
    internal const float DripFall = .07f, DripRate = .00018f, DripTicks = 20;
    internal const float SparkReach = 25, SparkDrag = .85f, SparkTicks = 14;
    internal const int IntakeCount = 6, MaximumEmbers = 10, MaximumDrips = 3, MaximumSparks = 6;

    internal static ScarletBodyState Apparition(float age, ReadOnlySpan<ScarletNote> notes, in ScarletApparitionMotion motion,
        bool flipped, bool reduced)
    {
        float heat = 0, ignite = 0, drain = 0, send = 0, snap = 0, engaged = 0, wind = 0;
        int fired = -1, pending = -1;
        for (int i = 0; i < notes.Length; i++)
        {
            var n = notes[i];
            if (!n.Holds(age)) continue;
            var e = ScarletEnvelope.Phase(age, n.Born, n.Fire, n.Span);
            float t = e.T;
            heat = Math.Max(heat, (n.Broad ? ScarletEnvelope.CrossflowHeat(e.P, t) : ScarletEnvelope.Heat(e.P, t)) * n.Register);
            ignite = Math.Max(ignite, ScarletEnvelope.Ignite(t));
            drain = Math.Max(drain, n.Broad ? ScarletEnvelope.Spend(age, n.Fire, n.End) : ScarletEnvelope.Drain(t, n.Span));
            snap = Math.Max(snap, ScarletEnvelope.Snap(t));
            engaged = Math.Max(engaged, ScarletEnvelope.Engaged(age, n.Born, n.Fire, n.Span));
            wind = Math.Max(wind, e.Wind * Math.Min(1, n.Register));
            if (t < 0)
            {
                send = Math.Max(send, ScarletEnvelope.Send(e.P));
                if (pending < 0 || n.Fire < notes[pending].Fire) pending = i;
            }
            else if (fired < 0 || n.Fire > notes[fired].Fire) fired = i;
        }
        if (fired < 0 && pending < 0) return default;
        float texture = flipped ? -1 : 1, front = -1, lean = 0, pour = 1, back = 0, run = -1;
        if (fired >= 0)
        {
            var n = notes[fired];
            float t = age - n.Fire;
            front = ScarletEnvelope.Front(t, n.Reach);
            lean = n.Broad ? 0 : n.Side * texture;
            pour = n.Register < 1 ? BasicPourLimit : 1;
            back = ScarletEnvelope.Return(t, n.Span);
            if (t < ScarletEnvelope.WhipTicks) run = ScarletEnvelope.Whip(t);
        }
        if (run < 0 && pending >= 0)
            run = ScarletEnvelope.Send(ScarletEnvelope.Progress(age, notes[pending].Born, notes[pending].Fire));
        float swing = pending >= 0 ? (notes[pending].Broad ? 0 : notes[pending].Side * texture) : lean;
        if (reduced) ignite *= ReducedIgnite;
        return new(true, heat, ignite, front, drain, lean, pour, run, motion.Row, swing, wind, send, back, snap, engaged);
    }

    internal static ScarletChoirState Choir(float age, ReadOnlySpan<ScarletNote> notes, bool reduced)
    {
        float heat = 0, ignite = 0, drain = 0, engaged = 0;
        Span<ScarletChoirLimb> limbs = stackalloc ScarletChoirLimb[4];
        bool any = false;
        foreach (var n in notes)
        {
            if (!n.Holds(age)) continue;
            any = true;
            var e = ScarletEnvelope.Phase(age, n.Born, n.Fire, n.Span);
            float t = e.T;
            heat = Math.Max(heat, (n.Broad ? ScarletEnvelope.CrossflowHeat(e.P, t) : ScarletEnvelope.Heat(e.P, t)) * n.Register);
            ignite = Math.Max(ignite, ScarletEnvelope.Ignite(t));
            drain = Math.Max(drain, n.Broad ? ScarletEnvelope.Spend(age, n.Fire, n.End) : ScarletEnvelope.Drain(t, n.Span));
            engaged = Math.Max(engaged, ScarletEnvelope.Engaged(age, n.Born, n.Fire, n.Span));
            float release = ScarletEnvelope.Release(t);
            float tear = ScarletEnvelope.Tear(t) * (n.Kind == ScarletNoteKind.Hands ? 1 : n.Broad ? .7f : .6f) * (reduced ? ReducedTear : 1);
            var limb = new ScarletChoirLimb(e.Prepare * (1 - release) * e.Decay, t < 0 ? ScarletEnvelope.Send(e.P) : 1,
                ScarletEnvelope.Return(t, n.Span), tear, ScarletEnvelope.Burst(t, e.Decay));
            for (int arm = 0; arm < 4; arm++)
                if (n.Broad || arm == n.ArmA || arm == n.ArmB) limbs[arm] = Max(limbs[arm], limb);
        }
        if (!any) return default;
        float surge = Math.Min(SurgeLimit, limbs[0].Burst + limbs[1].Burst + limbs[2].Burst + limbs[3].Burst);
        if (reduced) ignite *= ReducedIgnite;
        return new(true, heat, ignite, drain, surge, engaged, limbs[0], limbs[1], limbs[2], limbs[3]);

        static ScarletChoirLimb Max(ScarletChoirLimb a, ScarletChoirLimb b) => new(Math.Max(a.Lift, b.Lift),
            Math.Max(a.Send, b.Send), Math.Max(a.Return, b.Return), Math.Max(a.Tear, b.Tear), Math.Max(a.Burst, b.Burst));
    }

    // ---- Analytic particles. Callers own the anchors (measured from the art) and draw them in one batch. ----

    internal static int EmberCount(in ScarletNote n, bool reduced)
        => reduced ? 0 : n.Kind == ScarletNoteKind.Curtain || n.Broad ? MaximumEmbers : n.Kind == ScarletNoteKind.Beam ? 6 : 0;
    internal static int IntakeCountOf(in ScarletNote n, bool reduced) => !reduced && n.Kind == ScarletNoteKind.Curtain ? IntakeCount : 0;
    internal static int DripCount(in ScarletNote n, bool reduced)
        => reduced ? 0 : n.Kind == ScarletNoteKind.Curtain || n.Broad ? MaximumDrips : n.Kind == ScarletNoteKind.Beam ? 1 : 0;
    // The Choir never sheds a particle during a FourHands window: the full-height gaps are the safe ground.
    internal static int SparkCount(in ScarletNote n, bool reduced)
        => reduced ? 0 : n.Kind switch
        {
            ScarletNoteKind.Rope => MaximumSparks,
            ScarletNoteKind.Rift => 4,
            ScarletNoteKind.Rakes => 5,
            _ => 0
        };

    // Crown ember born on Fire from the hem or a foot: drifts sideways under drag and only ever rises (UV, -y up).
    internal static bool Ember(in ScarletNote n, int index, float age, out ScarletMote mote)
    {
        mote = default;
        float t = age - n.Fire, life = 22 + 8 * Hash(n.Seed, index, 1);
        if (t < 0 || t >= life || !n.Holds(age)) return false;
        float vx = (.0010f + .0015f * Hash(n.Seed, index, 2)) * ((index & 1) == 0 ? -1 : 1);
        float vy = .0028f + .0014f * Hash(n.Seed, index, 3);
        float x = vx * (1 - MathF.Pow(EmberDrag, t)) / (1 - EmberDrag);
        float y = -Math.Min(EmberRise, vy * t + .5f * EmberBuoyancy * t * t);
        float fade = 1 - ScarletEnvelope.Ease((t - (life - 6)) / 6);
        mote = new(x, y, t / life, ScarletEnvelope.Out(t / 2) * MathF.Exp(-t / 9) * .35f * fade, .6f + .4f * Hash(n.Seed, index, 4));
        return true;
    }

    // Crown intake: sparks on a .20 UV ring drawn into the crown ring during the first part of the warning.
    internal static bool Intake(in ScarletNote n, int index, float age, out ScarletMote mote)
    {
        mote = default;
        float p = ScarletEnvelope.Progress(age, n.Born, n.Fire);
        if (age < n.Born || age >= n.Fire || p >= .38f) return false;
        float u = p / .38f, angle = (index + Hash(n.Seed, index, 5)) * MathF.Tau / IntakeCount;
        float radius = .20f * (1 - ScarletEnvelope.Ease(u));
        mote = new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius, u, MathF.Sin(MathF.PI * u) * .3f, .5f + .5f * u);
        return true;
    }

    // Crown drip of molten wine-dark blood from a pendant: falls at most DripFall UV, an ember after ten ticks,
    // gone at twenty. Size carries the stretch.
    internal static bool Drip(in ScarletNote n, int index, float age, out ScarletMote mote)
    {
        mote = default;
        float t = age - n.Fire - 2 * Hash(n.Seed, index, 6);
        if (t < 0 || t >= DripTicks || !n.Holds(age)) return false;
        float fall = Math.Min(DripFall, DripRate * t * t);
        mote = new(0, fall, t / DripTicks, 1 - ScarletEnvelope.Ease((t - 14) / 6), 1 + fall / DripFall * 1.5f);
        return true;
    }

    // Spark thrown from a hook tip or claw along (dirX, dirY): drag stops it within SparkReach px of its anchor.
    internal static bool Spark(in ScarletNote n, int index, float age, float dirX, float dirY, out ScarletMote mote)
    {
        mote = default;
        float t = age - n.Fire;
        if (t < 0 || t >= SparkTicks || !n.Holds(age)) return false;
        float spread = (Hash(n.Seed, index, 7) - .5f), speed = 2.4f + 1.2f * Hash(n.Seed, index, 8);
        float s = MathF.Sin(spread), c = MathF.Cos(spread);
        float x = dirX * c - dirY * s, y = dirX * s + dirY * c, length = MathF.Sqrt(x * x + y * y);
        if (length < 1e-6f) { x = 0; y = -1; } else { x /= length; y /= length; }
        float distance = speed * (1 - MathF.Pow(SparkDrag, t)) / (1 - SparkDrag);
        float alpha = ScarletEnvelope.Out(t / 1.5f) * MathF.Exp(-t / 5) * .4f * (1 - ScarletEnvelope.Ease((t - 10) / 4));
        mote = new(x * distance, y * distance, t / SparkTicks, alpha, .7f + .3f * Hash(n.Seed, index, 9));
        return true;
    }

    // Deterministic [0, 1) from the note seed, particle index and channel.
    internal static float Hash(float seed, int index, int channel)
    {
        uint h = (uint)(seed * 65536) * 2654435761u ^ (uint)(index + 1) * 2246822519u ^ (uint)(channel + 1) * 3266489917u;
        h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
        return (h & 0xFFFFFF) / 16777216f;
    }
}
