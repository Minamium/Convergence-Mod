using System;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Ghost Samurai target stays locked despite distance changes and retargets only invalid connections")]
    private static void SamuraiTargetLock()
    {
        var a = new SamuraiTargetCandidate(0, Guid.NewGuid(), true, false, false, true, 90000);
        var b = new SamuraiTargetCandidate(1, Guid.NewGuid(), true, false, false, true, 1);
        var c = new SamuraiTargetCandidate(2, Guid.NewGuid(), true, false, false, true, 5);
        var current = new SamuraiTarget(a.Slot, a.Connection);
        AssertEqual(current, SamuraiTargetRules.Select(current, new[] { a, b, c }), "nearer B and C cannot steal living A");
        foreach (var invalid in new[] { a with { Dead = true }, a with { Active = false }, a with { Ghost = true } })
            AssertEqual(new SamuraiTarget(1, b.Connection), SamuraiTargetRules.Select(current, new[] { invalid, b, c }), "death/disconnect selects surviving B");
        AssertEqual(new SamuraiTarget(1, b.Connection),
            SamuraiTargetRules.Select(current, new[] { a with { Connection = Guid.NewGuid() }, b, c }), "reused slot does not inherit old target lock");
        AssertEqual(current, SamuraiTargetRules.Select(current, new[] { a with { DistanceSquared = 100000000 }, b }), "distance alone never invalidates lock");
    }

    [DomainTest("Ghost Samurai solo death and multiplayer wipe terminate without spectators or dead targets keeping the fight alive")]
    private static void SamuraiWipe()
    {
        for (int count = 1; count <= 4; count++)
        {
            var players = new SamuraiTargetCandidate[count + 1];
            for (int i = 0; i < count; i++) players[i] = new(i, Guid.NewGuid(), true, true, false, true, i);
            players[count] = new(8, Guid.NewGuid(), true, false, false, false, 0); // outsider
            var current = new SamuraiTarget(0, players[0].Connection);
            AssertEqual(SamuraiTarget.None, SamuraiTargetRules.Select(current, players), "all combatants dead ends fight now");
            players[count - 1] = players[count - 1] with { Dead = false };
            AssertEqual(count - 1, SamuraiTargetRules.Select(current, players).Slot, "one survivor continues every party size");
        }
        AssertEqual(SamuraiTarget.None, SamuraiTargetRules.Select(SamuraiTarget.None, Array.Empty<SamuraiTargetCandidate>()), "empty server cannot keep fight alive");
    }

    [DomainTest("Ghost Samurai non-target owner damage is halved only in multiplayer with a known owner")]
    private static void SamuraiNonTargetDamage()
    {
        for (int owner = 0; owner < 4; owner++)
        {
            float multiplier = SamuraiTargetRules.DamageMultiplier(true, 0, owner, true);
            AssertEqual(owner == 0 ? 1f : .5f, multiplier, "item/projectile/minion owner follows same policy");
            AssertEqual(owner == 0 ? 200f : 100f, multiplier * 200, "post-defense/crit result scaled");
            AssertEqual(1f, SamuraiTargetRules.DamageMultiplier(false, 0, owner, true), "solo remains full damage");
        }
        foreach (int owner in new[] { -1, 255, 999 })
            AssertEqual(1f, SamuraiTargetRules.DamageMultiplier(true, 0, owner, true), "unknown/server projectile owner not reduced");
        AssertEqual(1f, SamuraiTargetRules.DamageMultiplier(true, -1, 2, true), "no valid lock means no accidental reduction");
        AssertEqual(1f, SamuraiTargetRules.DamageMultiplier(true, 0, 2, false), "unresolved owner kept untouched");
        AssertEqual(1f, SamuraiTargetRules.DamageMultiplier(true, 2, 2, true), "new target immediately gets full damage");
        AssertEqual(.5f, SamuraiTargetRules.DamageMultiplier(true, 2, 0, true), "former target becomes non-target");
    }

    [DomainTest("Ghost Samurai dash speed and visual cue separate the early shout from the late fixed trajectory")]
    private static void SamuraiDashTimeline()
    {
        float oldSpeed = 1800f / 18;
        float newSpeed = GhostSamuraiRules.DashAttackSpeed;
        float reduction = 1 - newSpeed / oldSpeed;
        AssertEqual(true, reduction is >= .1f and <= .2f, "ten to twenty percent reduction");
        int fire = GhostSamuraiRules.DashApproach + GhostSamuraiRules.DashWarning;
        AssertEqual(102, fire, "first dash release unchanged");
        AssertEqual(24, fire - GhostSamuraiRules.DashShoutDelay, "old shout84 now24");
        AssertEqual(90, fire - GhostSamuraiRules.DashVisualCueTime, "closing ring starts twelve ticks before dash");
        AssertEqual(98, fire - GhostSamuraiRules.DashAimLockTime, "strong tracking continues to four ticks before dash");
        AssertEqual(60, GhostSamuraiRules.ChargedSlashDamage, "low test damage");
    }
}
