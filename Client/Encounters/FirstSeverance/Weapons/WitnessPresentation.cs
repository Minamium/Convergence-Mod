#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

internal enum WitnessPart : byte { Hang, Blade, Shard, Judgement }

// One drawn frame of one Last Witness projectile, already interpolated to the frame (WeaponDrawClock.Fraction) by its
// source; the offline preview builds the same states from WitnessRules. Textures are the frame's Asset.Value reads.
internal struct WitnessDrawState
{
    internal WitnessPart Part;
    // The blade is WitnessBladeArt's rung (k = 2), hanging and thrown.
    internal Texture2D? Blade, Sword, Shards;
    internal IDollEnergyMaterial? Energy;
    // Another player's weapon: its damaging light draws at DollWeaponCanvas.PeerLightAlpha and its void at
    // DollWeaponCanvas.PeerVoidAlpha; bodies stay opaque.
    internal bool Peer;
    internal int Seed;
    // Ticks since the projectile ended (residue), or negative while it lives.
    internal float Gone;

    // Hang: the owner's hand, the aim angle, the side the blade hangs on (+1/-1) and the score age.
    internal Vector2 Root;
    internal float Aim, Age;
    internal int Facing;
    internal bool Stealth;
    // World ticks plus the draw fraction: breathing (continuous across held scores) and flicker.
    internal float Clock;
    // The hanging blade is drawn (this owner has no thrown blade out); ticks since it appeared in the hang; it
    // appeared by catching the returning blade; testimonies spoken.
    internal bool Body;
    internal float BodyAge;
    internal bool Caught;
    internal int Spoken;
    // Peers only: ticks since the throw while the thrown blade has not arrived yet (negative: none).
    internal float Ghost;

    // Thrown blade (also Shard): centre, angle, recent centres (newest first, Trail[0] is the frame's centre).
    internal Vector2 Center;
    internal float Rotation;
    internal int SpinSign;
    internal WitnessPhase Phase;
    internal float PhaseAge;
    internal bool Anchored;
    internal Vector2[]? Trail;
    internal int TrailCount;

    // Shard: heading and shape (0-2).
    internal float Heading;
    internal int Shape;

    // Judgement: something stood inside the footprint at the execution.
    internal bool Struck;
}

// The drawing of Last Witness v2 through the shared Doll weapon layer (front stratum). Terraria-free: FNA, the pure
// WitnessRules and the exported anchors only, so the offline preview links it. 1 texel = 1 dot = 2 world px; the blade
// is one exported rung (WitnessBladeArt, k = 2) hanging and thrown, fitted to the 56 px hit disc. Live damage is the
// DollWitnessEnergy material (Ribbon: wake, spin arc, tails, threads, edges; Fill: the execution); forecasts are
// pearl-violet hairlines.
internal static class WitnessPresentation
{
    internal const int RibbonPass = 0, FillPass = 1;
    internal const float SpinArcSpan = 2.09f, ReducedArcSpan = 1.4f, SpinArcAlpha = .7f, SpinArcWidth = 3, WakeWidth = 10;
    internal const float CancelDissolve = 20, BladeDissolve = 16, ShardPuff = 10, JudgementResidue = 12;
    // The execution fill's porcelain ground between the cracks; only the craquelure and the pearl lip are opaque.
    internal const float FillGroundAlpha = .55f;

    private static readonly Vector2 BladePivot = X(WitnessBladeArt.Pivot);
    private static readonly Vector2 ShardPivot = X(DollArtAnchors.WitnessShards.Pivot);
    private static readonly Vector2 SwordPivot = X(DollArtAnchors.WitnessSword.StakePoint);

    internal static bool Emit(DollWeaponCanvas canvas, in WitnessDrawState s)
    {
        switch (s.Part)
        {
            case WitnessPart.Hang: return EmitHang(canvas, in s);
            case WitnessPart.Blade: return EmitBlade(canvas, in s);
            case WitnessPart.Shard: return EmitShard(canvas, in s);
            default: return EmitJudgement(canvas, in s);
        }
    }

    // Residue time, halved by Reduced Effects.
    internal static float Residue(DollWeaponCanvas canvas, float ticks) => canvas.Reduced ? ticks * .5f : ticks;

    // ---- Hang ------------------------------------------------------------------------------------

