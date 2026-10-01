#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Pure timing, state and geometry of the Ebon Thimble (docs/encounters/ebon-manor/REWARDS.md#magic).
// No Terraria/XNA references: the domain tests link this file together with EbonRewardRules.
internal static class EbonThimbleScore
{
    // Piece states (Projectile.ai[1]). A piece hangs, is armed by the release, flies, then crashes
    // (or is dropped when the owner can no longer use the weapon).
    internal const int Hanging = 0, Armed = 1, Flying = 2, Crashed = 3, Dropped = 4;
    // Controller phases (Projectile.ai[2]): holding, holding without mana, volley in progress.
    internal const int Channeling = 0, Spent = 1, Releasing = 2;

    internal const int LiftEaseTicks = 16, WindTicks = 5, CrashLife = 8, DropLife = 24, MaxFlight = 90, RecoveryTicks = 14;
    internal const int PianoCrashLife = 18, PianoFade = 12;
    internal const float PianoMaxSpeed = 44, PianoMargin = 96;
    // The fan: degrees from straight ahead (0) over the head (90) to straight behind (180).
    internal const float FanBack = 170, FanFront = 50, InnerRadius = 170, OuterRadius = 245;
    internal static readonly Vector2 FanPivot = new(-34, -14);

    internal enum Material : byte { Wood, Glass, Wire }

    // --- Timing --------------------------------------------------------------------------
    internal static bool LiftDue(int age, int lifted)
        => lifted >= 0 && lifted < EbonRewardRules.Pieces && age >= EbonRewardRules.LiftTick(lifted);
    internal static bool LaunchDue(int armedAge, int index) => armedAge >= EbonRewardRules.YankTick(index);
    internal static bool Finale(int lifted) => lifted >= EbonRewardRules.Pieces;
    internal static int LastLaunch(int lifted) => lifted <= 0 ? 0 : EbonRewardRules.YankTick(lifted - 1);
    // The controller stays to pose the throw until the last yank has had time to settle.
    internal static int VolleyEnd(int lifted) => LastLaunch(lifted) + RecoveryTicks;
    internal static int PianoAppears => EbonRewardRules.PianoTick(EbonRewardRules.Pieces);

    // --- Motion --------------------------------------------------------------------------
    internal static float NextSpeed(float speed) => Math.Min(EbonRewardRules.YankMax, speed + EbonRewardRules.YankAcceleration);
    internal static float NextFall(float speed) => Math.Min(PianoMaxSpeed, speed + EbonRewardRules.PianoGravity);

    // Ticks until a body starting at `start` px/tick and gaining `step` per tick has covered `distance`.
    internal static int TicksToCover(float distance, float start, float step, float max)
    {
        float speed = start, covered = 0;
        int ticks = 0;
        while (covered < distance && ticks < 1200)
        {
            covered += speed;
            speed = Math.Min(max, speed + step);
            ticks++;
        }
        return ticks;
    }

    // Offset from the owner's centre to the body centre of a hanging piece: a fan above and behind,
    // alternating two radii so neighbours do not stack, mirrored with the facing, bobbing on `tick`.
    internal static Vector2 Slot(int index, int facing, float tick)
    {
        index = Math.Clamp(index, 0, EbonRewardRules.Pieces - 1);
        float u = index / (float)(EbonRewardRules.Pieces - 1);
        float angle = (FanBack + (FanFront - FanBack) * u) * (MathF.PI / 180f);
        float radius = index % 2 == 0 ? InnerRadius : OuterRadius;
        float side = facing < 0 ? -1 : 1;
        float bob = MathF.Sin(tick * .06f + index * .8f) * 5f, sway = MathF.Sin(tick * .045f + index * 1.3f) * 4f;
        return new Vector2(side * (FanPivot.X + MathF.Cos(angle) * radius + sway), FanPivot.Y - MathF.Sin(angle) * radius + bob);
    }

    // 0 at the hand, 1 at the slot, a small overshoot on the way: the piece swings past its place and settles.
    internal static float LiftEase(int age)
    {
        float x = Math.Clamp(age / (float)LiftEaseTicks, 0, 1) - 1;
        const float c = 1.15f;
        return 1 + (c + 1) * x * x * x + c * x * x;
    }

    // 0..1 draw-back in the ticks before a yank: the piece is pulled toward the hand as the silk loads.
    // The first piece leaves on the release itself, so it has none.
    internal static float Wind(int armedAge, int index)
    {
        int left = EbonRewardRules.YankTick(index) - armedAge;
        if (left <= 0 || left > WindTicks) return 0;
        return EbonRewardRules.Smooth(1 - (left - 1) / (float)WindTicks);
    }

