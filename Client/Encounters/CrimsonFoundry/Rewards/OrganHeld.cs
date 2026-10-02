#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The held Canticle Organ (REWARDS.md, "Ranged - Canticle Organ", Aim and Presentation): held at its grip in the front
// hand, flipped vertically when aimed left, following the aim spring. The firing pipe kicks the organ back 2 px and it
// springs home; the hymn lifts it and it inhales (0-10) while the heart-gem brightens. The owner draws the aim its own
// spring computed (CanticleOrganPlayer, the same aim the shards leave along); peers spring toward the synced
// itemRotation. Shots and hymns are read from the replicated shards and hands (OrganProjectiles), so every peer poses
// the same way without a packet. Nothing here decides a hit or spends a resource.
[Autoload(Side = ModSide.Client)]
internal sealed class OrganPosePlayer : ModPlayer
{
    private float aim, previousAim, aimVelocity;
    private bool aimed, shot, hymn;
    private ulong shotAt, hymnAt, lastAnimated;

    internal bool Posing { get; private set; }
    internal int Pipe { get; private set; }

    internal void Shot(int pipe) { shot = true; shotAt = Main.GameUpdateCount; Pipe = pipe; }
    internal void Hymn() { hymn = true; hymnAt = Main.GameUpdateCount; }

    // Ticks since the last shot or hymn on the drawn clock (one sample behind, interpolated); -1 when none.
    internal float SinceShot(float fraction) => shot ? Since(shotAt, fraction) : -1;
    internal float SinceHymn(float fraction) => hymn ? Since(hymnAt, fraction) : -1;
    private static float Since(ulong at, float fraction)
    {
        ulong now = Main.GameUpdateCount;
        return now < at ? -1 : (float)(now - at) - 1 + fraction;
    }

    internal void Clear()
    {
        aimed = shot = hymn = false;
        Posing = false;
        shotAt = hymnAt = lastAnimated = 0;
        aimVelocity = 0;
    }

    // The aim the organ is drawn at: the spring's, lifted by the hymn's inhale toward the sky.
    internal float DrawAim(float fraction)
    {
        float baseAim;
        if (Player.whoAmI == Main.myPlayer && Player.GetModPlayer<CanticleOrganPlayer>() is { Aimed: true } own)
            baseAim = CanticleRules.LerpAngle(own.PreviousAim, own.Aim, fraction);
        else baseAim = CanticleRules.LerpAngle(previousAim, aim, fraction);
        float lift = CanticleRules.HymnLift * CanticleRules.Inhale(SinceHymn(fraction));
        return baseAim - lift * (MathF.Cos(baseAim) < 0 ? -1 : 1);
    }

    public override void PostUpdate()
    {
        bool held = Player.active && !Player.dead && Player.HeldItem.type == ModContent.ItemType<CrimsonCanticleOrgan>();
        if (!held) { Posing = false; aimed = false; return; }
        ulong now = Main.GameUpdateCount;
        if (Player.itemAnimation > 0) lastAnimated = now;
        previousAim = aim;
        if (Player.whoAmI != Main.myPlayer && float.IsFinite(Player.itemRotation))
        {
            // itemRotation is the aim in the facing frame, synced with each shot's use animation.
            Vector2 dir = new Vector2(MathF.Cos(Player.itemRotation), MathF.Sin(Player.itemRotation)) * Player.direction;
            float target = dir.ToRotation();
            if (!aimed) { aim = previousAim = target; aimVelocity = 0; aimed = true; }
            else (aim, aimVelocity) = CanticleRules.SpringAim(aim, aimVelocity, target);
        }
        bool hymning = hymn && now >= hymnAt && now - hymnAt < (ulong)CrimsonRewardRules.HymnUseTicks;
        // One tick of grace between two auto-reused shots, so the organ never blinks out between them.
        Posing = (Player.itemAnimation > 0 || now - lastAnimated <= 1 || hymning) && CrimsonRewardItems.Usable(Player);
        if (Posing) Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, DrawAim(1) - MathHelper.PiOver2);
    }

    // The arm follows the drawn aim, whatever pose the vanilla use style left (as the Moonloom Harp does).
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (!Posing || drawInfo.headOnlyRender) return;
        float arm = DrawAim(WeaponDrawClock.Fraction) - MathHelper.PiOver2;
        drawInfo.compositeFrontArmRotation = Player.gravDir == -1f ? -arm : arm;
    }
}

// Draws the organ in the player's own draw set, after the held-item layer so the front hand stays over the grip.
[Autoload(Side = ModSide.Client)]
internal sealed class OrganHeldLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        => !drawInfo.headOnlyRender && drawInfo.shadow == 0f && drawInfo.drawPlayer.active
            && drawInfo.drawPlayer.GetModPlayer<OrganPosePlayer>().Posing;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (OrganArt.Gun is not { } art) return;
        Player player = drawInfo.drawPlayer;
        var pose = player.GetModPlayer<OrganPosePlayer>();
        float fraction = WeaponDrawClock.Fraction;
        float aim = pose.DrawAim(fraction);
        Vector2 dir = aim.ToRotationVector2();
        Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
        Vector2 grip = hand - dir * CanticleRules.Kick(pose.SinceShot(fraction));
        bool flip = dir.X < 0;
        Color light = Lighting.GetColor(grip.ToTileCoordinates());
        drawInfo.DrawDataCache.Add(new DrawData(art.Texture, grip - Main.screenPosition, art.Source, light, aim, art.Origin(flip), art.Scale,
            flip ? SpriteEffects.FlipVertically : SpriteEffects.None));
        // The heart-gem brightens with the hymn's inhale: a soft crimson glow, additive (premultiplied, alpha 0).
        float glow = CanticleRules.Inhale(pose.SinceHymn(fraction));
        if (glow <= .01f) return;
        Texture2D soft = ScarletVfxHost.Assets.GetTexture("Luminance/BloomCircleSmall");
        Vector2 gem = grip + art.GemOffset(aim, flip);
        Vector2 origin = new(soft.Width * .5f, soft.Height * .5f);
        drawInfo.DrawDataCache.Add(new DrawData(soft, gem - Main.screenPosition, null, new Color(1f, .12f, .08f, 0f) * (.85f * glow), 0, origin,
            30f / soft.Width, SpriteEffects.None));
        drawInfo.DrawDataCache.Add(new DrawData(soft, gem - Main.screenPosition, null, new Color(1f, .62f, .4f, 0f) * (.6f * glow), 0, origin,
            10f / soft.Width, SpriteEffects.None));
    }
}