    private static bool EmitHang(DollWeaponCanvas canvas, in WitnessDrawState s)
    {
        float dissolve = s.Gone >= 0 ? s.Gone / Residue(canvas, CancelDissolve) : 0;
        if (dissolve >= 1) return false;
        float age = s.Age, f = s.Facing < 0 ? -1 : 1, light = LightAlpha(in s) * (1 - dissolve);
        HangFrame(in s, out Vector2 center, out float angle);
        DollFlip flip = f < 0 ? DollFlip.Vertical : DollFlip.None;
        Vector2 eye = Local(center, angle, f, WitnessRules.HangEye);

        if (s.Body && s.Blade is { } blade)
        {
            float flash = s.Caught ? 1 - Clamp01(s.BodyAge / 5f) : 0;
            // The wind before the throw pulses the steel ever faster (12 -> 4 ticks).
            if (age >= WitnessRules.ThrowWarn && age < WitnessRules.Throw)
            {
                float u = (age - WitnessRules.ThrowWarn) / (WitnessRules.Throw - WitnessRules.ThrowWarn);
                float period = 12 - 8 * u, phase = (age - WitnessRules.ThrowWarn) / period;
                flash = MathF.Max(flash, .35f * MathF.Pow(1 - (phase - MathF.Floor(phase)), 3));
            }
            var fx = new DollSpriteFx
            {
                Flash = flash,
                Fade = s.Caught ? 0 : 1 - Clamp01(s.BodyAge / 6f),
                Dissolve = dissolve,
                Seed = s.Seed,
            };
            canvas.Sprite(new DollSprite(blade, blade.Bounds, BladePivot), center, angle, flip, DollStratum.Front, 0, fx);
            if (!s.Caught && s.BodyAge < 10 && s.Gone < 0)
                canvas.Burst(center + new Vector2(0, 10), s.Seed, 6, s.BodyAge, 10, 1.6f, .08f, DollShardKind.Porcelain, 2.4f, MathF.PI / 2);
        }
        else if (s.Ghost >= 0 && s.Ghost < 6)
        {
            // A peer still waiting for the thrown blade: carry it on from the release instead of a gap.
            float reach = WitnessRules.ReleaseRadius + WitnessRules.OutboundSpeed * s.Ghost;
            if (s.Blade is { } ghost)
                canvas.Sprite(new DollSprite(ghost, ghost.Bounds, BladePivot), s.Root + Unit(s.Aim) * reach,
                    s.Aim + f * WitnessRules.CruiseSpin * s.Ghost, flip, DollStratum.Front, 0, new DollSpriteFx { Fade = Clamp01((s.Ghost - 3) / 3) });
        }

        if (s.Gone < 0)
        {
            Testimonies(canvas, in s, center, angle, f, light);
            SealAndWind(canvas, in s, center, angle, f, eye, light);
        }
        Eye(canvas, eye, age >= WitnessRules.Seal ? WitnessRules.Testimonies : s.Spoken, s.Stealth, s.Body ? 1 - dissolve : 0,
            age >= WitnessRules.Seal && age < WitnessRules.Throw ? 1 : 0, s.Clock);
        // The catch: a pearl ring opens around the blade as it settles back into the hang.
        if (s.Body && s.Caught && s.BodyAge < 10)
        {
            float u = RitualKineticMotion.Arrive(s.BodyAge / 8f);
            canvas.Ring(center, 10 + 26 * u, u < .5f ? DollTone.PearlViolet : DollTone.Lilac, u < .4f ? 2 : 1, 1 - Clamp01((s.BodyAge - 4) / 6));
            canvas.Burst(center, s.Seed + 3, 8, s.BodyAge, 12, 2.4f, .04f, DollShardKind.Pearl);
        }
        return true;
    }

    // The hanging blade's balance point and angle this frame (breathing and the first settle included).
    internal static void HangFrame(in WitnessDrawState s, out Vector2 center, out float angle)
    {
        float f = s.Facing < 0 ? -1 : 1;
        WitnessPose pose = WitnessRules.Pose(s.Age);
        float calm = 1 - RitualKineticMotion.Settle((s.Age - WitnessRules.Seal) / 12f);
        float beta = pose.Beta + .05f * MathF.Sin(MathF.Tau * s.Clock / 150f) * calm;
        float radius = pose.Radius + 2 * MathF.Sin(MathF.Tau * s.Clock / 120f) * calm;
        // The hold before the whip trembles.
        if (s.Age >= WitnessRules.LiftEnd && s.Age < WitnessRules.WhipStart) beta += .022f * MathF.Sin(s.Age * 2.7f);
        angle = s.Aim + f * beta;
        center = s.Root + Unit(angle) * radius;
        if (!s.Caught) center.Y -= 24 * (1 - RitualKineticMotion.Arrive(s.BodyAge / 12f));
    }

