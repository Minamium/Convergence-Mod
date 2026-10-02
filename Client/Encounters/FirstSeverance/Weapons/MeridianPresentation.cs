#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// What the Pale Meridian draws with, resolved by the caller for the frame (the game reads DollWeaponTextures and the
// Luminance material; the offline preview loads the same files). A missing texture skips its sprite; without the
// material the bodies fall back to the layer's built-in ramp and the glows are skipped.
internal sealed class MeridianArt
{
    internal Texture2D? Gun, Bare, Parts, Key;
    internal IDollEnergyMaterial? Energy;
}

// The held gun at one draw fraction. Pivot = the owner's rotated centre as drawn; Aim = the drawn aim (radians);
// Age = the fractional score age (frozen at the release); Stowed = fractional ticks since the release (0 while held,
// Released false); Fired = the finisher tier that fired (0 = none).
internal readonly record struct MeridianGunView(Vector2 Pivot, float Aim, float Age, bool Released, float Stowed, int Fired,
    float Gravity, bool Peer, int Seed);

// A round at one draw fraction: the drawn head, its velocity per game tick and its age in ticks.
internal readonly record struct MeridianRoundView(Vector2 Head, Vector2 Velocity, MeridianShot Kind, float Age, bool Peer, int Seed);

// A release line: the meridian or the lattice (ages as in PaleMeridianLattice, fractional).
internal readonly record struct MeridianLineView(Vector2 Origin, Vector2 Direction, float Node, int Tier, bool Lattice, float Age,
    bool Peer, int Seed);

// Pale Meridian presentation (docs/encounters/first-severance/WEAPONS.md "Pale Meridian"), recorded into the shared
// Doll weapon layer. The exported pixel art (k = 3 gun, 1 texel = 2 world px, never scaled) is placed by its muzzle
// anchor on the design muzzle (PaleMeridianRig), so the art follows the hit shapes. Light goes through the original
// DollMeridianEnergy material and the layer's lines, rings and debris. Every input is a view of replicated or owner
// state; nothing here touches hits, input, ammo or packets. Depends only on FNA and the pure Pale Meridian rules, so
// the offline preview links it.
internal static class MeridianPresentation
{
    internal const int PartCellWidth = DollArtAnchors.MeridianParts.FrameWidth, PartCellHeight = DollArtAnchors.MeridianParts.FrameHeight;
    internal const int KeyFrameWidth = DollArtAnchors.MeridianKey.FrameWidth, KeyFrameHeight = DollArtAnchors.MeridianKey.FrameHeight;
    // Recoil (px back along the aim) and muzzle climb (rad) per shot, eased in and out.
    internal const float NoteKick = 3, RoundKick = 1.5f, HeavyKick = 5, HeavyClimb = .05f, StrikeKick = 14, StrikeClimb = .10f;
    // Pack-away beats (ticks after the release): parts pop off, the key sinks, the bare gun fades by StowTicks.
    internal const int EjectAfterStrike = PaleMeridianLattice.MeridianFire + 1, EjectAfterMiss = 2, FadeStart = 16;
    internal const float ResidueLife = 18;
    internal const sbyte KeyDepth = -1, GunDepth = 0, PartDepth = 1, FlightDepth = 2;
    internal const sbyte ResidueLight = -1, BodyLight = 0, GlowLight = 1;

    // Part sizes in texels inside their 47x6 cells, from the export (cylinder, shroud, ring sight, spring housing).
    private static readonly Point[] partSize = { new(5, 5), new(47, 2), new(4, 6), new(3, 6) };

    private static Vector2 X(NVector2 v) => new(v.X, v.Y);
    private static NVector2 N(Vector2 v) => new(v.X, v.Y);
    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    // ---- The held gun --------------------------------------------------------------------------

    // Where the gun is drawn: its muzzle anchor, rotation and mirror, and its top side.
    internal readonly record struct GunPose(Vector2 Muzzle, Vector2 Grip, float Rotation, DollFlip Flip, Vector2 Axis, Vector2 Top, bool Mirrored);

    internal static GunPose Pose(in MeridianGunView view)
    {
        Vector2 axis = Unit(view.Aim);
        bool mirrored = PaleMeridianRig.Mirrored(N(axis), view.Gravity);
        NVector2 pivot = N(view.Pivot);
        Vector2 muzzle = X(PaleMeridianRig.World(pivot, N(axis), mirrored, PaleMeridianRig.Muzzle));
        Vector2 grip = X(PaleMeridianRig.World(pivot, N(axis), mirrored, PaleMeridianRig.Grip));
        Vector2 top = X(PaleMeridianRig.World(NVector2.Zero, N(axis), mirrored, NVector2.UnitY));
        // The bare gun slides into the hand from behind and below.
        float arrive = PaleMeridianScore.Arrive(view.Age / PaleMeridianScore.ArriveTicks);
        Vector2 slide = -axis * (1 - arrive) * 32 - top * (1 - arrive) * 10;
        // Packing away it sinks a little as it fades.
        float sink = view.Released ? PaleMeridianScore.Smooth((view.Stowed - FadeStart + 4) / 10) * 6 : 0;
        Recoil(view, out float back, out float climb);
        Vector2 shift = slide - axis * back - top * sink;
        float signed = mirrored ? climb : -climb;
        Vector2 gripAt = grip + shift;
        Vector2 muzzleAt = gripAt + Vector2.Transform(muzzle - grip, Matrix.CreateRotationZ(signed));
        Vector2 drawnAxis = Unit(view.Aim + signed);
        Vector2 drawnTop = X(PaleMeridianRig.World(NVector2.Zero, N(drawnAxis), mirrored, NVector2.UnitY));
        return new GunPose(muzzleAt, gripAt, view.Aim + signed, mirrored ? DollFlip.Vertical : DollFlip.None, drawnAxis, drawnTop, mirrored);
    }

