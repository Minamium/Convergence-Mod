#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

public sealed class GhostSamuraiBoss : ModNPC
{
    internal GhostSamuraiRuntime? Runtime;
    internal Guid Fight;
    internal SamuraiArenaBounds Arena;
    internal int Age, AttackTimer, TransitionRemaining;
    internal SamuraiPhase Phase = SamuraiPhase.Phase1;
    internal SamuraiAttack Attack;
    internal SamuraiBeat Beat;
    private ulong receivedAt;
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
        if (!Main.dedServ) { NPC.HitSound = SoundID.NPCHit2; NPC.DeathSound = SoundID.NPCDeath2; Music = MusicID.Boss3; }
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        => NPC.lifeMax = (int)(GhostSamuraiRules.Life * (1 + .55f * Math.Max(0, numPlayers - 1)));
    public override bool CheckActive() => false;
    public override void OnSpawn(IEntitySource source)
    {
        if (source is GhostSamuraiActorSource owned)
        {
            Runtime = owned.Runtime; Fight = owned.Fight; NPC.target = owned.Target; Arena = owned.Arena;
        }
    }
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;
        if (Main.netMode == NetmodeID.MultiplayerClient && TransitionRemaining == 0)
        {
            // Evaluate the same locked trajectory, instead of extrapolating a
            // single high velocity past its end. This path never decides hits.
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.ModProjectile is GhostSamuraiAttackProjectile rush && rush.Fight == Fight && rush.BossSlot == NPC.whoAmI
                    && rush.Hazard.Shape == SamuraiShape.RushVisual && rush.SlashAim.Locked && rush.Hazard.Live(VisualAge))
                {
                    NPC.Center = GhostSamuraiAttackProjectile.RushCenter(rush.DisplayHazard, VisualAge);
                    NPC.velocity = Vector2.Zero;
                    break;
                }
        }
        if (Main.netMode != NetmodeID.MultiplayerClient && (Runtime is null || !Runtime.Matches(this)))
        {
            // Unregistered/debug-spawned actors cannot run an unmanaged second fight.
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
        }
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
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        new SamuraiActorSnapshot(Fight, Age, Phase, Attack, Beat, AttackTimer, TransitionRemaining, NPC.lifeMax, Arena).Write(writer);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        SamuraiActorSnapshot state = SamuraiActorSnapshot.Read(reader);
        if (Main.netMode == NetmodeID.Server || Fight != Guid.Empty && Arena != state.Arena) return;
        if (!state.CanReplace(Fight, Age)) return;
        Fight = state.Fight; Age = state.Age; Phase = state.Phase; Attack = state.Attack; Beat = state.Beat;
        AttackTimer = state.AttackTimer; TransitionRemaining = state.TransitionRemaining; NPC.lifeMax = state.MaximumLife;
        Arena = state.Arena;
        receivedAt = Main.GameUpdateCount;
    }
}
