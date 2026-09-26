using System;
using System.Collections.Generic;
using System.Linq;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

public sealed class CrimsonRecoveryPlayer : ModPlayer
{
    private static readonly HashSet<CrimsonRecoveryPlayer> bound = new();
    private Guid fight, terminalFight;
    private CrimsonRecoveryState health;
    private CrimsonDownLatch latch;
    private CrimsonNativeFloor floor;
    private Vector2 anchor;
    private ulong validUntil, immunityUntil, lockoutUntil, nextFloorReport;
    private int downBuff, lockBuff;
    internal bool IsIncapacitated => fight != Guid.Empty && (health.Downed || latch.Pending);
    private bool OwnsHurt => Main.netMode == NetmodeID.SinglePlayer
        || Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;
    private bool Active => fight != Guid.Empty && Player.active && !Player.dead && !Player.ghost;

    internal static void Apply(CrimsonState state)
    {
        foreach (var m in state.Members ?? Array.Empty<CrimsonMember>())
        {
            var p = Main.player[m.Slot];
            var adapter = p.GetModPlayer<CrimsonRecoveryPlayer>();
            if (m.Out || !p.active || p.dead || p.ghost
                || Main.netMode != NetmodeID.MultiplayerClient && p.GetModPlayer<CrimsonConnection>().Token != m.Connection)
            { if (adapter.fight == state.Fight) adapter.Clear(); continue; }
            if (state.Stage == CrimsonStage.Defeat)
            {
                bool newDefeat = adapter.terminalFight != state.Fight;
                adapter.Clear(); adapter.terminalFight = state.Fight;
                if (newDefeat && adapter.OwnsHurt)
                {
                    p.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                        "Mods.Convergence.CrimsonRecovery.DefeatReason", p.name)), 9999, 0);
                    if (!p.dead) CrimsonPackets.Log($"event=DefeatDeathBlocked fight={state.Fight} slot={p.whoAmI} external_pre_kill=True");
                }
                continue;
            }
            if (state.Stage == CrimsonStage.Victory)
            { if (adapter.fight == state.Fight) adapter.Clear(); continue; }
            if (m.Recovery.Revision == 0 || state.Stage < CrimsonStage.Countdown || adapter.terminalFight == state.Fight) continue;
            adapter.ApplyState(state.Fight, m.Recovery, state.Age);
        }
    }
    private void ApplyState(Guid owner, CrimsonRecoveryState next, int age)
    {
        if (fight != owner) { Clear(); fight = owner; floor.Reset(Player.statLife); }
        if (!next.CanReplace(health)) return;
        bool wasDown = IsIncapacitated;
        uint previous = health.Revision;
        health = next; latch.Observe(next); bound.Add(this);
        validUntil = Main.GameUpdateCount + 180;
        immunityUntil = Main.GameUpdateCount + (ulong)Math.Clamp(next.ImmunityUntil - age, 0, 180);
        lockoutUntil = Main.GameUpdateCount + (ulong)Math.Clamp(next.LockoutUntil - age, 0, 3600);
        if (next.Downed) anchor = new(next.X, next.Y);
        // Initial binding is not a heal. Subsequent revisions represent only
        // authoritative Down/revive corrections, never ordinary damage.
        if (next.Revision > previous && next.Revision > 1 && !Player.dead)
        {
            int oldLife = Player.statLife;
            Player.statLife = Math.Clamp(next.Life, 1, Player.statLifeMax2);
            floor.Reset(Player.statLife);
            if (OwnsHurt && !Main.dedServ && Player.statLife > oldLife) Player.HealEffect(Player.statLife - oldLife, broadcast: false);
        }
        if (IsIncapacitated && !wasDown) StopGrapples();
        downBuff = ModContent.BuffType<CrimsonDownedDebuff>();
        lockBuff = ModContent.BuffType<RecoveryLockoutDebuff>();
    }
    public override void PreUpdate()
    {
        if (fight != Guid.Empty && (Main.gameMenu || !Player.active || Player.dead || Player.ghost
            || Main.GameUpdateCount > validUntil || CrimsonPackets.Snapshot.FightId.Value != fight)) Clear();
    }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (OwnsHurt && Active && !IsIncapacitated && Player.statLife > 1)
            modifiers.SetMaxDamage(Player.statLife - 1);
    }
    public override void PostHurt(Player.HurtInfo info)
    { if (OwnsHurt && Active && Player.statLife <= 1) LatchFloor("Hurt"); }
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        // Do not infer a hit here: another Mod may grant immunity or dodge.
        // Actual Hurt/PreKill (and the post-admission floor edge) own Down.
        return Active && (IsIncapacitated || Main.GameUpdateCount < immunityUntil);
    }
    public override bool PreKill(double damage, int hitDirection, bool pvp,
        ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        // Scoped fallback for DoT/foreign KillMe paths that do not pass Hurt.
        // This does not bypass other Mods' PreKill callbacks or God Mode.
        if (!OwnsHurt || !Active || Player.statLife > 0 && !IsIncapacitated) return true;
        Player.statLife = 1; LatchFloor("PreKill"); playSound = genDust = false;
        return false;
    }
    private void LatchFloor(string source)
    {
        if (!IsIncapacitated)
        {
            floor.Observe(1, nativeLethalReceipt: true);
            latch.Request(health.Revision); anchor = Player.position; StopGrapples();
            CrimsonPackets.Log($"event=NativeDownFloor fight={fight} slot={Player.whoAmI} health_revision={health.Revision} source={source}");
        }
        Player.statLife = 1;
        if (!health.Downed && Main.GameUpdateCount >= nextFloorReport)
        {
            nextFloorReport = Main.GameUpdateCount + 30;
            CrimsonPackets.RequestRecovery(revive: false);
        }
    }
    private void StopGrapples()
    {
        Player.mount.Dismount(Player);
        if (!OwnsHurt && Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (var projectile in Main.ActiveProjectiles)
            if (projectile.owner == Player.whoAmI && projectile.aiStyle == ProjAIStyleID.Hook) projectile.Kill();
    }
    public override void SetControls()
    {
        if (!IsIncapacitated) return;
        Player.controlLeft = Player.controlRight = Player.controlUp = Player.controlDown = Player.controlJump = false;
        Player.controlUseItem = Player.controlUseTile = Player.controlHook = Player.controlMount = false;
    }
    public override void PreUpdateMovement()
    {
        if (!IsIncapacitated) return;
        Player.velocity = Vector2.Zero; Player.position = anchor; Player.fallStart = (int)(anchor.Y / 16);
        if (Player.mount.Active) Player.mount.Dismount(Player);
    }
    public override bool CanUseItem(Item item) => !IsIncapacitated;
    public override bool? CanHitNPCWithItem(Item item, NPC target) => IsIncapacitated ? false : null;
    public override bool? CanHitNPCWithProj(Projectile projectile, NPC target) => IsIncapacitated ? false : null;
    public override void UpdateBadLifeRegen()
    {
        if (IsIncapacitated) { Player.lifeRegen = Player.lifeRegenCount = 0; Player.statLife = 1; }
    }
    public override void PostUpdate()
    {
        if (!Active) return;
        if (OwnsHurt && floor.Observe(Player.statLife)) LatchFloor("UpdateFloor");
        if (IsIncapacitated)
        {
            Player.statLife = 1; Player.lifeRegen = Player.lifeRegenCount = 0;
            Player.position = anchor; Player.velocity = Vector2.Zero;
            Player.buffImmune[downBuff] = false; Player.AddBuff(downBuff, 2, quiet: true);
            if (!Main.dedServ && Main.GameUpdateCount % 5 == 0)
            {
                var d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.RedTorch,
                    0, -.4f, 100, Color.IndianRed, .9f);
                d.noGravity = true;
            }
        }
        else if (downBuff > 0) Player.ClearBuff(downBuff);
        if (lockBuff > 0 && Main.GameUpdateCount < lockoutUntil)
        {
            Player.buffImmune[lockBuff] = false;
            Player.AddBuff(lockBuff, (int)(lockoutUntil - Main.GameUpdateCount), quiet: true);
        }
    }
    private void Clear()
    {
        bool recover = IsIncapacitated && Player.active && !Player.dead;
        bound.Remove(this); fight = Guid.Empty; health = default; latch = default; floor = default;
        validUntil = immunityUntil = lockoutUntil = nextFloorReport = 0;
        if (recover)
        {
            Player.statLife = Math.Max(Player.statLife, Math.Max(1, (int)Math.Ceiling(Player.statLifeMax2 * .35)));
            Player.immune = true; Player.immuneTime = Math.Max(Player.immuneTime, 180);
        }
        if (downBuff > 0) Player.ClearBuff(downBuff);
        if (lockBuff > 0) Player.ClearBuff(lockBuff);
        downBuff = lockBuff = 0;
    }
    internal static void ClearFight(Guid id)
    { foreach (var p in bound.ToArray()) if (p.fight == id) p.Clear(); }
    internal static void ClearAll() { foreach (var p in bound.ToArray()) p.Clear(); }
    public override void OnEnterWorld() { Clear(); terminalFight = Guid.Empty; }
    public override void PlayerDisconnect() => Clear();
}

public sealed class CrimsonDownedDebuff : ModBuff
{
    public override string Texture => "Convergence/Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem";
    public override void SetStaticDefaults()
    { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }
    public override bool RightClick(int buffIndex) => false;
}

internal sealed class CrimsonRecoverySystem : ModSystem
{
    public override void OnWorldUnload() => CrimsonRecoveryPlayer.ClearAll();
    public override void Unload() => CrimsonRecoveryPlayer.ClearAll();
}
