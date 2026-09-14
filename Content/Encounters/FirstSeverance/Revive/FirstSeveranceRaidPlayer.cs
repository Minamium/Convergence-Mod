using System;
using System.Collections.Generic;
using System.Linq;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Revive;

public sealed class FirstSeveranceRaidPlayer : ModPlayer
{
    private static readonly HashSet<FirstSeveranceRaidPlayer> bound = new();
    private FightId fightId;
    private uint healthRevision;
    private FirstSeveranceHitReceipt hitReceipt;
    private FirstSeveranceDownLatch downLatch;
    private uint resultNonce;
    private int hurtLifeBefore;
    private bool applyingIntent;
    private int boundBuffType, lockoutBuffType;
    private Vector2 downedPosition;
    private ulong immunityUntilLocalTick;
    private ulong weaknessUntilLocalTick;
    private ulong reviveLockoutUntilLocalTick;
    private ulong debugAssistUntilLocalTick;

    internal bool IsDebugAssistProtected => !fightId.IsNone && !IsRaidDowned && !IsRaidEliminated
        && Main.GameUpdateCount < debugAssistUntilLocalTick;

    internal bool IsRaidDowned { get; private set; }

    internal bool IsRaidEliminated { get; private set; }

    internal bool IsReviving { get; private set; }
    internal bool IsIncapacitated => IsRaidDowned || IsRaidEliminated || downLatch.Pending;
    private bool OwnsHurt => Main.netMode == NetmodeID.SinglePlayer
        || Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;
    private bool IsBound => !fightId.IsNone && Player.active && !Player.dead && !Player.ghost;

    internal void ApplyProjection(
        FightId owner,
        in FirstSeveranceCombatParticipantProjection participant,
        ulong authorityTick)
    {
        if (!participant.IsConnected)
        {
            ClearRaidState();
            return;
        }

        if (fightId != owner)
        {
            ClearRaidState();
            fightId = owner;
        }
        bound.Add(this);
        boundBuffType = ModContent.BuffType<RaidBoundDebuff>();
        lockoutBuffType = ModContent.BuffType<RecoveryLockoutDebuff>();

        bool wasDowned = IsRaidDowned;
        IsRaidDowned = participant.CombatState == RaidParticipantCombatState.Downed;
        IsRaidEliminated = participant.CombatState == RaidParticipantCombatState.Eliminated;
        downLatch.Observe(participant.HealthRevision, IsRaidDowned || IsRaidEliminated);
        IsReviving = participant.IsReviving;
        debugAssistUntilLocalTick = participant.DebugAssistProtected ? Main.GameUpdateCount + 300 : 0;
        if (IsRaidDowned || IsRaidEliminated)
            downedPosition = new Vector2(participant.AnchorX, participant.AnchorY);
        immunityUntilLocalTick = Main.GameUpdateCount
            + Remaining(participant.InvulnerabilityUntilTick, authorityTick, 180);
        weaknessUntilLocalTick = Main.GameUpdateCount
            + Remaining(participant.WeaknessUntilTick, authorityTick, 600);
        reviveLockoutUntilLocalTick = Main.GameUpdateCount
            + Remaining(participant.ReviveLockoutUntilTick, authorityTick, 3_600);
        RefreshRecoveryLockoutBuff();

        if (participant.HealthRevision > healthRevision)
        {
            int previousLife = Player.statLife;
            Player.statLife = Math.Clamp(participant.Life, 1, Player.statLifeMax2);
            healthRevision = participant.HealthRevision;
            if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
            {
                if (Player.statLife > previousLife)
                    Player.HealEffect(Player.statLife - previousLife, broadcast: false);
                else if (Player.statLife < previousLife)
                    CombatText.NewText(Player.Hitbox, Color.OrangeRed, previousLife - Player.statLife);
            }
        }

        if (IsRaidDowned && !wasDowned)
        {
            Player.mount.Dismount(Player);
            // Clear the existing grapple once; held-input suppression prevents new ones.
            for (int index = 0; index < Main.maxProjectiles; index++)
            {
                Projectile projectile = Main.projectile[index];
                if (projectile.active && projectile.owner == Player.whoAmI
                    && projectile.aiStyle == ProjAIStyleID.Hook)
                    projectile.Kill();
            }
        }
    }

