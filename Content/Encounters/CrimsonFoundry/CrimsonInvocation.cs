using System;
using System.Collections.Generic;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Pure score-stage contracts. No player position enters a barrage layout.
internal static class CrimsonInvocation
{
    internal const int SummonCount = 3, AllDefeated = 7, ManifestTicks = 150;
    internal const int DeploymentTicks = 150, MusicLeadTicks = 120, MusicFadeTicks = 150;
    internal static int TargetLife(int members) => (12000000 + 8000000 * (Math.Clamp(members, 1, 8) - 1)) / 4;
    internal static byte Defeat(byte mask, int index)
    {
        if (index is < 0 or >= SummonCount || mask > AllDefeated) throw new ArgumentOutOfRangeException();
        return (byte)(mask | 1 << index);
    }
    internal static int SelectAlive(int cue, byte defeated)
    {
        for (int i = 0; i < SummonCount; i++)
        {
            int index = (Math.Abs(cue % SummonCount) + i) % SummonCount;
            if ((defeated & 1 << index) == 0) return index;
        }
        return SummonCount; // The summoner herself, never a fourth apparition.
    }
    internal static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static float MusicGain(double scoreAge) => .39f * Ease((float)scoreAge / MusicFadeTicks);
    internal static float Manifest(float age, int start) => start < 0 ? 0 : Ease((age - start) / ManifestTicks);
    internal static float OpeningBars(CrimsonStage stage, float age, int musicStart, int introTicks)
    {
        if (stage == CrimsonStage.Deployment)
            return Math.Min(Ease(age / 24), Ease((DeploymentTicks - age) / 35));
        if (musicStart < 0) return 0;
        float elapsed = age - (musicStart - MusicLeadTicks), duration = MusicLeadTicks + introTicks;
        return Math.Min(Ease(elapsed / 24), Ease((duration - elapsed) / 40));
    }
}

internal readonly record struct CrimsonLane(float X, float Y, float DX, float DY, float Length, float HalfWidth);
internal sealed record CrimsonBarrage(float NormalX, float NormalY, float SafeOffset, float SafeWidth, IReadOnlyList<CrimsonLane> Lanes);

internal static class CrimsonBarrageGeometry
{
    internal static CrimsonBarrage Build(RaidFieldGeometry field, int cue, int source, bool finale)
    {
        // Each apparition owns a recognizable orientation; the final act mixes
        // them. All lanes of one volley share ONE usable corridor, not separate
        // random gaps that intersect into an impossible damage field.
        uint seed = unchecked((uint)cue * 747796405u + 2891336453u);
        seed = (seed ^ seed >> 16) * 2246822519u;
        int kind = finale ? (int)(seed % 4) : source;
        float angle = kind switch { 0 => MathF.PI / 2, 1 => 0, 2 => MathF.PI / 4, _ => -MathF.PI / 4 };
        float dx = MathF.Cos(angle), dy = MathF.Sin(angle), nx = -dy, ny = dx;
        float extent = MathF.Abs(nx) * (field.Right - field.Left) * .5f + MathF.Abs(ny) * (field.Bottom - field.Top) * .5f;
        float gap = finale ? 190 : 250;
        // Nearby offsets, not edge-to-opposite-edge player baiting.
        float safe = ((int)(seed >> 5) % 5 - 2) * (kind == 1 ? 65 : 110);
        safe = Math.Clamp(safe, -extent + gap, extent - gap);
        var lanes = new List<CrimsonLane>(40);
        AddRegion(-extent, safe - gap * .5f);
        AddRegion(safe + gap * .5f, extent);
        return new(nx, ny, safe, gap, lanes);

        void AddRegion(float low, float high)
        {
            int count = (int)MathF.Ceiling((high - low) / 104);
            if (count <= 0) return;
            float step = (high - low) / count;
            for (int i = 0; i < count; i++)
            {
                float u = low + step * (i + .5f), width = step * .5f + .25f;
                float x = field.CenterX + nx * u, y = field.CenterY + ny * u;
                if (!field.ClipAxis(x, y, dx, dy, out float first, out float last)) continue;
                // Extend through corners by the footprint width; masking still
                // stops at the field. Diagonal bands have no unannounced wedges.
                first -= width * 2; last += width * 2;
                lanes.Add(new(x + dx * first, y + dy * first, dx, dy, last - first, width));
            }
        }
    }
}
