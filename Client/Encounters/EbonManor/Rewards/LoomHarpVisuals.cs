#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Moonloom Harp presentation (docs/encounters/ebon-manor/REWARDS.md, "Ranged"). The bow body and its bowstring are
// drawn in the player's own draw set, aimed at the cursor and held at the hand; the string's two pegs are the horn
// tips of the art. The string is released and ringing at the shot, then drawn back toward the player over the rest
// of the use time so full draw lands on the next shot. Needles unspool a thread from the fire point; strings
// shimmer, quiver when an enemy crosses, brighten when armed and ring out when plucked (pixel layer).
// Everything is read from replicated projectile and player state on a fractional clock (WeaponDrawClock.Fraction);
// nothing here decides a hit, spends a resource or sends a packet.

// Resolved bow art: final LoomHarp.png, else a vanilla bow by reference. Texture and Source are what is drawn; Sub is
// the draw texture's pixels per art texel (final art is enlarged by nearest neighbour, see LoomHarp.Enlarge) and
// Scale the screen pixels per art texel. Grip and pegs are in art texels from the art's top-left corner.
internal sealed record LoomHarpArt(Texture2D Texture, Rectangle Source, float Scale, int Sub, bool Pixel, Vector2 Grip, Vector2 PegTop, Vector2 PegBottom)
{
    internal float DrawScale => Scale / Sub;
    // Rotation origin in the draw texture. A bow aimed left is drawn turned over, so the grip row turns with it.
    internal Vector2 Origin(bool flip) => new(Grip.X * Sub, flip ? Source.Height - Grip.Y * Sub : Grip.Y * Sub);
}

// The final needle as it lies nocked on the drawn string: same enlargement as the bow. Size is in art texels; the
// point is the right edge and the nock the left edge, on the middle row.
internal sealed record LoomNeedleArt(Texture2D Texture, float Scale, int Sub, Vector2 Size)
{
    internal float DrawScale => Scale / Sub;
    internal Vector2 PointOrigin => new(Size.X * Sub, Size.Y * Sub * .5f);
}

// Where the bow is and how far the string is drawn, for one player at one draw fraction. Dot is the weight of the
// string in px (its outline adds one px on each side).
internal readonly record struct LoomHarpPose(Vector2 Grip, Vector2 Aim, float Rotation, bool Flip, Vector2 PegTop, Vector2 PegBottom,
    float Pull, float PullDistance, float Ring, float RingPhase, bool Strum, float Dot);

internal static class LoomHarp
{
    // Final art is 18x56 logical texels. At the 2 px dot (EbonRewardArt.PixelScale) that is a 112 px bow next to a 42 px
    // tall player (its tips would hang below the feet), so the held bow and needle are drawn one px per texel: 56 px,
    // about the 60 px of the inventory icon. The needle (35 texels) keeps the bow's proportion at the same scale.
    internal const float HeldScale = 1f;
    // The player's draw set cannot switch to point sampling, so final art is enlarged this many times by nearest
    // neighbour and drawn at 1/Sub: the sampler then only blends within a quarter pixel of a texel edge.
    private const int Sub = 4;

    // Anchors of the exported LoomHarp.png from the export report (tools/export_ebon_reward_art.py; texture space,
    // top-left corner origin, texel centre +0.5): horn-tip pegs and the centre of the rose-wrapped grip.
    private const int FinalWidth = 18, FinalHeight = 56;
    private static readonly Vector2 FinalGrip = new(14.19f, 28.33f), FinalPegTop = new(1.5f, 2f), FinalPegBottom = new(1.5f, 54f);

    private static LoomHarpArt? art;
    private static LoomNeedleArt? needle;
    private static bool failed, needleFailed;

    internal static LoomHarpArt? Art
    {
        get
        {
            if (art is not null || failed) return art;
            try { art = Resolve(); }
            catch (Exception e)
            {
                failed = true;
                global::Convergence.ConvergenceMod.Instance.Logger.Warn("Moonloom Harp art unavailable; the bow is not drawn.", e);
            }
            return art;
        }
    }