    // Each testimony: a thread of light runs from the eye along the edge to its seat, the edge cracks, the shard
    // slides out and pulls back, then leaves (its own projectile draws it from there); a pearl notch stays lit.
    // Everything on the edge needs the blade in the hang: while this owner's thrown blade is still out (a held trigger
    // whose catch comes late) the shard just leaves its seat with a few sparks.
    private static void Testimonies(DollWeaponCanvas canvas, in WitnessDrawState s, Vector2 center, float angle, float f, float light)
    {
        float age = s.Age;
        for (int birth = 0; birth < WitnessRules.Testimonies; birth++)
        {
            float warn = WitnessRules.TestimonyWarn(birth), fire = WitnessRules.TestimonyFire(birth);
            int seat = WitnessRules.SeatOf(birth);
            float along = WitnessRules.SeatAlong(seat);
            Vector2 edge = Local(center, angle, f, new NVector2(along, WitnessRules.SeatEdge));
            if (s.Body && age >= fire && age < WitnessRules.Seal + 8)
            {
                // The notch where the shard left; at the seal the notches run along the edge into the eye.
                float run = age < WitnessRules.Seal ? 0 : RitualKineticMotion.Strike((age - WitnessRules.Seal - .6f * seat) / 5f);
                Vector2 eye = Local(center, angle, f, new NVector2(WitnessRules.HangEye.X, WitnessRules.SeatEdge));
                Vector2 at = Vector2.Lerp(edge, eye, run);
                if (run < 1)
                {
                    canvas.Dot(at, run > 0 ? DollTone.White : DollTone.PearlViolet, 2, light);
                    if (run > 0) canvas.Line(Vector2.Lerp(edge, eye, MathF.Max(0, run - .35f)), at, DollTone.Lilac, 1, light);
                }
            }
            if (age < warn || age >= fire + 5) continue;
            if (age < fire)
            {
                float t = age - warn;
                // The thread: written from the eye to the seat over the first five ticks.
                if (s.Energy is { } energy && s.Body)
                {
                    Vector2 from = Local(center, angle, f, new NVector2(WitnessRules.HangEye.X + 4, WitnessRules.SeatEdge - 1.5f));
                    Vector2 to = Local(center, angle, f, new NVector2(along, WitnessRules.SeatEdge - 1.5f));
                    canvas.EnergyQuad(energy, RibbonPass, from, to, 3, new Vector4(.85f, light, Clamp01(t / 5f), 0));
                }
                // The edge cracks at the seat: a short zig-zag hairline across the edge.
                if (t > 3 && s.Body)
                {
                    Vector2 c0 = Local(center, angle, f, new NVector2(along - 3, WitnessRules.SeatEdge - 4));
                    Vector2 c1 = Local(center, angle, f, new NVector2(along + 1, WitnessRules.SeatEdge - 1));
                    Vector2 c2 = Local(center, angle, f, new NVector2(along - 1, WitnessRules.SeatEdge + 2));
                    canvas.Line(c0, c1, DollTone.PearlViolet, 1, light);
                    canvas.Line(c1, c2, DollTone.White, 1, light);
                }
                // The shard itself, embedded behind the blade's edge, slides out pointing down the line of fire.
                if (s.Body && s.Shards is { } shards)
                {
                    Vector2 seatAt = Local(center, angle, f, WitnessRules.SeatLocal(birth, age));
                    canvas.Sprite(new DollSprite(shards, ShardFrame(birth % 3), ShardPivot), seatAt, s.Aim,
                        MathF.Cos(s.Aim) < 0 ? DollFlip.Vertical : DollFlip.None, DollStratum.Front, -1,
                        new DollSpriteFx { Flash = t > 7 ? .5f : 0 });
                }
            }
            else
            {
                // The shot: a short white flick down the line of fire and a few sparks off the edge.
                float t = age - fire;
                Vector2 seatAt = Local(center, angle, f, WitnessRules.SeatLocal(birth, fire));
                if (t < 2.5f && s.Body) canvas.Line(seatAt, seatAt + Unit(s.Aim) * (18 + 14 * t), DollTone.White, t < 1 ? 2 : 1, light);
                canvas.Burst(seatAt, s.Seed + birth * 13, 7, t, 9, 2.6f, .05f, DollShardKind.Spark, 2.2f, s.Aim);
            }
        }
    }

