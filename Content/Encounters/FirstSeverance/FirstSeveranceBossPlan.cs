#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeveranceBossKeys
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

internal sealed class FirstSeveranceBossPartDefinition
{
    public FirstSeveranceBossPartDefinition(
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

internal sealed class FirstSeveranceWeakPointDefinition
{
    public FirstSeveranceWeakPointDefinition(
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

internal sealed class FirstSeveranceBossFormDefinition
{
    public FirstSeveranceBossFormDefinition(
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
        EnabledPartKeys = FirstSeverancePlanCollections.Copy(enabledPartKeys, nameof(enabledPartKeys));
        AttackPatternKeys = FirstSeverancePlanCollections.Copy(attackPatternKeys, nameof(attackPatternKeys));
    }

    public string Key { get; }

    public bool IsBodyDamageable { get; }

    public bool IsWeakPointExposed { get; }

    public IReadOnlyList<string> EnabledPartKeys { get; }

    public IReadOnlyList<string> AttackPatternKeys { get; }
}

internal sealed class FirstSeveranceBossPlan
{
    private FirstSeveranceBossPlan(
        IReadOnlyList<FirstSeveranceBossPartDefinition> parts,
        FirstSeveranceWeakPointDefinition weakPoint,
        IReadOnlyList<FirstSeveranceBossFormDefinition> forms,
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
            FirstSeveranceBossFormDefinition form = forms[formIndex];
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

        Parts = FirstSeverancePlanCollections.Copy(parts, nameof(parts));
        WeakPoint = weakPoint;
        Forms = FirstSeverancePlanCollections.Copy(forms, nameof(forms));
        LastStandLifeThresholdPermille = lastStandLifeThresholdPermille;
    }

    public static FirstSeveranceBossPlan CreateDefault()
    {
        IReadOnlyList<string> strategicParts = Array.AsReadOnly(
            new[]
            {
                FirstSeveranceBossKeys.CrownPart,
                FirstSeveranceBossKeys.WingsPart,
                FirstSeveranceBossKeys.HeartCasingPart,
            });

        var parts = Array.AsReadOnly(
            new[]
            {
                new FirstSeveranceBossPartDefinition(
                    FirstSeveranceBossKeys.CrownPart,
                    "targeting_precision",
                    850,
                    "extend_target_telegraphs"),
                new FirstSeveranceBossPartDefinition(
                    FirstSeveranceBossKeys.WingsPart,
                    "field_traversal",
                    1000,
                    "reduce_charge_frequency"),
                new FirstSeveranceBossPartDefinition(
                    FirstSeveranceBossKeys.HeartCasingPart,
                    "weak_point_containment",
                    1150,
                    "extend_weak_point_exposure"),
            });

        var forms = Array.AsReadOnly(
            new[]
            {
                new FirstSeveranceBossFormDefinition(
                    FirstSeveranceBossKeys.SealedForm,
                    false,
                    false,
                    Array.Empty<string>(),
                    Array.AsReadOnly(new[] { "sealed_observation", "pylon_crossfire" })),
                new FirstSeveranceBossFormDefinition(
                    FirstSeveranceBossKeys.ManifestForm,
                    true,
                    false,
                    strategicParts,
                    Array.AsReadOnly(new[] { "crown_lattice", "wing_crossing", "choir_sweep" })),
                new FirstSeveranceBossFormDefinition(
                    FirstSeveranceBossKeys.ConvergenceForm,
                    false,
                    false,
                    strategicParts,
                    Array.AsReadOnly(new[] { "effigy_analysis", "identity_pressure" })),
                new FirstSeveranceBossFormDefinition(
                    FirstSeveranceBossKeys.ExposedForm,
                    false,
                    true,
                    strategicParts,
                    Array.AsReadOnly(new[] { "sustained_exposure_beam" })),
                new FirstSeveranceBossFormDefinition(
                    FirstSeveranceBossKeys.LastStandForm,
                    false,
                    false,
                    Array.Empty<string>(),
                    Array.AsReadOnly(new[] { "final_individual_judgment", "final_core_opening" })),
            });

        return new FirstSeveranceBossPlan(
            parts,
            new FirstSeveranceWeakPointDefinition(
                FirstSeveranceBossKeys.CoreWeakPoint,
                2500,
                false),
            forms,
            20);
    }

    public IReadOnlyList<FirstSeveranceBossPartDefinition> Parts { get; }

    public FirstSeveranceWeakPointDefinition WeakPoint { get; }

    public IReadOnlyList<FirstSeveranceBossFormDefinition> Forms { get; }

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
