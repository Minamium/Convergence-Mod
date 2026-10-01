#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Convergence.Content.Items.Oboro;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Moonshear's client presentation (REWARDS.md#melee). Reads accepted projectile state only: the shears
// sprite over the player, pixel-layer crescents/chalk/tear/stars/stitches, cues and the Snip's shake.
// Nothing here writes positions, input, hits or packets; dedicated servers never load it.
internal static class MoonshearArt
{
    // Placeholder: the Raid's Shears.png, two 360x140 halves with the pivot holes EbonScene measured.
    private static readonly Rectangle UpperSource = new(0, 0, 360, 140), LowerSource = new(0, 140, 360, 140);
    private static readonly Vector2 UpperPivot = new(159f, 21.5f), LowerPivot = new(137.7f, 121.8f);
    private const float PlaceholderScale = .6f; // one blade about 120 px
    internal const float Reach = EbonRewardRules.ShearReach;

    // Both halves turn about one shared pivot; a left facing mirrors them across their own axis.
    internal static void Draw(SpriteBatch batch, Vector2 pivot, float upper, float lower, int facing, float alpha)
    {
        if (alpha <= .01f) return;
        if (EbonRewardArt.HasFinal("ShearUpper") && EbonRewardArt.HasFinal("ShearLower"))
        {
            // Final pixel art: point sampled at the 2 px dot, pivot hole at 40% x / 50% y of each export.
            using var scope = new WorldGraphicsScope(batch);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D bottom = EbonRewardArt.Final("ShearLower"), top = EbonRewardArt.Final("ShearUpper");
            Half(batch, bottom, bottom.Bounds, new Vector2(bottom.Width * .4f, bottom.Height * .5f), EbonRewardArt.PixelScale, pivot, lower, facing, alpha);
            Half(batch, top, top.Bounds, new Vector2(top.Width * .4f, top.Height * .5f), EbonRewardArt.PixelScale, pivot, upper, facing, alpha);
            batch.End();
            return;
        }
        Texture2D sheet = EbonRewardArt.Raid("Shears");
        Half(batch, sheet, LowerSource, LowerPivot, PlaceholderScale, pivot, lower, facing, alpha);
        Half(batch, sheet, UpperSource, UpperPivot, PlaceholderScale, pivot, upper, facing, alpha);
    }

    private static void Half(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 hole, float scale,
        Vector2 pivot, float angle, int facing, float alpha)
    {
        bool mirror = facing < 0;
        batch.Draw(texture, pivot - Main.screenPosition, source, Color.White * alpha, angle,
            mirror ? new Vector2(hole.X, source.Height - hole.Y) : hole, scale,
            mirror ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
    }

    internal static ulong Elapsed(ulong now, ulong since) => now >= since ? now - since : 0;
}

// One kata stroke as drawn: snapshotted every tick from the accepted projectile and the same native
// hand basis the collision uses, so the crescent stays attached; its residue outlives the projectile.
internal sealed class MoonshearStrokeSource : IEbonPixelSource
{
    private const int Residue = 7, StarLife = 10, CloseLife = 14;
    private const float Inner = 34f;
    private int owner, stroke, ageTick, seed, facing, impacts;
    private float aim;
    private Vector2 handCenter, handAlong, handAcross;
    private ulong lastTick;
    private bool alive;
    private readonly Vector2[] impactAt = new Vector2[2];
    private readonly ulong[] impactTick = new ulong[2];

    internal void Track(Projectile projectile, MoonshearStroke cut, Player player)
    {
        var basis = OboroHandAnchor.Capture(player, cut.Facing);
        owner = projectile.owner; stroke = cut.Stroke; ageTick = cut.Age; aim = cut.Aim; facing = cut.Facing;
        seed = projectile.identity * 7 + stroke * 131 + owner * 977;
        handCenter = player.MountedCenter + new Vector2(basis.X, basis.Y);
        handAlong = new Vector2(basis.AlongX, basis.AlongY);
        handAcross = new Vector2(basis.AcrossX, basis.AcrossY);
        lastTick = Main.GameUpdateCount;
        alive = true;
    }

