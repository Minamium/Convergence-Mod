#nullable enable

using System;

namespace Convergence.Common.Compatibility.Calamity;

internal readonly record struct CalamityFirstSeveranceGateResult
{
    private CalamityFirstSeveranceGateResult(
        bool isAllowed,
        bool exoMechsDefeated,
        bool supremeCalamitasDefeated,
        bool bossRushActive,
        string failureCode)
    {
        if (isAllowed == !string.IsNullOrEmpty(failureCode))
        {
            throw new ArgumentException(
                "Allowed progression has no failure code; rejected progression requires one.",
                nameof(failureCode));
        }

        IsAllowed = isAllowed;
        ExoMechsDefeated = exoMechsDefeated;
        SupremeCalamitasDefeated = supremeCalamitasDefeated;
        BossRushActive = bossRushActive;
        FailureCode = failureCode;
    }

    public bool IsAllowed { get; }

    public bool ExoMechsDefeated { get; }

    public bool SupremeCalamitasDefeated { get; }

    public bool BossRushActive { get; }

    public string FailureCode { get; }

    public static CalamityFirstSeveranceGateResult Allow()
    {
        return new CalamityFirstSeveranceGateResult(
            true,
            exoMechsDefeated: true,
            supremeCalamitasDefeated: true,
            bossRushActive: false,
            string.Empty);
    }

    public static CalamityFirstSeveranceGateResult Reject(
        string failureCode,
        bool exoMechsDefeated = false,
        bool supremeCalamitasDefeated = false,
        bool bossRushActive = false)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A rejected progression gate requires a code.", nameof(failureCode));
        }

        return new CalamityFirstSeveranceGateResult(
            false,
            exoMechsDefeated,
            supremeCalamitasDefeated,
            bossRushActive,
            failureCode);
    }
}