    // The final needle for the nocked arrow, or null: the placeholder needle is drawn in code.
    internal static LoomNeedleArt? Needle
    {
        get
        {
            if (needle is not null || needleFailed || !EbonRewardArt.HasFinal("NeedleArrow")) return needle;
            try
            {
                Texture2D texture = EbonRewardArt.Final("NeedleArrow");
                needle = new LoomNeedleArt(Enlarge(texture, Pixels(texture), texture.Bounds), HeldScale, Sub, new Vector2(texture.Width, texture.Height));
            }
            catch (Exception e)
            {
                needleFailed = true;
                global::Convergence.ConvergenceMod.Instance.Logger.Warn("Moonloom Harp needle art unavailable; the nocked needle is drawn in code.", e);
            }
            return needle;
        }
    }

    // The enlarged copies are ours; the vanilla and mod textures are not.
    internal static void Reset()
    {
        Texture2D? bow = art is not null && art.Sub > 1 ? art.Texture : null, shaft = needle?.Texture;
        art = null;
        needle = null;
        failed = needleFailed = false;
        if (bow is not null || shaft is not null) Main.QueueMainThreadAction(() => { bow?.Dispose(); shaft?.Dispose(); });
    }

    private static LoomHarpArt Resolve()
    {
        if (EbonRewardArt.HasFinal("LoomHarp"))
        {
            Texture2D texture = EbonRewardArt.Final("LoomHarp");
            Color[] data = Pixels(texture);
            // The delivered 18x56 export has measured anchors; any other silhouette is measured from its pixels.
            if (texture.Width == FinalWidth && texture.Height == FinalHeight)
                return new LoomHarpArt(Enlarge(texture, data, texture.Bounds), new Rectangle(0, 0, FinalWidth * Sub, FinalHeight * Sub),
                    HeldScale, Sub, true, FinalGrip, FinalPegTop, FinalPegBottom);
            Anchors found = Measure(data, texture.Width, texture.Height);
            return new LoomHarpArt(Enlarge(texture, data, found.Source), new Rectangle(0, 0, found.Source.Width * Sub, found.Source.Height * Sub),
                HeldScale, Sub, true, found.Grip, found.PegTop, found.PegBottom);
        }
        var asset = TextureAssets.Item[ItemID.Marrow];
        if (!asset.IsLoaded) Main.instance.LoadItem(ItemID.Marrow);
        Texture2D vanilla = asset.Value;
        Anchors guessed = Measure(Pixels(vanilla), vanilla.Width, vanilla.Height);
        return new LoomHarpArt(vanilla, guessed.Source, 1f, 1, false, guessed.Grip, guessed.PegTop, guessed.PegBottom);
    }

