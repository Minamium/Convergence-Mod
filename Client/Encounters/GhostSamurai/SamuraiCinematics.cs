#nullable enable
using System;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.Client.Encounters.GhostSamurai;

internal enum SamuraiCut : byte { None, Summon, Phase, Victory, Defeat }

// Presentation only: the clocks of the four Ghost Samurai cinematics. The summon and
// change-of-form cuts ride the authority's harmless windows (IntroTime, TransitionTime)
// and end with them, so the camera and HUD are back before any forecast exists; the
// victory and defeat cuts play over a fight that is already over and never delay its
// cleanup, rewards or a re-summon. Terraria-free so the domain tests can check it.
internal static class SamuraiCinematics
{
    internal const int SummonDuration = GhostSamuraiRules.IntroTime, PhaseDuration = GhostSamuraiRules.TransitionTime;
    internal const int VictoryDuration = 240, DefeatDuration = 210;
    // The summoning runs the samurai's death backwards, then it takes its stance and shouts.
    internal const int ManifestStart = 24, ManifestEnd = ManifestStart + SamuraiRigMotion.DeathDuration;
    internal const int StanceStart = 128, StanceEnd = 236, SummonShout = 136;
    internal const int PhaseShout = 22, PhaseBurst = 44;
    // Victory: both blades slip from the dissolving hands, turn once and plant in the floor;
    // the seal is held over them and melts with them.
    internal const int BladeRelease = 22, BladeLand = 62, BladeFade = 170, VictorySealHold = 130, RequiemBell = 104;
    // Defeat: the samurai lowers its blades over the fallen and returns to mist.
    internal const int DefeatDepart = 44, DefeatSealHold = 110, DefeatBell = 30;
    internal const float BladePlantOffset = 112, BladeBuried = 8;

    internal static int Duration(SamuraiCut cut) => cut switch
    {
        SamuraiCut.Summon => SummonDuration,
        SamuraiCut.Phase => PhaseDuration,
        SamuraiCut.Victory => VictoryDuration,
        SamuraiCut.Defeat => DefeatDuration,
        _ => 0,
    };

    // 0 outside [start, end], easing in over rise and out over fall.
    internal static float Window(float t, float start, float end, float rise, float fall)
        => SamuraiRigMotion.Smooth((t - start) / rise) * SamuraiRigMotion.Smooth((end - t) / fall);

    internal static float Bars(SamuraiCut cut, float t) => cut == SamuraiCut.None ? 0 : Window(t, 0, Duration(cut), 24, 30);

    internal static float Camera(SamuraiCut cut, float t) => cut switch
    {
        SamuraiCut.Summon => Window(t, 0, SummonDuration, 50, 56),
        SamuraiCut.Phase => Window(t, 0, PhaseDuration, 26, 36),
        SamuraiCut.Victory => Window(t, 0, VictoryDuration, 30, 44),
        SamuraiCut.Defeat => Window(t, 0, DefeatDuration, 36, 40),
        _ => 0,
    };

    internal static float Title(SamuraiCut cut, float t) => cut switch
    {
        SamuraiCut.Summon => Window(t, SummonShout + 4, SummonDuration - 34, 26, 30),
        SamuraiCut.Phase => Window(t, PhaseBurst, PhaseDuration - 30, 20, 26),
        SamuraiCut.Victory => Window(t, 96, VictoryDuration - 30, 30, 34),
        SamuraiCut.Defeat => Window(t, 56, DefeatDuration - 26, 30, 30),
        _ => 0,
    };

    // How far the summoning has run the death backwards: DeathDuration = nothing yet, 0 = whole.
    internal static float ManifestAge(float age)
        => SamuraiRigMotion.DeathDuration * (1 - SamuraiRigMotion.Clamp((age - ManifestStart) / (ManifestEnd - ManifestStart)));
    internal static bool Manifesting(float age) => age < ManifestEnd;
    internal static bool Stance(float age) => age >= StanceStart && age < StanceEnd;
    internal static float DepartAge(float t) => Math.Max(0, t - DefeatDepart) * .62f;

    // A falling blade's hilt and angle (0 = +x, y down). It leaves the hand at
    // BladeRelease, is flung up a little, turns once and plants its tip in the floor
    // beside the body at BladeLand, then rocks to rest about the buried tip.
    internal static (float X, float Y, float Angle) FallenBlade(float t, float handX, float handY, float angle0,
        float length, int side, float bodyX, float floorY, float left, float right)
    {
        float rest = MathF.PI / 2 + side * .14f;
        float tipX = Math.Clamp(bodyX + side * BladePlantOffset, left + 48, right - 48), tipY = floorY + BladeBuried;
        if (t <= BladeRelease) return (handX, handY, angle0);
        if (t >= BladeLand)
        {
            float s = t - BladeLand;
            float a = rest + MathF.Sin(s * .9f) * .09f * MathF.Exp(-s / 9);
            return (tipX - MathF.Cos(a) * length, tipY - MathF.Sin(a) * length, a);
        }
        float u = SamuraiRigMotion.Clamp((t - BladeRelease) / (BladeLand - BladeRelease));
        float endX = tipX - MathF.Cos(rest) * length, endY = tipY - MathF.Sin(rest) * length;
        float x = SamuraiRigMotion.Mix(handX, endX, SamuraiRigMotion.Out(u));
        float y = SamuraiRigMotion.Mix(handY, endY, u * u) - 280 * u * (1 - u);
        float angle = angle0 + (SamuraiRigMotion.Wrap(rest - angle0) + side * MathF.Tau) * MathF.Pow(u, 1.3f);
        return (x, y, angle);
    }
}
