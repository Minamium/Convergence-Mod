using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.RitualArmamentArt;

namespace Convergence.Client.Encounters.FirstSeverance;

// Long-form construction and its micro-beats use the same fractional score.
// All extra coils, conduits and machinery are bounded client-only decoration.
internal static class RitualGrandArt
{
    private static Vector2 Center(Projectile p) => p.GetGlobalProjectile<RitualArmamentProjectileVisuals>().Center(p);
    private static Vector2 Axis(Projectile p) => p.GetGlobalProjectile<RitualArmamentProjectileVisuals>().Angle(p).ToRotationVector2();
    internal static void QueueBeam(Vector2 root, Vector2 axis, float length, float width, float age, Color color, float fade,
        float throatLength = 0, float throatWidth = 0)
    {
        if (length <= 0 || width <= 0 || fade <= 0) return;
        Span<Vector2> line = stackalloc Vector2[65];
        for (int i = 0; i < line.Length; i++) line[i] = root + axis * (length * i / (line.Length - 1));
        // One connected turbulent jet, not layered flat ribbons and loose lines.
        RitualSurfacePass.Ribbon(line, width, Light(color, fade), false, throatWidth, throatLength / length);
    }

    private static void Conduit(SpriteBatch batch, Vector2 from, Vector2 to, Vector2 bend, Color light, float clock, float opacity)
    {
        Vector2 previous = from;
        for (int i = 1; i <= 24; i++)
        {
            float t = i / 24f;
            Vector2 next = Vector2.Lerp(Vector2.Lerp(from, bend, t), Vector2.Lerp(bend, to, t), t);
            float moving = .25f + .75f * MathF.Pow(MathF.Max(0, MathF.Sin(t * 20 - clock * .24f)), 3);
            Line(batch, previous, next, Light(light, opacity * moving), 2 + moving * 2);
            previous = next;
        }
    }
    private static void Aperture(SpriteBatch batch, Vector2 at, Vector2 axis, float radius, float clock, Color color, float power)
    {
        float angle = axis.ToRotation();
        RitualKineticArt.Seal(batch, at, radius, angle + MathHelper.PiOver2, color, power);
        Ring(batch, at - axis * 18, new(radius * .17f, radius * .77f), angle,
            Light(Ivory, power * .6f), 3, true);
        Ring(batch, at + axis * 23, new(radius * .11f, radius * .52f), angle,
            Light(color, power * .8f), 4, true);
        int facets = Reduced ? 6 : 12;
        for (int i = 0; i < facets; i++)
        {
            float a = i * MathF.Tau / facets + clock * .018f;
            Vector2 p = at + new Vector2(MathF.Cos(a) * radius * .27f, MathF.Sin(a) * radius).RotatedBy(angle);
            Part(batch, 2, p, angle + a, radius * .20f, Color.White * power * .65f);
        }
    }

    internal static void QueueMagic(LacunaConvergence beam, float age)
        => QueueBeam(Center(beam.Projectile) + Axis(beam.Projectile) * 180, Axis(beam.Projectile), RitualGrandScore.MagicLength * RitualGrandScore.BeamOpening(age, RitualGrandScore.MagicFire),
            RitualGrandScore.MagicWidth * RitualGrandScore.BeamOpening(age, RitualGrandScore.MagicFire), age,
            ColorFor(RitualArmamentKind.Magic), beam.Fade * (Reduced ? .72f : 1));