    private static Color[] Pixels(Texture2D texture)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        return data;
    }

    // `source` of the texture (whose pixels are `data`) enlarged Sub times by nearest neighbour.
    private static Texture2D Enlarge(Texture2D texture, Color[] data, Rectangle source)
    {
        int width = source.Width * Sub, height = source.Height * Sub;
        var big = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            big[y * width + x] = data[(source.Y + y / Sub) * texture.Width + source.X + x / Sub];
        var result = new Texture2D(texture.GraphicsDevice, width, height);
        result.SetData(big);
        return result;
    }

    // Opaque bounds, grip and pegs found from the art's own pixels (grip = the belly of the ')' shape, pegs = its
    // left-most tips), in texels relative to Source's top-left. Used by the placeholder and by a re-exported bow.
    private readonly record struct Anchors(Rectangle Source, Vector2 Grip, Vector2 PegTop, Vector2 PegBottom);

    private static Anchors Measure(Color[] data, int w, int h)
    {
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (data[y * w + x].A <= 16) continue;
            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
        }
        if (maxX < 0)
            return new Anchors(new Rectangle(0, 0, w, h), new Vector2(w * .8f, h * .5f), new Vector2(w * .1f, h * .06f), new Vector2(w * .1f, h * .94f));
        var source = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        // The belly: the mean row of the right-most opaque pixels, held a pixel and a half inside the edge.
        float sum = 0; int count = 0;
        for (int y = minY; y <= maxY; y++)
        for (int x = Math.Max(minX, maxX - 1); x <= maxX; x++)
            if (data[y * w + x].A > 16) { sum += y; count++; }
        float gripY = count > 0 ? sum / count : (minY + maxY) * .5f;
        // The tips: among the left-most pixels, the one farthest above and the one farthest below the belly.
        int tipReach = minX + Math.Max(1, source.Width / 8);
        Vector2 top = new(minX, gripY - 4), bottom = new(minX, gripY + 4);
        float bestTop = 0, bestBottom = 0;
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= tipReach; x++)
        {
            if (data[y * w + x].A <= 16) continue;
            float away = y + .5f - gripY;
            if (-away > bestTop) { bestTop = -away; top = new Vector2(x, y); }
            if (away > bestBottom) { bestBottom = away; bottom = new Vector2(x, y); }
        }
        Vector2 ToArt(Vector2 p) => new(p.X - minX + .5f, p.Y - minY + .5f);
        return new Anchors(source, new Vector2(Math.Max(.5f, source.Width - 1.5f), gripY - minY + .5f), ToArt(top), ToArt(bottom));
    }

    // The bow at a draw fraction, or false when this player is not drawing the harp. Peg points follow the art's
    // transform exactly, mirrored when aiming left so the art never turns upside down.
    internal static bool TryGet(Player player, float fraction, out LoomHarpPose pose)
    {
        pose = default;
        if (!player.active || Art is not { } art) return false;
        LoomHarpPlayer state = player.GetModPlayer<LoomHarpPlayer>();
        if (!state.Posing) return false;
        Vector2 aim = state.Aim;
        float rotation = aim.ToRotation();
        Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation - MathHelper.PiOver2);
        Vector2 grip = hand + aim * 2f;
        bool flip = aim.X < 0;
        Vector2 Peg(Vector2 p)
        {
            Vector2 d = (p - art.Grip) * art.Scale;
            if (flip) d.Y = -d.Y;
            return grip + d.RotatedBy(rotation);
        }
        Vector2 top = Peg(art.PegTop), bottom = Peg(art.PegBottom);
        float since = state.Since(fraction);
        bool strum = state.Strumming;
        float pull = strum ? 0 : EbonLoomHarpRules.Pull(Math.Clamp(since / state.CycleLength, 0f, 1f));
        float ring = Math.Max(EbonLoomHarpRules.Ring(since, pull), EbonLoomHarpRules.Ring(state.SincePulse(fraction), 0, EbonLoomHarpRules.PulsePeak));
        float distance = Math.Clamp(Vector2.Distance(top, bottom) * .28f, 10f, 22f);
        // Pixel art keeps its own dot; the placeholder string is the 2 px dot.
        float dot = art.Pixel ? art.Scale : 2f;
        pose = new LoomHarpPose(grip, aim, rotation, flip, top, bottom, pull, distance, ring, (Main.GameUpdateCount + fraction) * 2.1f, strum, dot);
        return true;
    }
}

// Mod unload drops the cached art; world unload clears every player's harp presentation state.
[Autoload(Side = ModSide.Client)]
internal sealed class LoomHarpArtSystem : ModSystem
{
    public override void Unload() => LoomHarp.Reset();

    public override void OnWorldUnload()
    {
        foreach (Player player in Main.player) player.GetModPlayer<LoomHarpPlayer>().Clear();
    }
}

// Per-player harp state for presentation: when the current use cycle began, the aim and the arm pose. Runs for every
// player, so a second peer sees the same bow from the synced use animation.
[Autoload(Side = ModSide.Client)]
internal sealed class LoomHarpPlayer : ModPlayer
{
    private int previousAnimation;
    private ulong cycleStart, lastAnimated, pulseStart;
    private bool strumCycle, pulsed;

    internal bool Posing { get; private set; }
    internal Vector2 Aim { get; private set; } = Vector2.UnitX;
    internal int CycleLength { get; private set; } = 16;
    // Right click (owner) or a recent harp pluck (any peer): the string is struck, not drawn.
    internal bool Strumming => strumCycle || pulsed && Main.GameUpdateCount - pulseStart < 40;
    internal float Since(float fraction) => Main.GameUpdateCount - cycleStart + fraction;
    internal float SincePulse(float fraction) => pulsed ? Main.GameUpdateCount - pulseStart + fraction : -1;

    internal void Pulse() { pulsed = true; pulseStart = Main.GameUpdateCount; }

    internal void Clear()
    {
        Posing = pulsed = strumCycle = false;
        previousAnimation = 0;
        cycleStart = lastAnimated = pulseStart = 0;
    }