    // World position of a gun texel under the pose (the same transform the canvas applies to the sprite).
    internal static Vector2 Texel(in GunPose pose, Vector2 texel)
        => X(DollSpritePlacement.World(N(texel), DollArtAnchors.MeridianGun.Muzzle, N(pose.Muzzle), pose.Rotation, pose.Flip));

    private static float Kick(float t) => t < 0 ? 0 : PaleMeridianScore.Arrive(t / 1.5f) * (1 - PaleMeridianScore.Smooth((t - 1.5f) / 10));

    private static void Recoil(in MeridianGunView view, out float back, out float climb)
    {
        back = climb = 0;
        int now = view.Released ? 0 : (int)MathF.Floor(view.Age);
        for (int age = Math.Max(1, now - 13); age <= now; age++)
        {
            MeridianShot shot = PaleMeridianScore.Shot(age);
            if (shot == MeridianShot.None) continue;
            float k = Kick(view.Age - age);
            back += k * (shot == MeridianShot.Note ? NoteKick : shot == MeridianShot.Heavy ? HeavyKick : RoundKick);
            if (shot == MeridianShot.Heavy) climb += k * HeavyClimb;
        }
        if (view.Released && view.Fired > 0)
        {
            float k = Kick(view.Stowed - PaleMeridianLattice.MeridianFire);
            back += k * StrikeKick;
            climb += k * StrikeClimb;
        }
    }

    // The latest shot at or before `age` within `window` ticks, or -1.
    private static int LastShot(float age, int window, out MeridianShot shot)
    {
        shot = MeridianShot.None;
        int now = (int)MathF.Floor(age);
        for (int a = now; a >= Math.Max(1, now - window); a--)
        {
            shot = PaleMeridianScore.Shot(a);
            if (shot != MeridianShot.None) return a;
        }
        return -1;
    }

    private static int LastNote(float age, int window)
    {
        int now = (int)MathF.Floor(age);
        for (int a = now; a >= Math.Max(1, now - window); a--)
            if (PaleMeridianScore.Note(a) >= 0) return a;
        return -1;
    }

    internal static void EmitGun(DollWeaponCanvas canvas, MeridianArt art, in MeridianGunView view)
    {
        if (!float.IsFinite(view.Age) || !float.IsFinite(view.Aim) || !float.IsFinite(view.Pivot.X) || !float.IsFinite(view.Pivot.Y)) return;
        GunPose pose = Pose(view);
        float age = view.Age, stowed = view.Released ? view.Stowed : 0;
        int parts = PaleMeridianScore.Parts(age);
        int ejectAt = view.Fired > 0 ? EjectAfterStrike : EjectAfterMiss;
        bool ejected = view.Released && stowed >= ejectAt;
        bool complete = parts == PaleMeridianScore.PartCount && !ejected && art.Gun is not null;
        float fade = Math.Max(view.Released ? (stowed - FadeStart) / (PaleMeridianScore.StowTicks - FadeStart) : 0, 1 - age / 4);
        fade = Math.Clamp(fade, 0, 1);
        if (fade >= 1) return;
        float flash = GunFlash(view, ejectAt);
        Texture2D? body = complete ? art.Gun : art.Bare ?? art.Gun;
        if (body is not null)
            canvas.Sprite(new DollSprite(body, body.Bounds, X(DollArtAnchors.MeridianGun.Muzzle)), pose.Muzzle, pose.Rotation, pose.Flip,
                DollStratum.Front, GunDepth, new DollSpriteFx { Flash = flash, Fade = fade });
        EmitParts(canvas, art, view, pose, parts, complete, ejected, ejectAt, fade);
        EmitKey(canvas, art, view, pose, fade);
        EmitGunLight(canvas, art, view, pose, parts, complete);
    }

    private static float GunFlash(in MeridianGunView view, int ejectAt)
    {
        float age = view.Age, flash = 0;
        // The fourth seat completes the gun under a flash (the bare gun and parts swap to the assembled art there).
        float seat = age - PaleMeridianScore.Seats[PaleMeridianScore.PartCount - 1];
        if (seat >= 0 && seat < 6) flash = Math.Max(flash, .45f * (1 - seat / 6));
        float ignite = age - PaleMeridianScore.Ignite;
        if (ignite >= 0 && ignite < 8 && !view.Released) flash = Math.Max(flash, .5f * (1 - ignite / 8));
        if (view.Released)
        {
            float strike = view.Stowed - PaleMeridianLattice.MeridianFire;
            if (view.Fired > 0 && strike >= 0 && strike < 3) flash = Math.Max(flash, .3f * (1 - strike / 3));
            float eject = view.Stowed - ejectAt;
            if (eject >= -1 && eject < 1) flash = Math.Max(flash, .3f);
        }
        return flash;
    }