    internal static void Magic(SpriteBatch batch, LacunaConvergence beam, float age)
    {
        Color color = ColorFor(RitualArmamentKind.Magic);
        Vector2 root = Center(beam.Projectile), axis = Axis(beam.Projectile), muzzle = root + axis * 180;
        float merge = RitualGrandScore.Merge(age), charge = RitualGrandScore.Charge(age);
        float fireAge = age - RitualGrandScore.MagicFire;
        float recoil = RitualKineticMotion.Recoil(fireAge), fade = beam.Fade;
        float born = RitualGrandScore.Arrival(age, 0);
        Relic(batch, RitualArmamentKind.Magic, root + axis * (30 - recoil * 22),
            -.12f + MathF.Sin(age * .015f) * .04f, 135 * born, Color.White * fade);
        for (int i = 0; i < 7; i++)
        {
            float arrival = RitualGrandScore.Arrival(age, RitualGrandScore.SigilBirth(i));
            float visible = arrival * (1 - merge) * fade;
            if (visible <= .001f) continue;
            Vector2 at = beam.Sigil(i, age) + root - beam.Projectile.Center + (axis - beam.Axis) * (180 * merge);
            float local = age - RitualGrandScore.SigilBirth(i);
            float shot = age < RitualGrandScore.MagicMerge
                ? RitualKineticMotion.Recoil((local - 15) % 38) : 0;
            float radius = (64 + i * 5) * arrival * (1 - merge * .7f);
            RitualKineticArt.Seal(batch, at, radius, .14f * MathF.Sin(age * .013f + i) + (1 - arrival) * 1.8f, color, visible);
            RitualKineticArt.Seal(batch, at, radius * .63f, -age * .011f + i, Ivory, visible * .5f);
            Part(batch, 3, at, -age * .008f + i * .7f, radius * .68f, Color.White * visible * .7f);
            Glow(batch, at, new(24 + shot * 58), Light(color, visible * (.32f + shot * .6f)));
            Conduit(batch, root + axis * 30, at, root + new Vector2(-axis.X * 150, at.Y - root.Y), color, age + i * 13, visible * .4f);
            if (shot > 0) RitualKineticArt.Vent(batch, at, axis, (local - 15) % 38, color, false);
        }
        if (age < RitualGrandScore.MagicMerge) return;
        float combined = RitualKineticMotion.Arrive((age - RitualGrandScore.MagicMerge) / 22);
        float opening = RitualGrandScore.BeamOpening(age, RitualGrandScore.MagicFire);
        float radiusMain = (145 - charge * 62 + opening * 77 + recoil * 18) * combined;
        Aperture(batch, muzzle, axis, radiusMain, age, color, combined * fade);
        Glow(batch, muzzle, new Vector2(55 + charge * 56 + opening * 120, 60 + opening * 120),
            Light(color, (.35f + charge * .4f + opening * .45f) * combined * fade * (Reduced ? .55f : 1)));
        FirstSeveranceRaidVfx.Charge(batch, muzzle, axis, age, charge,
            recoil, combined * fade * .7f, color, Reduced, .45f + opening * .25f);
        if (fireAge < 0) return;
        Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
        Line(batch, muzzle - normal * recoil * 250, muzzle + normal * recoil * 250, Light(Ivory, recoil * fade), 8 * recoil);
        RitualKineticArt.Vent(batch, muzzle - axis * 38, axis, fireAge, color, true);
        // Persistent throat/pressure flow, not a new flash for every damage tick.
        for (int i = 0; i < (Reduced ? 2 : 4); i++)
        {
            float t = (fireAge / 32 + i / 4f) % 1;
            Ring(batch, muzzle + axis * (18 + t * 260), new(12 + t * 26, 60 + t * 25), axis.ToRotation(),
                Light(color, (1 - t) * opening * fade * .5f), 3 * (1 - t), true);
        }
    }

    internal static void Battery(SpriteBatch batch, MeridianBastion gun, float age)
    {
        Color color = ColorFor(RitualArmamentKind.Ranged);
        Vector2 root = Center(gun.Projectile), axis = Axis(gun.Projectile), normal = new(-axis.Y, axis.X);
        float born = RitualGrandScore.Arrival(age, 0), fade = gun.Fade;
        float charge = RitualKineticMotion.Strike((age - RitualGrandScore.BatteryLock - 18) / 48);
        float kick = 0;
        for (int back = 0; back <= 11; back++)
            if (RitualGrandScore.BatteryLane((int)age - back) >= 0)
                kick = Math.Max(kick, RitualKineticMotion.Recoil(back + age % 1));
        Vector2 muzzle = gun.Muzzle(0, age, root, axis);
        Vector2 body = root + axis * (25 - kick * 17 - (1 - born) * 90);
        // One receiver, one barrel. Later births dock small breech components,
        // never another complete gun or a large circle behind the player.
        Relic(batch, RitualArmamentKind.Ranged, body, axis.ToRotation() + MathHelper.PiOver4,
            226 * born, Color.White * born * fade);
        for (int i = 1; i < RitualGrandScore.Count(age, false); i++)
        {
            float latch = RitualGrandScore.Arrival(age, RitualGrandScore.BatteryBirth(i));
            int side = i % 2 == 0 ? 1 : -1;
            Vector2 socket = body - axis * (18 + (i / 2) * 27) + normal * side * (18 + charge * 11);
            Vector2 part = socket - axis * (1 - latch) * 85 + normal * side * (1 - latch) * 64;
            Part(batch, 1, part, axis.ToRotation() + MathHelper.PiOver2 + side * charge * .16f,
                66 * latch, Color.White * latch * fade, side < 0);
            Line(batch, socket - axis * 23, socket + axis * 15, Light(color, latch * fade * (.18f + charge * .45f)), 2);
        }
        FirstSeveranceRaidVfx.Charge(batch, muzzle, axis, age, charge,
            kick, born * fade * (charge + kick) * .6f, color, Reduced, .35f);
        Glow(batch, muzzle, new Vector2(24 + kick * 57), Light(color, kick * fade * .72f));
        // A narrow moving packet reads as a gunshot, not a full-size Raid flash.
        if (kick > .01f)
            FirstSeveranceRaidVfx.Beam(batch, muzzle, axis, 60 + kick * 170, 3 + kick * 5,
                age, 1, 1, kick * fade, color, Reduced, mouth: false, fireAge: 0);
    }

