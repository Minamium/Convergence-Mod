#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal static class ThirdSeveranceBossKeys
{
    public const string CrownPart = "crown";
    public const string WingsPart = "wings";
    public const string HeartCasingPart = "heart_casing";
    public const string CoreWeakPoint = "collective_core";

    public const string SealedForm = "sealed";
    public const string ManifestForm = "manifest";
    public const string ConvergenceForm = "convergence";
    public const string ExposedForm = "exposed";
    public const string LastStandForm = "last_stand";
}

internal sealed class ThirdSeveranceBossPartDefinition
{
    public ThirdSeveranceBossPartDefinition(
        string key,
        string responsibilityKey,
        int relativeDurabilityPermille,
        string destroyedEffectKey)
    {
        if (string.IsNullOrWhiteSpace(key)
            || string.IsNullOrWhiteSpace(responsibilityKey)
            || string.IsNullOrWhiteSpace(destroyedEffectKey))
        {
            throw new ArgumentException("Boss part keys must be non-empty.");
        }

        if (relativeDurabilityPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeDurabilityPermille));
        }

        Key = key;
        ResponsibilityKey = responsibilityKey;
        RelativeDurabilityPermille = relativeDurabilityPermille;
        DestroyedEffectKey = destroyedEffectKey;
    }

    public string Key { get; }

    // Stable keys let future authoritative executors map behavior without putting
    // Terraria or Calamity types into the immutable encounter plan.
    public string ResponsibilityKey { get; }

    public int RelativeDurabilityPermille { get; }

    public string DestroyedEffectKey { get; }
}

internal sealed class ThirdSeveranceWeakPointDefinition
{
    public ThirdSeveranceWeakPointDefinition(
        string key,
        int exposedDamageTakenPermille,
        bool isTargetableOutsideExposure)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A weak point requires a stable key.", nameof(key));
        }

        if (exposedDamageTakenPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exposedDamageTakenPermille));
        }

        Key = key;
        ExposedDamageTakenPermille = exposedDamageTakenPermille;
        IsTargetableOutsideExposure = isTargetableOutsideExposure;
    }

    public string Key { get; }

    public int ExposedDamageTakenPermille { get; }

    public bool IsTargetableOutsideExposure { get; }
}

internal sealed class ThirdSeveranceBossFormDefinition
{
    public ThirdSeveranceBossFormDefinition(
        string key,
        bool isBodyDamageable,
        bool isWeakPointExposed,
        IReadOnlyList<string> enabledPartKeys,
        IReadOnlyList<string> attackPatternKeys)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A boss form requires a stable key.", nameof(key));
        }

        Key = key;
        IsBodyDamageable = isBodyDamageable;
        IsWeakPointExposed = isWeakPointExposed;
        EnabledPartKeys = ThirdSeverancePlanCollections.Copy(enabledPartKeys, nameof(enabledPartKeys));
        AttackPatternKeys = ThirdSeverancePlanCollections.Copy(attackPatternKeys, nameof(attackPatternKeys));
    }

    public string Key { get; }

    public bool IsBodyDamageable { get; }

    public bool IsWeakPointExposed { get; }

    public IReadOnlyList<string> EnabledPartKeys { get; }

    public IReadOnlyList<string> AttackPatternKeys { get; }
}

internal sealed class ThirdSeveranceBossPlan
{
    private ThirdSeveranceBossPlan(
        IReadOnlyList<ThirdSeveranceBossPartDefinition> parts,
        ThirdSeveranceWeakPointDefinition weakPoint,
        IReadOnlyList<ThirdSeveranceBossFormDefinition> forms,
        int lastStandLifeThresholdPermille)
    {
        if (lastStandLifeThresholdPermille <= 0 || lastStandLifeThresholdPermille >= 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(lastStandLifeThresholdPermille));
        }

        ValidateUniqueKeys(parts, static part => part.Key, nameof(parts));
        ValidateUniqueKeys(forms, static form => form.Key, nameof(forms));

