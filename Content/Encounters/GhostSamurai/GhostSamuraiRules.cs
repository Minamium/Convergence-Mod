using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

internal enum SamuraiPhase : byte { Phase1 = 1, Phase2 = 2, Phase3 = 3 }
internal enum SamuraiAttack : byte { Idle, DirectionalSlash, ChargedSlash, GridSlash, Phase2DashSlash }
internal enum SamuraiBeat : byte { Recovery, Approach, Telegraph, Strike, Transition }
internal enum SamuraiShape : byte { Slash, Wisp }

// Shared by the authority, read-only presentation and dependency-free tests.
internal static class GhostSamuraiRules
{
    internal const int Life = 2_400_000, Defense = 160;
    internal const float Phase2Threshold = .66f, Phase3Threshold = .33f;
    internal const int TransitionTime = 90, AttackIntervalPhase1 = 36, AttackIntervalPhase2 = 24, RecoveryTime = 18;
    internal const int SlashWarning = 54, SlashLive = 10, DirectionalSlashInterval = 12, DirectionalPairInterval = 48;
    internal const int DirectionalPairCount = 4, DirectionalSlashCount = DirectionalPairCount * 2;
    internal const int DirectionalDuration = (DirectionalPairCount - 1) * DirectionalPairInterval + DirectionalSlashInterval + SlashWarning + SlashLive + RecoveryTime;
    internal const int ChargeAimTime = 48, ChargeWarning = 72, ChargeLive = 12;
    internal const int AimLockLead = 16, AimSyncInterval = 3, ChargeComboGap = 6;
    internal const int ChargeSecondWarning = 30, ChargeThirdWarning = 48;
    internal const int GridPrelude = 36, GridWarning = 84, GridLive = 12;
    internal const int GridEnd = GridPrelude + GridWarning + GridLive;
    internal const int GridFollowWarning = 48, GridFollowGap = 24, GridFollowStart = GridEnd + GridFollowGap - GridFollowWarning;
    internal const int DashApproach = 48, DashWarning = 54, DashLive = 18, DashRecovery = RecoveryTime;
    internal const int DashCadence = DashApproach + DashWarning + DashLive + DashRecovery;
    internal const float SlashLength = 1400, SlashHalfWidth = 32, ChargeHalfWidth = 150;
    internal const float GridWidth = 2520, GridHeight = 2520, GridSpacing = 180, GridHalfWidth = 34;
    internal const int GridVerticalLineCount = (int)(GridWidth / GridSpacing) + 1;
    internal const int GridHorizontalLineCount = (int)(GridHeight / GridSpacing) + 1;
    internal const float DashDistance = 1800, DashHalfWidth = 50, DashStandOff = 900, DashRetreatSpeed = 38;
    internal const int WispDelay = 18, WispBurstInterval = 18, SpreadDuration = 30, WispLife = 180, MaximumWisps = 18;
    // The follow-up charge forecast overlaps the end of the grid warning/live window.
    internal const int MaximumHazards = GridVerticalLineCount + GridHorizontalLineCount + 1 + MaximumWisps;
    internal const int WispSyncInterval = 6;
    internal const float SpreadSpeed = 3.5f, HomingSpeed = 4.5f, HomingStrength = .035f, WispRadius = 15;
    internal const int SlashDamage = 260, ChargeDamage = 380, GridDamage = 280, WispDamage = 200;
    internal const int HitCooldown = 50, AbandonTime = 180;
    internal const float AbandonDistance = 4000;

    internal static int AttackInterval(SamuraiPhase phase) => phase == SamuraiPhase.Phase1 ? AttackIntervalPhase1 : AttackIntervalPhase2;

    internal static int DirectionalSpawnTime(int step) => step / 2 * DirectionalPairInterval + step % 2 * DirectionalSlashInterval;
    internal static int DirectionalSpawnStep(int tick)
    {
        if (tick < 0 || tick / DirectionalPairInterval >= DirectionalPairCount) return -1;
        int local = tick % DirectionalPairInterval;
        return local == 0 ? tick / DirectionalPairInterval * 2
            : local == DirectionalSlashInterval ? tick / DirectionalPairInterval * 2 + 1 : -1;
    }

    private static int DirectionalStrike(float tick)
    {
        for (int i = DirectionalSlashCount - 1; i >= 0; i--)
            if (tick >= DirectionalSpawnTime(i) + SlashWarning) return i;
        return -1;
    }