    // The seal flares the eye; the lift raises a rim light along the edge; the whip leaves a growing swing arc.
    private static void SealAndWind(DollWeaponCanvas canvas, in WitnessDrawState s, Vector2 center, float angle, float f, Vector2 eye, float light)
    {
        float age = s.Age;
        if (age >= WitnessRules.Seal + 4 && age < WitnessRules.Seal + 16)
        {
            float u = (age - WitnessRules.Seal - 4) / 12f;
            canvas.Ring(eye, 4 + 22 * RitualKineticMotion.Arrive(u), u < .3f ? DollTone.White : DollTone.PearlViolet, u < .5f ? 2 : 1, light * (1 - u));
            if (u < .5f) canvas.Burst(eye, s.Seed + 77, 10, u * 12, 12, 2.2f, 0, DollShardKind.Pearl);
        }
        if (s.Energy is not { } energy || !s.Body) return;
        if (age >= WitnessRules.Seal && age < WitnessRules.Throw)
        {
            // Rim light along the cutting edge, pulsing faster through the wind.
            float pulse = .55f;
            if (age >= WitnessRules.ThrowWarn)
            {
                float u = (age - WitnessRules.ThrowWarn) / (WitnessRules.Throw - WitnessRules.ThrowWarn);
                float period = 12 - 8 * u, phase = (age - WitnessRules.ThrowWarn) / period;
                pulse = .5f + .5f * MathF.Pow(1 - (phase - MathF.Floor(phase)), 2);
            }
            float rise = Clamp01((age - WitnessRules.Seal) / 10f);
            Vector2 a = Local(center, angle, f, new NVector2(-26, WitnessRules.SeatEdge - .5f));
            Vector2 b = Local(center, angle, f, new NVector2(WitnessRules.HangTip.X - 4, WitnessRules.SeatEdge - 2.5f));
            canvas.EnergyQuad(energy, RibbonPass, a, b, 2.5f, new Vector4(.55f + .45f * pulse, light * rise, 2, .4f));
        }
        if (age >= WitnessRules.WhipStart && age < WitnessRules.Throw + 1)
        {
            // The swing arc grows behind the tip through the whip: the tip's own path over the last few ticks.
            Span<Vector2> spine = stackalloc Vector2[12];
            float from = MathF.Max(WitnessRules.WhipStart, age - 5), to = MathF.Min(age, WitnessRules.Throw);
            if (to - from < .2f) return;
            for (int i = 0; i < spine.Length; i++)
            {
                float a = from + (to - from) * i / (spine.Length - 1);
                WitnessPose pose = WitnessRules.Pose(a);
                float bladeAngle = s.Aim + f * pose.Beta;
                Vector2 c = s.Root + Unit(bladeAngle) * pose.Radius;
                spine[i] = Local(c, bladeAngle, f, new NVector2(WitnessRules.HangTip.X - 6, 0));
            }
            float grow = Clamp01((age - WitnessRules.WhipStart) / 8f);
            canvas.EnergyStrip(energy, RibbonPass, spine, 6 + 10 * grow, new Vector4(.75f + .25f * grow, light * MathF.Min(1, 2 - 2 * Clamp01(age - WitnessRules.Throw)), 2, .85f));
        }
    }

    // The witness eye glows through its hole: one sixth brighter per testimony, flaring at the seal; ruby under stealth.
    private static void Eye(DollWeaponCanvas canvas, Vector2 eye, int spoken, bool stealth, float alpha, float flare, float clock)
    {
        if (alpha <= .01f) return;
        DollTone tone = spoken switch
        {
            0 => DollTone.Plum, 1 => DollTone.PlumLight, 2 => DollTone.Violet, 3 => DollTone.Violet, 4 => DollTone.Lilac,
            5 => DollTone.PearlViolet, _ => DollTone.White,
        };
        if (flare > 0 && (int)(clock / 3) % 2 == 0) tone = DollTone.White;
        canvas.Dot(eye, tone, 2, alpha);
        // A pearl-violet glint rises over the eye as the testimony grows.
        if (spoken >= 2) canvas.Dot(eye + new Vector2(0, -4), spoken >= 5 ? DollTone.White : DollTone.PearlViolet, 1, alpha);
        if (stealth) canvas.Dot(eye + new Vector2(2, 2), DollTone.Ruby, 1, alpha);
    }

    // ---- Thrown blade ------------------------------------------------------------------------------

