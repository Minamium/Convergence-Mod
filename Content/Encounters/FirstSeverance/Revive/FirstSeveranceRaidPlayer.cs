using System;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Revive;

public sealed class FirstSeveranceRaidPlayer : ModPlayer
{
    private FightId fightId;
    private uint healthRevision;
    private Vector2 downedPosition;
    private ulong immunityUntilLocalTick;
    private ulong weaknessUntilLocalTick;

    internal bool IsRaidDowned { get; private set; }

    internal bool IsRaidEliminated { get; private set; }

    internal bool IsReviving { get; private set; }

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

        bool wasDowned = IsRaidDowned;
        IsRaidDowned = participant.CombatState == RaidParticipantCombatState.Downed;
        IsRaidEliminated = participant.CombatState == RaidParticipantCombatState.Eliminated;
        IsReviving = participant.IsReviving;
        downedPosition = new Vector2(participant.AnchorX, participant.AnchorY);
        immunityUntilLocalTick = Main.GameUpdateCount
            + Remaining(participant.InvulnerabilityUntilTick, authorityTick, 180);
        weaknessUntilLocalTick = Main.GameUpdateCount
            + Remaining(participant.WeaknessUntilTick, authorityTick, 600);

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

    internal void ClearRaidState()
    {
        if ((IsRaidDowned || IsRaidEliminated) && Player.active && !Player.dead)
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
        immunityUntilLocalTick = 0;
        weaknessUntilLocalTick = 0;
    }

    public override void SetControls()
    {
        if (IsRaidDowned || IsRaidEliminated)
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
        if (!IsRaidDowned && !IsRaidEliminated)
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
        return !IsRaidDowned && !IsRaidEliminated
            && (!IsReviving || item.type == ModContent.ItemType<ResuscitationKitItem>());
    }

    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        return IsRaidDowned || IsRaidEliminated || Main.GameUpdateCount < immunityUntilLocalTick;
    }

    public override void UpdateBadLifeRegen()
    {
        if (IsRaidDowned || IsRaidEliminated)
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

    public override bool? CanHitNPCWithItem(Item item, NPC target)
    {
        return IsRaidDowned || IsRaidEliminated || IsReviving
            ? false
            : null;
    }

    public override bool? CanHitNPCWithProj(Projectile projectile, NPC target)
    {
        return IsRaidDowned || IsRaidEliminated || IsReviving
            ? false
            : null;
    }

    public override void PostUpdate()
    {
        if (IsRaidDowned || IsRaidEliminated)
        {
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
}
