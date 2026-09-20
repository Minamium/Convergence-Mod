#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

public sealed class CrimsonBoss : ModNPC
{
    internal CrimsonRuntime? Runtime;
    internal CrimsonState State;
    private ulong receivedAt;
    internal float VisualAge => State.Age + (Main.netMode == NetmodeID.MultiplayerClient ? (float)Math.Min(30UL, Main.GameUpdateCount - receivedAt) : 0);
    internal bool Fresh => Main.netMode != NetmodeID.MultiplayerClient || Main.GameUpdateCount - receivedAt <= 60;
    public override string Texture => "Convergence/Assets/Textures/CrimsonFoundry/ScarletConjurer";
    public override void SetStaticDefaults() => NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
    public override void SetDefaults()
    {
        NPC.width = 28; NPC.height = 56; NPC.lifeMax = CrimsonPlaytestTuning.SoloTargetLife; NPC.defense = 65;
        NPC.damage = 0; NPC.knockBackResist = 0; NPC.aiStyle = -1;
        NPC.noGravity = NPC.noTileCollide = NPC.lavaImmune = NPC.netAlways = true;
        NPC.dontTakeDamage = true; NPC.boss = true;
        NPC.BossBar = ModContent.GetInstance<CrimsonBossBar>();
        if (!Main.dedServ) { NPC.HitSound = SoundID.NPCHit4; Music = 0; }
    }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool? CanBeHitByItem(Player player, Item item) => State.Contains(player.whoAmI) && NPC.life > State.DamageFloor(3) ? null : false;
    public override bool? CanBeHitByProjectile(Projectile projectile) => State.Contains(projectile.owner) && NPC.life > State.DamageFloor(3) ? null : false;
    public override void OnSpawn(IEntitySource source)
    {
        if (source is CrimsonActorSource owned) { Runtime = owned.Runtime; State = owned.State; }
    }
    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;
        // Project vulnerability on EVERY peer, otherwise clients never submit hits.
        NPC.dontTakeDamage = !Fresh || !State.Vulnerable(VisualAge) || NPC.life <= State.DamageFloor(3);
        NPC.boss = State.Stage is CrimsonStage.Countdown or CrimsonStage.Performance;
        if (State.TargetLife > 0) NPC.lifeMax = State.TargetLife;
        CrimsonGesture.ProjectMotion(NPC, this, 3);
        if (Main.netMode != NetmodeID.MultiplayerClient && (Runtime is null || !Runtime.Matches(this)))
        {
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
        }
    }
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (State.DamageFloor(3) > 0) modifiers.SetMaxDamage(Math.Max(1, NPC.life - State.DamageFloor(3)));
    }
    public override bool CheckDead()
    {
        NPC.life = 1;
        if (State.Vulnerable(VisualAge) && State.DamageFloor(3) == 0) Runtime?.Killed(this);
        NPC.dontTakeDamage = true;
        NPC.netUpdate = Main.netMode != NetmodeID.MultiplayerClient;
        return false;
    }
    public override void SendExtraAI(BinaryWriter writer) => State.WriteEnvelope(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var payload = CrimsonState.ReadEnvelope(reader);
        if (payload is not { } next) return;
        if (Main.netMode == NetmodeID.Server || !next.CanReplace(State)) return;
        State = next; receivedAt = Main.GameUpdateCount;
    }
}

// Retained type/codec for stable content identity; new phrases spawn gestures.
public sealed class CrimsonAttack : ModProjectile
{
    internal CrimsonHazard Hazard;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathLaser;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 160; Projectile.netImportant = true;
    }
    public override void OnSpawn(IEntitySource source)
    { if (source is CrimsonAttackSource owned) Hazard = owned.Hazard; }
    public override bool ShouldUpdatePosition() => false;
    internal bool TryBoss(out CrimsonBoss? boss)
    {
        boss = Hazard.Boss >= 0 && Hazard.Boss < Main.maxNPCs && Main.npc[Hazard.Boss].active
            ? Main.npc[Hazard.Boss].ModNPC as CrimsonBoss : null;
        return boss is not null && Hazard.Fight != Guid.Empty && boss.State.Fight == Hazard.Fight
            && boss.Fresh && boss.State.Stage is CrimsonStage.Countdown or CrimsonStage.Performance
            && Hazard.Epoch == boss.State.PhaseStart
            && CrimsonPhaseRules.ActiveSource(boss.State.Phase, boss.State.DefeatedMask, boss.State.PerformerDefeated, Hazard.Source);
    }
    internal float Age(CrimsonBoss boss) => boss.VisualAge + (Main.netMode == NetmodeID.MultiplayerClient ? 0 : 1);
    public override bool? CanDamage() => TryBoss(out var boss) && boss!.State.SourceActive(Hazard.Source, Age(boss))
        && Hazard.Live(Age(boss)) ? null : false;
    public override bool CanHitPlayer(Player target) => TryBoss(out var boss) && boss!.State.Contains(target.whoAmI);
    public override bool? CanHitNPC(NPC target) => false;
    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        => modifiers.SetMaxDamage(CrimsonPlaytestTuning.AttackDamage);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!TryBoss(out var boss) || !boss!.State.SourceActive(Hazard.Source, Age(boss)) || !Hazard.Live(Age(boss))) return false;
        float age = Age(boss!); Vector2 direction = new(Hazard.DX, Hazard.DY), origin = new(Hazard.X, Hazard.Y);
        if (Hazard.Shape == CrimsonShape.Bolt) origin += direction * Math.Max(0, Hazard.Travel(age) - CrimsonHazard.BoltTail);
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin,
            origin + direction * Hazard.Reach(age), Hazard.HitWidth(age) * 2, ref point);
    }
    public override void AI()
    {
        Projectile.hostile = TryBoss(out var boss) && boss!.State.SourceActive(Hazard.Source, Age(boss)) && Hazard.Live(Age(boss));
        Projectile.damage = CrimsonPlaytestTuning.AttackDamage;
        if (boss is not null)
        {
            float age = Age(boss);
            if (boss.Fresh && boss.State.Fight == Hazard.Fight)
                Projectile.timeLeft = Math.Max(2, Hazard.End + 14 - (int)age);
            Projectile.Center = new Vector2(Hazard.X, Hazard.Y) + new Vector2(Hazard.DX, Hazard.DY)
                * (Hazard.Shape == CrimsonShape.Bolt ? Hazard.Travel(age) : Hazard.Length * .5f);
            if (Main.netMode != NetmodeID.MultiplayerClient && age >= Hazard.End + 14) Projectile.Kill();
        }
        else if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter writer) => Hazard.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var next = CrimsonHazard.Read(reader);
        if (Main.netMode != NetmodeID.Server && (Hazard.Fight == Guid.Empty || Hazard == next)) Hazard = next;
    }
}

internal sealed class CrimsonConnection : ModPlayer
{
    internal Guid Token;
    internal uint LastNonce;
    internal ulong NextRequest;
    private void ResetConnection() { Token = Guid.NewGuid(); LastNonce = 0; NextRequest = 0; }
    public override void Initialize() => ResetConnection();
    public override void OnEnterWorld() => ResetConnection();
    public override void PlayerDisconnect() => ResetConnection();
}