    private static bool EmitBlade(DollWeaponCanvas canvas, in WitnessDrawState s)
    {
        float dissolve = s.Gone >= 0 ? s.Gone / Residue(canvas, BladeDissolve) : 0;
        if (dissolve >= 1) return false;
        Texture2D? texture = s.Blade;
        float reach = WitnessBladeArt.TipReach;
        float sign = s.SpinSign < 0 ? -1 : 1, light = LightAlpha(in s) * (1 - dissolve);
        DollFlip flip = sign < 0 ? DollFlip.Vertical : DollFlip.None;
        bool turning = s.Phase == WitnessPhase.Turn;
        float t = s.PhaseAge;

        if (s.Energy is { } energy && s.Gone < 0)
        {
            Wake(canvas, in s, energy, reach, light);
            SpinArc(canvas, in s, energy, reach, sign, light);
        }
        if (texture is not null)
        {
            float flash = 0;
            if (turning && s.Anchored && t < 3) flash = .6f * (1 - t / 3f);
            if (s.Phase == WitnessPhase.Return && t < 3) flash = MathF.Max(flash, .5f * (1 - t / 3f));
            canvas.Sprite(new DollSprite(texture, texture.Bounds, BladePivot), s.Center, s.Rotation, flip,
                DollStratum.Front, 0, new DollSpriteFx { Flash = flash, Dissolve = dissolve, Seed = s.Seed });
        }
        if (s.Gone >= 0)
        {
            canvas.Burst(s.Center, s.Seed + 5, 14, s.Gone, Residue(canvas, BladeDissolve), 2.8f, .1f, DollShardKind.Porcelain);
            return true;
        }
        // The eye burns in flight; ruby under stealth.
        Vector2 eye = Local(s.Center, s.Rotation, sign, WitnessRules.HangEye);
        canvas.Dot(eye, (int)(s.Clock / 2) % 3 == 0 ? DollTone.PearlViolet : DollTone.White, 2);
        if (s.Stealth) canvas.Dot(eye + new Vector2(2, 0), DollTone.Ruby, 1);

        if (turning)
        {
            if (s.Anchored && t < 8)
            {
                // The bite: an impact star and a ring burst where the blade struck.
                float u = t / 8f;
                float arm = 10 + 30 * RitualKineticMotion.Arrive(u);
                for (int k = 0; k < 4; k++)
                {
                    Vector2 d = Unit(s.Rotation + k * MathF.PI / 2 + MathF.PI / 4);
                    canvas.Line(s.Center + d * arm * .25f, s.Center + d * arm, k % 2 == 0 ? DollTone.White : DollTone.PearlViolet, u < .4f ? 2 : 1, light * (1 - u));
                }
                canvas.Ring(s.Center, 12 + 40 * RitualKineticMotion.Arrive(u), DollTone.PearlViolet, u < .3f ? 2 : 1, light * (1 - u));
                canvas.Burst(s.Center, s.Seed + 11, 16, t, 14, 3.4f, .06f, DollShardKind.Spark);
                canvas.Burst(s.Center, s.Seed + 12, 8, t, 16, 2.2f, .12f, DollShardKind.Porcelain);
            }
            // Each half-turn bite: the hit disc glints and sparks fly off the tip, tangentially.
            for (int bite = 0; bite < WitnessRules.TurnBites; bite++)
            {
                float since = t - WitnessRules.TurnBiteTick(bite);
                if (since < 0 || since >= 6) continue;
                float u = since / 6f;
                canvas.Ring(s.Center, WitnessRules.SpinRadius + 6 * u, s.Anchored ? DollTone.PearlViolet : DollTone.Lilac,
                    u < .34f ? 2 : 1, light * (s.Anchored ? 1 : .55f) * (1 - u));
                Vector2 tip = s.Center + Unit(s.Rotation) * reach * .9f;
                if (s.Anchored)
                    canvas.Burst(tip, s.Seed + 20 + bite, 9, since, 8, 3.8f, .04f, DollShardKind.Spark, 1.2f, s.Rotation + sign * MathF.PI / 2);
            }
            if (!s.Anchored && t < 20 && (int)t % 5 == 0)
                canvas.Burst(s.Center, s.Seed + 40 + (int)t, 4, t % 5, 6, 1.4f, .02f, DollShardKind.Pearl);
        }
        else if (s.Phase == WitnessPhase.Return && t < 6)
        {
            // Tearing free.
            Vector2 back = s.Trail is { } trail && s.TrailCount >= 2 ? trail[1] - trail[0] : -Unit(s.Rotation);
            canvas.Burst(s.Center, s.Seed + 31, 12, t, 10, 3.6f, .06f, DollShardKind.Spark, 2.4f, MathF.Atan2(back.Y, back.X));
        }
        return true;
    }

    // A narrow textured wake down the path, starting behind the spin so the blade itself stays clear.
    private static void Wake(DollWeaponCanvas canvas, in WitnessDrawState s, IDollEnergyMaterial energy, float reach, float light)
    {
        if (s.Trail is not { } trail || s.TrailCount < 2 || s.Phase == WitnessPhase.Turn) return;
        float skip = reach * .45f, length = (s.Phase == WitnessPhase.Return ? 110 : 150) * (canvas.Reduced ? .6f : 1);
        Span<Vector2> spine = stackalloc Vector2[DollWeaponCanvas.MaxStripPoints];
        int count = 0;
        float walked = 0;
        Vector2 previous = trail[0];
        for (int i = 1; i < s.TrailCount && count < spine.Length; i++)
        {
            Vector2 next = trail[i];
            float step = Vector2.Distance(previous, next);
            if (step < 1e-3f) continue;
            float start = walked, end = walked + step;
            if (end > skip && count == 0) spine[count++] = Vector2.Lerp(previous, next, Clamp01((skip - start) / step));
            if (end > skip && count > 0)
            {
                if (end >= skip + length)
                {
                    spine[count++] = Vector2.Lerp(previous, next, Clamp01((skip + length - start) / step));
                    break;
                }
                spine[count++] = next;
            }
            walked = end;
            previous = next;
        }
        if (count < 2) return;
        // Tail first (along 0) so the band narrows and cools away from the blade.
        spine.Slice(0, count).Reverse();
        float intensity = s.Phase == WitnessPhase.Return ? .82f : .95f;
        canvas.EnergyStrip(energy, RibbonPass, spine.Slice(0, count), WakeWidth, new Vector4(intensity, light * (canvas.Reduced ? .7f : 1), 2, .85f));
    }