    public override void PostUpdate()
    {
        ulong now = Main.GameUpdateCount;
        bool held = Player.active && !Player.dead && Player.HeldItem.type == ModContent.ItemType<EbonLoomHarp>();
        int animation = held && Player.itemAnimationMax > 0 ? Math.Max(0, Player.itemAnimation) : 0;
        if (animation > 0)
        {
            // A rise in the animation counter is a new cycle: the shot (or the strum) just happened.
            if (animation > previousAnimation)
            {
                cycleStart = now;
                CycleLength = Math.Max(1, Player.itemAnimationMax);
                strumCycle = Player.whoAmI == Main.myPlayer && Player.altFunctionUse == 2;
            }
            lastAnimated = now;
            // itemRotation is the aim in the player's facing frame (synced to peers with the use animation).
            if (float.IsFinite(Player.itemRotation))
                Aim = EbonRewardItems.Aim(new Vector2(MathF.Cos(Player.itemRotation), MathF.Sin(Player.itemRotation)) * Player.direction, Player.direction);
        }
        previousAnimation = animation;
        // One tick of grace between two auto-reused cycles, so the bow never blinks out between shots.
        Posing = held && (animation > 0 || lastAnimated != 0 && now - lastAnimated <= 1 && (Player.controlUseItem || Player.controlUseTile))
            && EbonRewardItems.Usable(Player);
        if (Posing) Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Aim.ToRotation() - MathHelper.PiOver2);
    }

    // The arm follows the same aim the bow is drawn at, whatever pose the vanilla use style left behind. Native
    // SetCompositeArmFront negates the rotation at inverted gravity; this draw-only override keeps that transform.
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (!Posing || drawInfo.headOnlyRender) return;
        float arm = Aim.ToRotation() - MathHelper.PiOver2;
        drawInfo.compositeFrontArmRotation = Player.gravDir == -1f ? -arm : arm;
    }
}