    internal static SamuraiBeat DirectionalBeat(float tick)
    {
        int last = DirectionalStrike(tick);
        if (last >= 0 && tick < DirectionalSpawnTime(last) + SlashWarning + SlashLive) return SamuraiBeat.Strike;
        return tick < DirectionalSpawnTime(DirectionalSlashCount - 1) + SlashWarning ? SamuraiBeat.Telegraph : SamuraiBeat.Recovery;
    }

    // Later warnings overlap earlier strikes. The blades follow the strike clock,
    // returning continuously into the next held pose rather than resetting at spawn.
    internal static float DirectionalPose(float tick)
    {
        if (tick < SlashWarning) return -MathF.Sin(Math.Clamp(tick / 16, 0, 1) * MathF.PI / 2);
        int strike = DirectionalStrike(tick);
        float local = tick - SlashWarning - DirectionalSpawnTime(strike);
        if (local < 5) return -1 + 2 * (local / 5) * (local / 5);
        if (local < SlashLive) return 1;
        bool last = strike == DirectionalSlashCount - 1;
        float recoil = Math.Clamp((local - SlashLive) / (last ? RecoveryTime : DirectionalSpawnTime(strike + 1) - DirectionalSpawnTime(strike) - SlashLive), 0, 1);
        return 1 - (last ? 1 : 2) * recoil * recoil * (3 - 2 * recoil);
    }

    internal static int ChargeCount(SamuraiPhase phase) => phase == SamuraiPhase.Phase1 ? 1 : 3;
    internal static int ChargeWindup(int pass, bool afterGrid) => pass == 0 ? (afterGrid ? GridFollowWarning : ChargeWarning)
        : pass == 1 ? ChargeSecondWarning : ChargeThirdWarning;
    internal static int ChargeStart(int pass, bool afterGrid)
    {
        int start = 0;
        for (int i = 0; i < pass; i++) start += ChargeWindup(i, afterGrid) + ChargeLive + ChargeComboGap;
        return start;
    }
    internal static int ChargePass(float tick, SamuraiPhase phase, bool afterGrid)
    {
        for (int pass = ChargeCount(phase) - 1; pass > 0; pass--)
            if (tick >= ChargeStart(pass, afterGrid)) return pass;
        return 0;
    }
    internal static int ChargeDuration(SamuraiPhase phase, bool afterGrid)
    {
        int last = ChargeCount(phase) - 1;
        return ChargeStart(last, afterGrid) + ChargeWindup(last, afterGrid) + ChargeLive + RecoveryTime;
    }
    internal static float ChargePose(float tick, SamuraiPhase phase, bool afterGrid)
    {
        if (tick < 0) return 0;
        int pass = ChargePass(tick, phase, afterGrid);
        float local = tick - ChargeStart(pass, afterGrid);
        int warning = ChargeWindup(pass, afterGrid);
        if (local < warning) return afterGrid && pass == 0 ? -1 : -MathF.Sin(Math.Clamp(local / 16, 0, 1) * MathF.PI / 2);
        if (local < warning + 5) { float release = (local - warning) / 5; return -1 + 2 * release * release; }
        if (local < warning + ChargeLive) return 1;
        int recovery = pass == ChargeCount(phase) - 1 ? RecoveryTime : ChargeComboGap;
        float recoil = Math.Clamp((local - warning - ChargeLive) / recovery, 0, 1);
        return 1 - recoil * recoil * (3 - 2 * recoil);
    }

    // Preserve one base opportunity per old slash/pattern; two opportunities yield
    // three separated bursts, instead of doubling wisps with the new paired strikes.
    internal static int WispBursts(int opportunity) => 1 + (opportunity & 1);

    internal static SamuraiHazard GridLine(bool vertical, int index, float x, float y, int born)
    {
        int count = vertical ? GridVerticalLineCount : GridHorizontalLineCount;
        if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        float offset = (index - (count - 1) * .5f) * GridSpacing;
        return new(SamuraiShape.Slash, x + (vertical ? offset : -GridWidth / 2), y + (vertical ? -GridHeight / 2 : offset),
            vertical ? 0 : 1, vertical ? 1 : 0, vertical ? GridHeight : GridWidth, GridHalfWidth,
            born, born + GridWarning, born + GridWarning + GridLive, GridDamage);
    }