    // The trailing spin arc on the hit disc's rim (0.89 of the drawn tip's reach): at most 2 dots wide, at most 120
    // degrees, alpha at most .7, never inside 0.8 of the tip's reach, so the physical blade stays readable.
    private static void SpinArc(DollWeaponCanvas canvas, in WitnessDrawState s, IDollEnergyMaterial energy, float reach, float sign, float light)
    {
        float span = canvas.Reduced ? ReducedArcSpan : SpinArcSpan;
        float spin = WitnessRules.Spin(s.Phase, s.PhaseAge);
        span *= Clamp01(spin / WitnessRules.CruiseSpin * .75f);
        if (span <= .05f) return;
        Span<Vector2> spine = stackalloc Vector2[14];
        float radius = WitnessBladeArt.ArcRadius;
        for (int i = 0; i < spine.Length; i++)
        {
            float a = s.Rotation - sign * span * (1 - i / (float)(spine.Length - 1));
            spine[i] = s.Center + Unit(a) * radius;
        }
        // A comet: white at the tip, cooling to plum at its tail.
        float intensity = s.Phase == WitnessPhase.Turn && !s.Anchored ? .85f : 1;
        canvas.EnergyStrip(energy, RibbonPass, spine, SpinArcWidth, new Vector4(intensity, light * SpinArcAlpha * (canvas.Reduced ? .65f : 1), 2, 1));
    }

    // ---- Shard ------------------------------------------------------------------------------------

    private static bool EmitShard(DollWeaponCanvas canvas, in WitnessDrawState s)
    {
        float residue = Residue(canvas, ShardPuff);
        if (s.Gone >= residue) return false;
        float light = LightAlpha(in s);
        if (s.Gone >= 0)
        {
            canvas.Burst(s.Center, s.Seed, 9, s.Gone, residue, 2.4f, .08f, DollShardKind.Porcelain);
            canvas.Burst(s.Center, s.Seed + 1, 5, s.Gone, residue, 2.8f, 0, DollShardKind.Spark);
            return true;
        }
        if (s.Energy is { } energy && s.Trail is { } trail && s.TrailCount >= 2)
        {
            // The tail: a short narrow ribbon behind the shard's back end.
            Span<Vector2> spine = stackalloc Vector2[6];
            Vector2 back = s.Center - Unit(s.Heading) * 10;
            float maximum = 56 * (canvas.Reduced ? .6f : 1), walked = 0;
            int count = 0;
            spine[count++] = back;
            Vector2 previous = back;
            for (int i = 1; i < s.TrailCount && count < spine.Length; i++)
            {
                float step = Vector2.Distance(previous, trail[i]);
                if (step < 1e-3f) continue;
                if (walked + step >= maximum)
                {
                    spine[count++] = Vector2.Lerp(previous, trail[i], (maximum - walked) / step);
                    break;
                }
                walked += step;
                spine[count++] = previous = trail[i];
            }
            if (count >= 2)
            {
                spine.Slice(0, count).Reverse();
                canvas.EnergyStrip(energy, RibbonPass, spine.Slice(0, count), 7, new Vector4(.8f, light, 2, .9f));
            }
        }
        if (s.Shards is { } shards)
            canvas.Sprite(new DollSprite(shards, ShardFrame(s.Shape), ShardPivot), s.Center, s.Heading,
                MathF.Cos(s.Heading) < 0 ? DollFlip.Vertical : DollFlip.None);
        return true;
    }

    // ---- Judgement --------------------------------------------------------------------------------

