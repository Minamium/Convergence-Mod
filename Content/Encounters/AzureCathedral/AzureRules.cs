using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.AzureCathedral;

// Pure clocks and geometry: the same envelope drives native collision and art.
internal static class AzureRules
{
    internal const int Members = 8, Segments = 22, Deploy = 120, Intro = 960, Ending = 180;
    internal const int IceBreak = 180, SwordLight = 430, SkyReveal = 460, WormArrival = 610;
    internal const int PhraseTicks = 480, CycleTicks = PhraseTicks * 6;
    internal const int ChargeTicks = 240, ChargeWarning = 100, ChargeEnd = 188;
    internal const float SegmentSpacing = 86, SegmentRadius = 43;
    internal static int Life(int members, bool worm) => checked((worm ? 4800000 : 2400000) + (members - 1) * (worm ? 2600000 : 1300000));
    internal static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static float Envelope(float elapsed, float duration)
        => elapsed < 0 || elapsed >= duration ? 0 : Ease(elapsed / 9) * Ease((duration - elapsed) / 16);
    internal static int Phrase(int age, int unlock) => Math.Max(0, age - unlock) / PhraseTicks % 6;
    internal static bool ChargePhrase(int phrase) => phrase is 0 or 3;
    internal static bool ChorusPhrase(int phrase) => phrase is 2 or 5;
    internal static int Clock(int age, int unlock) => Math.Max(0, age - unlock) % PhraseTicks;
    internal static (float X, float Y) Clamp(RaidFieldGeometry field, float x, float y, int w, int h)
    {
        var p = field.ClampBody(x, y, w, h, 0);
        return (Math.Clamp(p.X, field.Left + 2, field.Right - w - 2), Math.Max(field.Top + 2, p.Y));
    }
    internal static float Sweep(float elapsed, float duration) => -.64f + 1.28f * Ease(elapsed / duration);
    internal static bool Completed(int wormLife, int girlLife) => wormLife <= 0 && girlLife <= 0;
}

// Defeat is a one-way authority fact, not the presence of an already-dead NPC.
internal sealed class AzureDefeats
{
    internal bool Girl { get; private set; }
    internal bool Worm { get; private set; }
    internal bool Mark(bool worm)
    {
        if (worm ? Worm : Girl) return false;
        if (worm) Worm = true; else Girl = true;
        return true;
    }
    internal bool RequiresChain(bool summoned) => summoned && !Worm;
}
