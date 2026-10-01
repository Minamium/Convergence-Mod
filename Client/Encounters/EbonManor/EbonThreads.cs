#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.EbonManor;
using Luminance.Common.VerletIntergration;
using Microsoft.Xna.Framework;
using Terraria;

namespace Convergence.Client.Encounters.EbonManor;

// Decorative control silk from Noirette's fingers to each furniture or
// chandelier hook during its warning. Bounded Verlet chains give sag and
// follow-through while she glides; they never supply a hit coordinate.
internal static class EbonThreads
{
    private const int Links = 14, Capacity = 12;
    private sealed class Chain
    {
        internal readonly List<VerletSegment> Segments = new(Links);
        internal Guid Fight;
        internal int Born = -1;
        internal float X, Y;
        internal ulong Touched;
    }
    private static readonly Chain[] chains = new Chain[Capacity];
    private static readonly VerletSettings settings = new(TileCollision: false, SlowInWater: false, Gravity: .22f, MaxFallSpeed: 6);
    private static readonly List<Vector2> path = new(Links + 2);

    internal static void Reset()
    {
        foreach (var chain in chains) if (chain is not null) { chain.Segments.Clear(); chain.Born = -1; chain.Fight = Guid.Empty; }
    }

    // Hook in world space for a plan at this age (top ring of the art).
    internal static Vector2 Hook(in EbonAttackPlan plan, float age)
        => plan.Kind == EbonAttackKind.Chandelier ? EbonScene.ChandelierRing(plan, age) : EbonScene.PropHook(plan, age);

    internal static void Update(EbonBoss boss, in NoirettePose pose, float age)
    {
        ulong now = Main.GameUpdateCount;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not EbonAttack a || a.Plan.Fight != boss.State.Fight || !a.TryBoss(out _)) continue;
            var plan = a.Plan;
            if (plan.Kind is not (EbonAttackKind.Thread or EbonAttackKind.Chandelier) || age < plan.Born || age >= plan.Fire) continue;
            var chain = Find(plan) ?? Claim(plan);
            if (chain is null) continue;
            chain.Touched = now;
            Vector2 hand = EbonNoirette.Hand(pose, plan.Born / 29 % 2), hook = Hook(plan, age);
            if (!float.IsFinite(hand.X + hand.Y + hook.X + hook.Y)) continue;
            float progress = Math.Clamp((age - plan.Born) / Math.Max(1f, plan.Fire - plan.Born), 0, 1);
            if (chain.Segments.Count != Links || Vector2.DistanceSquared(chain.Segments[0].Position, hand) > 400 * 400)
            {
                chain.Segments.Clear();
                for (int j = 0; j < Links; j++)
                {
                    var at = Vector2.Lerp(hand, hook, j / (Links - 1f));
                    chain.Segments.Add(new(at, Vector2.Zero, j == 0 || j == Links - 1));
                }
            }
            chain.Segments[0].Position = chain.Segments[0].OldPosition = hand;
            chain.Segments[^1].Position = chain.Segments[^1].OldPosition = hook;
            // Slack while the load is lifted, drawn taut in the last beat.
            float slack = MathHelper.Lerp(1.10f, .985f, EbonVisualsMath.Ease((progress - .35f) / .65f));
            float rest = Vector2.Distance(hand, hook) / (Links - 1) * slack;
            if (rest < .5f) continue;
            VerletSimulations.VerletSimulation(chain.Segments, rest, settings, 8);
            for (int j = 1; j < Links - 1; j++)
            {
                var s = chain.Segments[j];
                if (!float.IsFinite(s.Position.X + s.Position.Y)) s.Position = Vector2.Lerp(hand, hook, j / (Links - 1f));
            }
        }
        foreach (var chain in chains)
            if (chain is not null && chain.Born >= 0 && now - chain.Touched > 2) { chain.Segments.Clear(); chain.Born = -1; }
    }

    internal static void Draw(EbonBoss boss, in NoirettePose pose, float age)
    {
        foreach (var chain in chains)
        {
            if (chain is null || chain.Born < 0 || chain.Fight != boss.State.Fight || chain.Segments.Count != Links) continue;
            if (!TryPlan(chain, boss, out var plan) || age >= plan.Fire) continue;
            float progress = Math.Clamp((age - plan.Born) / Math.Max(1f, plan.Fire - plan.Born), 0, 1);
            float appear = EbonVisualsMath.Ease((age - plan.Born) / 10);
            path.Clear();
            // Exact fractional endpoints; the simulated interior follows.
            path.Add(EbonNoirette.Hand(pose, plan.Born / 29 % 2));
            for (int j = 1; j < Links - 1; j++) path.Add(chain.Segments[j].Position);
            path.Add(Hook(plan, age));
            EbonMaterials.Thread(path, 2.6f + progress * 1.2f, EbonMaterials.Silk, .72f * appear, progress, 0, plan.Born * .07f, age);
        }
    }

    private static Chain? Find(in EbonAttackPlan plan)
    {
        foreach (var chain in chains)
            if (chain is not null && chain.Born == plan.Born && chain.Fight == plan.Fight && chain.X == plan.X && chain.Y == plan.Y) return chain;
        return null;
    }
    private static Chain? Claim(in EbonAttackPlan plan)
    {
        for (int i = 0; i < chains.Length; i++)
        {
            chains[i] ??= new Chain();
            if (chains[i].Born >= 0) continue;
            var chain = chains[i];
            chain.Fight = plan.Fight; chain.Born = plan.Born; chain.X = plan.X; chain.Y = plan.Y; chain.Segments.Clear();
            return chain;
        }
        return null;
    }
    private static bool TryPlan(Chain chain, EbonBoss boss, out EbonAttackPlan plan)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EbonAttack a && a.Plan.Fight == boss.State.Fight && a.Plan.Born == chain.Born
                && a.Plan.X == chain.X && a.Plan.Y == chain.Y) { plan = a.Plan; return true; }
        plan = default; return false;
    }
}