    private static bool EmitJudgement(DollWeaponCanvas canvas, in WitnessDrawState s)
    {
        float t = s.Age;
        float end = s.Gone >= 0 ? 1 - s.Gone / Residue(canvas, JudgementResidue) : 1;
        if (end <= 0 || t >= RitualArmamentChoreography.VerdictDuration) return false;
        float light = LightAlpha(in s) * end;
        float radius = WitnessRules.JudgementRadius(t);
        Span<Vector2> corners = stackalloc Vector2[3];
        Span<Vector2> footprint = stackalloc Vector2[3];
        for (int k = 0; k < 3; k++)
        {
            NVector2 c = RitualArmamentChoreography.Triangle(k, radius), at = RitualArmamentChoreography.Triangle(k, RitualArmamentChoreography.VerdictRadius);
            corners[k] = s.Center + new Vector2(c.X, c.Y);
            footprint[k] = s.Center + new Vector2(at.X, at.Y);
        }
        bool live = RitualArmamentChoreography.VerdictLive(t);
        float fade = WitnessRules.JudgementFade(t);

        // Forecast: the live footprint as a pearl-violet hairline from the call until the execution.
        if (t < RitualArmamentChoreography.VerdictEndHit)
        {
            float progress = WitnessRules.AuraRise(t * 1.6f);
            for (int k = 0; k < 3; k++) canvas.Forecast(footprint[k], footprint[(k + 1) % 3], progress, end);
        }
        // The auras: wavering hairlines rise 300 px above each corner where the swords will fall.
        if (t < RitualArmamentChoreography.VerdictLock + 2)
        {
            float rise = WitnessRules.AuraRise(t), height = WitnessRules.StakeHeight(t);
            for (int k = 0; k < 3; k++)
            {
                float waver = MathF.Sin(t * .7f + k * 2.1f) * 2;
                Vector2 bottom = corners[k] + new Vector2(waver, 0);
                Vector2 top = corners[k] + new Vector2(-waver, -WitnessRules.StakeDrop * rise);
                if (t < RitualArmamentChoreography.VerdictLock) canvas.Forecast(top, bottom, 1, end);
                // The falling sword's light streak.
                if (t >= WitnessRules.StakeFallStart && t < RitualArmamentChoreography.VerdictLock && s.Energy is { } fall)
                {
                    Vector2 pommel = corners[k] + new Vector2(0, -height - WitnessRules.SwordLength);
                    canvas.EnergyQuad(fall, RibbonPass, pommel + new Vector2(0, -70), pommel + new Vector2(0, 30), 7,
                        new Vector4(.8f, light, 2, .9f));
                }
            }
        }
        // The swords: fade in at the top, fall point-first, stake the corners, ride the closing, withdraw upward.
        if (s.Sword is { } sword && t >= WitnessRules.SwordAppear)
        {
            float height = WitnessRules.StakeHeight(t), withdraw = WitnessRules.Withdraw(t);
            for (int k = 0; k < 3; k++)
            {
                Vector2 point = corners[k] + new Vector2(0, -height - 60 * withdraw);
                canvas.Sprite(new DollSprite(sword, sword.Bounds, SwordPivot), point, 0, DollFlip.None, DollStratum.Front, 1,
                    new DollSpriteFx
                    {
                        Fade = 1 - WitnessRules.SwordReveal(t), Hide = withdraw, HideFromBottom = true,
                        Flash = t >= RitualArmamentChoreography.VerdictHit && t < RitualArmamentChoreography.VerdictHit + 4 ? .7f : 0,
                    });
                // A light outline beside the dark steel keeps the blade readable on dark ground (pearl-violet while it
                // falls, lilac once staked); it withdraws with the blade, which crops away into light from the point up.
                float cropped = WitnessRules.SwordLength * withdraw, guardAt = -WitnessRules.SwordGuard.Y - 6;
                if (cropped < guardAt - 4)
                {
                    DollTone rim = t < RitualArmamentChoreography.VerdictLock ? DollTone.PearlViolet : DollTone.Lilac;
                    float rimAlpha = WitnessRules.SwordReveal(t) * end;
                    canvas.Line(point + new Vector2(4, -guardAt), point + new Vector2(4, -MathF.Max(cropped, 6)), rim, 1, rimAlpha);
                    canvas.Line(point + new Vector2(-5, -guardAt), point + new Vector2(-5, -MathF.Max(cropped, 6)), rim, 1, rimAlpha);
                }
                if (withdraw > 0 && withdraw < 1)
                    canvas.Dot(point + new Vector2(0, -cropped), DollTone.White, 2, light);
            }
        }
        // The stakes land: corner stars, ring bursts and cracked porcelain ground.
        float since = t - RitualArmamentChoreography.VerdictLock;
        if (since >= 0 && since < 10)
        {
            float u = since / 10f;
            for (int k = 0; k < 3; k++)
            {
                for (int ray = 0; ray < 4; ray++)
                {
                    Vector2 d = Unit(ray * MathF.PI / 2 + MathF.PI / 4 * (k % 2));
                    canvas.Line(corners[k] + d * 3, corners[k] + d * (6 + 16 * RitualKineticMotion.Arrive(u)), ray % 2 == 0 ? DollTone.White : DollTone.PearlViolet,
                        u < .3f ? 2 : 1, light * (1 - u));
                }
                canvas.Ring(corners[k], 6 + 20 * RitualKineticMotion.Arrive(u), DollTone.PearlViolet, 1, light * (1 - u));
                canvas.Burst(corners[k], s.Seed + k * 7, 8, since, 12, 2.6f, .14f, DollShardKind.Porcelain, 2.6f, -MathF.PI / 2);
            }
        }
        // The execution, under the edges: opaque pearl craquelure over a translucent porcelain ground, drawn toward the
        // centre, and a small black eye that opens and shuts within ten ticks; the residue cools away by JudgementCool.
        // Then the edges: light written from stake to stake, closing with the stakes, flaring at the execution.
        if (s.Energy is { } energy && t >= WitnessRules.EdgeWriteStart)
        {
            if (t >= RitualArmamentChoreography.VerdictHit && fade > 0)
            {
                float collapse = s.Struck ? Clamp01((t - RitualArmamentChoreography.VerdictHit) / (RitualArmamentChoreography.VerdictEndHit - RitualArmamentChoreography.VerdictHit + 3)) : 0;
                float eye = s.Struck ? WitnessRules.ExecutionEye(t) * WitnessRules.EyeRadius : 0;
                Fill(canvas, energy, s.Center, corners, radius, eye, VoidAlpha(in s) * end,
                    new Vector4((s.Struck ? 1 : .55f) * (.3f + .7f * fade), light * fade, collapse, (s.Seed & 255) / 255f));
            }
            float write = WitnessRules.EdgeWrite(t);
            float hot = t > RitualArmamentChoreography.VerdictEndHit ? fade : 1;
            float width = live ? 14 : 6 + 4 * RitualKineticMotion.VerdictClosure(t);
            float intensity = live ? 1 : t > RitualArmamentChoreography.VerdictEndHit ? .25f + .65f * fade : .85f;
            if (light * hot > .004f)
                for (int k = 0; k < 3; k++)
                    canvas.EnergyQuad(energy, RibbonPass, corners[k], corners[(k + 1) % 3], width,
                        new Vector4(intensity, light * hot, write < 1 ? write : 2, 0));
        }
        if (live && s.Struck)
        {
            float u = (t - RitualArmamentChoreography.VerdictHit) / 3f;
            for (int k = 0; k < 3; k++)
                canvas.Burst(Vector2.Lerp(corners[k], corners[(k + 1) % 3], .5f), s.Seed + 50 + k, 10, u * 3, 14, 3.2f, .05f, DollShardKind.Spark);
        }
        return true;
    }