    private static void EmitParts(DollWeaponCanvas canvas, MeridianArt art, in MeridianGunView view, in GunPose pose, int parts,
        bool complete, bool ejected, int ejectAt, float fade)
    {
        Texture2D? strip = art.Parts;
        if (strip is null) return;
        float age = view.Age;
        Vector2 muzzleTexel = X(DollArtAnchors.MeridianGun.Muzzle);
        int lastNote = LastNote(age, 3);
        for (int i = 0; i < PaleMeridianScore.PartCount; i++)
        {
            Vector2 seat = X(DollArtAnchors.MeridianParts.Seats[i]);
            var source = new Rectangle(i * PartCellWidth, 0, PartCellWidth, PartCellHeight);
            var sprite = new DollSprite(strip, source, muzzleTexel - seat);
            int launch = PaleMeridianScore.Launch(i), seatTick = PaleMeridianScore.Seats[i];
            if (age >= seatTick)
            {
                if (complete) continue;
                if (!ejected)
                {
                    // Seated: flashes on its seat, then pulses on every note while the gun is still being built.
                    float flash = Math.Max(0, 1 - (age - seatTick) / 6);
                    if (lastNote >= 0 && !view.Released) flash = Math.Max(flash, .45f * (1 - (age - lastNote) / 3));
                    canvas.Sprite(sprite, pose.Muzzle, pose.Rotation, pose.Flip, DollStratum.Front, PartDepth,
                        new DollSpriteFx { Flash = flash, Fade = fade });
                    continue;
                }
                // Popped off after the release: a short ballistic arc, spinning, fading out.
                float t = view.Stowed - ejectAt;
                Vector2 centre = Texel(pose, seat + new Vector2(partSize[i].X, partSize[i].Y) * .5f);
                Vector2 velocity = pose.Top * (3.2f + .6f * i) + pose.Axis * (.9f * (i - 1.5f) - 1.2f);
                Vector2 at = centre + velocity * t + new Vector2(0, .16f * MathF.Sign(view.Gravity) * t * t);
                float spin = pose.Rotation + t * (.22f + .05f * i) * (i % 2 == 0 ? 1 : -1);
                var loose = new DollSprite(strip, source, new Vector2(partSize[i].X, partSize[i].Y) * .5f);
                canvas.Sprite(loose, at, spin, pose.Flip, DollStratum.Front, FlightDepth,
                    new DollSpriteFx { Fade = Math.Clamp((t - 4) / 9, 0, 1), Flash = Math.Max(0, .6f - t / 4) });
                continue;
            }
            if (age < launch || view.Released) continue;
            // In flight: violet afterimages trail the part; it spins to rest about its own centre as it arrives.
            Vector2 half = new Vector2(partSize[i].X, partSize[i].Y) * .5f;
            Vector2 seated = Texel(pose, seat + half);
            var flying = new DollSprite(strip, source, half);
            for (int echo = 2; echo >= 0; echo--)
            {
                float a = age - echo * 1.5f;
                if (a < launch || echo > 0 && a - launch > PaleMeridianScore.FlightApproach - 1) continue;
                Vector2 offset = X(PaleMeridianScore.PartOffset(a, i));
                float progress = PaleMeridianScore.Arrive((a - launch) / PaleMeridianScore.FlightApproach);
                float spin = (1 - progress) * (i % 2 == 0 ? 1.6f : -1.3f);
                Vector2 at = seated + pose.Axis * offset.X + pose.Top * offset.Y;
                var fx = echo == 0 ? new DollSpriteFx { Fade = fade }
                    : new DollSpriteFx { Silhouette = echo == 1 ? DollTone.Lilac : DollTone.Violet, Fade = echo == 1 ? .35f : .6f };
                canvas.Sprite(flying, at, pose.Rotation + spin, pose.Flip, DollStratum.Front, (sbyte)(FlightDepth - echo), fx);
            }
        }
    }

    // The wind-up key on the spring housing: it rises at KeyRise, ratchets through the wind, spins in overcharge, then
    // unwinds and sinks back after the release.
    internal static float KeyAngle(in MeridianGunView view)
    {
        float angle = PaleMeridianScore.KeyAngle(view.Age);
        if (view.Released) angle -= MathF.Min(view.Stowed, 8) * MathF.PI / 4;
        return angle;
    }

    internal static float KeyRise(in MeridianGunView view)
    {
        float rise = view.Age >= PaleMeridianScore.KeyRise ? PaleMeridianScore.KeyRiseAmount(view.Age) : 0;
        if (view.Released) rise *= 1 - PaleMeridianScore.Smooth((view.Stowed - (view.Fired > 0 ? 8 : 2)) / 6);
        return rise;
    }

