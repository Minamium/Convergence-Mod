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

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal sealed record CrimsonActorSource(CrimsonRuntime Runtime, CrimsonState State) : IEntitySource
{ public string Context => "CrimsonFoundry"; }
internal sealed record CrimsonAttackSource(CrimsonHazard Hazard) : IEntitySource
{ public string Context => "CrimsonFoundryScore"; }

// One session owns roster, score cursor, actor and every attack. Nothing extends
// Doll's containment, recovery service or native-Hurt receipt exception.
internal sealed class CrimsonRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly int summoner;
    private CrimsonBoss? actor;
    private CrimsonMember[] members = Array.Empty<CrimsonMember>();
    private int age, musicStart = -1, purge = -1, ending = -1;
    private CrimsonStage stage;
    private bool cleaned, killed, cancelled;
    internal CrimsonRuntime(FightId fight, int summoner) { this.fight = fight; this.summoner = summoner; }
    private CrimsonState State => new(fight.Value, age, musicStart, purge, stage, members);
    internal bool Matches(CrimsonBoss value) => !cleaned && ReferenceEquals(value, actor) && value.State.Fight == fight.Value;
    internal void Killed(CrimsonBoss value) { if (Matches(value)) killed = true; }
    internal bool Request(int sender, Guid connection, bool ready, bool cancel)
    {
        if (cleaned || stage != CrimsonStage.Ready || sender < 0 || sender >= Main.maxPlayers) return false;
        if (!Main.player[sender].active || Main.player[sender].dead || Main.player[sender].GetModPlayer<CrimsonConnection>().Token != connection) return false;
        int index = Array.FindIndex(members, m => m.Slot == sender && m.Connection == connection);
        if (index < 0) return false;
        if (cancel) { if (sender != summoner) return false; cancelled = true; }
        else members[index] = members[index] with { Ready = ready };
        Project(true);
        return true;
    }
    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (cleaned || Main.netMode == NetmodeID.MultiplayerClient) return EncounterRuntimeUpdate.None;
        if (context.Lifecycle == EncounterLifecycle.Validating)
        {
            if (!RefreshRoster()) return End(EncounterEndReason.Invalidated);
            Player p = Main.player[summoner];
            int slot = NPC.NewNPC(new CrimsonActorSource(this, State), (int)p.Center.X, (int)p.Center.Y - 230, ModContent.NPCType<CrimsonBoss>());
            if (slot >= Main.maxNPCs) return End(EncounterEndReason.EncounterActorMissing);
            actor = (CrimsonBoss)Main.npc[slot].ModNPC;
            actor.NPC.life = actor.NPC.lifeMax = 12000000 + 8000000 * (members.Length - 1);
            Project(true);
            CrimsonPackets.Log($"event=Preparing fight={fight.Value} members={members.Length} life={actor.NPC.lifeMax}");
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Preparing);
        }
        if (actor is null || !actor.NPC.active || actor.NPC.ModNPC != actor) return End(EncounterEndReason.EncounterActorMissing);
        age++;
        if (cancelled || age > 60 * 60 * 15) return End(EncounterEndReason.Cancelled);
        if (context.Lifecycle == EncounterLifecycle.Preparing)
        {
            if (!RefreshRoster()) return End(EncounterEndReason.Invalidated);
            if (age >= 150) stage = CrimsonStage.Ready;
            if (stage == CrimsonStage.Ready && Array.TrueForAll(members, m => m.Ready && !Main.player[m.Slot].dead))
            {
                // Two seconds ahead: every client can preload/arm the same
                // music epoch; the 8s score introduction starts after this lead.
                musicStart = age + 120; stage = CrimsonStage.Countdown;
                Project(true);
                CrimsonPackets.Log($"event=AllReady fight={fight.Value} music_start={musicStart}");
                return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Active);
            }
            if (age > 60 * 180) return End(EncounterEndReason.Cancelled);
        }
        else if (context.Lifecycle == EncounterLifecycle.Active)
        {
            for (int i = 0; i < members.Length; i++)
            {
                Player p = Main.player[members[i].Slot];
                if (!p.active || p.dead || p.ghost || p.GetModPlayer<CrimsonConnection>().Token != members[i].Connection)
                    members[i] = members[i] with { Out = true };
            }
            if (ending < 0 && (killed || Array.TrueForAll(members, m => m.Out)))
            {
                ending = age; stage = killed ? CrimsonStage.Victory : CrimsonStage.Defeat;
                actor.NPC.dontTakeDamage = true; ClearHazards();
                CrimsonPackets.Log($"event={stage} fight={fight.Value} age={age}");
                Project(true);
            }
            if (ending >= 0)
            {
                if (age >= ending + 150) return End(killed ? EncounterEndReason.Victory : EncounterEndReason.Defeat);
                Project(age % 15 == 0);
                return EncounterRuntimeUpdate.None;
            }
            var score = CrimsonRegistration.Score;
            int songAge = age - musicStart;
            if (songAge >= score.IntroTicks) stage = CrimsonStage.Performance;
            actor.NPC.dontTakeDamage = stage != CrimsonStage.Performance || purge >= 0 && age < purge + 90;
            if (stage == CrimsonStage.Performance && purge < 0 && actor.NPC.life <= actor.NPC.lifeMax / 2)
            {
                // Pick the next measured accent within 0.8s; no soundtrack restart.
                purge = age + 30;
                double pos = score.Position(songAge);
                for (int i = 0; i < score.BeatTicks.Length; i++)
                    if (score.BeatTicks[i] > pos && score.BeatTicks[i] - pos < 48 && score.Energy[i] >= .55f)
                    { purge = age + (int)Math.Round(score.BeatTicks[i] - pos); break; }
                actor.NPC.life = actor.NPC.lifeMax / 2;
                actor.NPC.dontTakeDamage = true; ClearHazards();
                CrimsonPackets.Log($"event=ArmorPurge fight={fight.Value} accent={purge} music_age={songAge}");
                Project(true);
            }
            if (stage == CrimsonStage.Performance && (purge < 0 || age >= purge + 90))
                score.Events(songAge, Schedule);
        }
        Move();
        Project(age % 10 == 0);
        return EncounterRuntimeUpdate.None;
    }
    private bool RefreshRoster()
    {
        var next = new List<CrimsonMember>();
        foreach (Player p in Main.ActivePlayers)
        {
            if (p.ghost) continue;
            var token = p.GetModPlayer<CrimsonConnection>().Token;
            var previous = Array.Find(members, m => m.Slot == p.whoAmI && m.Connection == token);
            next.Add(new((byte)p.whoAmI, token, previous.Ready && !p.dead, false));
        }
        if (next.Count is 0 or > CrimsonState.MaxMembers) return false;
        bool changed = next.Count != members.Length;
        for (int i = 0; i < next.Count && !changed; i++) changed |= next[i].Slot != members[i].Slot || next[i].Connection != members[i].Connection;
        if (changed) for (int i = 0; i < next.Count; i++) next[i] = next[i] with { Ready = false };
        members = next.ToArray();
        if (changed) Project(true);
        return true;
    }
    private void Move()
    {
        if (actor is null) return;
        var m = Array.Find(members, x => !x.Out && Main.player[x.Slot].active && !Main.player[x.Slot].dead);
        Player p = Main.player[m.Slot];
        bool fast = purge >= 0 && age >= purge + 90;
        float orbit = age * (fast ? .033f : .006f);
        Vector2 goal = p.Center + new Vector2(MathF.Sin(orbit) * (fast ? 440 : 170), -260 + MathF.Cos(orbit * 1.3f) * 70);
        Vector2 delta = goal - actor.NPC.Center;
        Vector2 desired = delta.SafeNormalize(Vector2.Zero) * Math.Min(delta.Length() * .08f, fast ? 40 : 8);
        actor.NPC.velocity = Vector2.Lerp(actor.NPC.velocity, desired, fast ? .17f : .07f);
    }
    private void Schedule(int id, int fire, float intensity, bool offbeat)
    {
        if (actor is null) return;
        int eventIndex = (id % 4096) / 2;
        bool fast = purge >= 0 && age >= purge + 90;
        // First form articulates; unarmored form uses more offbeat energy bolts.
        if (!fast && offbeat && eventIndex % 4 != 1) return;
        foreach (var m in members)
        {
            if (m.Out) continue;
            Player p = Main.player[m.Slot];
            if (!p.active || p.dead) continue;
            Vector2 anchor = p.Center; // Frozen at warning start, never aims again.
            float angle = ((eventIndex * 5 + m.Slot * 3) % 8) * MathHelper.PiOver4 + (offbeat ? .22f : 0);
            int lanes = intensity > .72f ? (fast ? 3 : 2) : 1;
            Vector2 direction = angle.ToRotationVector2();
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            for (int lane = 0; lane < lanes; lane++)
            {
                // Parallel geometry cannot trap the player at a crossed intersection.
                // Always threaten the frozen anchor. Symmetric TWO lanes would
                // leave the targeted player's current position permanently safe.
                float offset = lane == 0 ? 0 : (lane % 2 == 1 ? 155 : -155);
                Vector2 origin = anchor - direction * 520 + normal * offset;
                var shape = offbeat || eventIndex % 4 == 3 ? CrimsonShape.Bolt : CrimsonShape.Slash;
                var hazard = new CrimsonHazard(fight.Value, (short)actor.NPC.whoAmI, age,
                    musicStart + fire, musicStart + fire + (shape == CrimsonShape.Bolt ? 25 : 18), shape,
                    origin.X, origin.Y, direction.X, direction.Y, 1040, shape == CrimsonShape.Bolt ? 16 : 22, fast ? 510 : 450);
                int slot = Projectile.NewProjectile(new CrimsonAttackSource(hazard), origin, Vector2.Zero,
                    ModContent.ProjectileType<CrimsonAttack>(), hazard.Damage, 0, Main.myPlayer);
                if (slot >= Main.maxProjectiles) throw new InvalidOperationException("crimson.projectile_capacity");
                Main.projectile[slot].netUpdate = true;
            }
        }
        if (eventIndex % 8 == 0) CrimsonPackets.Log($"event=ScoreAccent fight={fight.Value} cue={id} fire={musicStart + fire} energy={intensity:F2} form={(fast ? 2 : 1)}");
    }
    private void Project(bool sync)
    {
        if (actor is null) return;
        actor.State = State;
        if (sync) actor.NPC.netUpdate = true;
    }
    private EncounterRuntimeUpdate End(EncounterEndReason reason) => EncounterRuntimeUpdate.End(CrimsonTermination.End(reason));
    private void ClearHazards()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is CrimsonAttack a && a.Hazard.Fight == fight.Value) p.Kill();
    }
    public void Cleanup(in EncounterCleanupContext context)
    {
        if (cleaned || context.FightId != fight) return;
        ClearHazards();
        if (actor is not null && actor.NPC.active && actor.NPC.ModNPC == actor && actor.State.Fight == fight.Value)
        {
            actor.NPC.active = false; actor.Runtime = null;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: actor.NPC.whoAmI);
        }
        cleaned = true;
        CrimsonPackets.Log($"event=Cleaned fight={fight.Value} reason={context.EndReason} age={age}");
    }
}