    internal static SamuraiPhase NextPhase(SamuraiPhase current, int life, int maximum)
    {
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (current == SamuraiPhase.Phase1 && life <= maximum * Phase2Threshold) return SamuraiPhase.Phase2;
        if (current == SamuraiPhase.Phase2 && life <= maximum * Phase3Threshold) return SamuraiPhase.Phase3;
        return current;
    }

    // One bounded draw selects uniformly among attacks other than the previous one.
    // Phase3 deliberately shares Phase2's pool until its own score is commissioned.
    internal static SamuraiAttack SelectNextAttack(SamuraiPhase phase, SamuraiAttack previous, int choice)
    {
        int count = phase == SamuraiPhase.Phase1 ? 3 : 4;
        int choices = count - ((int)previous is >= 1 && (int)previous <= count ? 1 : 0);
        if (choice < 0 || choice >= choices) throw new ArgumentOutOfRangeException(nameof(choice));
        for (int candidate = 1; candidate <= count; candidate++)
            if ((SamuraiAttack)candidate != previous && choice-- == 0) return (SamuraiAttack)candidate;
        throw new InvalidOperationException();
    }

    internal static float DashProgress(float tick)
    {
        float t = Math.Clamp(tick / DashLive, 0, 1);
        // Fast acceleration, continuous endpoints; no position teleport.
        return t * t * (3 - 2 * t);
    }

    internal static int Damage(SamuraiAttack attack) => attack switch
    {
        SamuraiAttack.ChargedSlash => ChargeDamage,
        SamuraiAttack.GridSlash => GridDamage,
        _ => SlashDamage,
    };
}

// A slash is an oriented rectangle; warning and hit use these exact endpoints/width.
// A wisp's immutable geometry carries its initial outward direction and live window;
// its moving center is supplied by the authority's SamuraiWispMotion.
internal readonly record struct SamuraiHazard(SamuraiShape Shape, float X, float Y, float DX, float DY,
    float Length, float Radius, int Born, int Fire, int End, int Damage)
{
    internal bool IsValid => Enum.IsDefined(Shape) && float.IsFinite(X) && float.IsFinite(Y)
        && Math.Abs(X) <= 500000 && Math.Abs(Y) <= 500000
        && float.IsFinite(DX) && float.IsFinite(DY) && Math.Abs(DX * DX + DY * DY - 1) < .01f
        && float.IsFinite(Length) && Length is >= 0 and <= 3000
        && float.IsFinite(Radius) && Radius is >= 1 and <= 200
        && Born >= 0 && Fire - Born is >= 24 and <= 180 && End > Fire && End - Fire <= 240
        && Damage is > 0 and <= 2000;

    internal bool Live(float age) => age >= Fire && age < End;
    internal bool Hits(float age, float x, float y, float halfX, float halfY)
    {
        if (Shape != SamuraiShape.Slash || !Live(age)) return false;
        // Exact separating-axis test: the two AABB axes and the two slash axes.
        float rx = x - (X + DX * Length * .5f), ry = y - (Y + DY * Length * .5f);
        return Math.Abs(rx) <= halfX + Math.Abs(DX) * Length * .5f + Math.Abs(DY) * Radius
            && Math.Abs(ry) <= halfY + Math.Abs(DY) * Length * .5f + Math.Abs(DX) * Radius
            && Math.Abs(rx * DX + ry * DY) <= Length * .5f + Math.Abs(DX) * halfX + Math.Abs(DY) * halfY
            && Math.Abs(-rx * DY + ry * DX) <= Radius + Math.Abs(DY) * halfX + Math.Abs(DX) * halfY;
    }

    internal void Write(BinaryWriter w)
    {
        w.Write((byte)Shape); w.Write(X); w.Write(Y); w.Write(DX); w.Write(DY); w.Write(Length);
        w.Write(Radius); w.Write(Born); w.Write(Fire); w.Write(End); w.Write(Damage);
    }

    internal static SamuraiHazard Read(BinaryReader r)
    {
        var result = new SamuraiHazard((SamuraiShape)r.ReadByte(), r.ReadSingle(), r.ReadSingle(),
            r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
        if (!result.IsValid) throw new InvalidDataException("ghost_samurai.hazard_invalid");
        return result;
    }
}