    internal static float Spin(int index) => (index % 2 == 0 ? 1 : -1) * (.11f + .008f * index);
    // A hanging piece lags the owner's motion like a pendulum.
    internal static float Sway(int index, float tick, float velocityX)
        => MathF.Sin(tick * .05f + index * 1.1f) * .07f + Math.Clamp(velocityX * .012f, -.22f, .22f);

    // --- Arm pose ------------------------------------------------------------------------
    internal static float Side(int facing) => facing < 0 ? -1 : 1;
    internal static float RaisedAngle(int facing) => -MathF.PI * .5f + Side(facing) * .5f;
    internal static float WrapAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;
        while (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }
    // Front-hand direction in world radians. Raised and weaving while holding (a fifth of the way toward
    // the cursor), pointing at the cursor during the throw; `kick` flicks it up and back after each event.
    internal static float ArmAngle(int facing, float aim, float throwBlend, float kick, float tick)
    {
        float raised = RaisedAngle(facing) + MathF.Sin(tick * .11f) * .05f;
        float angle = raised + WrapAngle(aim - raised) * (.18f + .82f * Math.Clamp(throwBlend, 0, 1));
        return angle - Side(facing) * .32f * Math.Clamp(kick, 0, 1);
    }
    internal static float Kick(int ticksSince) => ticksSince < 0 ? 0 : MathF.Pow(Math.Clamp(1 - ticksSince / 10f, 0, 1), 2);
    internal static float LiftKick(int age, int lifted) => lifted <= 0 ? 0 : Kick(age - EbonRewardRules.LiftTick(lifted - 1));
    internal static float LaunchKick(int releaseAge, int lifted)
    {
        float best = 0;
        for (int i = 0; i < lifted; i++) best = Math.Max(best, Kick(releaseAge - EbonRewardRules.YankTick(i)));
        return best;
    }
    // Arm swings to the cursor within three ticks of the release and relaxes after the last yank.
    internal static float Throw(int releaseAge, int lifted)
    {
        float rise = EbonRewardRules.Smooth(releaseAge / 3f);
        float fall = EbonRewardRules.Smooth((releaseAge - LastLaunch(lifted)) / (float)RecoveryTicks);
        return rise * (1 - fall);
    }

    // --- Furniture -----------------------------------------------------------------------
    // armchair, candelabra, portrait, clock, birdcage, mirror, music box, cello
    internal static Material MaterialOf(int index) => (((index % 8) + 8) % 8) switch
    {
        1 or 2 or 5 => Material.Glass,
        4 => Material.Wire,
        _ => Material.Wood,
    };
    internal static int Seed(int identity, int index) => unchecked(identity * 7919 + index * 104729 + 17);

    // --- Finale --------------------------------------------------------------------------
    // Where the piano waits (PianoHeight above the release cursor) and the height it lands at.
    internal static (Vector2 Start, float TargetY) PianoSpawn(Vector2 cursor, float worldWidth, float worldHeight)
    {
        float x = Math.Clamp(cursor.X, PianoMargin, Math.Max(PianoMargin, worldWidth - PianoMargin));
        float target = Math.Clamp(cursor.Y, PianoMargin, Math.Max(PianoMargin, worldHeight - PianoMargin));
        return (new Vector2(x, Math.Max(PianoMargin, target - EbonRewardRules.PianoHeight)), target);
    }
    internal static bool PianoLanded(float bottomY, float targetY) => bottomY >= targetY;
    internal static bool CircleTouchesBox(Vector2 center, float radius, Vector2 min, Vector2 max)
    {
        Vector2 nearest = new(Math.Clamp(center.X, min.X, max.X), Math.Clamp(center.Y, min.Y, max.Y));
        return Vector2.DistanceSquared(center, nearest) <= radius * radius;
    }

    // --- Replicated state sanity (peers parse owner state completely before acting) -----------
    private static bool Whole(float value) => float.IsFinite(value) && value == MathF.Floor(value);
    internal static bool ValidController(float age, float lifted, float phase)
        => Whole(age) && age >= 0 && age < 1_000_000 && Whole(lifted) && lifted >= 0 && lifted <= EbonRewardRules.Pieces
           && Whole(phase) && phase >= Channeling && phase <= Releasing;
    internal static bool ValidPiece(float index, float state, float age)
        => Whole(index) && index >= 0 && index < EbonRewardRules.Pieces && Whole(state) && state >= Hanging && state <= Dropped
           && Whole(age) && age >= 0 && age < 1_000_000;
    // ai[2]: 0 alive, n > 0 crashed for n - 1 ticks, n < 0 dropped for -n - 1 ticks.
    internal static bool ValidPiano(float age, float targetY, float state)
        => Whole(age) && age >= 0 && age < 1_000_000 && float.IsFinite(targetY) && Math.Abs(targetY) < 10_000_000
           && Whole(state) && Math.Abs(state) < 10_000;
}
