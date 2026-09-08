#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public enum RitualArmamentKind { Melee, Ranged, Magic, Summon, Rogue }

// Nominal non-critical, zero-defense output budgets, NOT measured Calamity DPS.
// Keep secondary damage in the same budget; never multiply damage by VFX count.
internal static class RitualArmamentRules
{
    internal const float TargetUpgrade = 1.10f;
    internal const float AcquisitionRange = 1800;
    internal const float RetainRange = 2100;
    internal const float TurnRate = .24f; // radians/game tick, independent of extraUpdates
    internal const int ChoirPeriod = 36;
    internal static int Damage(RitualArmamentKind kind) => (int)MathF.Round(TargetUpgrade * (kind switch
    {
        RitualArmamentKind.Melee => 3850,
        RitualArmamentKind.Ranged => 1820,
        RitualArmamentKind.Magic => 1840,
        RitualArmamentKind.Summon => 880,
        RitualArmamentKind.Rogue => 8800,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    }));
    internal static int UseTicks(RitualArmamentKind kind) => kind switch
    {
        RitualArmamentKind.Melee => 22,
        RitualArmamentKind.Ranged => 12,
        RitualArmamentKind.Magic => 20,
        RitualArmamentKind.Summon => 24,
        RitualArmamentKind.Rogue => 40,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    internal static int ScaledDamage(int damage, float factor)
        => (int)Math.Clamp(Math.Round((double)Math.Max(0, damage) * factor), 1, int.MaxValue / 4);
    internal static float Smooth(float x)
    {
        x = Math.Clamp(x, 0, 1);
        return Math.Clamp(x * x * x * (10 + x * (-15 + 6 * x)), 0, 1);
    }
    internal static float Envelope(float age, float attack, float release, float end)
        => Smooth(age / Math.Max(.001f, attack)) * (1 - Smooth((age - release) / Math.Max(.001f, end - release)));
    internal static Vector2 Steer(Vector2 velocity, Vector2 delta, float speed, float dt)
    {
        if (!Finite(velocity) || !Finite(delta) || !float.IsFinite(speed) || speed <= 0 || !float.IsFinite(dt) || dt <= 0)
            return Vector2.Zero;
        float current = velocity.LengthSquared() > .0001f ? MathF.Atan2(velocity.Y, velocity.X) : MathF.Atan2(delta.Y, delta.X);
        float wanted = delta.LengthSquared() > .0001f ? MathF.Atan2(delta.Y, delta.X) : current;
        float turn = MathF.IEEERemainder(wanted - current, MathF.Tau);
        float angle = current + Math.Clamp(turn, -TurnRate * dt, TurnRate * dt);
        float magnitude = velocity.Length() + (speed - velocity.Length()) * (1 - MathF.Exp(-.32f * dt));
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * magnitude;
    }
    internal static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    internal static float RangedMultiplier(int shot) => shot % 6 == 5 ? 2.1f : 1;
    internal static float ChoirMultiplier(int shot) => shot % 3 == 2 ? 1.4f : 1;
    internal static float RogueShare(bool returning) => returning ? .3f : .7f;
}
