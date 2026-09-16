#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal sealed record CrimsonChorusSource(CrimsonChorusPlan Plan) : IEntitySource
{ public string Context => "ScarletChorus"; }
internal sealed record CrimsonChorusImpactSource(CrimsonChorusImpact Impact) : IEntitySource
{ public string Context => "ScarletChorusVerdict"; }

// A harmless, immutable marker. Only CrimsonRuntime resolves its deadline.
public sealed class CrimsonChorus : ModProjectile
{
    internal CrimsonChorusPlan Plan;
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 1000;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.penetrate = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void OnSpawn(IEntitySource source)
    { if (source is CrimsonChorusSource own) { own.Plan.Validate(); Plan = own.Plan; } }
    internal static bool TryBoss(in CrimsonChorusPlan plan, out CrimsonBoss? boss)
    {
        boss = plan.Boss >= 0 && plan.Boss < Main.maxNPCs && Main.npc[plan.Boss].active
            ? Main.npc[plan.Boss].ModNPC as CrimsonBoss : null;
        return plan.Fight != Guid.Empty && boss is not null && boss.Fresh
            && boss.State.Fight == plan.Fight && boss.State.PhaseStart == plan.Epoch
            && boss.State.Stage == CrimsonStage.Performance && plan.MatchesRoster(boss.State.Members.Length)
            && CrimsonPhaseRules.ActiveSource(boss.State.Phase, boss.State.DefeatedMask, boss.State.PerformerDefeated, plan.Source);
    }
    public override void AI()
    {
        Projectile.Center = new(Plan.Center.X, Plan.Center.Y);
        if (TryBoss(Plan, out var boss))
        {
            Projectile.timeLeft = Math.Max(2, Plan.End + 20 - (int)boss!.VisualAge);
            if (Main.netMode != NetmodeID.MultiplayerClient && boss.VisualAge >= Plan.End + 20) Projectile.Kill();
        }
        else if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter writer) => Plan.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var next = CrimsonChorusPlan.Read(reader);
        if (Main.netMode != NetmodeID.Server && (Plan.Fight == Guid.Empty || Plan == next)) Plan = next;
    }
}

// Server verdict -> ordinary hostile projectile. Recipient gating also runs on
// the receiving client; do not rely solely on CanHitPlayer's hook locality.
// No manual Hurt, statLife subtraction, immunity clearing or accessory emulation.
public sealed class CrimsonChorusStrike : ModProjectile
{
    internal CrimsonChorusImpact Impact;
    private bool spent;
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 90;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.penetrate = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanHitNPC(NPC target) => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void OnSpawn(IEntitySource source)
    { if (source is CrimsonChorusImpactSource own) { own.Impact.Validate(); Impact = own.Impact; } }
    private bool Eligible(out Player? player)
    {
        player = null;
        if (spent || !CrimsonChorus.TryBoss(Impact.Plan, out var boss)
            || Impact.Member >= boss!.State.Members.Length) return false;
        float age = boss.VisualAge;
        if (age < Impact.Plan.Fire || age >= Impact.Plan.Fire + CrimsonChorusRules.ImpactTicks) return false;
        var member = boss.State.Members[Impact.Member];
        if (member.Out) return false;
        player = Main.player[member.Slot];
        return player.active && !player.dead && !player.ghost
            && (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer == member.Slot);
    }
    public override bool? CanDamage() => Eligible(out _) ? null : false;
    public override bool CanHitPlayer(Player target) => Eligible(out var recipient) && recipient!.whoAmI == target.whoAmI;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Eligible(out _);
    public override void OnHitPlayer(Player target, Player.HurtInfo info) { spent = true; Projectile.hostile = false; }
    public override void AI()
    {
        Projectile.damage = Impact.Damage;
        Projectile.hostile = Eligible(out var player);
        if (player is not null) Projectile.Center = player.Center;
        if (CrimsonChorus.TryBoss(Impact.Plan, out var boss))
        {
            Projectile.timeLeft = Math.Max(2, Impact.Plan.Fire + CrimsonChorusRules.ImpactTicks + 2 - (int)boss!.VisualAge);
            if (Main.netMode != NetmodeID.MultiplayerClient && boss.VisualAge >= Impact.Plan.Fire + CrimsonChorusRules.ImpactTicks) Projectile.Kill();
        }
        else if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter writer) => Impact.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var next = CrimsonChorusImpact.Read(reader);
        if (Main.netMode != NetmodeID.Server && (Impact.Plan.Fight == Guid.Empty || Impact == next)) Impact = next;
    }
}

internal sealed partial class CrimsonRuntime
{
    private CrimsonChorusPlan? chorus;
    private int chorusSlot = -1, chorusOrdinal, phrasesSinceChorus;
    private bool chorusResolved;

