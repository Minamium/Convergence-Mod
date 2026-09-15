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
    internal const float HorizontalSlashArenaBottomOffset = 144, HorizontalSlashMoveSpeed = 42;
    // The accepted body atlas extends beyond the combat box (including its tail
    // and 3px idle bob). Keep that visible body clear without changing hitboxes.
    internal const float HorizontalBodyHalfWidth = 112, HorizontalBodyAbove = 168, HorizontalBodyBelow = 216;
    internal const int HorizontalSlashMinimumMoveTime = 18, HorizontalSlashChargeTime = 108;
    internal const int HorizontalSlashRaiseTime = 18, HorizontalSlashHoldTime = 12, HorizontalSlashSwingTime = 6;
    internal const float HorizontalSlashSwordStartScale = 1, HorizontalSlashSwordMaxScale = 2.1f;
    internal const float HorizontalSlashWaveStartScale = .65f, HorizontalSlashWaveMaxScale = 1.5f;
    internal const int HorizontalSlashWaveGrowTime = 48;
    internal const int CleaveWindup = HorizontalSlashChargeTime + HorizontalSlashSwingTime, CleaveTrackTime = 24, CleaveLive = 12;
    internal const int ShockDelay = 24, ShockDamage = 200;
    internal const float CleaveRadius = 2800;
    internal const float ShockSpeed = 14, ShockWidth = 80, ShockHalfHeight = 24;
    internal const int MaximumComboTime = 600; // Fault containment, including the longest field crossing.

    internal static bool IsCombo(SamuraiAttack attack) => attack is SamuraiAttack.TripleVerticalSlash or SamuraiAttack.FrontalCleaveShockwave;
    internal static int VerticalSpawnStep(int timer) => timer >= 0 && timer % VerticalCadence == 0 && timer / VerticalCadence < VerticalCount ? timer / VerticalCadence : -1;
    private static float Smooth(float t) { t = Math.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
    internal static int ApproachDuration(SamuraiComboSnapshot state)
    {
        float dx = state.AnchorX - state.FromX, dy = state.AnchorY - state.FromY;
        // SmoothStep's maximum derivative is 1.5. The shared duration caps actual
        // movement at MoveSpeed while preserving zero velocity at both ends.
        return Math.Max(HorizontalSlashMinimumMoveTime, (int)MathF.Ceiling(1.5f * MathF.Sqrt(dx * dx + dy * dy) / HorizontalSlashMoveSpeed));
    }
    internal static float ApproachProgress(float timer, SamuraiComboSnapshot state) => Smooth(timer / ApproachDuration(state));
    internal static bool TryHorizontalAnchorY(SamuraiArenaBounds field, Func<float, bool> bodyClear, out float y)
    {
        float first = field.Bottom - GhostSamuraiRules.BodyHeight / 2f - HorizontalSlashArenaBottomOffset;
        float last = field.Top + HorizontalBodyAbove + 16;
        for (y = first; y >= last; y -= 16)
            if (bodyClear(y)) return true;
        y = 0;
        return false; // A completely blocked center column cannot host this attack.
    }
    internal static float HorizontalSwordScale(float local)
    {
        float charge = Smooth((local - HorizontalSlashRaiseTime) / (HorizontalSlashChargeTime - HorizontalSlashRaiseTime - HorizontalSlashHoldTime));
        float recoil = 1 - Smooth((local - CleaveWindup - CleaveLive) / ShockDelay);
        return HorizontalSlashSwordStartScale + (HorizontalSlashSwordMaxScale - HorizontalSlashSwordStartScale) * charge * recoil;
    }
    internal static float HorizontalSwingProgress(float local)
    {
        float t = Math.Clamp((local - HorizontalSlashChargeTime) / HorizontalSlashSwingTime, 0, 1);
        return t * t;
    }
    internal static float HorizontalArmAngle(float local, int side, int facing, float idle)
    {
        float raised = -side * (side == facing ? 1.5f : .95f);
        float windup = idle + (raised - idle) * Smooth(local / HorizontalSlashRaiseTime);
        float end = side == facing ? facing * 1.4f : side * .7f;
        float swing = windup + (end - windup) * HorizontalSwingProgress(local);
        return swing + (idle - swing) * Smooth((local - CleaveWindup - CleaveLive) / ShockDelay);
    }
    internal static float ShockScale(float ageSinceFire) => HorizontalSlashWaveStartScale
        + (HorizontalSlashWaveMaxScale - HorizontalSlashWaveStartScale) * Smooth(ageSinceFire / HorizontalSlashWaveGrowTime);
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
    {
        float scale = ShockScale(age - h.Fire), width = ShockWidth * scale, radius = h.Radius * scale;
        // Expand upwards from the fixed ground plane. Both drawing and authority
        // use this same rectangle; projectile width/height never move its origin.
        return h with { Shape = SamuraiShape.Slash, X = h.X + h.DX * ShockDistance(h, age) - h.DX * width / 2,
            Y = h.Y + h.Radius - radius, Length = width, Radius = radius };
    }

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