    internal void Impact(Vector2 at)
    {
        if (impacts >= impactAt.Length) return;
        impactAt[impacts] = at; impactTick[impacts] = Main.GameUpdateCount; impacts++;
    }

    internal void Release() => alive = false;

    private Vector2 Pivot(float age)
    {
        float arm = MoonshearMotion.ArmAngle(stroke, age, aim, facing);
        return handCenter + handAlong * MathF.Cos(arm) + handAcross * MathF.Sin(arm)
            + MoonshearMotion.AxisAngle(stroke, age, aim, facing).ToRotationVector2() * MoonshearMotion.Thrust(stroke, age);
    }

    public bool Emit(EbonPixelCanvas canvas)
    {
        ulong now = Main.GameUpdateCount;
        int since = (int)Math.Min(MoonshearArt.Elapsed(now, lastTick), 120UL);
        if (alive && since > 2) alive = false;
        float age = alive && since == 0 ? MoonshearMotion.DrawAge(stroke, ageTick, canvas.Fraction)
            : ageTick - 1f + since + canvas.Fraction;
        int release = MoonshearMotion.Release(stroke);
        // A stroke cancelled mid-way (item switch, Down) stops where it was: no phantom sweep after it.
        float stop = alive ? MoonshearMotion.LiveEnd(stroke) : Math.Min(MoonshearMotion.LiveEnd(stroke), ageTick);
        bool snip = stroke == MoonshearMotion.Snip, shut = snip && ageTick >= MoonshearMotion.SnipClose;
        bool busy = alive || age < stop + Residue || (shut && age < MoonshearMotion.SnipClose + CloseLife);
        for (int i = 0; i < impacts; i++) busy |= MoonshearArt.Elapsed(now, impactTick[i]) < StarLife;
        if (!busy || !Main.player[owner].active) return false;

        float head = MathF.Min(age, stop);
        if (head > release && age < stop + Residue)
        {
            // The whole accepted sweep from the release, dissolving from its tail once the cut is over.
            float tail = release - .2f, fade = Math.Clamp((age - stop) / Residue, 0f, 1f);
            float heat = snip ? .55f + .45f * EbonRewardRules.Smooth((age - release) / (MoonshearMotion.SnipClose - release))
                : stroke == MoonshearMotion.Wide ? .35f : .2f;
            Vector2 root = Pivot(head);
            for (int b = 0; b < 2; b++)
            {
                bool upper = b == 0;
                if (!MoonshearMotion.Cuts(stroke, upper)) continue;
                canvas.Crescent(root, MoonshearMotion.BladeAngle(stroke, tail, aim, facing, upper),
                    MoonshearMotion.BladeAngle(stroke, head, aim, facing, upper), Inner, MoonshearArt.Reach + 6, fade, heat, seed + b * 17);
            }
        }
        for (int i = 0; i < impacts; i++)
        {
            float t = MoonshearArt.Elapsed(now, impactTick[i]) + canvas.Fraction;
            if (t < StarLife) canvas.Star(impactAt[i], snip ? 24 : 16, t / StarLife, seed + 31 * i);
        }
        float closed = age - MoonshearMotion.SnipClose;
        if (shut && closed >= 0 && closed < CloseLife)
        {
            // The snip lands where the shut blades lie: one star, silk and sparks thrown along the cut.
            float axis = MoonshearMotion.AxisAngle(stroke, MoonshearMotion.SnipClose, aim, facing);
            Vector2 at = Pivot(MoonshearMotion.SnipClose) + axis.ToRotationVector2() * (MoonshearArt.Reach * .6f);
            canvas.Star(at, 34, closed / CloseLife, seed + 7);
            canvas.Burst(at, seed + 11, canvas.Reduced ? 5 : 10, closed, CloseLife, 3.2f, .1f, EbonShardKind.Silk, 1.4f, axis);
            canvas.Burst(at, seed + 23, canvas.Reduced ? 3 : 6, closed, CloseLife * .7f, 4.5f, .06f, EbonShardKind.Spark, 1f, axis);
        }
        return true;
    }
}

// The Cut Line: a tailor's-chalk forecast growing to the cursor, the tear chasing the racing shears,
// the live rip and its silk, then a short fade. A cancelled cut (item switch, Down) wipes away quickly.
internal sealed class MoonshearCutSource : IEbonPixelSource
{
    private const int Fade = 12, CancelFade = 6;
    private int owner, ageTick, seed;
    private Vector2 start, end;
    private ulong lastTick;
    private bool alive;

