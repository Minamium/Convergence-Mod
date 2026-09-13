using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

// One small descriptor, shared deterministic geometry; no projectile/NPC grid.
internal sealed class FirstSeveranceGridVolley
{
    internal const int OpeningTicks = 60, TelegraphTicks = 60;
    internal const int ActiveTicks = FirstSeveranceGridPulse.TransitTicks + FirstSeveranceGridPulse.EmissionTicks;
    internal const int StaggerTicks = 30;
    internal const int DurationTicks = TelegraphTicks + ActiveTicks + StaggerTicks;
    internal const int CadenceTicks = DurationTicks + 14;
    internal const float Spacing = 160f, HalfWidth = 12f;
    internal const int MaximumLines = 96;
    // Keep protocol29's decoder capacity; current authority emits only one.
    internal const int MaximumCoreBeams = 4, CoreSalvoFirstSerial = 3;
    internal const float CoreBeamLength = 3000f, CoreBeamHalfWidth = 72f;
    internal uint Serial { get; }
    internal ulong StartTick { get; }
    internal byte Pattern { get; }
    internal ulong FireTick => StartTick + TelegraphTicks;
    internal ulong CoreEndTick => FireTick + ActiveTicks;
    internal ulong EndTick => CoreEndTick + StaggerTicks;
    private readonly int[] offsets;
    private readonly FirstSeveranceLanceRay[] tracks;
    internal IReadOnlyList<FirstSeveranceLanceRay> Rays { get; }
    internal IReadOnlyList<FirstSeveranceLanceRay> CoreBeams { get; }

    internal FirstSeveranceGridVolley(uint serial, ulong startTick, byte pattern, float coreX, float groundY,
        IReadOnlyList<FirstSeveranceLanceRay>? coreBeams = null)
    {
        if (serial == 0 || startTick == 0 || startTick > ulong.MaxValue - 128 || pattern > 11 || (pattern & 4) != 0
            || !float.IsFinite(coreX) || !float.IsFinite(groundY)
            || Math.Abs(coreX) > 1_000_000 || Math.Abs(groundY) > 1_000_000)
            throw new ArgumentException("Invalid lattice descriptor.");
        Serial = serial; StartTick = startTick; Pattern = pattern;
        var field = FirstSeveranceContainmentBounds.FromGround(coreX, groundY);
        var rays = new List<FirstSeveranceLanceRay>(MaximumLines);
        int layout = pattern & 3;
        float offsetX = 40 + layout * 40;
        float offsetY = 40 + ((layout * 3) % 4) * 40;
        for (float x = field.Left + offsetX; x < field.Right; x += Spacing)
            rays.Add(new(x, field.Top, 0, 1, field.Bottom - field.Top, HalfWidth));
        for (float y = field.Top + offsetY; y < field.Bottom; y += Spacing)
            rays.Add(new(field.Left, y, 1, 0, field.Right - field.Left, HalfWidth));
        var fullTracks = rays.ToArray();
        rays = FirstSeveranceSafeWindows.CutGrid(rays, pattern, coreX, groundY);
        if (rays.Count > MaximumLines) throw new ArgumentException("Lattice exceeds bounded geometry.");
        Rays = Array.AsReadOnly(rays.ToArray());
        // Fisher-Yates on the accepted descriptor, independent of runtime/library
        // Random implementations. Cut segments share their parent track's clock.
        int[] order = new int[fullTracks.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        uint seed = unchecked(serial * 747796405u ^ (uint)startTick ^ (uint)(startTick >> 32) ^ (uint)pattern * 2891336453u) | 1u;
        for (int i = order.Length - 1; i > 0; i--)
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            int j = (int)(seed % (uint)(i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }
        int[] trackOffsets = new int[order.Length];
        for (int rank = 0; rank < order.Length; rank++)
            trackOffsets[order[rank]] = rank * StaggerTicks / Math.Max(1, order.Length - 1);
        offsets = new int[rays.Count];
        tracks = new FirstSeveranceLanceRay[rays.Count];
        for (int line = 0; line < rays.Count; line++)
        {
            var segment = rays[line];
            int track = Array.FindIndex(fullTracks, candidate => candidate.DirectionX == segment.DirectionX
                && candidate.DirectionY == segment.DirectionY
                && (segment.DirectionX == 0 ? candidate.X == segment.X : candidate.Y == segment.Y));
            tracks[line] = fullTracks[track];
            offsets[line] = trackOffsets[track];
        }
        coreBeams ??= Array.Empty<FirstSeveranceLanceRay>();
        if ((pattern >= 4 && coreBeams.Count != 0) || coreBeams.Count > MaximumCoreBeams || (serial < CoreSalvoFirstSerial && coreBeams.Count != 0))
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

    internal static int CoreTargetIndex(uint salvoOrdinal, int eligibleCount)
    {
        if (eligibleCount < 1 || eligibleCount > FirstSeveranceRoster.MaximumCount)
            throw new ArgumentOutOfRangeException(nameof(eligibleCount));
        return (int)(salvoOrdinal % (uint)eligibleCount);
    }

    internal bool CoreIntersects(ulong tick, float x, float y, float halfWidth, float halfHeight)
    {
        if (tick < FireTick || tick >= CoreEndTick) return false;
        foreach (var ray in CoreBeams)
            if (FirstSeveranceBeamIgnition.At(ray, (double)tick - FireTick).Intersects(x, y, halfWidth, halfHeight)) return true;
        return false;
    }

    internal bool IsFiring(ulong tick) => tick >= FireTick && tick < EndTick;
    internal ulong RevealTick(int line) => StartTick + (ulong)offsets[line];
    internal ulong LineFireTick(int line) => FireTick + (ulong)offsets[line];
    internal ulong LineEndTick(int line) => LineFireTick(line) + ActiveTicks;
    internal bool LineIsLive(int line, ulong tick) => tick >= LineFireTick(line) && tick < LineEndTick(line);
    internal FirstSeveranceGridPulse PulseAt(int line, double tick)
        => new(tracks[line], Rays[line], tick - LineFireTick(line));
    internal bool Intersects(ulong tick, float x, float y, float halfWidth, float halfHeight)
    {
        if (!IsFiring(tick)) return false;
        for (int line = 0; line < Rays.Count; line++)
            if (LineIsLive(line, tick) && PulseAt(line, tick).Intersects(x, y, halfWidth, halfHeight)) return true;
        return CoreIntersects(tick, x, y, halfWidth, halfHeight);
    }
}
