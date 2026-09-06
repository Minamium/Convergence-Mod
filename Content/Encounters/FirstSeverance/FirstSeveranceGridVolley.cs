using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

// One small descriptor, shared deterministic geometry; no projectile/NPC grid.
internal sealed class FirstSeveranceGridVolley
{
    internal const int TelegraphTicks = 60, ActiveTicks = 20, CadenceTicks = 102;
    internal const float Spacing = 160f, HalfWidth = 12f;
    internal const int MaximumLines = 32;
    internal const int MaximumCoreBeams = 4, CoreSalvoFirstSerial = 3;
    internal const float CoreBeamLength = 3000f, CoreBeamHalfWidth = 72f;
    internal uint Serial { get; }
    internal ulong StartTick { get; }
    internal byte Pattern { get; }
    internal ulong FireTick => StartTick + TelegraphTicks;
    internal ulong EndTick => FireTick + ActiveTicks;
    internal IReadOnlyList<FirstSeveranceLanceRay> Rays { get; }
    internal IReadOnlyList<FirstSeveranceLanceRay> CoreBeams { get; }

    internal FirstSeveranceGridVolley(uint serial, ulong startTick, byte pattern, float coreX, float groundY,
        IReadOnlyList<FirstSeveranceLanceRay>? coreBeams = null)
    {
        if (serial == 0 || startTick == 0 || startTick > ulong.MaxValue - 128 || pattern > 3
            || !float.IsFinite(coreX) || !float.IsFinite(groundY)
            || Math.Abs(coreX) > 1_000_000 || Math.Abs(groundY) > 1_000_000)
            throw new ArgumentException("Invalid lattice descriptor.");
        Serial = serial; StartTick = startTick; Pattern = pattern;
        var field = FirstSeveranceContainmentBounds.FromGround(coreX, groundY);
        var rays = new List<FirstSeveranceLanceRay>(MaximumLines);
        float offsetX = 40 + pattern * 40;
        float offsetY = 40 + ((pattern * 3) % 4) * 40;
        for (float x = field.Left + offsetX; x < field.Right; x += Spacing)
            rays.Add(new(x, field.Top, 0, 1, field.Bottom - field.Top, HalfWidth));
        for (float y = field.Top + offsetY; y < field.Bottom; y += Spacing)
            rays.Add(new(field.Left, y, 1, 0, field.Right - field.Left, HalfWidth));
        if (rays.Count > MaximumLines) throw new ArgumentException("Lattice exceeds bounded geometry.");
        Rays = Array.AsReadOnly(rays.ToArray());
        coreBeams ??= Array.Empty<FirstSeveranceLanceRay>();
        if (coreBeams.Count > MaximumCoreBeams || (serial < CoreSalvoFirstSerial && coreBeams.Count != 0))
            throw new ArgumentException("Invalid core salvo count or sequence.");
        foreach (var ray in coreBeams)
            if (!ray.IsValid || ray.X != coreX || ray.Y != groundY - FirstSeveranceLanceTuning.BossHeightAboveCore
                || ray.Length != CoreBeamLength || ray.HalfWidth != CoreBeamHalfWidth)
                throw new ArgumentException("Core salvo must originate at the Boss with the shared dimensions.");
        CoreBeams = FirstSeverancePlanCollections.Copy(coreBeams, nameof(coreBeams));
    }

    internal static FirstSeveranceLanceRay AimCoreBeam(float coreX, float groundY, float targetX, float targetY)
    {
        if (!float.IsFinite(targetX) || !float.IsFinite(targetY))
            throw new ArgumentException("Nonfinite core salvo target.");
        float y = groundY - FirstSeveranceLanceTuning.BossHeightAboveCore;
        float dx = targetX - coreX, dy = targetY - y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        return new(coreX, y, length > .001f ? dx / length : 0, length > .001f ? dy / length : 1,
            CoreBeamLength, CoreBeamHalfWidth);
    }

    internal bool CoreIntersects(ulong tick, float x, float y, float halfWidth, float halfHeight)
    {
        if (!IsFiring(tick)) return false;
        foreach (var ray in CoreBeams)
            if (ray.Intersects(x, y, halfWidth, halfHeight)) return true;
        return false;
    }

    internal bool IsFiring(ulong tick) => tick >= FireTick && tick < EndTick;
    internal bool Intersects(ulong tick, float x, float y, float halfWidth, float halfHeight)
    {
        if (!IsFiring(tick)) return false;
        foreach (var ray in Rays)
            if (ray.Intersects(x, y, halfWidth, halfHeight)) return true;
        return CoreIntersects(tick, x, y, halfWidth, halfHeight);
    }
}