    internal void Track(Projectile projectile, MoonshearCutLine cut)
    {
        owner = projectile.owner; ageTick = cut.Age; start = cut.Start; end = cut.End;
        seed = projectile.identity * 13 + owner * 977;
        lastTick = Main.GameUpdateCount;
        alive = true;
    }

    internal void Release() => alive = false;

    public bool Emit(EbonPixelCanvas canvas)
    {
        ulong now = Main.GameUpdateCount;
        int since = (int)Math.Min(MoonshearArt.Elapsed(now, lastTick), 120UL);
        if (alive && since > 2) alive = false;
        if (!Main.player[owner].active) return false;
        float age = ageTick - 1f + since + canvas.Fraction;
        float alpha = 1;
        if (!alive && ageTick < MoonshearMotion.TearEnd)
        {
            alpha = Math.Clamp(1 - (since + canvas.Fraction) / CancelFade, 0f, 1f);
            if (alpha <= 0) return false;
            age = ageTick;
        }
        if (age >= MoonshearMotion.TearEnd + Fade) return false;

        float chalk = age < MoonshearMotion.TearStart ? 1 : 1 - Math.Clamp((age - MoonshearMotion.TearStart) / 6f, 0f, 1f);
        if (chalk > 0)
            canvas.Band(start, end, MoonshearMotion.ChalkWidth, EbonBandMode.Chalk, MoonshearMotion.Forecast(age), chalk * alpha, seed);
        if (age < MoonshearMotion.TravelStart)
            canvas.Dot(Vector2.Lerp(start, end, MoonshearMotion.Forecast(age)), EbonTone.Ivory, 2, alpha);
        else
        {
            float tear = age < MoonshearMotion.TearEnd ? 1 : 1 - (age - MoonshearMotion.TearEnd) / Fade;
            canvas.Band(start, end, EbonRewardRules.CutWidth, EbonBandMode.Tear, MoonshearMotion.Travel(age), tear * alpha, seed + 1);
        }
        float torn = age - MoonshearMotion.TearStart;
        if (torn >= 0 && torn < 18 && alpha >= 1)
        {
            float angle = (end - start).ToRotation();
            int points = canvas.Reduced ? 2 : 4;
            for (int i = 0; i < points; i++)
            {
                Vector2 at = Vector2.Lerp(start, end, (i + .5f) / points);
                float side = (i & 1) == 0 ? MathF.PI / 2 : -MathF.PI / 2;
                canvas.Burst(at, seed + 13 * i, canvas.Reduced ? 3 : 6, torn, 18, 2.6f, .08f, EbonShardKind.Silk, 1.6f, angle + side);
            }
            if (torn < 12) canvas.Star(end, 30, torn / 12, seed + 5);
        }
        return true;
    }
}

// One mark bursting: a star and a pinch of silk on the NPC, the stitch itself flashing out.
internal sealed class MoonshearPopSource : IEbonPixelSource
{
    private const int Life = 14;
    private readonly Vector2 at;
    private readonly ulong born;
    private readonly int seed;
    private readonly bool last;

    internal MoonshearPopSource(Vector2 at, int seed, bool last)
    {
        this.at = at; this.seed = seed; this.last = last; born = Main.GameUpdateCount;
    }

    public bool Emit(EbonPixelCanvas canvas)
    {
        float t = MoonshearArt.Elapsed(Main.GameUpdateCount, born) + canvas.Fraction;
        if (t >= Life) return false;
        canvas.Star(at, last ? 30 : 20, t / Life, seed);
        if (t < 6) canvas.Stitch(at + new Vector2(0, -t * 2), 3, EbonTone.Rose, 1 - t / 6);
        canvas.Burst(at, seed + 3, canvas.Reduced ? (last ? 4 : 2) : (last ? 8 : 4), t, Life, 2.2f, .08f, EbonShardKind.Silk);
        return true;
    }
}

// The owner's chalk marks: a small row of 10 px stitched crosses over each marked NPC. Owner only: marks are
// owner bookkeeping, not replicated. Registered while any mark exists; the layer drops it on world unload.
internal sealed class MoonshearMarkSource : IEbonPixelSource
{
    internal static readonly MoonshearMarkSource Instance = new();
    internal static bool Registered;

