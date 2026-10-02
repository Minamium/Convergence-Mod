#nullable enable
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// One reward sprite: the final texture's planned name and logical size (in logical pixels, the 2 px dot), the brief part
// it comes from, the anchors to measure on delivery, and the vanilla texture that stands in until it exists. Sizes are
// the brief's targets with its "never more than" limits; the exporter records the measured sizes and anchors when the
// art is delivered.
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
// (REWARDS.md#art-and-audio; asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md). Until delivery each weapon's
// icon and in-world bodies draw that weapon's vanilla placeholder item texture, so the mechanics can be played first.
// Swapping in the real pixel art means exporting PNGs under Root with these names (icons at TexelScale); no code changes.
internal static class CrimsonRewardSprites
{
    internal const string Root = "Convergence/Assets/Textures/Items/ScarletRewards/";
    internal const float PixelScale = 2;

    // ---- SR01 / SR01P: Scarlet Score Reliquary (placeholder: Crimson Fishing Crate) -------------------------------
    internal static readonly CrimsonRewardSprite Reliquary = new(nameof(CrimsonScoreReliquary), "SR01 top-left (complete)", 30, 24, 32, 28, ItemID.CrimsonFishingCrate, Icon: true);
    internal static readonly CrimsonRewardSprite ReliquaryBody = new("ReliquaryBody", "SR01/SR01P body", 30, 18, 32, 22, ItemID.CrimsonFishingCrate, Anchors: "seal recess centre");
    internal static readonly CrimsonRewardSprite ReliquaryLid = new("ReliquaryLid", "SR01/SR01P lid", 32, 10, 34, 12, ItemID.CrimsonFishingCrate, Anchors: "rear hinge");
    internal static readonly CrimsonRewardSprite ReliquarySeal = new("ReliquarySeal", "SR01/SR01P wax seal", 8, 8, 10, 10, ItemID.CrimsonFishingCrate);

    // ---- SR02 / SR02I: Sable Scythe (placeholder: Death Sickle) --------------------------------------------------
    internal static readonly CrimsonRewardSprite Scythe = new(nameof(CrimsonSableScythe), "SR02I icon", 28, 28, 32, 32, ItemID.DeathSickle, Icon: true);
    internal static readonly CrimsonRewardSprite ScytheHeld = new("SableScythe", "SR02 held (haft lower left, blade upper right)", 60, 60, 64, 64, ItemID.DeathSickle, Anchors: "grip, hook tip");

    // ---- SR03: Canticle Organ (placeholder: Onyx Blaster) --------------------------------------------------------
    internal static readonly CrimsonRewardSprite Organ = new(nameof(CrimsonCanticleOrgan), "SR03 bottom-right icon", 28, 28, 28, 28, ItemID.OnyxBlaster, Icon: true);
    internal static readonly CrimsonRewardSprite OrganHeld = new("CanticleOrgan", "SR03 top-left held (facing right)", 40, 18, 44, 22, ItemID.OnyxBlaster, Anchors: "grip, four pipe mouths");
    internal static readonly CrimsonRewardSprite OrganShard = new("CanticleShard", "SR03 top-right bone shard", 10, 4, 12, 6, ItemID.OnyxBlaster);
    internal static readonly CrimsonRewardSprite BoneHand = new("BoneHand", "SR03 bottom-left bone hand (palm down)", 20, 28, 24, 32, ItemID.OnyxBlaster, Anchors: "palm");

    // ---- SR04 / SR04I: Scarlet Baton (placeholder: Crimson Rod) --------------------------------------------------
    internal static readonly CrimsonRewardSprite Baton = new(nameof(CrimsonBaton), "SR04I icon", 28, 28, 32, 32, ItemID.CrimsonRod, Icon: true);
    internal static readonly CrimsonRewardSprite BatonHeld = new("ScarletBaton", "SR04 held (grip lower left)", 36, 36, 40, 40, ItemID.CrimsonRod, Anchors: "grip, gem");

    // ---- SR05: Ember Censer (placeholder: Imp Staff and its buff) ------------------------------------------------
    internal static readonly CrimsonRewardSprite Censer = new(nameof(CrimsonEmberCenser), "SR05 left icon", 20, 30, 24, 32, ItemID.ImpStaff, Icon: true);
    internal static readonly CrimsonRewardSprite CenserMinion = new("EmberCenser", "SR05 middle crown censer (unlit)", 28, 30, 32, 36, ItemID.ImpStaff, Anchors: "ring, bowl mouth");
    internal static readonly CrimsonRewardSprite CenserBuff = new(nameof(CrimsonEmberCenserBuff), "SR05 right buff icon", 14, 14, 14, 14, ItemID.ImpStaff, "Terraria/Images/Buff_" + BuffID.ImpMinion, Icon: true);

    // ---- SR06: Bloodink Quill (placeholder: Bone Javelin) --------------------------------------------------------
    internal static readonly CrimsonRewardSprite Quill = new(nameof(CrimsonBloodinkQuill), "SR06 middle icon", 22, 28, 28, 32, ItemID.BoneJavelin, Icon: true);
    internal static readonly CrimsonRewardSprite QuillProjectile = new("BloodinkQuill", "SR06 left quill (nib right)", 24, 6, 28, 8, ItemID.BoneJavelin, Anchors: "nib");
    internal static readonly CrimsonRewardSprite SealedScore = new("SealedScore", "SR06 right rolled score", 20, 10, 24, 12, ItemID.BoneJavelin, Anchors: "seal");

    // ---- SR07: Scarlet Covenant (placeholders: the current Covenant icon and the Pygmies buff) --------------------
    internal static readonly CrimsonRewardSprite Covenant = new(nameof(CrimsonPact), "SR07 left icon", 28, 28, 32, 32, 0, "Convergence/Assets/Textures/CrimsonFoundry/CrimsonPact", Icon: true);
    internal static readonly CrimsonRewardSprite CovenantBuff = new(nameof(CrimsonPactBuff), "SR07 right buff icon", 14, 14, 14, 14, 0, "Terraria/Images/Buff_" + BuffID.Pygmies, Icon: true);

    internal static readonly CrimsonRewardSprite[] All =
    {
        Reliquary, ReliquaryBody, ReliquaryLid, ReliquarySeal, Scythe, ScytheHeld, Organ, OrganHeld, OrganShard, BoneHand,
        Baton, BatonHeld, Censer, CenserMinion, CenserBuff, Quill, QuillProjectile, SealedScore, Covenant, CovenantBuff,
    };
}