// Draws the bow and its string in the player's draw set, after the held-item layer so the front hand stays over the
// grip. The string is not a pixel-layer primitive: that layer composites before players are drawn, which would put
// the drawn string behind the torso. It uses the Ebon palette and a one-dot navy outline instead.
[Autoload(Side = ModSide.Client)]
internal sealed class LoomHarpBowLayer : PlayerDrawLayer
{
    private const int WavePoints = 13;

    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        => !drawInfo.headOnlyRender && drawInfo.shadow == 0f && drawInfo.drawPlayer.active
            && drawInfo.drawPlayer.GetModPlayer<LoomHarpPlayer>().Posing;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (!LoomHarp.TryGet(player, WeaponDrawClock.Fraction, out LoomHarpPose pose) || LoomHarp.Art is not { } art) return;
        SpriteEffects effects = pose.Flip ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Color color = Lighting.GetColor(pose.Grip.ToTileCoordinates());
        drawInfo.DrawDataCache.Add(new DrawData(art.Texture, pose.Grip - Main.screenPosition, art.Source, color, pose.Rotation,
            art.Origin(pose.Flip), art.DrawScale, effects));
        DrawString(ref drawInfo, pose);
    }

    // A straight string, a V toward the player with a needle nocked while drawn, or a standing wave just after the
    // release or a harp pluck. All outlines first so no outline cuts across a neighbouring segment.
    private static void DrawString(ref PlayerDrawSet drawInfo, in LoomHarpPose pose)
    {
        Span<Vector2> points = stackalloc Vector2[WavePoints];
        int count;
        Vector2 a = pose.PegTop, b = pose.PegBottom;
        if (pose.Pull > .02f)
        {
            points[0] = a;
            points[1] = (a + b) * .5f - pose.Aim * pose.PullDistance * pose.Pull;
            points[2] = b;
            count = 3;
        }
        else if (pose.Ring > .3f)
        {
            Vector2 across = b - a;
            Vector2 normal = across.LengthSquared() > 1 ? Vector2.Normalize(across).RotatedBy(MathHelper.PiOver2) : Vector2.UnitX;
            float swing = MathF.Cos(pose.RingPhase) * pose.Ring;
            for (int i = 0; i < WavePoints; i++)
            {
                float u = i / (WavePoints - 1f);
                points[i] = Vector2.Lerp(a, b, u) + normal * (swing * MathF.Sin(MathF.PI * u));
            }
            count = WavePoints;
        }
        else
        {
            points[0] = a;
            points[1] = b;
            count = 2;
        }
        Color outline = EbonPixelArt.Palette[(int)EbonTone.Outline], ivory = EbonPixelArt.Palette[(int)EbonTone.Ivory];
        for (int i = 1; i < count; i++) Segment(ref drawInfo, points[i - 1], points[i], outline, pose.Dot + 2f);
        for (int i = 1; i < count; i++) Segment(ref drawInfo, points[i - 1], points[i], ivory, pose.Dot);
        // The needle rests on the drawn string, its point at the grip (final art: just past it, as an arrow is longer than the draw).
        if (pose.Pull > .35f && !pose.Strum)
        {
            Vector2 nock = points[1];
            if (LoomHarp.Needle is { } shaft)
            {
                // Final art: the butt on the string, lying along the aim, turned over with the bow when aimed left.
                Vector2 point = nock + pose.Aim * ((shaft.Size.X - 1f) * shaft.Scale);
                drawInfo.DrawDataCache.Add(new DrawData(shaft.Texture, point - Main.screenPosition, shaft.Texture.Bounds, Color.White,
                    pose.Rotation, shaft.PointOrigin, shaft.DrawScale, pose.Flip ? SpriteEffects.FlipVertically : SpriteEffects.None));
                return;
            }
            Vector2 tip = nock + pose.Aim * (pose.PullDistance + 14f);
            Segment(ref drawInfo, nock, tip, outline, pose.Dot + 2f);
            Segment(ref drawInfo, nock, tip, EbonPixelArt.Palette[(int)EbonTone.Silver], pose.Dot);
            Segment(ref drawInfo, tip - pose.Aim * 2f, tip, EbonPixelArt.Palette[(int)EbonTone.Moon], pose.Dot);
        }
    }

    // One straight run, thickness in px (the dot or its outline), with a pixel of overlap at both joints.
    private static void Segment(ref PlayerDrawSet drawInfo, Vector2 from, Vector2 to, Color color, float thickness)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length < .5f) return;
        Vector2 direction = delta / length;
        drawInfo.DrawDataCache.Add(new DrawData(TextureAssets.MagicPixel.Value, from - direction - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            color, delta.ToRotation(), new Vector2(0, .5f), new Vector2(length + 2f, thickness), SpriteEffects.None));
    }
}

