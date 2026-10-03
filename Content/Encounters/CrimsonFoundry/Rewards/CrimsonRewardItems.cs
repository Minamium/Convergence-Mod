#nullable enable
using Convergence.Common.Compatibility.Calamity;
using Convergence.Content.Encounters.AzureCathedral;
using Convergence.Content.Encounters.EbonManor;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Shared item contract for the Scarlet Invocation reward set (docs/encounters/crimson-foundry/REWARDS.md).
// Weapons: CrimsonSableScythe (melee), CrimsonCanticleOrgan (ranged), CrimsonBaton (magic), CrimsonEmberCenser
// (summon), CrimsonBloodinkQuill (rogue); box CrimsonScoreReliquary; companion CrimsonPact (re-crafted).
// Content never references Client code: presentation enters through static hooks (CrimsonScoreReliquary.Opened).
internal static class CrimsonRewardItems
{
    // Final pixel art under CrimsonRewardSprites.Root, or the vanilla stand-in until it is delivered.
    internal static string Icon(in CrimsonRewardSprite sprite) => sprite.Path;

    // A weapon cannot be used, its held projectiles stop and its censers idle while the owner is dead or Down in any
    // Raid: Scarlet, Azure Cathedral, Ebon, or Down/eliminated in Doll (including its pending Down latch).
    internal static bool Usable(Player player) => player.active && !player.dead
        && !player.GetModPlayer<CrimsonRecoveryPlayer>().IsIncapacitated
        && !player.GetModPlayer<AzureRecoveryPlayer>().IsIncapacitated
        && !player.GetModPlayer<EbonRecoveryPlayer>().IsIncapacitated
        && !player.GetModPlayer<FirstSeveranceRaidPlayer>().IsIncapacitated;

    // Usable and free to act this tick (not stunned, not item-locked): the gate for starting builds and releases.
    internal static bool CanAct(Player player) => Usable(player) && !player.noItems && !player.CCed;

    // The scythe is ordinary Melee in every part (owner decision 4, 2026-10-03: not Calamity's true melee); the rogue
    // quill uses Calamity's rogue class through its compatibility adapter.
    internal static DamageClass DamageClassFor(CrimsonRewardKind kind) => kind switch
    {
        CrimsonRewardKind.Melee => DamageClass.Melee,
        CrimsonRewardKind.Ranged => DamageClass.Ranged,
        CrimsonRewardKind.Magic => DamageClass.Magic,
        CrimsonRewardKind.Summon => DamageClass.Summon,
        _ => CalamityRogueArmamentDamage.Class,
    };

    internal static int TypeFor(CrimsonRewardKind kind) => kind switch
    {
        CrimsonRewardKind.Melee => ModContent.ItemType<CrimsonSableScythe>(),
        CrimsonRewardKind.Ranged => ModContent.ItemType<CrimsonCanticleOrgan>(),
        CrimsonRewardKind.Magic => ModContent.ItemType<CrimsonBaton>(),
        CrimsonRewardKind.Summon => ModContent.ItemType<CrimsonEmberCenser>(),
        _ => ModContent.ItemType<CrimsonBloodinkQuill>(),
    };

    // One authoritative pool for the reliquary and the Scarlet Covenant recipe.
    internal static int[] RewardTypes() => new[]
    {
        TypeFor(CrimsonRewardKind.Melee), TypeFor(CrimsonRewardKind.Ranged), TypeFor(CrimsonRewardKind.Magic),
        TypeFor(CrimsonRewardKind.Summon), TypeFor(CrimsonRewardKind.Rogue),
    };

    // Tier shared with the Doll and Ebon sets: Red rarity, 40 gold, knockback 6, crit 8 (summon 0).
    internal static void Defaults(Item item, CrimsonRewardKind kind)
    {
        item.damage = CrimsonRewardRules.Damage(kind);
        item.DamageType = DamageClassFor(kind);
        item.useTime = item.useAnimation = CrimsonRewardRules.UseTicks(kind);
        item.knockBack = CrimsonRewardRules.Knockback;
        item.crit = CrimsonRewardRules.CritFor(kind);
        item.rare = ItemRarityID.Red;
        item.value = Item.sellPrice(gold: CrimsonRewardRules.SellGold);
    }

    internal static Vector2 Aim(Vector2 velocity, int facing)
        => velocity.LengthSquared() > .001f && float.IsFinite(velocity.X) && float.IsFinite(velocity.Y)
            ? Vector2.Normalize(velocity) : new Vector2(facing == -1 ? -1 : 1, 0);

    // Damage for secondary hits: always derived from the live weapon damage, at least 1.
    internal static int Hit(int damage, float multiplier) => CrimsonRewardRules.Scaled(damage, multiplier);

    // Segmented NPCs count once per release part: key ledgers by the realLife root.
    internal static int Root(NPC npc) => npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? npc.realLife : npc.whoAmI;

    // A projectile that refers to an NPC carries its slot; the owner also keeps this value and retires the
    // projectile when the slot is reused (the companion's incarnation guard).
    internal static ulong Incarnation(NPC npc) => npc.GetGlobalNPC<CrimsonCovenantIncarnation>().Value;

    // The NPC in `slot` if it is still the one the owner recorded (and still alive); remote clients pass 0 to accept
    // whatever replicated NPC holds the slot.
    internal static bool TryNpc(int slot, ulong incarnation, out NPC npc)
    {
        npc = null!;
        if (slot < 0 || slot >= Main.maxNPCs) return false;
        npc = Main.npc[slot];
        return npc.active && npc.life > 0 && (incarnation == 0 || Incarnation(npc) == incarnation);
    }

    // The native ai slots of every reward projectile must be finite and in range, or the projectile kills itself.
    internal static bool ValidAi(Projectile projectile, float min0, float max0, float min1, float max1, float min2, float max2)
        => CrimsonRewardRules.Valid(projectile.ai[0], min0, max0) && CrimsonRewardRules.Valid(projectile.ai[1], min1, max1)
            && CrimsonRewardRules.Valid(projectile.ai[2], min2, max2)
            && projectile.owner >= 0 && projectile.owner < Main.maxPlayers;
}
