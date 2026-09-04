#nullable enable

using System;
using Convergence.Common.Encounters.Runtime;
using Terraria.ModLoader;

namespace Convergence.Common.Compatibility.Calamity;

internal sealed class CalamityCompatibilitySystem : ModSystem
{
    private const string CalamityInternalName = "CalamityMod";
    private const string GetBossDownedCall = "GetBossDowned";
    private const string GetDifficultyActiveCall = "GetDifficultyActive";
    private const string ExoMechsProgressionKey = "exo mechs";
    private const string SupremeCalamitasProgressionKey = "supreme calamitas";
    private const string BossRushDifficultyKey = "bossrush";
    private static readonly Version MinimumSupportedVersion = new(2, 2, 4);
    private static readonly Version MaximumExclusiveVersion = new(2, 3, 0);
    private static Mod? detectedMod;

    internal static Version? DetectedVersion { get; private set; }

    internal static bool IsSupported { get; private set; }

    public override void PostSetupContent()
    {
        EncounterActivationPolicyCatalogSystem.Register(
            CalamityCompatibilityActivationPolicy.Instance);

        IsSupported = false;
        DetectedVersion = null;
        detectedMod = null;

        if (!ModLoader.TryGetMod(CalamityInternalName, out Mod calamity))
        {
            Mod.Logger.Error("Calamity Mod is required but was not loaded.");
            return;
        }

        DetectedVersion = calamity.Version;
        detectedMod = calamity;
        IsSupported = DetectedVersion.CompareTo(MinimumSupportedVersion) >= 0
            && DetectedVersion.CompareTo(MaximumExclusiveVersion) < 0;

        if (!IsSupported)
        {
            Mod.Logger.Warn(
                $"Calamity version {DetectedVersion} is outside the tested range "
                + $"[{MinimumSupportedVersion}, {MaximumExclusiveVersion}). "
                + "Encounter activation will remain disabled.");
        }
    }

    public override void Unload()
    {
        detectedMod = null;
        DetectedVersion = null;
        IsSupported = false;
    }

    internal static CalamityFirstSeveranceGateResult EvaluateFirstSeveranceGate()
    {
        if (!IsSupported || detectedMod is null)
        {
            return CalamityFirstSeveranceGateResult.Reject(
                "compatibility.calamity_unsupported");
        }

        if (!TryReadBooleanCall(
                detectedMod,
                GetBossDownedCall,
                ExoMechsProgressionKey,
                out bool exoMechsDefeated)
            || !TryReadBooleanCall(
                detectedMod,
                GetBossDownedCall,
                SupremeCalamitasProgressionKey,
                out bool supremeCalamitasDefeated)
            || !TryReadBooleanCall(
                detectedMod,
                GetDifficultyActiveCall,
                BossRushDifficultyKey,
                out bool bossRushActive))
        {
            return CalamityFirstSeveranceGateResult.Reject(
                "compatibility.calamity_progression_api_unavailable");
        }

        if (!exoMechsDefeated)
        {
            return CalamityFirstSeveranceGateResult.Reject(
                "first_severance.progression_exo_mechs_required",
                exoMechsDefeated,
                supremeCalamitasDefeated,
                bossRushActive);
        }

        if (!supremeCalamitasDefeated)
        {
            return CalamityFirstSeveranceGateResult.Reject(
                "first_severance.progression_supreme_calamitas_required",
                exoMechsDefeated,
                supremeCalamitasDefeated,
                bossRushActive);
        }

        if (bossRushActive)
        {
            return CalamityFirstSeveranceGateResult.Reject(
                "first_severance.world_conflict_boss_rush",
                exoMechsDefeated,
                supremeCalamitasDefeated,
                bossRushActive);
        }

        return CalamityFirstSeveranceGateResult.Allow();
    }

    private static bool TryReadBooleanCall(
        Mod calamity,
        string method,
        string argument,
        out bool value)
    {
        value = false;

        try
        {
            object? result = calamity.Call(method, argument);
            if (result is bool booleanResult)
            {
                value = booleanResult;
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            // A changed or unavailable public call keeps activation fail-closed.
            return false;
        }
    }
}
