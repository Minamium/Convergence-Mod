using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.AzureCathedral;

// Pure clocks and geometry: the same envelope drives native collision and art.
internal static class AzureRules
{
    // Temporary Cathedral-only playtest switch. Disable to restore every original
    // source budget and native final-damage limit without changing encounter plans.
    internal const bool DebugOneDamagePlaytest = true;
    internal static int NativeSourceDamage(int intended)
        => intended <= 0 ? 0 : DebugOneDamagePlaytest ? 1 : intended;
    internal static int NativeFinalDamageLimit(int intended)
        => NativeSourceDamage(intended);
    internal const int Members = 8, Segments = 44, Deploy = 120, Intro = 960, Ending = 180;
    internal const int IceBreak = 180, SwordLight = 430, SkyReveal = 460, WormArrival = 610;
    internal const int PhraseTicks = 480, CycleTicks = PhraseTicks * 6;
    internal const int ChargeTicks = 240, ChargeWarning = 100, ChargeEnd = 188;
    internal const int StagingTicks = 180, Devouring = 600, DevourRush = 180, DevourSlow = 252, DevourContact = 396;
    internal const int MeltRush = 240, MeltContact = 330, MeltPartTicks = 60, MeltEnding = 660;
    internal const int VolleyApproach = 80, VolleyTransit = 360, VolleyWarning = 24, VolleyFire = 260;
    internal const int CutWarning = 60, CutLive = 12, CutResidue = 20;
    internal const float SegmentSpacing = 86, SegmentRadius = 43;
    internal const float MouthReach = 126, CutRadius = 7;
    internal static int Life(int members, bool worm) => checked((worm ? 240000 : 2400000) + (members - 1) * (worm ? 130000 : 1300000));
    internal static int FuryLife(int duetMaximum) => checked(duetMaximum * 4);
    internal static bool Staged(int started, int age) => started >= 0 && age >= started + StagingTicks;
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
    internal static bool WormDamageable(AzurePhase phase, int index, bool live, int life, int maximum)
        => live && life > 0 && index >= 0 && index <= Segments
        && (phase == AzurePhase.Fury || phase == AzurePhase.Duet && index == 0 && life > WormFloor(maximum));
    internal static float VolleyProgress(float t) => Math.Clamp((t-VolleyApproach)/VolleyTransit,0,1);
    internal static float CutReach(float t) => Ease(t/2);
    internal static float CutWidth(float t) => CutReach(t)*(1-Ease((t-(CutLive-8))/8));
    // After a separate retreat, rush most of the distance, then visibly traverse
    // the final ~290px in 2.4 seconds. This is slow motion, not an overlapping hold.
    internal static float DevourTravel(float t)
        => t < DevourRush ? 0 : t < DevourSlow ? .86f * Ease((t-DevourRush)/(DevourSlow-DevourRush))
        : t < DevourContact ? .86f + .14f * Ease((t-DevourSlow)/(DevourContact-DevourSlow))
        : 1 + .08f * Ease((t-DevourContact)/45);
    internal static float JawOpening(float t)
        => Ease((t-(DevourRush-20))/80)*(1-Ease((t-(DevourContact-6))/16));
    internal static float FuryReveal(float t, int part) => Ease((t-DevourContact-14-part*.8f)/90);
    internal static float Melt(float elapsed, int segment)
        => Ease((elapsed - MeltContact - segment * (SegmentSpacing / 20)) / MeltPartTicks);
    internal static int VictoryCue => MeltContact+MeltPartTicks;
    internal static int MeltSettled => (int)MathF.Ceiling(MeltContact + Segments * (SegmentSpacing / 20) + MeltPartTicks);
    internal static float EndingSky(AzureStage stage, float elapsed)
        => 1-Ease((elapsed-(stage==AzureStage.Victory?MeltSettled:0)) /
            (stage==AzureStage.Victory?MeltEnding-MeltSettled:Ending));
    internal static float VictoryTitle(float elapsed)
        => Ease((elapsed-MeltSettled)/22)*Ease((MeltEnding-elapsed)/28);
    internal static bool Silhouette(float t) => t >= DevourContact-10 && t < DevourContact+14;
    internal static float MusicGain(int music, int ending, AzureStage stage, float age)
        => music < 0 ? 0 : Ease((age-music)/120) * (ending < 0 ? 1 :
            1-Ease((age-ending-(stage==AzureStage.Victory?MeltContact:0)) /
                (ExitDuration(stage)-(stage==AzureStage.Victory?MeltContact:0))));
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
