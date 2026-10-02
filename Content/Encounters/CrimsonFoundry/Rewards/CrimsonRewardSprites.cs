#nullable enable
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// One reward sprite: the final texture's name and logical size (in logical pixels, the 2 px dot), the delivery part it
// was exported from, its anchors, and the vanilla texture that stands in if the asset is missing. Width/Height are the
// exported sizes (tools/export_scarlet_reward_art.py, 2026-10-03); MaxWidth/MaxHeight are the brief's "never more than"
// limits the exporter fitted oversized drawings to. The measured anchors live in the art classes that use them.
// Texels: a world body is exported at one texel per logical pixel and drawn at PixelScale with point sampling
// (ScarletRewardArt). An item or buff icon (Icon) is consumed through ModItem/ModBuff.Texture, which tML draws at 1x,
// so it is exported at TexelScale = PixelScale texels per logical pixel (Ebon's "icon 2x"), never upscaled in code.
internal readonly record struct CrimsonRewardSprite(string Name, string Brief, int Width, int Height, int MaxWidth, int MaxHeight,
    int PlaceholderItem, string? PlaceholderPath = null, string Anchors = "", bool Icon = false)
{
    internal int TexelScale => Icon ? (int)CrimsonRewardSprites.PixelScale : 1;
    internal string FinalPath => CrimsonRewardSprites.Root + Name;
    internal string PlaceholderTexture => PlaceholderPath ?? $"Terraria/Images/Item_{PlaceholderItem}";
    internal bool HasFinal => ModContent.HasAsset(FinalPath);
    // The texture to load now: the delivered art, or the stand-in.
    internal string Path => HasFinal ? FinalPath : PlaceholderTexture;
}

// The one place that maps every Scarlet reward image to its final art or its placeholder
// (REWARDS.md#art-and-audio; asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md). The Codex pixel art is exported
// under Root with these names (icons at TexelScale); a missing PNG falls back to that weapon's vanilla stand-in
// (HasAsset), which is how the mechanics were played before delivery.
internal static class CrimsonRewardSprites
{
    internal const string Root = "Convergence/Assets/Textures/Items/ScarletRewards/";
    internal const float PixelScale = 2;

    // ---- SR01 / SR01P: Scarlet Score Reliquary (placeholder: Crimson Fishing Crate) -------------------------------
    // The body and lid share one lattice, so the closed casket is as wide as the icon; they keep the complete casket's
    // limit (the brief: every part keeps its size in the complete casket). The seal is one cell finer than that lattice
    // (11 across, not 10), the fewest cells that keep its three bars apart.
    internal static readonly CrimsonRewardSprite Reliquary = new(nameof(CrimsonScoreReliquary), "SR01_a top-left (complete)", 32, 25, 32, 28, ItemID.CrimsonFishingCrate, Icon: true);
    internal static readonly CrimsonRewardSprite ReliquaryBody = new("ReliquaryBody", "SR01P_c body", 32, 18, 32, 28, ItemID.CrimsonFishingCrate, Anchors: "mouth, seal place, lid place");
    internal static readonly CrimsonRewardSprite ReliquaryLid = new("ReliquaryLid", "SR01P_c lid", 30, 13, 32, 28, ItemID.CrimsonFishingCrate, Anchors: "rear hinge");
    internal static readonly CrimsonRewardSprite ReliquarySeal = new("ReliquarySeal", "SR01P_c wax seal", 11, 11, 11, 11, ItemID.CrimsonFishingCrate);

    // ---- SR02 / SR02I: Sable Scythe (placeholder: Death Sickle) --------------------------------------------------
    internal static readonly CrimsonRewardSprite Scythe = new(nameof(CrimsonSableScythe), "SR02I_c icon", 32, 28, 32, 32, ItemID.DeathSickle, Icon: true);
    internal static readonly CrimsonRewardSprite ScytheHeld = new("SableScythe", "SR02_c held (haft lower left, blade upper right)", 64, 56, 64, 64, ItemID.DeathSickle, Anchors: "grip, hook tip");

