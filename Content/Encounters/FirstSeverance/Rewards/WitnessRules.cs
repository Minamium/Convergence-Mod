#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Where the thrown blade is in its flight: out to the first contact (or its cap), two Axiom turns in the
// struck target, then back to the hang.
internal enum WitnessPhase : byte { Outbound, Turn, Return }

// The hanging blade's balance point in the aim frame: Beta radians from the aim (facing +1; mirrored by the
// facing), Radius px from the owner's hand. The blade's tip always points radially outward.
internal readonly record struct WitnessPose(float Beta, float Radius);

// Last Witness v2 (docs/encounters/first-severance/WEAPONS.md, "Rogue — Last Witness"): the timing, geometry and
// multipliers of one 282-tick score. These constants own the hit shapes and the clocks; the exported pixel art
// (DollArtAnchors) is fitted to them and the art-fit domain test keeps its anchors within one dot. Pure
// (System.Numerics only): linked into the domain tests and the offline preview.
//
// Budget (raw, one target, every hit landing, base 9680): six testimonies x0.28 (2710 each) plus the blade x5.4
// (52272) split 0.25 strike / 4 x 0.125 Axiom bites / 0.25 tear-free return = 7.08x = 68,532 per score, the
// same as 0.2.x; the stealth judgement adds 0.60 of the blade (31,363).
internal static class WitnessRules
{
    // ---- Multipliers -------------------------------------------------------------------------
    internal const float ShardMultiplier = .28f;
    internal const float BladeMultiplier = 5.4f;
    internal const float JudgementShare = .60f;
    internal const float StrikeShare = .25f, TurnShare = .125f, ReturnShare = .25f;
    internal const int Testimonies = 6, TurnBites = 4;

    // ---- Score (real ticks; attack speed never compresses it) ------------------------------
    internal const int Seal = RitualGrandScore.WitnessMerge;   // 174: the sentence is passed, release no longer cancels
    internal const int Throw = RitualGrandScore.WitnessFire;   // 218
    internal const int End = RitualGrandScore.WitnessEnd;      // 282
    internal const int LiftEnd = 202, WhipStart = 210, ThrowWarn = 196;
    // A held trigger re-uses the item after a score: the controller dies at End and the next one is first updated
    // about two ticks later (itemAnimation 2 -> 0, then a new use).
    internal const int ReuseGap = 2;
    internal const int FirstTestimony = 16, TestimonySpacing = 29, TestimonyLead = 10;
    internal const float HangTurnRate = .08f, SealedTurnRate = .025f;

    // ---- Hang (px, radians; facing +1) -----------------------------------------------------
    internal const float HangBeta = -.369f;   // 21 degrees above the line of fire
    internal const float HangRadius = 94;     // balance point from the hand (88 along the aim, 34 up)
    internal const float LiftBeta = -2.40f;   // over the shoulder, 137 degrees behind the aim
    internal const float LiftRadius = 76, ReleaseRadius = 92;
    internal const float KickRadius = 5, KickBeta = .05f;
    internal const float ArmFollow = .6f;

    // Six seats along the hanging blade's edge, in the blade frame (+x toward the tip, +y toward the cutting edge),
    // px from the balance point. A testimony slides SlideOut px out of the edge and pulls back PullBack px before
    // it fires.
    internal const float SeatFirst = -24, SeatSpacing = 16, SeatEdge = 6, SlideOut = 16, PullBack = 4;
    // A shard leaves along the aim, then seeks (native targeting, 0.24 rad/tick).
    internal const float ShardLaunch = 22, ShardSpeed = 36;

    // ---- Thrown blade ----------------------------------------------------------------------
    internal const float OutboundSpeed = 34, ReturnSpeed = 46;
    internal const int OutboundMinTicks = 4, OutboundMaxTicks = 27;
    // The hit shape while spinning: a disc swept between ticks. The drawn blade reaches beyond it.
    internal const float SpinRadius = 56;
    internal const float CatchRadius = 28, Leash = 3000;
    internal const float CruiseSpin = .45f;
    internal const int TurnTicks = 21, ReturnTick = 22, ReturnEase = 8;
    // Spin at the end of the two turns: 4 pi rad in TurnTicks with spin rising linearly from CruiseSpin.
    internal const float TurnPeak = 2f * (4f * MathF.PI / TurnTicks) - CruiseSpin;
    internal const float TurnFollow = .35f, TurnMaxSpeed = 34, ReturnResponse = .35f;