    private static void EmitKey(DollWeaponCanvas canvas, MeridianArt art, in MeridianGunView view, in GunPose pose, float fade)
    {
        Texture2D? key = art.Key;
        float rise = KeyRise(view);
        if (key is null || rise <= .01f) return;
        int frame = PaleMeridianScore.KeyFrame(KeyAngle(view));
        Vector2 seat = Texel(pose, X(DollArtAnchors.MeridianGun.KeySeat));
        float sunk = (1 - rise) * KeyFrameHeight;
        Vector2 foot = seat - pose.Top * sunk * DollSpritePlacement.WorldPerTexel;
        canvas.Sprite(new DollSprite(key, new Rectangle(frame * KeyFrameWidth, 0, KeyFrameWidth, KeyFrameHeight), X(DollArtAnchors.MeridianKey.ShaftBottom)),
            foot, pose.Rotation, pose.Flip, DollStratum.Front, KeyDepth,
            new DollSpriteFx { Hide = sunk / KeyFrameHeight, HideFromBottom = true, Fade = fade });
    }

    // The key's lobes, for sparks (texel row 4 of the frame, above its foot).
    private static Vector2 KeyTop(in GunPose pose, float rise)
        => Texel(pose, X(DollArtAnchors.MeridianGun.KeySeat)) + pose.Top * (KeyFrameHeight - 6) * rise * DollSpritePlacement.WorldPerTexel;

    private static void EmitGunLight(DollWeaponCanvas canvas, MeridianArt art, in MeridianGunView view, in GunPose pose, int parts, bool complete)
    {
        float age = view.Age, alpha = view.Peer ? DollWeaponCanvas.PeerLightAlpha : 1;
        Vector2 muzzle = pose.Muzzle + pose.Axis * 2;
        int seed = view.Seed;
        if (!view.Released)
        {
            // The latest round: an iris closing on the muzzle and a short flash ahead of it.
            int shotAge = LastShot(age, 6, out MeridianShot shot);
            if (shotAge >= 0)
            {
                float t = age - shotAge;
                if (t < 5)
                {
                    DollTone tone = t < 1.5f ? DollTone.White : t < 3 ? DollTone.PearlViolet : DollTone.Lilac;
                    canvas.Ring(muzzle, MathF.Max(2, 11 - 1.6f * t), tone, 1, alpha);
                }
                if (t < 2) canvas.Dot(muzzle, DollTone.White, 2, alpha);
                if (t < 4) Glow(canvas, art, muzzle + pose.Axis * 6, shot == MeridianShot.Heavy ? 13 : 8, 1 - t / 4, 0, alpha, seed, GlowLight);
            }
            // A pin of light runs along the barrel shroud on every note once the shroud is seated.
            int note = LastNote(age, 5);
            if (note >= 0 && parts >= 2)
            {
                float t = (age - note) / 5;
                Vector2 rear = Texel(pose, new Vector2(38, 6.5f)), front = Texel(pose, new Vector2(82, 6.5f));
                Vector2 pin = Vector2.Lerp(rear, front, PaleMeridianScore.Arrive(t));
                if (t < .8f) canvas.Line(Vector2.Lerp(rear, front, PaleMeridianScore.Arrive(t - .25f)), pin, DollTone.PearlViolet, 1, alpha);
                canvas.Dot(pin, DollTone.White, 1, alpha);
            }
            EmitSeatStars(canvas, view, pose, alpha);
            EmitWind(canvas, art, view, pose, muzzle, alpha);
            if (age >= PaleMeridianScore.Ignite) EmitOvercharge(canvas, art, view, pose, muzzle, alpha);
            return;
        }
        float s = view.Stowed;
        if (view.Fired > 0)
        {
            // The packet gathers at the muzzle through the forecast, then the strike flares.
            float gather = s / PaleMeridianLattice.MeridianFire;
            if (gather < 1) Glow(canvas, art, muzzle + pose.Axis * 3, 3 + 7 * gather, .45f + .55f * gather, 0, alpha, seed, GlowLight);
            // (The strike's own flare is the line's: MeridianPresentation.EmitMeridian.)
            // The spring unwinds: brass sparks fly off the turning key.
            if (s < 8 && KeyRise(view) > .2f)
                canvas.Burst(KeyTop(pose, KeyRise(view)), seed * 31 + (int)(s / 2), 4, s % 2 + .5f, 10, 2.2f, .12f, DollShardKind.Brass, 2.2f,
                    MathF.Atan2(pose.Top.Y, pose.Top.X));
        }
        else if (parts > 0 && s < 16)
            canvas.Burst(Texel(pose, new Vector2(30, 6)), seed * 17 + 3, 7, s, 18, 1.8f, .16f, DollShardKind.Brass, MathF.PI, MathF.Atan2(pose.Top.Y, pose.Top.X));
    }

    private static void EmitSeatStars(DollWeaponCanvas canvas, in MeridianGunView view, in GunPose pose, float alpha)
    {
        for (int i = 0; i < PaleMeridianScore.PartCount; i++)
        {
            float t = view.Age - PaleMeridianScore.Seats[i];
            if (t < 0 || t >= 10) continue;
            Vector2 seat = X(DollArtAnchors.MeridianParts.Seats[i]);
            Vector2 centre = Texel(pose, seat + new Vector2(partSize[i].X, partSize[i].Y) * .5f);
            Star(canvas, centre, (10 - t) * 1.9f, t < 3 ? DollTone.White : DollTone.PearlViolet, alpha, t * .05f);
            if (t < 3) canvas.Dot(centre, DollTone.White, 2, alpha);
        }
    }

