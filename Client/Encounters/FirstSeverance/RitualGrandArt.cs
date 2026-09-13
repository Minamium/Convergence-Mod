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
        Vector2 root = Center(gun.Projectile), axis = Axis(gun.Projectile);
        float lockIn = RitualKineticMotion.Arrive((age - RitualGrandScore.BatteryLock) / 18);
        float charge = RitualKineticMotion.Strike((age - RitualGrandScore.BatteryLock - 18) / 48);
        int count = RitualGrandScore.Count(age, false);
        float flash = RitualKineticMotion.Recoil(age - RitualGrandScore.BatteryFire);
        for (int i = 0; i < count; i++)
        {
            float born = RitualGrandScore.Arrival(age, RitualGrandScore.BatteryBirth(i));
            Vector2 at = gun.Muzzle(i, age, root, axis), normal = axis.RotatedBy(MathHelper.PiOver2);
            float kick = 0;
            for (int back = 0; back <= 11; back++)
                if (RitualGrandScore.BatteryLane((int)age - back) == i)
                    kick = Math.Max(kick, RitualKineticMotion.Recoil(back + age % 1));
            Vector2 body = at - axis * (150 + kick * (gun.Overdrive ? 26 : 18));
            Relic(batch, RitualArmamentKind.Ranged, body, axis.ToRotation() + MathHelper.PiOver4,
                260 * born, Color.White * born * gun.Fade);
            for (int side = -1; side <= 1; side += 2)
                Part(batch, 1, body + normal * side * (20 + charge * 25), axis.ToRotation() + side * (.14f + charge * .65f),
                    110 * born, Color.White * born * gun.Fade, side < 0, new(256, 400));
            Line(batch, body + axis * 75, at, Light(color, born * gun.Fade * (.3f + charge * .4f)), 3 + charge * 5);
            Aperture(batch, at, axis, (31 + charge * 15 + kick * 24) * born, age + i * 17, color, born * gun.Fade);
            Glow(batch, at, new(32 + kick * 95), Light(Ivory, kick * gun.Fade * .7f));
            if (kick > 0) Line(batch, at, at + axis * kick * 160, Light(color, kick * gun.Fade), 8 * kick);
            Conduit(batch, root - axis * 100, body, body - normal * i * 15, color, age + i * 11, born * gun.Fade * .45f);
        }
        Ring(batch, root - axis * 110, new(60 + lockIn * 30, 90 + count * 40), axis.ToRotation(),
            Light(color, .35f * gun.Fade), 3, true);
        if (flash > 0) RitualKineticArt.Vent(batch, root, axis, age - RitualGrandScore.BatteryFire, color, true);
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
        Vector2 center = litany.Crown;
        float fold = RitualGrandScore.WitnessFold(age);
        float fired = RitualKineticMotion.Arrive((age - RitualGrandScore.WitnessFire) / 5);
        for (int i = 0; i < RitualGrandScore.WitnessCount(age); i++)
        {
            float born = RitualGrandScore.Arrival(age, i * 29), angle = i * MathF.Tau / 6 + age * .012f;
            Vector2 at = center + angle.ToRotationVector2() * (190 + (1 - born) * 145) * (1 - fold);
            float fade = born * (1 - fold) * (1 - fired);
            Relic(batch, RitualArmamentKind.Rogue, at, angle + age * .02f, 105 * born, Color.White * fade);
            RitualKineticArt.Seal(batch, at, 48 * born, angle, color, fade * .8f);
            Conduit(batch, at, center, at + litany.Axis * 80, color, age + i * 12, fade * .5f);
        }
        float charge = RitualKineticMotion.Strike((age - RitualGrandScore.WitnessMerge - 20) / 24);
        Relic(batch, RitualArmamentKind.Rogue, center, -age * .014f, (230 - charge * 45) * fold,
            Color.White * fold * (1 - fired));
        Aperture(batch, center, litany.Axis, 115 * fold * (1 - fired), age, color, fold * (1 - fired));
        Glow(batch, center, new(45 + charge * 115), Light(color, fold * (1 - fired) * .8f));
        if (age >= RitualGrandScore.WitnessFire)
        {
            float after = age - RitualGrandScore.WitnessFire, flash = RitualKineticMotion.Recoil(after);
            RitualKineticArt.Vent(batch, center, litany.Axis, after, color, true);
            Ring(batch, center + litany.Axis * after * 9, new(40 + after * 8), 0,
                Light(Ivory, flash), 9 * flash, true);
        }
    }
}