    public bool Emit(EbonPixelCanvas canvas)
    {
        Player player = Main.LocalPlayer;
        MoonshearMarks? marks = player.active ? player.GetModPlayer<MoonshearPlayer>().Marks : null;
        if (marks is null || !marks.Any()) { Registered = false; return false; }
        ulong now = Main.GameUpdateCount;
        // Keep a huge boss's marks inside the zoomed view.
        Vector2 screen = new(Main.screenWidth, Main.screenHeight), view = screen / Math.Max(.1f, Main.GameViewMatrix.Zoom.X);
        Vector2 min = Main.screenPosition + (screen - view) * .5f + new Vector2(24, 36), max = min + view - new Vector2(48, 72);
        for (int i = 0; i < marks.Capacity; i++)
        {
            int count = marks.Count(i);
            if (count == 0 || !Main.npc[i].active) continue;
            NPC npc = Main.npc[i];
            float age = MoonshearArt.Elapsed(now, marks.Stamp(i)) + canvas.Fraction;
            float alpha = Math.Clamp((EbonRewardRules.MarkLife - age) / 60f, .25f, 1f);
            Vector2 anchor = Vector2.Clamp(new Vector2(npc.Center.X, npc.Top.Y - 14 + npc.gfxOffY), min, max);
            EbonTone tone = count >= EbonRewardRules.MaxMarks ? EbonTone.Moon : EbonTone.Rose;
            for (int k = 0; k < count; k++)
            {
                // The newest stitch is sewn in over a few ticks.
                float sewn = k == count - 1 ? EbonRewardRules.Smooth(age / 6f) : 1f;
                canvas.Stitch(anchor + new Vector2((k - (count - 1) * .5f) * 12f, 0), 2, tone, alpha * sewn);
            }
        }
        return true;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class MoonshearStrokeVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is MoonshearStroke;

    private MoonshearStrokeSource? source;
    private bool sourceTried, released, closed;
    internal bool Noted;
    private int lastObservedAge = -1, impactCount;
    private ulong impactTick;
    private ScreenShakeSystem.ShakeInfo? shake;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not MoonshearStroke cut || !projectile.active || cut.Age > MoonshearMotion.Duration(cut.Stroke)) return;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.type != ModContent.ItemType<EbonMoonshear>()) return;
        float arm = MoonshearMotion.ArmAngle(cut.Stroke, cut.Age, cut.Aim, cut.Facing);
        player.ChangeDir(cut.Facing);
        player.SetCompositeArmFront(true, OboroHandAnchor.Stretch(player, cut.Hand(player, cut.Age), arm), arm - MathF.PI / 2);
        if (!sourceTried)
        {
            sourceTried = true;
            var created = new MoonshearStrokeSource();
            created.Track(projectile, cut, player);
            if (EbonPixelLayer.Add(created)) source = created;
        }
        source?.Track(projectile, cut, player);

