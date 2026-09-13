using System;

namespace Convergence.Content.Encounters.GhostSamurai;

internal static class SamuraiWaveRules
{
    internal const float ChargedSlashWaveSpeed = 24;
    internal const float ChargedSlashWaveWidth = 64, ChargedSlashWaveHeight = 480;
    internal const int WaveLife = 90, MaximumWaveLife = 210;

    internal static SamuraiHazard Geometry(SamuraiHazard h, float age)
    {
        float travel = Math.Max(0, age - h.Fire) * ChargedSlashWaveSpeed - h.Length / 2;
        return h with { Shape = SamuraiShape.Slash, X = h.X + h.DX * travel, Y = h.Y + h.DY * travel };
    }

    // First actual OBB-vs-player-AABB contact tick at the captured reference.
    // This includes the leading edge and player size, not just center distance.
    internal static int FlightTicks(SamuraiHazard h, float x, float y, float halfX, float halfY)
    {
        var extended = h with { End = h.Fire + MaximumWaveLife };
        for (int flight = 0; flight < MaximumWaveLife; flight++)
            if (Geometry(extended, h.Fire + flight).Hits(h.Fire + flight, x, y, halfX, halfY)) return flight;
        throw new ArgumentOutOfRangeException(nameof(h), "ghost_samurai.wave_target_out_of_range");
    }

    internal static int GridFire(SamuraiHazard wave, int flight)
        => wave.Fire + flight - GhostSamuraiRules.GridToChargedSlashHitInterval;
}
