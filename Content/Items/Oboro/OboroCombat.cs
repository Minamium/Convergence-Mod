using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Items.Oboro;

public sealed partial class OboroPlayer
{
    private static NPC Root(NPC npc) => npc.realLife >= 0 && npc.realLife < Main.maxNPCs
        && Main.npc[npc.realLife].active ? Main.npc[npc.realLife] : npc;
    private static bool Enemy(NPC npc) => npc.active && npc.life > 0 && !npc.friendly && !npc.dontTakeDamage && !npc.immortal;
    private void ResolveSwing()
    {
        float p = age / (float)duration;
        if (!OboroRules.Live(step, p)) return;
        float current = aim + facing * OboroRules.Offset(step, p);
        float prior = Math.Max(OboroRules.Windup(step), (age - 1f) / duration);
        float previous = aim + facing * OboroRules.Offset(step, prior);
        var hand = OboroHandAnchor.Capture(Player, facing);
        Vector2 CenterAt(float progress, float angle)
        {
            var offset = OboroRules.RootOffset(step, progress, aim, angle, hand);
            return Player.MountedCenter + new Vector2(offset.X, offset.Y);
        }
        Vector2 center = CenterAt(p, current), priorCenter = CenterAt(prior, previous);
        float rootTravel = Vector2.Distance(center, priorCenter);
        foreach (NPC target in Main.ActiveNPCs)
        {
            if (!Enemy(target)) continue;
            Vector2 nearest = Vector2.Clamp(center, target.position, target.position + target.Size);
            if (Vector2.DistanceSquared(center, nearest) > MathF.Pow(OboroRules.Reach + OboroRules.BladeWidth + rootTravel, 2)) continue;
            NPC root = Root(target);
            var identity = root.GetGlobalNPC<OboroNpc>();
            if (identity.Generation == 0 || struck.Contains(identity.Generation)) continue;
            bool contact = false;
            // Sweep between successive blade poses; fast cuts cannot skip thin enemies.
            int samples = step == 2 ? OboroThirdSwingMotion.SweepSamples(prior, p, rootTravel)
                : step == 1 ? OboroSecondSwingMotion.SweepSamples(prior, p, rootTravel)
                : Math.Clamp((int)MathF.Ceiling(Math.Max(Math.Abs(current - previous) / .035f, rootTravel / 4)), 1, 64);
            for (int i = 0; i <= samples && !contact; i++)
            {
                float sample = MathHelper.Lerp(prior, p, i / (float)samples), collision = 0;
                float angle = aim + facing * OboroRules.Offset(step, sample);
                Vector2 origin = CenterAt(sample, angle);
                contact = Collision.CheckAABBvLineCollision(target.position, target.Size, origin,
                    origin + angle.ToRotationVector2() * OboroRules.Reach, OboroRules.BladeWidth, ref collision);
            }
            if (!contact || !Collision.CanHitLine(center, 1, 1, target.position, target.width, target.height)) continue;
            int power = Player.GetWeaponDamage(swingItem);
            int damage = (int)(power * (step == 2 ? OboroRules.HeavyMultiplier : 1));
            if (!Strike(target, swingItem, damage, facing)) continue;
            struck.Add(identity.Generation);
            if (!Enemy(root)) continue;
            if (step == 2) { identity.FireRemaining = OboroRules.FireTicks; root.netUpdate = true; }
            if (zanshin > 0)
            {
                if (!wounds.TryGetValue(identity.Generation, out OboroWounds entry))
                    wounds.Add(identity.Generation, entry = new(root.whoAmI, identity.Generation));
                if (entry.Add(current, power)) { identity.MarkCount++; root.netUpdate = true; }
            }
        }
    }
    private bool Strike(NPC target, Item item, int damage, int direction)
    {
        if (!Authority || !Enemy(target) || CombinedHooks.CanPlayerHitNPCWithItem(Player, item, target) == false) return false;
        // Native item hit hooks preserve defense, Calamity modifiers, encounter
        // target reductions and player on-hit effects. Only the server calls this.
        var modifiers = target.GetIncomingStrikeModifiers(item.DamageType, direction);
        modifiers.ArmorPenetration += Player.GetWeaponArmorPenetration(item);
        CombinedHooks.ModifyPlayerHitNPCWithItem(Player, item, target, ref modifiers);
        bool crit = Main.rand.Next(100) < Player.GetWeaponCrit(item);
        var hit = modifiers.ToHitInfo(damage, crit, Player.GetWeaponKnockback(item, item.knockBack), true, Player.luck);
        target.lastInteraction = Player.whoAmI;
        target.playerInteraction[Player.whoAmI] = true;
        int dealt = target.StrikeNPC(hit);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(target, hit);
        if (dealt > 0) CombinedHooks.OnPlayerHitNPCWithItem(Player, item, target, hit, dealt);
        return dealt > 0;
    }
    private static NPC Resolve(OboroWounds entry)
    {
        if (entry.Slot < 0 || entry.Slot >= Main.maxNPCs) return null;
        NPC npc = Main.npc[entry.Slot];
        return Enemy(npc) && npc.GetGlobalNPC<OboroNpc>().Generation == entry.Generation ? npc : null;
    }
    private void Detonate()
    {
        timing.EndZanshin();
        // Consume before hooks/damage: reentrant expiry/toggle cannot burst twice.
        var batch = new OboroWounds[wounds.Count]; wounds.Values.CopyTo(batch, 0); wounds.Clear();
        foreach (var entry in batch)
        {
            NPC npc = Resolve(entry);
            if (npc is null) continue;
            var state = npc.GetGlobalNPC<OboroNpc>();
            state.MarkCount = Math.Max(0, state.MarkCount - entry.Marks.Count); npc.netUpdate = true;
            var item = new Item(ModContent.ItemType<Oboro>()) { DamageType = DamageClass.Melee };
            var angles = new float[entry.Marks.Count];
            for (int i = 0; i < angles.Length; i++) angles[i] = OboroRules.PhantomAngle(entry.Marks[i].Angle, i);
            for (int i = 0; i < entry.Marks.Count && Enemy(npc); i++)
            {
                var mark = entry.Marks[i];
                Strike(npc, item, (int)(mark.Damage * OboroRules.PhantomMultiplier),
                    MathF.Cos(OboroRules.PhantomAngle(mark.Angle, i)) < 0 ? -1 : 1);
            }
            OboroPackets.Burst(new(npc.Center.X, npc.Center.Y, angles));
        }
        Publish();
    }
    private void PruneWounds()
    {
        if (wounds.Count == 0) return;
        // At most one entry per native NPC slot, allocated only while marking.
        var remove = new System.Collections.Generic.List<ulong>();
        foreach (var pair in wounds) if (Resolve(pair.Value) is null) remove.Add(pair.Key);
        foreach (ulong key in remove) wounds.Remove(key);
    }
    private void ClearWounds()
    {
        foreach (var entry in wounds.Values)
            if (Resolve(entry) is { } npc)
            { var state = npc.GetGlobalNPC<OboroNpc>(); state.MarkCount = Math.Max(0, state.MarkCount - entry.Marks.Count); npc.netUpdate = true; }
        wounds.Clear();
    }
}