    internal void ApplyRaidHit(in EncounterPacketHeader header, FightId owner, ulong sequence, FirstSeveranceHurtIntent intent)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer
            || !IsBound || fightId != owner || intent.HealthRevision != healthRevision || !intent.IsValid
            || !hitReceipt.TryAccept(header, owner, sequence, intent.Damage)) return;
        var result = ApplyNativeHurt(header.Revision, intent);
        if (!Player.dead) FirstSeverancePacketSystem.SendHurtResult(sequence, owner, result);
    }

    internal FirstSeveranceHurtResult ApplyNativeHurt(uint hitRevision, FirstSeveranceHurtIntent intent)
    {
        int before = Math.Max(1, Player.statLife);
        double applied = 0;
        applyingIntent = true;
        try
        {
            if (OwnsHurt && IsBound && !IsIncapacitated && !IsDebugAssistProtected)
                applied = Player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                    "Mods.Convergence.UI.FirstSeverance.NativeHitDeathReason", Player.name)),
                    intent.Damage, 0, dodgeable: intent.Dodgeable, scalingArmorPenetration: intent.ArmorPenetration);
        }
        finally { applyingIntent = false; }
        if (Player.statLife == 1 && !Player.dead && !IsDebugAssistProtected) LatchDown();
        return new(++resultNonce, hitRevision, healthRevision, before, Math.Max(1, Player.statLife),
            (int)Math.Clamp(applied, 0, FirstSeveranceHurtIntent.MaximumDamage));
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (OwnsHurt && IsBound && !IsIncapacitated && !IsDebugAssistProtected && Player.statLife > 1)
            modifiers.SetMaxDamage(Player.statLife - 1);
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (OwnsHurt && IsBound) hurtLifeBefore = Player.statLife;
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        if (!OwnsHurt || !IsBound || IsIncapacitated || IsDebugAssistProtected) return;
        Mod.Logger.Info($"FirstSeverance event=NativePlayerHurt fight={fightId} slot={Player.whoAmI} damage={info.Damage} source_damage={info.SourceDamage} life_before={hurtLifeBefore} life_after={Player.statLife} health_revision={healthRevision} raid_intent={applyingIntent}");
        if (Player.statLife == 1)
        {
            LatchDown();
            if (!applyingIntent) ReportFloor(hurtLifeBefore, info.Damage);
        }
    }

    private void LatchDown()
    {
        if (IsRaidDowned || IsRaidEliminated || downLatch.Pending) return;
        downLatch.Request(healthRevision);
        downedPosition = Player.position;
    }

    private void ReportFloor(int before, int damage)
    {
        var result = new FirstSeveranceHurtResult(++resultNonce, 0, healthRevision, Math.Max(1, before), 1,
            Math.Clamp(damage, 0, FirstSeveranceHurtIntent.MaximumDamage));
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat is { } combat && combat.FightId == fightId)
                FirstSeverancePacketSystem.SendHurtResult(combat.EncounterSequence, fightId, result);
        }
        else if (Main.netMode == NetmodeID.SinglePlayer
            && FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(Player.whoAmI, out ulong epoch))
            FirstSeveranceCombatAuthority.TryAcceptHurtResult(ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot.EncounterSequence,
                fightId, Player.whoAmI, epoch, result, out _);
    }

    internal void ClearRaidState()
    {
        bound.Remove(this);
        // Enumerate actual initialized instances; teardown cannot assume that
        // ModContent or every Main.player slot still has a registered ModPlayer.
        foreach (ModPlayer instance in Player.ModPlayers)
            if (instance is FirstSeveranceContainmentPlayer containment) containment.Clear(fightId);
        if (IsIncapacitated && Player.active && !Player.dead)
        {
            Player.statLife = Math.Max(Player.statLife,
                Math.Max(1, (int)Math.Ceiling(Player.statLifeMax2 * 0.35f)));
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 180);
        }

        IsRaidDowned = false;
        IsRaidEliminated = false;
        IsReviving = false;
        fightId = FightId.None;
        healthRevision = 0;
        hitReceipt = default;
        downLatch = default;
        resultNonce = 0;
        applyingIntent = false;
        immunityUntilLocalTick = 0;
        weaknessUntilLocalTick = 0;
        reviveLockoutUntilLocalTick = 0;
        debugAssistUntilLocalTick = 0;
        if (lockoutBuffType > 0) Player.ClearBuff(lockoutBuffType);
        if (boundBuffType > 0) Player.ClearBuff(boundBuffType);
        boundBuffType = lockoutBuffType = 0;
    }

    internal static void ClearAll()
    {
        foreach (var instance in bound.ToArray()) instance.ClearRaidState();
    }
    internal static void ClearFight(FightId owner)
    {
        foreach (var instance in bound.ToArray())
            if (instance.fightId == owner) instance.ClearRaidState();
    }
    public override void PlayerDisconnect() => ClearRaidState();
    public override void OnEnterWorld() => ClearRaidState();

    public override void SetControls()
    {
        if (IsIncapacitated)
        {
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.controlHook = false;
            Player.controlMount = false;
        }
        else if (IsReviving)
        {
            Player.controlHook = false;
            Player.controlMount = false;
        }
    }

    public override void PreUpdateMovement()
    {
        if (!IsIncapacitated)
        {
            return;
        }

        Player.velocity = Vector2.Zero;
        Player.position = downedPosition;
        Player.fallStart = (int)(Player.position.Y / 16f);
        if (Player.mount.Active)
        {
            Player.mount.Dismount(Player);
        }
    }

    public override bool CanUseItem(Item item)
    {
        return !IsIncapacitated
            && (!IsReviving || item.type == ModContent.ItemType<ResuscitationKitItem>());
    }

    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        if (OwnsHurt && IsBound && Player.statLife == 1 && !IsIncapacitated && !IsDebugAssistProtected)
        {
            LatchDown();
            if (!applyingIntent) ReportFloor(1, 0);
        }
        return IsDebugAssistProtected || IsIncapacitated || Main.GameUpdateCount < immunityUntilLocalTick;
    }

    public override void UpdateBadLifeRegen()
    {
        if (IsDebugAssistProtected || IsIncapacitated)
        {
            Player.lifeRegen = 0;
            Player.lifeRegenCount = 0;
            Player.statLife = Math.Max(1, Player.statLife);
        }
    }

    public override void PostUpdateEquips()
    {
        if (Main.GameUpdateCount < weaknessUntilLocalTick)
            Player.GetDamage(DamageClass.Generic) *= 0.8f;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp,
        ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        // Not a production lethal-hit adapter. Only an authority-issued, short-lived
        // development projection may suppress death. Terminal cleanup clears it first.
        if (!IsDebugAssistProtected)
            return true;
        Player.statLife = Math.Max(1, Player.statLife);
        return false;
    }

    public override bool? CanHitNPCWithItem(Item item, NPC target)
    {
        return IsIncapacitated || IsReviving
            ? false
            : null;
    }

    public override bool? CanHitNPCWithProj(Projectile projectile, NPC target)
    {
        return IsIncapacitated || IsReviving
            ? false
            : null;
    }

    public override void PostUpdate()
    {
        RefreshRecoveryLockoutBuff();
        if (IsBound && boundBuffType > 0)
        {
            Player.buffImmune[boundBuffType] = false;
            Player.AddBuff(boundBuffType, 2, quiet: true);
        }
        if (OwnsHurt && IsBound && Player.statLife == 1 && !IsIncapacitated && !IsDebugAssistProtected)
        {
            LatchDown();
            ReportFloor(1, 0);
        }
        if (IsIncapacitated)
        {
            Player.statLife = 1;
            Player.lifeRegen = Player.lifeRegenCount = 0;
            Player.position = downedPosition;
            Player.velocity = Vector2.Zero;
        }

        if (Main.dedServ || (!IsRaidDowned && !IsRaidEliminated))
        {
            return;
        }

        if (Main.rand.Next(4) == 0)
        {
            int dust = Dust.NewDust(
                Player.position,
                Player.width,
                Player.height,
                DustID.GemRuby,
                0f,
                -0.25f,
                120,
                default,
                0.8f);
            Main.dust[dust].noGravity = true;
        }
    }

    private static ulong Remaining(ulong deadline, ulong tick, ulong maximum)
        => deadline > tick ? Math.Min(maximum, deadline - tick) : 0;

    private void RefreshRecoveryLockoutBuff()
    {
        int type = lockoutBuffType;
        if (type <= 0) return;
        int ticks = (int)Remaining(reviveLockoutUntilLocalTick, Main.GameUpdateCount, 3_600);
        if (ticks > 0)
        {
            // This icon is a projection, not the authority predicate. Clearing it
            // through another Mod cannot remove the server-owned recovery lockout.
            Player.buffImmune[type] = false;
            Player.AddBuff(type, ticks, quiet: true);
        }
        else if (Player.HasBuff(type))
            Player.ClearBuff(type);
    }
}

internal sealed class FirstSeveranceRaidPlayerSystem : ModSystem
{
    public override void OnWorldUnload() => FirstSeveranceRaidPlayer.ClearAll();
    public override void Unload() => FirstSeveranceRaidPlayer.ClearAll();
}