        var knownPartKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int partIndex = 0; partIndex < parts.Count; partIndex++)
        {
            knownPartKeys.Add(parts[partIndex].Key);
        }

        for (int formIndex = 0; formIndex < forms.Count; formIndex++)
        {
            ThirdSeveranceBossFormDefinition form = forms[formIndex];
            for (int partIndex = 0; partIndex < form.EnabledPartKeys.Count; partIndex++)
            {
                if (!knownPartKeys.Contains(form.EnabledPartKeys[partIndex]))
                {
                    throw new ArgumentException(
                        $"Form '{form.Key}' references unknown part '{form.EnabledPartKeys[partIndex]}'.",
                        nameof(forms));
                }
            }
        }

        Parts = ThirdSeverancePlanCollections.Copy(parts, nameof(parts));
        WeakPoint = weakPoint;
        Forms = ThirdSeverancePlanCollections.Copy(forms, nameof(forms));
        LastStandLifeThresholdPermille = lastStandLifeThresholdPermille;
    }

    public static ThirdSeveranceBossPlan CreateDefault()
    {
        IReadOnlyList<string> strategicParts = Array.AsReadOnly(
            new[]
            {
                ThirdSeveranceBossKeys.CrownPart,
                ThirdSeveranceBossKeys.WingsPart,
                ThirdSeveranceBossKeys.HeartCasingPart,
            });

        var parts = Array.AsReadOnly(
            new[]
            {
                new ThirdSeveranceBossPartDefinition(
                    ThirdSeveranceBossKeys.CrownPart,
                    "targeting_precision",
                    850,
                    "extend_target_telegraphs"),
                new ThirdSeveranceBossPartDefinition(
                    ThirdSeveranceBossKeys.WingsPart,
                    "field_traversal",
                    1000,
                    "reduce_charge_frequency"),
                new ThirdSeveranceBossPartDefinition(
                    ThirdSeveranceBossKeys.HeartCasingPart,
                    "weak_point_containment",
                    1150,
                    "extend_weak_point_exposure"),
            });

        var forms = Array.AsReadOnly(
            new[]
            {
                new ThirdSeveranceBossFormDefinition(
                    ThirdSeveranceBossKeys.SealedForm,
                    false,
                    false,
                    Array.Empty<string>(),
                    Array.AsReadOnly(new[] { "sealed_observation", "pylon_crossfire" })),
                new ThirdSeveranceBossFormDefinition(
                    ThirdSeveranceBossKeys.ManifestForm,
                    true,
                    false,
                    strategicParts,
                    Array.AsReadOnly(new[] { "crown_lattice", "wing_crossing", "choir_sweep" })),
                new ThirdSeveranceBossFormDefinition(
                    ThirdSeveranceBossKeys.ConvergenceForm,
                    false,
                    false,
                    strategicParts,
                    Array.AsReadOnly(new[] { "effigy_analysis", "identity_pressure" })),
                new ThirdSeveranceBossFormDefinition(
                    ThirdSeveranceBossKeys.ExposedForm,
                    false,
                    true,
                    strategicParts,
                    Array.AsReadOnly(new[] { "sustained_exposure_beam" })),
                new ThirdSeveranceBossFormDefinition(
                    ThirdSeveranceBossKeys.LastStandForm,
                    false,
                    false,
                    Array.Empty<string>(),
                    Array.AsReadOnly(new[] { "final_individual_judgment", "final_core_opening" })),
            });

        return new ThirdSeveranceBossPlan(
            parts,
            new ThirdSeveranceWeakPointDefinition(
                ThirdSeveranceBossKeys.CoreWeakPoint,
                2500,
                false),
            forms,
            20);
    }

    public IReadOnlyList<ThirdSeveranceBossPartDefinition> Parts { get; }

    public ThirdSeveranceWeakPointDefinition WeakPoint { get; }

    public IReadOnlyList<ThirdSeveranceBossFormDefinition> Forms { get; }

    // 20 permille is 2%; tuning remains a server-owned balance decision.
    public int LastStandLifeThresholdPermille { get; }

    private static void ValidateUniqueKeys<T>(
        IReadOnlyList<T> values,
        Func<T, string> keySelector,
        string parameterName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("At least one definition is required.", parameterName);
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < values.Count; index++)
        {
            string key = keySelector(values[index]);
            if (!keys.Add(key))
            {
                throw new ArgumentException($"Duplicate definition key '{key}'.", parameterName);
            }
        }
    }
}