    // FillPass vertices: L = (x, y from the centre in dots, circumradius in dots, ground alpha), S = (dot position, eye
    // radius in dots, void alpha), T = style. Every value is per frame on the CPU; the shader only reads them.
    private static void Fill(DollWeaponCanvas canvas, IDollEnergyMaterial energy, Vector2 center, ReadOnlySpan<Vector2> corners, float radius,
        float eyeRadius, float voidAlpha, Vector4 style)
    {
        Span<DollPixelVertex> triangle = canvas.Energy(energy, FillPass, 1);
        if (triangle.IsEmpty) return;
        Vector2 middle = canvas.ToDot(center);
        float dots = radius * DollWeaponCanvas.DotScale, eye = eyeRadius * DollWeaponCanvas.DotScale;
        for (int i = 0; i < 3; i++)
        {
            Vector2 p = canvas.ToDot(corners[i]);
            triangle[i].Position = new Vector3(p, 0);
            triangle[i].Local = new Vector4(p.X - middle.X, p.Y - middle.Y, dots, FillGroundAlpha);
            triangle[i].Shape = new Vector4(p.X, p.Y, eye, voidAlpha);
            triangle[i].Style = style;
        }
    }

    // ---- Helpers --------------------------------------------------------------------------------------

    private static float LightAlpha(in WitnessDrawState s) => s.Peer ? DollWeaponCanvas.PeerLightAlpha : 1;
    private static float VoidAlpha(in WitnessDrawState s) => s.Peer ? DollWeaponCanvas.PeerVoidAlpha : 1;

    internal static Rectangle ShardFrame(int shape)
        => new(Math.Clamp(shape, 0, 2) * DollArtAnchors.WitnessShards.FrameWidth, 0, DollArtAnchors.WitnessShards.FrameWidth, DollArtAnchors.WitnessShards.FrameHeight);

    // A blade-frame point (px from the pivot: +x toward the tip, +y toward the edge) in the world.
    private static Vector2 Local(Vector2 center, float angle, float facing, NVector2 local)
    {
        Vector2 along = Unit(angle), edge = new Vector2(-along.Y, along.X) * facing;
        return center + along * local.X + edge * local.Y;
    }

    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    private static Vector2 X(NVector2 v) => new(v.X, v.Y);
    private static float Clamp01(float v) => float.IsFinite(v) ? Math.Clamp(v, 0f, 1f) : 0f;
}

// The DollWitnessEnergy material for canvas.Energy batches, bound by pass index (WitnessPresentation.RibbonPass,
// FillPass). The game resolves the Luminance shader on each use; the offline preview passes the compiled effect.
internal sealed class WitnessEnergyMaterial : IDollEnergyMaterial
{
    internal const string ShaderName = "Convergence.DollWitnessEnergy";
    // The noise clock wraps here (ten minutes of ticks), well inside float precision.
    internal const double ClockPeriod = 36000;
    private readonly Func<Effect?> resolve;

    internal WitnessEnergyMaterial(Func<Effect?> resolve) => this.resolve = resolve;

    public bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass)
    {
        Effect? effect = resolve();
        if (effect is null || effect.IsDisposed || pass < 0 || pass >= effect.CurrentTechnique.Passes.Count) return false;
        DollPixelArt.Set(effect, "uWorldViewProjection", context.Projection);
        DollPixelArt.Set(effect, "dotOrigin", context.DotOrigin);
        DollPixelArt.Set(effect, "clock", (float)(context.Clock % ClockPeriod));
        DollPixelArt.Set(effect, "reduced", context.Reduced ? 1f : 0f);
        effect.CurrentTechnique.Passes[pass].Apply();
        return true;
    }
}