    // ---- Judgement (presentation clocks; damage timing stays in RitualArmamentChoreography) --
    internal const float JudgementCorner = 265, StakeDrop = 300;
    internal const int SwordAppear = 4, StakeFallStart = 10, EdgeWriteStart = 17, EdgeWriteTicks = 5;
    internal const int WithdrawStart = 36, WithdrawTicks = 10;

    // ---- Art design anchors (px from each sprite's pivot; blade frame for the blades) -------
    // The exported sprites (DollArtAnchors) must land within one dot of these: the hanging blade (k = 2), the
    // thrown blade's large rung (k = 1), the shard's point and the judgement sword's guard above its stake point.
    internal static readonly Vector2 HangTip = new(63, -1.5f), HangEye = new(-30, .5f);
    internal static readonly Vector2 ThrownTip = new(125, -3), ThrownEye = new(-61, .7f);
    internal static readonly Vector2 ShardPoint = new(19, 1.5f);
    internal static readonly Vector2 SwordGuard = new(0, -89);
    internal const float SwordLength = 106;

    // ---- Budget ------------------------------------------------------------------------------
    internal static double BladeShares() => StrikeShare + TurnBites * (double)TurnShare + ReturnShare;
    internal static double CycleMultiplier() => Testimonies * (double)ShardMultiplier + BladeMultiplier * BladeShares();

    // ---- Score clocks --------------------------------------------------------------------------
    internal static int TestimonyFire(int birth) => FirstTestimony + TestimonySpacing * Math.Clamp(birth, 0, Testimonies - 1);
    internal static int TestimonyWarn(int birth) => TestimonyFire(birth) - TestimonyLead;

    // Testimonies fired by score age `age`.
    internal static int Spoken(float age)
    {
        if (!float.IsFinite(age)) return 0;
        int spoken = 0;
        while (spoken < Testimonies && age >= TestimonyFire(spoken)) spoken++;
        return spoken;
    }

    // The seats fill from the centre outward: 3-4-2-5-1-6.
    internal static int SeatOf(int birth) => Math.Clamp(birth, 0, Testimonies - 1) switch
    {
        0 => 2, 1 => 3, 2 => 1, 3 => 4, 4 => 0, _ => 5,
    };

    internal static float SeatAlong(int seat) => SeatFirst + SeatSpacing * Math.Clamp(seat, 0, Testimonies - 1);

    // px a testimony stands out beyond SeatEdge: it slides out over the warning, then pulls back for the shot.
    internal static float Slide(float age, int birth)
    {
        float warn = TestimonyWarn(birth), fire = TestimonyFire(birth);
        return SlideOut * RitualKineticMotion.Arrive((age - warn) / 7f) - PullBack * RitualKineticMotion.Settle((age - (fire - 3)) / 3f);
    }

    // A testimony's seat in the blade frame at score age `age` (where its shard leaves from at TestimonyFire).
    internal static Vector2 SeatLocal(int birth, float age) => new(SeatAlong(SeatOf(birth)), SeatEdge + Slide(age, birth));

    // Releasing the trigger cancels the score only before the seal; afterwards the throw completes on its own.
    internal static bool ReleaseCancels(float age) => !(age >= Seal);

    internal static float TurnRate(float age) => age < Seal ? HangTurnRate : SealedTurnRate;

    // Sum of the testimony kicks at `age` (each 0 -> 1 -> 0 over about 11 ticks).
    internal static float Kick(float age)
    {
        float sum = 0;
        for (int i = 0; i < Testimonies; i++) sum += RitualKineticMotion.Recoil(age - TestimonyFire(i));
        return sum;
    }

    // The blade's balance point through the score: hang with a kick per testimony; lift over the shoulder on a heavy
    // quintic ease (174-202); hold (202-210); whip forward on an accelerating Hermite curve that leaves at 218 with
    // the thrown spin (CruiseSpin rad/tick) and no brake; after the throw, the rest pose that catches the blade.
    internal static WitnessPose Pose(float age)
    {
        if (!float.IsFinite(age)) age = 0;
        if (age < Seal)
        {
            float kick = Kick(age);
            return new(HangBeta - KickBeta * kick, HangRadius - KickRadius * kick);
        }
        if (age < LiftEnd)
        {
            float u = RitualKineticMotion.Settle((age - Seal) / (LiftEnd - Seal));
            return new(HangBeta + (LiftBeta - HangBeta) * u, HangRadius + (LiftRadius - HangRadius) * u);
        }
        if (age < WhipStart) return new(LiftBeta, LiftRadius);
        if (age < Throw)
        {
            // Hermite: p0 = LiftBeta, m0 = 0, p1 = 0, m1 = CruiseSpin per tick over the whip.
            float s = (age - WhipStart) / (Throw - WhipStart), s2 = s * s, s3 = s2 * s;
            float beta = LiftBeta * (2 * s3 - 3 * s2 + 1) + CruiseSpin * (Throw - WhipStart) * (s3 - s2);
            return new(beta, LiftRadius + (ReleaseRadius - LiftRadius) * RitualKineticMotion.Settle(s));
        }
        return new(HangBeta, HangRadius);
    }

