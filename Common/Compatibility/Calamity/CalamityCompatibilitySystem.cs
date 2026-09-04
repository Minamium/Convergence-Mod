#nullable enable

using System;
using Convergence.Common.Encounters.Runtime;
using Terraria.ModLoader;

namespace Convergence.Common.Compatibility.Calamity;

internal sealed class CalamityCompatibilitySystem : ModSystem
{
    private const string CalamityInternalName = "CalamityMod";
    private static readonly Version MinimumSupportedVersion = new(2, 2, 4);
    private static readonly Version MaximumExclusiveVersion = new(2, 3, 0);

    internal static Version? DetectedVersion { get; private set; }

    internal static bool IsSupported { get; private set; }

    public override void PostSetupContent()
    {
        EncounterActivationPolicyCatalogSystem.Register(
            CalamityCompatibilityActivationPolicy.Instance);

        IsSupported = false;
        DetectedVersion = null;

        if (!ModLoader.TryGetMod(CalamityInternalName, out Mod calamity))
        {
            Mod.Logger.Error("Calamity Mod is required but was not loaded.");
            return;
        }

        DetectedVersion = calamity.Version;
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
        DetectedVersion = null;
        IsSupported = false;
    }
}
