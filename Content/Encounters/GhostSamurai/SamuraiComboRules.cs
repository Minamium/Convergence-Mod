using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

// Shared event clocks and collision geometry for the two independent attack states.
internal static class SamuraiComboRules
{
    internal const int VerticalCount = 3, VerticalWindup = 42, VerticalLockLead = 16;
    internal const int VerticalForecast = 24, VerticalLive = 8, VerticalCadence = 66;
    internal const float VerticalHalfWidth = 72;
    internal const int VerticalDuration = (VerticalCount - 1) * VerticalCadence + VerticalWindup + VerticalLive + GhostSamuraiRules.RecoveryTime;
    internal const int CleaveApproach = 48, CleaveWindup = 108, CleaveTrackTime = 24, CleaveLive = 12;
    internal const int ShockDelay = 24, ShockDamage = 200;
    internal const float CleaveStandOff = 320, CleaveRadius = 2800;
    internal const float ShockSpeed = 14, ShockWidth = 80, ShockHalfHeight = 24;
    internal const int MaximumComboTime = 600; // Fault containment, including the longest field crossing.

    internal static bool IsCombo(SamuraiAttack attack) => attack is SamuraiAttack.TripleVerticalSlash or SamuraiAttack.FrontalCleaveShockwave;
    internal static int VerticalSpawnStep(int timer) => timer >= 0 && timer % VerticalCadence == 0 && timer / VerticalCadence < VerticalCount ? timer / VerticalCadence : -1;
    internal static float ApproachProgress(float timer)
    {
        float remaining = 1 - Math.Clamp((timer + 1) / CleaveApproach, 0, 1);
        return 1 - remaining * remaining * remaining;
    }
    internal static SamuraiHazard Vertical(float x, SamuraiArenaBounds arena, int born)
        => new(SamuraiShape.VerticalSlash, x, arena.Top, 0, 1, arena.HalfHeight * 2, VerticalHalfWidth,
            born, born + VerticalWindup, born + VerticalWindup + VerticalLive, GhostSamuraiRules.SlashDamage);
    internal static SamuraiHazard Cleave(float x, float y, int facing, int born)
        => new(SamuraiShape.FrontalCleave, x, y, facing, 0, 0, CleaveRadius,
            born, born + CleaveWindup, born + CleaveWindup + CleaveLive, GhostSamuraiRules.SlashDamage);
    internal static SamuraiHazard Shock(float x, float groundY, int direction, float distance, int born)
        => new(SamuraiShape.GroundShockwave, x, groundY - ShockHalfHeight, direction, 0, distance, ShockHalfHeight,
            born, born + ShockDelay, born + ShockDelay + (int)MathF.Ceiling(distance / ShockSpeed) + 1, ShockDamage);
    internal static float ShockDistance(SamuraiHazard h, float age) => Math.Clamp((age - h.Fire) * ShockSpeed, 0, h.Length);
    internal static SamuraiHazard ShockGeometry(SamuraiHazard h, float age)
        => h with { Shape = SamuraiShape.Slash, X = h.X + h.DX * (ShockDistance(h, age) - ShockWidth / 2), Length = ShockWidth };

    internal static bool CleaveHits(SamuraiHazard h, float x, float y, float halfX, float halfY)
    {
        // Clip the full player AABB against the forward half-plane, then test the
        // nearest point against the disc. No portion behind the pivot can hit.
        float forward = (x - h.X) * h.DX;
        if (forward + halfX < 0) return false;
        float nearX = Math.Max(0, forward - halfX), nearY = Math.Max(0, Math.Abs(y - h.Y) - halfY);
        return nearX * nearX + nearY * nearY <= h.Radius * h.Radius;
    }

    internal static float SwingPose(float tick, float warning, float live, float recovery)
    {
        if (tick < 0) return 0;
        if (tick < warning) return -MathF.Sin(Math.Clamp(tick / 16, 0, 1) * MathF.PI / 2);
        if (tick < warning + 5) { float t = (tick - warning) / 5; return -1 + 2 * t * t; }
        if (tick < warning + live) return 1;
        float recoil = Math.Clamp((tick - warning - live) / recovery, 0, 1);
        return 1 - recoil * recoil * (3 - 2 * recoil);
    }
    internal static float VerticalPose(float tick)
    {
        int step = Math.Clamp((int)tick / VerticalCadence, 0, VerticalCount - 1);
        return SwingPose(tick - step * VerticalCadence, VerticalWindup, VerticalLive,
            step == VerticalCount - 1 ? GhostSamuraiRules.RecoveryTime : VerticalCadence - VerticalWindup - VerticalLive);
    }
}

// Full server-owned pose state. Approach anchors allow clients to evaluate the
// same path; step/facing/lock repair independently of individual projectile order.
internal readonly record struct SamuraiComboSnapshot(int Step, int Facing, bool Locked,
    float FromX, float FromY, float AnchorX, float AnchorY, float GroundY)
{
    internal bool IsValid(SamuraiAttack attack) => !SamuraiComboRules.IsCombo(attack) ? this == default
        : Step >= 0 && Step <= (attack == SamuraiAttack.TripleVerticalSlash ? 3 : 2)
            && Facing is -1 or 1 && Finite(FromX) && Finite(FromY) && Finite(AnchorX) && Finite(AnchorY) && Finite(GroundY);
    private static bool Finite(float v) => float.IsFinite(v) && Math.Abs(v) <= 500000;
    internal bool TryAdvance(int step, out SamuraiComboSnapshot next)
    {
        next = this;
        if (step != Step + 1 || step > 3) return false;
        next = this with { Step = step, Locked = false };
        return true;
    }
    internal void Write(BinaryWriter w)
    {
        w.Write((byte)Step); w.Write((sbyte)Facing); w.Write(Locked);
        w.Write(FromX); w.Write(FromY); w.Write(AnchorX); w.Write(AnchorY); w.Write(GroundY);
    }
    internal static SamuraiComboSnapshot Read(BinaryReader r, SamuraiAttack attack)
    {
        int step = r.ReadByte(), facing = r.ReadSByte(), flag = r.ReadByte();
        var state = new SamuraiComboSnapshot(step, facing, flag == 1, r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        if (flag > 1 || !state.IsValid(attack)) throw new InvalidDataException("ghost_samurai.combo_invalid");
        return state;
    }
}