        int previous = lastObservedAge, release = MoonshearMotion.Release(cut.Stroke);
        lastObservedAge = cut.Age;
        if (!released && previous < release && cut.Age >= release && cut.Age <= release + 2)
        {
            released = true;
            // The swing cue fires with the crescent's first live frame.
            if (cut.Stroke == MoonshearMotion.Rise) EbonRewardAudio.Play("ShearSwingRise", player.Center, .72f);
            else if (cut.Stroke == MoonshearMotion.Snip) EbonRewardAudio.Play("ShearSwing", player.Center, .55f, -.2f);
            else EbonRewardAudio.Play("ShearSwing", player.Center, .7f, cut.Stroke == MoonshearMotion.Wide ? -.08f : 0);
        }
        int close = MoonshearMotion.SnipClose;
        if (cut.Stroke != MoonshearMotion.Snip || closed || previous >= close || cut.Age < close || cut.Age > close + 2) return;
        closed = true;
        float axis = MoonshearMotion.AxisAngle(cut.Stroke, close, cut.Aim, cut.Facing);
        EbonRewardAudio.Play("ShearSnip", cut.Pivot(player, close) + axis.ToRotationVector2() * MoonshearArt.Reach * .6f, .9f);
        var config = ModContent.GetInstance<EbonVisualConfig>();
        if (projectile.owner == Main.myPlayer && !config.ReducedEffects && config.ScreenShake)
            shake = ScreenShakeSystem.StartShakeAtPoint(player.Center, 3.5f, angularVariance: .22f,
                shakeDirection: axis.ToRotationVector2(), shakeStrengthDissipationIncrement: .65f);
    }

    // Native hit callback (owner only): a contact star, bounded so a crowd cannot stack flashes.
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ || projectile.ModProjectile is not MoonshearStroke cut || damageDone <= 0 || source is null) return;
        bool snip = cut.Stroke == MoonshearMotion.Snip;
        if (impactCount >= (snip ? 2 : 1) || (impactCount > 0 && Main.GameUpdateCount - impactTick < 4)) return;
        impactCount++;
        impactTick = Main.GameUpdateCount;
        Vector2 pivot = cut.Pivot(Main.player[projectile.owner], cut.Age);
        Vector2 tip = pivot + cut.BladeAngle(cut.Age, MoonshearMotion.Cuts(cut.Stroke, true)).ToRotationVector2() * MoonshearArt.Reach * .8f;
        source.Impact(new Vector2(Math.Clamp(tip.X, target.Left.X, target.Right.X), Math.Clamp(tip.Y, target.Top.Y, target.Bottom.Y)));
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not MoonshearStroke cut) return false;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.type != ModContent.ItemType<EbonMoonshear>()) return false;
        float age = MoonshearMotion.DrawAge(cut.Stroke, cut.Age, WeaponDrawClock.Fraction);
        MoonshearArt.Draw(Main.spriteBatch, cut.Pivot(player, age), cut.BladeAngle(age, true), cut.BladeAngle(age, false), cut.Facing, 1f);
        return false;
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        source?.Release();
        if (shake is not null) shake.ShakeStrength = 0;
        shake = null;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class MoonshearCutVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is MoonshearCutLine;

    private MoonshearCutSource? source;
    private bool sourceTried, launched, torn;
    private int lastObservedAge = -1;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not MoonshearCutLine cut || !projectile.active) return;
        Player player = Main.player[projectile.owner];
        if (!player.active) return;
        if (!sourceTried)
        {
            sourceTried = true;
            var created = new MoonshearCutSource();
            created.Track(projectile, cut);
            if (EbonPixelLayer.Add(created)) source = created;
        }
        source?.Track(projectile, cut);
        int age = cut.Age, previous = lastObservedAge;
        lastObservedAge = age;
        if (age < MoonshearMotion.TearEnd && player.HeldItem.type == ModContent.ItemType<EbonMoonshear>())
        {
            // The arm traces the chalk, then stays out after the thrown shears until the tear closes.
            float angle = cut.LineAngle;
            player.ChangeDir(MathF.Cos(angle) < 0 ? -1 : 1);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, angle - MathF.PI / 2);
        }
        if (!launched && previous < MoonshearMotion.TravelStart && age >= MoonshearMotion.TravelStart && age <= MoonshearMotion.TravelStart + 2)
        {
            launched = true;
            EbonRewardAudio.Play("ShearSwing", cut.Start, .6f, .15f);
        }
        if (!torn && previous < MoonshearMotion.TearStart && age >= MoonshearMotion.TearStart && age <= MoonshearMotion.TearStart + 2)
        {
            torn = true;
            EbonRewardAudio.Play("ShearCut", Vector2.Lerp(cut.Start, cut.End, .5f), .85f);
        }
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not MoonshearCutLine cut) return false;
        Player player = Main.player[projectile.owner];
        float age = Math.Max(0, cut.Age - 1f + WeaponDrawClock.Fraction);
        if (!player.active || age >= MoonshearMotion.TearEnd) return false;
        float angle = cut.LineAngle;
        int facing = MathF.Cos(angle) < 0 ? -1 : 1;
        Vector2 pivot;
        float alpha = 1;
        if (age < MoonshearMotion.TravelStart)
        {
            // Held at the hand, pointing down the chalk, opening as the line is measured out.
            if (player.HeldItem.type != ModContent.ItemType<EbonMoonshear>()) return false;
            var point = OboroHandAnchor.Capture(player, facing).At(angle);
            pivot = player.MountedCenter + new Vector2(point.X, point.Y);
        }
        else
        {
            // The open shears race along the line, the tear opening at their crotch; they shut and fade at the end.
            pivot = Vector2.Lerp(cut.Start, cut.End, MoonshearMotion.Travel(age));
            if (age >= MoonshearMotion.TearStart) alpha = 1 - (age - MoonshearMotion.TearStart) / EbonRewardRules.CutLive;
        }
        float open = MoonshearMotion.RaceOpen(age);
        MoonshearArt.Draw(Main.spriteBatch, pivot, angle - facing * open, angle + facing * open, facing, alpha);
        return false;
    }

    public override void OnKill(Projectile projectile, int timeLeft) => source?.Release();
}

