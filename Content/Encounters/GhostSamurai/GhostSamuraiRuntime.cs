#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

// One coordinator-owned fight. The NPC is a damageable actor and read-only network
// projection; it does not own a second AI or a client-side random attack selector.
internal sealed class GhostSamuraiRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly int summoner;
    private readonly List<GhostSamuraiAttackProjectile> hazards = new();
    private readonly int[] nextHit = new int[256];
    private GhostSamuraiBoss? actor;
    private bool cleaned, killed;
    private int age, timer, transition, absent;
    private SamuraiPhase phase = SamuraiPhase.Phase1;
    private SamuraiAttack attack, previous;
    private SamuraiBeat beat;
    private float baseAngle;
    private Vector2 dashStart, dashEnd;
    private int dashSide;

    internal GhostSamuraiRuntime(FightId fight, int summoner) { this.fight = fight; this.summoner = summoner; }
    internal bool Matches(GhostSamuraiBoss npc) => !cleaned && ReferenceEquals(actor, npc) && npc.Fight == fight.Value;
    internal void RecordDeath(GhostSamuraiBoss npc) { if (Matches(npc)) killed = true; }

    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || cleaned) return EncounterRuntimeUpdate.None;
        if (context.Lifecycle == EncounterLifecycle.Validating)
        {
            Player p = Main.player[summoner];
            if (!p.active || p.dead) return End(EncounterEndReason.Defeat);
            int slot = NPC.NewNPC(new GhostSamuraiActorSource(this, fight.Value, summoner), (int)p.Center.X, (int)p.Center.Y - 280,
                ModContent.NPCType<GhostSamuraiBoss>());
            if (slot < 0 || slot >= Main.maxNPCs) return End(EncounterEndReason.EncounterActorMissing);
            actor = (GhostSamuraiBoss)Main.npc[slot].ModNPC;
            actor.NPC.netUpdate = true;
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Preparing);
        }
        if (killed) return End(EncounterEndReason.Victory);
        if (actor is null || !actor.NPC.active || actor.NPC.ModNPC != actor) return End(EncounterEndReason.EncounterActorMissing);
        if (context.Lifecycle == EncounterLifecycle.Preparing)
        {
            GhostSamuraiPackets.Log($"event=CombatStarted seq={context.EncounterSequence} fight={fight.Value} boss_slot={actor.NPC.whoAmI} target_slot={actor.NPC.target} max_life={actor.NPC.lifeMax}");
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Active);
        }
        if (context.Lifecycle != EncounterLifecycle.Active) return EncounterRuntimeUpdate.None;

        NPC npc = actor.NPC;
        age++;
        // Native projectile slots can be reused before this authority tick. An
        // active replacement must not keep an old ModProjectile in our budget.
        hazards.RemoveAll(p => !p.Projectile.active || p.Projectile.ModProjectile != p
            || p.Fight != fight.Value || age >= p.Hazard.End);
        if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead
            || Vector2.DistanceSquared(npc.Center, Main.player[npc.target].Center) > GhostSamuraiRules.AbandonDistance * GhostSamuraiRules.AbandonDistance)
            npc.TargetClosest(false);
        Player target = Main.player[Math.Clamp(npc.target, 0, Main.maxPlayers - 1)];
        if (!target.active || target.dead || Vector2.Distance(npc.Center, target.Center) > GhostSamuraiRules.AbandonDistance)
        {
            if (absent++ == 0) ClearHazards();
            npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0, -6), .04f);
            npc.dontTakeDamage = true;
            Project(npc);
            return absent >= GhostSamuraiRules.AbandonTime ? End(EncounterEndReason.Defeat) : EncounterRuntimeUpdate.None;
        }
        if (absent > 0) { absent = 0; FinishAttack(); }
        SamuraiPhase next = GhostSamuraiRules.NextPhase(phase, npc.life, npc.lifeMax);
        if (transition == 0 && next != phase)
        {
            phase = next;
            transition = GhostSamuraiRules.TransitionTime;
            attack = SamuraiAttack.Idle;
            timer = 0;
            ClearHazards();
            npc.netUpdate = true;
            GhostSamuraiPackets.Log($"event=PhaseChanged fight={fight.Value} age={age} phase={phase} life={npc.life} max_life={npc.lifeMax}");
        }
        npc.dontTakeDamage = transition > 0;
        if (transition > 0)
        {
            beat = SamuraiBeat.Transition;
            npc.velocity *= .88f;
            if (--transition == 0) FinishAttack();
        }
        else
        {
            UpdateAttack(npc, target);
            foreach (var hazard in hazards) hazard.AdvanceWisp(age, target.Center);
            ApplyDamage(npc);
        }
        Project(npc);
        return EncounterRuntimeUpdate.None;
    }

    private void Project(NPC npc)
    {
        if (actor is null) return;
        actor.Age = age; actor.Phase = phase; actor.Attack = attack; actor.Beat = beat;
        actor.AttackTimer = timer; actor.TransitionRemaining = transition;
        // Snapshots also re-anchor client-only clocks during long attacks / late join.
        if (age % 15 == 0) npc.netUpdate = true;
    }

    private void UpdateAttack(NPC npc, Player target)
    {
        if (attack == SamuraiAttack.Idle)
        {
            beat = SamuraiBeat.Recovery;
            Hover(npc, target.Center + new Vector2(npc.Center.X < target.Center.X ? -300 : 300, -200));
            if (++timer < GhostSamuraiRules.AttackInterval(phase)) return;
            int count = phase == SamuraiPhase.Phase1 ? 3 : 4;
            attack = GhostSamuraiRules.SelectNextAttack(phase, previous, Main.rand.Next(count - (previous == SamuraiAttack.Idle ? 0 : 1)));
            timer = 0;
            baseAngle = Main.rand.NextFloat(-.35f, .35f);
            dashSide = npc.Center.X < target.Center.X ? -1 : 1;
            npc.netUpdate = true;
        }
        switch (attack)
        {
            case SamuraiAttack.DirectionalSlash: DoDirectionalSlash(npc, target); break;
            case SamuraiAttack.ChargedSlash: DoChargedSlash(npc, target); break;
            case SamuraiAttack.GridSlash: DoGridSlash(npc, target); break;
            case SamuraiAttack.Phase2DashSlash: DoPhase2DashSlash(npc, target); break;
        }
        if (attack != SamuraiAttack.Idle) timer++;
    }

    private void DoDirectionalSlash(NPC npc, Player target)
    {
        if (timer >= GhostSamuraiRules.DirectionalDuration) { FinishAttack(); return; }
        int step = Math.Min(timer / GhostSamuraiRules.DirectionalSlashInterval, GhostSamuraiRules.DirectionalSlashCount - 1);
        Hover(npc, target.Center + new Vector2(step % 2 == 0 ? -260 : 260, -180));
        beat = GhostSamuraiRules.DirectionalBeat(timer);
        step = GhostSamuraiRules.DirectionalSpawnStep(timer);
        if (step < 0) return;
        float angle = baseAngle + step * MathHelper.PiOver4 + Main.rand.NextFloat(-.12f, .12f);
        Vector2 direction = angle.ToRotationVector2();
        Vector2 center = target.Center;
        AddSlash(center - direction * GhostSamuraiRules.SlashLength / 2, direction,
            GhostSamuraiRules.SlashLength, GhostSamuraiRules.SlashHalfWidth, GhostSamuraiRules.SlashWarning, GhostSamuraiRules.SlashLive);
        if (phase != SamuraiPhase.Phase1) AddWisps(npc.Center, age + GhostSamuraiRules.SlashWarning + GhostSamuraiRules.WispDelay, 3);
    }

    private void DoChargedSlash(NPC npc, Player target)
    {
        if (timer < GhostSamuraiRules.ChargeAimTime)
        {
            beat = SamuraiBeat.Approach;
            Hover(npc, target.Center + new Vector2(dashSide * 360, -100));
            return;
        }
        npc.velocity *= .86f;
        int local = timer - GhostSamuraiRules.ChargeAimTime;
        beat = local < GhostSamuraiRules.ChargeWarning ? SamuraiBeat.Telegraph
            : local < GhostSamuraiRules.ChargeWarning + GhostSamuraiRules.ChargeLive ? SamuraiBeat.Strike : SamuraiBeat.Recovery;
        if (local == 0)
        {
            Vector2 direction = (target.Center - npc.Center).SafeNormalize(Vector2.UnitX);
            AddSlash(target.Center - direction * 900, direction, 1800, GhostSamuraiRules.ChargeHalfWidth,
                GhostSamuraiRules.ChargeWarning, GhostSamuraiRules.ChargeLive);
            if (phase != SamuraiPhase.Phase1) AddWisps(npc.Center, age + GhostSamuraiRules.ChargeWarning + GhostSamuraiRules.WispDelay, 5);
        }
        if (local >= GhostSamuraiRules.ChargeWarning + GhostSamuraiRules.ChargeLive + GhostSamuraiRules.RecoveryTime) FinishAttack();
    }

    private void DoGridSlash(NPC npc, Player target)
    {
        npc.velocity *= .9f;
        if (timer < GhostSamuraiRules.GridPrelude) { beat = SamuraiBeat.Approach; return; }
        int local = timer - GhostSamuraiRules.GridPrelude;
        beat = local < GhostSamuraiRules.GridWarning ? SamuraiBeat.Telegraph
            : local < GhostSamuraiRules.GridWarning + GhostSamuraiRules.GridLive ? SamuraiBeat.Strike : SamuraiBeat.Recovery;
        if (local == 0)
        {
            float halfWidth = GhostSamuraiRules.GridWidth / 2, halfHeight = GhostSamuraiRules.GridHeight / 2;
            // Keep full cells available near world borders. This never changes tiles.
            Vector2 center = new(Math.Clamp(target.Center.X, halfWidth + 160, Main.maxTilesX * 16 - halfWidth - 160),
                Math.Clamp(target.Center.Y, halfHeight + 160, Main.maxTilesY * 16 - halfHeight - 160));
            for (int i = 0; i < GhostSamuraiRules.GridVerticalLineCount; i++)
                Spawn(GhostSamuraiRules.GridLine(true, i, center.X, center.Y, age));
            for (int i = 0; i < GhostSamuraiRules.GridHorizontalLineCount; i++)
                Spawn(GhostSamuraiRules.GridLine(false, i, center.X, center.Y, age));
            // Expanding the grid must not move its follow-up wisps out of reach.
            if (phase != SamuraiPhase.Phase1) AddWisps(npc.Center,
                age + GhostSamuraiRules.GridWarning + GhostSamuraiRules.WispDelay, 5);
        }
        if (local >= GhostSamuraiRules.GridWarning + GhostSamuraiRules.GridLive + GhostSamuraiRules.RecoveryTime) FinishAttack();
    }

    private void DoPhase2DashSlash(NPC npc, Player target)
    {
        int pass = timer / GhostSamuraiRules.DashCadence, local = timer % GhostSamuraiRules.DashCadence;
        if (pass >= 3) { FinishAttack(); return; }
        int side = pass % 2 == 0 ? dashSide : -dashSide;
        if (local < GhostSamuraiRules.DashApproach)
        {
            beat = SamuraiBeat.Approach;
            Hover(npc, target.Center + new Vector2(side * GhostSamuraiRules.DashDistance / 2, 0), 32);
            return;
        }
        int windup = local - GhostSamuraiRules.DashApproach;
        if (windup == 0)
        {
            npc.velocity = Vector2.Zero;
            dashStart = npc.Center;
            dashEnd = dashStart + new Vector2(-side * GhostSamuraiRules.DashDistance, 0);
            AddSlash(dashStart, new Vector2(-side, 0), GhostSamuraiRules.DashDistance,
                GhostSamuraiRules.DashHalfWidth, GhostSamuraiRules.DashWarning, GhostSamuraiRules.DashLive);
        }
        if (windup < GhostSamuraiRules.DashWarning)
        {
            beat = SamuraiBeat.Telegraph;
            npc.velocity = Vector2.Zero;
        }
        else if (windup < GhostSamuraiRules.DashWarning + GhostSamuraiRules.DashLive)
        {
            beat = SamuraiBeat.Strike;
            float progress = GhostSamuraiRules.DashProgress(windup - GhostSamuraiRules.DashWarning + 1);
            npc.velocity = Vector2.Lerp(dashStart, dashEnd, progress) - npc.Center;
        }
        else { beat = SamuraiBeat.Recovery; npc.velocity *= .7f; }
    }

    private static void Hover(NPC npc, Vector2 destination, float speed = 18)
    {
        Vector2 delta = destination - npc.Center;
        Vector2 desired = delta.SafeNormalize(Vector2.Zero) * Math.Min(speed, delta.Length() * .08f);
        npc.velocity = Vector2.Lerp(npc.velocity, desired, .16f);
    }

    private void AddSlash(Vector2 start, Vector2 direction, float length, float halfWidth, int warning, int live)
        => Spawn(new(SamuraiShape.Slash, start.X, start.Y, direction.X, direction.Y, length, halfWidth,
            age, age + warning, age + warning + live, GhostSamuraiRules.Damage(attack)));

    private void AddWisps(Vector2 origin, int born, int count)
    {
        int existing = 0;
        foreach (var p in hazards)
            if (p.Projectile.active && p.Hazard.Shape == SamuraiShape.Wisp) existing++;
        count = Math.Min(count, GhostSamuraiRules.MaximumWisps - existing);
        float rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        for (int i = 0; i < count; i++)
        {
            Vector2 d = (rotation + i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.18f, .18f)).ToRotationVector2();
            Spawn(new(SamuraiShape.Wisp, origin.X, origin.Y, d.X, d.Y, 0, GhostSamuraiRules.WispRadius,
                born, born + GhostSamuraiRules.SpreadDuration, born + GhostSamuraiRules.SpreadDuration + GhostSamuraiRules.WispLife,
                GhostSamuraiRules.WispDamage));
        }
    }

    private void Spawn(SamuraiHazard hazard)
    {
        if (actor is null || !hazard.IsValid || hazards.Count >= GhostSamuraiRules.MaximumHazards)
            throw new InvalidOperationException("ghost_samurai.hazard_capacity_or_shape");
        int slot = Projectile.NewProjectile(new GhostSamuraiAttackSource(fight.Value, actor.NPC.whoAmI, hazard), new Vector2(hazard.X, hazard.Y), Vector2.Zero,
            ModContent.ProjectileType<GhostSamuraiAttackProjectile>(), 0, 0, Main.myPlayer);
        if (slot < 0 || slot >= Main.maxProjectiles) throw new InvalidOperationException("ghost_samurai.projectile_capacity");
        var p = (GhostSamuraiAttackProjectile)Main.projectile[slot].ModProjectile;
        hazards.Add(p); // Register before any further mutation/synchronization.
        p.Projectile.timeLeft = hazard.End - age + 30;
        p.Projectile.netUpdate = true;
    }

    private void ApplyDamage(NPC npc)
    {
        foreach (Player p in Main.ActivePlayers)
        {
            if (p.dead || p.ghost || p.immune || p.creativeGodMode || age < nextHit[p.whoAmI]) continue;
            foreach (var hazard in hazards)
            {
                if (!hazard.Projectile.active || !hazard.Hits(age, p)) continue;
                // Resolve native defense/endurance/hooks once on the authority. Clients
                // receive the resulting HurtInfo; projectile collision is disabled there.
                double dealt = p.Hurt(PlayerDeathReason.ByNPC(npc.whoAmI), hazard.Hazard.Damage,
                    p.Center.X >= npc.Center.X ? 1 : -1, out Player.HurtInfo info, quiet: true);
                nextHit[p.whoAmI] = age + GhostSamuraiRules.HitCooldown;
                if (dealt > 0 && Main.netMode == NetmodeID.Server) NetMessage.SendPlayerHurt(p.whoAmI, info);
                break;
            }
        }
    }

    private void FinishAttack()
    {
        if (attack != SamuraiAttack.Idle) previous = attack;
        attack = SamuraiAttack.Idle; timer = 0; beat = SamuraiBeat.Recovery;
        if (actor is not null) actor.NPC.netUpdate = true;
    }
    private static EncounterRuntimeUpdate End(EncounterEndReason reason) => EncounterRuntimeUpdate.End(GhostSamuraiTermination.End(reason));

    private void ClearHazards()
    {
        // Exact Fight scan catches partially-created resources without touching peers.
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is GhostSamuraiAttackProjectile owned && owned.Fight == fight.Value) p.Kill();
        hazards.Clear();
    }
    public void Cleanup(in EncounterCleanupContext context)
    {
        if (cleaned || context.FightId != fight) return;
        ClearHazards();
        if (actor is not null && actor.NPC.active && actor.NPC.ModNPC == actor && actor.Fight == fight.Value)
        {
            actor.NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: actor.NPC.whoAmI);
        }
        if (actor is not null) actor.Runtime = null;
        cleaned = true;
        GhostSamuraiPackets.Log($"event=CombatEnded seq={context.EncounterSequence} fight={fight.Value} age={age} reason={context.EndReason} phase={phase} life={actor?.NPC.life ?? 0} max_life={actor?.NPC.lifeMax ?? 0} cleanup=complete");
    }
}