// Presentation hooks for the two projectiles: registers their pixel sources, fires cues from the same event as the
// visual peak (the needle leaving, a string being struck), and watches enemies crossing idle strings.
[Autoload(Side = ModSide.Client)]
internal sealed class LoomHarpProjectiles : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is EbonNeedleArrow or EbonHarpString;

    // Final needle art is drawn with its point this far ahead of the projectile's centre: the half of the 14 px hitbox.
    private const float NeedleLead = 7f;

    private LoomHarpNeedleSource? needle;
    private LoomHarpStringSource? thread;
    private bool seen, crossing, scheduledLast;
    private float lastState;
    private int scheduledRank;

    // Before the string's own update advances its state. A schedule replicated with its countdown already at 0
    // (the first string, which the owner advanced once before publishing) plucks in that very update, so the rank and
    // last flag are read here rather than from the state the update leaves behind.
    public override bool PreAI(Projectile projectile)
    {
        if (projectile.ModProjectile is EbonHarpString
            && EbonLoomHarpRules.TryDecode(projectile.ai[2], out int rank, out bool last, out _))
        {
            scheduledRank = rank;
            scheduledLast = last;
        }
        return true;
    }

    public override void PostAI(Projectile projectile)
    {
        if (!projectile.active || Main.dedServ) return;
        if (projectile.ModProjectile is EbonNeedleArrow) Needle(projectile);
        else Thread(projectile);
    }

    private void Needle(Projectile projectile)
    {
        if (!seen)
        {
            seen = true;
            Vector2 muzzle = new(projectile.ai[0], projectile.ai[1]);
            needle = new LoomHarpNeedleSource(projectile, muzzle);
            if (!EbonPixelLayer.Add(needle)) needle = null;
            // The string lets go: the cue is the same event as the bow's release and the needle's first frame.
            EbonRewardAudio.Play("HarpLoose", muzzle, .65f, 0, .06f, 4);
        }
        Lighting.AddLight(projectile.Center, .28f, .32f, .5f);
    }

    private void Thread(Projectile projectile)
    {
        float state = projectile.ai[2];
        if (!seen)
        {
            seen = true;
            lastState = state < 0 ? 0 : state;
            thread = new LoomHarpStringSource(projectile);
            if (!EbonPixelLayer.Add(thread)) thread = null;
            // Building climbs the arpeggio: the n-th standing string lands the n-th note, the glissando replays them.
            if (state == 0) EbonRewardAudio.Note(Standing(projectile) - 1, new Vector2(projectile.ai[0], projectile.ai[1]), .32f);
        }
        if (lastState >= 0 && state < 0) Struck(projectile);
        lastState = state;
        if (state == 0) Crossing(projectile);
        else if (EbonLoomHarpRules.Live(state))
        {
            Vector2 a = projectile.Center, b = new(projectile.ai[0], projectile.ai[1]);
            Lighting.AddLight(a, .3f, .34f, .55f);
            Lighting.AddLight((a + b) * .5f, .3f, .34f, .55f);
            Lighting.AddLight(b, .3f, .34f, .55f);
        }
    }

    private static int Standing(Projectile projectile)
    {
        int count = 0;
        foreach (Projectile other in Main.ActiveProjectiles)
            if (other.type == projectile.type && other.owner == projectile.owner && other.ai[2] == 0) count++;
        return Math.Max(1, count);
    }

    // The pluck begins this tick: note, flash, the bow's own string rings, and the last string lands the chord.
    private void Struck(Projectile projectile)
    {
        // A string that arrived already plucked (late peer) never showed its schedule: it sounds as the first.
        int rank = scheduledRank;
        bool last = scheduledLast;
        thread?.Pluck(rank, last);
        Vector2 a = projectile.Center, b = new(projectile.ai[0], projectile.ai[1]), middle = (a + b) * .5f;
        EbonRewardAudio.Note(rank, middle, .5f + rank * .03f);
        Player owner = Main.player[projectile.owner];
        owner.GetModPlayer<LoomHarpPlayer>().Pulse();
        if (!last) return;
        EbonRewardAudio.Play("HarpChord", owner.Center, .85f, 0, .02f, 2);
        var config = ModContent.GetInstance<EbonVisualConfig>();
        if (projectile.owner == Main.myPlayer && !config.ReducedEffects && config.ScreenShake)
        {
            Vector2 along = b - a;
            Vector2 across = along.LengthSquared() > 1 ? Vector2.Normalize(along).RotatedBy(MathHelper.PiOver2) : Vector2.UnitY;
            ScreenShakeSystem.StartShakeAtPoint(middle, 1.1f, angularVariance: .2f, shakeDirection: across, shakeStrengthDissipationIncrement: .55f);
        }
    }

    // An enemy stepping onto an idle string makes it quiver once per crossing. Visual only, throttled and
    // limited to strings near the screen; the damage path never reads this.
    private void Crossing(Projectile projectile)
    {
        if (thread is null || (Main.GameUpdateCount + (ulong)projectile.whoAmI) % 2 != 0) return;
        Vector2 a = projectile.Center, b = new(projectile.ai[0], projectile.ai[1]);
        var box = new Rectangle((int)MathF.Min(a.X, b.X) - 16, (int)MathF.Min(a.Y, b.Y) - 16,
            (int)MathF.Abs(a.X - b.X) + 32, (int)MathF.Abs(a.Y - b.Y) + 32);
        var view = new Rectangle((int)Main.screenPosition.X - 200, (int)Main.screenPosition.Y - 200, Main.screenWidth + 400, Main.screenHeight + 400);
        if (!box.Intersects(view)) { crossing = false; return; }
        bool touching = false;
        float speed = 0;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.friendly || !npc.CanBeChasedBy()) continue;
            Rectangle hit = npc.Hitbox;
            if (!hit.Intersects(box)) continue;
            if (!EbonRewardRules.BoxTouchesSegment(new NVector2(hit.Left, hit.Top), new NVector2(hit.Right, hit.Bottom),
                    new NVector2(a.X, a.Y), new NVector2(b.X, b.Y), 4f)) continue;
            touching = true;
            speed = MathF.Max(speed, npc.velocity.Length());
        }
        if (touching && !crossing) thread.Kick(3.5f + MathF.Min(speed, 5f));
        crossing = touching;
    }

    // Final needle art (when delivered) is drawn here; otherwise the pixel layer draws the needle.
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is EbonNeedleArrow && EbonRewardArt.HasFinal("NeedleArrow"))
            DrawNeedle(EbonRewardArt.Final("NeedleArrow"), projectile.Center, projectile.rotation);
        return false;
    }

    // Final art is pixel art: point sampling. The projectile batch is swapped for one that is and restored after,
    // as the silk spool does. The art points right (point = right edge, middle row) and is turned over when the
    // needle flies left so its light stays on top. The point sits NeedleLead ahead of the centre.
    private static void DrawNeedle(Texture2D texture, Vector2 center, float rotation)
    {
        if (!float.IsFinite(rotation)) return;
        SpriteBatch batch = Main.spriteBatch;
        WorldBatchParameters saved = WorldBatchParameters.Capture(batch);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
            saved.Raster, null, saved.Transform);
        batch.Draw(texture, center + rotation.ToRotationVector2() * NeedleLead - Main.screenPosition, null, Color.White, rotation,
            new Vector2(texture.Width, texture.Height * .5f), LoomHarp.HeldScale,
            MathF.Cos(rotation) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
        batch.End();
        saved.Restore(batch);
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        needle?.Detach(projectile.Center);
        thread?.Detach();
        needle = null;
        thread = null;
        seen = crossing = false;
    }
}

