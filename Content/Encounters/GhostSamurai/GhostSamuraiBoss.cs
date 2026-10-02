#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

public sealed class GhostSamuraiBoss : ModNPC
{
    internal GhostSamuraiRuntime? Runtime;
    internal Guid Fight;
    internal SamuraiArenaBounds Arena;
    internal int Age, AttackTimer, TransitionRemaining;
    internal int LockedTarget = -1;
    internal SamuraiPhase Phase = SamuraiPhase.Phase1;
    internal SamuraiAttack Attack;
    internal SamuraiBeat Beat;
    internal SamuraiComboSnapshot Combo;
    private ulong receivedAt;
    private readonly SamuraiClientMotion movement = new();
    internal Vector2 PresentationCenter => Main.netMode == NetmodeID.MultiplayerClient && movement.Age >= 0
        ? PredictedCenter() : NPC.Center;
    internal bool ProjectionFresh => Main.netMode != NetmodeID.MultiplayerClient || Main.GameUpdateCount - receivedAt <= 45;
    internal float VisualAge => Main.netMode == NetmodeID.MultiplayerClient
        ? Age + (float)Math.Min(30UL, Main.GameUpdateCount - receivedAt) : Age;
    internal float VisualAttackTimer => AttackTimer + (VisualAge - Age);

    public override string Texture => "Terraria/Images/NPC_" + NPCID.DungeonGuardian;
    public override void SetStaticDefaults()
    {
        NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
    }
    public override void SetDefaults()
    {
        NPC.width = GhostSamuraiRules.BodyWidth; NPC.height = GhostSamuraiRules.BodyHeight;
        NPC.lifeMax = GhostSamuraiRules.Life;
        NPC.defense = GhostSamuraiRules.Defense;
        NPC.damage = 0; // Runtime owns swept body contact; native client hits stay off.
        NPC.knockBackResist = 0;
        NPC.noGravity = NPC.noTileCollide = NPC.lavaImmune = NPC.boss = NPC.netAlways = true;
        NPC.aiStyle = -1;
        NPC.value = 0;
        // The fight's own track comes from GhostSamuraiMusicScene for players inside the seal;
        // 0 keeps vanilla boss music from starting for anyone else near the actor.
        if (!Main.dedServ) { NPC.HitSound = SoundID.NPCHit2; NPC.DeathSound = SoundID.NPCDeath2; Music = 0; }
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        => NPC.lifeMax = (int)(GhostSamuraiRules.Life * (1 + .55f * Math.Max(0, numPlayers - 1)));
    public override bool CheckActive() => false;
    // Native player strikes are resolved on the attacking client (or projectile
    // owner). Consume only the server's target snapshot; never select a target here.
    private float IncomingMultiplier(int owner) => SamuraiTargetRules.DamageMultiplier(
        Main.netMode != NetmodeID.SinglePlayer, LockedTarget, owner,
        owner >= 0 && owner < Main.maxPlayers && Main.player[owner].active);
    public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        => modifiers.FinalDamage *= IncomingMultiplier(player.whoAmI);
    public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        => modifiers.FinalDamage *= IncomingMultiplier(projectile.owner);
    public override void OnSpawn(IEntitySource source)
    {
        if (source is GhostSamuraiActorSource owned)
        {
            Runtime = owned.Runtime; Fight = owned.Fight; NPC.target = LockedTarget = owned.Target; Arena = owned.Arena;
        }
    }
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;
        if (Main.netMode == NetmodeID.MultiplayerClient && movement.Age >= 0)
        {
            NPC.Center = PredictedCenter();
            NPC.velocity = Vector2.Zero; // Do not integrate the received velocity a second time.
        }
        // Local strikes are resolved here; the summoning and a change of form cannot be hit.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NPC.dontTakeDamage = GhostSamuraiRules.Untouchable(Age, TransitionRemaining);
        if (Main.netMode != NetmodeID.MultiplayerClient && (Runtime is null || !Runtime.Matches(this)))
        {
            // Unregistered/debug-spawned actors cannot run an unmanaged second fight.
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
        }
    }
    private Vector2 PredictedCenter()
    {
        // Damaging rushes retain their exact immutable authority trajectory;
        // smoothing is reserved for harmless hover/correction, never collision.
        if (TransitionRemaining == 0)
        {
            if (Attack == SamuraiAttack.FrontalCleaveShockwave && Combo.IsValid(Attack))
                return Vector2.Lerp(new(Combo.FromX, Combo.FromY), new(Combo.AnchorX, Combo.AnchorY),
                    SamuraiComboRules.ApproachProgress(VisualAttackTimer, Combo));
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.ModProjectile is GhostSamuraiAttackProjectile rush && rush.Fight == Fight && rush.BossSlot == NPC.whoAmI
                    && rush.Hazard.Shape == SamuraiShape.RushVisual && rush.SlashAim.Locked && rush.Hazard.Live(VisualAge))
                    return GhostSamuraiAttackProjectile.RushCenter(rush.DisplayHazard, VisualAge);
        }
        var at = movement.At(Main.GameUpdateCount);
        return new(at.X, at.Y);
    }
    public override bool CheckDead()
    {
        if (Phase == SamuraiPhase.Phase3) return true;
        // Even a lethal burst must pass the two visible phase boundaries first.
        NPC.life = 1;
        NPC.dontTakeDamage = true;
        NPC.netUpdate = Main.netMode != NetmodeID.MultiplayerClient;
        return false;
    }
    public override void OnKill() => Runtime?.RecordDeath(this);
    public override void ModifyNPCLoot(NPCLoot npcLoot)
        => npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Content.Items.Oboro.Oboro>()));
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        var state = new SamuraiActorSnapshot(Fight, Age, Phase, Attack, Beat, AttackTimer, TransitionRemaining, NPC.lifeMax, Arena, LockedTarget, Combo);
        // Native sync can run before an owned OnSpawn, or for a debug-created NPC.
        // An absent envelope is harmless; never serialize an invalid zero arena.
        SamuraiNativeFrame.Write(writer, state, new(NPC.Center.X, NPC.Center.Y, NPC.velocity.X, NPC.velocity.Y));
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        if (!SamuraiNativeFrame.TryRead(reader, out var state, out var motion)) return;
        if (Main.netMode == NetmodeID.Server || Fight != Guid.Empty && Arena != state.Arena) return;
        if (Fight != Guid.Empty && Age == state.Age && (LockedTarget != state.LockedTarget || Combo != state.Combo)) return;
        if (!state.CanReplace(Fight, Age) || Fight != Guid.Empty && state.Age <= Age) return;
        movement.Accept(state.Age, motion, Main.GameUpdateCount);
        Fight = state.Fight; Age = state.Age; Phase = state.Phase; Attack = state.Attack; Beat = state.Beat;
        AttackTimer = state.AttackTimer; TransitionRemaining = state.TransitionRemaining; NPC.lifeMax = state.MaximumLife;
        Arena = state.Arena; LockedTarget = state.LockedTarget; Combo = state.Combo;
        if (SamuraiComboRules.IsCombo(Attack)) NPC.direction = NPC.spriteDirection = Combo.Facing;
        receivedAt = Main.GameUpdateCount;
    }
}