    // An 8-ray star (4 under Reduced Effects): long cardinal rays and short diagonals.
    private static void Star(DollWeaponCanvas canvas, Vector2 centre, float length, DollTone tone, float alpha, float turn)
    {
        if (length < 2) return;
        int rays = canvas.Reduced ? 4 : 8;
        for (int r = 0; r < rays; r++)
        {
            float angle = turn + r * MathF.Tau / rays;
            float reach = canvas.Reduced || r % 2 == 0 ? length : length * .55f;
            canvas.Line(centre + Unit(angle) * 3, centre + Unit(angle) * reach, tone, 1, alpha);
        }
    }

    private static void EmitWind(DollWeaponCanvas canvas, MeridianArt art, in MeridianGunView view, in GunPose pose, Vector2 muzzle, float alpha)
    {
        float age = view.Age;
        if (age < PaleMeridianScore.KeyRise || age >= PaleMeridianScore.Ignite) return;
        float progress = PaleMeridianScore.KeyAngle(age) / (3 * MathF.PI);
        // A violet iris tightens on the muzzle with every ratchet step while pressure gathers in it.
        float radius = 24 - 17 * progress, spin = -PaleMeridianScore.KeyAngle(age) * .5f;
        for (int arc = 0; arc < 3; arc++)
        {
            float start = spin + arc * MathF.Tau / 3;
            canvas.Arc(muzzle, radius, start, start + 1.25f, progress > .66f ? DollTone.PearlViolet : DollTone.Lilac, 1, alpha);
        }
        Glow(canvas, art, muzzle + pose.Axis * 4, 3 + 8 * progress, .35f + .65f * progress, 0, alpha, view.Seed, GlowLight);
        int step = PaleMeridianScore.WindStep(age);
        if (step <= 0) return;
        float t = age - PaleMeridianScore.WindTick(step);
        if (t >= 0 && t < 10)
            canvas.Burst(KeyTop(pose, KeyRise(view)), view.Seed * 13 + step, 5, t + .5f, 10, 2.4f, .12f, DollShardKind.Brass, 1.6f,
                MathF.Atan2(pose.Top.Y, pose.Top.X));
    }

    private static void EmitOvercharge(DollWeaponCanvas canvas, MeridianArt art, in MeridianGunView view, in GunPose pose, Vector2 muzzle, float alpha)
    {
        float t = view.Age - PaleMeridianScore.Ignite;
        if (t < 12)
        {
            // The spring lets go: a ring bursts off the muzzle and sparks scatter.
            float radius = 6 + 40 * PaleMeridianScore.Arrive(t / 10);
            DollTone tone = t < 2 ? DollTone.White : t < 4 ? DollTone.PearlViolet : t < 6 ? DollTone.Lilac : t < 9 ? DollTone.Violet : DollTone.PlumLight;
            canvas.Ring(muzzle, radius, tone, t < 4 ? 2 : 1, alpha);
            canvas.Burst(muzzle, view.Seed * 7 + 1, 14, t, 16, 3.4f, .05f, DollShardKind.Spark);
        }
        // The open muzzle glows violet and pulses with every round.
        int shotAge = LastShot(view.Age, 3, out MeridianShot shot);
        float pulse = shotAge >= 0 ? 1 - (view.Age - shotAge) / 3 : 0;
        Glow(canvas, art, muzzle + pose.Axis * 5, 9 + 3 * pulse + (shot == MeridianShot.Heavy ? 5 * pulse : 0), .6f + .4f * pulse,
            shot == MeridianShot.Heavy ? .9f - .3f * pulse : 0, alpha, view.Seed, GlowLight);
    }

    // ---- Rounds --------------------------------------------------------------------------------

    internal static int TrailPoints(MeridianShot kind) => kind switch { MeridianShot.Note => 4, MeridianShot.Heavy => 5, _ => 3 };