    // ---- SR03: Canticle Organ (placeholder: Onyx Blaster) --------------------------------------------------------
    internal static readonly CrimsonRewardSprite Organ = new(nameof(CrimsonCanticleOrgan), "SR03_d bottom-right icon", 27, 28, 28, 28, ItemID.OnyxBlaster, Icon: true);
    internal static readonly CrimsonRewardSprite OrganHeld = new("CanticleOrgan", "SR03_d top-left held (facing right)", 43, 22, 44, 22, ItemID.OnyxBlaster, Anchors: "grip, four pipe mouths");
    internal static readonly CrimsonRewardSprite OrganShard = new("CanticleShard", "SR03_d top-right bone shard", 11, 6, 12, 6, ItemID.OnyxBlaster);
    internal static readonly CrimsonRewardSprite BoneHand = new("BoneHand", "SR03_d bottom-left bone hand (palm down)", 20, 32, 24, 32, ItemID.OnyxBlaster, Anchors: "palm");

    // ---- SR04 / SR04I: Scarlet Baton (placeholder: Crimson Rod) --------------------------------------------------
    internal static readonly CrimsonRewardSprite Baton = new(nameof(CrimsonBaton), "SR04I_b icon", 26, 26, 32, 32, ItemID.CrimsonRod, Icon: true);
    internal static readonly CrimsonRewardSprite BatonHeld = new("ScarletBaton", "SR04_c held (grip lower left)", 37, 38, 40, 40, ItemID.CrimsonRod, Anchors: "grip, gem");

    // ---- SR05: Ember Censer (placeholder: Imp Staff and its buff) ------------------------------------------------
    internal static readonly CrimsonRewardSprite Censer = new(nameof(CrimsonEmberCenser), "SR05_d left icon", 18, 32, 24, 32, ItemID.ImpStaff, Icon: true);
    internal static readonly CrimsonRewardSprite CenserMinion = new("EmberCenser", "SR05_d middle crown censer (unlit)", 30, 36, 32, 36, ItemID.ImpStaff, Anchors: "ring, bowl mouth");
    internal static readonly CrimsonRewardSprite CenserBuff = new(nameof(CrimsonEmberCenserBuff), "SR05_d right buff icon (on 16x16)", 14, 13, 14, 14, ItemID.ImpStaff, "Terraria/Images/Buff_" + BuffID.ImpMinion, Icon: true);

    // ---- SR06: Bloodink Quill (placeholder: Bone Javelin) --------------------------------------------------------
    internal static readonly CrimsonRewardSprite Quill = new(nameof(CrimsonBloodinkQuill), "SR06_d middle icon", 20, 32, 28, 32, ItemID.BoneJavelin, Icon: true);
    internal static readonly CrimsonRewardSprite QuillProjectile = new("BloodinkQuill", "SR06_d left quill (nib right)", 25, 7, 28, 8, ItemID.BoneJavelin, Anchors: "nib");
    internal static readonly CrimsonRewardSprite SealedScore = new("SealedScore", "SR06_d right rolled score", 24, 10, 24, 12, ItemID.BoneJavelin, Anchors: "seal");

    // ---- SR07: Scarlet Covenant (placeholders: the current Covenant icon and the Pygmies buff) --------------------
    internal static readonly CrimsonRewardSprite Covenant = new(nameof(CrimsonPact), "SR07_b left icon", 29, 32, 32, 32, 0, "Convergence/Assets/Textures/CrimsonFoundry/CrimsonPact", Icon: true);
    internal static readonly CrimsonRewardSprite CovenantBuff = new(nameof(CrimsonPactBuff), "SR07_b right buff icon (on 16x16)", 14, 14, 14, 14, 0, "Terraria/Images/Buff_" + BuffID.Pygmies, Icon: true);

    internal static readonly CrimsonRewardSprite[] All =
    {
        Reliquary, ReliquaryBody, ReliquaryLid, ReliquarySeal, Scythe, ScytheHeld, Organ, OrganHeld, OrganShard, BoneHand,
        Baton, BatonHeld, Censer, CenserMinion, CenserBuff, Quill, QuillProjectile, SealedScore, Covenant, CovenantBuff,
    };
}
