#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Compatibility.Calamity;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Replication;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Calamity progression result preserves evidence")]
    private static void CalamityProgressionResultPreservesEvidence()
    {
        CalamityFirstSeveranceGateResult allowed = CalamityFirstSeveranceGateResult.Allow();
        AssertEqual(true, allowed.IsAllowed, "allowed progression");
        AssertEqual(true, allowed.ExoMechsDefeated, "allowed Exo Mechs evidence");
        AssertEqual(true, allowed.SupremeCalamitasDefeated, "allowed SCal evidence");
        AssertEqual(false, allowed.BossRushActive, "allowed Boss Rush evidence");
        AssertEqual(string.Empty, allowed.FailureCode, "allowed failure code");

        CalamityFirstSeveranceGateResult rejected =
            CalamityFirstSeveranceGateResult.Reject(
                "first_severance.progression_boss_rush_active",
                exoMechsDefeated: true,
                supremeCalamitasDefeated: true,
                bossRushActive: true);
        AssertEqual(false, rejected.IsAllowed, "rejected progression");
        AssertEqual(true, rejected.ExoMechsDefeated, "rejected Exo Mechs evidence");
        AssertEqual(true, rejected.SupremeCalamitasDefeated, "rejected SCal evidence");
        AssertEqual(true, rejected.BossRushActive, "rejected Boss Rush evidence");
        AssertEqual(
            "first_severance.progression_boss_rush_active",
            rejected.FailureCode,
            "rejected failure code");
        AssertThrows<ArgumentException>(
            () => _ = CalamityFirstSeveranceGateResult.Reject(string.Empty),
            "empty progression rejection code");
    }
}