    // The wake: a pearl pin fading to violet along the path the round actually flew (trail oldest first).
    internal static void EmitRoundWake(DollWeaponCanvas canvas, MeridianArt art, in MeridianRoundView view, ReadOnlySpan<Vector2> trail)
    {
        if (!float.IsFinite(view.Head.X) || !float.IsFinite(view.Head.Y)) return;
        Span<Vector2> spine = stackalloc Vector2[8];
        int n = 0;
        for (int i = 0; i < trail.Length && n < spine.Length - 1; i++)
        {
            Vector2 p = trail[i];
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || Vector2.DistanceSquared(p, view.Head) > 400 * 400) continue;
            if (n > 0 && Vector2.DistanceSquared(p, spine[n - 1]) < 4) continue;
            spine[n++] = p;
        }
        if (n == 0) spine[n++] = view.Head - view.Velocity * Math.Clamp(view.Age, .5f, 1.5f);
        spine[n++] = view.Head;
        if (Vector2.DistanceSquared(spine[0], view.Head) < 16) return;
        float width = view.Kind switch { MeridianShot.Note => 6, MeridianShot.Heavy => 10, _ => 4 };
        float alpha = view.Peer ? DollWeaponCanvas.PeerLightAlpha : 1;
        canvas.EnergyStrip(art.Energy ?? DollPixelArt.Ramp, MeridianEnergyMaterial.WakePass, spine[..n], width,
            new Vector4(1, alpha, 0, (view.Seed & 255) / 255f), BodyLight);
    }

    internal static void EmitRoundHead(DollWeaponCanvas canvas, MeridianArt art, in MeridianRoundView view)
    {
        if (!float.IsFinite(view.Head.X) || !float.IsFinite(view.Head.Y)) return;
        float alpha = view.Peer ? DollWeaponCanvas.PeerLightAlpha : 1;
        Vector2 direction = view.Velocity.LengthSquared() > 1e-4f ? Vector2.Normalize(view.Velocity) : Vector2.UnitX;
        switch (view.Kind)
        {
            case MeridianShot.Heavy:
                // A three-dot lance with a soft halo.
                Glow(canvas, art, view.Head - direction * 4, 11, .55f, 0, alpha, view.Seed, GlowLight);
                canvas.Line(view.Head - direction * 14, view.Head, DollTone.White, 3, alpha);
                break;
            case MeridianShot.Note:
                canvas.Dot(view.Head, DollTone.White, 2, alpha);
                canvas.Dot(view.Head - direction * 4, DollTone.PearlViolet, 1, alpha);
                break;
            default:
                canvas.Dot(view.Head, DollTone.PearlViolet, 2, alpha);
                canvas.Dot(view.Head, DollTone.White, 1, alpha);
                break;
        }
    }

    // ---- Release lines -------------------------------------------------------------------------

    internal static void EmitLine(DollWeaponCanvas canvas, MeridianArt art, in MeridianLineView view)
    {
        NVector2 origin = N(view.Origin), direction = N(view.Direction);
        if (!PaleMeridianLattice.Valid(origin, direction, view.Node, view.Tier, view.Lattice) || !float.IsFinite(view.Age)) return;
        if (view.Lattice) EmitLattice(canvas, art, view);
        else EmitMeridian(canvas, art, view);
    }

    private static void EmitMeridian(DollWeaponCanvas canvas, MeridianArt art, in MeridianLineView view)
    {
        float alpha = view.Peer ? DollWeaponCanvas.PeerLightAlpha : 1;
        float length = PaleMeridianLattice.Length(view.Node), age = view.Age;
        Vector2 origin = view.Origin, axis = view.Direction, end = origin + axis * length;
        int seed = view.Seed;
        if (age < PaleMeridianLattice.MeridianFire)
        {
            // The forecast: a one-dot pearl-violet hairline drawn out through the cursor, with sparse pearl glints.
            float progress = Math.Clamp((age + 1) / 4, 0, 1);
            canvas.Forecast(origin, end, progress, alpha);
            int glints = canvas.Reduced ? 3 : 6;
            for (int k = 0; k < glints; k++)
            {
                float at = .08f + .87f * Hash01(seed * 59 + k * 7);
                if (at > progress) continue;
                float phase = (age * .25f + Hash01(seed * 31 + k)) % 1;
                canvas.Dot(origin + axis * length * at, phase < .35f ? DollTone.White : DollTone.Pearl, 1, alpha);
            }
            if (view.Tier >= 2) canvas.ForecastArc(origin + axis * view.Node, 10, 0, MathF.Tau, progress, alpha);
            return;
        }
        float s = age - PaleMeridianLattice.MeridianFire;
        PaleMeridianLattice.Packet(s, PaleMeridianLattice.MeridianTransit, PaleMeridianLattice.MeridianTail, out float head, out float passed);
        Residue(canvas, art, origin, axis, 0, length * passed, length, age, PaleMeridianLattice.MeridianFire - 1 + PaleMeridianLattice.MeridianTail,
            PaleMeridianLattice.MeridianTransit, alpha, seed);
        if (head > passed)
        {
            // The packet: white head over a flowing pearl-violet body, cooling toward a plum tail.
            Band(canvas, art, MeridianEnergyMaterial.BodyPass, origin + axis * (length * passed), origin + axis * (length * head),
                PaleMeridianLattice.MeridianWidth, length * passed, new Vector4(.62f, alpha, 0, (seed & 255) / 255f),
                new Vector4(1, alpha, 0, (seed & 255) / 255f), BodyLight);
            if (head < 1) Glow(canvas, art, origin + axis * (length * head), 13, 1, 0, alpha, seed, GlowLight);
            else Glow(canvas, art, end, 13 * (1 - passed), 1 - passed, passed, alpha, seed, GlowLight);
        }
        if (s >= 0 && s < 5) Glow(canvas, art, origin + axis * 4, 17 - s, 1 - s / 5, s / 5, alpha, seed, GlowLight);
    }

    private static void EmitLattice(DollWeaponCanvas canvas, MeridianArt art, in MeridianLineView view)
    {
        float alpha = view.Peer ? DollWeaponCanvas.PeerLightAlpha : 1;
        float age = view.Age;
        if (age < -PaleMeridianLattice.SplitLead) return;
        Vector2 axis = view.Direction, across = new(-axis.Y, axis.X), node = view.Origin + axis * view.Node;
        Span<MeridianLatticeLine> lines = stackalloc MeridianLatticeLine[PaleMeridianLattice.MaxLines];
        int count = PaleMeridianLattice.Lines(view.Tier, lines);
        float opening = PaleMeridianLattice.SplitOpening(age);
        int seed = view.Seed;
        for (int i = 0; i < count; i++)
        {
            MeridianLatticeLine line = lines[i];
            Vector2 direction = line.Perpendicular ? across : axis;
            float half = line.HalfSpan;
            float s = age - PaleMeridianLattice.RippleStep * line.Ring;
            if (s < 0)
            {
                // The split: parallels slide out of the meridian, perpendiculars unfold through the node (harmless).
                Vector2 middle = node + axis * (line.Along * opening) + across * (line.Across * opening);
                float reach = half * (line.Perpendicular ? opening : .4f + .6f * opening);
                canvas.Forecast(middle, middle + direction * reach, 1, alpha);
                canvas.Forecast(middle, middle - direction * reach, 1, alpha);
                continue;
            }
            Vector2 centre = node + axis * line.Along + across * line.Across;
            PaleMeridianLattice.Packet(s, PaleMeridianLattice.LatticeTransit, PaleMeridianLattice.LatticeTail, out float head, out float passed);
            float firstPass = PaleMeridianLattice.RippleStep * line.Ring + PaleMeridianLattice.LatticeTail - 1;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 outward = direction * side;
                Residue(canvas, art, centre, outward, 0, half * passed, half, age, firstPass, PaleMeridianLattice.LatticeTransit, alpha, seed + side);
                if (head > passed)
                    Band(canvas, art, MeridianEnergyMaterial.BodyPass, centre + outward * (half * passed), centre + outward * (half * head),
                        PaleMeridianLattice.LatticeWidth, half * passed, new Vector4(.6f, alpha, 0, ((seed + 7 * side) & 255) / 255f),
                        new Vector4(1, alpha, 0, ((seed + 7 * side) & 255) / 255f), BodyLight);
            }
        }
        if (age < 0) return;
        // Brass glints where the lines cross (the meridian counts as a parallel), and a star at the node.
        float spacing = PaleMeridianLattice.Spacing(view.Tier);
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            if (!lines[i].Perpendicular) continue;
            float s = age - PaleMeridianLattice.RippleStep * lines[i].Ring;
            if (s < 0 || s > 16) continue;
            for (int k = -1; k <= 1; k++, index++)
            {
                if (canvas.Reduced && index % 2 == 1) continue;
                Vector2 crossing = node + axis * lines[i].Along + across * (k * spacing);
                float twinkle = (s * .3f + Hash01(seed * 3 + index)) % 1;
                DollTone tone = s < 3 || twinkle < .3f && s < 12 ? DollTone.White : s < 10 ? DollTone.BrassLight : DollTone.Brass;
                canvas.Dot(crossing, tone, s < 12 ? 2 : 1, alpha);
            }
        }
        if (age < 12)
        {
            Star(canvas, node, (12 - age) * 2.4f, age < 4 ? DollTone.White : DollTone.PearlViolet, alpha, MathF.PI / 8 + age * .04f);
            Glow(canvas, art, node, 14, 1 - age / 10, age / 10, alpha, seed, GlowLight);
        }
    }

    // The trace a packet leaves on [from, to] of a line (start + axis * d) as it cools to plum over ResidueLife ticks
    // (half under Reduced Effects). The tail passed distance d at age firstPass + transit * d / span.
    private static void Residue(DollWeaponCanvas canvas, MeridianArt art, Vector2 start, Vector2 axis, float from, float to, float span,
        float age, float firstPass, int transit, float alpha, int seed)
    {
        if (art.Energy is null || !(to - from > 2)) return;
        float life = canvas.Reduced ? ResidueLife / 2 : ResidueLife;
        float heatFrom = 1 - (age - (firstPass + transit * from / span)) / life;
        float heatTo = 1 - (age - (firstPass + transit * to / span)) / life;
        if (heatFrom <= 0 && heatTo <= 0) return;
        if (heatFrom <= 0)
        {
            // Only the most recently passed stretch is still warm.
            float cold = span * ((age - life - firstPass) / transit);
            from = Math.Clamp(cold, from, to);
            heatFrom = 0;
        }
        float seedValue = (seed & 255) / 255f;
        Band(canvas, art, MeridianEnergyMaterial.ResiduePass, start + axis * from, start + axis * to, 8, from,
            new Vector4(0, alpha, Math.Clamp(heatFrom, 0, 1), seedValue), new Vector4(0, alpha, Math.Clamp(heatTo, 0, 1), seedValue), ResidueLight);
    }

    // ---- Material geometry ---------------------------------------------------------------------

    // A band from a to b, `width` world px wide, for the DollMeridianEnergy bands: L = (along, across, length, half
    // width) in dots, S.z = the band's distance from its line origin (dots), T from `tail` at a to `head` at b.
    private static void Band(DollWeaponCanvas canvas, MeridianArt art, int pass, Vector2 a, Vector2 b, float width, float offset,
        Vector4 tail, Vector4 head, sbyte depth)
    {
        IDollEnergyMaterial material = art.Energy ?? DollPixelArt.Ramp;
        if (art.Energy is null) pass = 0;
        Span<DollPixelVertex> quad = canvas.Energy(material, pass, 2, depth);
        if (quad.IsEmpty) return;
        Vector2 da = canvas.ToDot(a), db = canvas.ToDot(b), delta = db - da;
        float length = delta.Length(), half = MathF.Max(width * DollWeaponCanvas.DotScale * .5f, .5f), grown = half + 1;
        Vector2 direction = length > 1e-3f ? delta / length : Vector2.UnitX, normal = new(-direction.Y, direction.X);
        float along0 = length > 0 ? -1 / length : 0, along1 = length > 0 ? 1 + 1 / length : 1, across = grown / half;
        float z = offset * DollWeaponCanvas.DotScale;
        Vector2 back = da - direction, front = db + direction;
        Vertex(ref quad[0], back - normal * grown, new Vector4(along0, -across, length, half), z, tail);
        Vertex(ref quad[1], front - normal * grown, new Vector4(along1, -across, length, half), z, head);
        Vertex(ref quad[2], back + normal * grown, new Vector4(along0, across, length, half), z, tail);
        quad[3] = quad[2];
        quad[4] = quad[1];
        Vertex(ref quad[5], front + normal * grown, new Vector4(along1, across, length, half), z, head);
    }

    // A round glow of `radius` world px (DollMeridianEnergy only; skipped without the material).
    private static void Glow(DollWeaponCanvas canvas, MeridianArt art, Vector2 centre, float radius, float intensity, float ring, float alpha,
        int seed, sbyte depth)
    {
        if (art.Energy is null || !(radius > 1) || !(intensity > .01f) || !(alpha > .004f) || !float.IsFinite(centre.X) || !float.IsFinite(centre.Y)) return;
        Span<DollPixelVertex> quad = canvas.Energy(art.Energy, MeridianEnergyMaterial.GlowPass, 2, depth);
        if (quad.IsEmpty) return;
        Vector2 c = canvas.ToDot(centre);
        float r = radius * DollWeaponCanvas.DotScale, grown = r + 1, k = grown / r;
        Vector4 style = new(Math.Clamp(intensity, 0, 1), alpha, Math.Clamp(ring, 0, 1), (seed & 255) / 255f);
        Vertex(ref quad[0], c + new Vector2(-grown, -grown), new Vector4(-k, -k, r, 0), 0, style);
        Vertex(ref quad[1], c + new Vector2(grown, -grown), new Vector4(k, -k, r, 0), 0, style);
        Vertex(ref quad[2], c + new Vector2(-grown, grown), new Vector4(-k, k, r, 0), 0, style);
        quad[3] = quad[2];
        quad[4] = quad[1];
        Vertex(ref quad[5], c + new Vector2(grown, grown), new Vector4(k, k, r, 0), 0, style);
    }

    private static void Vertex(ref DollPixelVertex vertex, Vector2 dot, Vector4 local, float offset, Vector4 style)
    {
        vertex.Position = new Vector3(dot, 0);
        vertex.Local = local;
        vertex.Shape = new Vector4(dot.X, dot.Y, offset, 0);
        vertex.Style = style;
    }

    private static float Hash01(int seed)
    {
        uint h = (uint)seed * 747796405u + 2891336453u;
        h = ((h >> (int)((h >> 28) + 4u)) ^ h) * 277803737u;
        return ((h >> 22) ^ h) % 10007 / 10007f;
    }
}

