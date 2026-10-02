using System;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    // Multipliers that live in the legacy ModProjectile files (not linkable here). The tools contract
    // test_doll_weapon_layer.py pins each one to its exact expression in those sources.
    private static class LegacyDollMultipliers
    {
        internal const float MeridianBuild = .95f, MeridianOverdrive = .62f, MeridianHeavy = 1.15f; // MeridianBastion
        internal const float LacunaBolt = .55f, LacunaBeam = 2.0f;                                   // LacunaConvergence
        internal const float ChoirNote = .85f;                                                       // RitualChoir
        internal const double ChoirChorus = 1.05;                                                    // ChoirRequiem, (int) total
        internal const int ChoirChorusCooldown = 12;                                                 // ChoirRequiem
        internal const float WitnessShard = .28f, WitnessBlade = 5.4f;                               // WitnessLitany
        internal const float WitnessVerdict = .60f;                                                  // RitualBolts
        internal const int WitnessReturnTicks = 42;                                                  // RitualBolts return
    }

    private static readonly DollFlip[] AllDollFlips = { DollFlip.None, DollFlip.Horizontal, DollFlip.Vertical, DollFlip.Both };

    private static void AssertDollNear(Vector2 expected, Vector2 actual, float tolerance, string context)
    {
        if (!(Vector2.Distance(expected, actual) <= tolerance))
            throw new InvalidOperationException($"Expected {context} {expected}, got {actual}.");
    }

    private static void AssertDollNear(double expected, double actual, double tolerance, string context)
    {
        if (!(Math.Abs(expected - actual) <= tolerance))
            throw new InvalidOperationException($"Expected {context} {expected}, got {actual}.");
    }

    [DomainTest("Doll sprite placement round-trips texels over rotations, flips and reversed gravity")]
    private static void DollSpritePlacementRoundTrip()
    {
        Vector2 size = new(65, 12);
        Vector2[] pivots = { new(0, 0), new(12.5f, 6), new(32.5f, 6.5f), new(65, 12) };
        Vector2[] texels = { new(0, 0), new(64.5f, .5f), new(30, 11.5f), new(65, 12), new(7.25f, 3.75f) };
        Vector2 pivotWorld = new(1234.5f, 811.25f);
        const float axisY = 840f;
        for (int step = 0; step <= 32; step++)
        {
            float rotation = step * MathF.PI / 16f;
            foreach (DollFlip flip in AllDollFlips)
            foreach (Vector2 pivot in pivots)
            foreach (Vector2 texel in texels)
            {
                Vector2 world = DollSpritePlacement.World(texel, pivot, pivotWorld, rotation, flip);
                AssertDollNear(texel, DollSpritePlacement.Texel(world, pivot, pivotWorld, rotation, flip), .005f, "texel round trip");
                float distance = Vector2.Distance(world, pivotWorld);
                AssertDollNear(Vector2.Distance(texel, pivot) * DollSpritePlacement.WorldPerTexel, distance, .01f,
                    "one texel is always two world px");
                // A flipped sprite is the pre-mirrored image placed about the mirrored pivot.
                Vector2 mirrored = DollSpritePlacement.World(DollSpritePlacement.MirrorAnchor(texel, size, flip),
                    DollSpritePlacement.MirrorAnchor(pivot, size, flip), pivotWorld, rotation, DollFlip.None);
                AssertDollNear(world, mirrored, .01f, "mirrored anchor");
                foreach (float gravDir in new[] { 1f, -1f })
                {
                    Vector2 placed = pivotWorld;
                    float turned = rotation;
                    DollFlip flipped = flip;
                    DollSpritePlacement.ApplyGravity(gravDir, axisY, ref placed, ref turned, ref flipped);
                    Vector2 reversed = DollSpritePlacement.World(texel, pivot, placed, turned, flipped);
                    Vector2 expected = gravDir < 0 ? new Vector2(world.X, 2 * axisY - world.Y) : world;
                    AssertDollNear(expected, reversed, .01f, "reversed gravity mirrors the upright placement");
                    AssertDollNear(texel, DollSpritePlacement.Texel(reversed, pivot, placed, turned, flipped), .005f,
                        "reversed gravity round trip");
                }
            }
        }
        AssertEqual(new Vector2(65 - 12.5f, 6), DollSpritePlacement.MirrorAnchor(new Vector2(12.5f, 6), size, DollFlip.Horizontal),
            "horizontal mirror anchor (w - x, y)");
        AssertEqual(new Vector2(12.5f, 12 - 6.5f), DollSpritePlacement.MirrorAnchor(new Vector2(12.5f, 6.5f), size, DollFlip.Vertical),
            "vertical mirror anchor (x, h - y)");
        Vector2 tip = DollSpritePlacement.World(new Vector2(65, 6), new Vector2(0, 6), Vector2.Zero, 0, DollFlip.None);
        AssertEqual(new Vector2(130, 0), tip, "a 65-texel blade is 130 world px long, never rescaled");
    }

    [DomainTest("Axis-aligned Doll sprites snap every texel corner onto the world 2 px dot grid")]
    private static void DollSpritePlacementSnap()
    {
        var random = new Random(4021);
        float[] quarterTurns = { 0, MathF.PI / 2, MathF.PI, 3 * MathF.PI / 2, MathF.Tau, -MathF.PI / 2, 1e-5f };
        for (int i = 0; i < 400; i++)
        {
            Vector2 pivotWorld = new(1000 + (float)random.NextDouble() * 4000, 500 + (float)random.NextDouble() * 3000);
            Vector2 pivotTexel = new(random.Next(0, 80) + (random.Next(2) == 0 ? 0 : .5f), random.Next(0, 40) + (random.Next(2) == 0 ? 0 : .5f));
            float rotation = quarterTurns[i % quarterTurns.Length];
            DollFlip flip = AllDollFlips[i % 4];
            AssertEqual(true, DollSpritePlacement.QuarterTurns(rotation) >= 0, "axis-aligned turn detected");
            Vector2 snapped = DollSpritePlacement.Snap(pivotWorld, pivotTexel, rotation, flip);
            AssertEqual(true, MathF.Abs(snapped.X - pivotWorld.X) <= 1.0001f && MathF.Abs(snapped.Y - pivotWorld.Y) <= 1.0001f,
                "snap moves the pivot by at most one world px per axis");
            for (int cx = 0; cx <= 3; cx++)
            for (int cy = 0; cy <= 2; cy++)
            {
                Vector2 corner = DollSpritePlacement.World(new Vector2(cx * 7, cy * 5), pivotTexel, snapped, rotation, flip);
                float ex = corner.X / 2 - MathF.Round(corner.X / 2), ey = corner.Y / 2 - MathF.Round(corner.Y / 2);
                AssertEqual(true, MathF.Abs(ex) < 2e-3f && MathF.Abs(ey) < 2e-3f, "texel corner on an even world coordinate");
            }
            AssertEqual(snapped, DollSpritePlacement.Snap(snapped, pivotTexel, rotation, flip), "snap is idempotent");
        }
        Vector2 free = new(101.3f, 77.7f);
        AssertEqual(-1, DollSpritePlacement.QuarterTurns(.01f), "a slight tilt is a rotation");
        AssertEqual(free, DollSpritePlacement.Snap(free, new Vector2(3, 4), .3f, DollFlip.None), "rotated sprites keep a continuous pivot");
        AssertEqual(free, DollSpritePlacement.Snap(free, new Vector2(3, 4), float.NaN, DollFlip.None), "non-finite rotation is not snapped");
    }

    [DomainTest("Doll cue clock fires each cue tick once through jitter, resync, late jumps and fresh scores")]
    private static void DollCueClockContract()
    {
        static int Fires(float[] ages, int cueTick, ref int slot, int late = DollCueClock.DefaultLate)
        {
            int fired = 0;
            float previous = ages[0];
            foreach (float age in ages)
            {
                if (DollCueClock.Take(ref slot, previous, age, cueTick, late)) fired++;
                previous = age;
            }
            return fired;
        }

        int slot = DollCueClock.Armed;
        float[] steady = new float[30];
        for (int i = 0; i < steady.Length; i++) steady[i] = i;
        AssertEqual(1, Fires(steady, 10, ref slot), "one fire on a steady clock");
        AssertEqual(0, Fires(steady, 10, ref slot), "replaying ages already heard never refires");

        slot = DollCueClock.Armed;
        AssertEqual(1, Fires(new float[] { 7, 8, 9, 11, 10, 12, 11, 13, 12, 14 }, 10, ref slot), "jitter fires once");
        slot = DollCueClock.Armed;
        AssertEqual(1, Fires(new float[] { 8, 9, 10, 8, 9, 10, 11, 9, 10, 12 }, 10, ref slot), "1-2 tick backward resync never refires");
        slot = DollCueClock.Armed;
        AssertEqual(0, Fires(new float[] { 8, 9, 14, 15, 16 }, 10, ref slot), "a cue more than 3 ticks late is skipped");
        AssertEqual(0, Fires(new float[] { 16, 12, 11, 10, 11, 12 }, 10, ref slot), "a skipped cue is consumed, not replayed by a resync");
        slot = DollCueClock.Armed;
        AssertEqual(1, Fires(new float[] { 8, 9, 13, 14 }, 10, ref slot), "a jump landing exactly 3 ticks late still fires");
        slot = DollCueClock.Armed;
        AssertEqual(1, Fires(new float[] { 8, 9, 30 }, 10, ref slot, int.MaxValue), "a loop may opt into starting late");

        slot = DollCueClock.Armed;
        AssertEqual(1, Fires(new float[] { 600, 640, 659, 0, 1, 2 }, 1, ref slot), "first pass");
        AssertEqual(1, Fires(new float[] { 2, 640, 659, 0, 1, 2 }, 1, ref slot), "a fresh score (age drop > 30) re-arms the cue");
        AssertEqual(0, Fires(new float[] { 40, 15, 16, 17 }, 1, ref slot), "a drop of 30 or less is a resync, not a fresh score");
        DollCueClock.Reset(ref slot);
        AssertEqual(DollCueClock.Armed, slot, "explicit re-arm");

        slot = DollCueClock.Armed;
        int[] ladder = { 5, 10, 15, 20, 25, 30 };
        int beats = 0;
        float last = 0;
        for (float age = 0; age < 40; age += 1)
        {
            foreach (int cue in ladder)
                if (DollCueClock.Take(ref slot, last, age, cue)) beats++;
            last = age;
        }
        AssertEqual(ladder.Length, beats, "increasing cue ticks in one slot each fire once");
        slot = DollCueClock.Armed;
        AssertEqual(false, DollCueClock.Take(ref slot, 9, float.NaN, 10), "non-finite age never fires");
        AssertEqual(DollCueClock.Armed, slot, "non-finite age consumes nothing");
        AssertEqual(false, DollCueClock.Take(ref slot, 8, 9, 10), "an early sample does not consume the cue");
        AssertEqual(true, DollCueClock.Take(ref slot, 9, 10, 10), "the cue fires on its tick");
    }

    [DomainTest("Doll weapon budget baselines equal the legacy scores recomputed with per-hit rounding")]
    private static void DollWeaponBudgetBaselines()
    {
        // Claws: swipes run through the 360-tick charge at the 28-tick cadence; the crush blocks swipes.
        int claw = NullCantorClawMotion.BaseDamage;
        int crush = RitualArmamentRules.ScaledDamage(claw, NullCantorClawMotion.CrushMultiplier);
        int clawCycle = NullCantorClawMotion.ChargeTicks + NullCantorClawMotion.CrushTicks;
        double swipes = (double)NullCantorClawMotion.ChargeTicks / NullCantorClawMotion.SwingTicks;
        AssertEqual(DollWeaponBudget.Claw.CycleTicks, clawCycle, "claw cycle ticks");
        AssertEqual(DollWeaponBudget.Claw.BaseDamage, claw, "claw base");
        AssertDollNear(DollWeaponBudget.Claw.CycleRaw, swipes * claw + crush, 1e-6, "claw cycle raw");
        AssertDollNear(DollWeaponBudget.Claw.CycleMultiplier, swipes + NullCantorClawMotion.CrushMultiplier, 1e-4, "claw cycle multiplier");
        AssertEqual(19_603L, (long)Math.Round(DollWeaponBudget.Claw.PerSecond), "claw sustained per second");

        // Pale Meridian: the build needles, then the overdrive cadence with one heavy needle per 36 ticks.
        int ranged = RitualArmamentRules.Damage(RitualArmamentKind.Ranged);
        int buildNeedle = RitualArmamentRules.ScaledDamage(ranged, LegacyDollMultipliers.MeridianBuild);
        int needle = RitualArmamentRules.ScaledDamage(ranged, LegacyDollMultipliers.MeridianOverdrive);
        int heavy = RitualArmamentRules.ScaledDamage(ranged, LegacyDollMultipliers.MeridianHeavy);
        int built = 0;
        for (int age = 1; age < RitualGrandScore.BatteryFire; age++)
            if (RitualGrandScore.BatteryLane(age) >= 0) built++;
        AssertEqual(15, built, "Meridian build needles");
        AssertEqual(DollWeaponBudget.MeridianBuild.BaseDamage, ranged, "Meridian base");
        AssertEqual(DollWeaponBudget.MeridianBuild.CycleTicks, RitualGrandScore.BatteryFire, "Meridian build ticks");
        AssertEqual(DollWeaponBudget.MeridianOverdrive.BaseDamage, ranged, "Meridian overdrive base");
        AssertEqual(36, DollWeaponBudget.MeridianOverdrive.CycleTicks, "Meridian overdrive heavy-needle period");
        AssertDollNear(DollWeaponBudget.MeridianBuild.CycleRaw, built * buildNeedle, 1e-6, "Meridian build raw");
        AssertDollNear(DollWeaponBudget.MeridianBuild.CycleMultiplier, built * LegacyDollMultipliers.MeridianBuild, 1e-4, "Meridian build multiplier");
        double overdrive = 0, overdriveMultiplier = 0;
        for (int age = RitualGrandScore.BatteryFire; age < RitualGrandScore.BatteryFire + DollWeaponBudget.MeridianOverdrive.CycleTicks; age++)
        {
            if (RitualGrandScore.BatteryLane(age) < 0) continue;
            bool isHeavy = age % 36 == 0;
            overdrive += isHeavy ? heavy : needle;
            overdriveMultiplier += isHeavy ? LegacyDollMultipliers.MeridianHeavy : LegacyDollMultipliers.MeridianOverdrive;
        }
        AssertDollNear(DollWeaponBudget.MeridianOverdrive.CycleRaw, overdrive, 1e-6, "Meridian overdrive raw per 36 ticks");
        AssertDollNear(DollWeaponBudget.MeridianOverdrive.CycleMultiplier, overdriveMultiplier, 1e-4, "Meridian overdrive multiplier");
        AssertEqual(26_588L, (long)Math.Round(DollWeaponBudget.MeridianOverdrive.PerSecond), "Meridian overdrive per second");
        AssertEqual(true, DollWeaponBudget.Within(DollWeaponBudget.MeridianOverdrive.PerSecond, 26_593, .001),
            "per-hit rounding stays within 0.1% of the unrounded 7.97x figure");

        // Lacuna Testament: seven sigils' bolts until the merge, then the beam per root every 10 ticks.
        int magic = RitualArmamentRules.Damage(RitualArmamentKind.Magic);
        int bolt = RitualArmamentRules.ScaledDamage(magic, LegacyDollMultipliers.LacunaBolt);
        int beam = RitualArmamentRules.ScaledDamage(magic, LegacyDollMultipliers.LacunaBeam);
        int bolts = 0;
        for (int age = 1; age < RitualGrandScore.MagicFire; age++)
            for (int lane = 0; lane < 7; lane++)
                if (RitualGrandScore.MagicBoltAt(age, lane)) bolts++;
        AssertEqual(26, bolts, "Lacuna bolts (9 + 6 + 4 + 3 + 2 + 1 + 1)");
        AssertEqual(DollWeaponBudget.LacunaBuild.CycleTicks, RitualGrandScore.MagicFire, "Lacuna build ticks");
        AssertEqual(DollWeaponBudget.LacunaBuild.BaseDamage, magic, "Lacuna build base");
        AssertEqual(DollWeaponBudget.LacunaBeam.BaseDamage, magic, "Lacuna beam base");
        AssertDollNear(DollWeaponBudget.LacunaBeam.CycleMultiplier, LegacyDollMultipliers.LacunaBeam, 1e-4, "Lacuna beam multiplier");
        AssertDollNear(DollWeaponBudget.LacunaBuild.CycleRaw, bolts * bolt, 1e-6, "Lacuna build raw");
        AssertDollNear(DollWeaponBudget.LacunaBuild.CycleMultiplier, bolts * LegacyDollMultipliers.LacunaBolt, 1e-4, "Lacuna build multiplier");
        AssertEqual(DollWeaponBudget.LacunaBeam.CycleTicks, RitualGrandScore.MagicHitCadence, "Lacuna beam cadence");
        AssertDollNear(DollWeaponBudget.LacunaBeam.CycleRaw, beam, 1e-6, "Lacuna beam hit");
        AssertEqual(24_288L, (long)Math.Round(DollWeaponBudget.LacunaBeam.PerSecond), "Lacuna beam per second");

        // Choir of the Unmade, per voice: notes before assembly, then 15 chorus hits truncated by (int).
        int summon = RitualArmamentRules.Damage(RitualArmamentKind.Summon);
        int note = RitualArmamentRules.ScaledDamage(summon, LegacyDollMultipliers.ChoirNote);
        int chorus = (int)Math.Clamp(summon * LegacyDollMultipliers.ChoirChorus, 1, int.MaxValue / 4.0);
        int chorusLength = RitualGrandScore.ChoirReleaseEnd - RitualGrandScore.ChoirFire, chorusHits = 0;
        for (int age = 1; age <= chorusLength; age += LegacyDollMultipliers.ChoirChorusCooldown) chorusHits++;
        AssertEqual(15, chorusHits, "chorus hits per concert");
        double concerts = 0, concertMultipliers = 0;
        for (int ordinal = 0; ordinal < 8; ordinal++)
        {
            int notes = 0;
            for (int age = 0; age < RitualGrandScore.ChoirCycle; age++)
                if (RitualGrandScore.ChoirNoteAt(age, ordinal)) notes++;
            AssertEqual(ordinal < 4 ? 6 : 5, notes, "notes per voice ordinal");
            concerts += notes * note + chorusHits * chorus;
            concertMultipliers += notes * LegacyDollMultipliers.ChoirNote + chorusHits * LegacyDollMultipliers.ChoirChorus;
        }
        AssertEqual(DollWeaponBudget.ChoirConcert.CycleTicks, RitualGrandScore.ChoirCycle, "Choir concert ticks");
        AssertEqual(DollWeaponBudget.ChoirConcert.BaseDamage, summon, "Choir concert base");
        AssertEqual(DollWeaponBudget.ChoirChorus.BaseDamage, summon, "Choir chorus base");
        AssertEqual(DollWeaponBudget.ChoirChorus.CycleTicks, LegacyDollMultipliers.ChoirChorusCooldown, "Choir chorus cadence");
        AssertDollNear(DollWeaponBudget.ChoirChorus.CycleMultiplier, LegacyDollMultipliers.ChoirChorus, 1e-4, "Choir chorus multiplier");
        AssertDollNear(DollWeaponBudget.ChoirConcert.CycleRaw, concerts / 8, 1e-6, "Choir mean concert raw per voice");
        AssertDollNear(DollWeaponBudget.ChoirConcert.CycleMultiplier, concertMultipliers / 8, 1e-4, "Choir mean concert multiplier");
        AssertEqual(1_797L, (long)Math.Round(DollWeaponBudget.ChoirConcert.PerSecond), "Choir mean per second per voice");
        AssertDollNear(DollWeaponBudget.ChoirChorus.CycleRaw, chorus, 1e-6, "Choir chorus hit per voice");
        AssertEqual(5_080L, (long)Math.Round(DollWeaponBudget.ChoirChorus.PerSecond), "Choir chorus per second per voice");

        // Last Witness: six testimonies, then the blade split outbound/return; the stealth verdict on top.
        int rogue = RitualArmamentRules.Damage(RitualArmamentKind.Rogue);
        int shard = RitualArmamentRules.ScaledDamage(rogue, LegacyDollMultipliers.WitnessShard);
        int blade = RitualArmamentRules.ScaledDamage(rogue, LegacyDollMultipliers.WitnessBlade);
        int shards = 0;
        for (int age = 1; age < RitualGrandScore.WitnessMerge; age++)
            if (age % 29 == 16) shards++;
        AssertEqual(6, shards, "Witness testimonies");
        double split = RitualArmamentRules.RogueShare(false) + RitualArmamentRules.RogueShare(true);
        AssertDollNear(1, split, 1e-6, "outbound and return shares sum to the blade");
        AssertEqual(DollWeaponBudget.Witness.CycleTicks, RitualGrandScore.WitnessEnd, "Witness cycle ticks");
        AssertEqual(DollWeaponBudget.Witness.BaseDamage, rogue, "Witness base");
        AssertDollNear(DollWeaponBudget.Witness.CycleRaw, shards * shard + blade * split, .01, "Witness cycle raw");
        AssertDollNear(DollWeaponBudget.Witness.CycleMultiplier,
            shards * LegacyDollMultipliers.WitnessShard + LegacyDollMultipliers.WitnessBlade, 1e-4, "Witness cycle multiplier");
        AssertEqual(14_581L, (long)Math.Round(DollWeaponBudget.Witness.PerSecond), "Witness per second");
        AssertDollNear(DollWeaponBudget.WitnessVerdictRaw, RitualArmamentRules.ScaledDamage(blade, LegacyDollMultipliers.WitnessVerdict), 1e-6, "verdict raw");
        AssertDollNear(DollWeaponBudget.WitnessVerdictMultiplier, LegacyDollMultipliers.WitnessVerdict * LegacyDollMultipliers.WitnessBlade, 1e-4, "verdict multiplier");

        AssertEqual(true, DollWeaponBudget.Within(1.029 * 1000, 1000), "inside the 3% band");
        AssertEqual(false, DollWeaponBudget.Within(1.031 * 1000, 1000), "outside the 3% band");
        AssertEqual(false, DollWeaponBudget.Within(double.NaN, 1000), "non-finite never passes");
        AssertDollNear(-.02, DollWeaponBudget.Deviation(980, 1000), 1e-9, "deviation sign");
    }

    [DomainTest("Doll weapon budget cold-press windows equal the legacy timelines")]
    private static void DollWeaponBudgetColdWindows()
    {
        int window = DollWeaponBudget.ColdWindowTicks;

        // Claws: tick 0 is the press. Swipe hits land on the first live update, the crush on its impact
        // update (three updates per tick); the crush kills the running swipe and blocks swipes for 42 ticks.
        int claw = NullCantorClawMotion.BaseDamage;
        int crush = RitualArmamentRules.ScaledDamage(claw, NullCantorClawMotion.CrushMultiplier);
        int swipeHit = ((int)MathF.Ceiling(NullCantorClawMotion.SweepStart * NullCantorClawMotion.SwingTicks * 3) - 1) / 3;
        int crushHit = (NullCantorClawMotion.CrushImpactTick * 3 - 1) / 3;
        double[] ClawTimeline(int delay)
        {
            var raw = new double[window];
            var charge = new NullCantorClawCharge();
            int crushEnd = int.MinValue, swipeEnd = int.MinValue;
            for (int tick = 0; tick < window; tick++)
            {
                bool crushing = tick < crushEnd;
                if (!crushing && charge.Ready && tick >= NullCantorClawMotion.ChargeTicks + delay && charge.TrySpend())
                {
                    crushEnd = tick + NullCantorClawMotion.CrushTicks;
                    swipeEnd = tick;
                    if (tick + crushHit < window) raw[tick + crushHit] += crush;
                    crushing = true;
                }
                else if (!crushing && tick >= swipeEnd)
                {
                    swipeEnd = tick + NullCantorClawMotion.SwingTicks;
                    if (tick + swipeHit < window) raw[tick + swipeHit] += claw;
                }
                charge.Tick(true, true, crushing);
            }
            return raw;
        }
        var clawTimelines = new double[240][];
        for (int delay = 0; delay < clawTimelines.Length; delay++) clawTimelines[delay] = ClawTimeline(delay);
        double clawBest = DollWeaponBudget.BestColdWindow(clawTimelines.Length, (delay, tick) => clawTimelines[delay][tick], out int clawDelay);
        AssertDollNear(DollWeaponBudget.ClawColdWindow, clawBest, 1e-6, "claw best cold window");
        AssertDollNear(20 * claw + crush, DollWeaponBudget.ColdWindow(tick => clawTimelines[0][tick]), 1e-6, "crush pressed when ready");
        AssertDollNear(21 * claw + crush, DollWeaponBudget.ColdWindow(tick => clawTimelines[clawDelay][tick]), 1e-6,
            "crush timed after a swipe lands");

        // Pale Meridian: score age a lands on tick a - 1.
        int ranged = RitualArmamentRules.Damage(RitualArmamentKind.Ranged);
        double Meridian(int tick)
        {
            int age = tick + 1;
            if (RitualGrandScore.BatteryLane(age) < 0) return 0;
            float factor = age < RitualGrandScore.BatteryFire ? LegacyDollMultipliers.MeridianBuild
                : age % 36 == 0 ? LegacyDollMultipliers.MeridianHeavy : LegacyDollMultipliers.MeridianOverdrive;
            return RitualArmamentRules.ScaledDamage(ranged, factor);
        }
        AssertDollNear(DollWeaponBudget.MeridianColdWindow, DollWeaponBudget.ColdWindow(Meridian), 1e-6, "Meridian cold window");

        // Lacuna Testament: bolts during construction, then the beam from its first opened tick.
        int magic = RitualArmamentRules.Damage(RitualArmamentKind.Magic);
        int firstBeam = RitualGrandScore.MagicFire;
        while (RitualGrandScore.BeamOpening(firstBeam, RitualGrandScore.MagicFire) <= 0) firstBeam++;
        AssertEqual(RitualGrandScore.MagicFire + 1, firstBeam, "the beam opens the tick after release");
        double Lacuna(int tick)
        {
            int age = tick + 1;
            double raw = 0;
            for (int lane = 0; lane < 7; lane++)
                if (RitualGrandScore.MagicBoltAt(age, lane)) raw += RitualArmamentRules.ScaledDamage(magic, LegacyDollMultipliers.LacunaBolt);
            if (age >= firstBeam && (age - firstBeam) % RitualGrandScore.MagicHitCadence == 0)
                raw += RitualArmamentRules.ScaledDamage(magic, LegacyDollMultipliers.LacunaBeam);
            return raw;
        }
        AssertDollNear(DollWeaponBudget.LacunaColdWindow, DollWeaponBudget.ColdWindow(Lacuna), 1e-6, "Lacuna cold window");

        // Choir, per voice (the best, six-note ordinal): the chorus starts at the fire beat and hits every 12 ticks.
        int summon = RitualArmamentRules.Damage(RitualArmamentKind.Summon);
        int chorusLength = RitualGrandScore.ChoirReleaseEnd - RitualGrandScore.ChoirFire;
        double Choir(int ordinal, int tick)
        {
            int age = tick + 1;
            double raw = RitualGrandScore.ChoirNoteAt(age, ordinal) ? RitualArmamentRules.ScaledDamage(summon, LegacyDollMultipliers.ChoirNote) : 0;
            int chorusAge = age - RitualGrandScore.ChoirFire;
            if (chorusAge >= 1 && chorusAge <= chorusLength && (chorusAge - 1) % LegacyDollMultipliers.ChoirChorusCooldown == 0)
                raw += (int)(summon * LegacyDollMultipliers.ChoirChorus);
            return raw;
        }
        double choirBest = DollWeaponBudget.BestColdWindow(8, Choir, out int bestOrdinal);
        AssertDollNear(DollWeaponBudget.ChoirColdWindow, choirBest, 1e-6, "Choir best voice cold window");
        AssertEqual(true, bestOrdinal < 4, "a six-note voice is the best");

        // Last Witness: litanies back to back; testimonies, the thrown blade (0.70) and its return (0.30).
        int rogue = RitualArmamentRules.Damage(RitualArmamentKind.Rogue);
        int blade = RitualArmamentRules.ScaledDamage(rogue, LegacyDollMultipliers.WitnessBlade);
        double Witness(int tick)
        {
            int age = tick % RitualGrandScore.WitnessEnd + 1;
            double raw = age < RitualGrandScore.WitnessMerge && age % 29 == 16
                ? RitualArmamentRules.ScaledDamage(rogue, LegacyDollMultipliers.WitnessShard) : 0;
            if (age == RitualGrandScore.WitnessFire) raw += blade * RitualArmamentRules.RogueShare(false);
            if (age == RitualGrandScore.WitnessFire + LegacyDollMultipliers.WitnessReturnTicks) raw += blade * RitualArmamentRules.RogueShare(true);
            return raw;
        }
        AssertDollNear(DollWeaponBudget.WitnessColdWindow, DollWeaponBudget.ColdWindow(Witness), .01, "Witness cold window");
        AssertThrows<ArgumentOutOfRangeException>(() => DollWeaponBudget.BestColdWindow(0, (_, _) => 0, out _), "no timing choices");
    }
}