    private bool TryScheduleChorus()
    {
        if (actor is null || phase == 0 || performerDefeated || phrasesSinceChorus < CrimsonChorusRules.PhrasesBetween)
            return false;
        var score = CrimsonRegistration.Score;
        int earliest = Math.Max(Math.Max(unlockAt, age + CrimsonRhythm.LookAheadTicks), phraseEnd + 14) - musicStart;
        var beats = CrimsonRhythm.NextBeats(score, earliest, 11);
        byte mask = 0;
        for (int i = 0; i < members.Length; i++) if (!members[i].Out) mask |= (byte)(1 << i);
        var field = State.Field;
        var plan = new CrimsonChorusPlan(fight.Value, (short)actor.NPC.whoAmI, phaseStart, ++chorusOrdinal,
            phase, chorusOrdinal % 2 == 1 ? CrimsonChorusKind.Stack : CrimsonChorusKind.Spread, mask,
            musicStart + (int)Math.Round(beats[0]), musicStart + (int)Math.Round(beats[8]), musicStart + (int)Math.Round(beats[10]),
            (int)ground.X, (int)ground.Y, new(field.CenterX + (chorusOrdinal % 2 == 0 ? -160 : 160), field.CenterY + 80));
        plan.Validate();
        int slot = Projectile.NewProjectile(new CrimsonChorusSource(plan), new(plan.Center.X, plan.Center.Y),
            Vector2.Zero, ModContent.ProjectileType<CrimsonChorus>(), 0, 0, Main.myPlayer);
        if (slot >= Main.maxProjectiles) throw new InvalidOperationException("crimson.chorus_capacity");
        chorus = plan; chorusSlot = slot; chorusResolved = false; phrasesSinceChorus = 0;
        Main.projectile[slot].timeLeft = plan.End + 20 - age; Main.projectile[slot].netUpdate = true;
        nextPhrase = plan.End;
        CrimsonPackets.Log($"event=ChorusCalled fight={fight.Value} epoch={phaseStart} serial={plan.Serial} kind={plan.Kind} born={plan.Born} fire={plan.Fire} members={mask}");
        return true;
    }
    private void TickChorus()
    {
        if (chorus is not { } plan) return;
        if (age >= plan.End) { chorus = null; chorusSlot = -1; return; }
        if (chorusSlot < 0 || chorusSlot >= Main.maxProjectiles || !Main.projectile[chorusSlot].active
            || Main.projectile[chorusSlot].ModProjectile is not CrimsonChorus marker || marker.Plan != plan)
            throw new InvalidOperationException("crimson.chorus_actor_missing");
        if (chorusResolved || age < plan.Fire) return;
        // Never repay a missed deadline by a late burst after a stalled session.
        chorusResolved = true;
        if (age >= plan.Fire + CrimsonChorusRules.ImpactTicks) return;
        var positions = new CrimsonPoint[members.Length]; byte living = 0;
        for (int i = 0; i < members.Length; i++)
        {
            Player p = Main.player[members[i].Slot];
            positions[i] = new(p.Center.X, p.Center.Y);
            if (!members[i].Out && p.active && !p.dead && !p.ghost
                && p.GetModPlayer<CrimsonConnection>().Token == members[i].Connection) living |= (byte)(1 << i);
        }
        int[] damage = CrimsonChorusRules.Resolve(plan.Kind, plan.Center, positions, plan.Members, living);
        int needed = 0, free = 0;
        foreach (int d in damage) if (d > 0) needed++;
        foreach (Projectile p in Main.projectile) if (!p.active) free++;
        if (free < needed) throw new InvalidOperationException("crimson.chorus_capacity");
        for (int i = 0; i < damage.Length; i++)
        {
            if (damage[i] == 0) continue;
            var impact = new CrimsonChorusImpact(plan, (byte)i, damage[i]); impact.Validate();
            int slot = Projectile.NewProjectile(new CrimsonChorusImpactSource(impact), Main.player[members[i].Slot].Center,
                Vector2.Zero, ModContent.ProjectileType<CrimsonChorusStrike>(), damage[i], 0, Main.myPlayer);
            if (slot >= Main.maxProjectiles) throw new InvalidOperationException("crimson.chorus_capacity");
            Main.projectile[slot].netUpdate = true;
        }
        CrimsonPackets.Log($"event=ChorusResolved fight={fight.Value} epoch={phaseStart} serial={plan.Serial} kind={plan.Kind} living={living} native_sources={string.Join(",", damage)}");
    }
    private void ClearChorus(int source)
    {
        if (chorus is { } plan && (source < 0 || source == plan.Source))
        { chorus = null; chorusSlot = -1; chorusResolved = false; }
        if (source < 0) phrasesSinceChorus = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            CrimsonChorusPlan? own = p.ModProjectile is CrimsonChorus marker ? marker.Plan
                : p.ModProjectile is CrimsonChorusStrike strike ? strike.Impact.Plan : null;
            if (own is { } value && value.Fight == fight.Value && (source < 0 || source == value.Source)) p.Kill();
        }
    }
}