// The needle in flight: a faint thread unspooling from the fire point to the tip (the string it is about to
// become), a bright silver needle at the tip, and a small star where the string let go. After the arrow ends the
// thread lingers a few ticks while the string source takes over.
internal sealed class LoomHarpNeedleSource : IEbonPixelSource
{
    private Projectile? arrow;
    private readonly Vector2 muzzle;
    private readonly ulong born;
    private readonly int seed;
    private readonly bool drawNeedle = !EbonRewardArt.HasFinal("NeedleArrow");
    private Vector2 tip, heading;
    private ulong endedAt;

    internal LoomHarpNeedleSource(Projectile arrow, Vector2 muzzle)
    {
        this.arrow = arrow;
        this.muzzle = tip = muzzle;
        born = Main.GameUpdateCount;
        seed = arrow.whoAmI * 7 + (int)(born & 127);
        heading = arrow.velocity.LengthSquared() > .01f ? Vector2.Normalize(arrow.velocity) : Vector2.UnitX;
    }

    internal void Detach(Vector2 end)
    {
        if (arrow is null) return;
        arrow = null;
        tip = end;
        endedAt = Main.GameUpdateCount;
    }

    public bool Emit(EbonPixelCanvas canvas)
    {
        ulong now = Main.GameUpdateCount;
        if (arrow is not null && (!arrow.active || arrow.ModProjectile is not EbonNeedleArrow)) Detach(arrow.Center);
        float fade = 1;
        if (arrow is not null)
        {
            tip = arrow.Center;
            if (arrow.velocity.LengthSquared() > .01f) heading = Vector2.Normalize(arrow.velocity);
        }
        else
        {
            fade = 1 - (now - endedAt + canvas.Fraction) / 4f;
            if (fade <= 0) return false;
        }
        float age = now - born + canvas.Fraction;
        if (Vector2.DistanceSquared(muzzle, tip) > 4) canvas.Thread(muzzle, tip, EbonTone.Silver, 1, .6f * fade, .5f);
        if (drawNeedle && arrow is not null)
        {
            canvas.Thread(tip - heading * 26, tip, EbonTone.Moon, 2, fade);
            canvas.Dot(tip, EbonTone.Ivory, 2, fade);
        }
        if (age < 7) canvas.Star(muzzle, 9, age / 7f, seed);
        return true;
    }
}