// The DollMeridianEnergy material: binds the compiled effect, the frame's projection/clock and the two noise
// textures (s1, s2, wrapped linear) for a pass. Providers are read on each Apply (the game reads Luminance's shader and
// noise assets there; never a texture cached from an asynchronous request). Returns false when anything is missing,
// so the batch is skipped.
internal sealed class MeridianEnergyMaterial : IDollEnergyMaterial
{
    internal const string ShaderName = "Convergence.DollMeridianEnergy";
    internal const int BodyPass = 0, WakePass = 1, GlowPass = 2, ResiduePass = 3;
    private static readonly string[] passes = { "MeridianBodyPass", "MeridianWakePass", "MeridianGlowPass", "MeridianResiduePass" };

    private readonly Func<Effect?> effect;
    private readonly Func<Texture2D?> noiseA, noiseB;

    internal MeridianEnergyMaterial(Func<Effect?> effect, Func<Texture2D?> noiseA, Func<Texture2D?> noiseB)
    {
        this.effect = effect;
        this.noiseA = noiseA;
        this.noiseB = noiseB;
    }

    public bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass)
    {
        if ((uint)pass >= (uint)passes.Length || effect() is not { IsDisposed: false } shader) return false;
        if (noiseA() is not { IsDisposed: false } a || noiseB() is not { IsDisposed: false } b) return false;
        DollPixelArt.Set(shader, "uWorldViewProjection", context.Projection);
        DollPixelArt.Set(shader, "dotOrigin", context.DotOrigin);
        DollPixelArt.Set(shader, "clock", (float)(context.Clock % 7200.0));
        DollPixelArt.Set(shader, "reduced", context.Reduced ? 1f : 0f);
        device.Textures[1] = a;
        device.SamplerStates[1] = SamplerState.LinearWrap;
        device.Textures[2] = b;
        device.SamplerStates[2] = SamplerState.LinearWrap;
        DollPixelArt.Apply(shader, passes[pass]);
        return true;
    }
}