    // The front arm's angle from the aim (facing +1): it points at the blade while it hangs, rides the lift and the
    // whip, follows through ArmFollow rad past the aim after the throw and returns to the hang by 268, so a held
    // trigger flows into the next score without a jump.
    internal static float ArmBeta(float age)
    {
        if (!float.IsFinite(age)) age = 0;
        if (age < Throw) return Pose(age).Beta;
        return ArmFollow * RitualKineticMotion.Settle((age - Throw) / 10f) * (1 - RitualKineticMotion.Settle((age - 232) / 18f))
            + HangBeta * RitualKineticMotion.Settle((age - 244) / 24f);
    }

    // Balance point and blade angle in the world for an aim angle and facing (+1/-1).
    internal static Vector2 BalancePoint(Vector2 root, float aim, int facing, in WitnessPose pose)
        => root + Unit(aim + Facing(facing) * pose.Beta) * pose.Radius;

    internal static float BladeAngle(float aim, int facing, in WitnessPose pose) => aim + Facing(facing) * pose.Beta;

    // Blade-frame point to world: +x along the blade, +y toward the cutting edge (the art's +y), which the facing
    // mirrors so the spine stays outward.
    internal static Vector2 BladeToWorld(Vector2 center, float angle, int facing, Vector2 local)
    {
        Vector2 along = Unit(angle), edge = new Vector2(-along.Y, along.X) * Facing(facing);
        return center + along * local.X + edge * local.Y;
    }

    // ---- Thrown blade --------------------------------------------------------------------------

    // Outbound ticks without a contact when nothing is targeted: to the cursor distance, at least OutboundMinTicks, at
    // most OutboundMaxTicks (a blade homing on a target flies the full OutboundMaxTicks).
    internal static int OutboundCap(float distance)
        => !float.IsFinite(distance) ? OutboundMaxTicks
            : Math.Clamp((int)MathF.Ceiling(Math.Max(0, distance) / OutboundSpeed), OutboundMinTicks, OutboundMaxTicks);

    internal static float TurnSpin(float t) => CruiseSpin + (TurnPeak - CruiseSpin) * Math.Clamp(float.IsFinite(t) ? t : 0, 0, TurnTicks) / TurnTicks;

    // Angle turned t ticks into the Axiom turns: exactly 4 pi at TurnTicks.
    internal static float TurnAngle(float t)
    {
        t = Math.Clamp(float.IsFinite(t) ? t : 0, 0, TurnTicks);
        return CruiseSpin * t + (TurnPeak - CruiseSpin) / (2f * TurnTicks) * t * t;
    }

    // The half-turn bite (0-3) live on turn tick t, or -1.
    internal static int TurnWindow(int t) => t switch { 6 => 0, 12 => 1, 17 => 2, 21 => 3, _ => -1 };

    internal static int TurnBiteTick(int bite) => Math.Clamp(bite, 0, TurnBites - 1) switch { 0 => 6, 1 => 12, 2 => 17, _ => 21 };

    // After tearing free the spin eases back to cruise.
    internal static float ReturnSpin(float t) => TurnPeak + (CruiseSpin - TurnPeak) * RitualKineticMotion.Settle(t / ReturnEase);

    internal static float Spin(WitnessPhase phase, float phaseAge) => phase switch
    {
        WitnessPhase.Outbound => CruiseSpin,
        WitnessPhase.Turn => TurnSpin(phaseAge),
        _ => ReturnSpin(phaseAge),
    };

    // Rotation advanced from phase age t to t + 1 (exact over the turns, so two revolutions close on 4 pi).
    internal static float SpinStep(WitnessPhase phase, int t)
        => phase == WitnessPhase.Turn && t < TurnTicks ? TurnAngle(t + 1) - TurnAngle(t) : Spin(phase, t + 1);

    internal static float Share(WitnessPhase phase) => phase switch
    {
        WitnessPhase.Outbound => StrikeShare,
        WitnessPhase.Turn => TurnShare,
        _ => ReturnShare,
    };

