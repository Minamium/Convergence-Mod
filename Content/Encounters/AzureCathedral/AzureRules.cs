using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.AzureCathedral;

// Pure clocks and geometry: the same envelope drives native collision and art.
internal static class AzureRules
{
    internal const int Members = 8, Segments = 44, Deploy = 120, Intro = 960, Ending = 180;
    internal const int IceBreak = 180, SwordLight = 430, SkyReveal = 460, WormArrival = 610;
    internal const int PhraseTicks = 480, CycleTicks = PhraseTicks * 6;
    internal const int ChargeTicks = 240, ChargeWarning = 100, ChargeEnd = 188;
    internal const int Devouring = 480, DevourRush = 150, DevourSlow = 222, DevourContact = 306, MeltEnding = 420;
    internal const int VolleyApproach = 120, VolleyWarning = 24, VolleyFire = 174;
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
    internal static int WormFloor(int maximum) => Math.Max(1, (maximum + 4) / 5);
    internal static bool CanDevour(AzurePhase phase, int girlLife, int wormLife, int maximum)
        => phase == AzurePhase.Duet && girlLife <= 0 && wormLife <= WormFloor(maximum);
    internal static bool CanRefill(AzurePhase phase, int started, int age)
        => phase == AzurePhase.Devouring && age >= started + Devouring;
    internal static int ExitDuration(AzureStage stage) => stage == AzureStage.Victory ? MeltEnding : Ending;
    // A fast approach brakes at the mouth, then the last few pixels last over a second.
    internal static float DevourTravel(float t)
        => t < DevourRush ? 0 : t < DevourSlow ? .965f * Ease((t-DevourRush)/(DevourSlow-DevourRush))
        : t < DevourContact ? .965f + .035f * Ease((t-DevourSlow)/(DevourContact-DevourSlow))
        : 1 + .12f * Ease((t-DevourContact)/45);
    internal static float Melt(float elapsed, int segment)
        => Ease((elapsed - 95 - segment * 2.6f) / 160);
    internal static bool Silhouette(float t) => t >= DevourContact-10 && t < DevourContact+14;
    internal static float MusicGain(int music, int ending, AzureStage stage, float age)
        => music < 0 ? 0 : Ease((age-music)/120) * (ending < 0 ? 1 : 1-Ease((age-ending)/ExitDuration(stage)));
    internal static float ThroatRadius(float distance, float radius)
        => radius * (Math.Clamp(32 / Math.Max(2*radius,1),.16f,1) + (1-Math.Clamp(32/Math.Max(2*radius,1),.16f,1))*MathF.Pow(Math.Clamp(distance/260,0,1),1.65f));
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