// One silk string. Idle: a shimmering thread that fades over its last StringFade ticks, with a landing star and a
// short twang when it is born and a quiver when an enemy crosses; armed (scheduled by the glissando): brighter, with glints at both
// ends; plucked: a flash of the cutting band, a ringing string, a star at both ends and a few motes, kept alive
// past the projectile's own short life so the ring-out is not cut off. A string retired early dissolves in 10 ticks.
internal sealed class LoomHarpStringSource : IEbonPixelSource
{
    private Projectile? thread;
    private readonly Vector2 a, b;
    private readonly int seed;
    private readonly ulong born = Main.GameUpdateCount;
    private ulong kickAt, pluckAt, detachedAt;
    private float kickAmp, lastAlpha = 1;
    private bool plucked, last;
    private int rank;

    internal LoomHarpStringSource(Projectile thread)
    {
        this.thread = thread;
        a = thread.Center;
        b = new Vector2(thread.ai[0], thread.ai[1]);
        seed = thread.whoAmI * 13 + (int)(Main.GameUpdateCount & 255);
        Kick(5f);
    }

    internal void Kick(float amplitude)
    {
        ulong now = Main.GameUpdateCount;
        // Keep a stronger quiver that is still ringing.
        if (EbonLoomHarpRules.Quiver(now - kickAt, kickAmp) > amplitude) return;
        kickAt = now;
        kickAmp = amplitude;
    }

    internal void Pluck(int rank, bool last)
    {
        plucked = true;
        pluckAt = Main.GameUpdateCount;
        this.rank = rank;
        this.last = last;
    }

    internal void Detach()
    {
        if (thread is null) return;
        thread = null;
        detachedAt = Main.GameUpdateCount;
    }

    public bool Emit(EbonPixelCanvas canvas)
    {
        ulong now = Main.GameUpdateCount;
        float fraction = canvas.Fraction;
        if (thread is not null && (!thread.active || thread.ModProjectile is not EbonHarpString)) Detach();
        if (plucked) return EmitPlucked(canvas, now - pluckAt + fraction);
        float alpha;
        bool armed = false;
        if (thread is not null)
        {
            armed = thread.ai[2] > 0;
            // A string called up by the glissando flares back to full, however far it had faded.
            alpha = lastAlpha = armed ? 1 : EbonLoomHarpRules.Fade(thread.timeLeft);
        }
        else
        {
            alpha = lastAlpha * (1 - (now - detachedAt + fraction) / 10f);
            if (alpha <= .02f) return false;
        }
        if (armed)
        {
            canvas.Thread(a, b, EbonTone.Ivory, 1, alpha, .8f);
            canvas.Dot(a, EbonTone.Moon, 1, alpha);
            canvas.Dot(b, EbonTone.Moon, 1, alpha);
            return true;
        }
        float since = now - kickAt + fraction;
        float amplitude = EbonLoomHarpRules.Quiver(since, kickAmp);
        if (amplitude > .3f) canvas.String(a, b, EbonTone.Ivory, amplitude, since * 1.9f, 1, alpha);
        else canvas.Thread(a, b, EbonTone.Silver, 1, alpha * .9f, .35f);
        // Where the needle stands; a small star marks the landing, on the beat of the note that climbs the arpeggio.
        canvas.Dot(b, EbonTone.Ivory, 1, alpha * .8f);
        float landed = now - born + fraction;
        if (landed < 7) canvas.Star(b, 10, landed / 7f, seed + 4);
        return true;
    }

    private bool EmitPlucked(EbonPixelCanvas canvas, float age)
    {
        if (age > EbonLoomHarpRules.RingOut) return false;
        float live = EbonRewardRules.PluckLive + 1;
        // The band that cuts, brightest at the pluck and gone with the live window.
        if (age < live) canvas.Thread(a, b, EbonTone.Moon, 3, 1 - age / live);
        canvas.String(a, b, EbonTone.Ivory, EbonLoomHarpRules.PluckAmplitude(age), age * 2.3f + rank * .7f, 1, 1 - age / EbonLoomHarpRules.RingOut);
        if (age < 12)
        {
            float radius = last ? 24 : 16;
            canvas.Star(a, radius, age / 12f, seed);
            canvas.Star(b, radius, age / 12f, seed + 1);
        }
        int motes = canvas.Reduced ? 2 : 5;
        canvas.Burst(a, seed + 2, motes, age, 22, 1.3f, .03f, EbonShardKind.Spark);
        canvas.Burst(b, seed + 3, motes, age, 22, 1.3f, .03f, EbonShardKind.Spark);
        return true;
    }
}
