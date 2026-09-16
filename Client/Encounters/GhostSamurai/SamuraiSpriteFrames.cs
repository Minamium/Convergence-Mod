using System;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.Client.Encounters.GhostSamurai;

// Presentation only. No frame is allowed to advance an attack or create a hit.
internal static class SamuraiSpriteFrames
{
    internal const int Columns = 4, Rows = 3, Count = 12;
    internal static int Idle(float age) => (int)(age / 10) % 3;
    internal static int Select(SamuraiAttack attack, SamuraiBeat beat, float timer, float age, float pose)
    {
        if (attack == SamuraiAttack.Phase3CircleAttack) return beat == SamuraiBeat.Recovery ? Idle(age) : 11;
        if (attack == SamuraiAttack.Phase2DashSlash)
        {
            float local = timer % GhostSamuraiRules.DashCadence;
            if (local < GhostSamuraiRules.DashApproach) return 3;
            if (local < GhostSamuraiRules.DashApproach + GhostSamuraiRules.DashWarning) return 8;
            if (local < GhostSamuraiRules.DashApproach + GhostSamuraiRules.DashWarning + GhostSamuraiRules.DashLive) return 10;
            return Idle(age);
        }
        if (beat == SamuraiBeat.Recovery || Math.Abs(pose) < .03f) return Idle(age);
        bool heavy = attack is SamuraiAttack.ChargedSlash or SamuraiAttack.GridSlash or SamuraiAttack.FrontalCleaveShockwave;
        if (heavy) return beat == SamuraiBeat.Strike || pose > 0 ? 9 : 8;
        int strike = -1;
        if (attack == SamuraiAttack.DirectionalSlash)
            for (int i = 0; i < GhostSamuraiRules.DirectionalSlashCount; i++)
                if (timer >= GhostSamuraiRules.DirectionalSpawnTime(i) + GhostSamuraiRules.SlashWarning) strike = i;
        int blade = beat == SamuraiBeat.Strike || pose > 0 ? Math.Max(0, strike) : strike + 1;
        bool up = attack == SamuraiAttack.DirectionalSlash && blade % 2 == 1;
        return beat == SamuraiBeat.Strike || pose > 0 ? up ? 7 : 5 : up ? 6 : 4;
    }
    internal static (int X, int Y, int Width, int Height) Cell(int index, int width, int height)
    {
        index = Math.Clamp(index, 0, Count - 1);
        int col = index % Columns, row = index / Columns;
        int x = col * width / Columns, y = row * height / Rows;
        return (x, y, (col + 1) * width / Columns - x, (row + 1) * height / Rows - y);
    }
}