    internal static void Choir(SpriteBatch batch, ChoirSentinel choir, Vector2 center, float age)
    {
        float assembly = RitualGrandScore.ChoirAssembly(age);
        float charge = RitualKineticMotion.Strike((age - RitualGrandScore.ChoirCharge) / 48);
        float release = RitualGrandScore.ChoirLive(age) ? 1 : 0;
        Color color = ColorFor(RitualArmamentKind.Summon);
        float swell = .96f + MathF.Sin(age * .018f + choir.Ordinal) * .04f;
        Relic(batch, RitualArmamentKind.Summon, center, choir.Projectile.rotation, (160 + assembly * 25) * swell, Color.White);
        int tiers = Math.Clamp(1 + (int)(age / 76), 1, 4);
        for (int tier = 0; tier < tiers; tier++)
        {
            float born = RitualGrandScore.Arrival(age, tier * 76);
            RitualKineticArt.Seal(batch, center + new Vector2(0, -50 - tier * 24), (43 - tier * 5) * born,
                .16f * MathF.Sin(age * .014f + tier), color, born * .42f * (1 - assembly * .6f));
        }
        if (assembly > 0)
            Conduit(batch, center, choir.ConcertCenter, center - new Vector2(0, 160), color, age + choir.Ordinal * 9, assembly * .65f);
        if (!choir.IsLeader || assembly <= .001f) return;
        Vector2 organ = choir.ConcertCenter;
        float fireAge = age - RitualGrandScore.ChoirFire;
        float kick = RitualKineticMotion.Recoil(fireAge);
        float size = Math.Min(570, 330 + choir.ChoirCount * 24) * assembly;
        Relic(batch, RitualArmamentKind.Summon, organ - new Vector2(0, 120 + kick * 35),
            MathF.Sin(age * .01f) * .025f, size, Color.White * assembly);
        Aperture(batch, organ + new Vector2(0, 25), Vector2.UnitY, (100 + charge * 55 + kick * 35) * assembly,
            age, color, assembly);
        for (int i = 0; i < 3; i++)
            Ring(batch, organ - new Vector2(0, 30 + i * 65), new(size * (.48f - i * .08f), 27),
                MathF.Sin(age * .012f + i) * .08f, Light(color, assembly * .45f), 3, true);
        Glow(batch, organ + new Vector2(0, 25), new(90 + charge * 55), Light(color, assembly * (.4f + release * .25f)));
    }

    internal static void Requiem(SpriteBatch batch, ChoirRequiem beam, float age)
    {
        Aperture(batch, beam.Projectile.Center, beam.Axis, 110 + RitualKineticMotion.Recoil(age) * 50,
            age, ColorFor(RitualArmamentKind.Summon), beam.Opening * beam.Fade);
        RitualKineticArt.Vent(batch, beam.Projectile.Center, beam.Axis, age, Gold, true);
    }

    internal static void Witness(SpriteBatch batch, WitnessLitany litany, float age)
    {
        Color color = ColorFor(RitualArmamentKind.Rogue);
        Vector2 axis = Axis(litany.Projectile), normal = new(-axis.Y, axis.X);
        Vector2 center = Center(litany.Projectile) + axis * 128;
        float arrive = RitualGrandScore.Arrival(age, 0);
        float held = 1 - RitualKineticMotion.Arrive((age - RitualGrandScore.WitnessFire) / 4);
        float fold = RitualGrandScore.WitnessFold(age);
        float charge = RitualKineticMotion.Strike((age - RitualGrandScore.WitnessFire + 22) / 22);
        float recoil = RitualKineticMotion.Recoil(age - RitualGrandScore.WitnessFire);
        // A single suspended execution blade. Six irregular pressure cuts load
        // its edge; a short compressed hold precedes the actual thrown relic.
        Vector2 blade = center - axis * (28 * charge + (1 - arrive) * 90);
        float roll = axis.ToRotation() + MathHelper.PiOver4 + .20f * (1 - fold) * MathF.Sin(age * .035f);
        Relic(batch, RitualArmamentKind.Rogue, blade, roll, (148 + fold * 40) * arrive, Color.White * arrive * held);
        for (int i = 0; i < RitualGrandScore.WitnessCount(age); i++)
        {
            float beat = age - i * 29;
            float pulse = RitualKineticMotion.Recoil(beat - 2);
            int side = i % 2 == 0 ? -1 : 1;
            Vector2 inlet = blade - axis * (i * 12 - 28) + normal * side * (21 + (1 - fold) * 12);
            Glow(batch, inlet, new Vector2(14 + pulse * 38), Light(color, (.16f + pulse * .42f) * held));
            FirstSeveranceRaidVfx.Sparks(batch, inlet, axis, beat, 1, pulse * held * .3f, color, true, .18f);
        }
        FirstSeveranceRaidVfx.Charge(batch, blade + axis * 35, axis, age, charge,
            recoil, (charge * held + recoil) * .55f, color, Reduced, .45f);
        if (recoil > .01f)
        {
            FirstSeveranceRaidVfx.Flare(batch, center, age - RitualGrandScore.WitnessFire,
                recoil * .65f, color, Reduced, .62f);
            FirstSeveranceRaidVfx.Sparks(batch, center, axis, age - RitualGrandScore.WitnessFire,
                1, recoil * .7f, color, false, .65f);
        }
    }

}