[Autoload(Side = ModSide.Client)]
internal sealed class MoonshearPopVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is MoonshearPop;

    private bool shown;

    // First tick seen on any peer: the star, the next arpeggio note and, on the last pop, the chord.
    public override void PostAI(Projectile projectile)
    {
        if (shown || projectile.ModProjectile is not MoonshearPop pop || !projectile.active) return;
        shown = true;
        Vector2 at = projectile.Center;
        EbonPixelLayer.Add(new MoonshearPopSource(at, projectile.identity * 17 + pop.Step * 5, pop.Last));
        EbonRewardAudio.Note(pop.Step, at, .62f);
        if (pop.Last) EbonRewardAudio.Play("ShearCut", at, .75f, 0, .02f, 2);
    }
}

// Sub-tick arm pose (the WeaponArmDraw technique) and owner mark-layer registration.
[Autoload(Side = ModSide.Client)]
internal sealed class MoonshearClientPlayer : ModPlayer
{
    private ulong retryAt;

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender || Player.dead || !Player.active || Player.HeldItem.ModItem is not EbonMoonshear) return;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Player.whoAmI || p.ModProjectile is not MoonshearStroke cut || cut.Age > MoonshearMotion.Duration(cut.Stroke)) continue;
            float age = MoonshearMotion.DrawAge(cut.Stroke, cut.Age, WeaponDrawClock.Fraction);
            float rotation = MoonshearMotion.ArmAngle(cut.Stroke, age, cut.Aim, cut.Facing) - MathF.PI / 2f;
            drawInfo.compositeFrontArmRotation = Player.gravDir == -1f ? -rotation : rotation;
            return;
        }
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || MoonshearMarkSource.Registered || Main.GameUpdateCount < retryAt) return;
        if (Player.GetModPlayer<MoonshearPlayer>().Marks?.Any() != true) return;
        MoonshearMarkSource.Registered = EbonPixelLayer.Add(MoonshearMarkSource.Instance);
        if (!MoonshearMarkSource.Registered) retryAt = Main.GameUpdateCount + 30;
    }

    public override void OnEnterWorld() => retryAt = 0;
}

// Assigns the Content mark hook and clears client state on world/Mod unload.
[Autoload(Side = ModSide.Client)]
internal sealed class MoonshearVisualSystem : ModSystem
{
    public override void Load() => MoonshearStroke.Marked = OnMarked;

    public override void Unload()
    {
        MoonshearStroke.Marked = null;
        MoonshearMarkSource.Registered = false;
    }

    public override void OnWorldUnload() => MoonshearMarkSource.Registered = false;

    // A building hit climbs the arpeggio by the NPC's mark count, once per stroke.
    private static void OnMarked(Projectile projectile, NPC target, int marks)
    {
        if (Main.dedServ || marks <= 0 || projectile.ModProjectile is not MoonshearStroke) return;
        var visuals = projectile.GetGlobalProjectile<MoonshearStrokeVisuals>();
        if (visuals.Noted) return;
        visuals.Noted = true;
        EbonRewardAudio.Note(marks - 1, target.Center, .4f);
    }
}