    // True when the disc of `radius` swept from a to b touches the box [min, max]. The distance from the box is
    // convex along the segment, so a ternary search finds its minimum exactly enough (no sampling gaps).
    internal static bool SweptDiscTouchesBox(Vector2 a, Vector2 b, float radius, Vector2 min, Vector2 max)
    {
        if (!Finite(a) || !Finite(b) || !Finite(min) || !Finite(max) || !float.IsFinite(radius) || radius < 0
            || max.X < min.X || max.Y < min.Y) return false;
        float limit = radius * radius;
        if (BoxDistanceSquared(a, min, max) <= limit || BoxDistanceSquared(b, min, max) <= limit) return true;
        float lo = 0, hi = 1;
        for (int i = 0; i < 48; i++)
        {
            float m1 = lo + (hi - lo) / 3f, m2 = hi - (hi - lo) / 3f;
            if (BoxDistanceSquared(Vector2.Lerp(a, b, m1), min, max) <= BoxDistanceSquared(Vector2.Lerp(a, b, m2), min, max)) hi = m2;
            else lo = m1;
        }
        return BoxDistanceSquared(Vector2.Lerp(a, b, (lo + hi) * .5f), min, max) <= limit;
    }

    internal static float BoxDistanceSquared(Vector2 p, Vector2 min, Vector2 max)
    {
        float dx = MathF.Max(MathF.Max(min.X - p.X, 0), p.X - max.X), dy = MathF.Max(MathF.Max(min.Y - p.Y, 0), p.Y - max.Y);
        return dx * dx + dy * dy;
    }

    // ---- Judgement presentation clocks ----------------------------------------------------------
    internal static float AuraRise(float t) => RitualKineticMotion.Arrive(t / StakeFallStart);
    internal static float SwordReveal(float t) => RitualKineticMotion.Arrive((t - SwordAppear) / (StakeFallStart - SwordAppear));

    // Height of a sword's point above its corner: it falls point-first, accelerating, and stakes the corner at the lock.
    internal static float StakeHeight(float t)
        => StakeDrop * (1 - RitualKineticMotion.Strike((t - StakeFallStart) / (RitualArmamentChoreography.VerdictLock - StakeFallStart)));

    internal static float EdgeWrite(float t) => RitualArmamentChoreography.Smooth((t - EdgeWriteStart) / EdgeWriteTicks);

    // Drawn corner radius: the stakes close inward, accelerating, onto the 185 px footprint at the execution.
    internal static float JudgementRadius(float t)
        => JudgementCorner - (JudgementCorner - RitualArmamentChoreography.VerdictRadius) * RitualKineticMotion.VerdictClosure(t);

    internal static float Withdraw(float t) => RitualArmamentChoreography.Smooth((t - WithdrawStart) / WithdrawTicks);

    internal static float JudgementFade(float t)
        => 1 - RitualArmamentChoreography.Smooth((t - RitualArmamentChoreography.VerdictEndHit)
            / (RitualArmamentChoreography.VerdictDuration - RitualArmamentChoreography.VerdictEndHit));

    // ---- Nominal timeline (for the budget tests) -------------------------------------------------

    // Raw damage landing on `tick` after a cold press with the trigger held: scores back to back with ReuseGap,
    // score age a on tick start + a - 1 (the controller is first updated on its spawn tick); testimonies count on
    // their spawn tick, the blade's strike `contact` ticks after the throw (1..OutboundMaxTicks), its four bites
    // and its tear-free return after that. One target, every hit landing, before defense.
    internal static double ColdRaw(int itemDamage, int contact, int tick)
    {
        if (tick < 0) return 0;
        int age = tick % (End + ReuseGap) + 1;
        double raw = 0;
        for (int i = 0; i < Testimonies; i++)
            if (age == TestimonyFire(i)) raw += RitualArmamentRules.ScaledDamage(itemDamage, ShardMultiplier);
        int blade = RitualArmamentRules.ScaledDamage(itemDamage, BladeMultiplier);
        int strike = Throw + Math.Clamp(contact, 1, OutboundMaxTicks);
        if (age == strike) raw += RitualArmamentRules.ScaledDamage(blade, StrikeShare);
        for (int bite = 0; bite < TurnBites; bite++)
            if (age == strike + TurnBiteTick(bite)) raw += RitualArmamentRules.ScaledDamage(blade, TurnShare);
        if (age == strike + ReturnTick) raw += RitualArmamentRules.ScaledDamage(blade, ReturnShare);
        return raw;
    }

    // ---- Small helpers ---------------------------------------------------------------------------
    internal static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    internal static float Facing(int facing) => facing < 0 ? -1 : 1;
    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
}
